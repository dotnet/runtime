// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

[assembly: System.Resources.NeutralResourcesLanguage("en-US")]

namespace Microsoft.Interop
{
    [Generator]
    public sealed class VtableIndexStubGenerator : IIncrementalGenerator
    {
        internal sealed record IncrementalStubGenerationContext(
            SignatureContext SignatureContext,
            ContainingSyntaxContext ContainingSyntaxContext,
            ContainingSyntax StubMethodSyntaxTemplate,
            MethodSignatureDiagnosticLocations DiagnosticLocation,
            SequenceEqualImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> CallingConvention,
            VirtualMethodIndexData VtableIndexData,
            MarshallingInfo ExceptionMarshallingInfo,
            MarshallingGeneratorFactoryKey<(TargetFramework TargetFramework, Version TargetFrameworkVersion)> ManagedToUnmanagedGeneratorFactory,
            MarshallingGeneratorFactoryKey<(TargetFramework TargetFramework, Version TargetFrameworkVersion)> UnmanagedToManagedGeneratorFactory,
            ManagedTypeInfo TypeKeyType,
            ManagedTypeInfo TypeKeyOwner,
            SequenceEqualImmutableArray<Diagnostic> Diagnostics);

        public static class StepNames
        {
            public const string CalculateStubInformation = nameof(CalculateStubInformation);
            public const string GenerateManagedToNativeStub = nameof(GenerateManagedToNativeStub);
            public const string GenerateNativeToManagedStub = nameof(GenerateNativeToManagedStub);
        }

        private static readonly ContainingSyntax NativeTypeContainingSyntax = new(
                                    TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.PartialKeyword)),
                                    SyntaxKind.InterfaceDeclaration,
                                    Identifier("Native"),
                                    null);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Get all methods with the [VirtualMethodIndex] attribute.
            var attributedMethods = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    TypeNames.VirtualMethodIndexAttribute,
                    static (node, ct) => node is MethodDeclarationSyntax,
                    static (context, ct) => context.TargetSymbol is IMethodSymbol methodSymbol
                        ? new { Syntax = (MethodDeclarationSyntax)context.TargetNode, Symbol = methodSymbol }
                        : null)
                .Where(
                    static modelData => modelData is not null);

            var methodsWithDiagnostics = attributedMethods.Select(static (data, ct) =>
            {
                Diagnostic? diagnostic = GetDiagnosticIfInvalidMethodForGeneration(data.Syntax, data.Symbol);
                return new { data.Syntax, data.Symbol, Diagnostic = diagnostic };
            });

            // Split the methods we want to generate and the ones we don't into two separate groups.
            var methodsToGenerate = methodsWithDiagnostics.Where(static data => data.Diagnostic is null);
            var invalidMethodDiagnostics = methodsWithDiagnostics.Where(static data => data.Diagnostic is not null);

            context.RegisterSourceOutput(invalidMethodDiagnostics, static (context, invalidMethod) =>
            {
                context.ReportDiagnostic(invalidMethod.Diagnostic);
            });

            // Calculate all of information to generate both managed-to-unmanaged and unmanaged-to-managed stubs
            // for each method.
            IncrementalValuesProvider<IncrementalStubGenerationContext> generateStubInformation = methodsToGenerate
                .Combine(context.CreateStubEnvironmentProvider())
                .Select(static (data, ct) => new
                {
                    data.Left.Syntax,
                    data.Left.Symbol,
                    Environment = data.Right
                })
                .Select(
                    static (data, ct) => CalculateStubInformation(data.Syntax, data.Symbol, data.Environment, ct)
                )
                .WithTrackingName(StepNames.CalculateStubInformation);

            // Generate the code for the managed-to-unmangaed stubs and the diagnostics from code-generation.
            IncrementalValuesProvider<(MemberDeclarationSyntax, ImmutableArray<Diagnostic>)> generateManagedToNativeStub = generateStubInformation
                .Where(data => data.VtableIndexData.Direction is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                .Select(
                    static (data, ct) => GenerateManagedToNativeStub(data)
                )
                .WithComparer(Comparers.GeneratedSyntax)
                .WithTrackingName(StepNames.GenerateManagedToNativeStub);

            context.RegisterDiagnostics(generateManagedToNativeStub.SelectMany((stubInfo, ct) => stubInfo.Item2));

            context.RegisterConcatenatedSyntaxOutputs(generateManagedToNativeStub.Select((data, ct) => data.Item1), "ManagedToNativeStubs.g.cs");

            // Filter the list of all stubs to only the stubs that requested unmanaged-to-managed stub generation.
            IncrementalValuesProvider<IncrementalStubGenerationContext> nativeToManagedStubContexts =
                generateStubInformation
                .Where(data => data.VtableIndexData.Direction is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional);

            // Generate the code for the unmanaged-to-managed stubs and the diagnostics from code-generation.
            IncrementalValuesProvider<(MemberDeclarationSyntax, ImmutableArray<Diagnostic>)> generateNativeToManagedStub = nativeToManagedStubContexts
                .Select(
                    static (data, ct) => GenerateNativeToManagedStub(data)
                )
                .WithComparer(Comparers.GeneratedSyntax)
                .WithTrackingName(StepNames.GenerateNativeToManagedStub);

            context.RegisterDiagnostics(generateNativeToManagedStub.SelectMany((stubInfo, ct) => stubInfo.Item2));

            context.RegisterConcatenatedSyntaxOutputs(generateNativeToManagedStub.Select((data, ct) => data.Item1), "NativeToManagedStubs.g.cs");

            // Generate the native interface metadata for each interface that contains a method with the [VirtualMethodIndex] attribute.
            IncrementalValuesProvider<MemberDeclarationSyntax> generateNativeInterface = generateStubInformation
                .Select(static (context, ct) => context.ContainingSyntaxContext)
                .Collect()
                .SelectMany(static (syntaxContexts, ct) => syntaxContexts.Distinct())
                .Select(static (context, ct) => GenerateNativeInterfaceMetadata(context));

            context.RegisterConcatenatedSyntaxOutputs(generateNativeInterface, "NativeInterfaces.g.cs");

            // Generate a method named PopulateUnmanagedVirtualMethodTable on the native interface implementation
            // that fills in a span with the addresses of the unmanaged-to-managed stub functions at their correct
            // indices.
            IncrementalValuesProvider<MemberDeclarationSyntax> populateVTable =
                nativeToManagedStubContexts
                .Collect()
                .SelectMany(static (data, ct) => data.GroupBy(stub => stub.ContainingSyntaxContext))
                .Select(static (vtable, ct) => GeneratePopulateVTableMethod(vtable));

            context.RegisterConcatenatedSyntaxOutputs(populateVTable, "PopulateVTable.g.cs");
        }

        private static ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> GenerateCallConvSyntaxFromAttributes(AttributeData? suppressGCTransitionAttribute, AttributeData? unmanagedCallConvAttribute)
        {
            const string CallConvsField = "CallConvs";
            ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax>.Builder callingConventions = ImmutableArray.CreateBuilder<FunctionPointerUnmanagedCallingConventionSyntax>();

            if (suppressGCTransitionAttribute is not null)
            {
                callingConventions.Add(FunctionPointerUnmanagedCallingConvention(Identifier("SuppressGCTransition")));
            }
            if (unmanagedCallConvAttribute is not null)
            {
                foreach (KeyValuePair<string, TypedConstant> arg in unmanagedCallConvAttribute.NamedArguments)
                {
                    if (arg.Key == CallConvsField)
                    {
                        foreach (TypedConstant callConv in arg.Value.Values)
                        {
                            ITypeSymbol callConvSymbol = (ITypeSymbol)callConv.Value!;
                            if (callConvSymbol.Name.StartsWith("CallConv", StringComparison.Ordinal))
                            {
                                callingConventions.Add(FunctionPointerUnmanagedCallingConvention(Identifier(callConvSymbol.Name.Substring("CallConv".Length))));
                            }
                        }
                    }
                }
            }
            return callingConventions.ToImmutable();
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
            ContainingSyntax stubMethodSyntax,
            SignatureContext stub,
            BlockSyntax stubCode)
        {
            // Create stub function
            return MethodDeclaration(stub.StubReturnType, stubMethodSyntax.Identifier)
                .AddAttributeLists(stub.AdditionalAttributes.ToArray())
                .WithModifiers(StripTriviaFromModifiers(stubMethodSyntax.Modifiers))
                .WithParameterList(ParameterList(SeparatedList(stub.StubParameters)))
                .WithBody(stubCode);
        }

        private static VirtualMethodIndexCompilationData? ProcessVirtualMethodIndexAttribute(AttributeData attrData)
        {
            // Found the attribute, but it has an error so report the error.
            // This is most likely an issue with targeting an incorrect TFM.
            if (attrData.AttributeClass?.TypeKind is null or TypeKind.Error)
            {
                return null;
            }

            var namedArguments = ImmutableDictionary.CreateRange(attrData.NamedArguments);

            if (attrData.ConstructorArguments.Length == 0 || attrData.ConstructorArguments[0].Value is not int)
            {
                return null;
            }

            MarshalDirection direction = MarshalDirection.Bidirectional;
            bool implicitThis = true;
            bool exceptionMarshallingDefined = false;
            ExceptionMarshalling exceptionMarshalling = ExceptionMarshalling.Custom;
            INamedTypeSymbol? exceptionMarshallingCustomType = null;
            if (namedArguments.TryGetValue(nameof(VirtualMethodIndexCompilationData.Direction), out TypedConstant directionValue))
            {
                // TypedConstant's Value property only contains primitive values.
                if (directionValue.Value is not int)
                {
                    return null;
                }
                // A boxed primitive can be unboxed to an enum with the same underlying type.
                direction = (MarshalDirection)directionValue.Value!;
            }
            if (namedArguments.TryGetValue(nameof(VirtualMethodIndexCompilationData.ImplicitThisParameter), out TypedConstant implicitThisValue))
            {
                if (implicitThisValue.Value is not bool)
                {
                    return null;
                }
                implicitThis = (bool)implicitThisValue.Value!;
            }
            if (namedArguments.TryGetValue(nameof(VirtualMethodIndexCompilationData.ExceptionMarshalling), out TypedConstant exceptionMarshallingValue))
            {
                exceptionMarshallingDefined = true;
                // TypedConstant's Value property only contains primitive values.
                if (exceptionMarshallingValue.Value is not int)
                {
                    return null;
                }
                // A boxed primitive can be unboxed to an enum with the same underlying type.
                exceptionMarshalling = (ExceptionMarshalling)exceptionMarshallingValue.Value!;
            }
            if (namedArguments.TryGetValue(nameof(VirtualMethodIndexCompilationData.ExceptionMarshallingCustomType), out TypedConstant exceptionMarshallingCustomTypeValue))
            {
                if (exceptionMarshallingCustomTypeValue.Value is not INamedTypeSymbol)
                {
                    return null;
                }
                exceptionMarshallingCustomType = (INamedTypeSymbol)exceptionMarshallingCustomTypeValue.Value;
            }

            return new VirtualMethodIndexCompilationData((int)attrData.ConstructorArguments[0].Value).WithValuesFromNamedArguments(namedArguments) with
            {
                Direction = direction,
                ImplicitThisParameter = implicitThis,
                ExceptionMarshallingDefined = exceptionMarshallingDefined,
                ExceptionMarshalling = exceptionMarshalling,
                ExceptionMarshallingCustomType = exceptionMarshallingCustomType,
            };
        }

        private static IncrementalStubGenerationContext CalculateStubInformation(MethodDeclarationSyntax syntax, IMethodSymbol symbol, StubEnvironment environment, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            INamedTypeSymbol? lcidConversionAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.LCIDConversionAttribute);
            INamedTypeSymbol? suppressGCTransitionAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.SuppressGCTransitionAttribute);
            INamedTypeSymbol? unmanagedCallConvAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.UnmanagedCallConvAttribute);
            INamedTypeSymbol iUnmanagedInterfaceTypeType = environment.Compilation.GetTypeByMetadataName(TypeNames.IUnmanagedInterfaceType_Metadata)!;
            // Get any attributes of interest on the method
            AttributeData? virtualMethodIndexAttr = null;
            AttributeData? lcidConversionAttr = null;
            AttributeData? suppressGCTransitionAttribute = null;
            AttributeData? unmanagedCallConvAttribute = null;
            foreach (AttributeData attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass is not null
                    && attr.AttributeClass.ToDisplayString() == TypeNames.VirtualMethodIndexAttribute)
                {
                    virtualMethodIndexAttr = attr;
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
            }

            Debug.Assert(virtualMethodIndexAttr is not null);

            var generatorDiagnostics = new GeneratorDiagnostics();

            // Process the LibraryImport attribute
            VirtualMethodIndexCompilationData? virtualMethodIndexData = ProcessVirtualMethodIndexAttribute(virtualMethodIndexAttr!);

            if (virtualMethodIndexData is null)
            {
                virtualMethodIndexData = new VirtualMethodIndexCompilationData(-1);
            }
            else if (virtualMethodIndexData.Index < 0)
            {
                // Report missing or invalid index
            }

            if (virtualMethodIndexData.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshalling))
            {
                // User specified StringMarshalling.Custom without specifying StringMarshallingCustomType
                if (virtualMethodIndexData.StringMarshalling == StringMarshalling.Custom && virtualMethodIndexData.StringMarshallingCustomType is null)
                {
                    generatorDiagnostics.ReportInvalidStringMarshallingConfiguration(
                        virtualMethodIndexAttr, symbol.Name, SR.InvalidStringMarshallingConfigurationMissingCustomType);
                }

                // User specified something other than StringMarshalling.Custom while specifying StringMarshallingCustomType
                if (virtualMethodIndexData.StringMarshalling != StringMarshalling.Custom && virtualMethodIndexData.StringMarshallingCustomType is not null)
                {
                    generatorDiagnostics.ReportInvalidStringMarshallingConfiguration(
                        virtualMethodIndexAttr, symbol.Name, SR.InvalidStringMarshallingConfigurationNotCustom);
                }
            }

            if (!virtualMethodIndexData.ImplicitThisParameter && virtualMethodIndexData.Direction is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
            {
                // Report invalid configuration
            }

            if (lcidConversionAttr is not null)
            {
                // Using LCIDConversion with source-generated interop is not supported
                generatorDiagnostics.ReportConfigurationNotSupported(lcidConversionAttr, nameof(TypeNames.LCIDConversionAttribute));
            }

            // Create the stub.
            var signatureContext = SignatureContext.Create(symbol, DefaultMarshallingInfoParser.Create(environment, generatorDiagnostics, symbol, virtualMethodIndexData, virtualMethodIndexAttr), environment, typeof(VtableIndexStubGenerator).Assembly);

            var containingSyntaxContext = new ContainingSyntaxContext(syntax);

            var methodSyntaxTemplate = new ContainingSyntax(syntax.Modifiers.StripTriviaFromTokens(), SyntaxKind.MethodDeclaration, syntax.Identifier, syntax.TypeParameterList);

            ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> callConv = GenerateCallConvSyntaxFromAttributes(suppressGCTransitionAttribute, unmanagedCallConvAttribute);

            var typeKeyOwner = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(symbol.ContainingType);
            ManagedTypeInfo typeKeyType = SpecialTypeInfo.Byte;

            INamedTypeSymbol? iUnmanagedInterfaceTypeInstantiation = symbol.ContainingType.AllInterfaces.FirstOrDefault(iface => SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, iUnmanagedInterfaceTypeType));
            if (iUnmanagedInterfaceTypeInstantiation is null)
            {
                // TODO: Report invalid configuration
            }
            else
            {
                // The type key is the second generic type parameter, so we need to get the info for the
                // second argument.
                typeKeyType = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(iUnmanagedInterfaceTypeInstantiation.TypeArguments[1]);
            }

            MarshallingInfo exceptionMarshallingInfo = CreateExceptionMarshallingInfo(virtualMethodIndexAttr, symbol, environment.Compilation, generatorDiagnostics, virtualMethodIndexData);

            return new IncrementalStubGenerationContext(
                signatureContext,
                containingSyntaxContext,
                methodSyntaxTemplate,
                new MethodSignatureDiagnosticLocations(syntax),
                new SequenceEqualImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax>(callConv, SyntaxEquivalentComparer.Instance),
                VirtualMethodIndexData.From(virtualMethodIndexData),
                exceptionMarshallingInfo,
                ComInterfaceGeneratorHelpers.CreateGeneratorFactory(environment, MarshalDirection.ManagedToUnmanaged),
                ComInterfaceGeneratorHelpers.CreateGeneratorFactory(environment, MarshalDirection.UnmanagedToManaged),
                typeKeyType,
                typeKeyOwner,
                new SequenceEqualImmutableArray<Diagnostic>(generatorDiagnostics.Diagnostics.ToImmutableArray()));
        }

        private static MarshallingInfo CreateExceptionMarshallingInfo(AttributeData virtualMethodIndexAttr, ISymbol symbol, Compilation compilation, GeneratorDiagnostics diagnostics, VirtualMethodIndexCompilationData virtualMethodIndexData)
        {
            if (virtualMethodIndexData.ExceptionMarshallingDefined)
            {
                // User specified ExceptionMarshalling.Custom without specifying ExceptionMarshallingCustomType
                if (virtualMethodIndexData.ExceptionMarshalling == ExceptionMarshalling.Custom && virtualMethodIndexData.ExceptionMarshallingCustomType is null)
                {
                    diagnostics.ReportInvalidExceptionMarshallingConfiguration(
                        virtualMethodIndexAttr, symbol.Name, SR.InvalidExceptionMarshallingConfigurationMissingCustomType);
                    return NoMarshallingInfo.Instance;
                }

                // User specified something other than ExceptionMarshalling.Custom while specifying ExceptionMarshallingCustomType
                if (virtualMethodIndexData.ExceptionMarshalling != ExceptionMarshalling.Custom && virtualMethodIndexData.ExceptionMarshallingCustomType is not null)
                {
                    diagnostics.ReportInvalidExceptionMarshallingConfiguration(
                        virtualMethodIndexAttr, symbol.Name, SR.InvalidExceptionMarshallingConfigurationNotCustom);
                }
            }

            if (virtualMethodIndexData.ExceptionMarshalling == ExceptionMarshalling.Com)
            {
                return new ComExceptionMarshalling();
            }
            if (virtualMethodIndexData.ExceptionMarshalling == ExceptionMarshalling.Custom)
            {
                return virtualMethodIndexData.ExceptionMarshallingCustomType is null
                    ? NoMarshallingInfo.Instance
                    : CustomMarshallingInfoHelper.CreateNativeMarshallingInfoForNonSignatureElement(
                        compilation.GetTypeByMetadataName(TypeNames.System_Exception),
                        virtualMethodIndexData.ExceptionMarshallingCustomType!,
                        virtualMethodIndexAttr,
                        compilation,
                        diagnostics);
            }
            // This should not be reached in normal usage, but a developer can cast any int to the ExceptionMarshalling enum, so we should handle this case without crashing the generator.
            diagnostics.ReportInvalidExceptionMarshallingConfiguration(
                virtualMethodIndexAttr, symbol.Name, SR.InvalidExceptionMarshallingValue);
            return NoMarshallingInfo.Instance;
        }

        private static (MemberDeclarationSyntax, ImmutableArray<Diagnostic>) GenerateManagedToNativeStub(
            IncrementalStubGenerationContext methodStub)
        {
            var diagnostics = new GeneratorDiagnostics();

            // Generate stub code
            var stubGenerator = new ManagedToNativeVTableMethodGenerator(
                methodStub.ManagedToUnmanagedGeneratorFactory.Key.TargetFramework,
                methodStub.ManagedToUnmanagedGeneratorFactory.Key.TargetFrameworkVersion,
                methodStub.SignatureContext.ElementTypeInformation,
                methodStub.VtableIndexData.SetLastError,
                methodStub.VtableIndexData.ImplicitThisParameter,
                (elementInfo, ex) =>
                {
                    diagnostics.ReportMarshallingNotSupported(methodStub.DiagnosticLocation, elementInfo, ex.NotSupportedDetails);
                },
                methodStub.ManagedToUnmanagedGeneratorFactory.GeneratorFactory);

            BlockSyntax code = stubGenerator.GenerateStubBody(
                methodStub.VtableIndexData.Index,
                methodStub.CallingConvention.Array,
                methodStub.TypeKeyOwner.Syntax,
                methodStub.TypeKeyType);

            return (
                methodStub.ContainingSyntaxContext.AddContainingSyntax(
                    NativeTypeContainingSyntax)
                .WrapMemberInContainingSyntaxWithUnsafeModifier(
                    PrintGeneratedSource(
                        methodStub.StubMethodSyntaxTemplate,
                        methodStub.SignatureContext,
                        code)
                    .WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifier(IdentifierName(methodStub.ContainingSyntaxContext.ContainingSyntax[0].Identifier)))),
                methodStub.Diagnostics.Array.AddRange(diagnostics.Diagnostics));
        }

        private const string ThisParameterIdentifier = "@this";

        private static (MemberDeclarationSyntax, ImmutableArray<Diagnostic>) GenerateNativeToManagedStub(
            IncrementalStubGenerationContext methodStub)
        {
            var diagnostics = new GeneratorDiagnostics();
            ImmutableArray<TypePositionInfo> elements = AddImplicitElementInfos(methodStub);

            // Generate stub code
            var stubGenerator = new UnmanagedToManagedStubGenerator(
                methodStub.UnmanagedToManagedGeneratorFactory.Key.TargetFramework,
                methodStub.UnmanagedToManagedGeneratorFactory.Key.TargetFrameworkVersion,
                elements,
                (elementInfo, ex) =>
                {
                    diagnostics.ReportMarshallingNotSupported(methodStub.DiagnosticLocation, elementInfo, ex.NotSupportedDetails);
                },
                methodStub.UnmanagedToManagedGeneratorFactory.GeneratorFactory);

            BlockSyntax code = stubGenerator.GenerateStubBody(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(ThisParameterIdentifier),
                    IdentifierName(methodStub.StubMethodSyntaxTemplate.Identifier)));

            (ParameterListSyntax unmanagedParameterList, TypeSyntax returnType, _) = stubGenerator.GenerateAbiMethodSignatureData();

            AttributeSyntax unmanagedCallersOnlyAttribute = Attribute(
                ParseName(TypeNames.UnmanagedCallersOnlyAttribute));

            if (methodStub.CallingConvention.Array.Length != 0)
            {
                unmanagedCallersOnlyAttribute = unmanagedCallersOnlyAttribute.AddArgumentListArguments(
                    AttributeArgument(
                        ImplicitArrayCreationExpression(
                            InitializerExpression(SyntaxKind.CollectionInitializerExpression,
                                SeparatedList<ExpressionSyntax>(
                                    methodStub.CallingConvention.Array.Select(callConv => TypeOfExpression(ParseName($"System.Runtime.CompilerServices.CallConv{callConv.Name.ValueText}")))))))
                    .WithNameEquals(NameEquals(IdentifierName("CallConvs"))));
            }

            MethodDeclarationSyntax unmanagedToManagedStub =
                MethodDeclaration(returnType, $"ABI_{methodStub.StubMethodSyntaxTemplate.Identifier.Text}")
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithParameterList(unmanagedParameterList)
                .AddAttributeLists(AttributeList(SingletonSeparatedList(unmanagedCallersOnlyAttribute)))
                .WithBody(code);

            return (
                methodStub.ContainingSyntaxContext.AddContainingSyntax(
                    NativeTypeContainingSyntax)
                .WrapMemberInContainingSyntaxWithUnsafeModifier(
                    unmanagedToManagedStub),
                methodStub.Diagnostics.Array.AddRange(diagnostics.Diagnostics));
        }

        private static ImmutableArray<TypePositionInfo> AddImplicitElementInfos(IncrementalStubGenerationContext methodStub)
        {
            ImmutableArray<TypePositionInfo> originalElements = methodStub.SignatureContext.ElementTypeInformation;

            var elements = ImmutableArray.CreateBuilder<TypePositionInfo>(originalElements.Length + 2);

            elements.Add(new TypePositionInfo(methodStub.TypeKeyOwner, new NativeThisInfo(methodStub.TypeKeyType))
            {
                InstanceIdentifier = ThisParameterIdentifier,
                NativeIndex = 0,
            });
            foreach (TypePositionInfo element in originalElements)
            {
                elements.Add(element with
                {
                    NativeIndex = TypePositionInfo.IncrementIndex(element.NativeIndex)
                });
            }

            if (methodStub.ExceptionMarshallingInfo != NoMarshallingInfo.Instance)
            {
                elements.Add(
                    new TypePositionInfo(
                        new ReferenceTypeInfo($"global::{TypeNames.System_Exception}", TypeNames.System_Exception),
                        methodStub.ExceptionMarshallingInfo)
                    {
                        InstanceIdentifier = "__exception",
                        ManagedIndex = TypePositionInfo.ExceptionIndex,
                        NativeIndex = TypePositionInfo.ReturnIndex
                    });
            }

            return elements.ToImmutable();
        }

        private static Diagnostic? GetDiagnosticIfInvalidMethodForGeneration(MethodDeclarationSyntax methodSyntax, IMethodSymbol method)
        {
            // Verify the method has no generic types or defined implementation
            // and is not marked static or sealed
            if (methodSyntax.TypeParameterList is not null
                || methodSyntax.Body is not null
                || methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
                || methodSyntax.Modifiers.Any(SyntaxKind.SealedKeyword))
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

        private static MemberDeclarationSyntax GenerateNativeInterfaceMetadata(ContainingSyntaxContext context)
        {
            return context.WrapMemberInContainingSyntaxWithUnsafeModifier(
                InterfaceDeclaration("Native")
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.PartialKeyword)))
                .WithBaseList(BaseList(SingletonSeparatedList((BaseTypeSyntax)SimpleBaseType(IdentifierName(context.ContainingSyntax[0].Identifier)))))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(Attribute(ParseName(TypeNames.System_Runtime_InteropServices_DynamicInterfaceCastableImplementationAttribute))))));
        }

        private static MemberDeclarationSyntax GeneratePopulateVTableMethod(IGrouping<ContainingSyntaxContext, IncrementalStubGenerationContext> vtableMethods)
        {
            const string vtableParameter = "vtable";
            ContainingSyntaxContext containingSyntax = vtableMethods.Key.AddContainingSyntax(NativeTypeContainingSyntax);
            MethodDeclarationSyntax populateVtableMethod = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)),
                "PopulateUnmanagedVirtualMethodTable")
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword)))
                .AddParameterListParameters(
                    Parameter(Identifier(vtableParameter))
                    .WithType(GenericName(TypeNames.System_Span).AddTypeArgumentListArguments(IdentifierName("nint"))));

            foreach (var method in vtableMethods)
            {
                var stubGenerator = new UnmanagedToManagedStubGenerator(
                    method.UnmanagedToManagedGeneratorFactory.Key.TargetFramework,
                    method.UnmanagedToManagedGeneratorFactory.Key.TargetFrameworkVersion,
                    AddImplicitElementInfos(method),
                    // Swallow diagnostics here since the diagnostics will be reported by the unmanaged->managed stub generation
                    (elementInfo, ex) => { },
                    method.UnmanagedToManagedGeneratorFactory.GeneratorFactory);

                List<FunctionPointerParameterSyntax> functionPointerParameters = new();
                var (paramList, retType, _) = stubGenerator.GenerateAbiMethodSignatureData();
                functionPointerParameters.AddRange(paramList.Parameters.Select(p => FunctionPointerParameter(p.Type)));
                functionPointerParameters.Add(FunctionPointerParameter(retType));

                // delegate* unmanaged<...>
                ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> callConv = method.CallingConvention.Array;
                FunctionPointerTypeSyntax functionPointerType = FunctionPointerType(
                        FunctionPointerCallingConvention(Token(SyntaxKind.UnmanagedKeyword), callConv.IsEmpty ? null : FunctionPointerUnmanagedCallingConventionList(SeparatedList(callConv))),
                        FunctionPointerParameterList(SeparatedList(functionPointerParameters)));

                // <vtableParameter>[<index>] = (nint)(<functionPointerType>)&ABI_<methodIdentifier>;
                populateVtableMethod = populateVtableMethod.AddBodyStatements(
                    ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            ElementAccessExpression(
                                IdentifierName(vtableParameter))
                            .AddArgumentListArguments(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(method.VtableIndexData.Index)))),
                            CastExpression(IdentifierName("nint"),
                                CastExpression(functionPointerType,
                                    PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                        IdentifierName($"ABI_{method.StubMethodSyntaxTemplate.Identifier}")))))));
            }

            return containingSyntax.WrapMemberInContainingSyntaxWithUnsafeModifier(populateVtableMethod);
        }
    }
}
