// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
                ComInterfaceDispatchMarshallingInfo.Instance,
                ClassifyMemberKind(symbol));
        }

        private static StubMemberKind ClassifyMemberKind(IMethodSymbol symbol) => (symbol.MethodKind, symbol.AssociatedSymbol) switch
        {
            (MethodKind.PropertyGet, IPropertySymbol { IsIndexer: true }) => StubMemberKind.IndexerGetter,
            (MethodKind.PropertySet, IPropertySymbol { IsIndexer: true }) => StubMemberKind.IndexerSetter,
            (MethodKind.PropertyGet, _) => StubMemberKind.PropertyGetter,
            (MethodKind.PropertySet, _) => StubMemberKind.PropertySetter,
            _ => StubMemberKind.Method,
        };

        internal static IncrementalMethodStubGenerationContext CalculateStubInformation(MemberDeclarationSyntax? syntax, IMethodSymbol symbol, int index, StubEnvironment environment, ComInterfaceInfo owningInterface, CancellationToken ct)
        {
            ISignatureDiagnosticLocations locations = syntax switch
            {
                null => NoneSignatureDiagnosticLocations.Instance,
                MethodDeclarationSyntax methodSyntax => new MethodSignatureDiagnosticLocations(methodSyntax),
                PropertyDeclarationSyntax propertySyntax => CreatePropertyAccessorDiagnosticLocations(propertySyntax, symbol),
                IndexerDeclarationSyntax indexerSyntax => CreateIndexerAccessorDiagnosticLocations(indexerSyntax, symbol),
                _ => throw new UnreachableException(),
            };

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
            ContainingSyntax methodSyntaxTemplate = syntax switch
            {
                MethodDeclarationSyntax methodSyntax => new ContainingSyntax(
                    new SyntaxTokenList(methodSyntax.Modifiers.Where(static m => !m.IsKind(SyntaxKind.NewKeyword) && !m.IsKind(SyntaxKind.PartialKeyword) && !m.IsKind(SyntaxKind.VirtualKeyword))).StripAccessibilityModifiers(),
                    SyntaxKind.MethodDeclaration,
                    methodSyntax.Identifier,
                    methodSyntax.TypeParameterList),
                // Property / indexer accessors are emitted as plain methods named e.g. 'get_Foo' / 'set_Foo'
                // ('get_Item' / 'set_Item' for indexers, or the [IndexerName]-renamed value).
                PropertyDeclarationSyntax or IndexerDeclarationSyntax => new ContainingSyntax(
                    TokenList(),
                    SyntaxKind.MethodDeclaration,
                    Identifier(symbol.Name),
                    typeParameters: null),
                _ => throw new UnreachableException(),
            };

            StubMemberKind memberKind = ClassifyMemberKind(symbol);

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
                ComInterfaceDispatchMarshallingInfo.Instance,
                memberKind);
        }

        // For a property accessor, the user-visible source location is the property's identifier.
        // The getter has no managed parameters; the setter has the implicit 'value' parameter which we report at
        // the property identifier (it has no source location of its own).
        private static MethodSignatureDiagnosticLocations CreatePropertyAccessorDiagnosticLocations(PropertyDeclarationSyntax propertySyntax, IMethodSymbol accessor)
        {
            Location identifierLocation = propertySyntax.Identifier.GetLocation();
            ImmutableArray<Location> parameterLocations = accessor.MethodKind is MethodKind.PropertySet
                ? ImmutableArray.Create(identifierLocation)
                : ImmutableArray<Location>.Empty;
            return new MethodSignatureDiagnosticLocations(accessor.Name, parameterLocations, identifierLocation);
        }

        // For an indexer accessor, the user-visible source location is the 'this' keyword (indexers have no
        // identifier token). Diagnostics that index into ManagedParameterLocations must see one entry per
        // index parameter, plus the implicit 'value' parameter for the setter, with 'value' falling back to
        // the 'this' location since it has no syntactic representation.
        private static MethodSignatureDiagnosticLocations CreateIndexerAccessorDiagnosticLocations(IndexerDeclarationSyntax indexerSyntax, IMethodSymbol accessor)
        {
            Location thisLocation = indexerSyntax.ThisKeyword.GetLocation();
            var indexParameters = indexerSyntax.ParameterList.Parameters;
            int parameterCount = accessor.MethodKind is MethodKind.PropertySet
                ? indexParameters.Count + 1
                : indexParameters.Count;
            var builder = ImmutableArray.CreateBuilder<Location>(parameterCount);
            foreach (var parameter in indexParameters)
            {
                builder.Add(parameter.GetLocation());
            }
            if (accessor.MethodKind is MethodKind.PropertySet)
            {
                builder.Add(thisLocation);
            }
            return new MethodSignatureDiagnosticLocations(accessor.Name, builder.MoveToImmutable(), thisLocation);
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

            BasePropertyDeclarationSyntax? bufferedDeclaredGetter = null;
            foreach (ComMethodContext declaredMethod in data.DeclaredMethods)
            {
                if (declaredMethod.ManagedToUnmanagedStub is GeneratedStubCodeContext managedToUnmanagedContext)
                {
                    EmitMemberHonoringPropertyMerge(writer, managedToUnmanagedContext.Stub.Node, ref bufferedDeclaredGetter);
                }

                if (declaredMethod.UnmanagedToManagedStub is GeneratedStubCodeContext unmanagedToManagedContext &&
                    unmanagedToManagedContext.Diagnostics.All(static d => d.Descriptor.DefaultSeverity != DiagnosticSeverity.Error))
                {
                    writer.InnerWriter.WriteLine();
                    writer.WriteMultilineNode(unmanagedToManagedContext.Stub.Node.NormalizeWhitespace());
                }
            }
            FlushBufferedPropertyGetter(writer, ref bufferedDeclaredGetter);

            BasePropertyDeclarationSyntax? bufferedShadowGetter = null;
            string derivedInterfaceName = data.Interface.Info.Type.FullTypeName;
            foreach (ComMethodContext inheritedStub in data.InheritedMethods)
            {
                if (inheritedStub is not { IsExternallyDefined: false, ManagedToUnmanagedStub: GeneratedStubCodeContext shadowImplementationContextContext })
                {
                    continue;
                }

                MemberDeclarationSyntax stubNode = shadowImplementationContextContext.Stub.Node;
                if (stubNode is BasePropertyDeclarationSyntax basePropertyNode)
                {
                    // The accessor stub was generated for the base interface; rewrite its explicit-interface
                    // specifier to point at the derived interface before emitting/merging. Both property and
                    // indexer declarations expose a WithExplicitInterfaceSpecifier on the base type.
                    basePropertyNode = basePropertyNode.WithExplicitInterfaceSpecifier(
                        ExplicitInterfaceSpecifier(ParseName(derivedInterfaceName)));
                    EmitMemberHonoringPropertyMerge(writer, basePropertyNode, ref bufferedShadowGetter);
                }
                else if (stubNode is MethodDeclarationSyntax methodNode)
                {
                    FlushBufferedPropertyGetter(writer, ref bufferedShadowGetter);
                    MethodDeclarationSyntax preparedNode = methodNode
                        .WithExplicitInterfaceSpecifier(
                            ExplicitInterfaceSpecifier(ParseName(derivedInterfaceName)))
                        .NormalizeWhitespace();
                    writer.InnerWriter.WriteLine();
                    writer.WriteMultilineNode(preparedNode);
                }
            }
            FlushBufferedPropertyGetter(writer, ref bufferedShadowGetter);

            BasePropertyDeclarationSyntax? bufferedUnreachableGetter = null;
            foreach (ComMethodContext inheritedStub in data.InheritedMethods)
            {
                if (inheritedStub.IsExternallyDefined)
                {
                    continue;
                }

                if (inheritedStub.GenerationContext is { MemberKind: var kind } && kind.IsPropertyOrIndexerAccessor())
                {
                    // Property/indexer accessors must be emitted as one explicit-interface declaration per
                    // get/set pair. Synthesize a single-accessor declaration here and let the merge helper
                    // collapse a getter+setter pair into one declaration.
                    BasePropertyDeclarationSyntax synthesized = SynthesizeUnreachableInheritedPropertyAccessor(inheritedStub);
                    EmitMemberHonoringPropertyMerge(writer, synthesized, ref bufferedUnreachableGetter);
                    continue;
                }

                FlushBufferedPropertyGetter(writer, ref bufferedUnreachableGetter);
                writer.InnerWriter.WriteLine();
                writer.Write($"{inheritedStub.GenerationContext.SignatureContext.StubReturnType} {inheritedStub.OriginalDeclaringInterface.Info.Type.FullTypeName}.{inheritedStub.MethodInfo.MethodName}");
                writer.Write($"({string.Join(", ", inheritedStub.GenerationContext.SignatureContext.StubParameters.Select(p => p.NormalizeWhitespace().ToString()))})");
                writer.WriteLine(" => throw new global::System.Diagnostics.UnreachableException();");
            }
            FlushBufferedPropertyGetter(writer, ref bufferedUnreachableGetter);

            writer.Indent--;
            writer.WriteLine('}');
        }

        private static void EmitMemberHonoringPropertyMerge(
            IndentedTextWriter writer,
            MemberDeclarationSyntax node,
            ref BasePropertyDeclarationSyntax? bufferedGetter)
        {
            // Property and indexer accessor stubs arrive one per accessor (get then set when both exist).
            // Merge consecutive get+set halves of the same property/indexer into a single declaration
            // before emitting, so the resulting code is a valid explicit interface implementation.
            //
            // Three cases below:
            //   (1) Incoming node is a getter accessor — flush any prior buffered getter
            //       (an orphan with no matching setter) and stash this one to wait for a paired setter.
            //   (2) Incoming node is a setter accessor that pairs with the buffered getter (same
            //       target property/indexer) — merge them into a single declaration and emit.
            //   (3) Anything else (orphan setter, setter targeting a different property/indexer than
            //       the buffered getter, or a non-property syntax node) — flush any buffered getter
            //       and emit the incoming node as-is.
            if (node is BasePropertyDeclarationSyntax basePropertyDecl)
            {
                bool isGetter = basePropertyDecl.AccessorList!.Accessors[0].Kind() is SyntaxKind.GetAccessorDeclaration;
                if (isGetter)
                {
                    // Case (1): buffer the getter; wait for a possible paired setter.
                    FlushBufferedPropertyGetter(writer, ref bufferedGetter);
                    bufferedGetter = basePropertyDecl;
                    return;
                }
                if (bufferedGetter is not null
                    && IsSameAccessorTarget(bufferedGetter, basePropertyDecl))
                {
                    // Case (2): setter pairs with the buffered getter — merge and emit one declaration.
                    BasePropertyDeclarationSyntax merged = MergePropertyAccessors(bufferedGetter, basePropertyDecl);
                    writer.InnerWriter.WriteLine();
                    writer.WriteMultilineNode(merged.NormalizeWhitespace());
                    bufferedGetter = null;
                    return;
                }
            }
            // Case (3): flush any buffered getter and emit the incoming node as-is.
            FlushBufferedPropertyGetter(writer, ref bufferedGetter);
            writer.InnerWriter.WriteLine();
            writer.WriteMultilineNode(node.NormalizeWhitespace());
        }

        // The buffer holds either a property getter or an indexer getter. Two consecutive accessor stubs
        // merge into one declaration iff they target the SAME underlying property/indexer:
        //  - same explicit-interface specifier (or both unqualified),
        //  - same syntactic shape (both PropertyDeclaration or both IndexerDeclaration),
        //  - for properties: same identifier text (the property name).
        //  - for indexers: same index-parameter type signature (overloads must NOT cross-pair).
        private static bool IsSameAccessorTarget(BasePropertyDeclarationSyntax getter, BasePropertyDeclarationSyntax setter)
        {
            string getterExplicit = getter.ExplicitInterfaceSpecifier?.Name.ToString() ?? string.Empty;
            string setterExplicit = setter.ExplicitInterfaceSpecifier?.Name.ToString() ?? string.Empty;
            if (getterExplicit != setterExplicit)
            {
                return false;
            }
            return (getter, setter) switch
            {
                (PropertyDeclarationSyntax g, PropertyDeclarationSyntax s) => g.Identifier.Text == s.Identifier.Text,
                (IndexerDeclarationSyntax g, IndexerDeclarationSyntax s) => HaveSameParameterTypes(g.ParameterList, s.ParameterList),
                _ => false,
            };
        }

        private static bool HaveSameParameterTypes(BaseParameterListSyntax a, BaseParameterListSyntax b)
        {
            var aParams = a.Parameters;
            var bParams = b.Parameters;
            if (aParams.Count != bParams.Count)
            {
                return false;
            }
            for (int i = 0; i < aParams.Count; i++)
            {
                if (aParams[i].Type!.NormalizeWhitespace().ToString() != bParams[i].Type!.NormalizeWhitespace().ToString())
                {
                    return false;
                }
            }
            return true;
        }

        private static BasePropertyDeclarationSyntax SynthesizeUnreachableInheritedPropertyAccessor(ComMethodContext inheritedStub)
        {
            IncrementalMethodStubGenerationContext genCtx = inheritedStub.GenerationContext;
            Debug.Assert(genCtx.MemberKind.IsPropertyOrIndexerAccessor());

            bool isSetter = genCtx.MemberKind.IsAccessorSetter();
            bool isIndexer = genCtx.MemberKind.IsIndexerAccessor();

            ImmutableArray<ParameterSyntax> stubParameters = genCtx.SignatureContext.StubParameters.ToImmutableArray();
            TypeSyntax valueType;
            ImmutableArray<ParameterSyntax> indexParameters;
            if (isSetter)
            {
                valueType = stubParameters[stubParameters.Length - 1].Type!;
                indexParameters = stubParameters.RemoveAt(stubParameters.Length - 1);
            }
            else
            {
                valueType = genCtx.SignatureContext.StubReturnType;
                indexParameters = stubParameters;
            }

            AccessorDeclarationSyntax accessor = AccessorDeclaration(
                isSetter ? SyntaxKind.SetAccessorDeclaration : SyntaxKind.GetAccessorDeclaration)
                .WithExpressionBody(ArrowExpressionClause(
                    ThrowExpression(
                        ObjectCreationExpression(ParseTypeName("global::System.Diagnostics.UnreachableException"))
                            .WithArgumentList(ArgumentList()))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

            ExplicitInterfaceSpecifierSyntax explicitSpecifier = ExplicitInterfaceSpecifier(
                ParseName(inheritedStub.OriginalDeclaringInterface.Info.Type.FullTypeName));

            if (isIndexer)
            {
                return IndexerDeclaration(valueType)
                    .WithExplicitInterfaceSpecifier(explicitSpecifier)
                    .WithParameterList(BracketedParameterList(SeparatedList(indexParameters)))
                    .WithAccessorList(AccessorList(SingletonList(accessor)));
            }

            string accessorName = inheritedStub.MethodInfo.MethodName;
            string propertyName = IncrementalMethodStubGenerationContext.GetPropertyNameFromAccessor(accessorName);

            return PropertyDeclaration(valueType, Identifier(propertyName))
                .WithExplicitInterfaceSpecifier(explicitSpecifier)
                .WithAccessorList(AccessorList(SingletonList(accessor)));
        }

        private static void FlushBufferedPropertyGetter(IndentedTextWriter writer, ref BasePropertyDeclarationSyntax? bufferedGetter)
        {
            if (bufferedGetter is null)
            {
                return;
            }
            writer.InnerWriter.WriteLine();
            writer.WriteMultilineNode(bufferedGetter.NormalizeWhitespace());
            bufferedGetter = null;
        }

        private static BasePropertyDeclarationSyntax MergePropertyAccessors(
            BasePropertyDeclarationSyntax getter,
            BasePropertyDeclarationSyntax setter)
        {
            var combined = new List<AccessorDeclarationSyntax>(2);
            combined.AddRange(getter.AccessorList!.Accessors);
            combined.AddRange(setter.AccessorList!.Accessors);
            return getter.WithAccessorList(AccessorList(List(combined)));
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

                // Buffered getter state for merging consecutive get+set pairs into one declaration.
                // For ordinary properties IndexParamList / IndexArgList are null; for indexers they hold
                // the formatted parameter list (e.g. "int i, string s") and the argument-forwarding list
                // (e.g. "i, s") respectively. The parameter list also serves as part of the merge identity
                // so that overloaded indexers do not accidentally cross-pair.
                (string? PropName, string? DeclaringType, string? PropType,
                    SequenceEqualImmutableArray<AttributeInfo> PropAttrs,
                    string? IndexParamList, string? IndexArgList) pendingGetter = default;

                foreach (ComMethodContext shadow in shadowingMethods)
                {
                    IncrementalMethodStubGenerationContext generationContext = shadow.GenerationContext;
                    SignatureContext sigContext = generationContext.SignatureContext;

                    if (generationContext.MemberKind.IsPropertyOrIndexerAccessor())
                    {
                        bool isSetter = generationContext.MemberKind.IsAccessorSetter();
                        bool isIndexer = generationContext.MemberKind.IsIndexerAccessor();
                        string accessorName = shadow.MethodInfo.MethodName;
                        string propName = IncrementalMethodStubGenerationContext.GetPropertyNameFromAccessor(accessorName);
                        string declaringType = shadow.OriginalDeclaringInterface.Info.Type.FullTypeName;

                        // Materialize the parameter sequences once — StubParameters / ManagedParameters
                        // are IEnumerable<T>, not lists.
                        ImmutableArray<ParameterSyntax> stubParams = sigContext.StubParameters.ToImmutableArray();
                        ImmutableArray<TypePositionInfo> managedParams = sigContext.ManagedParameters.ToImmutableArray();

                        // The value type for an accessor is the StubReturnType for a getter and the LAST
                        // managed parameter type for a setter (the implicit 'value'). For both ordinary
                        // properties (one managed parameter) and indexer setters (index params + value),
                        // the value entry is always last.
                        TypeSyntax valueTypeSyntax = isSetter
                            ? stubParams[stubParams.Length - 1].Type!
                            : sigContext.StubReturnType;
                        string propType = valueTypeSyntax.NormalizeWhitespace().ToString();
                        SequenceEqualImmutableArray<AttributeInfo> propAttrs = shadow.MethodInfo.AssociatedAttributes;

                        string? indexParamList = null;
                        string? indexArgList = null;
                        if (isIndexer)
                        {
                            // For indexers the index parameter list is the StubParameters minus the
                            // implicit value entry (for setters). For getters, all StubParameters are
                            // index parameters.
                            int indexCount = isSetter ? stubParams.Length - 1 : stubParams.Length;
                            indexParamList = string.Join(", ", stubParams.Take(indexCount).Select(p => p.NormalizeWhitespace().ToString()));
                            indexArgList = string.Join(", ", managedParams.Take(indexCount).Select(mp => $"{(mp.IsByRef ? $"{MarshallerHelpers.GetManagedArgumentRefKindKeyword(mp)} " : "")}{mp.InstanceIdentifier}"));
                        }

                        if (!isSetter)
                        {
                            FlushPendingGetter(writer, ref pendingGetter);
                            pendingGetter = (propName, declaringType, propType, propAttrs, indexParamList, indexArgList);
                            continue;
                        }

                        // Setter: try to pair with a buffered getter. Identity includes the index parameter
                        // list so overloaded indexers (same name, different param types) stay separate.
                        if (pendingGetter.PropName == propName
                            && pendingGetter.DeclaringType == declaringType
                            && pendingGetter.IndexParamList == indexParamList)
                        {
                            EmitPropertyAttributes(writer, pendingGetter.PropAttrs);
                            EmitDeclarationHead(writer, pendingGetter.PropType!, pendingGetter.PropName!, pendingGetter.IndexParamList);
                            writer.WriteLine('{');
                            writer.Indent++;
                            EmitAccessor(writer, isSetter: false, pendingGetter.DeclaringType!, pendingGetter.PropName!, pendingGetter.IndexArgList);
                            EmitAccessor(writer, isSetter: true, pendingGetter.DeclaringType!, pendingGetter.PropName!, pendingGetter.IndexArgList);
                            writer.Indent--;
                            writer.WriteLine('}');
                            pendingGetter = default;
                            continue;
                        }

                        FlushPendingGetter(writer, ref pendingGetter);
                        EmitPropertyAttributes(writer, propAttrs);
                        EmitDeclarationHead(writer, propType, propName, indexParamList);
                        writer.WriteLine('{');
                        writer.Indent++;
                        EmitAccessor(writer, isSetter: true, declaringType, propName, indexArgList);
                        writer.Indent--;
                        writer.WriteLine('}');
                        continue;
                    }

                    FlushPendingGetter(writer, ref pendingGetter);

                    // AssociatedAttributes is currently populated only for property/indexer accessors;
                    // ordinary method stubs must not carry any. If this fires, a new producer is feeding
                    // the field for a non-property member and the emitter needs to decide how to consume it.
                    Debug.Assert(shadow.MethodInfo.AssociatedAttributes.Array.IsEmpty);

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

                FlushPendingGetter(writer, ref pendingGetter);

                writer.Indent--;
                writer.WriteLine('}');

                static void FlushPendingGetter(IndentedTextWriter writer, ref (string? PropName, string? DeclaringType, string? PropType, SequenceEqualImmutableArray<AttributeInfo> PropAttrs, string? IndexParamList, string? IndexArgList) pending)
                {
                    if (pending.PropName is null)
                    {
                        return;
                    }
                    EmitPropertyAttributes(writer, pending.PropAttrs);
                    EmitDeclarationHead(writer, pending.PropType!, pending.PropName!, pending.IndexParamList);
                    writer.WriteLine('{');
                    writer.Indent++;
                    EmitAccessor(writer, isSetter: false, pending.DeclaringType!, pending.PropName!, pending.IndexArgList);
                    writer.Indent--;
                    writer.WriteLine('}');
                    pending = default;
                }

                // Writes either `new T Name` (property) or `new T this[<paramList>]` (indexer) on its own line.
                static void EmitDeclarationHead(IndentedTextWriter writer, string propType, string propName, string? indexParamList)
                {
                    if (indexParamList is null)
                    {
                        writer.WriteLine($"new {propType} {propName}");
                    }
                    else
                    {
                        writer.WriteLine($"new {propType} this[{indexParamList}]");
                    }
                }

                // Writes either `get => ((Base)this).Name;` / `set => ((Base)this).Name = value;` for properties
                // or `get => ((Base)this)[<argList>];` / `set => ((Base)this)[<argList>] = value;` for indexers.
                // For indexers the propName isn't part of the access expression (the IL-level naming comes
                // from `[IndexerName]` propagated via AssociatedAttributes).
                static void EmitAccessor(IndentedTextWriter writer, bool isSetter, string declaringType, string propName, string? indexArgList)
                {
                    string access = indexArgList is null
                        ? $"(({declaringType})this).{propName}"
                        : $"(({declaringType})this)[{indexArgList}]";
                    writer.WriteLine(isSetter
                        ? $"set => {access} = value;"
                        : $"get => {access};");
                }

                static void EmitPropertyAttributes(IndentedTextWriter writer, SequenceEqualImmutableArray<AttributeInfo> attrs)
                {
                    // The derived-interface property shadow is a pure C# forwarder: its accessors are
                    // `get => ((Base)this).Prop;` / `set => ((Base)this).Prop = value;`, with no COM call
                    // and therefore no marshalling. Marshalling attributes declared on the source property
                    // are intentionally suppressed here so the shadow only carries semantically-meaningful
                    // user attributes (e.g. attributes used for documentation, tooling, or reflection).
                    //
                    // `[IndexerName("X")]` (for indexers) is NOT marshalling and must be propagated so the
                    // shadow's IL accessor names (get_X / set_X) match the source's — otherwise the runtime
                    // sees `get_Item` / `set_Item` and the shadow loses identity with the base indexer.
                    foreach (AttributeInfo attrInfo in attrs)
                    {
                        if (attrInfo.Type is "global::" + TypeNames.MarshalUsingAttribute
                            or "global::" + TypeNames.System_Runtime_InteropServices_MarshalAsAttribute)
                        {
                            continue;
                        }
                        writer.WriteLine($"[{attrInfo.Type}({string.Join(", ", attrInfo.Arguments)})]");
                    }
                }
            });
        }
    }
}
