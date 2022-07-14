// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

[assembly: System.Resources.NeutralResourcesLanguage("en-US")]

namespace Microsoft.Interop
{
    [Generator]
    public sealed class VtableIndexStubGenerator : IIncrementalGenerator
    {
        internal sealed record IncrementalStubGenerationContext(
            StubEnvironment Environment,
            SignatureContext SignatureContext,
            ContainingSyntaxContext ContainingSyntaxContext,
            ContainingSyntax StubMethodSyntaxTemplate,
            MethodSignatureDiagnosticLocations DiagnosticLocation,
            ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> CallingConvention,
            VirtualMethodIndexData VtableIndexData,
            IMarshallingGeneratorFactory GeneratorFactory,
            ManagedTypeInfo TypeKeyType,
            ManagedTypeInfo TypeKeyOwner,
            ImmutableArray<Diagnostic> Diagnostics)
        {
            public bool Equals(IncrementalStubGenerationContext? other)
            {
                return other is not null
                    && StubEnvironment.AreCompilationSettingsEqual(Environment, other.Environment)
                    && SignatureContext.Equals(other.SignatureContext)
                    && ContainingSyntaxContext.Equals(other.ContainingSyntaxContext)
                    && VtableIndexData.Equals(other.VtableIndexData)
                    && CallingConvention.SequenceEqual(other.CallingConvention, (IEqualityComparer<FunctionPointerUnmanagedCallingConventionSyntax>)SyntaxEquivalentComparer.Instance)
                    && Diagnostics.SequenceEqual(other.Diagnostics);
            }

            public override int GetHashCode()
            {
                throw new UnreachableException();
            }
        }

        public static class StepNames
        {
            public const string CalculateStubInformation = nameof(CalculateStubInformation);
            public const string GenerateManagedToNativeStub = nameof(GenerateManagedToNativeStub);
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var attributedMethods = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (node, ct) => ShouldVisitNode(node),
                    static (context, ct) =>
                    {
                        MethodDeclarationSyntax syntax = (MethodDeclarationSyntax)context.Node;
                        if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is IMethodSymbol methodSymbol
                            && methodSymbol.GetAttributes().Any(static attribute => attribute.AttributeClass?.ToDisplayString() == TypeNames.VirtualMethodIndexAttribute))
                        {
                            return new { Syntax = syntax, Symbol = methodSymbol };
                        }

                        return null;
                    })
                .Where(
                    static modelData => modelData is not null);

            var methodsWithDiagnostics = attributedMethods.Select(static (data, ct) =>
            {
                Diagnostic? diagnostic = GetDiagnosticIfInvalidMethodForGeneration(data.Syntax, data.Symbol);
                return new { Syntax = data.Syntax, Symbol = data.Symbol, Diagnostic = diagnostic };
            });

            var methodsToGenerate = methodsWithDiagnostics.Where(static data => data.Diagnostic is null);
            var invalidMethodDiagnostics = methodsWithDiagnostics.Where(static data => data.Diagnostic is not null);

            context.RegisterSourceOutput(invalidMethodDiagnostics, static (context, invalidMethod) =>
            {
                context.ReportDiagnostic(invalidMethod.Diagnostic);
            });

            IncrementalValuesProvider<IncrementalStubGenerationContext> generateStubInformation = methodsToGenerate
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

            IncrementalValuesProvider<(MemberDeclarationSyntax, ImmutableArray<Diagnostic>)> generateManagedToNativeStub = generateStubInformation
                .Where(data => data.VtableIndexData.Direction is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                .Select(
                    static (data, ct) => GenerateManagedToNativeStub(data)
                )
                .WithComparer(Comparers.GeneratedSyntax)
                .WithTrackingName(StepNames.GenerateManagedToNativeStub);

            context.RegisterDiagnostics(generateManagedToNativeStub.SelectMany((stubInfo, ct) => stubInfo.Item2));

            context.RegisterConcatenatedSyntaxOutputs(generateManagedToNativeStub.Select((data, ct) => data.Item1), "ManagedToNativeStubs.g.cs");

            IncrementalValuesProvider<MemberDeclarationSyntax> generateNativeInterface = generateStubInformation
                .Select(static (context, ct) => context.ContainingSyntaxContext)
                .Collect()
                .SelectMany(static (syntaxContexts, ct) => syntaxContexts.Distinct())
                .Select(static (context, ct) => GenerateNativeInterfaceMetadata(context));

            context.RegisterConcatenatedSyntaxOutputs(generateNativeInterface, "NativeInterfaces.g.cs");
        }

        private static ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> GenerateCallConvSyntaxFromAttributes(AttributeData? suppressGCTransitionAttribute, AttributeData? unmanagedCallConvAttribute)
        {
            const string CallConvsField = "CallConvs";
            ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax>.Builder callingConventions = ImmutableArray.CreateBuilder<FunctionPointerUnmanagedCallingConventionSyntax>();

            if (suppressGCTransitionAttribute is not null)
            {
                callingConventions.Add(FunctionPointerUnmanagedCallingConvention(Identifier("SuppressGCTransition")));
            }
            if (unmanagedCallConvAttribute is not null)
            {
                foreach (KeyValuePair<string, TypedConstant> arg in unmanagedCallConvAttribute.NamedArguments)
                {
                    if (arg.Key == CallConvsField)
                    {
                        foreach (TypedConstant callConv in arg.Value.Values)
                        {
                            ITypeSymbol callConvSymbol = (ITypeSymbol)callConv.Value!;
                            if (callConvSymbol.Name.StartsWith("CallConv", StringComparison.Ordinal))
                            {
                                callingConventions.Add(FunctionPointerUnmanagedCallingConvention(Identifier(callConvSymbol.Name.Substring("CallConv".Length))));
                            }
                        }
                    }
                }
            }
            return callingConventions.ToImmutable();
        }

        private static SyntaxTokenList StripTriviaFromModifiers(SyntaxTokenList tokenList)
        {
            SyntaxToken[] strippedTokens = new SyntaxToken[tokenList.Count];
            for (int i = 0; i < tokenList.Count; i++)
            {
                strippedTokens[i] = tokenList[i].WithoutTrivia();
            }
            return new SyntaxTokenList(strippedTokens);
        }

        private static MemberDeclarationSyntax PrintGeneratedSource(
            ContainingSyntax userDeclaredMethod,
            ContainingSyntax originalInterfaceType,
            SignatureContext stub,
            BlockSyntax stubCode)
        {
            // Create stub function
            return MethodDeclaration(stub.StubReturnType, userDeclaredMethod.Identifier)
                .WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifier(IdentifierName(originalInterfaceType.Identifier)))
                .AddAttributeLists(stub.AdditionalAttributes.ToArray())
                .WithModifiers(StripTriviaFromModifiers(userDeclaredMethod.Modifiers))
                .WithParameterList(ParameterList(SeparatedList(stub.StubParameters)))
                .WithBody(stubCode);
        }

        private static VirtualMethodIndexData? ProcessVirtualMethodIndexAttribute(AttributeData attrData)
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
            if (namedArguments.TryGetValue(nameof(VirtualMethodIndexData.Direction), out TypedConstant directionValue))
            {
                // TypedConstant's Value property only contains primitive values.
                if (directionValue.Value is not int)
                {
                    return null;
                }
                // A boxed primitive can be unboxed to an enum with the same underlying type.
                direction = (MarshalDirection)directionValue.Value!;
            }
            if (namedArguments.TryGetValue(nameof(VirtualMethodIndexData.ImplicitThisParameter), out TypedConstant implicitThisValue))
            {
                if (implicitThisValue.Value is not bool)
                {
                    return null;
                }
                implicitThis = (bool)implicitThisValue.Value!;
            }

            return new VirtualMethodIndexData((int)attrData.ConstructorArguments[0].Value).WithValuesFromNamedArguments(namedArguments) with
            {
                Direction = direction,
                ImplicitThisParameter = implicitThis
            };
        }

        private static IncrementalStubGenerationContext CalculateStubInformation(MethodDeclarationSyntax syntax, IMethodSymbol symbol, StubEnvironment environment, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            INamedTypeSymbol? lcidConversionAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.LCIDConversionAttribute);
            INamedTypeSymbol? suppressGCTransitionAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.SuppressGCTransitionAttribute);
            INamedTypeSymbol? unmanagedCallConvAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.UnmanagedCallConvAttribute);
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

            var generatorDiagnostics = new GeneratorDiagnostics();

            // Process the LibraryImport attribute
            VirtualMethodIndexData? virtualMethodIndexData = ProcessVirtualMethodIndexAttribute(virtualMethodIndexAttr!);

            if (virtualMethodIndexData is null)
            {
                virtualMethodIndexData = new VirtualMethodIndexData(-1);
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
            var signatureContext = SignatureContext.Create(symbol, virtualMethodIndexData, environment, generatorDiagnostics, typeof(VtableIndexStubGenerator).Assembly);

            var containingSyntaxContext = new ContainingSyntaxContext(syntax);

            var methodSyntaxTemplate = new ContainingSyntax(syntax.Modifiers.StripTriviaFromTokens(), SyntaxKind.MethodDeclaration, syntax.Identifier, syntax.TypeParameterList);

            ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> callConv = GenerateCallConvSyntaxFromAttributes(suppressGCTransitionAttribute, unmanagedCallConvAttribute);

            var typeKeyOwner = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(symbol.ContainingType);
            ManagedTypeInfo typeKeyType = SpecialTypeInfo.Byte;

            IFieldSymbol? typeKeyField = symbol.ContainingType.GetMembers("TypeKey").OfType<IFieldSymbol>().FirstOrDefault(f => f.IsStatic);
            if (typeKeyField is null)
            {
                // Report invalid configuration
            }
            else
            {
                typeKeyType = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(typeKeyField.Type);
            }

            return new IncrementalStubGenerationContext(
                environment,
                signatureContext,
                containingSyntaxContext,
                methodSyntaxTemplate,
                new MethodSignatureDiagnosticLocations(syntax),
                callConv,
                virtualMethodIndexData,
                GetMarshallingGeneratorFactory(environment),
                typeKeyType,
                typeKeyOwner,
                generatorDiagnostics.Diagnostics.ToImmutableArray());
        }

        private static IMarshallingGeneratorFactory GetMarshallingGeneratorFactory(StubEnvironment env)
        {
            IAssemblySymbol coreLibraryAssembly = env.Compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly;
            ITypeSymbol? disabledRuntimeMarshallingAttributeType = coreLibraryAssembly.GetTypeByMetadataName(TypeNames.System_Runtime_CompilerServices_DisableRuntimeMarshallingAttribute);
            bool runtimeMarshallingDisabled = disabledRuntimeMarshallingAttributeType is not null
                && env.Compilation.Assembly.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, disabledRuntimeMarshallingAttributeType));
            InteropGenerationOptions options = new(UseMarshalType: true);
            IMarshallingGeneratorFactory generatorFactory;

            generatorFactory = new UnsupportedMarshallingFactory();
            generatorFactory = new NoMarshallingInfoErrorMarshallingFactory(generatorFactory);
            generatorFactory = new MarshalAsMarshallingGeneratorFactory(options, generatorFactory);

            IMarshallingGeneratorFactory elementFactory = new AttributedMarshallingModelGeneratorFactory(
                new CharMarshallingGeneratorFactory(generatorFactory, useBlittableMarshallerForUtf16: true), new AttributedMarshallingModelOptions(runtimeMarshallingDisabled, MarshalMode.ElementIn, MarshalMode.ElementRef, MarshalMode.ElementOut));
            // We don't need to include the later generator factories for collection elements
            // as the later generator factories only apply to parameters.
            generatorFactory = new AttributedMarshallingModelGeneratorFactory(generatorFactory, elementFactory, new AttributedMarshallingModelOptions(runtimeMarshallingDisabled, MarshalMode.ManagedToUnmanagedIn, MarshalMode.ManagedToUnmanagedRef, MarshalMode.ManagedToUnmanagedOut));

            generatorFactory = new ByValueContentsMarshalKindValidator(generatorFactory);
            return generatorFactory;
        }

        private static (MemberDeclarationSyntax, ImmutableArray<Diagnostic>) GenerateManagedToNativeStub(
            IncrementalStubGenerationContext methodStub)
        {
            var diagnostics = new GeneratorDiagnostics();

            // Generate stub code
            var stubGenerator = new ManagedToNativeVTableMethodGenerator(
                methodStub.Environment,
                methodStub.SignatureContext.ElementTypeInformation,
                methodStub.VtableIndexData.SetLastError,
                methodStub.VtableIndexData.ImplicitThisParameter,
                (elementInfo, ex) =>
                {
                    diagnostics.ReportMarshallingNotSupported(methodStub.DiagnosticLocation, elementInfo, ex.NotSupportedDetails);
                },
                methodStub.GeneratorFactory);

            BlockSyntax code = stubGenerator.GenerateStubBody(
                methodStub.VtableIndexData.Index,
                methodStub.CallingConvention,
                methodStub.TypeKeyOwner.Syntax,
                methodStub.TypeKeyType);

            return (
                methodStub.ContainingSyntaxContext.AddContainingSyntax(
                    new ContainingSyntax(
                        TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.PartialKeyword)),
                        SyntaxKind.InterfaceDeclaration,
                        Identifier("Native"),
                        null))
                .WrapMemberInContainingSyntaxWithUnsafeModifier(
                    PrintGeneratedSource(
                        methodStub.StubMethodSyntaxTemplate,
                        methodStub.ContainingSyntaxContext.ContainingSyntax[0],
                        methodStub.SignatureContext,
                        code)),
                methodStub.Diagnostics.AddRange(diagnostics.Diagnostics));
        }

        private static bool ShouldVisitNode(SyntaxNode syntaxNode)
        {
            // We only support C# method declarations.
            if (syntaxNode.Language != LanguageNames.CSharp
                || !syntaxNode.IsKind(SyntaxKind.MethodDeclaration))
            {
                return false;
            }

            // Filter out methods with no attributes early.
            return ((MethodDeclarationSyntax)syntaxNode).AttributeLists.Count > 0;
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
    }
}
