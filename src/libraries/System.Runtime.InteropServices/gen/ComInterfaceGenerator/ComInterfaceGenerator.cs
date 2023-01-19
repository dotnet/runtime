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

namespace Microsoft.Interop
{
    [Generator]
    public sealed class ComInterfaceGenerator : IIncrementalGenerator
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
            // Get all methods with the [GeneratedComInterface] attribute.
            var attributedMethods = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    TypeNames.GeneratedComInterfaceAttribute,
                    static (node, ct) => node is InterfaceDeclarationSyntax,
                    static (context, ct) => context.TargetSymbol is INamedTypeSymbol methodSymbol
                        ? new { Syntax = (InterfaceDeclarationSyntax)context.TargetNode, Symbol = methodSymbol }
                        : null)
                .Where(
                    static modelData => modelData is not null);

            var interfacesWithDiagnostics = attributedMethods.Select(static (data, ct) =>
            {
                Diagnostic? diagnostic = GetDiagnosticIfInvalidTypeForGeneration(data.Syntax, data.Symbol);
                return new { data.Syntax, data.Symbol, Diagnostic = diagnostic };
            });

            // Split the methods we want to generate and the ones we don't into two separate groups.
            var interfacesToGenerate = interfacesWithDiagnostics.Where(static data => data.Diagnostic is null);
            var invalidTypeDiagnostics = interfacesWithDiagnostics.Where(static data => data.Diagnostic is not null);

            context.RegisterSourceOutput(invalidTypeDiagnostics, static (context, invalidMethod) =>
            {
                context.ReportDiagnostic(invalidMethod.Diagnostic);
            });

            var methodsWithDiagnostics = interfacesToGenerate
                .SelectMany(static (data, ct) =>
            {
                ContainingSyntaxContext containingSyntax = new(data.Syntax);
                Location interfaceLocation = data.Syntax.GetLocation();
                var methods = ImmutableArray.CreateBuilder<(MethodDeclarationSyntax Syntax, IMethodSymbol Symbol, int Index, Diagnostic? Diagnostic)>();
                // Start at offset 3 as 0-2 are IUnknown.
                int methodVtableOffset = 3;
                foreach (var member in data.Symbol.GetMembers())
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
                            // TODO: Report that this method is not supported.
                            methods.Add((null!, (IMethodSymbol)member, 0, member.CreateDiagnostic(null!)));
                        }
                        else
                        {
                            var syntax = (MethodDeclarationSyntax)data.Syntax.FindNode(locationInAttributeSyntax.SourceSpan);
                            var method = (IMethodSymbol)member;
                            Diagnostic? diagnostic = GetDiagnosticIfInvalidMethodForGeneration(syntax, method);
                            methods.Add((syntax, method, diagnostic is not null ? methodVtableOffset++ : 0, diagnostic));
                        }
                    }
                }
                return methods.ToImmutable();
            });

            // Split the methods we want to generate and the ones we don't into two separate groups.
            var methodsToGenerate = methodsWithDiagnostics.Where(static data => data.Diagnostic is null);
            var invalidMethodDiagnostics = methodsWithDiagnostics.Where(static data => data.Diagnostic is not null);

            context.RegisterSourceOutput(invalidTypeDiagnostics, static (context, invalidMethod) =>
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

            // Generate the code for the managed-to-unmangaed stubs and the diagnostics from code-generation.
            IncrementalValuesProvider<(MemberDeclarationSyntax, ImmutableArray<Diagnostic>)> generateManagedToNativeStub = generateStubInformation
                .Where(data => data.VtableIndexData.Direction is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                .Select(
                    static (data, ct) => VtableIndexStubGenerator.GenerateManagedToNativeStub(data)
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
            IncrementalValuesProvider<(MemberDeclarationSyntax, ImmutableArray<Diagnostic>)> generateNativeToManagedStub = nativeToManagedStubContexts
                .Select(
                    static (data, ct) => VtableIndexStubGenerator.GenerateNativeToManagedStub(data)
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

            var containingSyntaxContext = new ContainingSyntaxContext(syntax);

            var methodSyntaxTemplate = new ContainingSyntax(syntax.Modifiers.StripTriviaFromTokens(), SyntaxKind.MethodDeclaration, syntax.Identifier, syntax.TypeParameterList);

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
                new SequenceEqualImmutableArray<Diagnostic>(generatorDiagnostics.Diagnostics.ToImmutableArray()));
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

        private static MemberDeclarationSyntax GenerateNativeInterfaceMetadata(ContainingSyntaxContext context)
        {
            return context.WrapMemberInContainingSyntaxWithUnsafeModifier(
                InterfaceDeclaration("Native")
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.PartialKeyword)))
                .WithBaseList(BaseList(SingletonSeparatedList((BaseTypeSyntax)SimpleBaseType(IdentifierName(context.ContainingSyntax[0].Identifier)))))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(Attribute(ParseName(TypeNames.System_Runtime_InteropServices_DynamicInterfaceCastableImplementationAttribute))))));
        }

        private static MemberDeclarationSyntax GeneratePopulateVTableMethod(IGrouping<ContainingSyntaxContext, IncrementalMethodStubGenerationContext> vtableMethods)
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
                FunctionPointerTypeSyntax functionPointerType = VtableIndexStubGenerator.GenerateUnmanagedFunctionPointerTypeForMethod(method);

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
