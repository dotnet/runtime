// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.CollectionExtensions;

namespace Microsoft.Interop
{
    [Generator]
    public sealed partial class ComInterfaceGenerator : IIncrementalGenerator
    {
        private sealed record class GeneratedStubCodeContext(
            ManagedTypeInfo OriginalDefiningType,
            ContainingSyntaxContext ContainingSyntaxContext,
            SyntaxEquivalentNode<MethodDeclarationSyntax> Stub,
            SequenceEqualImmutableArray<Diagnostic> Diagnostics) : GeneratedMethodContextBase(OriginalDefiningType, Diagnostics);

        private sealed record SkippedStubContext(ManagedTypeInfo OriginalDefiningType) : GeneratedMethodContextBase(OriginalDefiningType, new(ImmutableArray<Diagnostic>.Empty));

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

            var interfaceSymbolAndDiagnostic = attributedInterfaces.Select(static (data, ct) =>
            {
                var (info, diagnostic) = ComInterfaceInfo.From(data.Symbol, data.Syntax);
                return (InterfaceInfo: info, Diagnostic: diagnostic, Symbol: data.Symbol);
            });
            context.RegisterDiagnostics(interfaceSymbolAndDiagnostic.Select((data, ct) => data.Diagnostic));

            var interfaceSymbolsWithoutDiagnostics = interfaceSymbolAndDiagnostic
                .Where(data => data.Diagnostic is null);

            var interfacesToGenerate = interfaceSymbolsWithoutDiagnostics
                .Select((data, ct) => data.InterfaceInfo!);

            var interfaceContexts = interfacesToGenerate.Collect().SelectMany(ComInterfaceContext.GetContexts);

            // Get the information we need about methods themselves
            var interfaceMethods = interfaceSymbolsWithoutDiagnostics.Select(static (pair, ct) =>
            {
                var symbol = pair.Symbol;
                var info = pair.InterfaceInfo;
                List<ComMethodInfo> comMethods = new();
                foreach (var member in symbol.GetMembers())
                {
                    if (ComMethodInfo.IsComMethod(info, member, out ComMethodInfo? methodInfo))
                    {
                        comMethods.Add(methodInfo);
                    }
                }
                return comMethods.ToSequenceEqualImmutableArray();
            });
            context.RegisterDiagnostics(interfaceMethods.SelectMany(static (methodList, ct) => methodList.Select(m => m.Diagnostic)));

            // Generate a map from Com interface to the methods it declares
            var interfaceToDeclaredMethodsMap = interfaceContexts
                .Zip(interfaceMethods)
                .Collect()
                .Select(static (data, ct) =>
                {
                    return data.ToValueEqualityImmutableDictionary<(ComInterfaceContext, SequenceEqualImmutableArray<ComMethodInfo>), ComInterfaceContext, SequenceEqualImmutableArray<ComMethodInfo>>(
                        static pair => pair.Item1,
                        static pair => pair.Item2);
                });

            // Combine info about base methods and declared methods to get a list of interfaces, and all the methods they need to worry about (including both declared and inherited methods)
            var interfaceAndMethodsContexts = interfaceToDeclaredMethodsMap
                .Combine(interfaceContexts.Collect())
                .Combine(context.CreateStubEnvironmentProvider())
                .SelectMany(static (data, ct) =>
                {
                    var ((ifaceToMethodsMap, ifaceToBaseMap), env) = data;
                    return ComInterfaceAndMethodsContext.CalculateAllMethods(ifaceToMethodsMap, env, ct);
                });

            // Separate the methods which have methods from those that don't
            var interfacesWithMethodsAndItsMethods = interfaceAndMethodsContexts
                .Where(static data => data.Methods.Length != 0);

            // Separate out the interface for generation that doesn't depend on the methods
            var interfacesWithMethods = interfacesWithMethodsAndItsMethods
                .Select(static (data, ct) => data.Interface);

            {
                // Marker interfaces are COM interfaces that don't have any methods.
                // The lack of methods breaks the mechanism we use later to stitch back together interface-level data
                // and method-level data, but that's okay because marker interfaces are much simpler.
                // We'll handle them seperately because they are so simple.
                var markerInterfaces = interfaceAndMethodsContexts
                    .Where(static data => !data.DeclaredMethods.Any())
                    .Select(static (data, ct) => data.Interface);

                var markerInterfaceIUnknownDerived = markerInterfaces
                    .Select(static (data, ct) => data.Info)
                    .Select(GenerateIUnknownDerivedAttributeApplication)
                    .WithComparer(SyntaxEquivalentComparer.Instance)
                    .SelectNormalized();

                context.RegisterSourceOutput(markerInterfaces.Zip(markerInterfaceIUnknownDerived), (context, data) =>
                {
                    var (interfaceContext, iUnknownDerivedAttributeApplication) = data;
                    context.AddSource(
                        interfaceContext.Info.Type.FullTypeName.Replace("global::", ""),
                        GenerateMarkerInterfaceSource(interfaceContext.Info) + iUnknownDerivedAttributeApplication);
                });
            }

            // Generate the code for the managed-to-unmanaged stubs and the diagnostics from code-generation.
            context.RegisterDiagnostics(interfacesWithMethodsAndItsMethods
                .SelectMany((data, ct) => data.DeclaredMethods.SelectMany(m => m.ManagedToUnmanagedStub.Diagnostics)));
            var managedToNativeInterfaceImplementations = interfacesWithMethodsAndItsMethods
                .Select(GenerateImplementationInterface)
                .WithTrackingName(StepNames.GenerateManagedToNativeInterfaceImplementation)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            // Generate the code for the unmanaged-to-managed stubs and the diagnostics from code-generation.
            context.RegisterDiagnostics(interfacesWithMethodsAndItsMethods
                .SelectMany((data, ct) => data.DeclaredMethods.SelectMany(m => m.NativeToManagedStub.Diagnostics)));
            var nativeToManagedVtableMethods = interfacesWithMethodsAndItsMethods
                .Select(GenerateImplementationVTableMethods)
                .WithTrackingName(StepNames.GenerateNativeToManagedVTableMethods)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            // Generate the native interface metadata for each [GeneratedComInterface]-attributed interface.
            var nativeInterfaceInformation = interfacesWithMethods
                .Select(static (data, ct) => data.Info)
                .Select(GenerateInterfaceInformation)
                .WithTrackingName(StepNames.GenerateInterfaceInformation)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            var shadowingMethods = interfacesWithMethodsAndItsMethods
                .Select((data, ct) =>
                {
                    var context = data.Interface.Info;
                    var methods = data.InheritedMethods.Select(m => (MemberDeclarationSyntax)m.GenerateShadow());
                    var typeDecl = TypeDeclaration(context.ContainingSyntax.TypeKind, context.ContainingSyntax.Identifier)
                        .WithModifiers(context.ContainingSyntax.Modifiers)
                        .WithTypeParameterList(context.ContainingSyntax.TypeParameters)
                        .WithMembers(List(methods));
                    return data.Interface.Info.TypeDefinitionContext.WrapMemberInContainingSyntaxWithUnsafeModifier(typeDecl);
                })
                .SelectNormalized();

            // Generate a method named CreateManagedVirtualFunctionTable on the native interface implementation
            // that allocates and fills in the memory for the vtable.
            var nativeToManagedVtables = interfacesWithMethodsAndItsMethods
                .Select(GenerateImplementationVTable)
                .WithTrackingName(StepNames.GenerateNativeToManagedVTable)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            var iUnknownDerivedAttributeApplication = interfacesWithMethods
                .Select(static (data, ct) => data.Info)
                .Select(GenerateIUnknownDerivedAttributeApplication)
                .WithTrackingName(StepNames.GenerateIUnknownDerivedAttribute)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            var filesToGenerate = interfacesWithMethods
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
                context.AddSource(data.TypeName.Replace("global::", ""), data.Source);
            });
        }

        private static string GenerateMarkerInterfaceSource(ComInterfaceInfo iface) => $$"""
            file unsafe class InterfaceInformation : global::System.Runtime.InteropServices.Marshalling.IIUnknownInterfaceType
            {
                public static global::System.Guid Iid => new(new global::System.ReadOnlySpan<byte>(new byte[] { {{string.Join(",", iface.InterfaceId.ToByteArray())}} }));

                private static void** m_vtable;

                public static void** ManagedVirtualMethodTable
                {
                    get
                    {
                        if (m_vtable == null)
                        {
                            nint* vtable = (nint*)global::System.Runtime.CompilerServices.RuntimeHelpers.AllocateTypeAssociatedMemory(typeof({{iface.Type.FullTypeName}}), sizeof(nint) * 3);
                            global::System.Runtime.InteropServices.ComWrappers.GetIUnknownImpl(out vtable[0], out vtable[1], out vtable[2]);
                            m_vtable = (void**)vtable;
                        }
                        return m_vtable;
                    }
                }
            }

            [global::System.Runtime.InteropServices.DynamicInterfaceCastableImplementation]
            file interface InterfaceImplementation : {{iface.Type.FullTypeName}}
            {}
            """;

        private static readonly AttributeSyntax s_iUnknownDerivedAttributeTemplate =
            Attribute(
                GenericName(TypeNames.IUnknownDerivedAttribute)
                    .AddTypeArgumentListArguments(
                        IdentifierName("InterfaceInformation"),
                        IdentifierName("InterfaceImplementation")));

        private static MemberDeclarationSyntax GenerateIUnknownDerivedAttributeApplication(ComInterfaceInfo context, CancellationToken _)
            => context.TypeDefinitionContext.WrapMemberInContainingSyntaxWithUnsafeModifier(
                TypeDeclaration(context.ContainingSyntax.TypeKind, context.ContainingSyntax.Identifier)
                    .WithModifiers(context.ContainingSyntax.Modifiers)
                    .WithTypeParameterList(context.ContainingSyntax.TypeParameters)
                    .AddAttributeLists(AttributeList(SingletonSeparatedList(s_iUnknownDerivedAttributeTemplate))));

        // Todo: extract info needed from the IMethodSymbol into MethodInfo and only pass a MethodInfo here
        private static IncrementalMethodStubGenerationContext CalculateStubInformation(MethodDeclarationSyntax syntax, IMethodSymbol symbol, int index, StubEnvironment environment, ManagedTypeInfo typeKeyOwner, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            INamedTypeSymbol? lcidConversionAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.LCIDConversionAttribute);
            INamedTypeSymbol? suppressGCTransitionAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.SuppressGCTransitionAttribute);
            INamedTypeSymbol? unmanagedCallConvAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.UnmanagedCallConvAttribute);
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

            AttributeData? generatedComAttribute = null;
            foreach (var attr in symbol.ContainingType.GetAttributes())
            {
                if (generatedComAttribute is not null && attr.AttributeClass?.ToDisplayString() == TypeNames.GeneratedComInterfaceAttribute)
                {
                    generatedComAttribute = attr;
                }
            }

            var generatorDiagnostics = new GeneratorDiagnostics();

            if (lcidConversionAttr is not null)
            {
                // Using LCIDConversion with source-generated interop is not supported
                generatorDiagnostics.ReportConfigurationNotSupported(lcidConversionAttr, nameof(TypeNames.LCIDConversionAttribute));
            }

            // Create the stub.
            var signatureContext = SignatureContext.Create(symbol, DefaultMarshallingInfoParser.Create(environment, generatorDiagnostics, symbol, new InteropAttributeCompilationData(), generatedComAttribute), environment, typeof(VtableIndexStubGenerator).Assembly);

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
                        // Convert the current element into an out parameter on the native signature
                        // while keeping it at the return position in the managed signature.
                        var managedSignatureAsNativeOut = returnSwappedSignatureElements[i] with
                        {
                            RefKind = RefKind.Out,
                            RefKindSyntax = SyntaxKind.OutKeyword,
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

            var containingSyntaxContext = new ContainingSyntaxContext(syntax);

            var methodSyntaxTemplate = new ContainingSyntax(syntax.Modifiers.StripAccessibilityModifiers().StripTriviaFromTokens(), SyntaxKind.MethodDeclaration, syntax.Identifier, syntax.TypeParameterList);

            ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> callConv = VtableIndexStubGenerator.GenerateCallConvSyntaxFromAttributes(suppressGCTransitionAttribute, unmanagedCallConvAttribute);

            var virtualMethodIndexData = new VirtualMethodIndexData(index, ImplicitThisParameter: true, MarshalDirection.Bidirectional, true, ExceptionMarshalling.Com);

            return new IncrementalMethodStubGenerationContext(
                signatureContext,
                containingSyntaxContext,
                methodSyntaxTemplate,
                new MethodSignatureDiagnosticLocations(syntax),
                callConv.ToSequenceEqualImmutableArray(SyntaxEquivalentComparer.Instance),
                virtualMethodIndexData,
                new ComExceptionMarshalling(),
                ComInterfaceGeneratorHelpers.CreateGeneratorFactory(environment, MarshalDirection.ManagedToUnmanaged),
                ComInterfaceGeneratorHelpers.CreateGeneratorFactory(environment, MarshalDirection.UnmanagedToManaged),
                typeKeyOwner,
                generatorDiagnostics.Diagnostics.ToSequenceEqualImmutableArray(),
                ComInterfaceDispatchMarshallingInfo.Instance);
        }



        private static readonly InterfaceDeclarationSyntax ImplementationInterfaceTemplate = InterfaceDeclaration("InterfaceImplementation")
                .WithModifiers(TokenList(Token(SyntaxKind.FileKeyword), Token(SyntaxKind.UnsafeKeyword), Token(SyntaxKind.PartialKeyword)));

        private static InterfaceDeclarationSyntax GenerateImplementationInterface(ComInterfaceAndMethodsContext interfaceGroup, CancellationToken _)
        {
            var definingType = interfaceGroup.Interface.Info.Type;
            var shadowImplementations = interfaceGroup.ShadowingMethods.Select(m => (m, m.ManagedToUnmanagedStub))
                .Where(p => p.ManagedToUnmanagedStub is GeneratedStubCodeContext)
                .Select(ctx => ((GeneratedStubCodeContext)ctx.ManagedToUnmanagedStub).Stub.Node
                .WithExplicitInterfaceSpecifier(
                    ExplicitInterfaceSpecifier(ParseName(definingType.FullTypeName))));
            return ImplementationInterfaceTemplate
                .AddBaseListTypes(SimpleBaseType(definingType.Syntax))
                .WithMembers(
                    List<MemberDeclarationSyntax>(
                        interfaceGroup.Methods
                        .Select(m => m.ManagedToUnmanagedStub)
                        .OfType<GeneratedStubCodeContext>()
                        .Select(ctx => ctx.Stub.Node)
                        .Concat(shadowImplementations)))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(Attribute(ParseName(TypeNames.System_Runtime_InteropServices_DynamicInterfaceCastableImplementationAttribute)))));
        }
        private static InterfaceDeclarationSyntax GenerateImplementationVTableMethods(ComInterfaceAndMethodsContext comInterfaceAndMethods, CancellationToken _)
        {
            return ImplementationInterfaceTemplate
                .WithMembers(
                    List<MemberDeclarationSyntax>(
                        comInterfaceAndMethods.DeclaredMethods
                            .Select(m => m.NativeToManagedStub)
                            .OfType<GeneratedStubCodeContext>()
                            .Select(context => context.Stub.Node) ));
        }

        private static readonly TypeSyntax VoidStarStarSyntax = PointerType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))));

        private const string CreateManagedVirtualFunctionTableMethodName = "CreateManagedVirtualFunctionTable";

        private static readonly MethodDeclarationSyntax CreateManagedVirtualFunctionTableMethodTemplate = MethodDeclaration(VoidStarStarSyntax, CreateManagedVirtualFunctionTableMethodName)
            .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword));
        private static InterfaceDeclarationSyntax GenerateImplementationVTable(ComInterfaceAndMethodsContext interfaceMethods, CancellationToken _)
        {
            const string vtableLocalName = "vtable";
            var interfaceType = interfaceMethods.Interface.Info.Type;
            var interfaceMethodStubs = interfaceMethods.DeclaredMethods.Select(m => m.GenerationContext);

            ImmutableArray<IncrementalMethodStubGenerationContext> vtableExposedContexts = interfaceMethodStubs
                .Where(c => c.VtableIndexData.Direction is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
                .ToImmutableArray();

            // If none of the methods are exposed as part of the vtable, then don't emit
            // a vtable (return null).
            if (vtableExposedContexts.Length == 0)
            {
                return ImplementationInterfaceTemplate
                .AddMembers(
                    CreateManagedVirtualFunctionTableMethodTemplate
                        .WithBody(
                            Block(
                                ReturnStatement(LiteralExpression(SyntaxKind.NullLiteralExpression)))));
            }

            // void** vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(<interfaceType>, sizeof(void*) * <max(vtableIndex) + 1>);
            var vtableDeclarationStatement =
                LocalDeclarationStatement(
                    VariableDeclaration(
                        VoidStarStarSyntax,
                        SingletonSeparatedList(
                            VariableDeclarator(vtableLocalName)
                            .WithInitializer(
                                EqualsValueClause(
                                    CastExpression(VoidStarStarSyntax,
                                        InvocationExpression(
                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                ParseTypeName(TypeNames.System_Runtime_CompilerServices_RuntimeHelpers),
                                                IdentifierName("AllocateTypeAssociatedMemory")))
                                        .AddArgumentListArguments(
                                            Argument(TypeOfExpression(interfaceType.Syntax)),
                                            Argument(
                                                BinaryExpression(
                                                    SyntaxKind.MultiplyExpression,
                                                    SizeOfExpression(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))),
                                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(3 + interfaceMethods.Methods.Length)))))))))));

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
                        ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    ParseTypeName(TypeNames.System_Runtime_InteropServices_ComWrappers),
                                    IdentifierName("GetIUnknownImpl")))
                            .AddArgumentListArguments(
                                Argument(IdentifierName("v0"))
                                        .WithRefKindKeyword(Token(SyntaxKind.OutKeyword)),
                                Argument(IdentifierName("v1"))
                                    .WithRefKindKeyword(Token(SyntaxKind.OutKeyword)),
                                Argument(IdentifierName("v2"))
                                    .WithRefKindKeyword(Token(SyntaxKind.OutKeyword)))),
                        // m_vtable[0] = (void*)v0;
                        ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                ElementAccessExpression(
                                    IdentifierName(vtableLocalName),
                                    BracketedArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                LiteralExpression(
                                                    SyntaxKind.NumericLiteralExpression,
                                                    Literal(0)))))),
                                CastExpression(
                                    PointerType(
                                        PredefinedType(Token(SyntaxKind.VoidKeyword))),
                                    IdentifierName("v0")))),
                        // m_vtable[1] = (void*)v1;
                        ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                ElementAccessExpression(
                                    IdentifierName(vtableLocalName),
                                    BracketedArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                LiteralExpression(
                                                    SyntaxKind.NumericLiteralExpression,
                                                    Literal(1)))))),
                                CastExpression(
                                    PointerType(
                                        PredefinedType(Token(SyntaxKind.VoidKeyword))),
                                    IdentifierName("v1")))),
                        // m_vtable[2] = (void*)v2;
                        ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                ElementAccessExpression(
                                    IdentifierName(vtableLocalName),
                                    BracketedArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                LiteralExpression(
                                                    SyntaxKind.NumericLiteralExpression,
                                                    Literal(2)))))),
                                CastExpression(
                                    PointerType(
                                        PredefinedType(Token(SyntaxKind.VoidKeyword))),
                                    IdentifierName("v2")))));
            }
            else
            {
                // NativeMemory.Copy(StrategyBasedComWrappers.DefaultIUnknownInteraceDetailsStrategy.GetIUnknownDerivedDetails(typeof(<baseInterfaceType>).TypeHandle).ManagedVirtualMethodTable, vtable, (nuint)(sizeof(void*) * <startingOffset>));
                fillBaseInterfaceSlots = Block()
                    .AddStatements(
                        ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    ParseTypeName(TypeNames.System_Runtime_InteropServices_NativeMemory),
                                    IdentifierName("Copy")))
                            .WithArgumentList(
                                ArgumentList(
                                    SeparatedList(
                                        new[]
                                        {
                                            Argument(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    InvocationExpression(
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                ParseTypeName(TypeNames.StrategyBasedComWrappers),
                                                                IdentifierName("DefaultIUnknownInterfaceDetailsStrategy")),
                                                            IdentifierName("GetIUnknownDerivedDetails")))
                                                    .WithArgumentList(
                                                        ArgumentList(
                                                            SingletonSeparatedList(
                                                                Argument(
                                                                    MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        TypeOfExpression(
                                                                            ParseTypeName(interfaceMethods.Interface.Base.Info.Type.FullTypeName)), //baseInterfaceTypeInfo.BaseInterface.FullTypeName)),
                                                                        IdentifierName("TypeHandle")))))),
                                                    IdentifierName("ManagedVirtualMethodTable"))),
                                            Argument(IdentifierName(vtableLocalName)),
                                            Argument(CastExpression(IdentifierName("nuint"),
                                                ParenthesizedExpression(
                                                    BinaryExpression(SyntaxKind.MultiplyExpression,
                                                        SizeOfExpression(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))),
                                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(interfaceMethods.InheritedMethods.Count() + 3))))))
                                        })))));
            }

            var vtableSlotAssignments = VirtualMethodPointerStubGenerator.GenerateVirtualMethodTableSlotAssignments(interfaceMethodStubs, vtableLocalName);

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
            .AddBaseListTypes(SimpleBaseType(ParseTypeName(TypeNames.IIUnknownInterfaceType)));

        private static ClassDeclarationSyntax GenerateInterfaceInformation(ComInterfaceInfo context, CancellationToken _)
        {
            const string vtableFieldName = "_vtable";
            return InterfaceInformationTypeTemplate
                .AddMembers(
                    // public static System.Guid Iid { get; } = new(<embeddedDataBlob>);
                    PropertyDeclaration(ParseTypeName(TypeNames.System_Guid), "Iid")
                        .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                        .AddAccessorListAccessors(
                            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))
                        .WithInitializer(
                            EqualsValueClause(
                                ImplicitObjectCreationExpression()
                                    .AddArgumentListArguments(
                                        Argument(CreateEmbeddedDataBlobCreationStatement(context.InterfaceId.ToByteArray())))))
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                    // private static void** _vtable;
                    FieldDeclaration(VariableDeclaration(VoidStarStarSyntax, SingletonSeparatedList(VariableDeclarator(vtableFieldName))))
                        .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword)),
                    // public static void* VirtualMethodTableManagedImplementation => _vtable != null ? _vtable : (_vtable = InterfaceImplementation.CreateManagedVirtualMethodTable());
                    PropertyDeclaration(VoidStarStarSyntax, "ManagedVirtualMethodTable")
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
                                            InvocationExpression(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("InterfaceImplementation"),
                                                    IdentifierName(CreateManagedVirtualFunctionTableMethodName))))))))
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                    );

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

        private sealed record InterfaceSymbolInfo<TBaseInterfaceKey>(ComInterfaceInfo Info, Diagnostic? Diagnostic, TBaseInterfaceKey ThisInterfaceKey, TBaseInterfaceKey? BaseInterfaceKey);
    }
}
