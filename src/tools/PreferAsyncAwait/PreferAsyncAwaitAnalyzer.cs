// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable RS2008 // Not a shipping analyzer, so we don't need release tracking

namespace PreferAsyncAwait;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferAsyncAwaitAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ASYNC0001";

    private static readonly DiagnosticDescriptor s_rule = new(
        id: DiagnosticId,
        title: "Method can be converted to async",
        messageFormat: "'{0}' returns a task from a method call and can be converted to async",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Non-async methods, local functions, and lambdas that return a Task/ValueTask from a single method invocation can be converted to use async/await for improved stack traces and exception handling.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var taskType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var taskOfTType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            var valueTaskType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
            var valueTaskOfTType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

            if (taskType is null && taskOfTType is null && valueTaskType is null && valueTaskOfTType is null)
                return;

            compilationContext.RegisterSyntaxNodeAction(
                ctx => AnalyzeNode(ctx, taskType, taskOfTType, valueTaskType, valueTaskOfTType),
                SyntaxKind.MethodDeclaration,
                SyntaxKind.LocalFunctionStatement,
                SyntaxKind.ParenthesizedLambdaExpression,
                SyntaxKind.SimpleLambdaExpression);
        });
    }

    private static void AnalyzeNode(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskOfTType,
        INamedTypeSymbol? valueTaskType,
        INamedTypeSymbol? valueTaskOfTType)
    {
        IMethodSymbol? methodSymbol;
        SyntaxTokenList modifiers;
        ExpressionSyntax? expressionBody;
        BlockSyntax? blockBody;
        Location reportLocation;
        string displayName;

        switch (context.Node)
        {
            case MethodDeclarationSyntax method:
                if (method.Modifiers.Any(SyntaxKind.AsyncKeyword))
                    return;
                if (method.ExpressionBody is null && method.Body is null)
                    return;
                methodSymbol = context.SemanticModel.GetDeclaredSymbol(method, context.CancellationToken);
                modifiers = method.Modifiers;
                expressionBody = method.ExpressionBody?.Expression;
                blockBody = method.Body;
                reportLocation = method.Identifier.GetLocation();
                displayName = methodSymbol?.Name ?? "method";
                break;

            case LocalFunctionStatementSyntax localFunc:
                if (localFunc.Modifiers.Any(SyntaxKind.AsyncKeyword))
                    return;
                if (localFunc.ExpressionBody is null && localFunc.Body is null)
                    return;
                methodSymbol = context.SemanticModel.GetDeclaredSymbol(localFunc, context.CancellationToken) as IMethodSymbol;
                modifiers = localFunc.Modifiers;
                expressionBody = localFunc.ExpressionBody?.Expression;
                blockBody = localFunc.Body;
                reportLocation = localFunc.Identifier.GetLocation();
                displayName = methodSymbol?.Name ?? "local function";
                break;

            case ParenthesizedLambdaExpressionSyntax lambda:
                if (lambda.Modifiers.Any(SyntaxKind.AsyncKeyword))
                    return;
                methodSymbol = context.SemanticModel.GetSymbolInfo(lambda, context.CancellationToken).Symbol as IMethodSymbol;
                modifiers = lambda.Modifiers;
                expressionBody = lambda.Body as ExpressionSyntax;
                blockBody = lambda.Body as BlockSyntax;
                if (expressionBody is null && blockBody is null)
                    return;
                reportLocation = lambda.ArrowToken.GetLocation();
                displayName = "lambda";
                break;

            case SimpleLambdaExpressionSyntax lambda:
                if (lambda.Modifiers.Any(SyntaxKind.AsyncKeyword))
                    return;
                methodSymbol = context.SemanticModel.GetSymbolInfo(lambda, context.CancellationToken).Symbol as IMethodSymbol;
                modifiers = lambda.Modifiers;
                expressionBody = lambda.Body as ExpressionSyntax;
                blockBody = lambda.Body as BlockSyntax;
                if (expressionBody is null && blockBody is null)
                    return;
                reportLocation = lambda.ArrowToken.GetLocation();
                displayName = "lambda";
                break;

            default:
                return;
        }

        if (methodSymbol is null)
            return;

        AnalyzeFunction(context, methodSymbol, modifiers, expressionBody, blockBody,
            context.Node, reportLocation, displayName, taskType, taskOfTType, valueTaskType, valueTaskOfTType);
    }

    private static void AnalyzeFunction(
        SyntaxNodeAnalysisContext context,
        IMethodSymbol methodSymbol,
        SyntaxTokenList modifiers,
        ExpressionSyntax? expressionBody,
        BlockSyntax? blockBody,
        SyntaxNode syntaxNode,
        Location reportLocation,
        string displayName,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskOfTType,
        INamedTypeSymbol? valueTaskType,
        INamedTypeSymbol? valueTaskOfTType)
    {
        if (!IsTaskLikeType(methodSymbol.ReturnType, taskType, taskOfTType, valueTaskType, valueTaskOfTType))
            return;

        // Skip functions with ref/in/out parameters (cannot be async)
        foreach (var param in methodSymbol.Parameters)
        {
            if (param.RefKind is RefKind.Ref or RefKind.Out or RefKind.In)
                return;
        }

        // Skip functions with Span/ReadOnlySpan parameters (ref structs cannot be in async methods)
        foreach (var param in methodSymbol.Parameters)
        {
            if (param.Type.IsRefLikeType)
                return;
        }

        // Skip functions with [MethodImpl(MethodImplOptions.Synchronized)]
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "MethodImplAttribute" &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is int flags &&
                (flags & 0x0020) != 0) // MethodImplOptions.Synchronized
                return;
        }

        // Skip functions with unsafe modifier or inside unsafe type
        if (modifiers.Any(SyntaxKind.UnsafeKeyword))
            return;
        foreach (var ancestor in syntaxNode.Ancestors())
        {
            if (ancestor is TypeDeclarationSyntax typeDecl && typeDecl.Modifiers.Any(SyntaxKind.UnsafeKeyword))
                return;
        }

        // Skip functions containing lock statements (cannot await in lock body)
        if (blockBody is not null)
        {
            foreach (var node in blockBody.DescendantNodes(n => !IsNestedFunctionLike(n)))
            {
                if (node is LockStatementSyntax)
                    return;
            }
        }

        // Check expression body
        if (expressionBody is not null)
        {
            if (IsTaskReturningInvocation(expressionBody, context.SemanticModel, taskType, taskOfTType, valueTaskType, valueTaskOfTType))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, reportLocation, displayName));
            }

            return;
        }

        // Check block body
        if (blockBody is not null)
        {
            // Skip functions that contain fire-and-forget task-returning calls (bare expression
            // statements or assignments to variables). Making the function async would trigger
            // CS4014 for those calls. Explicit discards (`_ = SomeAsync();`) are fine — they
            // suppress CS4014.
            if (HasUnawaitedTaskCalls(blockBody, context.SemanticModel, taskType, taskOfTType, valueTaskType, valueTaskOfTType))
                return;

            // Collect all return statements that belong to this function (not nested lambdas/local functions)
            var returnStatements = blockBody.DescendantNodes(n => !IsNestedFunctionLike(n))
                .OfType<ReturnStatementSyntax>()
                .ToList();

            // Must have at least one return statement, and all must return invocations
            if (returnStatements.Count == 0)
                return;

            foreach (var returnStatement in returnStatements)
            {
                if (returnStatement.Expression is null)
                    return;

                if (!IsTaskReturningInvocation(returnStatement.Expression, context.SemanticModel, taskType, taskOfTType, valueTaskType, valueTaskOfTType))
                    return;
            }

            context.ReportDiagnostic(Diagnostic.Create(s_rule, reportLocation, displayName));
        }
    }

    private static bool HasUnawaitedTaskCalls(
        BlockSyntax body,
        SemanticModel semanticModel,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskOfTType,
        INamedTypeSymbol? valueTaskType,
        INamedTypeSymbol? valueTaskOfTType)
    {
        foreach (var node in body.DescendantNodes(n => !IsNestedFunctionLike(n)))
        {
            if (node is not InvocationExpressionSyntax invocation)
                continue;

            var typeInfo = semanticModel.GetTypeInfo(invocation);
            if (typeInfo.Type is null || !IsTaskLikeType(typeInfo.Type, taskType, taskOfTType, valueTaskType, valueTaskOfTType))
                continue;

            var parent = invocation.Parent;

            // Bare expression statement: `SomeAsync();` — would trigger CS4014
            if (parent is ExpressionStatementSyntax)
                return true;

            // Assigned to a variable: `var t = SomeAsync();` or `Task t = SomeAsync();`
            if (parent is EqualsValueClauseSyntax)
                return true;

            // Simple assignment: `t = SomeAsync();` (but NOT `_ = SomeAsync();`)
            if (parent is AssignmentExpressionSyntax assignment &&
                assignment.Left is not IdentifierNameSyntax { Identifier.Text: "_" })
                return true;
        }

        return false;
    }

    internal static bool IsNestedFunctionLike(SyntaxNode node) =>
        node is LocalFunctionStatementSyntax
            or ParenthesizedLambdaExpressionSyntax
            or SimpleLambdaExpressionSyntax
            or AnonymousMethodExpressionSyntax;

    private static bool IsTaskReturningInvocation(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskOfTType,
        INamedTypeSymbol? valueTaskType,
        INamedTypeSymbol? valueTaskOfTType)
    {
        if (expression is not InvocationExpressionSyntax)
            return false;

        var typeInfo = semanticModel.GetTypeInfo(expression);
        if (typeInfo.Type is null)
            return false;

        return IsTaskLikeType(typeInfo.Type, taskType, taskOfTType, valueTaskType, valueTaskOfTType);
    }

    internal static bool IsTaskLikeType(
        ITypeSymbol type,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskOfTType,
        INamedTypeSymbol? valueTaskType,
        INamedTypeSymbol? valueTaskOfTType)
    {
        if (taskType is not null && SymbolEqualityComparer.Default.Equals(type, taskType))
            return true;

        if (valueTaskType is not null && SymbolEqualityComparer.Default.Equals(type, valueTaskType))
            return true;

        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var originalDef = namedType.OriginalDefinition;

            if (taskOfTType is not null && SymbolEqualityComparer.Default.Equals(originalDef, taskOfTType))
                return true;

            if (valueTaskOfTType is not null && SymbolEqualityComparer.Default.Equals(originalDef, valueTaskOfTType))
                return true;
        }

        return false;
    }

    internal static bool IsGenericTaskType(
        ITypeSymbol type,
        INamedTypeSymbol? taskOfTType,
        INamedTypeSymbol? valueTaskOfTType)
    {
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var originalDef = namedType.OriginalDefinition;

            if (taskOfTType is not null && SymbolEqualityComparer.Default.Equals(originalDef, taskOfTType))
                return true;

            if (valueTaskOfTType is not null && SymbolEqualityComparer.Default.Equals(originalDef, valueTaskOfTType))
                return true;
        }

        return false;
    }
}
