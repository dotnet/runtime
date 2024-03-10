// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Interop.Analyzers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

[assembly: System.Resources.NeutralResourcesLanguage("en-US")]

namespace Microsoft.Interop
{
    [Generator]
    public sealed class VtableIndexStubGenerator : IIncrementalGenerator
    {
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
            IncrementalValuesProvider<IncrementalMethodStubGenerationContext> generateStubInformation = methodsToGenerate
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
            IncrementalValuesProvider<(MemberDeclarationSyntax, ImmutableArray<DiagnosticInfo>)> generateManagedToNativeStub = generateStubInformation
                .Where(data => data.VtableIndexData.Direction is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                .Select(
                    static (data, ct) => GenerateManagedToNativeStub(data)
                )
                .WithComparer(Comparers.GeneratedSyntax)
                .WithTrackingName(StepNames.GenerateManagedToNativeStub);

            context.RegisterDiagnostics(generateManagedToNativeStub.SelectMany((stubInfo, ct) => stubInfo.Item2));

            context.RegisterConcatenatedSyntaxOutputs(generateManagedToNativeStub.Select((data, ct) => data.Item1), "ManagedToNativeStubs.g.cs");

            // Filter the list of all stubs to only the stubs that requested unmanaged-to-managed stub generation.
            IncrementalValuesProvider<IncrementalMethodStubGenerationContext> nativeToManagedStubContexts =
                generateStubInformation
                .Where(data => data.VtableIndexData.Direction is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional);

            // Generate the code for the unmanaged-to-managed stubs and the diagnostics from code-generation.
            IncrementalValuesProvider<(MemberDeclarationSyntax, ImmutableArray<DiagnosticInfo>)> generateNativeToManagedStub = nativeToManagedStubContexts
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

        private static IncrementalMethodStubGenerationContext CalculateStubInformation(MethodDeclarationSyntax syntax, IMethodSymbol symbol, StubEnvironment environment, CancellationToken ct)
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

            var locations = new MethodSignatureDiagnosticLocations(syntax);
            var generatorDiagnostics = new GeneratorDiagnosticsBag(new DiagnosticDescriptorProvider(), locations, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.ComInterfaceGenerator.SR));

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
            var signatureContext = SignatureContext.Create(
                symbol,
                DefaultMarshallingInfoParser.Create(environment, generatorDiagnostics, symbol, virtualMethodIndexData, virtualMethodIndexAttr),
                environment,
                new CodeEmitOptions(SkipInit: true),
                typeof(VtableIndexStubGenerator).Assembly);

            var containingSyntaxContext = new ContainingSyntaxContext(syntax);

            var methodSyntaxTemplate = new ContainingSyntax(syntax.Modifiers.StripAccessibilityModifiers(), SyntaxKind.MethodDeclaration, syntax.Identifier, syntax.TypeParameterList);

            ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> callConv = VirtualMethodPointerStubGenerator.GenerateCallConvSyntaxFromAttributes(suppressGCTransitionAttribute, unmanagedCallConvAttribute, defaultCallingConventions: ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax>.Empty);

            var interfaceType = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(symbol.ContainingType);

            INamedTypeSymbol expectedUnmanagedInterfaceType = iUnmanagedInterfaceTypeType;

            bool implementsIUnmanagedInterfaceOfSelf = symbol.ContainingType.AllInterfaces.Any(iface => SymbolEqualityComparer.Default.Equals(iface, expectedUnmanagedInterfaceType));
            if (!implementsIUnmanagedInterfaceOfSelf)
            {
                // TODO: Report invalid configuration
            }

            var unmanagedObjectUnwrapper = symbol.ContainingType.GetAttributes().FirstOrDefault(att => att.AttributeClass.IsOfType(TypeNames.UnmanagedObjectUnwrapperAttribute));
            if (unmanagedObjectUnwrapper is null)
            {
                // TODO: report invalid configuration - or ensure that this will never happen at this point
            }
            var unwrapperSyntax = ParseTypeName(unmanagedObjectUnwrapper.AttributeClass.TypeArguments[0].ToDisplayString());

            MarshallingInfo exceptionMarshallingInfo = CreateExceptionMarshallingInfo(virtualMethodIndexAttr, symbol, environment.Compilation, generatorDiagnostics, virtualMethodIndexData);

            return new IncrementalMethodStubGenerationContext(
                signatureContext,
                containingSyntaxContext,
                methodSyntaxTemplate,
                locations,
                new SequenceEqualImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax>(callConv, SyntaxEquivalentComparer.Instance),
                VirtualMethodIndexData.From(virtualMethodIndexData),
                exceptionMarshallingInfo,
                environment.EnvironmentFlags,
                interfaceType,
                interfaceType,
                new SequenceEqualImmutableArray<DiagnosticInfo>(generatorDiagnostics.Diagnostics.ToImmutableArray()),
                new ObjectUnwrapperInfo(unwrapperSyntax));
        }

        private static MarshallingInfo CreateExceptionMarshallingInfo(AttributeData virtualMethodIndexAttr, ISymbol symbol, Compilation compilation, GeneratorDiagnosticsBag diagnostics, VirtualMethodIndexCompilationData virtualMethodIndexData)
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

        private static (MemberDeclarationSyntax, ImmutableArray<DiagnosticInfo>) GenerateManagedToNativeStub(
            IncrementalMethodStubGenerationContext methodStub)
        {
            var (stub, diagnostics) = VirtualMethodPointerStubGenerator.GenerateManagedToNativeStub(methodStub, VtableIndexStubGeneratorHelpers.GetGeneratorResolver);

            return (
                methodStub.ContainingSyntaxContext.AddContainingSyntax(
                    NativeTypeContainingSyntax)
                .WrapMemberInContainingSyntaxWithUnsafeModifier(
                    stub),
                methodStub.Diagnostics.Array.AddRange(diagnostics));
        }

        private static (MemberDeclarationSyntax, ImmutableArray<DiagnosticInfo>) GenerateNativeToManagedStub(
            IncrementalMethodStubGenerationContext methodStub)
        {
            var (stub, diagnostics) = VirtualMethodPointerStubGenerator.GenerateNativeToManagedStub(methodStub, VtableIndexStubGeneratorHelpers.GetGeneratorResolver);

            return (
                methodStub.ContainingSyntaxContext.AddContainingSyntax(
                    NativeTypeContainingSyntax)
                .WrapMemberInContainingSyntaxWithUnsafeModifier(
                    stub),
                methodStub.Diagnostics.Array.AddRange(diagnostics));
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

            // Verify there is an [UnmanagedObjectUnwrapperAttribute<TMapper>]
            if (!method.ContainingType.GetAttributes().Any(att => att.AttributeClass.IsOfType(TypeNames.UnmanagedObjectUnwrapperAttribute)))
            {
                return Diagnostic.Create(GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingUnmanagedObjectUnwrapperAttribute, methodSyntax.Identifier.GetLocation(), method.Name);
            }

            return null;
        }

        private static MemberDeclarationSyntax GenerateNativeInterfaceMetadata(ContainingSyntaxContext context)
        {
            return context.WrapMemberInContainingSyntaxWithUnsafeModifier(
                InterfaceDeclaration("Native")
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.PartialKeyword)))
                .WithBaseList(BaseList(SingletonSeparatedList((BaseTypeSyntax)SimpleBaseType(IdentifierName(context.ContainingSyntax[0].Identifier)))))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(Attribute(NameSyntaxes.System_Runtime_InteropServices_DynamicInterfaceCastableImplementationAttribute)))));
        }

        private static MemberDeclarationSyntax GeneratePopulateVTableMethod(IGrouping<ContainingSyntaxContext, IncrementalMethodStubGenerationContext> vtableMethods)
        {
            ContainingSyntaxContext containingSyntax = vtableMethods.Key.AddContainingSyntax(NativeTypeContainingSyntax);

            const string vtableParameter = "vtable";
            MethodDeclarationSyntax populateVtableMethod = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)),
                "PopulateUnmanagedVirtualMethodTable")
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.UnsafeKeyword)))
                .AddParameterListParameters(
                    Parameter(Identifier(vtableParameter))
                    .WithType(PointerType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))))));

            return containingSyntax.WrapMembersInContainingSyntaxWithUnsafeModifier(
                populateVtableMethod.WithBody(VirtualMethodPointerStubGenerator.GenerateVirtualMethodTableSlotAssignments(vtableMethods, vtableParameter, VtableIndexStubGeneratorHelpers.GetGeneratorResolver)));
        }
    }
}
