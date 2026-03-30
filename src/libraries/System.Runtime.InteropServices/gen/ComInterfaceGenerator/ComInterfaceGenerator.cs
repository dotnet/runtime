// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    [Generator]
    public sealed partial class ComInterfaceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var stubEnvironment = context.CreateStubEnvironmentProvider();
            // Get all types with the [GeneratedComInterface] attribute.
            var attributedInterfaces = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    TypeNames.GeneratedComInterfaceAttribute,
                    static (node, ct) => node is InterfaceDeclarationSyntax,
                    static (context, ct) => context.TargetSymbol is INamedTypeSymbol interfaceSymbol
                        ? ((InterfaceDeclarationSyntax)context.TargetNode, interfaceSymbol)
                        : default)
                .Collect()
                .Combine(stubEnvironment)
                .Select(
                    // Do all the work to get to the IncrementalComInterfaceContext in one step
                    // Intermediate results with symbols won't be incremental, and considering the overhead of setting up the incremental
                    // steps for projects that don't use the generator, we make this tradeoff.
                    static (input, ct) =>
                    {
                        if (input.Left.Length == 0)
                        {
                            return
                            (
                                InterfaceContexts: ImmutableArray<ComInterfaceContext>.Empty.ToSequenceEqual(),
                                MethodContexts: ImmutableArray<ComMethodContext>.Empty.ToSequenceEqual()
                            );
                        }
                        StubEnvironment stubEnvironment = input.Right;
                        List<(ComInterfaceInfo, INamedTypeSymbol)> interfaceInfos = new();
                        HashSet<(ComInterfaceInfo, INamedTypeSymbol)> externalIfaces = new(ComInterfaceInfo.EqualityComparerForExternalIfaces.Instance);
                        foreach (var (syntax, symbol) in input.Left)
                        {
                            var cii = ComInterfaceInfo.From(symbol, syntax, stubEnvironment, CancellationToken.None);
                            if (cii.HasValue)
                                interfaceInfos.Add(cii.Value);
                            var externalBase = ComInterfaceInfo.CreateInterfaceInfoForBaseInterfacesInOtherCompilations(symbol);
                            // Avoid adding duplicates if multiple interfaces derive from the same external interface.
                            if (!externalBase.IsDefaultOrEmpty)
                            {
                                foreach (var b in externalBase)
                                {
                                    externalIfaces.Add(b);
                                }
                            }
                        }
                        interfaceInfos.AddRange(externalIfaces);

                        var comInterfaceContexts = ComInterfaceContext.GetContexts(interfaceInfos.Select(i => i.Item1).ToImmutableArray(), ct);

                        // Get all valid methods from all interfaces
                        Dictionary<ComMethodInfo, IMethodSymbol> methodSymbols = new();
                        List<List<ComMethodInfo>> methods = new();
                        foreach (var cii in interfaceInfos)
                        {
                            var cmi = ComMethodInfo.GetMethodsFromInterface(cii, ct);
                            var inner = new List<ComMethodInfo>();
                            foreach (var m in cmi)
                            {
                                if (m.HasValue)
                                {
                                    inner.Add(m.Value.ComMethod);
                                    methodSymbols.Add(m.Value.ComMethod, m.Value.Symbol);
                                }
                            }
                            methods.Add(inner);
                        }

                        List<(ComInterfaceContext, SequenceEqualImmutableArray<ComMethodInfo>)> ifaceCtxs = new();
                        for (int i = 0; i < interfaceInfos.Count; i++)
                        {
                            var cic = comInterfaceContexts[i];
                            if (cic.HasValue)
                            {
                                ifaceCtxs.Add((cic.Value, methods[i].ToSequenceEqualImmutableArray()));
                            }
                        }

                        var result = ComMethodContext.CalculateAllMethods(ifaceCtxs, ct);

                        List<ComMethodContext> methodContexts = new();
                        foreach (var data in result)
                        {
                            methodContexts.Add(new ComMethodContext(
                                data.Method,
                                data.OwningInterface,
                                CalculateStubInformation(
                                    data.Method.MethodInfo.Syntax,
                                    methodSymbols[data.Method.MethodInfo],
                                    data.Method.Index,
                                    stubEnvironment,
                                    data.OwningInterface.Info,
                                    ct)));
                        }

                        return
                        (
                            InterfaceContexts: ifaceCtxs.Select(x => x.Item1).Where(x => !x.IsExternallyDefined).ToSequenceEqualImmutableArray(),
                            MethodContexts: methodContexts.ToSequenceEqualImmutableArray()
                        );
                    });

            // Create list of methods (inherited and declared) and their owning interface
            var interfaceContextsToGenerate = attributedInterfaces.SelectMany(static (a, ct) => a.InterfaceContexts);
            var comMethodContexts = attributedInterfaces.Select(static (a, ct) => a.MethodContexts);

            var interfaceAndMethodsContexts = comMethodContexts
                .Combine(interfaceContextsToGenerate.Collect())
                .SelectMany(static (data, ct) =>
                    GroupComContextsForInterfaceGeneration(data.Left.Array, data.Right, ct));

            context.RegisterSourceOutput(interfaceAndMethodsContexts, static (context, data) =>
            {
                ComInterfaceContext interfaceContext = data.Interface;

                using StringWriter sw = new();
                using IndentedTextWriter writer = new(sw);
                writer.WriteLine("// <auto-generated />");
                writer.WriteLine("#pragma warning disable CS0612, CS0618, CS0649, CS1591"); // Suppress warnings about [Obsolete], "lack of assignment", and missing XML documentation in generated code.

                // If the user has specified 'ManagedObjectWrapper', it means that the COM interface will never be used to marshal a native
                // object as an RCW (eg. the IDIC vtable will also not be generated, nor any additional supporting code). To reduce binary
                // size, we're not emitting the interface methods on the implementation interface that has '[DynamicInterfaceCastableImplementation]'
                // on it. However, doing so will cause the CA2256 warning to be produced. We can't remove the attribute, as that would cause
                // the wrong exception to be thrown when trying an IDIC cast with this interface (not 'InvalidCastException'). Because this is
                // a niche scenario, and we don't want to regress perf or size, we can just disable the warning instead.
                if (interfaceContext.Options is ComInterfaceOptions.ManagedObjectWrapper)
                {
                    writer.WriteLine("#pragma warning disable CA2256");
                }

                sw.WriteLine();
                WriteImplementationVTableStruct(writer, data);
                sw.WriteLine();
                WriteInterfaceInformation(writer, interfaceContext.Info);
                sw.WriteLine();
                WriteInterfaceImplementation(writer, data);
                sw.WriteLine();
                WriteIUnknownDerivedOriginalInterfacePart(writer, data);

                context.AddSource(interfaceContext.Info.Type.FullTypeName.Replace(TypeNames.GlobalAlias, ""), sw.ToString());
            });
        }

        private static bool IsHResultLikeType(ManagedTypeInfo type)
        {
            string typeName = type.FullTypeName.Split('.', ':')[^1];
            return typeName.Equals("hr", StringComparison.OrdinalIgnoreCase)
                || typeName.Equals("hresult", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Calculates the shared information needed for both source-available and sourceless stub generation.
        /// </summary>
        private static IncrementalMethodStubGenerationContext CalculateSharedStubInformation(
            IMethodSymbol symbol,
            int index,
            StubEnvironment environment,
            ISignatureDiagnosticLocations diagnosticLocations,
            ComInterfaceInfo owningInterfaceInfo,
            CancellationToken ct)
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

            var generatorDiagnostics = new GeneratorDiagnosticsBag(new DiagnosticDescriptorProvider(), diagnosticLocations, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.ComInterfaceGenerator.SR));

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
                typeof(ComInterfaceGenerator).Assembly);

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
                        new TypePositionInfo(SpecialTypeInfo.Int32, new ManagedHResultExceptionMarshallingInfo(owningInterfaceInfo.InterfaceId))
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

            ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> callConv = VirtualMethodPointerStubGenerator.GenerateCallConvSyntaxFromAttributes(
                suppressGCTransitionAttribute,
                unmanagedCallConvAttribute,
                ImmutableArray.Create(FunctionPointerUnmanagedCallingConvention(Identifier("MemberFunction"))));

            var declaringType = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(symbol.ContainingType);

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
                diagnosticLocations,
                callConv.ToSequenceEqualImmutableArray(SyntaxEquivalentComparer.Instance),
                new VirtualMethodIndexData(index, ImplicitThisParameter: true, direction, true, ExceptionMarshalling.Com),
                exceptionMarshallingInfo,
                environment.EnvironmentFlags,
                owningInterfaceInfo.Type,
                declaringType,
                generatorDiagnostics.Diagnostics.ToSequenceEqualImmutableArray(),
                ComInterfaceDispatchMarshallingInfo.Instance);
        }

        internal static IncrementalMethodStubGenerationContext CalculateStubInformation(MethodDeclarationSyntax? syntax, IMethodSymbol symbol, int index, StubEnvironment environment, ComInterfaceInfo owningInterface, CancellationToken ct)
        {
            ISignatureDiagnosticLocations locations = syntax is null
                ? NoneSignatureDiagnosticLocations.Instance
                : new MethodSignatureDiagnosticLocations(syntax);

            var sourcelessStubInformation = CalculateSharedStubInformation(
                symbol,
                index,
                environment,
                locations,
                owningInterface,
                ct);

            if (syntax is null)
                return sourcelessStubInformation;

            var containingSyntaxContext = new ContainingSyntaxContext(syntax);
            var methodSyntaxTemplate = new ContainingSyntax(
                new SyntaxTokenList(syntax.Modifiers.Where(static m => !m.IsKind(SyntaxKind.NewKeyword))).StripAccessibilityModifiers(),
                SyntaxKind.MethodDeclaration,
                syntax.Identifier,
                syntax.TypeParameterList);

            return new SourceAvailableIncrementalMethodStubGenerationContext(
                sourcelessStubInformation.SignatureContext,
                containingSyntaxContext,
                methodSyntaxTemplate,
                locations,
                sourcelessStubInformation.CallingConvention,
                sourcelessStubInformation.VtableIndexData,
                sourcelessStubInformation.ExceptionMarshallingInfo,
                sourcelessStubInformation.EnvironmentFlags,
                sourcelessStubInformation.TypeKeyOwner,
                sourcelessStubInformation.DeclaringType,
                sourcelessStubInformation.Diagnostics,
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

        private static void WriteImplementationVTableStruct(IndentedTextWriter writer, ComInterfaceAndMethodsContext interfaceMethods)
        {
            writer.WriteLine("[global::System.Runtime.InteropServices.StructLayoutAttribute(global::System.Runtime.InteropServices.LayoutKind.Sequential)]");
            writer.WriteLine("file unsafe struct InterfaceImplementationVtable");
            writer.WriteLine('{');
            writer.Indent++;
            writer.WriteLine("public delegate* unmanaged[MemberFunction]<void*, global::System.Guid*, void**, int> QueryInterface_0;");
            writer.WriteLine("public delegate* unmanaged[MemberFunction]<void*, uint> AddRef_1;");
            writer.WriteLine("public delegate* unmanaged[MemberFunction]<void*, uint> Release_2;");
            if (interfaceMethods.Interface.Base is not null)
            {
                foreach (ComMethodContext inheritedMethod in interfaceMethods.InheritedMethods)
                {
                    FunctionPointerTypeSyntax functionPointerType = VirtualMethodPointerStubGenerator.GenerateUnmanagedFunctionPointerTypeForMethod(
                        inheritedMethod.GenerationContext,
                        ComInterfaceGeneratorHelpers.GetGeneratorResolver);

                    writer.WriteLine($"public {functionPointerType.NormalizeWhitespace()} {inheritedMethod.MethodInfo.MethodName}_{inheritedMethod.GenerationContext.VtableIndexData.Index};");
                }
            }

            foreach (ComMethodContext declaredMethod in
                interfaceMethods.DeclaredMethods
                    .Where(context => context.UnmanagedToManagedStub.Diagnostics.All(diag => diag.Descriptor.DefaultSeverity != DiagnosticSeverity.Error)))
            {
                FunctionPointerTypeSyntax functionPointerType = VirtualMethodPointerStubGenerator.GenerateUnmanagedFunctionPointerTypeForMethod(
                    declaredMethod.GenerationContext,
                    ComInterfaceGeneratorHelpers.GetGeneratorResolver);

                writer.WriteLine($"public {functionPointerType.NormalizeWhitespace()} {declaredMethod.MethodInfo.MethodName}_{declaredMethod.GenerationContext.VtableIndexData.Index};");
            }

            writer.Indent--;
            writer.WriteLine('}');
        }

        private static void WriteInterfaceInformation(IndentedTextWriter writer, ComInterfaceInfo interfaceInfo)
        {
            writer.WriteLine("file unsafe sealed class InterfaceInformation : global::System.Runtime.InteropServices.Marshalling.IIUnknownInterfaceType");
            writer.WriteLine('{');
            writer.Indent++;
            writer.WriteLine($"public static global::System.Guid Iid {{ get; }} = new([{string.Join(", ", interfaceInfo.InterfaceId.ToByteArray())}]);");
            writer.WriteLine($"public static void** ManagedVirtualMethodTable => {(interfaceInfo.Options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper) ? "(void**)global::System.Runtime.CompilerServices.Unsafe.AsPointer(in InterfaceImplementation.Vtable)" : "null")};");
            writer.Indent--;
            writer.WriteLine('}');
        }

        private static void WriteInterfaceImplementation(IndentedTextWriter writer, ComInterfaceAndMethodsContext data)
        {
            writer.WriteLine("[global::System.Runtime.InteropServices.DynamicInterfaceCastableImplementationAttribute]");
            writer.WriteLine($"file unsafe interface InterfaceImplementation : {data.Interface.Info.Type.FullTypeName}");
            writer.WriteLine('{');
            writer.Indent++;

            if (data.Interface.Options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper))
            {
                writer.WriteLine("[global::System.Runtime.CompilerServices.FixedAddressValueTypeAttribute]");
                writer.WriteLine("public static readonly InterfaceImplementationVtable Vtable;");
                writer.InnerWriter.WriteLine();
                writer.WriteLine("static InterfaceImplementation()");
                writer.WriteLine('{');
                writer.Indent++;

                if (data.Interface.Base is { } baseInterface)
                {
                    writer.WriteLine("global::System.Runtime.InteropServices.NativeMemory.Copy(");
                    writer.Indent++;
                    writer.WriteLine($"global::System.Runtime.InteropServices.Marshalling.StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof({baseInterface.Info.Type.FullTypeName}).TypeHandle).ManagedVirtualMethodTable,");
                    writer.WriteLine("global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref Vtable),");
                    writer.WriteLine($"(nuint)(sizeof(void*) * {data.BaseVTableSize}));");
                    writer.Indent--;
                }
                else
                {
                    // If we don't have a base interface, we need to manually fill in the base IUnknown slots.
                    writer.WriteLine("global::System.Runtime.InteropServices.ComWrappers.GetIUnknownImpl(");
                    writer.Indent++;
                    writer.WriteLine("out *(nint*)&((InterfaceImplementationVtable*)global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref Vtable))->QueryInterface_0,");
                    writer.WriteLine("out *(nint*)&((InterfaceImplementationVtable*)global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref Vtable))->AddRef_1,");
                    writer.WriteLine("out *(nint*)&((InterfaceImplementationVtable*)global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref Vtable))->Release_2);");
                    writer.Indent--;
                }

                writer.InnerWriter.WriteLine();

                foreach (ComMethodContext declaredMethodContext in data.DeclaredMethods
                    .Where(context => context.UnmanagedToManagedStub.Diagnostics.All(diag => diag.Descriptor.DefaultSeverity != DiagnosticSeverity.Error)))
                {
                    writer.WriteLine($"Vtable.{declaredMethodContext.MethodInfo.MethodName}_{declaredMethodContext.GenerationContext.VtableIndexData.Index} = &ABI_{((SourceAvailableIncrementalMethodStubGenerationContext)declaredMethodContext.GenerationContext).StubMethodSyntaxTemplate.Identifier};");
                }

                writer.Indent--;
                writer.WriteLine('}');
            }

            foreach (ComMethodContext declaredMethod in data.DeclaredMethods)
            {
                if (declaredMethod.ManagedToUnmanagedStub is GeneratedStubCodeContext managedToUnmanagedContext)
                {
                    writer.InnerWriter.WriteLine();
                    writer.WriteMultilineNode(managedToUnmanagedContext.Stub.Node.NormalizeWhitespace());
                }

                if (declaredMethod.UnmanagedToManagedStub is GeneratedStubCodeContext unmanagedToManagedContext &&
                    unmanagedToManagedContext.Diagnostics.All(static d => d.Descriptor.DefaultSeverity != DiagnosticSeverity.Error))
                {
                    writer.InnerWriter.WriteLine();
                    writer.WriteMultilineNode(unmanagedToManagedContext.Stub.Node.NormalizeWhitespace());
                }
            }

            foreach (ComMethodContext inheritedStub in data.InheritedMethods)
            {
                if (inheritedStub is not { IsExternallyDefined: false, ManagedToUnmanagedStub: GeneratedStubCodeContext shadowImplementationContextContext })
                {
                    continue;
                }

                MethodDeclarationSyntax preparedNode = shadowImplementationContextContext.Stub.Node
                    .WithExplicitInterfaceSpecifier(
                        ExplicitInterfaceSpecifier(ParseName(data.Interface.Info.Type.FullTypeName)))
                    .NormalizeWhitespace();

                writer.InnerWriter.WriteLine();
                writer.WriteMultilineNode(preparedNode);
            }

            foreach (ComMethodContext inheritedStub in data.InheritedMethods)
            {
                if (inheritedStub.IsExternallyDefined)
                {
                    continue;
                }

                writer.InnerWriter.WriteLine();
                writer.Write($"{inheritedStub.GenerationContext.SignatureContext.StubReturnType} {inheritedStub.OriginalDeclaringInterface.Info.Type.FullTypeName}.{inheritedStub.MethodInfo.MethodName}");
                writer.Write($"({string.Join(", ", inheritedStub.GenerationContext.SignatureContext.StubParameters.Select(p => p.NormalizeWhitespace().ToString()))})");
                writer.WriteLine(" => throw new global::System.Diagnostics.UnreachableException();");
            }

            writer.Indent--;
            writer.WriteLine('}');
        }

        private static void WriteIUnknownDerivedOriginalInterfacePart(IndentedTextWriter writer, ComInterfaceAndMethodsContext data)
        {
            data.Interface.Info.TypeDefinitionContext.WriteToWithUnsafeModifier(writer, (data.Interface.Info.ContainingSyntax, data.ShadowingMethods), static (writer, data) =>
            {
                (ContainingSyntax syntax, IEnumerable<ComMethodContext>? shadowingMethods) = data;

                writer.WriteLine("[global::System.Runtime.InteropServices.Marshalling.IUnknownDerivedAttribute<InterfaceInformation, InterfaceImplementation>]");
                writer.WriteLine($"{string.Join(" ", syntax.Modifiers.AddToModifiers(SyntaxKind.UnsafeKeyword))} {syntax.TypeKind.GetDeclarationKeyword()} {syntax.Identifier}{syntax.TypeParameters}");
                writer.WriteLine('{');
                writer.Indent++;

                foreach (ComMethodContext shadow in shadowingMethods)
                {
                    IncrementalMethodStubGenerationContext generationContext = shadow.GenerationContext;
                    SignatureContext sigContext = generationContext.SignatureContext;

                    foreach (AttributeListSyntax additionalAttr in sigContext.AdditionalAttributes)
                    {
                        writer.WriteLine(additionalAttr.NormalizeWhitespace().ToString());
                    }

                    foreach (AttributeInfo attrInfo in shadow.MethodInfo.Attributes)
                    {
                        writer.WriteLine($"[{attrInfo.Type}({string.Join(", ", attrInfo.Arguments)})]");
                    }

                    writer.Write($"new {sigContext.StubReturnType} {shadow.MethodInfo.MethodName}");
                    writer.Write($"({string.Join(", ", sigContext.StubParameters.Select(p => p.NormalizeWhitespace().ToString()))})");
                    writer.Write($" => (({shadow.OriginalDeclaringInterface.Info.Type.FullTypeName})this).{shadow.MethodInfo.MethodName}");
                    writer.WriteLine($"({string.Join(", ", sigContext.ManagedParameters.Select(mp => $"{(mp.IsByRef ? $"{MarshallerHelpers.GetManagedArgumentRefKindKeyword(mp)} " : "")}{mp.InstanceIdentifier}"))});");
                }

                writer.Indent--;
                writer.WriteLine('}');
            });
        }
    }
}
