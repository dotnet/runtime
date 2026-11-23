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
public sealed class SyncReaderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<(string Name, string? Source)> sources = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ClassDeclarationSyntax declaration && SyncReaderLowering.IsReaderName(declaration.Identifier.ValueText),
            static (context, cancellationToken) =>
            {
                ClassDeclarationSyntax declaration = (ClassDeclarationSyntax)context.Node;
                string? source = SyncReaderLowering.Generate(declaration, context.SemanticModel, cancellationToken, out _);
                return (declaration.Identifier.ValueText, source);
            }).WithTrackingName("SyncReaderSources");

        context.RegisterSourceOutput(sources, static (context, source) =>
        {
            if (source.Source is not null)
            {
                context.AddSource(source.Name + ".Sync.g.cs", source.Source);
            }
        });
    }
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SyncReaderLoweringAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor UnsupportedShape = new DiagnosticDescriptor(
        "DCSG001",
        "Unsupported synchronous reader lowering",
        "Cannot lower dictionary reader method '{0}': {1}",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(UnsupportedShape);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(static context =>
        {
            ClassDeclarationSyntax declaration = (ClassDeclarationSyntax)context.Node;
            if (SyncReaderLowering.IsReaderName(declaration.Identifier.ValueText))
            {
                SyncReaderLowering.Generate(declaration, context.SemanticModel, context.CancellationToken, out ImmutableArray<Diagnostic> diagnostics);
                foreach (Diagnostic diagnostic in diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }, SyntaxKind.ClassDeclaration);
    }
}

internal static class SyncReaderLowering
{
    internal static bool IsReaderName(string name) =>
        name is "XmlDictionaryReader" or "XmlBaseReader" or "XmlBinaryReader" or "XmlBufferReader" or "XmlUTF8TextReader" or "XmlJsonReader" or "ValueHandle" or "ReflectionXmlReader" or "EncodingStreamWrapper" or "JsonEncodingStreamWrapper";

    private static bool IsReader(INamedTypeSymbol? type) => type?.ToDisplayString() is
        "System.Xml.XmlDictionaryReader" or "System.Xml.XmlBaseReader" or "System.Xml.XmlBinaryReader" or
        "System.Xml.XmlBufferReader" or "System.Xml.XmlUTF8TextReader" or "System.Runtime.Serialization.Json.XmlJsonReader" or "System.Xml.ValueHandle" or
        "System.Runtime.Serialization.ReflectionXmlReader" or "System.Xml.EncodingStreamWrapper" or "System.Runtime.Serialization.Json.JsonEncodingStreamWrapper";

    private static bool IsValueTask(ITypeSymbol? type) =>
        type is INamedTypeSymbol { Name: "ValueTask", Arity: 0 or 1 } named &&
        named.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks";

    private static bool IsCancellationToken(ITypeSymbol type) => type.ToDisplayString() == "System.Threading.CancellationToken";

    private static ITypeParameterSymbol? GetMode(IMethodSymbol method) =>
        method.TypeParameters.FirstOrDefault(static parameter => parameter.Name == "TMode");

    private static bool IsLoweredMethod(IMethodSymbol method) =>
        IsReader(method.ContainingType) && (method.ContainingType.Name != "ValueHandle" || method.Name == "Sign") &&
        (IsValueTask(method.ReturnType) || method.ReturnsVoid) && GetMode(method) is not null;

    private static bool IsReadEntryPoint(IMethodSymbol method) => method.Name == "ReadCore" && method.ContainingType.ToDisplayString() is
        "System.Xml.XmlBinaryReader" or "System.Xml.XmlUTF8TextReader" or "System.Runtime.Serialization.Json.XmlJsonReader";

    internal static string? Generate(ClassDeclarationSyntax declaration, SemanticModel semanticModel, CancellationToken cancellationToken,
        out ImmutableArray<Diagnostic> diagnostics, string? generatedName = null, bool plainContract = false)
    {
        List<Diagnostic> errors = new List<Diagnostic>();
        List<MemberDeclarationSyntax> methods = new List<MemberDeclarationSyntax>();
        if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol type || !IsReader(type))
        {
            diagnostics = ImmutableArray<Diagnostic>.Empty;
            return null;
        }

        foreach (MethodDeclarationSyntax method in declaration.Members.OfType<MethodDeclarationSyntax>().OrderBy(static method => method.ToString(), StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (semanticModel.GetDeclaredSymbol(method, cancellationToken) is IMethodSymbol symbol && IsLoweredMethod(symbol))
            {
                if (!declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    errors.Add(Diagnostic.Create(SyncReaderLoweringAnalyzer.UnsupportedShape, declaration.Identifier.GetLocation(), symbol.Name, "the reader class must be partial"));
                    continue;
                }

                Rewriter rewriter = new Rewriter(semanticModel, symbol, cancellationToken, errors, plainContract);
                methods.Add(rewriter.Lower(method));
            }
        }

        diagnostics = errors.ToImmutableArray();
        if (errors.Count != 0 || methods.Count == 0)
        {
            return null;
        }

        ClassDeclarationSyntax generatedClass = declaration.WithAttributeLists(default).WithBaseList(null)
            .WithMembers(SyntaxFactory.List(methods)).WithoutTrivia();
        if (generatedName is not null)
        {
            generatedClass = generatedClass.WithIdentifier(SyntaxFactory.Identifier(generatedName));
        }
        CompilationUnitSyntax root = (CompilationUnitSyntax)declaration.SyntaxTree.GetRoot(cancellationToken);
        BaseNamespaceDeclarationSyntax originalNamespace = (BaseNamespaceDeclarationSyntax)declaration.Parent!;
        NamespaceDeclarationSyntax generatedNamespace = SyntaxFactory.NamespaceDeclaration(originalNamespace.Name)
            .WithUsings(originalNamespace.Usings).WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(generatedClass));
        string source = SyntaxFactory.CompilationUnit().WithUsings(root.Usings)
            .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(generatedNamespace))
            .NormalizeWhitespace().ToFullString();

        return "// Licensed to the .NET Foundation under one or more agreements.\n" +
            "// The .NET Foundation licenses this file to you under the MIT license.\n" +
            "// <auto-generated/>\n#nullable enable\n" + source + "\n";
    }

    private sealed class Rewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel _semanticModel;
        private readonly IMethodSymbol _method;
        private readonly ITypeParameterSymbol _mode;
        private readonly CancellationToken _cancellationToken;
        private readonly List<Diagnostic> _errors;
        private readonly ITypeSymbol? _resultType;
        private readonly string _syncModeName;
        private readonly bool _plainContract;

        internal Rewriter(SemanticModel semanticModel, IMethodSymbol method, CancellationToken cancellationToken, List<Diagnostic> errors, bool plainContract)
        {
            _semanticModel = semanticModel;
            _method = method;
            _mode = GetMode(method)!;
            _cancellationToken = cancellationToken;
            _errors = errors;
            _plainContract = plainContract;
            _resultType = method.ReturnsVoid ? null : ((INamedTypeSymbol)method.ReturnType).TypeArguments.FirstOrDefault();
            _syncModeName = _mode.ConstraintTypes.Any(static type => type.ToDisplayString() == "System.Runtime.Serialization.IReflectionSerialization")
                ? plainContract ? "global::System.Runtime.Serialization.SyncReflectionPolicy" : "global::System.Runtime.Serialization.SyncReflectionSerialization"
                : "global::System.Xml.SyncXmlStreamIO";
        }

        internal MethodDeclarationSyntax Lower(MethodDeclarationSyntax method)
        {
            foreach (SyntaxNode node in method.DescendantNodes())
            {
                if (node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax ||
                    node is UsingStatementSyntax usingStatement && !usingStatement.AwaitKeyword.IsKind(SyntaxKind.None) ||
                    node is LocalDeclarationStatementSyntax local && !local.AwaitKeyword.IsKind(SyntaxKind.None) ||
                    node is CommonForEachStatementSyntax forEach && !forEach.AwaitKeyword.IsKind(SyntaxKind.None))
                {
                    Error(node, "nested functions, await using, and await foreach are not supported");
                }
            }

            BlockSyntax? body = (BlockSyntax?)Visit(method.Body);
            ArrowExpressionClauseSyntax? expressionBody = method.ExpressionBody;
            if (expressionBody is not null)
            {
                if (!_method.IsAsync && _resultType is null)
                {
                    body = SyntaxFactory.Block(CompleteStatement(expressionBody.Expression));
                    expressionBody = null;
                }
                else
                {
                    expressionBody = expressionBody.WithExpression(_method.IsAsync ?
                        (ExpressionSyntax)Visit(expressionBody.Expression)! : Complete(expressionBody.Expression));
                }
            }

            SeparatedSyntaxList<TypeParameterSyntax> parameters = SyntaxFactory.SeparatedList(method.TypeParameterList!.Parameters
                .Where(parameter => parameter.Identifier.ValueText != _mode.Name));
            bool readEntryPoint = IsReadEntryPoint(_method);
            return method.WithIdentifier(SyntaxFactory.Identifier(readEntryPoint ? "Read" : method.Identifier.ValueText + "Sync"))
                .WithModifiers(readEntryPoint ? SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword)) :
                    SyntaxFactory.TokenList(method.Modifiers.Where(static modifier => !modifier.IsKind(SyntaxKind.AsyncKeyword))))
                .WithReturnType(_resultType is null ? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)) : TypeName(_resultType))
                .WithTypeParameterList(parameters.Count == 0 ? null : method.TypeParameterList.WithParameters(parameters))
                .WithConstraintClauses(SyntaxFactory.List(method.ConstraintClauses.Where(clause => clause.Name.Identifier.ValueText != _mode.Name)
                    .Select(clause => (TypeParameterConstraintClauseSyntax)Visit(clause)!)))
                .WithParameterList(method.ParameterList.WithParameters(SyntaxFactory.SeparatedList(method.ParameterList.Parameters
                    .Where((_, index) => !IsCancellationToken(_method.Parameters[index].Type)).Select(parameter => (ParameterSyntax)Visit(parameter)!))))
                .WithBody(body).WithExpressionBody(expressionBody)
                .WithSemicolonToken(expressionBody is null ? default : method.SemicolonToken);
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            ISymbol? symbol = _semanticModel.GetSymbolInfo(node, _cancellationToken).Symbol;
            if (_plainContract && symbol is INamedTypeSymbol type && SyncContractLowering.IsDelegateName(type.Name))
            {
                return SyntaxFactory.IdentifierName("Sync" + type.Name).WithTriviaFrom(node);
            }
            if (SymbolEqualityComparer.Default.Equals(symbol, _mode))
            {
                return SyntaxFactory.ParseName(_syncModeName).WithTriviaFrom(node);
            }
            // Only noncancellable synchronous entrypoints call these generated methods.
            if (symbol is IParameterSymbol parameter && IsCancellationToken(parameter.Type) &&
                SymbolEqualityComparer.Default.Equals(parameter.ContainingSymbol, _method))
            {
                return SyntaxFactory.ParseExpression("global::System.Threading.CancellationToken.None").WithTriviaFrom(node);
            }

            return base.VisitIdentifierName(node);
        }

        public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
        {
            if (_plainContract && _semanticModel.GetSymbolInfo(node, _cancellationToken).Symbol is INamedTypeSymbol)
            {
                return SyncContractLowering.PlainType(node, _semanticModel, _cancellationToken);
            }
            if (node.Parent is not InvocationExpressionSyntax &&
                !(node.Parent is MemberAccessExpressionSyntax member && member.Parent is InvocationExpressionSyntax) &&
                _semanticModel.GetSymbolInfo(node, _cancellationToken).Symbol is IMethodSymbol method && CanLowerCall(method))
            {
                Error(node, "method groups are not supported");
            }

            return base.VisitGenericName(node);
        }

        public override SyntaxNode? VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            if (TryModeCondition(node.Condition, out bool condition))
            {
                return Parenthesize((ExpressionSyntax)Visit(condition ? node.WhenTrue : node.WhenFalse)!).WithTriviaFrom(node);
            }

            return base.VisitConditionalExpression(node);
        }

        public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
        {
            if (_plainContract && SyncContractLowering.TryCompletionCondition(node.Condition, _semanticModel, _cancellationToken, out bool completed))
            {
                return completed ? Visit(node.Statement) :
                    node.Else is null ? SyntaxFactory.EmptyStatement() : Visit(node.Else.Statement);
            }
            if (!TryModeCondition(node.Condition, out bool condition))
            {
                return base.VisitIfStatement(node);
            }
            foreach (SingleVariableDesignationSyntax variable in node.Condition.DescendantNodes().OfType<SingleVariableDesignationSyntax>())
            {
                ISymbol? symbol = _semanticModel.GetDeclaredSymbol(variable, _cancellationToken);
                SyntaxNode declaration = _method.DeclaringSyntaxReferences[0].GetSyntax(_cancellationToken);
                if (declaration.DescendantNodes().OfType<IdentifierNameSyntax>().Any(reference =>
                    !node.Span.Contains(reference.Span) && reference.Identifier.ValueText == variable.Identifier.ValueText &&
                    SymbolEqualityComparer.Default.Equals(_semanticModel.GetSymbolInfo(reference, _cancellationToken).Symbol, symbol)))
                {
                    Error(node.Condition, "declare out and pattern variables before a mode-only condition");
                }
            }

            return (condition ? Visit(node.Statement) : node.Else is null ? SyntaxFactory.EmptyStatement() : Visit(node.Else.Statement))!.WithTriviaFrom(node);
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (_plainContract && node.Name.Identifier.ValueText == "Result" &&
                _semanticModel.GetSymbolInfo(node, _cancellationToken).Symbol is IPropertySymbol property &&
                SyncContractLowering.IsAsyncOperation(property.ContainingType))
            {
                return Visit(node.Expression);
            }
            return base.VisitMemberAccessExpression(node);
        }

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            List<StatementSyntax> statements = new List<StatementSyntax>();
            foreach (StatementSyntax statement in node.Statements)
            {
                StatementSyntax lowered = (StatementSyntax)Visit(statement)!;
                if (lowered is not EmptyStatementSyntax)
                {
                    statements.Add(lowered);
                }
                if (EndsControlFlow(lowered))
                {
                    break;
                }
            }

            return node.WithStatements(SyntaxFactory.List(statements));
        }

        private static bool EndsControlFlow(StatementSyntax statement) => statement is
            ReturnStatementSyntax or ThrowStatementSyntax or GotoStatementSyntax or BreakStatementSyntax or ContinueStatementSyntax ||
            statement is BlockSyntax { Statements.Count: > 0 } block && EndsControlFlow(block.Statements.Last());

        public override SyntaxNode? VisitAwaitExpression(AwaitExpressionSyntax node)
        {
            return Parenthesize(Complete(AwaitedOperation(node))).WithTriviaFrom(node);
        }

        public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            if (node.Expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } invocation &&
                _semanticModel.GetSymbolInfo(invocation, _cancellationToken).Symbol is IMethodSymbol { Name: "ThrowIfCancellationRequested" } method &&
                IsCancellationToken(method.ContainingType) && IsSyncToken(member.Expression))
            {
                return SyntaxFactory.EmptyStatement().WithTriviaFrom(node);
            }

            return node.Expression is AwaitExpressionSyntax awaitExpression ?
                CompleteStatement(AwaitedOperation(awaitExpression)).WithTriviaFrom(node) : base.VisitExpressionStatement(node);
        }

        private ExpressionSyntax AwaitedOperation(AwaitExpressionSyntax node)
        {
            ExpressionSyntax expression = node.Expression;
            if (expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } configured &&
                _semanticModel.GetSymbolInfo(configured, _cancellationToken).Symbol is IMethodSymbol { Name: "ConfigureAwait" } configureMethod &&
                IsValueTask(configureMethod.ContainingType))
            {
                if (configured.ArgumentList.Arguments.Count != 1 ||
                    _semanticModel.GetConstantValue(configured.ArgumentList.Arguments[0].Expression, _cancellationToken).Value is not false)
                {
                    Error(node, "only ConfigureAwait(false) is supported");
                }
                expression = member.Expression;
            }
            if (!IsValueTask(_semanticModel.GetTypeInfo(expression, _cancellationToken).Type))
            {
                Error(node, "await must consume a ValueTask");
            }

            return expression;
        }

        public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
        {
            if (_method.IsAsync || node.Expression is null)
            {
                return base.VisitReturnStatement(node);
            }

            if (_resultType is not null)
            {
                return node.WithExpression(Complete(node.Expression));
            }

            StatementSyntax statement = CompleteStatement(node.Expression);
            return statement is EmptyStatementSyntax ? node.WithExpression(null) :
                SyntaxFactory.Block(statement, SyntaxFactory.ReturnStatement()).WithTriviaFrom(node);
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" } &&
                _semanticModel.GetConstantValue(node, _cancellationToken).Value is string name)
            {
                return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(name)).WithTriviaFrom(node);
            }

            IMethodSymbol? method = _semanticModel.GetSymbolInfo(node, _cancellationToken).Symbol as IMethodSymbol;
            if (method?.Name == "Complete" && method.ContainingType.ToDisplayString() == "System.Runtime.Serialization.ReflectionSerialization" &&
                node.ArgumentList.Arguments.Count == 1)
            {
                return Complete(node.ArgumentList.Arguments[0].Expression).WithTriviaFrom(node);
            }
            if (method is not null && CanLowerCall(method))
            {
                if (method.ReturnsVoid)
                {
                    return LowerCall(node, method);
                }
                Error(node, "ValueTask calls must be awaited, returned, or passed to the completed-only adapter");
            }

            return base.VisitInvocationExpression(node);
        }

        private ExpressionSyntax Complete(ExpressionSyntax expression)
        {
            if (_plainContract)
            {
                return (ExpressionSyntax)Visit(expression)!;
            }
            if (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                return SyntaxFactory.ParenthesizedExpression(Complete(parenthesized.Expression));
            }
            if (expression is ConditionalExpressionSyntax conditional)
            {
                if (TryModeCondition(conditional.Condition, out bool condition))
                {
                    return Complete(condition ? conditional.WhenTrue : conditional.WhenFalse);
                }

                return conditional.WithCondition((ExpressionSyntax)Visit(conditional.Condition)!)
                    .WithWhenTrue(Complete(conditional.WhenTrue)).WithWhenFalse(Complete(conditional.WhenFalse));
            }
            if (expression is InvocationExpressionSyntax invocation &&
                _semanticModel.GetSymbolInfo(invocation, _cancellationToken).Symbol is IMethodSymbol method && CanLowerCall(method))
            {
                return LowerCall(invocation, method);
            }
            if (expression is ObjectCreationExpressionSyntax creation &&
                _semanticModel.GetSymbolInfo(creation, _cancellationToken).Symbol is IMethodSymbol constructor &&
                IsValueTask(constructor.ContainingType) && constructor.ContainingType.Arity == 1 &&
                creation.ArgumentList?.Arguments.Count == 1 &&
                SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, constructor.ContainingType.TypeArguments[0]))
            {
                return (ExpressionSyntax)Visit(creation.ArgumentList.Arguments[0].Expression)!;
            }
            if (expression is DefaultExpressionSyntax || expression.IsKind(SyntaxKind.DefaultLiteralExpression))
            {
                if (_semanticModel.GetTypeInfo(expression, _cancellationToken).ConvertedType is INamedTypeSymbol type &&
                    IsValueTask(type) && type.Arity == 1)
                {
                    return SyntaxFactory.DefaultExpression(TypeName(type.TypeArguments[0]));
                }
            }

            // Synchronous leaves are validated only in Debug builds.
            return SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("global::System.Runtime.Serialization.ReflectionSerialization.Complete"),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument((ExpressionSyntax)Visit(expression)!))));
        }

        private StatementSyntax CompleteStatement(ExpressionSyntax expression)
        {
            if (IsCompletedNoOp(expression))
            {
                return SyntaxFactory.EmptyStatement();
            }
            if (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                return CompleteStatement(parenthesized.Expression);
            }
            if (expression is ConditionalExpressionSyntax conditional)
            {
                if (TryModeCondition(conditional.Condition, out bool condition))
                {
                    return CompleteStatement(condition ? conditional.WhenTrue : conditional.WhenFalse);
                }

                return SyntaxFactory.IfStatement((ExpressionSyntax)Visit(conditional.Condition)!, SyntaxFactory.Block(CompleteStatement(conditional.WhenTrue)),
                    SyntaxFactory.ElseClause(SyntaxFactory.Block(CompleteStatement(conditional.WhenFalse))));
            }

            ExpressionSyntax completed = Complete(expression);
            if (completed is not InvocationExpressionSyntax)
            {
                completed = SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("global::System.Runtime.Serialization.ReflectionSerialization.Complete"),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument((ExpressionSyntax)Visit(expression)!))));
            }

            return SyntaxFactory.ExpressionStatement(completed);
        }

        private bool IsCompletedNoOp(ExpressionSyntax expression)
        {
            if (_semanticModel.GetSymbolInfo(expression, _cancellationToken).Symbol is IPropertySymbol { Name: "CompletedTask", IsStatic: true } property &&
                IsValueTask(property.ContainingType) && property.ContainingType.Arity == 0)
            {
                return true;
            }

            return (expression is DefaultExpressionSyntax || expression.IsKind(SyntaxKind.DefaultLiteralExpression)) &&
                _semanticModel.GetTypeInfo(expression, _cancellationToken).ConvertedType is INamedTypeSymbol { Arity: 0 } type && IsValueTask(type);
        }

        private bool CanLowerCall(IMethodSymbol method) =>
            IsLoweredMethod(method.OriginalDefinition) && GetMode(method.OriginalDefinition) is ITypeParameterSymbol mode &&
            (SymbolEqualityComparer.Default.Equals(method.TypeArguments[mode.Ordinal], _mode) ||
                method.TypeArguments[mode.Ordinal].ToDisplayString() is "System.Xml.SyncXmlStreamIO" or "System.Runtime.Serialization.SyncReflectionSerialization");

        private ExpressionSyntax LowerCall(InvocationExpressionSyntax invocation, IMethodSymbol method)
        {
            int modeOrdinal = GetMode(method.OriginalDefinition)!.Ordinal;
            ExpressionSyntax expression = invocation.Expression;
            SimpleNameSyntax? name = expression as SimpleNameSyntax ?? (expression as MemberAccessExpressionSyntax)?.Name;
            if (name is null)
            {
                Error(invocation, "only direct and member-access method calls are supported");
                return invocation;
            }

            IEnumerable<TypeSyntax> arguments = name is GenericNameSyntax generic ?
                generic.TypeArgumentList.Arguments.Where((_, index) => index != modeOrdinal).Select(argument => (TypeSyntax)Visit(argument)!) :
                method.TypeArguments.Where((_, index) => index != modeOrdinal).Select(TypeName);
            SeparatedSyntaxList<TypeSyntax> typeArguments = SyntaxFactory.SeparatedList(arguments);
            string generatedMethodName = IsReadEntryPoint(method.OriginalDefinition) ? "Read" : name.Identifier.ValueText + "Sync";
            SimpleNameSyntax generatedName = typeArguments.Count == 0 ? SyntaxFactory.IdentifierName(generatedMethodName) :
                SyntaxFactory.GenericName(SyntaxFactory.Identifier(generatedMethodName), SyntaxFactory.TypeArgumentList(typeArguments));
            expression = expression is MemberAccessExpressionSyntax member ?
                member.WithExpression((ExpressionSyntax)Visit(member.Expression)!).WithName(generatedName) : generatedName;

            List<ArgumentSyntax> callArguments = new List<ArgumentSyntax>();
            for (int index = 0; index < invocation.ArgumentList.Arguments.Count; index++)
            {
                ArgumentSyntax argument = invocation.ArgumentList.Arguments[index];
                IParameterSymbol parameter = argument.NameColon is null ? method.Parameters[index] :
                    method.Parameters.First(parameter => parameter.Name == argument.NameColon.Name.Identifier.ValueText);
                if (IsCancellationToken(parameter.Type))
                {
                    if (!IsSyncToken(argument.Expression))
                    {
                        Error(argument, "cancellation arguments must forward the method token or CancellationToken.None");
                    }
                }
                else
                {
                    callArguments.Add((ArgumentSyntax)Visit(argument)!);
                }
            }

            return invocation.WithExpression(expression).WithArgumentList(invocation.ArgumentList.WithArguments(SyntaxFactory.SeparatedList(callArguments)));
        }

        private bool IsSyncToken(ExpressionSyntax expression)
        {
            ISymbol? symbol = _semanticModel.GetSymbolInfo(expression, _cancellationToken).Symbol;
            return symbol is IParameterSymbol parameter && IsCancellationToken(parameter.Type) &&
                SymbolEqualityComparer.Default.Equals(parameter.ContainingSymbol, _method) ||
                symbol is IPropertySymbol { Name: "None", IsStatic: true } property && IsCancellationToken(property.ContainingType);
        }

        private bool TryModeCondition(ExpressionSyntax expression, out bool result)
        {
            while (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                expression = parenthesized.Expression;
            }
            result = false;
            if (expression is LiteralExpressionSyntax literal &&
                (literal.IsKind(SyntaxKind.TrueLiteralExpression) || literal.IsKind(SyntaxKind.FalseLiteralExpression)))
            {
                result = literal.IsKind(SyntaxKind.TrueLiteralExpression);
                return true;
            }
            if (expression is PrefixUnaryExpressionSyntax not && not.IsKind(SyntaxKind.LogicalNotExpression) &&
                TryModeCondition(not.Operand, out bool operand))
            {
                result = !operand;
                return true;
            }
            if (expression is BinaryExpressionSyntax boolean &&
                (boolean.IsKind(SyntaxKind.LogicalAndExpression) || boolean.IsKind(SyntaxKind.LogicalOrExpression)) &&
                TryModeCondition(boolean.Left, out bool leftValue))
            {
                if (leftValue == boolean.IsKind(SyntaxKind.LogicalOrExpression))
                {
                    result = leftValue;
                    return true;
                }

                return TryModeCondition(boolean.Right, out result);
            }
            if (expression is not BinaryExpressionSyntax binary ||
                !(binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression)) ||
                binary.Left is not TypeOfExpressionSyntax left || binary.Right is not TypeOfExpressionSyntax right)
            {
                return false;
            }

            ITypeSymbol? leftType = _semanticModel.GetTypeInfo(left.Type, _cancellationToken).Type;
            ITypeSymbol? rightType = _semanticModel.GetTypeInfo(right.Type, _cancellationToken).Type;
            ITypeSymbol? other = SymbolEqualityComparer.Default.Equals(leftType, _mode) ? rightType :
                SymbolEqualityComparer.Default.Equals(rightType, _mode) ? leftType : null;
            string? name = other?.ToDisplayString();
            if (name is not ("System.Xml.SyncXmlStreamIO" or "System.Xml.AsyncXmlStreamIO" or
                "System.Runtime.Serialization.SyncReflectionSerialization" or "System.Runtime.Serialization.AsyncReflectionSerialization"))
            {
                return false;
            }

            result = (name is "System.Xml.SyncXmlStreamIO" or "System.Runtime.Serialization.SyncReflectionSerialization") == binary.IsKind(SyntaxKind.EqualsExpression);
            return true;
        }

        private TypeSyntax TypeName(ITypeSymbol type) => SyntaxFactory.ParseTypeName(string.Concat(type.ToDisplayParts(
            SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier))
            .Select(part => SymbolEqualityComparer.Default.Equals(part.Symbol, _mode) ? _syncModeName : part.ToString())));

        private static ExpressionSyntax Parenthesize(ExpressionSyntax expression) =>
            expression is LiteralExpressionSyntax or IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax or ParenthesizedExpressionSyntax ?
                expression : SyntaxFactory.ParenthesizedExpression(expression);

        private void Error(SyntaxNode node, string message) =>
            _errors.Add(Diagnostic.Create(SyncReaderLoweringAnalyzer.UnsupportedShape, node.GetLocation(), _method.Name, message));
    }
}
