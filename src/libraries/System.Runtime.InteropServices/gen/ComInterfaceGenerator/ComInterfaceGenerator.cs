// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.SyntaxFactoryExtensions;

namespace Microsoft.Interop
{
    [Generator]
    public sealed partial class ComInterfaceGenerator : IIncrementalGenerator
    {
        public static class StepNames
        {
            public const string CalculateStubInformation = nameof(CalculateStubInformation);
            public const string GenerateManagedToNativeStub = nameof(GenerateManagedToNativeStub);
            public const string GenerateNativeToManagedStub = nameof(GenerateNativeToManagedStub);
            public const string GenerateManagedToNativeInterfaceImplementation = nameof(GenerateManagedToNativeInterfaceImplementation);
            public const string GenerateNativeToManagedVTableMethods = nameof(GenerateNativeToManagedVTableMethods);
            public const string GenerateNativeToManagedVTableStruct = nameof(GenerateNativeToManagedVTableStruct);
            public const string GenerateNativeToManagedVTable = nameof(GenerateNativeToManagedVTable);
            public const string GenerateInterfaceInformation = nameof(GenerateInterfaceInformation);
            public const string GenerateIUnknownDerivedAttribute = nameof(GenerateIUnknownDerivedAttribute);
            public const string GenerateShadowingMethods = nameof(GenerateShadowingMethods);
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Get all types with the [GeneratedComInterface] attribute.
            var attributedInterfaces = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    TypeNames.GeneratedComInterfaceAttribute,
                    static (node, ct) => node is InterfaceDeclarationSyntax,
                    static (context, ct) => context.TargetSymbol is INamedTypeSymbol interfaceSymbol
                        ? new { Syntax = (InterfaceDeclarationSyntax)context.TargetNode, Symbol = interfaceSymbol }
                        : null)
                .Where(
                    static modelData => modelData is not null);
            var stubEnvironment = context.CreateStubEnvironmentProvider();
            var interfaceSymbolOrDiagnostics = attributedInterfaces.Combine(stubEnvironment).Select(static (data, ct) =>
            {
                return ComInterfaceInfo.From(data.Left.Symbol, data.Left.Syntax, data.Right, ct);
            });
            var interfaceSymbolsToGenerateWithoutDiagnostics = context.FilterAndReportDiagnostics(interfaceSymbolOrDiagnostics);

            var externalInterfaceSymbols = attributedInterfaces.SelectMany(static (data, ct) =>
            {
                return ComInterfaceInfo.CreateInterfaceInfoForBaseInterfacesInOtherCompilations(data.Symbol);
            });

            var interfaceSymbolsWithoutDiagnostics = interfaceSymbolsToGenerateWithoutDiagnostics.Concat(externalInterfaceSymbols);

            var interfaceContextsOrDiagnostics = interfaceSymbolsWithoutDiagnostics
                .Select((data, ct) => data.InterfaceInfo!)
                .Collect()
                .SelectMany(ComInterfaceContext.GetContexts);

            // Filter down interface symbols to remove those with diagnostics from GetContexts
            (var interfaceContexts, interfaceSymbolsWithoutDiagnostics) = context.FilterAndReportDiagnostics(interfaceContextsOrDiagnostics, interfaceSymbolsWithoutDiagnostics);

            var comMethodsAndSymbolsOrDiagnostics = interfaceSymbolsWithoutDiagnostics.Select(ComMethodInfo.GetMethodsFromInterface);
            var methodInfoAndSymbolGroupedByInterface = context
                .FilterAndReportDiagnostics<(ComMethodInfo MethodInfo, IMethodSymbol Symbol)>(comMethodsAndSymbolsOrDiagnostics);

            var methodInfosGroupedByInterface = methodInfoAndSymbolGroupedByInterface
                .Select(static (methods, ct) =>
                    methods.Select(pair => pair.MethodInfo).ToSequenceEqualImmutableArray());
            // Create list of methods (inherited and declared) and their owning interface
            var comMethodContextBuilders = interfaceContexts
                .Zip(methodInfosGroupedByInterface)
                .Collect()
                .SelectMany(static (data, ct) =>
                {
                    return data.GroupBy(data => data.Left.GetTopLevelBase());
                })
                .SelectMany(static (data, ct) =>
                {
                    return ComMethodContext.CalculateAllMethods(data, ct);
                })
                // Now that we've determined method offsets, we can remove all externally defined methods.
                // We'll also filter out methods originally declared on externally defined base interfaces
                // as we may not be able to emit them into our assembly.
                .Where(context => !context.Method.OriginalDeclaringInterface.IsExternallyDefined);

            // Now that we've determined method offsets, we can remove all externally defined interfaces.
            var interfaceContextsToGenerate = interfaceContexts.Where(context => !context.IsExternallyDefined);

            // A dictionary isn't incremental, but it will have symbols, so it will never be incremental anyway.
            var methodInfoToSymbolMap = methodInfoAndSymbolGroupedByInterface
                .SelectMany((data, ct) => data)
                .Collect()
                .Select((data, ct) => data.ToDictionary(static x => x.MethodInfo, static x => x.Symbol));
            var comMethodContexts = comMethodContextBuilders
                .Combine(methodInfoToSymbolMap)
                .Combine(stubEnvironment)
                .Select((param, ct) =>
                {
                    var ((data, symbolMap), env) = param;
                    return new ComMethodContext(
                        data.Method,
                        data.OwningInterface,
                        CalculateStubInformation(data.Method.MethodInfo.Syntax, symbolMap[data.Method.MethodInfo], data.Method.Index, env, data.OwningInterface.Info.Type, ct));
                }).WithTrackingName(StepNames.CalculateStubInformation);

            var interfaceAndMethodsContexts = comMethodContexts
                .Collect()
                .Combine(interfaceContextsToGenerate.Collect())
                .SelectMany((data, ct) => GroupComContextsForInterfaceGeneration(data.Left, data.Right, ct));

            // Generate the code for the managed-to-unmanaged stubs.
            var managedToNativeInterfaceImplementations = interfaceAndMethodsContexts
                .Select(GenerateImplementationInterface)
                .WithTrackingName(StepNames.GenerateManagedToNativeInterfaceImplementation)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            // Generate the code for the unmanaged-to-managed stubs.
            var nativeToManagedVtableStructs = interfaceAndMethodsContexts
                .Select(GenerateInterfaceImplementationVtable)
                .WithTrackingName(StepNames.GenerateNativeToManagedVTableStruct)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            var nativeToManagedVtableMethods = interfaceAndMethodsContexts
                .Select(GenerateImplementationVTableMethods)
                .WithTrackingName(StepNames.GenerateNativeToManagedVTableMethods)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            // Report diagnostics for managed-to-unmanaged and unmanaged-to-managed stubs, deduplicating diagnostics that are reported for both.
            context.RegisterDiagnostics(
                interfaceAndMethodsContexts
                    .SelectMany((data, ct) => data.DeclaredMethods.SelectMany(m => m.ManagedToUnmanagedStub.Diagnostics).Union(data.DeclaredMethods.SelectMany(m => m.UnmanagedToManagedStub.Diagnostics))));

            // Generate the native interface metadata for each [GeneratedComInterface]-attributed interface.
            var nativeInterfaceInformation = interfaceContextsToGenerate
                .Select(static (data, ct) => data.Info)
                .Select(GenerateInterfaceInformation)
                .WithTrackingName(StepNames.GenerateInterfaceInformation)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            var shadowingMethodDeclarations = interfaceAndMethodsContexts
                .Select((data, ct) =>
                {
                    var context = data.Interface.Info;
                    var methods = data.ShadowingMethods.Select(m => m.Shadow);
                    var typeDecl = TypeDeclaration(context.ContainingSyntax.TypeKind, context.ContainingSyntax.Identifier)
                        .WithModifiers(context.ContainingSyntax.Modifiers)
                        .WithTypeParameterList(context.ContainingSyntax.TypeParameters)
                        .WithMembers(List<MemberDeclarationSyntax>(methods));
                    return data.Interface.Info.TypeDefinitionContext.WrapMemberInContainingSyntaxWithUnsafeModifier(typeDecl);
                })
                .WithTrackingName(StepNames.GenerateShadowingMethods)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            // Generate a method named CreateManagedVirtualFunctionTable on the native interface implementation
            // that allocates and fills in the memory for the vtable.
            var nativeToManagedVtables = interfaceAndMethodsContexts
                .Select(GenerateImplementationVTable)
                .WithTrackingName(StepNames.GenerateNativeToManagedVTable)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            var iUnknownDerivedAttributeApplication = interfaceContextsToGenerate
                .Select(static (data, ct) => data.Info)
                .Select(GenerateIUnknownDerivedAttributeApplication)
                .WithTrackingName(StepNames.GenerateIUnknownDerivedAttribute)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            var filesToGenerate = interfaceContextsToGenerate
                .Zip(nativeInterfaceInformation)
                .Zip(managedToNativeInterfaceImplementations)
                .Zip(nativeToManagedVtableMethods)
                .Zip(nativeToManagedVtableStructs)
                .Zip(nativeToManagedVtables)
                .Zip(iUnknownDerivedAttributeApplication)
                .Zip(shadowingMethodDeclarations)
                .Select(static (data, ct) =>
                {
                    var (((((((interfaceContext, interfaceInfo), managedToNativeStubs), nativeToManagedStubs), nativeToManagedStructs), nativeToManagedVtable), iUnknownDerivedAttribute), shadowingMethod) = data;

                    using StringWriter source = new();
                    source.WriteLine("// <auto-generated />");
                    source.WriteLine("#pragma warning disable CS0612, CS0618, CS0649"); // Suppress warnings about [Obsolete] and "lack of assignment" in generated code.

                    // If the user has specified 'ManagedObjectWrapper', it means that the COM interface will never be used to marshal a native
                    // object as an RCW (eg. the IDIC vtable will also not be generated, nor any additional supporting code). To reduce binary
                    // size, we're not emitting the interface methods on the implementation interface that has '[DynamicInterfaceCastableImplementation]'
                    // on it. However, doing so will cause the CA2256 warning to be produced. We can't remove the attribute, as that would cause
                    // the wrong exception to be thrown when trying an IDIC cast with this interface (not 'InvalidCastException'). Because this is
                    // a niche scenario, and we don't want to regress perf or size, we can just disable the warning instead.
                    if (interfaceContext.Options is ComInterfaceOptions.ManagedObjectWrapper)
                    {
                        source.WriteLine("#pragma warning disable CA2256");
                    }

                    interfaceInfo.WriteTo(source);
                    // Two newlines looks cleaner than one
                    source.WriteLine();
                    source.WriteLine();
                    // TODO: Merge the three InterfaceImplementation partials? We have them all right here.
                    managedToNativeStubs.WriteTo(source);
                    source.WriteLine();
                    source.WriteLine();
                    nativeToManagedStubs.WriteTo(source);
                    source.WriteLine();
                    source.WriteLine();
                    nativeToManagedStructs.WriteTo(source);
                    source.WriteLine();
                    source.WriteLine();
                    nativeToManagedVtable.WriteTo(source);
                    source.WriteLine();
                    source.WriteLine();
                    iUnknownDerivedAttribute.WriteTo(source);
                    source.WriteLine();
                    source.WriteLine();
                    shadowingMethod.WriteTo(source);
                    return new { TypeName = interfaceContext.Info.Type.FullTypeName, Source = source.ToString() };
                });

            context.RegisterSourceOutput(filesToGenerate, (context, data) =>
            {
                context.AddSource(data.TypeName.Replace(TypeNames.GlobalAlias, ""), data.Source);
            });
        }

        private static readonly AttributeSyntax s_iUnknownDerivedAttributeTemplate =
            Attribute(
                GenericName(TypeNames.GlobalAlias + TypeNames.IUnknownDerivedAttribute)
                    .AddTypeArgumentListArguments(
                        IdentifierName("InterfaceInformation"),
                        IdentifierName("InterfaceImplementation")));

        private static MemberDeclarationSyntax GenerateIUnknownDerivedAttributeApplication(ComInterfaceInfo context, CancellationToken _)
            => context.TypeDefinitionContext.WrapMemberInContainingSyntaxWithUnsafeModifier(
                TypeDeclaration(context.ContainingSyntax.TypeKind, context.ContainingSyntax.Identifier)
                    .WithModifiers(context.ContainingSyntax.Modifiers)
                    .WithTypeParameterList(context.ContainingSyntax.TypeParameters)
                    .AddAttributeLists(AttributeList(SingletonSeparatedList(s_iUnknownDerivedAttributeTemplate))));

        private static bool IsHResultLikeType(ManagedTypeInfo type)
        {
            string typeName = type.FullTypeName.Split('.', ':')[^1];
            return typeName.Equals("hr", StringComparison.OrdinalIgnoreCase)
                || typeName.Equals("hresult", StringComparison.OrdinalIgnoreCase);
        }

        private static IncrementalMethodStubGenerationContext CalculateStubInformation(MethodDeclarationSyntax syntax, IMethodSymbol symbol, int index, StubEnvironment environment, ManagedTypeInfo owningInterface, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            INamedTypeSymbol? lcidConversionAttrType = environment.LcidConversionAttrType;
            INamedTypeSymbol? suppressGCTransitionAttrType = environment.SuppressGCTransitionAttrType;
            INamedTypeSymbol? unmanagedCallConvAttrType = environment.UnmanagedCallConvAttrType;
            // Get any attributes of interest on the method
            AttributeData? lcidConversionAttr = null;
            AttributeData? suppressGCTransitionAttribute = null;
            AttributeData? unmanagedCallConvAttribute = null;
            foreach (AttributeData attr in symbol.GetAttributes())
            {
                if (lcidConversionAttrType is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, lcidConversionAttrType))
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

            var locations = new MethodSignatureDiagnosticLocations(syntax);
            var generatorDiagnostics = new GeneratorDiagnosticsBag(new DiagnosticDescriptorProvider(), locations, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.ComInterfaceGenerator.SR));

            if (lcidConversionAttr is not null)
            {
                // Using LCIDConversion with source-generated interop is not supported
                generatorDiagnostics.ReportConfigurationNotSupported(lcidConversionAttr, nameof(TypeNames.LCIDConversionAttribute));
            }

            GeneratedComInterfaceCompilationData.TryGetGeneratedComInterfaceAttributeFromInterface(symbol.ContainingType, out var generatedComAttribute);
            var generatedComInterfaceAttributeData = GeneratedComInterfaceCompilationData.GetDataFromAttribute(generatedComAttribute);
            // Create the stub.

            var signatureContext = SignatureContext.Create(
                symbol,
                DefaultMarshallingInfoParser.Create(
                    environment,
                    generatorDiagnostics,
                    symbol,
                    generatedComInterfaceAttributeData,
                    generatedComAttribute),
                environment,
                new CodeEmitOptions(SkipInit: true),
                typeof(VtableIndexStubGenerator).Assembly);

            if (!symbol.MethodImplementationFlags.HasFlag(MethodImplAttributes.PreserveSig))
            {
                // Search for the element information for the managed return value.
                // We need to transform it such that any return type is converted to an out parameter at the end of the parameter list.
                ImmutableArray<TypePositionInfo> returnSwappedSignatureElements = signatureContext.ElementTypeInformation;
                for (int i = 0; i < returnSwappedSignatureElements.Length; ++i)
                {
                    if (returnSwappedSignatureElements[i].IsManagedReturnPosition)
                    {
                        if (returnSwappedSignatureElements[i].ManagedType == SpecialTypeInfo.Void)
                        {
                            // Return type is void, just remove the element from the signature list.
                            // We don't introduce an out parameter.
                            returnSwappedSignatureElements = returnSwappedSignatureElements.RemoveAt(i);
                        }
                        else
                        {
                            if ((returnSwappedSignatureElements[i].ManagedType is SpecialTypeInfo { SpecialType: SpecialType.System_Int32 or SpecialType.System_Enum } or EnumTypeInfo
                                    && returnSwappedSignatureElements[i].MarshallingAttributeInfo.Equals(NoMarshallingInfo.Instance))
                                || (IsHResultLikeType(returnSwappedSignatureElements[i].ManagedType)))
                            {
                                generatorDiagnostics.ReportDiagnostic(DiagnosticInfo.Create(GeneratorDiagnostics.ComMethodManagedReturnWillBeOutVariable, symbol.Locations[0]));
                            }
                            // Convert the current element into an out parameter on the native signature
                            // while keeping it at the return position in the managed signature.
                            var managedSignatureAsNativeOut = returnSwappedSignatureElements[i] with
                            {
                                RefKind = RefKind.Out,
                                ManagedIndex = TypePositionInfo.ReturnIndex,
                                NativeIndex = symbol.Parameters.Length
                            };
                            returnSwappedSignatureElements = returnSwappedSignatureElements.SetItem(i, managedSignatureAsNativeOut);
                        }
                        break;
                    }
                }

                signatureContext = signatureContext with
                {
                    // Add the HRESULT return value in the native signature.
                    // This element does not have any influence on the managed signature, so don't assign a managed index.
                    ElementTypeInformation = returnSwappedSignatureElements.Add(
                        new TypePositionInfo(SpecialTypeInfo.Int32, new ManagedHResultExceptionMarshallingInfo())
                        {
                            NativeIndex = TypePositionInfo.ReturnIndex
                        })
                };
            }
            else
            {
                // If our method is PreserveSig, we will notify the user if they are returning a type that may be an HRESULT type
                // that is defined as a structure. These types used to work with built-in COM interop, but they do not work with
                // source-generated interop as we now use the MemberFunction calling convention, which is more correct.
                TypePositionInfo? managedReturnInfo = signatureContext.ElementTypeInformation.FirstOrDefault(e => e.IsManagedReturnPosition);
                if (managedReturnInfo is { MarshallingAttributeInfo: UnmanagedBlittableMarshallingInfo, ManagedType: ValueTypeInfo valueType }
                    && IsHResultLikeType(valueType))
                {
                    generatorDiagnostics.ReportDiagnostic(DiagnosticInfo.Create(
                        GeneratorDiagnostics.HResultTypeWillBeTreatedAsStruct,
                        symbol.Locations[0],
                        ImmutableDictionary<string, string>.Empty.Add(GeneratorDiagnosticProperties.AddMarshalAsAttribute, "Error"),
                        valueType.DiagnosticFormattedName));
                }
            }

            var direction = GetDirectionFromOptions(generatedComInterfaceAttributeData.Options);

            // Ensure the size of collections are known at marshal / unmarshal in time.
            // A collection that is marshalled in cannot have a size that is an 'out' parameter.
            foreach (TypePositionInfo parameter in signatureContext.ManagedParameters)
            {
                MarshallerHelpers.ValidateCountInfoAvailableAtCall(
                    direction,
                    parameter,
                    generatorDiagnostics,
                    symbol,
                    GeneratorDiagnostics.SizeOfInCollectionMustBeDefinedAtCallOutParam,
                    GeneratorDiagnostics.SizeOfInCollectionMustBeDefinedAtCallReturnValue);
            }

            var containingSyntaxContext = new ContainingSyntaxContext(syntax);

            var methodSyntaxTemplate = new ContainingSyntax(new SyntaxTokenList(syntax.Modifiers.Where(static m => !m.IsKind(SyntaxKind.NewKeyword))).StripAccessibilityModifiers(), SyntaxKind.MethodDeclaration, syntax.Identifier, syntax.TypeParameterList);

            ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> callConv = VirtualMethodPointerStubGenerator.GenerateCallConvSyntaxFromAttributes(
                suppressGCTransitionAttribute,
                unmanagedCallConvAttribute,
                ImmutableArray.Create(FunctionPointerUnmanagedCallingConvention(Identifier("MemberFunction"))));

            var declaringType = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(symbol.ContainingType);

            var virtualMethodIndexData = new VirtualMethodIndexData(index, ImplicitThisParameter: true, direction, true, ExceptionMarshalling.Com);

            MarshallingInfo exceptionMarshallingInfo;

            if (generatedComInterfaceAttributeData.ExceptionToUnmanagedMarshaller is null)
            {
                exceptionMarshallingInfo = new ComExceptionMarshalling();
            }
            else
            {
                exceptionMarshallingInfo = CustomMarshallingInfoHelper.CreateNativeMarshallingInfoForNonSignatureElement(
                    environment.Compilation.GetTypeByMetadataName(TypeNames.System_Exception),
                    (INamedTypeSymbol)generatedComInterfaceAttributeData.ExceptionToUnmanagedMarshaller,
                    generatedComAttribute,
                    environment.Compilation,
                    generatorDiagnostics);
            }

            return new IncrementalMethodStubGenerationContext(
                signatureContext,
                containingSyntaxContext,
                methodSyntaxTemplate,
                locations,
                callConv.ToSequenceEqualImmutableArray(SyntaxEquivalentComparer.Instance),
                virtualMethodIndexData,
                exceptionMarshallingInfo,
                environment.EnvironmentFlags,
                owningInterface,
                declaringType,
                generatorDiagnostics.Diagnostics.ToSequenceEqualImmutableArray(),
                ComInterfaceDispatchMarshallingInfo.Instance);
        }

        private static MarshalDirection GetDirectionFromOptions(ComInterfaceOptions options)
        {
            if (options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper | ComInterfaceOptions.ComObjectWrapper))
            {
                return MarshalDirection.Bidirectional;
            }
            if (options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper))
            {
                return MarshalDirection.UnmanagedToManaged;
            }
            if (options.HasFlag(ComInterfaceOptions.ComObjectWrapper))
            {
                return MarshalDirection.ManagedToUnmanaged;
            }
            throw new ArgumentOutOfRangeException(nameof(options), "No-wrapper options should have been filtered out before calling this method.");
        }

        private static ImmutableArray<ComInterfaceAndMethodsContext> GroupComContextsForInterfaceGeneration(ImmutableArray<ComMethodContext> methods, ImmutableArray<ComInterfaceContext> interfaces, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            // We can end up with an empty set of contexts here as the compiler will call a SelectMany
            // after a Collect with no input entries
            if (interfaces.IsEmpty)
            {
                return ImmutableArray<ComInterfaceAndMethodsContext>.Empty;
            }

            // Due to how the source generator driver processes the input item tables and our limitation that methods on COM interfaces can only be defined in a single partial definition of the type,
            // we can guarantee that, if the interface contexts are in order of I1, I2, I3, I4..., then then method contexts are ordered as follows:
            // - I1.M1
            // - I1.M2
            // - I1.M3
            // - I2.M1
            // - I2.M2
            // - I2.M3
            // - I4.M1 (I3 had no methods)
            // - etc...
            // This enable us to group our contexts by their containing syntax rather simply.
            var contextList = ImmutableArray.CreateBuilder<ComInterfaceAndMethodsContext>();
            int methodIndex = 0;
            foreach (var iface in interfaces)
            {
                var methodList = ImmutableArray.CreateBuilder<ComMethodContext>();
                while (methodIndex < methods.Length && methods[methodIndex].OwningInterface == iface)
                {
                    var method = methods[methodIndex];
                    if (method.MethodInfo.IsUserDefinedShadowingMethod)
                    {
                        bool shadowFound = false;
                        int shadowIndex = -1;
                        // Don't remove method, but make it so that it doesn't generate any stubs
                        for (int i = methodList.Count - 1; i > -1; i--)
                        {
                            var potentialShadowedMethod = methodList[i];
                            if (MethodEquals(method, potentialShadowedMethod))
                            {
                                shadowFound = true;
                                shadowIndex = i;
                                break;
                            }
                        }
                        if (shadowFound)
                        {
                            methodList[shadowIndex].IsHiddenOnDerivedInterface = true;
                        }
                        // We might not find the shadowed method if it's defined on a non-GeneratedComInterface-attributed interface. Thats okay and we can disregard it.
                    }
                    methodList.Add(methods[methodIndex++]);
                }
                contextList.Add(new(iface, methodList.ToImmutable().ToSequenceEqual()));
            }
            return contextList.ToImmutable();

            static bool MethodEquals(ComMethodContext a, ComMethodContext b)
            {
                if (a.MethodInfo.MethodName != b.MethodInfo.MethodName)
                    return false;
                if (a.GenerationContext.SignatureContext.ManagedParameters.SequenceEqual(b.GenerationContext.SignatureContext.ManagedParameters))
                    return true;
                return false;
            }
        }

        private static readonly InterfaceDeclarationSyntax ImplementationInterfaceTemplate = InterfaceDeclaration("InterfaceImplementation")
                .WithModifiers(TokenList(Token(SyntaxKind.FileKeyword), Token(SyntaxKind.UnsafeKeyword), Token(SyntaxKind.PartialKeyword)));

        private static InterfaceDeclarationSyntax GenerateImplementationInterface(ComInterfaceAndMethodsContext interfaceGroup, CancellationToken _)
        {
            var definingType = interfaceGroup.Interface.Info.Type;
            var shadowImplementations = interfaceGroup.InheritedMethods.Select(m => (Method: m, ManagedToUnmanagedStub: m.ManagedToUnmanagedStub))
                .Where(p => p.ManagedToUnmanagedStub is GeneratedStubCodeContext)
                .Select(ctx => ((GeneratedStubCodeContext)ctx.ManagedToUnmanagedStub).Stub.Node
                .WithExplicitInterfaceSpecifier(
                    ExplicitInterfaceSpecifier(ParseName(definingType.FullTypeName))));
            var inheritedStubs = interfaceGroup.InheritedMethods.Select(m => m.UnreachableExceptionStub);
            return ImplementationInterfaceTemplate
                .AddBaseListTypes(SimpleBaseType(definingType.Syntax))
                .WithMembers(
                    List<MemberDeclarationSyntax>(
                        interfaceGroup.DeclaredMethods
                        .Select(m => m.ManagedToUnmanagedStub)
                        .OfType<GeneratedStubCodeContext>()
                        .Select(ctx => ctx.Stub.Node)
                        .Concat(shadowImplementations)
                        .Concat(inheritedStubs)))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(Attribute(NameSyntaxes.System_Runtime_InteropServices_DynamicInterfaceCastableImplementationAttribute))));
        }

        private static InterfaceDeclarationSyntax GenerateImplementationVTableMethods(ComInterfaceAndMethodsContext comInterfaceAndMethods, CancellationToken _)
        {
            return ImplementationInterfaceTemplate
                .WithMembers(
                    List<MemberDeclarationSyntax>(
                        comInterfaceAndMethods.DeclaredMethods
                            .Select(m => m.UnmanagedToManagedStub)
                            .OfType<GeneratedStubCodeContext>()
                            .Where(context => context.Diagnostics.All(diag => diag.Descriptor.DefaultSeverity != DiagnosticSeverity.Error))
                            .Select(context => context.Stub.Node)));
        }

        private static readonly StructDeclarationSyntax InterfaceImplementationVtableTemplate = StructDeclaration("InterfaceImplementationVtable")
            .WithModifiers(TokenList(Token(SyntaxKind.FileKeyword), Token(SyntaxKind.UnsafeKeyword)));

        private static StructDeclarationSyntax GenerateInterfaceImplementationVtable(ComInterfaceAndMethodsContext interfaceMethods, CancellationToken _)
        {
            StructDeclarationSyntax vtableDeclaration =
                InterfaceImplementationVtableTemplate
                    .AddMembers(
                        FieldDeclaration(
                            VariableDeclaration(
                                FunctionPointerType(
                                    FunctionPointerCallingConvention(
                                        Token(SyntaxKind.UnmanagedKeyword),
                                        FunctionPointerUnmanagedCallingConventionList(
                                            SingletonSeparatedList(
                                                FunctionPointerUnmanagedCallingConvention(Identifier("MemberFunction"))))),
                                    FunctionPointerParameterList(
                                        SeparatedList([
                                            FunctionPointerParameter(TypeSyntaxes.VoidStar),
                                            FunctionPointerParameter(PointerType(TypeSyntaxes.System_Guid)),
                                            FunctionPointerParameter(TypeSyntaxes.VoidStarStar),
                                            FunctionPointerParameter(ParseTypeName("int"))]))))
                            .AddVariables(VariableDeclarator("QueryInterface_0")))
                        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword))),
                        FieldDeclaration(
                            VariableDeclaration(
                                FunctionPointerType(
                                    FunctionPointerCallingConvention(
                                        Token(SyntaxKind.UnmanagedKeyword),
                                        FunctionPointerUnmanagedCallingConventionList(
                                            SingletonSeparatedList(
                                                FunctionPointerUnmanagedCallingConvention(Identifier("MemberFunction"))))),
                                    FunctionPointerParameterList(
                                        SeparatedList([
                                            FunctionPointerParameter(TypeSyntaxes.VoidStar),
                                            FunctionPointerParameter(ParseTypeName("uint"))]))))
                            .AddVariables(VariableDeclarator("AddRef_1")))
                        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword))),
                        FieldDeclaration(
                            VariableDeclaration(
                                FunctionPointerType(
                                    FunctionPointerCallingConvention(
                                        Token(SyntaxKind.UnmanagedKeyword),
                                        FunctionPointerUnmanagedCallingConventionList(
                                            SingletonSeparatedList(
                                                FunctionPointerUnmanagedCallingConvention(Identifier("MemberFunction"))))),
                                    FunctionPointerParameterList(
                                        SeparatedList([
                                            FunctionPointerParameter(TypeSyntaxes.VoidStar),
                                            FunctionPointerParameter(ParseTypeName("uint"))]))))
                            .AddVariables(VariableDeclarator("Release_2")))
                        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword))))
                    .AddAttributeLists(
                        AttributeList(
                            SingletonSeparatedList(
                                Attribute(
                                    NameSyntaxes.System_Runtime_InteropServices_StructLayoutAttribute,
                                    AttributeArgumentList(
                                        SingletonSeparatedList(
                                            AttributeArgument(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    TypeSyntaxes.System_Runtime_InteropServices_LayoutKind,
                                                    IdentifierName("Sequential")))))))));

            if (interfaceMethods.Interface.Base is not null)
            {
                foreach (ComMethodContext inheritedMethod in interfaceMethods.InheritedMethods)
                {
                    FunctionPointerTypeSyntax functionPointerType = VirtualMethodPointerStubGenerator.GenerateUnmanagedFunctionPointerTypeForMethod(
                        inheritedMethod.GenerationContext,
                        ComInterfaceGeneratorHelpers.GetGeneratorResolver);

                    vtableDeclaration = vtableDeclaration
                        .AddMembers(
                            FieldDeclaration(
                                VariableDeclaration(functionPointerType)
                                .AddVariables(VariableDeclarator($"{inheritedMethod.MethodInfo.MethodName}_{inheritedMethod.GenerationContext.VtableIndexData.Index}")))
                            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword))));
                }
            }

            foreach (ComMethodContext declaredMethod in
                interfaceMethods.DeclaredMethods
                .Where(context => context.UnmanagedToManagedStub.Diagnostics.All(diag => diag.Descriptor.DefaultSeverity != DiagnosticSeverity.Error)))
            {
                FunctionPointerTypeSyntax functionPointerType = VirtualMethodPointerStubGenerator.GenerateUnmanagedFunctionPointerTypeForMethod(
                        declaredMethod.GenerationContext,
                        ComInterfaceGeneratorHelpers.GetGeneratorResolver);

                vtableDeclaration = vtableDeclaration
                    .AddMembers(
                        FieldDeclaration(
                            VariableDeclaration(functionPointerType)
                            .AddVariables(VariableDeclarator($"{declaredMethod.MethodInfo.MethodName}_{declaredMethod.GenerationContext.VtableIndexData.Index}")))
                        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword))));
            }

            return vtableDeclaration;
        }

        private static InterfaceDeclarationSyntax GenerateImplementationVTable(ComInterfaceAndMethodsContext interfaceMethods, CancellationToken _)
        {
            if (!interfaceMethods.Interface.Options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper))
            {
                return ImplementationInterfaceTemplate;
            }

            BlockSyntax fillBaseInterfaceSlots;


            if (interfaceMethods.Interface.Base is null)
            {
                // If we don't have a base interface, we need to manually fill in the base iUnknown slots.
                fillBaseInterfaceSlots = Block()
                    .AddStatements(
                        // ComWrappers.GetIUnknownImpl(
                        //      out *(nint*)&((InterfaceImplementationVtable*)Unsafe.AsPointer(ref Vtable))->QueryInterface_0,
                        //      out *(nint*)&((InterfaceImplementationVtable*)Unsafe.AsPointer(ref Vtable))->AddRef_1,
                        //      out *(nint*)&((InterfaceImplementationVtable*)Unsafe.AsPointer(ref Vtable))->Release_2);
                        MethodInvocationStatement(
                            TypeSyntaxes.System_Runtime_InteropServices_ComWrappers,
                            IdentifierName("GetIUnknownImpl"),
                            OutArgument(
                                PrefixUnaryExpression(
                                    SyntaxKind.PointerIndirectionExpression,
                                    CastExpression(
                                        PointerType(ParseTypeName("nint")),
                                        PrefixUnaryExpression(
                                            SyntaxKind.AddressOfExpression,
                                            MemberAccessExpression(
                                                SyntaxKind.PointerMemberAccessExpression,
                                                ParenthesizedExpression(
                                                    CastExpression(
                                                        PointerType(ParseTypeName("InterfaceImplementationVtable")),
                                                        InvocationExpression(
                                                            MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                TypeSyntaxes.System_Runtime_CompilerServices_Unsafe,
                                                                IdentifierName("AsPointer")))
                                                        .AddArgumentListArguments(
                                                            Argument(
                                                                RefExpression(IdentifierName("Vtable")))))),
                                                IdentifierName("QueryInterface_0")))))),
                            OutArgument(
                                PrefixUnaryExpression(
                                    SyntaxKind.PointerIndirectionExpression,
                                    CastExpression(
                                        PointerType(ParseTypeName("nint")),
                                        PrefixUnaryExpression(
                                            SyntaxKind.AddressOfExpression,
                                            MemberAccessExpression(
                                                SyntaxKind.PointerMemberAccessExpression,
                                                ParenthesizedExpression(
                                                    CastExpression(
                                                        PointerType(ParseTypeName("InterfaceImplementationVtable")),
                                                        InvocationExpression(
                                                            MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                TypeSyntaxes.System_Runtime_CompilerServices_Unsafe,
                                                                IdentifierName("AsPointer")))
                                                        .AddArgumentListArguments(
                                                            Argument(
                                                                RefExpression(IdentifierName("Vtable")))))),
                                                IdentifierName("AddRef_1")))))),
                            OutArgument(
                                PrefixUnaryExpression(
                                    SyntaxKind.PointerIndirectionExpression,
                                    CastExpression(
                                        PointerType(ParseTypeName("nint")),
                                        PrefixUnaryExpression(
                                            SyntaxKind.AddressOfExpression,
                                            MemberAccessExpression(
                                                SyntaxKind.PointerMemberAccessExpression,
                                                ParenthesizedExpression(
                                                    CastExpression(
                                                        PointerType(ParseTypeName("InterfaceImplementationVtable")),
                                                        InvocationExpression(
                                                            MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                TypeSyntaxes.System_Runtime_CompilerServices_Unsafe,
                                                                IdentifierName("AsPointer")))
                                                        .AddArgumentListArguments(
                                                            Argument(
                                                                RefExpression(IdentifierName("Vtable")))))),
                                                IdentifierName("Release_2"))))))));
            }
            else
            {
                // NativeMemory.Copy(StrategyBasedComWrappers.DefaultIUnknownInteraceDetailsStrategy.GetIUnknownDerivedDetails(typeof(<baseInterfaceType>).TypeHandle).ManagedVirtualMethodTable, vtable, (nuint)(sizeof(void*) * <startingOffset>));
                fillBaseInterfaceSlots = Block(
                        MethodInvocationStatement(
                            TypeSyntaxes.System_Runtime_InteropServices_NativeMemory,
                            IdentifierName("Copy"),
                            Argument(
                                MethodInvocation(
                                    TypeSyntaxes.StrategyBasedComWrappers
                                        .Dot(IdentifierName("DefaultIUnknownInterfaceDetailsStrategy")),
                                    IdentifierName("GetIUnknownDerivedDetails"),
                                    Argument( //baseInterfaceTypeInfo.BaseInterface.FullTypeName)),
                                        TypeOfExpression(ParseTypeName(interfaceMethods.Interface.Base.Info.Type.FullTypeName))
                                            .Dot(IdentifierName("TypeHandle"))))
                                    .Dot(IdentifierName("ManagedVirtualMethodTable"))),
                            Argument(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        TypeSyntaxes.System_Runtime_CompilerServices_Unsafe,
                                        IdentifierName("AsPointer")))
                                    .AddArgumentListArguments(
                                        Argument(
                                            RefExpression(IdentifierName("Vtable"))))),
                            Argument(CastExpression(IdentifierName("nuint"),
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.MultiplyExpression,
                                        SizeOfExpression(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))),
                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(interfaceMethods.InheritedMethods.Count() + 3))))))));
            }

            var validDeclaredMethods = interfaceMethods.DeclaredMethods
                    .Where(context => context.UnmanagedToManagedStub.Diagnostics.All(diag => diag.Descriptor.DefaultSeverity != DiagnosticSeverity.Error));

            System.Collections.Generic.List<StatementSyntax> statements = new();

            foreach (var declaredMethodContext in validDeclaredMethods)
            {
                statements.Add(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("Vtable"),
                                IdentifierName($"{declaredMethodContext.MethodInfo.MethodName}_{declaredMethodContext.GenerationContext.VtableIndexData.Index}")),
                            PrefixUnaryExpression(
                                SyntaxKind.AddressOfExpression,
                                IdentifierName($"ABI_{declaredMethodContext.GenerationContext.StubMethodSyntaxTemplate.Identifier}")))));
            }

            return ImplementationInterfaceTemplate
                .AddMembers(
                    FieldDeclaration(
                        VariableDeclaration(ParseTypeName("InterfaceImplementationVtable"))
                        .AddVariables(VariableDeclarator("Vtable")))
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword)))
                    .AddAttributeLists(
                        AttributeList(
                            SingletonSeparatedList(
                                Attribute(NameSyntaxes.System_Runtime_CompilerServices_FixedAddressValueTypeAttribute)))),
                    ConstructorDeclaration("InterfaceImplementation")
                        .AddModifiers(Token(SyntaxKind.StaticKeyword))
                        .WithBody(
                            Block(
                                fillBaseInterfaceSlots,
                                Block(statements))));
        }

        private static readonly ClassDeclarationSyntax InterfaceInformationTypeTemplate =
            ClassDeclaration("InterfaceInformation")
            .AddModifiers(Token(SyntaxKind.FileKeyword), Token(SyntaxKind.UnsafeKeyword))
            .AddBaseListTypes(SimpleBaseType(TypeSyntaxes.IIUnknownInterfaceType));

        private static ClassDeclarationSyntax GenerateInterfaceInformation(ComInterfaceInfo context, CancellationToken _)
        {
            ClassDeclarationSyntax interfaceInformationType = InterfaceInformationTypeTemplate
                .AddMembers(
                    // public static System.Guid Iid { get; } = new(<embeddedDataBlob>);
                    PropertyDeclaration(TypeSyntaxes.System_Guid, "Iid")
                        .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                        .AddAccessorListAccessors(
                            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))
                        .WithInitializer(
                            EqualsValueClause(
                                ImplicitObjectCreationExpression()
                                    .AddArgumentListArguments(
                                        Argument(CreateEmbeddedDataBlobCreationStatement(context.InterfaceId.ToByteArray())))))
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

            if (context.Options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper))
            {
                return interfaceInformationType.AddMembers(
                        // public static void** VirtualMethodTableManagedImplementation => (void**)System.Runtime.CompilerServices.Unsafe.AsPointer(in InterfaceImplementation.Vtable);
                        PropertyDeclaration(TypeSyntaxes.VoidStarStar, "ManagedVirtualMethodTable")
                            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                            .WithExpressionBody(
                                ArrowExpressionClause(
                                    CastExpression(
                                        PointerType(PointerType(ParseTypeName("void"))),
                                        InvocationExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                TypeSyntaxes.System_Runtime_CompilerServices_Unsafe,
                                                IdentifierName("AsPointer")))
                                        .AddArgumentListArguments(
                                            InArgument(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("InterfaceImplementation"),
                                                    IdentifierName("Vtable")))))))
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
            }

            return interfaceInformationType.AddMembers(
                PropertyDeclaration(TypeSyntaxes.VoidStarStar, "ManagedVirtualMethodTable")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                    .WithExpressionBody(ArrowExpressionClause(LiteralExpression(SyntaxKind.NullLiteralExpression)))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));


            static ExpressionSyntax CreateEmbeddedDataBlobCreationStatement(ReadOnlySpan<byte> bytes)
            {
                var literals = new CollectionElementSyntax[bytes.Length];

                for (int i = 0; i < bytes.Length; i++)
                {
                    literals[i] = ExpressionElement(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(bytes[i])));
                }

                // [ <byte literals> ]
                return CollectionExpression(SeparatedList(literals));
            }
        }
    }
}
