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
            var interfaceSymbolsWithoutDiagnostics = context.FilterAndReportDiagnostics(interfaceSymbolOrDiagnostics);

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
                });

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
                .Combine(interfaceContexts.Collect())
                .SelectMany((data, ct) => GroupComContextsForInterfaceGeneration(data.Left, data.Right, ct));

            // Generate the code for the managed-to-unmanaged stubs.
            var managedToNativeInterfaceImplementations = interfaceAndMethodsContexts
                .Select(GenerateImplementationInterface)
                .WithTrackingName(StepNames.GenerateManagedToNativeInterfaceImplementation)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            // Generate the code for the unmanaged-to-managed stubs.
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
            var nativeInterfaceInformation = interfaceContexts
                .Select(static (data, ct) => data.Info)
                .Select(GenerateInterfaceInformation)
                .WithTrackingName(StepNames.GenerateInterfaceInformation)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            var shadowingMethods = interfaceAndMethodsContexts
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

            var iUnknownDerivedAttributeApplication = interfaceContexts
                .Select(static (data, ct) => data.Info)
                .Select(GenerateIUnknownDerivedAttributeApplication)
                .WithTrackingName(StepNames.GenerateIUnknownDerivedAttribute)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            var filesToGenerate = interfaceContexts
                .Zip(nativeInterfaceInformation)
                .Zip(managedToNativeInterfaceImplementations)
                .Zip(nativeToManagedVtableMethods)
                .Zip(nativeToManagedVtables)
                .Zip(iUnknownDerivedAttributeApplication)
                .Zip(shadowingMethods)
                .Select(static (data, ct) =>
                {
                    var ((((((interfaceContext, interfaceInfo), managedToNativeStubs), nativeToManagedStubs), nativeToManagedVtable), iUnknownDerivedAttribute), shadowingMethod) = data;

                    using StringWriter source = new();
                    source.WriteLine("// <auto-generated />");
                    source.WriteLine("#pragma warning disable CS0612, CS0618"); // Suppress warnings about [Obsolete] member usage in generated code.
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

            var methodSyntaxTemplate = new ContainingSyntax(syntax.Modifiers.StripAccessibilityModifiers(), SyntaxKind.MethodDeclaration, syntax.Identifier, syntax.TypeParameterList);

            ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> callConv = VirtualMethodPointerStubGenerator.GenerateCallConvSyntaxFromAttributes(
                suppressGCTransitionAttribute,
                unmanagedCallConvAttribute,
                ImmutableArray.Create(FunctionPointerUnmanagedCallingConvention(Identifier("MemberFunction"))));

            var declaringType = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(symbol.ContainingType);

            var virtualMethodIndexData = new VirtualMethodIndexData(index, ImplicitThisParameter: true, direction, true, ExceptionMarshalling.Com);

            return new IncrementalMethodStubGenerationContext(
                signatureContext,
                containingSyntaxContext,
                methodSyntaxTemplate,
                locations,
                callConv.ToSequenceEqualImmutableArray(SyntaxEquivalentComparer.Instance),
                virtualMethodIndexData,
                new ComExceptionMarshalling(),
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
                    methodList.Add(methods[methodIndex++]);
                }
                contextList.Add(new(iface, methodList.ToImmutable().ToSequenceEqual()));
            }
            return contextList.ToImmutable();
        }

        private static readonly InterfaceDeclarationSyntax ImplementationInterfaceTemplate = InterfaceDeclaration("InterfaceImplementation")
                .WithModifiers(TokenList(Token(SyntaxKind.FileKeyword), Token(SyntaxKind.UnsafeKeyword), Token(SyntaxKind.PartialKeyword)));

        private static InterfaceDeclarationSyntax GenerateImplementationInterface(ComInterfaceAndMethodsContext interfaceGroup, CancellationToken _)
        {
            var definingType = interfaceGroup.Interface.Info.Type;
            var shadowImplementations = interfaceGroup.ShadowingMethods.Select(m => (Method: m, ManagedToUnmanagedStub: m.ManagedToUnmanagedStub))
                .Where(p => p.ManagedToUnmanagedStub is GeneratedStubCodeContext)
                .Select(ctx => ((GeneratedStubCodeContext)ctx.ManagedToUnmanagedStub).Stub.Node
                .WithExplicitInterfaceSpecifier(
                    ExplicitInterfaceSpecifier(ParseName(definingType.FullTypeName))));
            var inheritedStubs = interfaceGroup.ShadowingMethods.Select(m => m.UnreachableExceptionStub);
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

        private const string CreateManagedVirtualFunctionTableMethodName = "CreateManagedVirtualFunctionTable";

        private static readonly MethodDeclarationSyntax CreateManagedVirtualFunctionTableMethodTemplate = MethodDeclaration(TypeSyntaxes.VoidStarStar, CreateManagedVirtualFunctionTableMethodName)
            .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword));

        private static InterfaceDeclarationSyntax GenerateImplementationVTable(ComInterfaceAndMethodsContext interfaceMethods, CancellationToken _)
        {
            if (!interfaceMethods.Interface.Options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper))
            {
                return ImplementationInterfaceTemplate;
            }

            const string vtableLocalName = "vtable";
            var interfaceType = interfaceMethods.Interface.Info.Type;

            // void** vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(<interfaceType>, sizeof(void*) * <max(vtableIndex) + 1>);
            var vtableDeclarationStatement =
                Declare(
                    TypeSyntaxes.VoidStarStar,
                    vtableLocalName,
                    CastExpression(TypeSyntaxes.VoidStarStar,
                        MethodInvocation(
                            TypeSyntaxes.System_Runtime_CompilerServices_RuntimeHelpers,
                            IdentifierName("AllocateTypeAssociatedMemory"),
                            Argument(TypeOfExpression(interfaceType.Syntax)),
                            Argument(
                                BinaryExpression(
                                    SyntaxKind.MultiplyExpression,
                                    SizeOfExpression(TypeSyntaxes.VoidStar),
                                    IntLiteral(3 + interfaceMethods.Methods.Length))))));

            BlockSyntax fillBaseInterfaceSlots;


            if (interfaceMethods.Interface.Base is null)
            {
                // If we don't have a base interface, we need to manually fill in the base iUnknown slots.
                fillBaseInterfaceSlots = Block()
                    .AddStatements(
                        // nint v0, v1, v2;
                        LocalDeclarationStatement(VariableDeclaration(ParseTypeName("nint"))
                            .AddVariables(
                                VariableDeclarator("v0"),
                                VariableDeclarator("v1"),
                                VariableDeclarator("v2")
                            )),
                        // ComWrappers.GetIUnknownImpl(out v0, out v1, out v2);
                        MethodInvocationStatement(
                            TypeSyntaxes.System_Runtime_InteropServices_ComWrappers,
                            IdentifierName("GetIUnknownImpl"),
                            OutArgument(IdentifierName("v0")),
                            OutArgument(IdentifierName("v1")),
                            OutArgument(IdentifierName("v2"))),
                        // m_vtable[0] = (void*)v0;
                        AssignmentStatement(
                            IndexExpression(
                                IdentifierName(vtableLocalName),
                                Argument(IntLiteral(0))),
                            CastExpression(TypeSyntaxes.VoidStar, IdentifierName("v0"))),
                        // m_vtable[1] = (void*)v1;
                        AssignmentStatement(
                            IndexExpression(
                                IdentifierName(vtableLocalName),
                                Argument(IntLiteral(1))),
                            CastExpression(TypeSyntaxes.VoidStar, IdentifierName("v1"))),
                        // m_vtable[2] = (void*)v2;
                        AssignmentStatement(
                            IndexExpression(
                                IdentifierName(vtableLocalName),
                                Argument(IntLiteral(2))),
                            CastExpression(TypeSyntaxes.VoidStar, IdentifierName("v2"))));
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
                            Argument(IdentifierName(vtableLocalName)),
                            Argument(CastExpression(IdentifierName("nuint"),
                                ParenthesizedExpression(
                                    BinaryExpression(SyntaxKind.MultiplyExpression,
                                        SizeOfExpression(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))),
                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(interfaceMethods.ShadowingMethods.Count() + 3))))))));
            }

            var vtableSlotAssignments = VirtualMethodPointerStubGenerator.GenerateVirtualMethodTableSlotAssignments(
                interfaceMethods.DeclaredMethods
                    .Where(context => context.UnmanagedToManagedStub.Diagnostics.All(diag => diag.Descriptor.DefaultSeverity != DiagnosticSeverity.Error))
                    .Select(context => context.GenerationContext),
                vtableLocalName,
                ComInterfaceGeneratorHelpers.GetGeneratorResolver);

            return ImplementationInterfaceTemplate
                .AddMembers(
                    CreateManagedVirtualFunctionTableMethodTemplate
                        .WithBody(
                            Block(
                                vtableDeclarationStatement,
                                fillBaseInterfaceSlots,
                                vtableSlotAssignments,
                                ReturnStatement(IdentifierName(vtableLocalName)))));
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
                const string vtableFieldName = "_vtable";
                return interfaceInformationType.AddMembers(
                        // private static void** _vtable;
                        FieldDeclaration(VariableDeclaration(TypeSyntaxes.VoidStarStar, SingletonSeparatedList(VariableDeclarator(vtableFieldName))))
                            .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword)),
                        // public static void* VirtualMethodTableManagedImplementation => _vtable != null ? _vtable : (_vtable = InterfaceImplementation.CreateManagedVirtualMethodTable());
                        PropertyDeclaration(TypeSyntaxes.VoidStarStar, "ManagedVirtualMethodTable")
                            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                            .WithExpressionBody(
                                ArrowExpressionClause(
                                    ConditionalExpression(
                                        BinaryExpression(SyntaxKind.NotEqualsExpression,
                                            IdentifierName(vtableFieldName),
                                            LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                        IdentifierName(vtableFieldName),
                                        ParenthesizedExpression(
                                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                IdentifierName(vtableFieldName),
                                                MethodInvocation(
                                                    IdentifierName("InterfaceImplementation"),
                                                    IdentifierName(CreateManagedVirtualFunctionTableMethodName)))))))
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
            }

            return interfaceInformationType.AddMembers(
                PropertyDeclaration(TypeSyntaxes.VoidStarStar, "ManagedVirtualMethodTable")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                    .WithExpressionBody(ArrowExpressionClause(LiteralExpression(SyntaxKind.NullLiteralExpression)))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));


            static ExpressionSyntax CreateEmbeddedDataBlobCreationStatement(ReadOnlySpan<byte> bytes)
            {
                var literals = new LiteralExpressionSyntax[bytes.Length];

                for (int i = 0; i < bytes.Length; i++)
                {
                    literals[i] = LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(bytes[i]));
                }

                // new System.ReadOnlySpan<byte>(new[] { <byte literals> } )
                return ObjectCreationExpression(
                    GenericName(TypeNames.System_ReadOnlySpan)
                        .AddTypeArgumentListArguments(PredefinedType(Token(SyntaxKind.ByteKeyword))))
                    .AddArgumentListArguments(
                        Argument(
                            ArrayCreationExpression(
                                    ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)), SingletonList(ArrayRankSpecifier())),
                                    InitializerExpression(
                                        SyntaxKind.ArrayInitializerExpression,
                                        SeparatedList<ExpressionSyntax>(literals)))));
            }
        }
    }
}
