// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

[assembly: System.Resources.NeutralResourcesLanguage("en-US")]

namespace Microsoft.Interop
{
    [Generator]
    public sealed class DownlevelLibraryImportGenerator : IIncrementalGenerator
    {
        internal sealed record IncrementalStubGenerationContext(
            SignatureContext SignatureContext,
            ContainingSyntaxContext ContainingSyntaxContext,
            ContainingSyntax StubMethodSyntaxTemplate,
            MethodSignatureDiagnosticLocations DiagnosticLocation,
            SequenceEqualImmutableArray<AttributeSyntax> ForwardedAttributes,
            LibraryImportData LibraryImportData,
            EnvironmentFlags EnvironmentFlags);

        public static class StepNames
        {
            public const string CalculateStubInformation = nameof(CalculateStubInformation);
            public const string GenerateSingleStub = nameof(GenerateSingleStub);
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Collect all methods adorned with LibraryImportAttribute and filter out invalid ones
            // (diagnostics for invalid methods are reported by the analyzer)
            var methodsToGenerate = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    TypeNames.LibraryImportAttribute,
                    static (node, ct) => node is MethodDeclarationSyntax,
                    static (context, ct) => context.TargetSymbol is IMethodSymbol methodSymbol
                        ? new { Syntax = (MethodDeclarationSyntax)context.TargetNode, Symbol = methodSymbol }
                        : null)
                .Where(
                    static modelData => modelData is not null
                        && Analyzers.DownlevelLibraryImportDiagnosticsAnalyzer.GetDiagnosticIfInvalidMethodForGeneration(modelData.Syntax, modelData.Symbol) is null);

            IncrementalValueProvider<StubEnvironment> stubEnvironment = context.CreateStubEnvironmentProvider();

            IncrementalValuesProvider<MemberDeclarationSyntax> generateSingleStub = methodsToGenerate
                .Combine(stubEnvironment)
                .Select(static (data, ct) => new
                {
                    data.Left.Syntax,
                    data.Left.Symbol,
                    Environment = data.Right,
                })
                .Select(
                    static (data, ct) => CalculateStubInformation(data.Syntax, data.Symbol, data.Environment, ct)
                )
                .WithTrackingName(StepNames.CalculateStubInformation)
                .Select(
                    static (data, ct) => GenerateSource(data)
                )
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .WithTrackingName(StepNames.GenerateSingleStub);

            context.RegisterConcatenatedSyntaxOutputs(generateSingleStub, "LibraryImports.g.cs");
        }

        private static List<AttributeSyntax> GenerateSyntaxForForwardedAttributes(AttributeData? defaultDllImportSearchPathsAttribute)
        {
            // Manually rehydrate the forwarded attributes with fully qualified types so we don't have to worry about any using directives.
            List<AttributeSyntax> attributes = [];

            if (defaultDllImportSearchPathsAttribute is not null)
            {
                attributes.Add(
                    Attribute(NameSyntaxes.DefaultDllImportSearchPathsAttribute).AddArgumentListArguments(
                        AttributeArgument(
                            CastExpression(TypeSyntaxes.DllImportSearchPath,
                                LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    Literal((int)defaultDllImportSearchPathsAttribute.ConstructorArguments[0].Value!))))));
            }
            return attributes;
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

        private static MethodDeclarationSyntax PrintGeneratedSource(
            ContainingSyntax userDeclaredMethod,
            SignatureContext stub,
            BlockSyntax stubCode)
        {
            // Create stub function
            return MethodDeclaration(stub.StubReturnType, userDeclaredMethod.Identifier)
                .AddAttributeLists(stub.AdditionalAttributes.ToArray())
                .WithModifiers(StripTriviaFromModifiers(userDeclaredMethod.Modifiers))
                .WithParameterList(ParameterList(SeparatedList(stub.StubParameters)))
                .WithBody(stubCode);
        }

        private static LibraryImportCompilationData? ProcessLibraryImportAttribute(AttributeData attrData)
        {
            // Found the LibraryImport, but it has an error so report the error.
            // This is most likely an issue with targeting an incorrect TFM.
            if (attrData.AttributeClass?.TypeKind is null or TypeKind.Error)
            {
                return null;
            }

            if (attrData.ConstructorArguments.Length == 0)
            {
                return null;
            }

            ImmutableDictionary<string, TypedConstant> namedArguments = ImmutableDictionary.CreateRange(attrData.NamedArguments);

            string? entryPoint = null;
            if (namedArguments.TryGetValue(nameof(LibraryImportCompilationData.EntryPoint), out TypedConstant entryPointValue))
            {
                if (entryPointValue.Value is not string)
                {
                    return null;
                }
                entryPoint = (string)entryPointValue.Value!;
            }

            return new LibraryImportCompilationData(attrData.ConstructorArguments[0].Value!.ToString())
            {
                EntryPoint = entryPoint,
            }.WithValuesFromNamedArguments(namedArguments);
        }

        private static IncrementalStubGenerationContext CalculateStubInformation(
            MethodDeclarationSyntax originalSyntax,
            IMethodSymbol symbol,
            StubEnvironment environment,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            INamedTypeSymbol? defaultDllImportSearchPathsAttrType = environment.DefaultDllImportSearchPathsAttrType;
            // Get any attributes of interest on the method
            AttributeData? generatedDllImportAttr = null;
            AttributeData? defaultDllImportSearchPathsAttribute = null;
            foreach (AttributeData attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass is not null
                    && attr.AttributeClass.ToDisplayString() == TypeNames.LibraryImportAttribute)
                {
                    generatedDllImportAttr = attr;
                }
                else if (defaultDllImportSearchPathsAttrType is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, defaultDllImportSearchPathsAttrType))
                {
                    defaultDllImportSearchPathsAttribute = attr;
                }
            }

            Debug.Assert(generatedDllImportAttr is not null);

            var locations = new MethodSignatureDiagnosticLocations(originalSyntax);

            // Process the LibraryImport attribute
            LibraryImportCompilationData libraryImportData =
                ProcessLibraryImportAttribute(generatedDllImportAttr!) ??
                new LibraryImportCompilationData("INVALID_CSHARP_SYNTAX");

            // Create a diagnostics bag that discards all diagnostics.
            // Diagnostics are now reported by the analyzer, not the generator.
            var discardedDiagnostics = new GeneratorDiagnosticsBag(new DiagnosticDescriptorProvider(), locations, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.LibraryImportGenerator.Downlevel.SR));

            // Create the stub.
            var signatureContext = SignatureContext.Create(
                symbol,
                DownlevelLibraryImportGeneratorHelpers.CreateMarshallingInfoParser(environment, discardedDiagnostics, symbol, libraryImportData),
                environment,
                new CodeEmitOptions(SkipInit: false),
                typeof(DownlevelLibraryImportGenerator).Assembly);

            var containingTypeContext = new ContainingSyntaxContext(originalSyntax);

            var methodSyntaxTemplate = new ContainingSyntax(originalSyntax.Modifiers, SyntaxKind.MethodDeclaration, originalSyntax.Identifier, originalSyntax.TypeParameterList);

            List<AttributeSyntax> additionalAttributes = GenerateSyntaxForForwardedAttributes(defaultDllImportSearchPathsAttribute);
            return new IncrementalStubGenerationContext(
                signatureContext,
                containingTypeContext,
                methodSyntaxTemplate,
                locations,
                new SequenceEqualImmutableArray<AttributeSyntax>(additionalAttributes.ToImmutableArray(), SyntaxEquivalentComparer.Instance),
                LibraryImportData.From(libraryImportData),
                environment.EnvironmentFlags
                );
        }

        private static MemberDeclarationSyntax GenerateSource(
            IncrementalStubGenerationContext pinvokeStub)
        {
            // Note: Diagnostics are now reported by the analyzer, so we pass a discarding diagnostics bag
            var discardedDiagnostics = new GeneratorDiagnosticsBag(new DiagnosticDescriptorProvider(), pinvokeStub.DiagnosticLocation, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.LibraryImportGenerator.Downlevel.SR));

            // Generate stub code
            var stubGenerator = new ManagedToNativeStubGenerator(
                pinvokeStub.SignatureContext.ElementTypeInformation,
                pinvokeStub.LibraryImportData.SetLastError,
                discardedDiagnostics,
                DownlevelLibraryImportGeneratorHelpers.GeneratorResolver,
                new CodeEmitOptions(SkipInit: false));

            // Check if the generator should produce a forwarder stub - regular DllImport.
            // This is done if the signature is blittable or if some parameters cannot be marshalled.
            if (stubGenerator.NoMarshallingRequired
                || stubGenerator.HasForwardedTypes
                || pinvokeStub.LibraryImportData.SetLastError)
            {
                return PrintForwarderStub(pinvokeStub.StubMethodSyntaxTemplate, pinvokeStub);
            }

            ImmutableArray<AttributeSyntax> forwardedAttributes = pinvokeStub.ForwardedAttributes.Array;

            const string innerPInvokeName = "__PInvoke";

            BlockSyntax code = stubGenerator.GenerateStubBody(innerPInvokeName);

            LocalFunctionStatementSyntax dllImport = CreateTargetDllImportAsLocalStatement(
                stubGenerator,
                pinvokeStub.LibraryImportData,
                innerPInvokeName,
                pinvokeStub.StubMethodSyntaxTemplate.Identifier.Text);

            if (!forwardedAttributes.IsEmpty)
            {
                dllImport = dllImport.AddAttributeLists(AttributeList(SeparatedList(forwardedAttributes)));
            }

            dllImport = dllImport.WithLeadingTrivia(Comment("// Local P/Invoke"));
            code = code.AddStatements(dllImport);

            return pinvokeStub.ContainingSyntaxContext.WrapMemberInContainingSyntaxWithUnsafeModifier(PrintGeneratedSource(pinvokeStub.StubMethodSyntaxTemplate, pinvokeStub.SignatureContext, code));
        }

        private static MemberDeclarationSyntax PrintForwarderStub(ContainingSyntax userDeclaredMethod, IncrementalStubGenerationContext stub)
        {
            LibraryImportData pinvokeData = stub.LibraryImportData with { EntryPoint = stub.LibraryImportData.EntryPoint ?? userDeclaredMethod.Identifier.ValueText };

            if (pinvokeData.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshalling)
                && pinvokeData.StringMarshalling != StringMarshalling.Utf16)
            {
                pinvokeData = pinvokeData with { IsUserDefined = pinvokeData.IsUserDefined & ~InteropAttributeMember.StringMarshalling };
            }

            SyntaxTokenList modifiers = StripTriviaFromModifiers(userDeclaredMethod.Modifiers);
            modifiers = modifiers.AddToModifiers(SyntaxKind.ExternKeyword);
            // Create stub function
            MethodDeclarationSyntax stubMethod = MethodDeclaration(stub.SignatureContext.StubReturnType, userDeclaredMethod.Identifier)
                .WithModifiers(modifiers)
                .WithParameterList(ParameterList(SeparatedList(stub.SignatureContext.StubParameters)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .AddModifiers()
                .AddAttributeLists(
                    AttributeList(
                        SingletonSeparatedList(
                            CreateForwarderDllImport(pinvokeData))));

            MemberDeclarationSyntax toPrint = stub.ContainingSyntaxContext.WrapMemberInContainingSyntaxWithUnsafeModifier(stubMethod);

            return toPrint;
        }

        private static LocalFunctionStatementSyntax CreateTargetDllImportAsLocalStatement(
            ManagedToNativeStubGenerator stubGenerator,
            LibraryImportData libraryImportData,
            string stubTargetName,
            string stubMethodName)
        {
            (ParameterListSyntax parameterList, TypeSyntax returnType, AttributeListSyntax returnTypeAttributes) = stubGenerator.GenerateTargetMethodSignatureData();
            LocalFunctionStatementSyntax localDllImport = LocalFunctionStatement(returnType, stubTargetName)
                .AddModifiers(
                    Token(SyntaxKind.StaticKeyword),
                    Token(SyntaxKind.ExternKeyword),
                    Token(SyntaxKind.UnsafeKeyword))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .WithAttributeLists(
                    SingletonList(AttributeList(
                        SingletonSeparatedList(
                                Attribute(
                                    NameSyntaxes.DllImportAttribute,
                                    AttributeArgumentList(
                                        SeparatedList(
                                            [
                                                AttributeArgument(LiteralExpression(
                                                        SyntaxKind.StringLiteralExpression,
                                                        Literal(libraryImportData.ModuleName))),
                                                AttributeArgument(
                                                    NameEquals(nameof(DllImportAttribute.EntryPoint)),
                                                    null,
                                                    LiteralExpression(
                                                        SyntaxKind.StringLiteralExpression,
                                                        Literal(libraryImportData.EntryPoint ?? stubMethodName))),
                                                AttributeArgument(
                                                    NameEquals(nameof(DllImportAttribute.ExactSpelling)),
                                                    null,
                                                    LiteralExpression(SyntaxKind.TrueLiteralExpression))
                                            ]
                                            )))))))
                .WithParameterList(parameterList);
            if (returnTypeAttributes is not null)
            {
                localDllImport = localDllImport.AddAttributeLists(returnTypeAttributes.WithTarget(AttributeTargetSpecifier(Token(SyntaxKind.ReturnKeyword))));
            }
            return localDllImport;
        }

        private static AttributeSyntax CreateForwarderDllImport(LibraryImportData target)
        {
            var newAttributeArgs = new List<AttributeArgumentSyntax>
            {
                AttributeArgument(LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    Literal(target.ModuleName))),
                AttributeArgument(
                    NameEquals(nameof(DllImportAttribute.EntryPoint)),
                    null,
                    CreateStringExpressionSyntax(target.EntryPoint)),
                AttributeArgument(
                    NameEquals(nameof(DllImportAttribute.ExactSpelling)),
                    null,
                    LiteralExpression(SyntaxKind.TrueLiteralExpression))
            };

            if (target.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshalling))
            {
                Debug.Assert(target.StringMarshalling == StringMarshalling.Utf16);
                NameEqualsSyntax name = NameEquals(nameof(DllImportAttribute.CharSet));
                ExpressionSyntax value = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    AliasQualifiedName("global", IdentifierName(typeof(CharSet).FullName)),
                    IdentifierName(nameof(CharSet.Unicode)));
                newAttributeArgs.Add(AttributeArgument(name, null, value));
            }

            if (target.IsUserDefined.HasFlag(InteropAttributeMember.SetLastError))
            {
                NameEqualsSyntax name = NameEquals(nameof(DllImportAttribute.SetLastError));
                ExpressionSyntax value = CreateBoolExpressionSyntax(target.SetLastError);
                newAttributeArgs.Add(AttributeArgument(name, null, value));
            }

            // Create new attribute
            return Attribute(
                NameSyntaxes.DllImportAttribute,
                AttributeArgumentList(SeparatedList(newAttributeArgs)));

            static ExpressionSyntax CreateBoolExpressionSyntax(bool trueOrFalse)
            {
                return LiteralExpression(
                    trueOrFalse
                        ? SyntaxKind.TrueLiteralExpression
                        : SyntaxKind.FalseLiteralExpression);
            }

            static ExpressionSyntax CreateStringExpressionSyntax(string str)
            {
                return LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    Literal(str));
            }
        }

    }
}
