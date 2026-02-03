using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Community.LiteDB.Aot.SourceGenerators.Helpers;

/// <summary>
/// Helper for parsing lambda expressions in source generators
/// </summary>
internal static class LambdaParser
{
    /// <summary>
    /// Extracts the body of a lambda expression and replaces the parameter name
    /// Example: "id => id.Value.ToString()" with parameter "entity.Id" ? "entity.Id.Value.ToString()"
    /// </summary>
    /// <param name="lambdaExpression">The lambda expression syntax node</param>
    /// <param name="newParameterExpression">The new expression to replace the parameter (e.g., "entity.Id")</param>
    /// <returns>The body with parameter replaced</returns>
    public static string? ExtractLambdaBody(ExpressionSyntax lambdaExpression, string newParameterExpression)
    {
        // Handle SimpleLambdaExpression: id => id.Value.ToString()
        if (lambdaExpression is SimpleLambdaExpressionSyntax simpleLambda)
        {
            var parameterName = simpleLambda.Parameter.Identifier.Text;
            var body = simpleLambda.Body.ToString();
            
            // Replace parameter name with new expression
            // Use word boundaries to avoid partial replacements
            var result = System.Text.RegularExpressions.Regex.Replace(
                body,
                $@"\b{System.Text.RegularExpressions.Regex.Escape(parameterName)}\b",
                newParameterExpression
            );
            
            return result;
        }
        
        // Handle ParenthesizedLambdaExpression: (id) => id.Value.ToString()
        if (lambdaExpression is ParenthesizedLambdaExpressionSyntax parenLambda)
        {
            if (parenLambda.ParameterList.Parameters.Count > 0)
            {
                var parameterName = parenLambda.ParameterList.Parameters[0].Identifier.Text;
                var body = parenLambda.Body.ToString();
                
                var result = System.Text.RegularExpressions.Regex.Replace(
                    body,
                    $@"\b{System.Text.RegularExpressions.Regex.Escape(parameterName)}\b",
                    newParameterExpression
                );
                
                return result;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets the parameter name from a lambda expression
    /// </summary>
    public static string? GetLambdaParameterName(ExpressionSyntax lambdaExpression)
    {
        if (lambdaExpression is SimpleLambdaExpressionSyntax simpleLambda)
        {
            return simpleLambda.Parameter.Identifier.Text;
        }
        
        if (lambdaExpression is ParenthesizedLambdaExpressionSyntax parenLambda)
        {
            if (parenLambda.ParameterList.Parameters.Count > 0)
            {
                return parenLambda.ParameterList.Parameters[0].Identifier.Text;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets the body of a lambda expression without parameter
    /// Example: "id => id.Value.ToString()" ? "id.Value.ToString()"
    /// </summary>
    public static string? GetLambdaBody(ExpressionSyntax lambdaExpression)
    {
        if (lambdaExpression is SimpleLambdaExpressionSyntax simpleLambda)
        {
            return simpleLambda.Body.ToString();
        }
        
        if (lambdaExpression is ParenthesizedLambdaExpressionSyntax parenLambda)
        {
            return parenLambda.Body.ToString();
        }
        
        return null;
    }
}
