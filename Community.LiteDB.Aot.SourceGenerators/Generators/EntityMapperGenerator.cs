using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Community.LiteDB.Aot.SourceGenerators.Models;
using Community.LiteDB.Aot.SourceGenerators.Helpers;

namespace Community.LiteDB.Aot.SourceGenerators.Generators;

/// <summary>
/// Incremental source generator for LiteDB.Aot entity mappers
/// </summary>
[Generator]
public class EntityMapperGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all classes that might be DbContext
        var dbContextClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsPotentialDbContext(node),
                transform: static (ctx, _) => GetDbContextInfo(ctx))
            .Where(static context => context is not null);
        
        // Generate code for each DbContext
        context.RegisterSourceOutput(dbContextClasses, static (spc, dbContext) =>
        {
            if (dbContext == null) return;
            
            GenerateMappers(spc, dbContext);
        });
    }
    
    private static bool IsPotentialDbContext(SyntaxNode node)
    {
        // Look for class declarations with "DbContext" in the name or base type
        return node is ClassDeclarationSyntax classDecl &&
               (classDecl.Identifier.Text.Contains("Context") ||
                classDecl.Identifier.Text.Contains("DbContext"));
    }
    
    private static DbContextInfo? GetDbContextInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        
        // Get the symbol for the class
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        if (classSymbol == null) return null;
        
        // Check if it inherits from LiteDbContext
        if (!SyntaxHelper.InheritsFrom(classSymbol, "LiteDbContext"))
            return null;
        
        var dbContextInfo = new DbContextInfo
        {
            ClassName = classSymbol.Name,
            Namespace = classSymbol.ContainingNamespace.ToDisplayString()
        };
        
        // Find OnModelCreating method
        var onModelCreatingMethod = classDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == "OnModelCreating");
        
        if (onModelCreatingMethod == null)
            return dbContextInfo; // Empty context is OK
        
        // Parse Entity<T>() calls
        var entityCalls = SyntaxHelper.FindMethodInvocations(onModelCreatingMethod, "Entity");
        
        foreach (var entityCall in entityCalls)
        {
            var entityTypeName = SyntaxHelper.GetGenericTypeArgument(entityCall);
            if (entityTypeName == null) continue;
            
            // Get the type symbol - try to resolve from syntax context
            INamedTypeSymbol? entityType = null;
            
            // First, try to get it from the syntax directly
            if (entityCall.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name is GenericNameSyntax genericName)
            {
                var typeArg = genericName.TypeArgumentList.Arguments.FirstOrDefault();
                if (typeArg != null)
                {
                    var typeInfo = semanticModel.GetTypeInfo(typeArg);
                    entityType = typeInfo.Type as INamedTypeSymbol;
                }
            }
            
            // Fallback: try GetTypeByMetadataName with full namespace
            if (entityType == null)
            {
                entityType = semanticModel.Compilation.GetTypeByMetadataName(entityTypeName);
            }
            
            // Last resort: search by name
            if (entityType == null)
            {
                entityType = semanticModel.Compilation.GetSymbolsWithName(entityTypeName)
                    .OfType<INamedTypeSymbol>()
                    .FirstOrDefault();
            }
            
            if (entityType == null) continue;
            
            var entityInfo = AnalyzeEntity(entityType, entityCall, semanticModel);
            if (entityInfo != null)
            {
                dbContextInfo.Entities.Add(entityInfo);
            }
        }
        
        return dbContextInfo;
    }
    
    private static EntityInfo? AnalyzeEntity(
        INamedTypeSymbol entityType,
        InvocationExpressionSyntax entityCall,
        SemanticModel semanticModel)
    {
        var entityInfo = new EntityInfo
        {
            Name = entityType.Name,
            Namespace = entityType.ContainingNamespace.ToDisplayString(),
            FullTypeName = SyntaxHelper.GetFullName(entityType),
            CollectionName = entityType.Name.ToLowerInvariant() + "s" // Default plural
        };
        
        // Get all properties from the entity type
        var properties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && p.GetMethod != null)
            .ToList();
        
        foreach (var property in properties)
        {
            // Check if property should be ignored via attributes
            if (AttributeHelper.ShouldIgnore(property))
            {
                entityInfo.IgnoredProperties.Add(property.Name);
                continue;
            }
            
            var propInfo = new Models.PropertyInfo
            {
                Name = property.Name,
                TypeName = GetTypeName(property.Type),
                BsonFieldName = property.Name,
                IsNullable = IsNullableType(property.Type)
            };
            
            // Analyze property setter accessibility (for DDD support)
            var setMethod = property.SetMethod;
            if (setMethod != null)
            {
                propInfo.HasPublicSetter = setMethod.DeclaredAccessibility == Accessibility.Public;
                propInfo.HasInitOnlySetter = setMethod.IsInitOnly;
                
                // Generate backing field name for non-public setters
                if (!propInfo.HasPublicSetter || propInfo.HasInitOnlySetter)
                {
                    // Compiler-generated backing field format: <PropertyName>k__BackingField
                    propInfo.BackingFieldName = $"<{property.Name}>k__BackingField";
                }
            }
            else
            {
                // No setter - readonly property
                propInfo.HasPublicSetter = false;
            }
            
            // Read Data Annotations attributes
            propInfo.IsKey = AttributeHelper.IsKey(property);
            propInfo.IsRequired = AttributeHelper.IsRequired(property);
            propInfo.IsReadOnly = AttributeHelper.IsReadOnly(property);
            propInfo.DisplayName = AttributeHelper.GetDisplayName(property);
            propInfo.Description = AttributeHelper.GetDescription(property);
            propInfo.DefaultValue = AttributeHelper.GetDefaultValue(property);
            
            // Validation attributes
            propInfo.MaxLength = AttributeHelper.GetStringLength(property);
            propInfo.MinLength = AttributeHelper.GetMinLength(property);
            var (rangeMin, rangeMax) = AttributeHelper.GetRange(property);
            propInfo.RangeMin = rangeMin;
            propInfo.RangeMax = rangeMax;
            propInfo.RegularExpression = AttributeHelper.GetRegularExpression(property);
            
            // Additional validation attributes
            propInfo.IsEmailAddress = AttributeHelper.IsEmailAddress(property);
            propInfo.IsPhone = AttributeHelper.IsPhone(property);
            propInfo.IsUrl = AttributeHelper.IsUrl(property);
            propInfo.IsCreditCard = AttributeHelper.IsCreditCard(property);
            propInfo.CompareProperty = AttributeHelper.GetCompareProperty(property);
            
            // Detect collection types
            if (IsCollectionType(property.Type, out var itemType))
            {
                propInfo.IsCollection = true;
                propInfo.CollectionItemType = itemType?.Name;
                
                // Check if collection item is a nested object
                if (itemType != null && IsNestedObjectType(itemType))
                {
                    propInfo.IsCollectionItemNested = true;
                    propInfo.NestedTypeName = itemType.Name;
                    propInfo.NestedTypeFullName = itemType.ToDisplayString();
                }
            }
            // Detect nested object types
            else if (IsNestedObjectType(property.Type))
            {
                propInfo.IsNestedObject = true;
                propInfo.NestedTypeName = property.Type.Name;
                propInfo.NestedTypeFullName = property.Type.ToDisplayString();
            }
            
            entityInfo.Properties.Add(propInfo);
        }
        
        // Parse configuration from OnModelCreating
        ParseEntityConfiguration(entityInfo, entityCall);
        
        // Analyze nested types
        AnalyzeNestedTypes(entityInfo, entityType, semanticModel);
        
        // If no ID found from configuration, check for [Key] attribute
        if (entityInfo.IdProperty == null)
        {
            entityInfo.IdProperty = entityInfo.Properties.FirstOrDefault(p => p.IsKey);
            if (entityInfo.IdProperty != null)
            {
                // [Key] attribute found - enable AutoId by default for int/long types
                var idTypeName = entityInfo.IdProperty.TypeName.TrimEnd('?');
                if (idTypeName == "int" || idTypeName == "Int32" || 
                    idTypeName == "long" || idTypeName == "Int64")
                {
                    entityInfo.AutoId = true;
                }
            }
        }
        
        // If still no ID found, use "Id" property by convention
        if (entityInfo.IdProperty == null)
        {
            entityInfo.IdProperty = entityInfo.Properties.FirstOrDefault(p => p.Name == "Id");
            if (entityInfo.IdProperty != null)
            {
                entityInfo.AutoId = true; // Default to AutoId
            }
        }
        
        return entityInfo;
    }
    
    private static string GetTypeName(ITypeSymbol type)
    {
        // Handle nullable value types (int?, DateTime?, etc.)
        if (type is INamedTypeSymbol namedType && 
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            var underlyingType = namedType.TypeArguments[0];
            return underlyingType.Name + "?";
        }
        
        // Handle arrays
        if (type is IArrayTypeSymbol arrayType)
        {
            return GetTypeName(arrayType.ElementType) + "[]";
        }
        
        // Regular types
        return type.Name;
    }
    
    private static bool IsNullableType(ITypeSymbol type)
    {
        // Nullable value types (int?, DateTime?, etc.)
        if (type is INamedTypeSymbol namedType && 
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }
        
        // Nullable reference types (string?, Customer?, etc.)
        return type.NullableAnnotation == NullableAnnotation.Annotated;
    }
    
    private static bool IsCollectionType(ITypeSymbol type, out ITypeSymbol? itemType)
    {
        itemType = null;
        
        // Handle arrays
        if (type is IArrayTypeSymbol arrayType)
        {
            itemType = arrayType.ElementType;
            return true;
        }
        
        // Handle generic collections (List<T>, IEnumerable<T>, etc.)
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeDefName = namedType.OriginalDefinition.ToDisplayString();
            
            if (typeDefName.StartsWith("System.Collections.Generic.List<") ||
                typeDefName.StartsWith("System.Collections.Generic.IList<") ||
                typeDefName.StartsWith("System.Collections.Generic.ICollection<") ||
                typeDefName.StartsWith("System.Collections.Generic.IEnumerable<"))
            {
                itemType = namedType.TypeArguments[0];
                return true;
            }
        }
        
        return false;
    }
    
    private static bool IsPrimitiveType(ITypeSymbol type)
    {
        // Handle nullable value types
        if (type is INamedTypeSymbol namedType && 
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            type = namedType.TypeArguments[0];
        }
        
        var typeName = type.Name;
        
        // Primitive types
        if (type.SpecialType != SpecialType.None && type.SpecialType != SpecialType.System_Object)
        {
            return true;
        }
        
        // Common value types
        if (typeName == "Guid" || typeName == "DateTime" || typeName == "DateTimeOffset" || 
            typeName == "TimeSpan" || typeName == "Decimal")
        {
            return true;
        }
        
        // Arrays of primitives
        if (type is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType.Name == "Byte"; // byte[]
        }
        
        // Enums
        if (type.TypeKind == TypeKind.Enum)
        {
            return true;
        }
        
        return false;
    }
    
    private static bool IsNestedObjectType(ITypeSymbol type)
    {
        // Not a primitive type
        if (IsPrimitiveType(type))
        {
            return false;
        }
        
        // Not string
        if (type.SpecialType == SpecialType.System_String)
        {
            return false;
        }
        
        // Not a collection
        if (IsCollectionType(type, out _))
        {
            return false;
        }
        
        // Not object
        if (type.SpecialType == SpecialType.System_Object)
        {
            return false;
        }
        
        // Must be a class or struct
        if (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct)
        {
            return true;
        }
        
        return false;
    }
    
    private static void ParseEntityConfiguration(EntityInfo entityInfo, InvocationExpressionSyntax entityCall)
    {
        // Find HasKey call
        var hasKeyCall = SyntaxHelper.FindMethodInvocations(entityCall, "HasKey").FirstOrDefault();
        if (hasKeyCall != null && hasKeyCall.ArgumentList.Arguments.Count > 0)
        {
            var keyExpression = hasKeyCall.ArgumentList.Arguments[0].Expression;
            var keyPropertyName = SyntaxHelper.GetPropertyNameFromLambda(keyExpression);
            
            if (keyPropertyName != null)
            {
                var idProp = entityInfo.Properties.FirstOrDefault(p => p.Name == keyPropertyName);
                if (idProp != null)
                {
                    entityInfo.IdProperty = idProp;
                    
                    // Check for AutoIncrement
                    var parent = hasKeyCall.Parent;
                    if (parent is MemberAccessExpressionSyntax memberAccess)
                    {
                        if (memberAccess.Name.Identifier.Text == "AutoIncrement")
                        {
                            entityInfo.AutoId = true;
                        }
                        
                        // Check for HasConversion (ValueObject ID conversion)
                        // Look for: .HasKey(x => x.Id).HasConversion(toDb: ..., fromDb: ...)
                        var currentNode = memberAccess.Parent;
                        while (currentNode != null)
                        {
                            if (currentNode is InvocationExpressionSyntax invocation &&
                                invocation.Expression is MemberAccessExpressionSyntax conversionMember &&
                                conversionMember.Name.Identifier.Text == "HasConversion")
                            {
                                // Extract conversion lambda expressions
                                if (invocation.ArgumentList.Arguments.Count >= 2)
                                {
                                    ExpressionSyntax? toDbExpr = null;
                                    ExpressionSyntax? fromDbExpr = null;

                                    // Try to find by name "toDb" and "fromDb"
                                    foreach (var arg in invocation.ArgumentList.Arguments)
                                    {
                                        if (arg.NameColon?.Name.Identifier.Text == "toDb")
                                            toDbExpr = arg.Expression;
                                        else if (arg.NameColon?.Name.Identifier.Text == "fromDb")
                                            fromDbExpr = arg.Expression;
                                    }
                                    
                                    // Fallback to positional arguments if names not found (0=toDb, 1=fromDb)
                                    if (toDbExpr == null && fromDbExpr == null)
                                    {
                                        toDbExpr = invocation.ArgumentList.Arguments[0].Expression;
                                        fromDbExpr = invocation.ArgumentList.Arguments[1].Expression;
                                    }
                                    
                                    if (toDbExpr != null && fromDbExpr != null)
                                    {
                                        // Parse lambda bodies using LambdaParser
                                        var toDbBody = LambdaParser.GetLambdaBody(toDbExpr);
                                        var fromDbBody = LambdaParser.GetLambdaBody(fromDbExpr);
                                        
                                        if (toDbBody != null && fromDbBody != null)
                                        {
                                            entityInfo.IdConversionToDb = toDbBody;
                                            entityInfo.IdConversionFromDb = fromDbBody;
                                            entityInfo.IdConversionTargetType = "string"; // Always string for LiteDB
                                        }
                                    }
                                }
                                break;
                            }
                            currentNode = currentNode.Parent;
                        }
                    }
                }
            }
        }
        
        // Find ToCollection call
        var toCollectionCall = SyntaxHelper.FindMethodInvocations(entityCall, "ToCollection").FirstOrDefault();
        if (toCollectionCall != null && toCollectionCall.ArgumentList.Arguments.Count > 0)
        {
            var collectionNameArg = toCollectionCall.ArgumentList.Arguments[0].Expression;
            if (collectionNameArg is LiteralExpressionSyntax literal)
            {
                entityInfo.CollectionName = literal.Token.ValueText;
            }
        }
        
        // Find Ignore calls
        var ignoreCalls = SyntaxHelper.FindMethodInvocations(entityCall, "Ignore");
        foreach (var ignoreCall in ignoreCalls)
        {
            if (ignoreCall.ArgumentList.Arguments.Count > 0)
            {
                var ignoreExpression = ignoreCall.ArgumentList.Arguments[0].Expression;
                var propertyName = SyntaxHelper.GetPropertyNameFromLambda(ignoreExpression);
                if (propertyName != null)
                {
                    entityInfo.IgnoredProperties.Add(propertyName);
                }
            }
        }
    }
    
    private static void AnalyzeNestedTypes(EntityInfo entityInfo, INamedTypeSymbol entityType, SemanticModel semanticModel)
    {
        // Maximum nesting depth (3 levels: Customer -> Address -> Country)
        const int MaxDepth = 3;
        
        // Track analyzed types to prevent infinite recursion
        var analyzedTypes = new HashSet<string>();
        
        // Start recursive analysis from depth 1
        AnalyzeNestedTypesRecursive(entityInfo, entityInfo.Properties, entityInfo.NestedTypes, semanticModel, analyzedTypes, 1, MaxDepth);
    }
    
    private static void AnalyzeNestedTypesRecursive(
        EntityInfo rootEntity,
        List<Models.PropertyInfo> properties,
        Dictionary<string, Models.NestedTypeInfo> targetNestedTypes,
        SemanticModel semanticModel,
        HashSet<string> analyzedTypes,
        int currentDepth,
        int maxDepth)
    {
        // Stop if we've reached max depth
        if (currentDepth > maxDepth)
            return;
        
        // Find all nested object properties at this level
        var nestedProps = properties
            .Where(p => p.IsNestedObject && !string.IsNullOrEmpty(p.NestedTypeName))
            .ToList();
        
        foreach (var nestedProp in nestedProps)
        {
            var nestedTypeName = nestedProp.NestedTypeName!;
            var nestedTypeFullName = nestedProp.NestedTypeFullName ?? nestedTypeName;
            
            // Skip if already analyzed (prevent cycles)
            if (analyzedTypes.Contains(nestedTypeFullName))
            {
                // Reference to already analyzed type - just mark it
                if (!targetNestedTypes.ContainsKey(nestedTypeName) && rootEntity.NestedTypes.ContainsKey(nestedTypeName))
                {
                    targetNestedTypes[nestedTypeName] = rootEntity.NestedTypes[nestedTypeName];
                }
                continue;
            }
            
            // Skip if already in this collection
            if (targetNestedTypes.ContainsKey(nestedTypeName))
                continue;
            
            // Find the type symbol for this nested type
            var nestedTypeSymbol = FindNestedTypeSymbol(nestedTypeFullName, semanticModel);
            if (nestedTypeSymbol == null)
                continue;
            
            // Mark as analyzed
            analyzedTypes.Add(nestedTypeFullName);
            
            // Analyze the nested type
            var nestedTypeInfo = new Models.NestedTypeInfo
            {
                Name = nestedTypeSymbol.Name,
                Namespace = nestedTypeSymbol.ContainingNamespace.ToDisplayString(),
                FullTypeName = nestedTypeSymbol.ToDisplayString(),
                Depth = currentDepth
            };
            
            // Get properties of nested type
            var nestedProperties = nestedTypeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public && p.GetMethod != null)
                .ToList();
            
            foreach (var property in nestedProperties)
            {
                var propInfo = new Models.PropertyInfo
                {
                    Name = property.Name,
                    TypeName = GetTypeName(property.Type),
                    BsonFieldName = property.Name,
                    IsNullable = IsNullableType(property.Type)
                };
                
                // Analyze property setter accessibility (for DDD support)
                var setMethod = property.SetMethod;
                if (setMethod != null)
                {
                    propInfo.HasPublicSetter = setMethod.DeclaredAccessibility == Accessibility.Public;
                    propInfo.HasInitOnlySetter = setMethod.IsInitOnly;
                    
                    // Generate backing field name for non-public setters
                    if (!propInfo.HasPublicSetter || propInfo.HasInitOnlySetter)
                    {
                        propInfo.BackingFieldName = $"<{property.Name}>k__BackingField";
                    }
                }
                else
                {
                    // No setter - readonly property
                    propInfo.HasPublicSetter = false;
                }
                
                // Detect collection types
                if (IsCollectionType(property.Type, out var itemType))
                {
                    propInfo.IsCollection = true;
                    propInfo.CollectionItemType = itemType?.Name;
                }
                // Detect nested objects within this nested type
                else if (IsNestedObjectType(property.Type) && currentDepth < maxDepth)
                {
                    propInfo.IsNestedObject = true;
                    propInfo.NestedTypeName = property.Type.Name;
                    propInfo.NestedTypeFullName = property.Type.ToDisplayString();
                }
                
                nestedTypeInfo.Properties.Add(propInfo);
            }
            
            // Add to target collection
            targetNestedTypes[nestedTypeName] = nestedTypeInfo;
            
            // Also add to root entity for global access
            if (!rootEntity.NestedTypes.ContainsKey(nestedTypeName))
            {
                rootEntity.NestedTypes[nestedTypeName] = nestedTypeInfo;
            }
            
            // Recursively analyze nested types within this nested type
            if (currentDepth < maxDepth)
            {
                AnalyzeNestedTypesRecursive(
                    rootEntity,
                    nestedTypeInfo.Properties,
                    nestedTypeInfo.NestedTypes,
                    semanticModel,
                    analyzedTypes,
                    currentDepth + 1,
                    maxDepth);
            }
        }
    }
    
    private static INamedTypeSymbol? FindNestedTypeSymbol(string typeName, SemanticModel semanticModel)
    {
        // Remove any namespace qualifiers and generic markers for search
        var simpleName = typeName.Split('.').Last().Split('<').First().Split('?').First();
        
        // Try to find by full name first
        var typeSymbol = semanticModel.Compilation.GetTypeByMetadataName(typeName);
        if (typeSymbol != null)
            return typeSymbol;
        
        // Fallback: search by simple name in current compilation
        var candidates = semanticModel.Compilation.GetSymbolsWithName(
            simpleName,
            SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .ToList();
        
        // Prefer non-generic types
        return candidates.FirstOrDefault(c => !c.IsGenericType) ?? candidates.FirstOrDefault();
    }
    
    private static void GenerateMappers(SourceProductionContext context, DbContextInfo dbContextInfo)
    {
        // Step 1: Collect all nested types from all entities into global collection
        CollectGlobalNestedTypes(dbContextInfo);
        
        // Step 2: Generate shared mappers for nested types (one per type, reusable)
        foreach (var kvp in dbContextInfo.GlobalNestedTypes)
        {
            var nestedTypeName = kvp.Key;
            var nestedTypeInfo = kvp.Value;
            
            var sharedMapperCode = CodeGenerator.GenerateSharedNestedMapper(nestedTypeInfo);
            var fileName = $"{nestedTypeName}Mapper.g.cs";
            context.AddSource(fileName, sharedMapperCode);
        }
        
        // Step 3: Generate mapper for each entity (referencing shared nested mappers)
        foreach (var entity in dbContextInfo.Entities)
        {
            var mapperCode = CodeGenerator.GenerateMapper(entity, useSharedMappers: true);
            var fileName = $"{entity.Name}Mapper.g.cs";
            context.AddSource(fileName, mapperCode);
        }
        
        // Step 4: Generate DbContext partial class
        if (dbContextInfo.Entities.Count > 0)
        {
            var contextCode = CodeGenerator.GenerateDbContextPartial(dbContextInfo);
            var fileName = $"{dbContextInfo.ClassName}.g.cs";
            context.AddSource(fileName, contextCode);
        }
    }
    
    /// <summary>
    /// Collects all nested types from all entities into the global collection
    /// </summary>
    private static void CollectGlobalNestedTypes(DbContextInfo dbContextInfo)
    {
        foreach (var entity in dbContextInfo.Entities)
        {
            CollectNestedTypesRecursively(entity.NestedTypes, dbContextInfo.GlobalNestedTypes);
        }
    }
    
    /// <summary>
    /// Recursively collects nested types into target dictionary
    /// </summary>
    private static void CollectNestedTypesRecursively(
        Dictionary<string, Models.NestedTypeInfo> source,
        Dictionary<string, Models.NestedTypeInfo> target)
    {
        foreach (var kvp in source)
        {
            var name = kvp.Key;
            var info = kvp.Value;
            
            // Add to global collection if not already present
            if (!target.ContainsKey(name))
            {
                target[name] = info;
            }
            
            // Recursively collect nested types within this type
            if (info.NestedTypes.Any())
            {
                CollectNestedTypesRecursively(info.NestedTypes, target);
            }
        }
    }
}
