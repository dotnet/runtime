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
    public sealed class LibraryImportGenerator : IIncrementalGenerator
    {
        internal sealed record IncrementalStubGenerationContext(
            SignatureContext SignatureContext,
            ContainingSyntaxContext ContainingSyntaxContext,
            ContainingSyntax StubMethodSyntaxTemplate,
            MethodSignatureDiagnosticLocations DiagnosticLocation,
            SequenceEqualImmutableArray<AttributeSyntax> ForwardedAttributes,
            LibraryImportData LibraryImportData,
            TargetFrameworkSettings TargetFramework,
            LibraryImportGeneratorOptions Options,
            EnvironmentFlags EnvironmentFlags,
            SequenceEqualImmutableArray<DiagnosticInfo> Diagnostics);

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
                DiagnosticInfo? diagnostic = GetDiagnosticIfInvalidMethodForGeneration(data.Syntax, data.Symbol);
                return diagnostic is not null
                    ? DiagnosticOr<(MethodDeclarationSyntax Syntax, IMethodSymbol Symbol)>.From(diagnostic)
                    : DiagnosticOr<(MethodDeclarationSyntax Syntax, IMethodSymbol Symbol)>.From((data.Syntax, data.Symbol));
            });

            var methodsToGenerate = context.FilterAndReportDiagnostics(methodsWithDiagnostics);

            // Compute generator options
            IncrementalValueProvider<LibraryImportGeneratorOptions> stubOptions = context.AnalyzerConfigOptionsProvider
                .Select(static (options, ct) => new LibraryImportGeneratorOptions(options.GlobalOptions));

            IncrementalValueProvider<TargetFrameworkSettings> targetFramework = context.AnalyzerConfigOptionsProvider.Select((options, ct) => options.GlobalOptions.GetTargetFrameworkSettings());
            IncrementalValueProvider<StubEnvironment> stubEnvironment = context.CreateStubEnvironmentProvider();

            // Validate environment that is being used to generate stubs.
            context.RegisterDiagnostics(context.CompilationProvider.Combine(attributedMethods.Collect()).Combine(targetFramework).SelectMany((data, ct) =>
            {
                if (data.Left.Right.IsEmpty // no attributed methods
                    || data.Left.Left.Options is CSharpCompilationOptions { AllowUnsafe: true } // Unsafe code enabled
                    || data.Right.TargetFramework != TargetFramework.Net) // Downlevel scenarios use forwarders and don't need unsafe code
                {
                    return ImmutableArray<DiagnosticInfo>.Empty;
                }

                return ImmutableArray.Create(DiagnosticInfo.Create(GeneratorDiagnostics.RequiresAllowUnsafeBlocks, null));
            }));

            IncrementalValuesProvider<(MemberDeclarationSyntax, ImmutableArray<DiagnosticInfo>)> generateSingleStub = methodsToGenerate
                .Combine(stubEnvironment)
                .Combine(stubOptions)
                .Combine(targetFramework)
                .Select(static (data, ct) => new
                {
                    data.Left.Left.Left.Syntax,
                    data.Left.Left.Left.Symbol,
                    Environment = data.Left.Left.Right,
                    Options = data.Left.Right,
                    TargetFramework = data.Right
                })
                .Select(
                    static (data, ct) => CalculateStubInformation(data.Syntax, data.Symbol, data.Environment, data.TargetFramework, data.Options, ct)
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
                attributes.Add(Attribute(NameSyntaxes.SuppressGCTransitionAttribute));
            }
            if (unmanagedCallConvAttribute is not null)
            {
                AttributeSyntax unmanagedCallConvSyntax = Attribute(NameSyntaxes.UnmanagedCallConvAttribute);
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

                        ArrayTypeSyntax arrayOfSystemType = ArrayType(TypeSyntaxes.System_Type, SingletonList(ArrayRankSpecifier()));

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
            TargetFrameworkSettings targetFramework,
            LibraryImportGeneratorOptions options,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            INamedTypeSymbol? lcidConversionAttrType = environment.LcidConversionAttrType;
            INamedTypeSymbol? suppressGCTransitionAttrType = environment.SuppressGCTransitionAttrType;
            INamedTypeSymbol? unmanagedCallConvAttrType = environment.UnmanagedCallConvAttrType;
            INamedTypeSymbol? defaultDllImportSearchPathsAttrType = environment.DefaultDllImportSearchPathsAttrType;
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

            var locations = new MethodSignatureDiagnosticLocations(originalSyntax);
            var generatorDiagnostics = new GeneratorDiagnosticsBag(new DiagnosticDescriptorProvider(), locations, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.LibraryImportGenerator.SR));

            // Process the LibraryImport attribute
            LibraryImportCompilationData libraryImportData =
                ProcessLibraryImportAttribute(generatedDllImportAttr!) ??
                new LibraryImportCompilationData("INVALID_CSHARP_SYNTAX");

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
            var signatureContext = SignatureContext.Create(
                symbol,
                LibraryImportGeneratorHelpers.CreateMarshallingInfoParser(environment, targetFramework, generatorDiagnostics, symbol, libraryImportData, generatedDllImportAttr),
                environment,
                new CodeEmitOptions(SkipInit: targetFramework.TargetFramework == TargetFramework.Net),
                typeof(LibraryImportGenerator).Assembly);

            var containingTypeContext = new ContainingSyntaxContext(originalSyntax);

            var methodSyntaxTemplate = new ContainingSyntax(originalSyntax.Modifiers, SyntaxKind.MethodDeclaration, originalSyntax.Identifier, originalSyntax.TypeParameterList);

            List<AttributeSyntax> additionalAttributes = GenerateSyntaxForForwardedAttributes(suppressGCTransitionAttribute, unmanagedCallConvAttribute, defaultDllImportSearchPathsAttribute);
            return new IncrementalStubGenerationContext(
                signatureContext,
                containingTypeContext,
                methodSyntaxTemplate,
                locations,
                new SequenceEqualImmutableArray<AttributeSyntax>(additionalAttributes.ToImmutableArray(), SyntaxEquivalentComparer.Instance),
                LibraryImportData.From(libraryImportData),
                targetFramework,
                options,
                environment.EnvironmentFlags,
                new SequenceEqualImmutableArray<DiagnosticInfo>(generatorDiagnostics.Diagnostics.ToImmutableArray())
                );
        }

        private static (MemberDeclarationSyntax, ImmutableArray<DiagnosticInfo>) GenerateSource(
            IncrementalStubGenerationContext pinvokeStub,
            LibraryImportGeneratorOptions options)
        {
            var diagnostics = new GeneratorDiagnosticsBag(new DiagnosticDescriptorProvider(), pinvokeStub.DiagnosticLocation, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.LibraryImportGenerator.SR));
            if (options.GenerateForwarders)
            {
                return (PrintForwarderStub(pinvokeStub.StubMethodSyntaxTemplate, explicitForwarding: true, pinvokeStub, diagnostics), pinvokeStub.Diagnostics.Array.AddRange(diagnostics.Diagnostics));
            }

            bool supportsTargetFramework = !pinvokeStub.LibraryImportData.SetLastError
                || options.GenerateForwarders
                || (pinvokeStub.TargetFramework is (TargetFramework.Net, { Major: >= 6 }));

            foreach (TypePositionInfo typeInfo in pinvokeStub.SignatureContext.ElementTypeInformation)
            {
                if (typeInfo.MarshallingAttributeInfo is MissingSupportMarshallingInfo)
                {
                    supportsTargetFramework = false;
                    break;
                }
            }

            // Generate stub code
            var stubGenerator = new PInvokeStubCodeGenerator(
                pinvokeStub.SignatureContext.ElementTypeInformation,
                pinvokeStub.LibraryImportData.SetLastError && !options.GenerateForwarders,
                diagnostics,
                LibraryImportGeneratorHelpers.CreateGeneratorResolver(pinvokeStub.TargetFramework, pinvokeStub.Options, pinvokeStub.EnvironmentFlags),
                new CodeEmitOptions(SkipInit: pinvokeStub.TargetFramework is (TargetFramework.Net, _)));

            // Check if the generator should produce a forwarder stub - regular DllImport.
            // This is done if the signature is blittable or the target framework is not supported.
            if (stubGenerator.StubIsBasicForwarder
                || !supportsTargetFramework)
            {
                return (PrintForwarderStub(pinvokeStub.StubMethodSyntaxTemplate, !supportsTargetFramework, pinvokeStub, diagnostics), pinvokeStub.Diagnostics.Array.AddRange(diagnostics.Diagnostics));
            }

            ImmutableArray<AttributeSyntax> forwardedAttributes = pinvokeStub.ForwardedAttributes.Array;

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

            return (pinvokeStub.ContainingSyntaxContext.WrapMemberInContainingSyntaxWithUnsafeModifier(PrintGeneratedSource(pinvokeStub.StubMethodSyntaxTemplate, pinvokeStub.SignatureContext, code)), pinvokeStub.Diagnostics.Array.AddRange(diagnostics.Diagnostics));
        }

        private static MemberDeclarationSyntax PrintForwarderStub(ContainingSyntax userDeclaredMethod, bool explicitForwarding, IncrementalStubGenerationContext stub, GeneratorDiagnosticsBag diagnostics)
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
                                    NameSyntaxes.DllImportAttribute,
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

            static ExpressionSyntax CreateEnumExpressionSyntax<T>(T value) where T : Enum
            {
                return MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(typeof(T).FullName),
                    IdentifierName(value.ToString()));
            }
        }

        private static DiagnosticInfo? GetDiagnosticIfInvalidMethodForGeneration(MethodDeclarationSyntax methodSyntax, IMethodSymbol method)
        {
            // Verify the method has no generic types or defined implementation
            // and is marked static and partial.
            if (methodSyntax.TypeParameterList is not null
                || methodSyntax.Body is not null
                || !methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
                || !methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.InvalidAttributedMethodSignature, methodSyntax.Identifier.GetLocation(), method.Name);
            }

            // Verify that the types the method is declared in are marked partial.
            if (methodSyntax.Parent is TypeDeclarationSyntax typeDecl && !typeDecl.IsInPartialContext(out var nonPartialIdentifier))
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingModifiers, methodSyntax.Identifier.GetLocation(), method.Name, nonPartialIdentifier);
            }

            // Verify the method does not have a ref return
            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.ReturnConfigurationNotSupported, methodSyntax.Identifier.GetLocation(), "ref return", method.ToDisplayString());
            }

            return null;
        }
    }
}
