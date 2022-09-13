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
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

[assembly: System.Resources.NeutralResourcesLanguage("en-US")]

namespace Microsoft.Interop
{
    [Generator]
    public sealed class LibraryImportGenerator : IIncrementalGenerator
    {
        internal sealed record IncrementalStubGenerationContext(
            StubEnvironment Environment,
            SignatureContext SignatureContext,
            ContainingSyntaxContext ContainingSyntaxContext,
            ContainingSyntax StubMethodSyntaxTemplate,
            MethodSignatureDiagnosticLocations DiagnosticLocation,
            ImmutableArray<AttributeSyntax> ForwardedAttributes,
            LibraryImportData LibraryImportData,
            MarshallingGeneratorFactoryKey<(TargetFramework, Version, LibraryImportGeneratorOptions)> GeneratorFactoryKey,
            ImmutableArray<Diagnostic> Diagnostics)
        {
            public bool Equals(IncrementalStubGenerationContext? other)
            {
                return other is not null
                    && StubEnvironment.AreCompilationSettingsEqual(Environment, other.Environment)
                    && SignatureContext.Equals(other.SignatureContext)
                    && ContainingSyntaxContext.Equals(other.ContainingSyntaxContext)
                    && StubMethodSyntaxTemplate.Equals(other.StubMethodSyntaxTemplate)
                    && LibraryImportData.Equals(other.LibraryImportData)
                    && DiagnosticLocation.Equals(DiagnosticLocation)
                    && ForwardedAttributes.SequenceEqual(other.ForwardedAttributes, (IEqualityComparer<AttributeSyntax>)SyntaxEquivalentComparer.Instance)
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
            // Collect all methods adorned with LibraryImportAttribute
            var attributedMethods = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    context,
                    TypeNames.LibraryImportAttribute,
                    static (node, ct) => node is MethodDeclarationSyntax,
                    static (context, ct) => context.TargetSymbol is IMethodSymbol methodSymbol
                        ? new { Syntax = (MethodDeclarationSyntax)context.TargetNode, Symbol = methodSymbol }
                        : null)
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
            IncrementalValueProvider<LibraryImportGeneratorOptions> stubOptions = context.AnalyzerConfigOptionsProvider
                .Select(static (options, ct) => new LibraryImportGeneratorOptions(options.GlobalOptions));

            IncrementalValueProvider<StubEnvironment> stubEnvironment = context.CreateStubEnvironmentProvider();

            // Validate environment that is being used to generate stubs.
            context.RegisterDiagnostics(stubEnvironment.Combine(attributedMethods.Collect()).SelectMany((data, ct) =>
            {
                if (data.Right.IsEmpty // no attributed methods
                    || data.Left.Compilation.Options is CSharpCompilationOptions { AllowUnsafe: true } // Unsafe code enabled
                    || data.Left.TargetFramework != TargetFramework.Net) // Non-.NET 5 scenarios use forwarders and don't need unsafe code
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                return ImmutableArray.Create(Diagnostic.Create(GeneratorDiagnostics.RequiresAllowUnsafeBlocks, null));
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

            context.RegisterConcatenatedSyntaxOutputs(generateSingleStub.Select((data, ct) => data.Item1), "LibraryImports.g.cs");
        }

        private static List<AttributeSyntax> GenerateSyntaxForForwardedAttributes(AttributeData? suppressGCTransitionAttribute, AttributeData? unmanagedCallConvAttribute, AttributeData? defaultDllImportSearchPathsAttribute)
        {
            const string CallConvsField = "CallConvs";
            // Manually rehydrate the forwarded attributes with fully qualified types so we don't have to worry about any using directives.
            List<AttributeSyntax> attributes = new();

            if (suppressGCTransitionAttribute is not null)
            {
                attributes.Add(Attribute(ParseName(TypeNames.SuppressGCTransitionAttribute)));
            }
            if (unmanagedCallConvAttribute is not null)
            {
                AttributeSyntax unmanagedCallConvSyntax = Attribute(ParseName(TypeNames.UnmanagedCallConvAttribute));
                foreach (KeyValuePair<string, TypedConstant> arg in unmanagedCallConvAttribute.NamedArguments)
                {
                    if (arg.Key == CallConvsField)
                    {
                        InitializerExpressionSyntax callConvs = InitializerExpression(SyntaxKind.ArrayInitializerExpression);
                        foreach (TypedConstant callConv in arg.Value.Values)
                        {
                            callConvs = callConvs.AddExpressions(
                                TypeOfExpression(((ITypeSymbol)callConv.Value!).AsTypeSyntax()));
                        }

                        ArrayTypeSyntax arrayOfSystemType = ArrayType(ParseTypeName(TypeNames.System_Type), SingletonList(ArrayRankSpecifier()));

                        unmanagedCallConvSyntax = unmanagedCallConvSyntax.AddArgumentListArguments(
                            AttributeArgument(
                                ArrayCreationExpression(arrayOfSystemType)
                                .WithInitializer(callConvs))
                            .WithNameEquals(NameEquals(IdentifierName(CallConvsField))));
                    }
                }
                attributes.Add(unmanagedCallConvSyntax);
            }
            if (defaultDllImportSearchPathsAttribute is not null)
            {
                attributes.Add(
                    Attribute(ParseName(TypeNames.DefaultDllImportSearchPathsAttribute)).AddArgumentListArguments(
                        AttributeArgument(
                            CastExpression(ParseTypeName(TypeNames.DllImportSearchPath),
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

        private static MemberDeclarationSyntax PrintGeneratedSource(
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

        private static LibraryImportData? ProcessLibraryImportAttribute(AttributeData attrData)
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
            if (namedArguments.TryGetValue(nameof(LibraryImportData.EntryPoint), out TypedConstant entryPointValue))
            {
                if (entryPointValue.Value is not string)
                {
                    return null;
                }
                entryPoint = (string)entryPointValue.Value!;
            }

            return new LibraryImportData(attrData.ConstructorArguments[0].Value!.ToString())
            {
                EntryPoint = entryPoint,
            }.WithValuesFromNamedArguments(namedArguments);
        }

        private static IncrementalStubGenerationContext CalculateStubInformation(
            MethodDeclarationSyntax originalSyntax,
            IMethodSymbol symbol,
            StubEnvironment environment,
            LibraryImportGeneratorOptions options,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            INamedTypeSymbol? lcidConversionAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.LCIDConversionAttribute);
            INamedTypeSymbol? suppressGCTransitionAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.SuppressGCTransitionAttribute);
            INamedTypeSymbol? unmanagedCallConvAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.UnmanagedCallConvAttribute);
            INamedTypeSymbol? defaultDllImportSearchPathsAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.DefaultDllImportSearchPathsAttribute);
            // Get any attributes of interest on the method
            AttributeData? generatedDllImportAttr = null;
            AttributeData? lcidConversionAttr = null;
            AttributeData? suppressGCTransitionAttribute = null;
            AttributeData? unmanagedCallConvAttribute = null;
            AttributeData? defaultDllImportSearchPathsAttribute = null;
            foreach (AttributeData attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass is not null
                    && attr.AttributeClass.ToDisplayString() == TypeNames.LibraryImportAttribute)
                {
                    generatedDllImportAttr = attr;
                }
                else if (lcidConversionAttrType is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, lcidConversionAttrType))
                {
                    lcidConversionAttr = attr;
                }
                else if (suppressGCTransitionAttrType is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, suppressGCTransitionAttrType))
                {
                    suppressGCTransitionAttribute = attr;
                }
                else if (unmanagedCallConvAttrType is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, unmanagedCallConvAttrType))
                {
                    unmanagedCallConvAttribute = attr;
                }
                else if (defaultDllImportSearchPathsAttrType is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, defaultDllImportSearchPathsAttrType))
                {
                    defaultDllImportSearchPathsAttribute = attr;
                }
            }

            Debug.Assert(generatedDllImportAttr is not null);

            var generatorDiagnostics = new GeneratorDiagnostics();

            // Process the LibraryImport attribute
            LibraryImportData libraryImportData =
                ProcessLibraryImportAttribute(generatedDllImportAttr!) ??
                new LibraryImportData("INVALID_CSHARP_SYNTAX");

            if (libraryImportData.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshalling))
            {
                // User specified StringMarshalling.Custom without specifying StringMarshallingCustomType
                if (libraryImportData.StringMarshalling == StringMarshalling.Custom && libraryImportData.StringMarshallingCustomType is null)
                {
                    generatorDiagnostics.ReportInvalidStringMarshallingConfiguration(
                        generatedDllImportAttr, symbol.Name, SR.InvalidStringMarshallingConfigurationMissingCustomType);
                }

                // User specified something other than StringMarshalling.Custom while specifying StringMarshallingCustomType
                if (libraryImportData.StringMarshalling != StringMarshalling.Custom && libraryImportData.StringMarshallingCustomType is not null)
                {
                    generatorDiagnostics.ReportInvalidStringMarshallingConfiguration(
                        generatedDllImportAttr, symbol.Name, SR.InvalidStringMarshallingConfigurationNotCustom);
                }
            }

            if (lcidConversionAttr is not null)
            {
                // Using LCIDConversion with LibraryImport is not supported
                generatorDiagnostics.ReportConfigurationNotSupported(lcidConversionAttr, nameof(TypeNames.LCIDConversionAttribute));
            }

            // Create the stub.
            var signatureContext = SignatureContext.Create(symbol, libraryImportData, environment, generatorDiagnostics, typeof(LibraryImportGenerator).Assembly);

            var containingTypeContext = new ContainingSyntaxContext(originalSyntax);

            var methodSyntaxTemplate = new ContainingSyntax(originalSyntax.Modifiers.StripTriviaFromTokens(), SyntaxKind.MethodDeclaration, originalSyntax.Identifier, originalSyntax.TypeParameterList);

            List<AttributeSyntax> additionalAttributes = GenerateSyntaxForForwardedAttributes(suppressGCTransitionAttribute, unmanagedCallConvAttribute, defaultDllImportSearchPathsAttribute);
            return new IncrementalStubGenerationContext(
                environment,
                signatureContext,
                containingTypeContext,
                methodSyntaxTemplate,
                new MethodSignatureDiagnosticLocations(originalSyntax),
                additionalAttributes.ToImmutableArray(),
                libraryImportData,
                LibraryImportGeneratorHelpers.CreateGeneratorFactory(environment, options),
                generatorDiagnostics.Diagnostics.ToImmutableArray());
        }

        private static (MemberDeclarationSyntax, ImmutableArray<Diagnostic>) GenerateSource(
            IncrementalStubGenerationContext pinvokeStub,
            LibraryImportGeneratorOptions options)
        {
            var diagnostics = new GeneratorDiagnostics();
            if (options.GenerateForwarders)
            {
                return (PrintForwarderStub(pinvokeStub.StubMethodSyntaxTemplate, explicitForwarding: true, pinvokeStub, diagnostics), pinvokeStub.Diagnostics.AddRange(diagnostics.Diagnostics));
            }

            // Generate stub code
            var stubGenerator = new PInvokeStubCodeGenerator(
                pinvokeStub.Environment,
                pinvokeStub.SignatureContext.ElementTypeInformation,
                pinvokeStub.LibraryImportData.SetLastError && !options.GenerateForwarders,
                (elementInfo, ex) =>
                {
                    diagnostics.ReportMarshallingNotSupported(pinvokeStub.DiagnosticLocation, elementInfo, ex.NotSupportedDetails, ex.DiagnosticProperties ?? ImmutableDictionary<string, string>.Empty);
                },
                pinvokeStub.GeneratorFactoryKey.GeneratorFactory);

            // Check if the generator should produce a forwarder stub - regular DllImport.
            // This is done if the signature is blittable or the target framework is not supported.
            if (stubGenerator.StubIsBasicForwarder
                || !stubGenerator.SupportsTargetFramework)
            {
                return (PrintForwarderStub(pinvokeStub.StubMethodSyntaxTemplate, !stubGenerator.SupportsTargetFramework, pinvokeStub, diagnostics), pinvokeStub.Diagnostics.AddRange(diagnostics.Diagnostics));
            }

            ImmutableArray<AttributeSyntax> forwardedAttributes = pinvokeStub.ForwardedAttributes;

            const string innerPInvokeName = "__PInvoke";

            BlockSyntax code = stubGenerator.GeneratePInvokeBody(innerPInvokeName);

            LocalFunctionStatementSyntax dllImport = CreateTargetDllImportAsLocalStatement(
                stubGenerator,
                options,
                pinvokeStub.LibraryImportData,
                innerPInvokeName,
                pinvokeStub.StubMethodSyntaxTemplate.Identifier.Text);

            if (!forwardedAttributes.IsEmpty)
            {
                dllImport = dllImport.AddAttributeLists(AttributeList(SeparatedList(forwardedAttributes)));
            }

            dllImport = dllImport.WithLeadingTrivia(Comment("// Local P/Invoke"));
            code = code.AddStatements(dllImport);

            return (pinvokeStub.ContainingSyntaxContext.WrapMemberInContainingSyntaxWithUnsafeModifier(PrintGeneratedSource(pinvokeStub.StubMethodSyntaxTemplate, pinvokeStub.SignatureContext, code)), pinvokeStub.Diagnostics.AddRange(diagnostics.Diagnostics));
        }

        private static MemberDeclarationSyntax PrintForwarderStub(ContainingSyntax userDeclaredMethod, bool explicitForwarding, IncrementalStubGenerationContext stub, GeneratorDiagnostics diagnostics)
        {
            LibraryImportData pinvokeData = stub.LibraryImportData with { EntryPoint = stub.LibraryImportData.EntryPoint ?? userDeclaredMethod.Identifier.ValueText };

            if (pinvokeData.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshalling)
                && pinvokeData.StringMarshalling != StringMarshalling.Utf16)
            {
                // Report a diagnostic when forwarding explicitly due to generator options or down-level support. Otherwise, StringMarshalling can just be omitted
                if (explicitForwarding)
                {
                    diagnostics.ReportCannotForwardToDllImport(
                        stub.DiagnosticLocation,
                        $"{nameof(TypeNames.LibraryImportAttribute)}{Type.Delimiter}{nameof(StringMarshalling)}",
                        $"{nameof(StringMarshalling)}{Type.Delimiter}{pinvokeData.StringMarshalling}");
                }

                pinvokeData = pinvokeData with { IsUserDefined = pinvokeData.IsUserDefined & ~InteropAttributeMember.StringMarshalling };
            }

            if (pinvokeData.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshallingCustomType))
            {
                // Report a diagnostic when forwarding explicitly due to generator options or down-level support. Otherwise, StringMarshallingCustomType can just be omitted
                if (explicitForwarding)
                {
                    diagnostics.ReportCannotForwardToDllImport(
                        stub.DiagnosticLocation,
                        $"{nameof(TypeNames.LibraryImportAttribute)}{Type.Delimiter}{nameof(InteropAttributeMember.StringMarshallingCustomType)}");
                }

                pinvokeData = pinvokeData with { IsUserDefined = pinvokeData.IsUserDefined & ~InteropAttributeMember.StringMarshallingCustomType };
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
            PInvokeStubCodeGenerator stubGenerator,
            LibraryImportGeneratorOptions options,
            LibraryImportData libraryImportData,
            string stubTargetName,
            string stubMethodName)
        {
            Debug.Assert(!options.GenerateForwarders, "GenerateForwarders should have already been handled to use a forwarder stub");

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
                                ParseName(typeof(DllImportAttribute).FullName),
                                AttributeArgumentList(
                                    SeparatedList(
                                        new[]
                                        {
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
                                        }
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
                ExpressionSyntax value = CreateEnumExpressionSyntax(CharSet.Unicode);
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
                ParseName(typeof(DllImportAttribute).FullName),
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

            static ExpressionSyntax CreateEnumExpressionSyntax<T>(T value) where T : Enum
            {
                return MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(typeof(T).FullName),
                    IdentifierName(value.ToString()));
            }
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
                return Diagnostic.Create(GeneratorDiagnostics.InvalidAttributedMethodSignature, methodSyntax.Identifier.GetLocation(), method.Name);
            }

            // Verify that the types the method is declared in are marked partial.
            for (SyntaxNode? parentNode = methodSyntax.Parent; parentNode is TypeDeclarationSyntax typeDecl; parentNode = parentNode.Parent)
            {
                if (!typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return Diagnostic.Create(GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingModifiers, methodSyntax.Identifier.GetLocation(), method.Name, typeDecl.Identifier);
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
