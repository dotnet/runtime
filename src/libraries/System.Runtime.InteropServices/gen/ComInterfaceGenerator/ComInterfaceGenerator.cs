// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.CollectionExtensions;

namespace Microsoft.Interop
{
    [Generator]
    public sealed class ComInterfaceGenerator : IIncrementalGenerator
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

        private static bool TryGetBaseComInterface(INamedTypeSymbol comIface, [NotNullWhen(true)] out INamedTypeSymbol? baseComIface)
        {
            baseComIface = null;
            foreach (var implemented in comIface.Interfaces)
            {
                if (implemented.GetAttributes().Any(static attr => attr.AttributeClass?.ToDisplayString() == TypeNames.GeneratedComInterfaceAttribute))
                {
                    // We'll filter out cases where there's multiple matching interfaces when determining
                    // if this is a valid candidate for generation.
                    baseComIface = implemented;
                    break;
                }
            }
            return baseComIface is not null;
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


            var interfacesAndDiagnostics = attributedInterfaces.Select(static (data, ct) =>
            {
                Diagnostic? Diagnostic = GetDiagnosticIfInvalidTypeForGeneration(data.Syntax, data.Symbol);
                INamedTypeSymbol? BaseInterfaceSymbol = TryGetBaseComInterface(data.Symbol, out var baseComInterface) ? baseComInterface : null;
                ComInterfaceContext Context = ComInterfaceContext.From(data.Symbol, data.Syntax);
                return new { data.Syntax, data.Symbol, Context, Diagnostic, BaseInterfaceSymbol };
            });

            // Split the types we want to generate and the ones we don't into two separate groups.
            var interfacesToGenerate = interfacesAndDiagnostics.Where(static data => data.Diagnostic is null);
            {
                var invalidTypeDiagnostics = interfacesAndDiagnostics.Where(static data => data.Diagnostic is not null);
                context.RegisterDiagnostics(invalidTypeDiagnostics.Select((data, ct) => data.Diagnostic));
            }

            // Get the information we need about methods themselves
            var interfaceMethods = interfacesToGenerate.Select((data, ct) =>
            {
                INamedTypeSymbol iface = data.Symbol;
                List<MethodInfo> comMethods = new();
                foreach (var member in iface.GetMembers())
                {
                    if (MethodInfo.IsComInterface(data.Context, member, out MethodInfo? methodInfo))
                    {
                        comMethods.Add(methodInfo);
                    }
                }
                return comMethods.ToSequenceEqualImmutableArray();
            });

            // Create a map of Com interface to its base for use later.
            var ifaceToBaseMap = interfacesToGenerate.Collect().Select((data, ct) =>
            {
                Dictionary<ComInterfaceContext, ComInterfaceContext?> ifaceToBaseMap = new();
                Dictionary<INamedTypeSymbol, ComInterfaceContext> contexts = new(SymbolEqualityComparer.Default);
                foreach (var iface in data)
                {
                    contexts.Add(iface.Symbol, iface.Context);
                }
                foreach (var iface in data)
                {
                    ifaceToBaseMap.Add(iface.Context, iface.BaseInterfaceSymbol is not null ? contexts[iface.BaseInterfaceSymbol] : null);
                }
                return ifaceToBaseMap.ToValueEqualImmutable();
            });

            // Generate a map from Com interface to the methods it declares
            var interfaceToDeclaredMethodsMap = interfacesToGenerate
                .Select((iface, ct) => iface.Context)
                .Zip(interfaceMethods)
                .Collect()
                .Select((data, ct) =>
                {
                    return data.ToValueEqualityImmutableDictionary<(ComInterfaceContext, SequenceEqualImmutableArray<MethodInfo>), ComInterfaceContext, SequenceEqualImmutableArray<MethodInfo>>(
                        static pair => pair.Item1,
                        static pair => pair.Item2);
                });

            // Combine info about base methods and declared methods to get a list of interfaces, and all the methods they need to worry about (including both declared and inherited methods)
            var interfaceAndMethodsContexts = interfaceToDeclaredMethodsMap
                .Combine(ifaceToBaseMap)
                .Combine(context.CreateStubEnvironmentProvider())
                .SelectMany((data, ct) =>
                {
                    var ((ifaceToMethodsMap, ifaceToBaseMap), env) = data;
                    return ComInterfaceAndMethods.GetAllMethods(ifaceToBaseMap, ifaceToMethodsMap, env, ct);
                });


            // Separate the methods which declare methods from those that don't declare methods
            var interfacesWithMethodsAndItsMethods = interfaceAndMethodsContexts
                .Where(data => data.DeclaredMethods.Any());

            var interfacesWithMethods = interfacesWithMethodsAndItsMethods
                .Select(static (data, ct) => data.Interface);

            {
                // Marker interfaces are COM interfaces that don't have any methods.
                // The lack of methods breaks the mechanism we use later to stitch back together interface-level data
                // and method-level data, but that's okay because marker interfaces are much simpler.
                // We'll handle them seperately because they are so simple.
                var markerInterfaces = interfaceAndMethodsContexts
                    .Where(data => !data.DeclaredMethods.Any())
                    .Select(static (data, ct) => data.Interface);

                var markerInterfaceIUnknownDerived = markerInterfaces
                    .Select(static (context, ct) => GenerateIUnknownDerivedAttributeApplication(context))
                    .WithComparer(SyntaxEquivalentComparer.Instance)
                    .SelectNormalized();

                context.RegisterSourceOutput(markerInterfaces.Zip(markerInterfaceIUnknownDerived), (context, data) =>
                {
                    var (interfaceContext, iUnknownDerivedAttributeApplication) = data;
                    context.AddSource(
                        interfaceContext.InterfaceType.FullTypeName.Replace("global::", ""),
                        GenerateMarkerInterfaceSource(interfaceContext) + iUnknownDerivedAttributeApplication);
                });
            }

            var allMethods = interfaceAndMethodsContexts.SelectMany(static (data, ct) => data.DeclaredMethods);

            // Split the methods we want to generate and the ones with warnings into different groups, and warn on the invalid methods
            {
                var invalidMethods = allMethods.Where(static data => data.Diagnostic is not null);

                context.RegisterSourceOutput(invalidMethods, static (context, invalidMethod) =>
                {
                    context.ReportDiagnostic(invalidMethod.Diagnostic);
                });
            }
            var methodsToGenerate = allMethods.Where(static data =>
            {
                return data.Diagnostic is null;
            });
            IncrementalValuesProvider<IncrementalMethodStubGenerationContext> generateStubInformation = methodsToGenerate.Select((data, ct) => data.GenerationContext);

            // Generate the code for the managed-to-unmanaged stubs and the diagnostics from code-generation.
            var generateManagedToNativeStub = generateStubInformation
                .Select(
                    static (data, ct) =>
                    {
                        if (data.VtableIndexData.Direction is not (MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional))
                        {
                            return (GeneratedMethodContextBase)new SkippedStubContext(data.OriginalDefiningType);
                        }
                        var (methodStub, diagnostics) = VirtualMethodPointerStubGenerator.GenerateManagedToNativeStub(data);
                        return new GeneratedStubCodeContext(data.TypeKeyOwner, data.ContainingSyntaxContext, new(methodStub), new(diagnostics));
                    }
                )
                .WithTrackingName(StepNames.GenerateManagedToNativeStub);

            context.RegisterDiagnostics(generateManagedToNativeStub.SelectMany((stubInfo, ct) => stubInfo.Diagnostics.Array));

            var managedToNativeInterfaceImplementations = interfacesWithMethodsAndItsMethods.Select((data, ct) =>
                {
                    return GenerateImplementationInterface(data);
                }).WithTrackingName(StepNames.GenerateManagedToNativeInterfaceImplementation)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            // Filter the list of all stubs to only the stubs that requested unmanaged-to-managed stub generation.
            IncrementalValuesProvider<IncrementalMethodStubGenerationContext> nativeToManagedStubContexts =
                generateStubInformation
                .Where(static data => data.VtableIndexData.Direction is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional);

            // Generate the code for the unmanaged-to-managed stubs and the diagnostics from code-generation.
            var generateNativeToManagedStub = generateStubInformation
                .Select(
                    static (data, ct) =>
                    {
                        if (data.VtableIndexData.Direction is not (MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional))
                        {
                            return (GeneratedMethodContextBase)new SkippedStubContext(data.OriginalDefiningType);
                        }
                        var (methodStub, diagnostics) = VirtualMethodPointerStubGenerator.GenerateNativeToManagedStub(data);
                        return new GeneratedStubCodeContext(data.OriginalDefiningType, data.ContainingSyntaxContext, new(methodStub), new(diagnostics));
                    }
                )
                .WithTrackingName(StepNames.GenerateNativeToManagedStub);

            context.RegisterDiagnostics(generateNativeToManagedStub.SelectMany((stubInfo, ct) => stubInfo.Diagnostics.Array));

            var nativeToManagedVtableMethods = generateNativeToManagedStub
                .Collect()
                .SelectMany(static (stubs, ct) => GroupContextsForInterfaceGeneration(stubs))
                .Select(static (interfaceGroup, ct) => GenerateImplementationVTableMethods(interfaceGroup.Array))
                .WithTrackingName(StepNames.GenerateNativeToManagedVTableMethods)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            // Generate the native interface metadata for each [GeneratedComInterface]-attributed interface.
            var nativeInterfaceInformation = interfacesWithMethods
                .Select(static (context, ct) => GenerateInterfaceInformation(context))
                .WithTrackingName(StepNames.GenerateInterfaceInformation)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            // Generate a method named CreateManagedVirtualFunctionTable on the native interface implementation
            // that allocates and fills in the memory for the vtable.
            var nativeToManagedVtables = interfacesWithMethodsAndItsMethods.Select((data, ct) => GenerateImplementationVTable(data))
                .WithTrackingName(StepNames.GenerateNativeToManagedVTable)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            var iUnknownDerivedAttributeApplication = interfacesWithMethods
                .Select(static (context, ct) => GenerateIUnknownDerivedAttributeApplication(context))
                .WithTrackingName(StepNames.GenerateIUnknownDerivedAttribute)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            var filesToGenerate = interfacesWithMethods
                .Zip(nativeInterfaceInformation)
                .Zip(managedToNativeInterfaceImplementations)
                .Zip(nativeToManagedVtableMethods)
                .Zip(nativeToManagedVtables)
                .Zip(iUnknownDerivedAttributeApplication)
                .Select(static (data, ct) =>
                {
                    var (((((interfaceContext, interfaceInfo), managedToNativeStubs), nativeToManagedStubs), nativeToManagedVtable), iUnknownDerivedAttribute) = data;

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
                    return new { TypeName = interfaceContext.InterfaceType.FullTypeName, Source = source.ToString() };
                });

            context.RegisterSourceOutput(filesToGenerate, (context, data) =>
            {
                context.AddSource(data.TypeName.Replace("global::", ""), data.Source);
            });
        }

        private static string GenerateMarkerInterfaceSource(ComInterfaceContext iface) => $$"""
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
                            nint* vtable = (nint*)global::System.Runtime.CompilerServices.RuntimeHelpers.AllocateTypeAssociatedMemory(typeof({{iface.InterfaceType.FullTypeName}}), sizeof(nint) * 3);
                            global::System.Runtime.InteropServices.ComWrappers.GetIUnknownImpl(out vtable[0], out vtable[1], out vtable[2]);
                            m_vtable = (void**)vtable;
                        }
                        return m_vtable;
                    }
                }
            }

            [global::System.Runtime.InteropServices.DynamicInterfaceCastableImplementation]
            file interface InterfaceImplementation : {{iface.InterfaceType.FullTypeName}}
            {}
            """;

        private static readonly AttributeSyntax s_iUnknownDerivedAttributeTemplate =
            Attribute(
                GenericName(TypeNames.IUnknownDerivedAttribute)
                    .AddTypeArgumentListArguments(
                        IdentifierName("InterfaceInformation"),
                        IdentifierName("InterfaceImplementation")));

        private static MemberDeclarationSyntax GenerateIUnknownDerivedAttributeApplication(ComInterfaceContext context)
            => context.TypeDefinitionContext.WrapMemberInContainingSyntaxWithUnsafeModifier(
                TypeDeclaration(context.ContainingSyntax.TypeKind, context.ContainingSyntax.Identifier)
                    .WithModifiers(context.ContainingSyntax.Modifiers)
                    .WithTypeParameterList(context.ContainingSyntax.TypeParameters)
                    .AddAttributeLists(AttributeList(SingletonSeparatedList(s_iUnknownDerivedAttributeTemplate))));

        private static IncrementalMethodStubGenerationContext CalculateStubInformation(MethodDeclarationSyntax syntax, IMethodSymbol symbol, int index, StubEnvironment environment, CancellationToken ct)
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

            var typeKeyOwner = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(symbol.ContainingType);

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

        private static Diagnostic? GetDiagnosticIfInvalidTypeForGeneration(InterfaceDeclarationSyntax syntax, INamedTypeSymbol type)
        {
            // Verify the method has no generic types or defined implementation
            // and is not marked static or sealed
            if (syntax.TypeParameterList is not null)
            {
                return Diagnostic.Create(GeneratorDiagnostics.InvalidAttributedMethodSignature, syntax.Identifier.GetLocation(), type.Name);
            }

            // Verify that the types the method is declared in are marked partial.
            for (SyntaxNode? parentNode = syntax.Parent; parentNode is TypeDeclarationSyntax typeDecl; parentNode = parentNode.Parent)
            {
                if (!typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return Diagnostic.Create(GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingModifiers, syntax.Identifier.GetLocation(), type.Name, typeDecl.Identifier);
                }
            }
            var guidAttr = type.GetAttributes().Where(attr => attr.AttributeClass.ToDisplayString() == TypeNames.System_Runtime_InteropServices_GuidAttribute).SingleOrDefault();
            var interfaceTypeAttr = type.GetAttributes().Where(attr => attr.AttributeClass.ToDisplayString() == TypeNames.InterfaceTypeAttribute).SingleOrDefault();
            // Assume interfaceType is IUnknown for now
            if (interfaceTypeAttr is not null
                && (guidAttr is null
                    || guidAttr.ConstructorArguments.SingleOrDefault().Value as string is null))
            {
                return Diagnostic.Create(GeneratorDiagnostics.InvalidAttributedInterfaceMissingGuidAttribute, syntax.Identifier.GetLocation(), type.ToDisplayString());
                // Missing Guid
            }

            // Error if more than one GeneratedComInterface base interface type.
            INamedTypeSymbol? baseInterface = null;
            foreach (var implemented in type.Interfaces)
            {
                if (implemented.GetAttributes().Any(static attr => attr.AttributeClass?.ToDisplayString() == TypeNames.GeneratedComInterfaceAttribute))
                {
                    if (baseInterface is not null)
                    {
                        return Diagnostic.Create(GeneratorDiagnostics.MultipleComInterfaceBaseTypesAttribute, syntax.Identifier.GetLocation(), type.ToDisplayString());
                    }
                    baseInterface = implemented;
                }
            }

            return null;
        }

        private static Diagnostic? GetDiagnosticIfInvalidMethodForGeneration(MethodDeclarationSyntax syntax, IMethodSymbol method)
        {
            // Verify the method has no generic types or defined implementation
            // and is not marked static or sealed
            if (syntax.TypeParameterList is not null
                || syntax.Body is not null
                || syntax.Modifiers.Any(SyntaxKind.SealedKeyword))
            {
                return Diagnostic.Create(GeneratorDiagnostics.InvalidAttributedMethodSignature, syntax.Identifier.GetLocation(), method.Name);
            }

            // Verify the method does not have a ref return
            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
            {
                return Diagnostic.Create(GeneratorDiagnostics.ReturnConfigurationNotSupported, syntax.Identifier.GetLocation(), "ref return", method.ToDisplayString());
            }

            return null;
        }

        private static ImmutableArray<SequenceEqualImmutableArray<GeneratedMethodContextBase>> GroupContextsForInterfaceGeneration(ImmutableArray<GeneratedMethodContextBase> contexts)
        {
            // We can end up with an empty set of contexts here as the compiler will call a SelectMany
            // after a Collect with no input entries
            if (contexts.IsEmpty)
            {
                return ImmutableArray<SequenceEqualImmutableArray<GeneratedMethodContextBase>>.Empty;
            }

            ImmutableArray<SequenceEqualImmutableArray<GeneratedMethodContextBase>>.Builder allGroupsBuilder = ImmutableArray.CreateBuilder<SequenceEqualImmutableArray<GeneratedMethodContextBase>>();

            // Due to how the source generator driver processes the input item tables and our limitation that methods on COM interfaces can only be defined in a single partial definition of the type,
            // we can guarantee that the method contexts are ordered as follows:
            // - I1.M1
            // - I1.M2
            // - I1.M3
            // - I2.M1
            // - I2.M2
            // - I2.M3
            // - I3.M1
            // - etc...
            // This enable us to group our contexts by their containing syntax rather simply.
            ManagedTypeInfo? lastSeenDefiningType = null;
            ImmutableArray<GeneratedMethodContextBase>.Builder groupBuilder = ImmutableArray.CreateBuilder<GeneratedMethodContextBase>();
            foreach (var context in contexts)
            {
                if (lastSeenDefiningType is null || lastSeenDefiningType == context.OriginalDefiningType)
                {
                    groupBuilder.Add(context);
                }
                else
                {
                    allGroupsBuilder.Add(new(groupBuilder.ToImmutable()));
                    groupBuilder.Clear();
                    groupBuilder.Add(context);
                }
                lastSeenDefiningType = context.OriginalDefiningType;
            }

            allGroupsBuilder.Add(new(groupBuilder.ToImmutable()));
            return allGroupsBuilder.ToImmutable();
        }

        private static readonly InterfaceDeclarationSyntax ImplementationInterfaceTemplate = InterfaceDeclaration("InterfaceImplementation")
                .WithModifiers(TokenList(Token(SyntaxKind.FileKeyword), Token(SyntaxKind.UnsafeKeyword), Token(SyntaxKind.PartialKeyword)));
        private static InterfaceDeclarationSyntax GenerateImplementationInterface(ComInterfaceAndMethods interfaceGroup)
        {
            var definingType = interfaceGroup.Interface.InterfaceType;
            return ImplementationInterfaceTemplate
                .AddBaseListTypes(SimpleBaseType(definingType.Syntax))
                .WithMembers(List<MemberDeclarationSyntax>(interfaceGroup.DeclaredMethods.Select(m => m.ManagedToUnmanagedStub).OfType<GeneratedStubCodeContext>().Select(ctx => ctx.Stub.Node)))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(Attribute(ParseName(TypeNames.System_Runtime_InteropServices_DynamicInterfaceCastableImplementationAttribute)))));
        }
        private static InterfaceDeclarationSyntax GenerateImplementationVTableMethods(ImmutableArray<GeneratedMethodContextBase> interfaceGroup)
        {
            return ImplementationInterfaceTemplate
                .WithMembers(List<MemberDeclarationSyntax>(interfaceGroup.OfType<GeneratedStubCodeContext>().Select(context => context.Stub.Node)));
        }

        private static readonly TypeSyntax VoidStarStarSyntax = PointerType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))));

        private const string CreateManagedVirtualFunctionTableMethodName = "CreateManagedVirtualFunctionTable";

        private static readonly MethodDeclarationSyntax CreateManagedVirtualFunctionTableMethodTemplate = MethodDeclaration(VoidStarStarSyntax, CreateManagedVirtualFunctionTableMethodName)
            .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword));
        private static InterfaceDeclarationSyntax GenerateImplementationVTable(ComInterfaceAndMethods interfaceMethods)
        {
            const string vtableLocalName = "vtable";
            var interfaceType = interfaceMethods.Interface.InterfaceType;
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


            if (interfaceMethods.BaseInterface is null)
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
                                                                            ParseTypeName(interfaceMethods.BaseInterface.InterfaceType.FullTypeName)), //baseInterfaceTypeInfo.BaseInterface.FullTypeName)),
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

        private static ClassDeclarationSyntax GenerateInterfaceInformation(ComInterfaceContext context)
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

        private sealed record ComInterfaceContext(
            ManagedTypeInfo InterfaceType,
            InterfaceDeclarationSyntax InterfaceDeclaration,
            ContainingSyntaxContext TypeDefinitionContext,
            ContainingSyntax ContainingSyntax,
            Guid InterfaceId)
        {
            public static ComInterfaceContext From(INamedTypeSymbol symbol, InterfaceDeclarationSyntax syntax)
            {
                Guid? guid = null;
                var guidAttr = symbol.GetAttributes().Where(attr => attr.AttributeClass.ToDisplayString() == TypeNames.System_Runtime_InteropServices_GuidAttribute).SingleOrDefault();
                if (guidAttr is not null)
                {
                    string? guidstr = guidAttr.ConstructorArguments.SingleOrDefault().Value as string;
                    if (guidstr is not null)
                        guid = new Guid(guidstr);
                }
                return new ComInterfaceContext(
                    ManagedTypeInfo.CreateTypeInfoForTypeSymbol(symbol),
                    syntax,
                    new ContainingSyntaxContext(syntax),
                    new ContainingSyntax(syntax.Modifiers, syntax.Kind(), syntax.Identifier, syntax.TypeParameterList),
                    guid ?? Guid.Empty);
            }

            public override int GetHashCode()
            {
                // ContainingSyntax and ContainingSyntaxContext do not implement GetHashCode
                return HashCode.Combine(InterfaceType, TypeDefinitionContext, InterfaceId);
            }

            public bool Equals(ComInterfaceContext other)
            {
                // ContainingSyntax and ContainingSyntaxContext are not used in the hash code
                return InterfaceType == other.InterfaceType
                    && TypeDefinitionContext == other.TypeDefinitionContext
                    && InterfaceId == other.InterfaceId;
            }
        }

        /// <summary>
        /// Represents a method that has been determined to be a COM interface method.
        /// </summary>
        private sealed record MethodInfo(
            [property: Obsolete] IMethodSymbol Symbol,
            MethodDeclarationSyntax Syntax,
            string MethodName,
            SequenceEqualImmutableArray<(ManagedTypeInfo Type, string Name, RefKind RefKind)> Parameters,
            Diagnostic? Diagnostic)
        {
            public static bool IsComInterface(ComInterfaceContext ifaceContext, ISymbol member, [NotNullWhen(true)] out MethodInfo? comMethodInfo)
            {
                comMethodInfo = null;
                Location interfaceLocation = ifaceContext.InterfaceDeclaration.GetLocation();
                if (member.Kind == SymbolKind.Method && !member.IsStatic)
                {
                    // We only support methods that are defined in the same partial interface definition as the
                    // [GeneratedComInterface] attribute.
                    // This restriction not only makes finding the syntax for a given method cheaper,
                    // but it also enables us to ensure that we can determine vtable method order easily.
                    Location? methodLocationInAttributedInterfaceDeclaration = null;
                    foreach (var methodLocation in member.Locations)
                    {
                        if (methodLocation.SourceTree == interfaceLocation.SourceTree
                            && interfaceLocation.SourceSpan.Contains(methodLocation.SourceSpan))
                        {
                            methodLocationInAttributedInterfaceDeclaration = methodLocation;
                            break;
                        }
                    }

                    // TODO: this should cause a diagnostic
                    if (methodLocationInAttributedInterfaceDeclaration is null)
                    {
                        return false;
                    }

                    // Find the matching declaration syntax
                    MethodDeclarationSyntax? comMethodDeclaringSyntax = null;
                    foreach (var declaringSyntaxReference in member.DeclaringSyntaxReferences)
                    {
                        var declaringSyntax = declaringSyntaxReference.GetSyntax();
                        Debug.Assert(declaringSyntax.IsKind(SyntaxKind.MethodDeclaration));
                        if (declaringSyntax.GetLocation().SourceSpan.Contains(methodLocationInAttributedInterfaceDeclaration.SourceSpan))
                        {
                            comMethodDeclaringSyntax = (MethodDeclarationSyntax)declaringSyntax;
                            break;
                        }
                    }
                    if (comMethodDeclaringSyntax is null)
                        throw new NotImplementedException("Found a method that was declared in the attributed interface declaration, but couldn't find the syntax for it.");

                    List<(ManagedTypeInfo ParameterType, string Name, RefKind RefKind)> parameters = new();
                    foreach (var parameter in ((IMethodSymbol)member).Parameters)
                    {
                        parameters.Add((ManagedTypeInfo.CreateTypeInfoForTypeSymbol(parameter.Type), parameter.Name, parameter.RefKind));
                    }

                    var diag = GetDiagnosticIfInvalidMethodForGeneration(comMethodDeclaringSyntax, (IMethodSymbol)member);

                    comMethodInfo = new((IMethodSymbol)member, comMethodDeclaringSyntax, member.Name, parameters.ToSequenceEqualImmutableArray(), diag);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Represents a method, its declaring interface, and its index in the interface's vtable.
        /// </summary>
        private sealed record ComInterfaceMethodContext(ComInterfaceContext DeclaringInterface, MethodInfo MethodInfo, int Index, IncrementalMethodStubGenerationContext GenerationContext)
        {
            public GeneratedMethodContextBase ManagedToUnmanagedStub
            {
                get
                {
                    if (GenerationContext.VtableIndexData.Direction is not (MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional))
                    {
                        return (GeneratedMethodContextBase)new SkippedStubContext(DeclaringInterface.InterfaceType);
                    }
                    var (methodStub, diagnostics) = VirtualMethodPointerStubGenerator.GenerateManagedToNativeStub(GenerationContext);
                    return new GeneratedStubCodeContext(GenerationContext.TypeKeyOwner, GenerationContext.ContainingSyntaxContext, new(methodStub), new(diagnostics));
                }
            }

            public Diagnostic? Diagnostic => MethodInfo.Diagnostic;

            public MethodDeclarationSyntax GenerateShadow()
            {
                // DeclarationCopiedFromBaseDeclaration(<Arguments>)
                // {
                //    return ((<baseInterfaceType>)this).<MethodName>(<Arguments>);
                // }
                return MethodInfo.Syntax.WithBody(
                    Block(
                        ReturnStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    CastExpression(DeclaringInterface.InterfaceType.Syntax, IdentifierName(Token(SyntaxKind.ThisKeyword))),
                                    IdentifierName(MethodInfo.MethodName)),
                                ArgumentList(
                                    // TODO: RefKind keywords
                                    SeparatedList(MethodInfo.Parameters.Select(p => Argument(IdentifierName(p.Name)))))))));
            }
        }

        /// <summary>
        /// Represents an interface and all of the methods that need to be generated for it (methods declared on the interface and methods inherited from base interfaces).
        /// </summary>
        private sealed record ComInterfaceAndMethods(ComInterfaceContext Interface, SequenceEqualImmutableArray<ComInterfaceMethodContext> Methods, ComInterfaceContext? BaseInterface)
        {
            /// <summary>
            /// COM methods that are declared on the attributed interface declaration.
            /// </summary>
            public IEnumerable<ComInterfaceMethodContext> DeclaredMethods => Methods.Where(m => m.DeclaringInterface == Interface);

            /// <summary>
            /// COM methods that are declared on an interface the interface inherits from.
            /// </summary>
            public IEnumerable<ComInterfaceMethodContext> ShadowingMethods => Methods.Where(m => m.DeclaringInterface != Interface);

            public static IEnumerable<ComInterfaceAndMethods> GetAllMethods(ValueEqualityImmutableDictionary<ComInterfaceContext, ComInterfaceContext?> ifaceToBaseMap, ValueEqualityImmutableDictionary<ComInterfaceContext, SequenceEqualImmutableArray<MethodInfo>> ifaceToMethodsMap, StubEnvironment environment, CancellationToken ct)
            {
                Dictionary<ComInterfaceContext, IEnumerable<ComInterfaceMethodContext>> allMethodsCache = new();

                foreach (var kvp in ifaceToMethodsMap)
                {
                    IEnumerable<ComInterfaceMethodContext> asdf = AddMethods(kvp.Key, kvp.Value);
                }

                return allMethodsCache.Select(kvp => new ComInterfaceAndMethods(kvp.Key, kvp.Value.ToSequenceEqualImmutableArray(), ifaceToBaseMap[kvp.Key]));

                IEnumerable<ComInterfaceMethodContext> AddMethods(ComInterfaceContext iface, IEnumerable<MethodInfo> declaredMethods)
                {
                    if (allMethodsCache.TryGetValue(iface, out var cachedValue))
                    {
                        return cachedValue;
                    }

                    int startingIndex = 3;
                    List<ComInterfaceMethodContext> methods = new();
                    if (ifaceToBaseMap.TryGetValue(iface, out var baseComIface) && baseComIface is not null)
                    {
                        if (!allMethodsCache.TryGetValue(baseComIface, out var baseMethods))
                        {
                            baseMethods = AddMethods(baseComIface, ifaceToMethodsMap[baseComIface]);
                        }

                        foreach (var method in baseMethods)
                        {
                            startingIndex++;
                            methods.Add(method);
                        }
                    }
                    foreach (var method in declaredMethods)
                    {
                        var ctx = CalculateStubInformation(method.Syntax, method.Symbol, startingIndex, environment, ct);
                        methods.Add(new ComInterfaceMethodContext(iface, method, startingIndex++, ctx));
                    }
                    allMethodsCache[iface] = methods;
                    return methods;
                }
            }
        }
    }
}
