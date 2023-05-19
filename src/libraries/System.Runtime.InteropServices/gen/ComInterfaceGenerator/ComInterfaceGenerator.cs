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

            var interfaceSymbolAndDiagnostics = attributedInterfaces.Select(static (data, ct) =>
            {
                var (info, diagnostic) = ComInterfaceInfo.From(data.Symbol, data.Syntax);
                return (InterfaceInfo: info, Diagnostic: diagnostic, Symbol: data.Symbol);
            });
            context.RegisterDiagnostics(interfaceSymbolAndDiagnostics.Select((data, ct) => data.Diagnostic));

            var interfaceSymbolsWithoutDiagnostics = interfaceSymbolAndDiagnostics
                .Where(data => data.Diagnostic is null)
                .Select((data, ct) =>
                (data.InterfaceInfo, data.Symbol));

            var interfaceContexts = interfaceSymbolsWithoutDiagnostics
                .Select((data, ct) => data.InterfaceInfo!)
                .Collect()
                .SelectMany(ComInterfaceContext.GetContexts);

            var comMethodsAndSymbolsAndDiagnostics = interfaceSymbolsWithoutDiagnostics.Select(ComMethodInfo.GetMethodsFromInterface);
            context.RegisterDiagnostics(comMethodsAndSymbolsAndDiagnostics.SelectMany(static (methodList, ct) => methodList.Select(m => m.Diagnostic)));
            var methodInfoAndSymbolGroupedByInterface = comMethodsAndSymbolsAndDiagnostics
                .Select(static (methods, ct) =>
                    methods
                        .Where(pair => pair.Diagnostic is null)
                        .Select(pair => (pair.Symbol, pair.ComMethod))
                        .ToSequenceEqualImmutableArray());

            var methodInfosGroupedByInterface = methodInfoAndSymbolGroupedByInterface
                .Select(static (methods, ct) =>
                    methods.Select(pair => pair.ComMethod).ToSequenceEqualImmutableArray());
            // Create list of methods (inherited and declared) and their owning interface
            var comMethodContextBuilders = interfaceContexts
                .Zip(methodInfosGroupedByInterface)
                .Collect()
                .SelectMany(static (data, ct) =>
                {
                    return ComMethodContext.CalculateAllMethods(data, ct);
                });

            // A dictionary isn't incremental, but it will have symbols, so it will never be incremental anyway.
            var methodInfoToSymbolMap = methodInfoAndSymbolGroupedByInterface
                .SelectMany((data, ct) => data)
                .Collect()
                .Select((data, ct) => data.ToDictionary(static x => x.ComMethod, static x => x.Symbol));
            var comMethodContexts = comMethodContextBuilders
                .Combine(methodInfoToSymbolMap)
                .Combine(context.CreateStubEnvironmentProvider())
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

            // Generate the code for the managed-to-unmanaged stubs and the diagnostics from code-generation.
            context.RegisterDiagnostics(interfaceAndMethodsContexts
                .SelectMany((data, ct) => data.DeclaredMethods.SelectMany(m => m.ManagedToUnmanagedStub.Diagnostics)));
            var managedToNativeInterfaceImplementations = interfaceAndMethodsContexts
                .Select(GenerateImplementationInterface)
                .WithTrackingName(StepNames.GenerateManagedToNativeInterfaceImplementation)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            // Generate the code for the unmanaged-to-managed stubs and the diagnostics from code-generation.
            context.RegisterDiagnostics(interfaceAndMethodsContexts
                .SelectMany((data, ct) => data.DeclaredMethods.SelectMany(m => m.UnmanagedToManagedStub.Diagnostics)));
            var nativeToManagedVtableMethods = interfaceAndMethodsContexts
                .Select(GenerateImplementationVTableMethods)
                .WithTrackingName(StepNames.GenerateNativeToManagedVTableMethods)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

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

        private static IncrementalMethodStubGenerationContext CalculateStubInformation(MethodDeclarationSyntax syntax, IMethodSymbol symbol, int index, StubEnvironment environment, ManagedTypeInfo owningInterface, CancellationToken ct)
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
                if (generatedComAttribute is null
                    && attr.AttributeClass?.ToDisplayString() == TypeNames.GeneratedComInterfaceAttribute)
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

            var generatedComInterfaceAttributeData = new InteropAttributeCompilationData();
            if (generatedComAttribute is not null)
            {
                var args = generatedComAttribute.NamedArguments.ToImmutableDictionary();
                generatedComInterfaceAttributeData = generatedComInterfaceAttributeData.WithValuesFromNamedArguments(args);
            }
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
            }

            var containingSyntaxContext = new ContainingSyntaxContext(syntax);

            var methodSyntaxTemplate = new ContainingSyntax(syntax.Modifiers.StripAccessibilityModifiers().StripTriviaFromTokens(), SyntaxKind.MethodDeclaration, syntax.Identifier, syntax.TypeParameterList);

            ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> callConv = VirtualMethodPointerStubGenerator.GenerateCallConvSyntaxFromAttributes(
                suppressGCTransitionAttribute,
                unmanagedCallConvAttribute,
                ImmutableArray.Create(FunctionPointerUnmanagedCallingConvention(Identifier("MemberFunction"))));

            var declaringType = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(symbol.ContainingType);

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
                owningInterface,
                declaringType,
                generatorDiagnostics.Diagnostics.ToSequenceEqualImmutableArray(),
                ComInterfaceDispatchMarshallingInfo.Instance);
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
                .AddAttributeLists(AttributeList(SingletonSeparatedList(Attribute(ParseName(TypeNames.System_Runtime_InteropServices_DynamicInterfaceCastableImplementationAttribute)))));
        }

        private static InterfaceDeclarationSyntax GenerateImplementationVTableMethods(ComInterfaceAndMethodsContext comInterfaceAndMethods, CancellationToken _)
        {
            return ImplementationInterfaceTemplate
                .WithMembers(
                    List<MemberDeclarationSyntax>(
                        comInterfaceAndMethods.DeclaredMethods
                            .Select(m => m.UnmanagedToManagedStub)
                            .OfType<GeneratedStubCodeContext>()
                            .Select(context => context.Stub.Node)));
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
                                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(interfaceMethods.ShadowingMethods.Count() + 3))))))
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
    }
}
