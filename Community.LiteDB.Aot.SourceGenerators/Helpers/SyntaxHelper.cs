using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Community.LiteDB.Aot.SourceGenerators.Helpers;

internal static class SyntaxHelper
{
    /// <summary>
    /// Gets the full name including namespace
    /// </summary>
    public static string GetFullName(INamedTypeSymbol symbol)
    {
        if (symbol.ContainingNamespace.IsGlobalNamespace)
        {
            return symbol.Name;
        }
        
        return $"{symbol.ContainingNamespace.ToDisplayString()}.{symbol.Name}";
    }
    
    /// <summary>
    /// Checks if a type inherits from a specific base type
    /// </summary>
    public static bool InheritsFrom(INamedTypeSymbol typeSymbol, string baseTypeName)
    {
        var current = typeSymbol.BaseType;
        while (current != null)
        {
            if (current.Name == baseTypeName)
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }
    
    /// <summary>
    /// Gets property name from lambda expression like: x => x.PropertyName
    /// </summary>
    public static string? GetPropertyNameFromLambda(ExpressionSyntax expression)
    {
        // Handle: x => x.Property
        if (expression is LambdaExpressionSyntax lambda)
        {
            if (lambda.Body is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Name.Identifier.Text;
            }
        }
        
        // Handle direct member access: x.Property
        if (expression is MemberAccessExpressionSyntax directMember)
        {
            return directMember.Name.Identifier.Text;
        }
        
        return null;
    }
    
    /// <summary>
    /// Finds method invocation by name in a syntax tree
    /// </summary>
    public static IEnumerable<InvocationExpressionSyntax> FindMethodInvocations(
        SyntaxNode root, 
        string methodName)
    {
        return root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation =>
            {
                var expression = invocation.Expression;
                
                // Handle: builder.Entity<T>(...)
                if (expression is MemberAccessExpressionSyntax memberAccess)
                {
                    return memberAccess.Name.Identifier.Text == methodName;
                }
                
                // Handle: Entity<T>(...)
                if (expression is IdentifierNameSyntax identifier)
                {
                    return identifier.Identifier.Text == methodName;
                }
                
                return false;
            });
    }
    
    /// <summary>
    /// Gets the type argument from a generic method: Entity&lt;Customer&gt;
    /// </summary>
    public static string? GetGenericTypeArgument(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name is GenericNameSyntax genericName)
        {
            var typeArg = genericName.TypeArgumentList.Arguments.FirstOrDefault();
            return typeArg?.ToString();
        }
        
        return null;
    }
}
