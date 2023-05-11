// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Collections.Generic;

namespace Microsoft.Interop.JavaScript
{
    [Generator]
    public sealed class JSExportGenerator : IIncrementalGenerator
    {
        internal sealed record IncrementalStubGenerationContext(
            JSSignatureContext SignatureContext,
            ContainingSyntaxContext ContainingSyntaxContext,
            ContainingSyntax StubMethodSyntaxTemplate,
            MethodSignatureDiagnosticLocations DiagnosticLocation,
            JSExportData JSExportData,
            MarshallingGeneratorFactoryKey<(TargetFramework TargetFramework, Version TargetFrameworkVersion, JSGeneratorOptions)> GeneratorFactoryKey,
            SequenceEqualImmutableArray<Diagnostic> Diagnostics);

        public static class StepNames
        {
            public const string CalculateStubInformation = nameof(CalculateStubInformation);
            public const string GenerateSingleStub = nameof(GenerateSingleStub);
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Collect all methods adorned with JSExportAttribute
            var attributedMethods = context.SyntaxProvider
                .ForAttributeWithMetadataName(Constants.JSExportAttribute,
                   static (node, ct) => node is MethodDeclarationSyntax,
                   static (context, ct) => new { Syntax = (MethodDeclarationSyntax)context.TargetNode, Symbol = (IMethodSymbol)context.TargetSymbol });

            // Validate if attributed methods can have source generated
            var methodsWithDiagnostics = attributedMethods.Select(static (data, ct) =>
            {
                Diagnostic? diagnostic = GetDiagnosticIfInvalidMethodForGeneration(data.Syntax, data.Symbol);
                return new { Syntax = data.Syntax, Symbol = data.Symbol, Diagnostic = diagnostic };
            });

            var methodsToGenerate = methodsWithDiagnostics.Where(static data => data.Diagnostic is null);
            var invalidMethodDiagnostics = methodsWithDiagnostics.Where(static data => data.Diagnostic is not null);

            // Report diagnostics for invalid methods
            context.RegisterSourceOutput(invalidMethodDiagnostics, static (context, invalidMethod) =>
            {
                context.ReportDiagnostic(invalidMethod.Diagnostic);
            });

            // Compute generator options
            IncrementalValueProvider<JSGeneratorOptions> stubOptions = context.AnalyzerConfigOptionsProvider
                .Select(static (options, ct) => new JSGeneratorOptions(options.GlobalOptions));

            IncrementalValueProvider<StubEnvironment> stubEnvironment = context.CreateStubEnvironmentProvider();

            // Validate environment that is being used to generate stubs.
            context.RegisterDiagnostics(stubEnvironment.Combine(attributedMethods.Collect()).SelectMany((data, ct) =>
            {
                if (data.Right.IsEmpty // no attributed methods
                    || data.Left.Compilation.Options is CSharpCompilationOptions { AllowUnsafe: true }) // Unsafe code enabled
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                return ImmutableArray.Create(Diagnostic.Create(GeneratorDiagnostics.JSExportRequiresAllowUnsafeBlocks, null));
            }));

            IncrementalValuesProvider<(MemberDeclarationSyntax, StatementSyntax, AttributeListSyntax, ImmutableArray<Diagnostic>)> generateSingleStub = methodsToGenerate
                .Combine(stubEnvironment)
                .Combine(stubOptions)
                .Select(static (data, ct) => new
                {
                    data.Left.Left.Syntax,
                    data.Left.Left.Symbol,
                    Environment = data.Left.Right,
                    Options = data.Right
                })
                .Select(
                    static (data, ct) => CalculateStubInformation(data.Syntax, data.Symbol, data.Environment, data.Options, ct)
                )
                .WithTrackingName(StepNames.CalculateStubInformation)
                .Select(
                    static (data, ct) => GenerateSource(data)
                )
                .WithComparer(Comparers.GeneratedSyntax4)
                .WithTrackingName(StepNames.GenerateSingleStub);

            context.RegisterDiagnostics(generateSingleStub.SelectMany((stubInfo, ct) => stubInfo.Item4));

            IncrementalValueProvider<ImmutableArray<(StatementSyntax, AttributeListSyntax)>> regSyntax = generateSingleStub
                .Select(
                    static (data, ct) => (data.Item2, data.Item3))
                .Collect();

            IncrementalValueProvider<string> registration = regSyntax
                .Select(static (data, ct) => GenerateRegSource(data))
                .Select(static (data, ct) => data.NormalizeWhitespace().ToFullString());

            IncrementalValueProvider<ImmutableArray<(string, string)>> generated = generateSingleStub
                .Combine(registration)
                .Select(
                    static (data, ct) => (data.Left.Item1.NormalizeWhitespace().ToFullString(), data.Right))
                .Collect();


            context.RegisterSourceOutput(generated,
                (context, generatedSources) =>
                {
                    // Don't generate a file if we don't have to, to avoid the extra IDE overhead once we have generated
                    // files in play.
                    if (generatedSources.IsEmpty)
                        return;

                    StringBuilder source = new();
                    // Mark in source that the file is auto-generated.
                    source.AppendLine("// <auto-generated/>");
                    // this is the assembly level registration
                    source.AppendLine(generatedSources[0].Item2);
                    // this is the method wrappers to be called from JS
                    foreach (var generated in generatedSources)
                    {
                        source.AppendLine(generated.Item1);
                    }

                    // Once https://github.com/dotnet/roslyn/issues/61326 is resolved, we can avoid the ToString() here.
                    context.AddSource("JSExports.g.cs", source.ToString());
                });

        }

        private static MemberDeclarationSyntax PrintGeneratedSource(
            ContainingSyntaxContext containingSyntaxContext,
            BlockSyntax wrapperStatements, string wrapperName)
        {

            MemberDeclarationSyntax wrappperMethod = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier(wrapperName))
                .WithModifiers(TokenList(new[] { Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.UnsafeKeyword) }))
                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                    Attribute(IdentifierName(Constants.DebuggerNonUserCodeAttribute))))))
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(Identifier("__arguments_buffer")).WithType(PointerType(ParseTypeName(Constants.JSMarshalerArgumentGlobal))))))
                .WithBody(wrapperStatements);

            MemberDeclarationSyntax toPrint = containingSyntaxContext.WrapMembersInContainingSyntaxWithUnsafeModifier(wrappperMethod);

            return toPrint;
        }

        private static JSExportData? ProcessJSExportAttribute(AttributeData attrData)
        {
            // Found the JSExport, but it has an error so report the error.
            // This is most likely an issue with targeting an incorrect TFM.
            if (attrData.AttributeClass?.TypeKind is null or TypeKind.Error)
            {
                return null;
            }

            return new JSExportData();
        }

        private static IncrementalStubGenerationContext CalculateStubInformation(
            MethodDeclarationSyntax originalSyntax,
            IMethodSymbol symbol,
            StubEnvironment environment,
            JSGeneratorOptions options,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            // Get any attributes of interest on the method
            AttributeData? jsExportAttr = null;
            foreach (AttributeData attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass is not null
                    && attr.AttributeClass.ToDisplayString() == Constants.JSExportAttribute)
                {
                    jsExportAttr = attr;
                }
            }

            Debug.Assert(jsExportAttr is not null);

            var generatorDiagnostics = new GeneratorDiagnostics();

            // Process the JSExport attribute
            JSExportData? jsExportData = ProcessJSExportAttribute(jsExportAttr!);

            if (jsExportData is null)
            {
                generatorDiagnostics.ReportConfigurationNotSupported(jsExportAttr!, "Invalid syntax");
                jsExportData = new JSExportData();
            }

            // Create the stub.
            var signatureContext = JSSignatureContext.Create(symbol, environment, generatorDiagnostics, ct);

            var containingTypeContext = new ContainingSyntaxContext(originalSyntax);

            var methodSyntaxTemplate = new ContainingSyntax(originalSyntax.Modifiers.StripTriviaFromTokens(), SyntaxKind.MethodDeclaration, originalSyntax.Identifier, originalSyntax.TypeParameterList);

            return new IncrementalStubGenerationContext(
                signatureContext,
                containingTypeContext,
                methodSyntaxTemplate,
                new MethodSignatureDiagnosticLocations(originalSyntax),
                jsExportData,
                CreateGeneratorFactory(environment, options),
                new SequenceEqualImmutableArray<Diagnostic>(generatorDiagnostics.Diagnostics.ToImmutableArray()));
        }

        private static MarshallingGeneratorFactoryKey<(TargetFramework, Version, JSGeneratorOptions)> CreateGeneratorFactory(StubEnvironment env, JSGeneratorOptions options)
        {
            JSGeneratorFactory jsGeneratorFactory = new JSGeneratorFactory();
            return MarshallingGeneratorFactoryKey.Create((env.TargetFramework, env.TargetFrameworkVersion, options), jsGeneratorFactory);
        }

        private static NamespaceDeclarationSyntax GenerateRegSource(
            ImmutableArray<(StatementSyntax Registration, AttributeListSyntax Attribute)> methods)
        {
            const string generatedNamespace = "System.Runtime.InteropServices.JavaScript";
            const string initializerClass = "__GeneratedInitializer";
            const string initializerName = "__Register_";
            const string selfInitName = "__Net7SelfInit_";

            if (methods.IsEmpty) return NamespaceDeclaration(IdentifierName(generatedNamespace));

            var registerStatements = new List<StatementSyntax>();
            registerStatements.AddRange(JSExportCodeGenerator.GenerateJSExportArchitectureCheck());

            var attributes = new List<AttributeListSyntax>();
            foreach (var m in methods)
            {
                registerStatements.Add(m.Registration);
                attributes.Add(m.Attribute);
            }

            FieldDeclarationSyntax field = FieldDeclaration(VariableDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)))
                            .WithVariables(SingletonSeparatedList(
                                VariableDeclarator(Identifier("initialized")))))
                            .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)))
                            .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                                Attribute(IdentifierName(Constants.ThreadStaticGlobal))))));

            MemberDeclarationSyntax method = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier(initializerName))
                            .WithAttributeLists(List(attributes))
                            .WithModifiers(TokenList(new[] { Token(SyntaxKind.StaticKeyword) }))
                            .WithBody(Block(registerStatements));

            // when we are running code generated by .NET8 on .NET7 runtime we need to auto initialize the assembly, because .NET7 doesn't call the registration from JS
            // this also keeps the code protected from trimming
            MemberDeclarationSyntax initializerMethod = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier(selfInitName))
                            .WithAttributeLists(List(new[]{
                                    AttributeList(SingletonSeparatedList(Attribute(IdentifierName(Constants.ModuleInitializerAttributeGlobal)))),
                                }))
                            .WithModifiers(TokenList(new[] {
                                Token(SyntaxKind.StaticKeyword),
                                Token(SyntaxKind.InternalKeyword)
                            }))
                            .WithBody(Block(
                                IfStatement(BinaryExpression(SyntaxKind.EqualsExpression,
                                    IdentifierName("Environment.Version.Major"),
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(7))),
                                    Block(SingletonList<StatementSyntax>(
                                        ExpressionStatement(InvocationExpression(IdentifierName(initializerName))))))));

            var ns = NamespaceDeclaration(IdentifierName(generatedNamespace))
                        .WithMembers(
                            SingletonList<MemberDeclarationSyntax>(
                                ClassDeclaration(initializerClass)
                                .WithModifiers(TokenList(new SyntaxToken[]{
                                    Token(SyntaxKind.UnsafeKeyword)}))
                                .WithMembers(List(new[] { field, initializerMethod, method }))
                                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                                    Attribute(IdentifierName(Constants.CompilerGeneratedAttributeGlobal)))
                                )))));

            return ns;
        }

        private static (MemberDeclarationSyntax, StatementSyntax, AttributeListSyntax, ImmutableArray<Diagnostic>) GenerateSource(
            IncrementalStubGenerationContext incrementalContext)
        {
            var diagnostics = new GeneratorDiagnostics();

            // Generate stub code
            var stubGenerator = new JSExportCodeGenerator(
            incrementalContext.GeneratorFactoryKey.Key.TargetFramework,
            incrementalContext.GeneratorFactoryKey.Key.TargetFrameworkVersion,
            incrementalContext.SignatureContext.SignatureContext.ElementTypeInformation,
            incrementalContext.JSExportData,
            incrementalContext.SignatureContext,
            (elementInfo, ex) =>
            {
                diagnostics.ReportMarshallingNotSupported(incrementalContext.DiagnosticLocation, elementInfo, ex.NotSupportedDetails, ex.DiagnosticProperties ?? ImmutableDictionary<string, string>.Empty);
            },
            incrementalContext.GeneratorFactoryKey.GeneratorFactory);

            var wrapperName = "__Wrapper_" + incrementalContext.StubMethodSyntaxTemplate.Identifier + "_" + incrementalContext.SignatureContext.TypesHash;

            BlockSyntax wrapper = stubGenerator.GenerateJSExportBody();
            StatementSyntax registration = stubGenerator.GenerateJSExportRegistration();
            AttributeListSyntax registrationAttribute = AttributeList(SingletonSeparatedList(Attribute(IdentifierName(Constants.DynamicDependencyAttributeGlobal))
                    .WithArgumentList(AttributeArgumentList(SeparatedList(new[]{
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(wrapperName))),
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(incrementalContext.SignatureContext.StubTypeFullName))),
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(incrementalContext.SignatureContext.AssemblyName))),
                    }
                    )))));

            return (PrintGeneratedSource(incrementalContext.ContainingSyntaxContext, wrapper, wrapperName),
                registration, registrationAttribute,
                incrementalContext.Diagnostics.Array.AddRange(diagnostics.Diagnostics));
        }

        private static Diagnostic? GetDiagnosticIfInvalidMethodForGeneration(MethodDeclarationSyntax methodSyntax, IMethodSymbol method)
        {
            // Verify the method has no generic types or defined implementation
            // and is marked static and partial.
            if (methodSyntax.TypeParameterList is not null
                || (methodSyntax.Body is null && methodSyntax.ExpressionBody is null)
                || !methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
                || methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return Diagnostic.Create(GeneratorDiagnostics.InvalidExportAttributedMethodSignature, methodSyntax.Identifier.GetLocation(), method.Name);
            }

            // Verify that the types the method is declared in are marked partial.
            for (SyntaxNode? parentNode = methodSyntax.Parent; parentNode is TypeDeclarationSyntax typeDecl; parentNode = parentNode.Parent)
            {
                if (!typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return Diagnostic.Create(GeneratorDiagnostics.InvalidExportAttributedMethodContainingTypeMissingModifiers, methodSyntax.Identifier.GetLocation(), method.Name, typeDecl.Identifier);
                }
            }

            // Verify the method does not have a ref return
            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
            {
                return Diagnostic.Create(GeneratorDiagnostics.ReturnConfigurationNotSupported, methodSyntax.Identifier.GetLocation(), "ref return", method.ToDisplayString());
            }

            return null;
        }
    }
}
