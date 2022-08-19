// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

[assembly: System.Resources.NeutralResourcesLanguage("en-US")]

namespace Microsoft.Interop.JavaScript
{
    [Generator]
    public sealed class JSImportGenerator : IIncrementalGenerator
    {
        internal sealed record IncrementalStubGenerationContext(
            StubEnvironment Environment,
            JSSignatureContext SignatureContext,
            ContainingSyntaxContext ContainingSyntaxContext,
            ContainingSyntax StubMethodSyntaxTemplate,
            MethodSignatureDiagnosticLocations DiagnosticLocation,
            JSImportData JSImportData,
            MarshallingGeneratorFactoryKey<(TargetFramework, Version, JSGeneratorOptions)> GeneratorFactoryKey,
            ImmutableArray<Diagnostic> Diagnostics)
        {
            public bool Equals(IncrementalStubGenerationContext? other)
            {
                return other is not null
                    && StubEnvironment.AreCompilationSettingsEqual(Environment, other.Environment)
                    && SignatureContext.Equals(other.SignatureContext)
                    && ContainingSyntaxContext.Equals(other.ContainingSyntaxContext)
                    && StubMethodSyntaxTemplate.Equals(other.StubMethodSyntaxTemplate)
                    && JSImportData.Equals(other.JSImportData)
                    && DiagnosticLocation.Equals(DiagnosticLocation)
                    && GeneratorFactoryKey.Equals(other.GeneratorFactoryKey)
                    && Diagnostics.SequenceEqual(other.Diagnostics);
            }

            public override int GetHashCode()
            {
                throw new UnreachableException();
            }
        }

        public static class StepNames
        {
            public const string CalculateStubInformation = nameof(CalculateStubInformation);
            public const string GenerateSingleStub = nameof(GenerateSingleStub);
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Collect all methods adorned with JSImportAttribute
            var attributedMethods = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (node, ct) => ShouldVisitNode(node),
                    static (context, ct) =>
                    {
                        MethodDeclarationSyntax syntax = (MethodDeclarationSyntax)context.Node;
                        if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is IMethodSymbol methodSymbol
                            && methodSymbol.GetAttributes().Any(static attribute => attribute.AttributeClass?.ToDisplayString() == Constants.JSImportAttribute))
                        {
                            return new { Syntax = syntax, Symbol = methodSymbol };
                        }

                        return null;
                    })
                .Where(
                    static modelData => modelData is not null);

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

                return ImmutableArray.Create(Diagnostic.Create(GeneratorDiagnostics.JSImportRequiresAllowUnsafeBlocks, null));
            }));

            IncrementalValuesProvider<(MemberDeclarationSyntax, ImmutableArray<Diagnostic>)> generateSingleStub = methodsToGenerate
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
                .Combine(stubOptions)
                .Select(
                    static (data, ct) => GenerateSource(data.Left, data.Right)
                )
                .WithComparer(Comparers.GeneratedSyntax)
                .WithTrackingName(StepNames.GenerateSingleStub);

            context.RegisterDiagnostics(generateSingleStub.SelectMany((stubInfo, ct) => stubInfo.Item2));

            context.RegisterConcatenatedSyntaxOutputs(generateSingleStub.Select((data, ct) => data.Item1), "JSImports.g.cs");
        }

        private static SyntaxTokenList StripTriviaFromModifiers(SyntaxTokenList tokenList)
        {
            SyntaxToken[] strippedTokens = new SyntaxToken[tokenList.Count];
            for (int i = 0; i < tokenList.Count; i++)
            {
                strippedTokens[i] = tokenList[i].WithoutTrivia();
            }
            return new SyntaxTokenList(strippedTokens);
        }

        private static TypeDeclarationSyntax CreateTypeDeclarationWithoutTrivia(TypeDeclarationSyntax typeDeclaration)
        {
            return TypeDeclaration(
                typeDeclaration.Kind(),
                typeDeclaration.Identifier)
                .WithTypeParameterList(typeDeclaration.TypeParameterList)
                .WithModifiers(StripTriviaFromModifiers(typeDeclaration.Modifiers));
        }

        private static MemberDeclarationSyntax PrintGeneratedSource(
            ContainingSyntax userDeclaredMethod,
            JSSignatureContext stub,
            BlockSyntax stubCode)
        {
            // Create stub function
            MethodDeclarationSyntax stubMethod = MethodDeclaration(stub.StubReturnType, userDeclaredMethod.Identifier)
                .AddAttributeLists(stub.AdditionalAttributes.ToArray())
                .WithModifiers(StripTriviaFromModifiers(userDeclaredMethod.Modifiers))
                .WithParameterList(ParameterList(SeparatedList(stub.StubParameters)))
                .WithBody(stubCode);

            FieldDeclarationSyntax sigField = FieldDeclaration(VariableDeclaration(IdentifierName(Constants.JSFunctionSignatureGlobal))
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(stub.BindingName)))))
                .AddModifiers(Token(SyntaxKind.StaticKeyword))
                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                    Attribute(IdentifierName(Constants.ThreadStaticGlobal))))));

            MemberDeclarationSyntax toPrint = WrapMethodInContainingScopes(stub, stubMethod, sigField);
            return toPrint;
        }

        private static MemberDeclarationSyntax WrapMethodInContainingScopes(JSSignatureContext stub, MemberDeclarationSyntax stubMethod, MemberDeclarationSyntax sigField)
        {
            // Stub should have at least one containing type
            Debug.Assert(stub.StubContainingTypes.Any());

            // Add stub function and JSImport declaration to the first (innermost) containing
            MemberDeclarationSyntax containingType = CreateTypeDeclarationWithoutTrivia(stub.StubContainingTypes.First())
                .AddMembers(stubMethod, sigField);

            // Add type to the remaining containing types (skipping the first which was handled above)
            foreach (TypeDeclarationSyntax typeDecl in stub.StubContainingTypes.Skip(1))
            {
                containingType = CreateTypeDeclarationWithoutTrivia(typeDecl)
                    .WithMembers(SingletonList(containingType));
            }

            MemberDeclarationSyntax toPrint = containingType;

            // Add type to the containing namespace
            if (stub.StubTypeNamespace is not null)
            {
                toPrint = NamespaceDeclaration(IdentifierName(stub.StubTypeNamespace))
                    .AddMembers(toPrint);
            }

            return toPrint;
        }

        private static JSImportData? ProcessJSImportAttribute(AttributeData attrData)
        {
            // Found the JSImport, but it has an error so report the error.
            // This is most likely an issue with targeting an incorrect TFM.
            if (attrData.AttributeClass?.TypeKind is null or TypeKind.Error)
            {
                return null;
            }

            if (attrData.ConstructorArguments.Length == 1)
            {
                return new JSImportData(attrData.ConstructorArguments[0].Value!.ToString(), null);
            }
            if (attrData.ConstructorArguments.Length == 2)
            {
                return new JSImportData(attrData.ConstructorArguments[0].Value!.ToString(), attrData.ConstructorArguments[1].Value!.ToString());
            }
            return null;
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
            AttributeData? jsImportAttr = null;
            foreach (AttributeData attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass is not null
                    && attr.AttributeClass.ToDisplayString() == Constants.JSImportAttribute)
                {
                    jsImportAttr = attr;
                }
            }

            Debug.Assert(jsImportAttr is not null);

            var generatorDiagnostics = new GeneratorDiagnostics();

            // Process the JSImport attribute
            JSImportData? jsImportData = ProcessJSImportAttribute(jsImportAttr!);

            if (jsImportData is null)
            {
                generatorDiagnostics.ReportConfigurationNotSupported(jsImportAttr!, "Invalid syntax");
                jsImportData = new JSImportData("INVALID_CSHARP_SYNTAX", null);
            }

            // Create the stub.
            var signatureContext = JSSignatureContext.Create(symbol, environment, generatorDiagnostics, ct);

            var containingTypeContext = new ContainingSyntaxContext(originalSyntax);

            var methodSyntaxTemplate = new ContainingSyntax(originalSyntax.Modifiers.StripTriviaFromTokens(), SyntaxKind.MethodDeclaration, originalSyntax.Identifier, originalSyntax.TypeParameterList);
            return new IncrementalStubGenerationContext(
                environment,
                signatureContext,
                containingTypeContext,
                methodSyntaxTemplate,
                new MethodSignatureDiagnosticLocations(originalSyntax),
                jsImportData,
                CreateGeneratorFactory(environment, options),
                generatorDiagnostics.Diagnostics.ToImmutableArray());
        }

        private static MarshallingGeneratorFactoryKey<(TargetFramework, Version, JSGeneratorOptions)> CreateGeneratorFactory(StubEnvironment env, JSGeneratorOptions options)
        {
            JSGeneratorFactory jsGeneratorFactory = new JSGeneratorFactory();

            return MarshallingGeneratorFactoryKey.Create((env.TargetFramework, env.TargetFrameworkVersion, options), jsGeneratorFactory);
        }

        private static (MemberDeclarationSyntax, ImmutableArray<Diagnostic>) GenerateSource(
            IncrementalStubGenerationContext incrementalContext,
            JSGeneratorOptions options)
        {
            var diagnostics = new GeneratorDiagnostics();

            // Generate stub code
            var stubGenerator = new JSImportCodeGenerator(
            incrementalContext.Environment,
            incrementalContext.SignatureContext.ElementTypeInformation,
            incrementalContext.JSImportData,
            incrementalContext.SignatureContext,
            (elementInfo, ex) =>
            {
                diagnostics.ReportMarshallingNotSupported(incrementalContext.DiagnosticLocation, elementInfo, ex.NotSupportedDetails, ex.DiagnosticProperties ?? ImmutableDictionary<string, string>.Empty);
            },
            incrementalContext.GeneratorFactoryKey.GeneratorFactory);

            BlockSyntax code = stubGenerator.GenerateJSImportBody();

            return (PrintGeneratedSource(incrementalContext.StubMethodSyntaxTemplate, incrementalContext.SignatureContext, code), incrementalContext.Diagnostics.AddRange(diagnostics.Diagnostics));
        }

        private static bool ShouldVisitNode(SyntaxNode syntaxNode)
        {
            // We only support C# method declarations.
            if (syntaxNode.Language != LanguageNames.CSharp
                || !syntaxNode.IsKind(SyntaxKind.MethodDeclaration))
            {
                return false;
            }

            // Filter out methods with no attributes early.
            return ((MethodDeclarationSyntax)syntaxNode).AttributeLists.Count > 0;
        }

        private static Diagnostic? GetDiagnosticIfInvalidMethodForGeneration(MethodDeclarationSyntax methodSyntax, IMethodSymbol method)
        {
            // Verify the method has no generic types or defined implementation
            // and is marked static and partial.
            if (methodSyntax.TypeParameterList is not null
                || methodSyntax.Body is not null
                || !methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
                || !methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return Diagnostic.Create(GeneratorDiagnostics.InvalidImportAttributedMethodSignature, methodSyntax.Identifier.GetLocation(), method.Name);
            }

            // Verify that the types the method is declared in are marked partial.
            for (SyntaxNode? parentNode = methodSyntax.Parent; parentNode is TypeDeclarationSyntax typeDecl; parentNode = parentNode.Parent)
            {
                if (!typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return Diagnostic.Create(GeneratorDiagnostics.InvalidImportAttributedMethodContainingTypeMissingModifiers, methodSyntax.Identifier.GetLocation(), method.Name, typeDecl.Identifier);
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
