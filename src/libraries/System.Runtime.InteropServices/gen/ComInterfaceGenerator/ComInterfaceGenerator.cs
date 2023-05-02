// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    [Generator]
    public sealed class ComInterfaceGenerator : IIncrementalGenerator
    {
        private sealed record ComInterfaceContext(
            ManagedTypeInfo InterfaceType,
            ContainingSyntaxContext TypeDefinitionContext,
            ContainingSyntax InterfaceTypeSyntax,
            int MethodStartIndex,
            Guid InterfaceId);

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

            var interfacesWithDiagnostics = attributedInterfaces.Select(static (data, ct) =>
            {
                Diagnostic? diagnostic = GetDiagnosticIfInvalidTypeForGeneration(data.Syntax, data.Symbol);
                return new { data.Syntax, data.Symbol, Diagnostic = diagnostic };
            });

            // Split the types we want to generate and the ones we don't into two separate groups.
            var interfacesToGenerate = interfacesWithDiagnostics.Where(static data => data.Diagnostic is null);
            var invalidTypeDiagnostics = interfacesWithDiagnostics.Where(static data => data.Diagnostic is not null);

            var interfaceBaseInfo = interfacesToGenerate.Collect().SelectMany((data, ct) =>
            {
                ImmutableArray<(int StartingOffset, ManagedTypeInfo? BaseInterface, bool IsMarkerInterface)>.Builder baseInterfaceInfo = ImmutableArray.CreateBuilder<(int, ManagedTypeInfo?, bool)>(data.Length);
                // Track the calculated last offsets of the interfaces.
                // If a type has invalid methods, we'll count them and issue an error when generating code for that
                // interface.
                Dictionary<INamedTypeSymbol, int> derivedNextOffset = new(SymbolEqualityComparer.Default);
                foreach (var iface in data)
                {
                    var (starting, baseType, derivedStarting) = CalculateOffsetsForInterface(iface.Symbol, derivedNextOffset);
                    baseInterfaceInfo.Add((starting, baseType is not null ? ManagedTypeInfo.CreateTypeInfoForTypeSymbol(baseType) : null, starting == derivedStarting));
                }
                return baseInterfaceInfo.MoveToImmutable();

                static (int Starting, INamedTypeSymbol? BaseType, int DerivedStarting) CalculateOffsetsForInterface(INamedTypeSymbol iface, Dictionary<INamedTypeSymbol, int> derivedNextOffsetCache)
                {
                    INamedTypeSymbol? baseInterface = null;
                    foreach (var implemented in iface.Interfaces)
                    {
                        if (implemented.GetAttributes().Any(static attr => attr.AttributeClass?.ToDisplayString() == TypeNames.GeneratedComInterfaceAttribute))
                        {
                            // We'll filter out cases where there's multiple matching interfaces when determining
                            // if this is a valid candidate for generation.
                            Debug.Assert(baseInterface is null);
                            baseInterface = implemented;
                        }
                    }

                    // Cache the starting offsets for each base interface so we don't have to recalculate them.
                    int startingOffset = 3;
                    if (baseInterface is not null)
                    {
                        if (!derivedNextOffsetCache.TryGetValue(baseInterface, out int offset))
                        {
                            offset = CalculateOffsetsForInterface(baseInterface, derivedNextOffsetCache).DerivedStarting;
                        }

                        startingOffset = offset;
                    }

                    // This calculation isn't strictly accurate. This will count methods that aren't in the same declaring syntax as the attribute on the interface,
                    // but we'll emit an error later if that's a problem. We also can't detect this error if the base type is in metadata.
                    int ifaceDerivedNextOffset = startingOffset + iface.GetMembers().Where(static m => m is IMethodSymbol { IsStatic: false }).Count();
                    derivedNextOffsetCache[iface] = ifaceDerivedNextOffset;

                    return (startingOffset, baseInterface, ifaceDerivedNextOffset);
                }
            });

            // Zip the interface base information back with the symbols and syntax for the interface
            // to calculate the interface context.
            // The generator infrastructure preserves ordering of the tables once Select statements are in use,
            // so we can rely on the order matching here.
            var interfaceContexts = interfacesToGenerate
                .Zip(interfaceBaseInfo.Select((data, ct) => data.StartingOffset))
                .Select((data, ct) =>
            {
                var (iface, startingOffset) = data;
                Guid? guid = null;
                var guidAttr = iface.Symbol.GetAttributes().Where(attr => attr.AttributeClass.ToDisplayString() == TypeNames.System_Runtime_InteropServices_GuidAttribute).SingleOrDefault();
                if (guidAttr is not null)
                {
                    string? guidstr = guidAttr.ConstructorArguments.SingleOrDefault().Value as string;
                    if (guidstr is not null)
                        guid = new Guid(guidstr);
                }
                return new ComInterfaceContext(
                    ManagedTypeInfo.CreateTypeInfoForTypeSymbol(iface.Symbol),
                    new ContainingSyntaxContext(iface.Syntax),
                    new ContainingSyntax(iface.Syntax.Modifiers, iface.Syntax.Kind(), iface.Syntax.Identifier, iface.Syntax.TypeParameterList),
                    startingOffset,
                    guid ?? Guid.Empty);
            });

            context.RegisterDiagnostics(invalidTypeDiagnostics.Select((data, ct) => data.Diagnostic));

            // Zip the incremental interface context back with the symbols and syntax for the interface
            // to calculate the methods to generate.
            var interfacesWithMethods = interfacesToGenerate
                .Zip(interfaceContexts)
                .Select(static (data, ct) =>
            {
                var (interfaceData, interfaceContext) = data;
                Location interfaceLocation = interfaceData.Syntax.GetLocation();
                var methods = ImmutableArray.CreateBuilder<(MethodDeclarationSyntax Syntax, IMethodSymbol Symbol, int Index, Diagnostic? Diagnostic)>();
                int methodVtableOffset = interfaceContext.MethodStartIndex;
                foreach (var member in interfaceData.Symbol.GetMembers())
                {
                    if (member.Kind == SymbolKind.Method && !member.IsStatic)
                    {
                        // We only support methods that are defined in the same partial interface definition as the
                        // [GeneratedComInterface] attribute.
                        // This restriction not only makes finding the syntax for a given method cheaper,
                        // but it also enables us to ensure that we can determine vtable method order easily.
                        Location? locationInAttributeSyntax = null;
                        foreach (var location in member.Locations)
                        {
                            if (location.SourceTree == interfaceLocation.SourceTree
                                && interfaceLocation.SourceSpan.Contains(location.SourceSpan))
                            {
                                locationInAttributeSyntax = location;
                            }
                        }

                        if (locationInAttributeSyntax is null)
                        {
                            methods.Add((
                                null!,
                                (IMethodSymbol)member,
                                0,
                                member.CreateDiagnostic(
                                    GeneratorDiagnostics.MethodNotDeclaredInAttributedInterface,
                                    member.ToDisplayString(),
                                    interfaceData.Symbol.ToDisplayString())));
                        }
                        else
                        {
                            var syntax = (MethodDeclarationSyntax)interfaceData.Syntax.FindNode(locationInAttributeSyntax.SourceSpan);
                            var method = (IMethodSymbol)member;
                            Diagnostic? diagnostic = GetDiagnosticIfInvalidMethodForGeneration(syntax, method);
                            methods.Add((syntax, method, diagnostic is null ? methodVtableOffset++ : 0, diagnostic));
                        }
                    }
                }
                return (Interface: interfaceContext, Methods: methods.ToImmutable());
            });

            var interfaceWithMethodsContexts = interfacesWithMethods
                .Where(data => data.Methods.Length > 0)
                .Select(static (data, ct) => data.Interface);

            // Marker interfaces are COM interfaces that don't have any methods.
            // The lack of methods breaks the mechanism we use later to stitch back together interface-level data
            // and method-level data, but that's okay because marker interfaces are much simpler.
            // We'll handle them seperately because they are so simple.
            var markerInterfaces = interfacesWithMethods
                .Where(data => data.Methods.Length == 0)
                .Select(static (data, ct) => data.Interface);

            var markerInterfaceIUnknownDerived = markerInterfaces.Select(static (context, ct) => GenerateIUnknownDerivedAttributeApplication(context))
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            context.RegisterSourceOutput(markerInterfaces.Zip(markerInterfaceIUnknownDerived), (context, data) =>
            {
                var (interfaceContext, iUnknownDerivedAttributeApplication) = data;
                context.AddSource(
                    interfaceContext.InterfaceType.FullTypeName.Replace("global::", ""),
                    GenerateMarkerInterfaceSource(interfaceContext) + iUnknownDerivedAttributeApplication);
            });

            var methodsWithDiagnostics = interfacesWithMethods.SelectMany(static (data, ct) => data.Methods);

            // Split the methods we want to generate and the ones we don't into two separate groups.
            var methodsToGenerate = methodsWithDiagnostics.Where(static data => data.Diagnostic is null);
            var invalidMethodDiagnostics = methodsWithDiagnostics.Where(static data => data.Diagnostic is not null);

            context.RegisterSourceOutput(invalidMethodDiagnostics, static (context, invalidMethod) =>
            {
                context.ReportDiagnostic(invalidMethod.Diagnostic);
            });

            // Calculate all of information to generate both managed-to-unmanaged and unmanaged-to-managed stubs
            // for each method.
            IncrementalValuesProvider<IncrementalMethodStubGenerationContext> generateStubInformation = methodsToGenerate
                .Combine(context.CreateStubEnvironmentProvider())
                .Select(static (data, ct) => new
                {
                    data.Left.Syntax,
                    data.Left.Symbol,
                    data.Left.Index,
                    Environment = data.Right
                })
                .Select(
                    static (data, ct) => CalculateStubInformation(data.Syntax, data.Symbol, data.Index, data.Environment, ct)
                )
                .WithTrackingName(StepNames.CalculateStubInformation);

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

            var managedToNativeInterfaceImplementations = generateManagedToNativeStub
                .Collect()
                .SelectMany(static (stubs, ct) => GroupContextsForInterfaceGeneration(stubs))
                .Select(static (interfaceGroup, ct) => GenerateImplementationInterface(interfaceGroup.Array))
                .WithTrackingName(StepNames.GenerateManagedToNativeInterfaceImplementation)
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
            var nativeInterfaceInformation = interfaceWithMethodsContexts
                .Select(static (context, ct) => GenerateInterfaceInformation(context))
                .WithTrackingName(StepNames.GenerateInterfaceInformation)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            // Generate a method named CreateManagedVirtualFunctionTable on the native interface implementation
            // that allocates and fills in the memory for the vtable.
            var nativeToManagedVtables =
                generateStubInformation
                .Collect()
                .SelectMany(static (data, ct) => GroupContextsForInterfaceGeneration(data.CastArray<GeneratedMethodContextBase>()))
                .Zip(interfaceBaseInfo.Where(static info => !info.IsMarkerInterface))
                .Select(static (data, ct) => GenerateImplementationVTable(ImmutableArray.CreateRange(data.Left.Array.Cast<IncrementalMethodStubGenerationContext>()), data.Right))
                .WithTrackingName(StepNames.GenerateNativeToManagedVTable)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            var iUnknownDerivedAttributeApplication = interfaceWithMethodsContexts
                .Select(static (context, ct) => GenerateIUnknownDerivedAttributeApplication(context))
                .WithTrackingName(StepNames.GenerateIUnknownDerivedAttribute)
                .WithComparer(SyntaxEquivalentComparer.Instance)
                .SelectNormalized();

            var filesToGenerate = interfaceWithMethodsContexts
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
                TypeDeclaration(context.InterfaceTypeSyntax.TypeKind, context.InterfaceTypeSyntax.Identifier)
                    .WithModifiers(context.InterfaceTypeSyntax.Modifiers)
                    .WithTypeParameterList(context.InterfaceTypeSyntax.TypeParameters)
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
                new SequenceEqualImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax>(callConv, SyntaxEquivalentComparer.Instance),
                virtualMethodIndexData,
                new ComExceptionMarshalling(),
                ComInterfaceGeneratorHelpers.CreateGeneratorFactory(environment, MarshalDirection.ManagedToUnmanaged),
                ComInterfaceGeneratorHelpers.CreateGeneratorFactory(environment, MarshalDirection.UnmanagedToManaged),
                typeKeyOwner,
                new SequenceEqualImmutableArray<Diagnostic>(generatorDiagnostics.Diagnostics.ToImmutableArray()),
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
        private static InterfaceDeclarationSyntax GenerateImplementationInterface(ImmutableArray<GeneratedMethodContextBase> interfaceGroup)
        {
            var definingType = interfaceGroup[0].OriginalDefiningType;
            return ImplementationInterfaceTemplate
                .AddBaseListTypes(SimpleBaseType(definingType.Syntax))
                .WithMembers(List<MemberDeclarationSyntax>(interfaceGroup.OfType<GeneratedStubCodeContext>().Select(context => context.Stub.Node)))
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
        private static InterfaceDeclarationSyntax GenerateImplementationVTable(ImmutableArray<IncrementalMethodStubGenerationContext> interfaceMethodStubs, (int StartingOffset, ManagedTypeInfo? BaseInterface, bool) baseInterfaceTypeInfo)
        {
            const string vtableLocalName = "vtable";
            var interfaceType = interfaceMethodStubs[0].OriginalDefiningType;

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
                                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1 + interfaceMethodStubs.Max(x => x.VtableIndexData.Index))))))))))));

            BlockSyntax fillBaseInterfaceSlots;

            if (baseInterfaceTypeInfo.BaseInterface is null)
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
                                                                            ParseTypeName(baseInterfaceTypeInfo.BaseInterface.FullTypeName)),
                                                                        IdentifierName("TypeHandle")))))),
                                                    IdentifierName("ManagedVirtualMethodTable"))),
                                            Argument(IdentifierName(vtableLocalName)),
                                            Argument(CastExpression(IdentifierName("nuint"),
                                                ParenthesizedExpression(
                                                    BinaryExpression(SyntaxKind.MultiplyExpression,
                                                        SizeOfExpression(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))),
                                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(baseInterfaceTypeInfo.StartingOffset))))))
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
    }
}
