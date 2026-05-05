// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace PreferAsyncAwait;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class PreferAsyncAwaitCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(PreferAsyncAwaitAnalyzer.DiagnosticId);

    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var diagnostic = context.Diagnostics[0];
        var token = root.FindToken(diagnostic.Location.SourceSpan.Start);

        // The diagnostic is reported on:
        //   - Identifier token for methods and local functions (parent is the declaration)
        //   - Arrow token for lambdas (parent is the lambda expression)
        SyntaxNode? targetNode = token.Parent;
        if (targetNode is not (MethodDeclarationSyntax or LocalFunctionStatementSyntax or LambdaExpressionSyntax))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Convert to async",
                createChangedDocument: ct => ConvertToAsync(context.Document, targetNode, ct),
                equivalenceKey: nameof(PreferAsyncAwaitCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> ConvertToAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return document;

        IMethodSymbol? methodSymbol = node switch
        {
            MethodDeclarationSyntax m => semanticModel.GetDeclaredSymbol(m, cancellationToken),
            LocalFunctionStatementSyntax l => semanticModel.GetDeclaredSymbol(l, cancellationToken) as IMethodSymbol,
            LambdaExpressionSyntax lambda => semanticModel.GetSymbolInfo(lambda, cancellationToken).Symbol as IMethodSymbol,
            _ => null
        };
        if (methodSymbol is null)
            return document;

        var taskOfTType = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTaskOfTType = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        bool isGenericReturn = PreferAsyncAwaitAnalyzer.IsGenericTaskType(methodSymbol.ReturnType, taskOfTType, valueTaskOfTType);

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        SyntaxNode newNode = node switch
        {
            MethodDeclarationSyntax m => ConvertMethod(m, isGenericReturn),
            LocalFunctionStatementSyntax l => ConvertLocalFunction(l, isGenericReturn),
            ParenthesizedLambdaExpressionSyntax p => ConvertParenthesizedLambda(p, isGenericReturn),
            SimpleLambdaExpressionSyntax s => ConvertSimpleLambda(s, isGenericReturn),
            _ => node
        };

        editor.ReplaceNode(node, newNode);

        return editor.GetChangedDocument();
    }

    private static MethodDeclarationSyntax ConvertMethod(MethodDeclarationSyntax method, bool isGenericReturn)
    {
        var asyncToken = SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space);

        MethodDeclarationSyntax newMethod;
        if (method.Modifiers.Count > 0)
        {
            var lastMod = method.Modifiers.Last();
            var updatedLastMod = lastMod.WithTrailingTrivia(SyntaxFactory.Space);
            newMethod = method.WithModifiers(method.Modifiers.Replace(lastMod, updatedLastMod).Add(asyncToken));
        }
        else
        {
            asyncToken = asyncToken.WithLeadingTrivia(method.ReturnType.GetLeadingTrivia());
            newMethod = method.WithModifiers(SyntaxFactory.TokenList(asyncToken))
                              .WithReturnType(method.ReturnType.WithoutLeadingTrivia());
        }

        if (method.ExpressionBody is { } expressionBody)
        {
            var newExpr = TransformExpression(expressionBody.Expression);
            return newMethod.WithExpressionBody(expressionBody.WithExpression(newExpr));
        }

        if (method.Body is { } body)
        {
            return newMethod.WithBody(TransformBlockReturns(body, isGenericReturn));
        }

        return newMethod;
    }

    private static LocalFunctionStatementSyntax ConvertLocalFunction(LocalFunctionStatementSyntax localFunc, bool isGenericReturn)
    {
        var asyncToken = SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space);
        var newFunc = localFunc.WithModifiers(localFunc.Modifiers.Add(asyncToken));

        if (localFunc.ExpressionBody is { } expressionBody)
        {
            var newExpr = TransformExpression(expressionBody.Expression);
            return newFunc.WithExpressionBody(expressionBody.WithExpression(newExpr));
        }

        if (localFunc.Body is { } body)
        {
            return newFunc.WithBody(TransformBlockReturns(body, isGenericReturn));
        }

        return newFunc;
    }

    private static ParenthesizedLambdaExpressionSyntax ConvertParenthesizedLambda(
        ParenthesizedLambdaExpressionSyntax lambda, bool isGenericReturn)
    {
        var asyncToken = SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space);
        var newLambda = lambda.WithModifiers(lambda.Modifiers.Add(asyncToken));

        if (lambda.Body is ExpressionSyntax expr)
        {
            var newExpr = TransformExpression(expr);
            return newLambda.WithBody(newExpr);
        }

        if (lambda.Body is BlockSyntax body)
        {
            return newLambda.WithBody(TransformBlockReturns(body, isGenericReturn));
        }

        return newLambda;
    }

    private static SimpleLambdaExpressionSyntax ConvertSimpleLambda(
        SimpleLambdaExpressionSyntax lambda, bool isGenericReturn)
    {
        var asyncToken = SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space);
        var newLambda = lambda.WithModifiers(lambda.Modifiers.Add(asyncToken));

        if (lambda.Body is ExpressionSyntax expr)
        {
            var newExpr = TransformExpression(expr);
            return newLambda.WithBody(newExpr);
        }

        if (lambda.Body is BlockSyntax body)
        {
            return newLambda.WithBody(TransformBlockReturns(body, isGenericReturn));
        }

        return newLambda;
    }

    private static BlockSyntax TransformBlockReturns(BlockSyntax body, bool isGenericReturn) =>
        body.ReplaceNodes(
            body.DescendantNodes(n => !IsNestedFunctionLike(n)).OfType<ReturnStatementSyntax>(),
            (originalReturn, _) => TransformReturn(originalReturn, isGenericReturn));

    private static ExpressionSyntax TransformExpression(ExpressionSyntax expression)
    {
        if (TryUnwrapFromResult(expression, out var unwrapped))
            return unwrapped!.WithTriviaFrom(expression);

        var awaitExpr = SyntaxFactory.AwaitExpression(AddConfigureAwaitFalse(expression))
            .WithTriviaFrom(expression);
        return awaitExpr;
    }

    private static SyntaxNode TransformReturn(ReturnStatementSyntax returnStatement, bool isGenericReturn)
    {
        if (returnStatement.Expression is null)
            return returnStatement;

        if (TryUnwrapFromResult(returnStatement.Expression, out var unwrapped))
        {
            if (isGenericReturn)
            {
                return returnStatement.WithExpression(unwrapped!.WithLeadingTrivia(returnStatement.Expression.GetLeadingTrivia()));
            }
            else
            {
                return SyntaxFactory.ExpressionStatement(unwrapped!)
                    .WithLeadingTrivia(returnStatement.GetLeadingTrivia())
                    .WithTrailingTrivia(returnStatement.GetTrailingTrivia());
            }
        }

        var awaitExpression = SyntaxFactory.AwaitExpression(AddConfigureAwaitFalse(returnStatement.Expression));

        if (isGenericReturn)
        {
            return returnStatement.WithExpression(awaitExpression);
        }
        else
        {
            return SyntaxFactory.ExpressionStatement(awaitExpression)
                .WithLeadingTrivia(returnStatement.GetLeadingTrivia())
                .WithTrailingTrivia(returnStatement.GetTrailingTrivia());
        }
    }

    private static bool TryUnwrapFromResult(
        ExpressionSyntax expr,
        out ExpressionSyntax? inner)
    {
        inner = null;
        if (expr is not InvocationExpressionSyntax invocation)
            return false;
        if (invocation.ArgumentList.Arguments.Count != 1)
            return false;

        var memberName = invocation.Expression switch
        {
            MemberAccessExpressionSyntax { Name: var name } => name,
            _ => null
        };

        if (memberName is null)
            return false;

        var identText = memberName switch
        {
            GenericNameSyntax gns => gns.Identifier.Text,
            IdentifierNameSyntax ins => ins.Identifier.Text,
            _ => null
        };

        if (identText != "FromResult")
            return false;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var receiverText = memberAccess.Expression switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                GenericNameSyntax gn => gn.Identifier.Text,
                MemberAccessExpressionSyntax nested => nested.Name switch
                {
                    IdentifierNameSyntax nid => nid.Identifier.Text,
                    GenericNameSyntax ngn => ngn.Identifier.Text,
                    _ => null
                },
                _ => null
            };

            if (receiverText is not ("Task" or "ValueTask"))
                return false;
        }

        inner = invocation.ArgumentList.Arguments[0].Expression;
        return true;
    }

    private static InvocationExpressionSyntax AddConfigureAwaitFalse(ExpressionSyntax expression) =>
        SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                StripAsTask(expression),
                SyntaxFactory.IdentifierName("ConfigureAwait")),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))));

    /// <summary>
    /// Strips a trailing <c>.AsTask()</c> call since it's unnecessary in async methods.
    /// </summary>
    private static ExpressionSyntax StripAsTask(ExpressionSyntax expression)
    {
        if (expression is InvocationExpressionSyntax
            {
                ArgumentList.Arguments.Count: 0,
                Expression: MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier.Text: "AsTask" } } memberAccess
            })
        {
            return memberAccess.Expression.WithTriviaFrom(expression);
        }

        return expression;
    }

    private static bool IsNestedFunctionLike(SyntaxNode node) =>
        node is LocalFunctionStatementSyntax
            or ParenthesizedLambdaExpressionSyntax
            or SimpleLambdaExpressionSyntax
            or AnonymousMethodExpressionSyntax;
}
