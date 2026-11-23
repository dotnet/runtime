// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Private.DataContractSerialization.Generators;

[Generator]
public sealed class SyncContractGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<(string Name, string? Source, string? Reader)> sources = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ClassDeclarationSyntax declaration && SyncContractLowering.IsContractName(declaration.Identifier.ValueText) ||
                node is DelegateDeclarationSyntax callback && SyncContractLowering.IsDelegateName(callback.Identifier.ValueText) ||
                node is StructDeclarationSyntax { Identifier.ValueText: "SyncReflectionSerialization" },
            static (context, cancellationToken) =>
            {
                MemberDeclarationSyntax declaration = (MemberDeclarationSyntax)context.Node;
                string? source = SyncContractLowering.Generate(declaration, context.SemanticModel, cancellationToken, out _);
                string? reader = source is not null && declaration is ClassDeclarationSyntax { Identifier.ValueText: "ReflectionXmlReader" } xmlReader
                    ? SyncReaderLowering.Generate(xmlReader, context.SemanticModel, cancellationToken, out _, "SyncReflectionXmlReader", plainContract: true)
                    : null;
                return (declaration is DelegateDeclarationSyntax method ? method.Identifier.ValueText : ((TypeDeclarationSyntax)declaration).Identifier.ValueText, source, reader);
            }).WithTrackingName("SyncContractSources");
        context.RegisterSourceOutput(sources, static (context, source) =>
        {
            if (source.Source is not null)
            {
                context.AddSource(source.Name + ".SyncContract.g.cs", source.Source);
            }
            if (source.Reader is not null)
            {
                context.AddSource(source.Name + ".SyncContractReader.g.cs", source.Reader);
            }
        });
    }
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SyncContractLoweringAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor UnsupportedShape = new DiagnosticDescriptor(
        "DCSG002", "Unsupported synchronous contract lowering",
        "Cannot lower serializer contract '{0}': {1}", "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(UnsupportedShape);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(static context =>
        {
            MemberDeclarationSyntax declaration = (MemberDeclarationSyntax)context.Node;
            string name = declaration is DelegateDeclarationSyntax method ? method.Identifier.ValueText : ((TypeDeclarationSyntax)declaration).Identifier.ValueText;
            if (SyncContractLowering.IsContractName(name) || SyncContractLowering.IsDelegateName(name) || name == "SyncReflectionSerialization")
            {
                SyncContractLowering.Generate(declaration, context.SemanticModel, context.CancellationToken, out ImmutableArray<Diagnostic> diagnostics);
                foreach (Diagnostic diagnostic in diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.DelegateDeclaration);
    }
}

internal static class SyncContractLowering
{
    internal static bool IsContractName(string name) => name is
        "ReflectionReader" or "ReflectionClassWriter" or "ReflectionXmlReader" or "ReflectionXmlClassWriter" or
        "ReflectionJsonReader" or "ReflectionJsonClassWriter";

    internal static bool IsDelegateName(string name) => name is
        "ReflectionWriter" or "ReflectionValueWriter" or "ReflectionMemberValueWriter" or
        "ReflectionClassReader" or "ReflectionObjectReader" or "ReflectionValueReader" or "ReflectionCollectionBodyReader";

    private static bool IsContract(INamedTypeSymbol? type) => type is not null &&
        (IsContractName(type.Name) || IsDelegateName(type.Name) || type.Name == "SyncReflectionSerialization") &&
        type.ContainingNamespace.ToDisplayString() is "System.Runtime.Serialization" or "System.Runtime.Serialization.Json";

    internal static bool IsAsyncOperation(ITypeSymbol? type) => type is INamedTypeSymbol named &&
        named.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks" &&
        (named.Name == "ValueTask" || named.Name == "Task" && named.Arity == 0);

    internal static bool TryCompletionCondition(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken, out bool completed)
    {
        bool negated = expression is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression };
        ExpressionSyntax condition = negated ? ((PrefixUnaryExpressionSyntax)expression).Operand : expression;
        completed = !negated;
        return condition is MemberAccessExpressionSyntax completion &&
            model.GetSymbolInfo(completion, cancellationToken).Symbol is IPropertySymbol { Name: "IsCompletedSuccessfully" } property &&
            IsAsyncOperation(property.ContainingType);
    }

    internal static TypeSyntax PlainType(TypeSyntax syntax, SemanticModel model, CancellationToken cancellationToken) =>
        (TypeSyntax)new Rewriter(model, cancellationToken, new List<Diagnostic>()).Visit(syntax)!;

    internal static string? Generate(MemberDeclarationSyntax declaration, SemanticModel model, CancellationToken cancellationToken,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        List<Diagnostic> errors = new List<Diagnostic>();
        ISymbol? symbol = declaration is DelegateDeclarationSyntax callback
            ? model.GetDeclaredSymbol(callback, cancellationToken)
            : model.GetDeclaredSymbol((TypeDeclarationSyntax)declaration, cancellationToken);
        if (symbol is not INamedTypeSymbol type || !IsContract(type))
        {
            diagnostics = ImmutableArray<Diagnostic>.Empty;
            return null;
        }
        Rewriter rewriter = new Rewriter(model, cancellationToken, errors);
        MemberDeclarationSyntax generated = (MemberDeclarationSyntax)rewriter.Visit(declaration)!;
        diagnostics = errors.ToImmutableArray();
        if (errors.Count != 0)
        {
            return null;
        }
        CompilationUnitSyntax original = (CompilationUnitSyntax)declaration.SyntaxTree.GetRoot(cancellationToken);
        BaseNamespaceDeclarationSyntax originalNamespace = (BaseNamespaceDeclarationSyntax)declaration.Parent!;
        NamespaceDeclarationSyntax ns = SyntaxFactory.NamespaceDeclaration(originalNamespace.Name)
            .WithUsings(originalNamespace.Usings).WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(generated));
        return "// Licensed to the .NET Foundation under one or more agreements.\n" +
            "// The .NET Foundation licenses this file to you under the MIT license.\n" +
            "// <auto-generated/>\n#nullable enable\n" +
            SyntaxFactory.CompilationUnit().WithUsings(original.Usings)
                .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(ns)).NormalizeWhitespace().ToFullString() + "\n";
    }

    private sealed class Rewriter(SemanticModel model, CancellationToken cancellationToken, List<Diagnostic> errors) : CSharpSyntaxRewriter
    {
        private bool _voidReturn;

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            ClassDeclarationSyntax result = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;
            if (node.Identifier.ValueText == "ReflectionXmlReader")
            {
                result = result.WithMembers(SyntaxFactory.List(result.Members.Where(
                    static member => member is not MethodDeclarationSyntax { Identifier.ValueText: "ReadMembers" })));
            }
            return IsContractName(node.Identifier.ValueText)
                ? result.WithIdentifier(SyntaxFactory.Identifier("Sync" + node.Identifier.ValueText)).WithoutLeadingTrivia()
                : result;
        }

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
        {
            StructDeclarationSyntax result = (StructDeclarationSyntax)base.VisitStructDeclaration(node)!;
            return node.Identifier.ValueText == "SyncReflectionSerialization"
                ? result.WithIdentifier(SyntaxFactory.Identifier("SyncReflectionPolicy")).WithBaseList(null).WithoutLeadingTrivia()
                : result;
        }

        public override SyntaxNode? VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            DelegateDeclarationSyntax result = (DelegateDeclarationSyntax)base.VisitDelegateDeclaration(node)!;
            return IsDelegateName(node.Identifier.ValueText)
                ? result.WithIdentifier(SyntaxFactory.Identifier("Sync" + node.Identifier.ValueText)).WithoutLeadingTrivia()
                : result;
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (model.GetSymbolInfo(node, cancellationToken).Symbol is INamedTypeSymbol type && IsContract(type))
            {
                if (type.Name != "SyncReflectionSerialization")
                {
                    return SyntaxFactory.IdentifierName("Sync" + type.Name).WithTriviaFrom(node);
                }
                if (node.Parent is MemberAccessExpressionSyntax access && access.Expression == node)
                {
                    return SyntaxFactory.IdentifierName("SyncReflectionPolicy").WithTriviaFrom(node);
                }
                return node;
            }
            if (model.GetSymbolInfo(node, cancellationToken).Symbol is ITypeParameterSymbol { Name: "TMode" } &&
                node.Parent is MemberAccessExpressionSyntax member && member.Expression == node)
            {
                return SyntaxFactory.ParseName("global::System.Runtime.Serialization.SyncReflectionPolicy").WithTriviaFrom(node);
            }
            if (model.GetSymbolInfo(node, cancellationToken).Symbol is INamedTypeSymbol task && IsAsyncOperation(task))
            {
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)).WithTriviaFrom(node);
            }
            return base.VisitIdentifierName(node);
        }

        public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
        {
            if (model.GetSymbolInfo(node, cancellationToken).Symbol is INamedTypeSymbol type)
            {
                if (IsAsyncOperation(type))
                {
                    return Visit(node.TypeArgumentList.Arguments[0])!.WithTriviaFrom(node);
                }
                if (type.Name == "Func" && type.ContainingNamespace.ToDisplayString() == "System" &&
                    IsAsyncOperation(type.TypeArguments.Last()) && ((INamedTypeSymbol)type.TypeArguments.Last()).Arity == 0)
                {
                    TypeSyntax[] arguments = node.TypeArgumentList.Arguments.Take(node.TypeArgumentList.Arguments.Count - 1)
                        .Select(argument => (TypeSyntax)Visit(argument)!).ToArray();
                    return arguments.Length == 0 ? SyntaxFactory.IdentifierName("Action") :
                        SyntaxFactory.GenericName("Action").WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(arguments)));
                }
            }
            return base.VisitGenericName(node);
        }

        public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (model.GetSymbolInfo(node, cancellationToken).Symbol is INamedTypeSymbol type && IsAsyncOperation(type))
            {
                return type.Arity == 0 ? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)) :
                    SyntaxFactory.ParseTypeName(type.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                            SymbolDisplayMiscellaneousOptions.UseSpecialTypes)));
            }
            return base.VisitQualifiedName(node);
        }

        public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            ConstructorDeclarationSyntax result = (ConstructorDeclarationSyntax)base.VisitConstructorDeclaration(node)!;
            string name = node.Identifier.ValueText;
            return name == "SyncReflectionSerialization"
                ? result.WithIdentifier(SyntaxFactory.Identifier("SyncReflectionPolicy"))
                : IsContractName(name) ? result.WithIdentifier(SyntaxFactory.Identifier("Sync" + name)) : result;
        }

        public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            if (node.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
            {
                errors.Add(Diagnostic.Create(SyncContractLoweringAnalyzer.UnsupportedShape, node.GetLocation(), node.ToString(),
                    "async delegates must use parenthesized parameters"));
                return node;
            }
            bool previous = _voidReturn;
            _voidReturn = ReturnsVoid(node);
            SimpleLambdaExpressionSyntax result = (SimpleLambdaExpressionSyntax)base.VisitSimpleLambdaExpression(node)!;
            if (_voidReturn && node.Body is ExpressionSyntax expression)
            {
                result = result.WithBody(SyntaxFactory.Block(VoidStatement(expression)));
            }
            _voidReturn = previous;
            return result;
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.Name.Identifier.ValueText == "Result" &&
                model.GetSymbolInfo(node, cancellationToken).Symbol is IPropertySymbol property && IsAsyncOperation(property.ContainingType))
            {
                return Visit(node.Expression);
            }
            if (node.Name.Identifier.ValueText == "CompletedTask" && IsAsyncOperation(model.GetTypeInfo(node.Expression, cancellationToken).Type))
            {
                return node;
            }
            MemberAccessExpressionSyntax result = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(node)!;
            if (node.Name.Identifier.ValueText is "Then" or "CreateWriterSequence" or "CreateMemberWriter" or "CreateElementWriter" or
                "WriteElement" or "WriteElementWithNamespace" or "WriteCollection" or
                "CreateTypedPrimitiveElementWriter" or "CreateNullableTypedPrimitiveElementWriter" &&
                model.GetSymbolInfo(node.Expression, cancellationToken).Symbol is INamedTypeSymbol type &&
                type.ToDisplayString() == "System.Runtime.Serialization.ReflectionSerialization")
            {
                return result.WithExpression(SyntaxFactory.ParseName("global::System.Runtime.Serialization.SyncReflectionPipeline"));
            }
            return result;
        }

        public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            if (IsAsyncOperation(model.GetTypeInfo(node, cancellationToken).Type))
            {
                if (!IsPlainOperation(node))
                {
                    Error(node, "ValueTask construction must wrap one synchronous result");
                    return node;
                }
                return Visit(node.ArgumentList.Arguments[0].Expression);
            }
            return base.VisitObjectCreationExpression(node);
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            bool previous = _voidReturn;
            IMethodSymbol? method = model.GetDeclaredSymbol(node, cancellationToken);
            _voidReturn = method?.ReturnsVoid == true || IsAsyncOperation(method?.ReturnType) && ((INamedTypeSymbol)method!.ReturnType).Arity == 0;
            MethodDeclarationSyntax result = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
            if (_voidReturn && node.ExpressionBody is not null)
            {
                result = result.WithBody(SyntaxFactory.Block(VoidStatement(node.ExpressionBody.Expression)))
                    .WithExpressionBody(null).WithSemicolonToken(default);
            }
            _voidReturn = previous;
            return result.WithModifiers(SyntaxFactory.TokenList(result.Modifiers.Where(static modifier => !modifier.IsKind(SyntaxKind.AsyncKeyword))));
        }

        public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            bool previous = _voidReturn;
            IMethodSymbol? method = model.GetDeclaredSymbol(node, cancellationToken);
            _voidReturn = method?.ReturnsVoid == true || IsAsyncOperation(method?.ReturnType) && ((INamedTypeSymbol)method!.ReturnType).Arity == 0;
            LocalFunctionStatementSyntax result = (LocalFunctionStatementSyntax)base.VisitLocalFunctionStatement(node)!;
            if (_voidReturn && node.ExpressionBody is not null)
            {
                result = result.WithBody(SyntaxFactory.Block(VoidStatement(node.ExpressionBody.Expression)))
                    .WithExpressionBody(null).WithSemicolonToken(default);
            }
            _voidReturn = previous;
            return result.WithModifiers(SyntaxFactory.TokenList(result.Modifiers.Where(static modifier => !modifier.IsKind(SyntaxKind.AsyncKeyword))));
        }

        public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
        {
            if (TryCompletionCondition(node.Condition, model, cancellationToken, out bool completed))
            {
                return completed ? Visit(node.Statement) :
                    node.Else is null ? SyntaxFactory.EmptyStatement() : Visit(node.Else.Statement);
            }
            if (node.Condition is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.EqualsExpression) &&
                binary.Left is TypeOfExpressionSyntax left && binary.Right is TypeOfExpressionSyntax right &&
                model.GetTypeInfo(left.Type, cancellationToken).Type is ITypeParameterSymbol { Name: "TMode" } &&
                model.GetTypeInfo(right.Type, cancellationToken).Type?.ToDisplayString() == "System.Runtime.Serialization.SyncReflectionSerialization")
            {
                return Visit(node.Statement);
            }
            return base.VisitIfStatement(node);
        }

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            List<StatementSyntax> statements = new List<StatementSyntax>();
            bool ended = false;
            foreach (StatementSyntax statement in node.Statements)
            {
                if (ended && statement is not LocalFunctionStatementSyntax)
                {
                    continue;
                }
                StatementSyntax rewritten = (StatementSyntax)Visit(statement)!;
                statements.Add(rewritten);
                ended |= EndsControlFlow(rewritten);
            }
            return node.WithStatements(SyntaxFactory.List(statements));
        }

        private static bool EndsControlFlow(StatementSyntax statement) =>
            statement is ReturnStatementSyntax or ThrowStatementSyntax ||
            statement is BlockSyntax { Statements.Count: > 0 } block && EndsControlFlow(block.Statements.Last());

        public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            bool async = node.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
            if (async && (model.GetTypeInfo(node, cancellationToken).ConvertedType is not INamedTypeSymbol { DelegateInvokeMethod.ReturnType: INamedTypeSymbol result } ||
                !IsAsyncOperation(result)))
            {
                Error(node, "async delegates must return ValueTask");
                return node;
            }
            foreach (SyntaxNode child in async ? node.Body.DescendantNodesAndSelf() : Enumerable.Empty<SyntaxNode>())
            {
                if (child is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax ||
                    child is UsingStatementSyntax use && !use.AwaitKeyword.IsKind(SyntaxKind.None) ||
                    child is LocalDeclarationStatementSyntax local && !local.AwaitKeyword.IsKind(SyntaxKind.None) ||
                    child is CommonForEachStatementSyntax each && !each.AwaitKeyword.IsKind(SyntaxKind.None))
                {
                    Error(child, "nested functions, await using, and await foreach are not supported");
                }
            }
            bool previous = _voidReturn;
            _voidReturn = ReturnsVoid(node);
            ParenthesizedLambdaExpressionSyntax lowered = (ParenthesizedLambdaExpressionSyntax)base.VisitParenthesizedLambdaExpression(node)!;
            if (_voidReturn && node.Body is ExpressionSyntax expression)
            {
                lowered = lowered.WithBody(SyntaxFactory.Block(VoidStatement(expression)));
            }
            _voidReturn = previous;
            return lowered.WithAsyncKeyword(default);
        }

        public override SyntaxNode? VisitAwaitExpression(AwaitExpressionSyntax node)
        {
            ExpressionSyntax expression = node.Expression;
            if (expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } configured &&
                model.GetSymbolInfo(configured, cancellationToken).Symbol is IMethodSymbol { Name: "ConfigureAwait" } configure &&
                configure.ContainingType.Name is "ValueTask" or "Task")
            {
                if (configured.ArgumentList.Arguments.Count != 1 ||
                    model.GetConstantValue(configured.ArgumentList.Arguments[0].Expression, cancellationToken).Value is not false)
                {
                    Error(node, "only ConfigureAwait(false) is supported");
                }
                expression = member.Expression;
            }
            if (!IsAsyncOperation(model.GetTypeInfo(expression, cancellationToken).Type))
            {
                Error(node, "await must consume a ValueTask or void Task");
            }
            else if (!IsPlainOperation(expression))
            {
                Error(node, "await must consume a lowered synchronous policy, delegate, or result");
            }
            return Visit(expression);
        }

        public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            if (node.Expression is AwaitExpressionSyntax awaited)
            {
                ExpressionSyntax expression = awaited.Expression is InvocationExpressionSyntax
                    { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ConfigureAwait" } configure }
                    ? configure.Expression : awaited.Expression;
                if (IsCompletedTask(expression))
                {
                    Visit(awaited);
                    return SyntaxFactory.EmptyStatement();
                }
            }
            return base.VisitExpressionStatement(node);
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetResult", Expression:
                InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetAwaiter" } awaiter } } &&
                IsAsyncOperation(model.GetTypeInfo(awaiter.Expression, cancellationToken).Type))
            {
                return Visit(awaiter.Expression);
            }
            return base.VisitInvocationExpression(node);
        }

        public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
        {
            if (_voidReturn && node.Expression is not null)
            {
                return SyntaxFactory.Block(VoidStatement(node.Expression), SyntaxFactory.ReturnStatement());
            }
            return base.VisitReturnStatement(node);
        }

        private bool ReturnsVoid(AnonymousFunctionExpressionSyntax node)
        {
            ITypeSymbol? type = (model.GetTypeInfo(node, cancellationToken).ConvertedType as INamedTypeSymbol)?.DelegateInvokeMethod?.ReturnType;
            return type?.SpecialType == SpecialType.System_Void || IsAsyncOperation(type) && ((INamedTypeSymbol)type!).Arity == 0;
        }

        private StatementSyntax VoidStatement(ExpressionSyntax expression)
        {
            if (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                return VoidStatement(parenthesized.Expression);
            }
            if (expression is ConditionalExpressionSyntax conditional)
            {
                return SyntaxFactory.IfStatement((ExpressionSyntax)Visit(conditional.Condition)!,
                    SyntaxFactory.Block(VoidStatement(conditional.WhenTrue)),
                    SyntaxFactory.ElseClause(SyntaxFactory.Block(VoidStatement(conditional.WhenFalse))));
            }
            if (IsCompletedTask(expression))
            {
                return SyntaxFactory.EmptyStatement();
            }
            if (expression is ThrowExpressionSyntax thrown)
            {
                return SyntaxFactory.ThrowStatement((ExpressionSyntax)Visit(thrown.Expression)!);
            }
            if (IsAsyncOperation(model.GetTypeInfo(expression, cancellationToken).Type) && !IsPlainOperation(expression))
            {
                Error(expression, "void operations must call a lowered synchronous policy or delegate");
            }
            return SyntaxFactory.ExpressionStatement((ExpressionSyntax)Visit(expression)!);
        }

        private bool IsCompletedTask(ExpressionSyntax expression) =>
            model.GetSymbolInfo(expression, cancellationToken).Symbol is IPropertySymbol { Name: "CompletedTask" } property &&
            IsAsyncOperation(property.ContainingType);

        private bool IsPlainOperation(ExpressionSyntax expression)
        {
            if (expression is IdentifierNameSyntax &&
                model.GetSymbolInfo(expression, cancellationToken).Symbol is ILocalSymbol or IParameterSymbol &&
                IsAsyncOperation(model.GetTypeInfo(expression, cancellationToken).Type))
            {
                return true;
            }
            if (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                return IsPlainOperation(parenthesized.Expression);
            }
            if (expression is ConditionalExpressionSyntax conditional)
            {
                return IsPlainOperation(conditional.WhenTrue) && IsPlainOperation(conditional.WhenFalse);
            }
            if (IsCompletedTask(expression))
            {
                return true;
            }
            if (expression is ObjectCreationExpressionSyntax creation &&
                model.GetSymbolInfo(creation, cancellationToken).Symbol is IMethodSymbol constructor &&
                constructor.ContainingType.Arity == 1 && creation.ArgumentList?.Arguments.Count == 1)
            {
                return SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, constructor.ContainingType.TypeArguments[0]);
            }
            // The member-loop generator's output is not available in this semantic model.
            if (expression is ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 1 } generated &&
                generated.ArgumentList.Arguments[0].Expression is InvocationExpressionSyntax
                    { Expression: SimpleNameSyntax { Identifier.ValueText: "ReadMembersSync" } } &&
                generated.FirstAncestorOrSelf<ClassDeclarationSyntax>()?.Identifier.ValueText == "ReflectionXmlReader")
            {
                return true;
            }
            if (expression is InvocationExpressionSyntax invocation &&
                model.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method)
            {
                return method.MethodKind == MethodKind.DelegateInvoke &&
                    (IsContract(method.ContainingType) || method.ContainingType.Name == "Func" &&
                        method.ContainingType.ContainingNamespace.ToDisplayString() == "System") ||
                    IsContract(method.ContainingType) ||
                    method.ContainingType.ToDisplayString() == "System.Runtime.Serialization.ReflectionSerialization" &&
                    method.Name is "WriteElement" or "WriteElementWithNamespace" or "WriteCollection" ||
                    invocation.Expression is MemberAccessExpressionSyntax member &&
                    model.GetSymbolInfo(member.Expression, cancellationToken).Symbol is ITypeParameterSymbol { Name: "TMode" } parameter &&
                    parameter.ConstraintTypes.Any(static constraint => constraint.ToDisplayString() == "System.Runtime.Serialization.IReflectionSerialization");
            }
            return false;
        }

        private void Error(SyntaxNode node, string message) =>
            errors.Add(Diagnostic.Create(SyncContractLoweringAnalyzer.UnsupportedShape, node.GetLocation(), node.ToString(), message));
    }
}
