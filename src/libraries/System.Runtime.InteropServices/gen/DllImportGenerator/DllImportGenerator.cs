// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
    public sealed class DllImportGenerator : IIncrementalGenerator
    {
        internal sealed record IncrementalStubGenerationContext(
            StubEnvironment Environment,
            DllImportStubContext StubContext,
            ImmutableArray<AttributeSyntax> ForwardedAttributes,
            GeneratedDllImportData DllImportData,
            ImmutableArray<Diagnostic> Diagnostics)
        {
            public bool Equals(IncrementalStubGenerationContext? other)
            {
                return other is not null
                    && StubEnvironment.AreCompilationSettingsEqual(Environment, other.Environment)
                    && StubContext.Equals(other.StubContext)
                    && DllImportData.Equals(other.DllImportData)
                    && ForwardedAttributes.SequenceEqual(other.ForwardedAttributes, (IEqualityComparer<AttributeSyntax>)SyntaxEquivalentComparer.Instance)
                    && Diagnostics.SequenceEqual(other.Diagnostics);
            }

            public override int GetHashCode()
            {
                throw new UnreachableException();
            }
        }

        public class IncrementalityTracker
        {
            public enum StepName
            {
                CalculateStubInformation,
                GenerateSingleStub,
                NormalizeWhitespace,
                ConcatenateStubs,
                OutputSourceFile
            }

            public record ExecutedStepInfo(StepName Step, object Input);

            private readonly List<ExecutedStepInfo> _executedSteps = new();
            public IEnumerable<ExecutedStepInfo> ExecutedSteps => _executedSteps;

            internal void RecordExecutedStep(ExecutedStepInfo step) => _executedSteps.Add(step);
        }

        /// <summary>
        /// This property provides a test-only hook to enable testing the incrementality of the source generator.
        /// This will be removed when https://github.com/dotnet/roslyn/issues/54832 is implemented and can be consumed.
        /// </summary>
        public IncrementalityTracker? IncrementalTracker { get; set; }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var attributedMethods = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (node, ct) => ShouldVisitNode(node),
                    static (context, ct) =>
                        new
                        {
                            Syntax = (MethodDeclarationSyntax)context.Node,
                            Symbol = (IMethodSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node, ct)!
                        })
                .Where(
                    static modelData => modelData.Symbol.IsStatic && modelData.Symbol.GetAttributes().Any(
                        static attribute => attribute.AttributeClass?.ToDisplayString() == TypeNames.GeneratedDllImportAttribute)
                );

            var methodsToGenerate = attributedMethods.Where(static data => !data.Symbol.ReturnsByRef && !data.Symbol.ReturnsByRefReadonly);

            var refReturnMethods = attributedMethods.Where(static data => data.Symbol.ReturnsByRef || data.Symbol.ReturnsByRefReadonly);

            context.RegisterSourceOutput(refReturnMethods, static (context, refReturnMethod) =>
            {
                context.ReportDiagnostic(Diagnostic.Create(GeneratorDiagnostics.ReturnConfigurationNotSupported, refReturnMethod.Syntax.GetLocation(), "ref return", refReturnMethod.Symbol.ToDisplayString()));
            });

            IncrementalValueProvider<(Compilation compilation, TargetFramework targetFramework, Version targetFrameworkVersion)> compilationAndTargetFramework = context.CompilationProvider
                .Select(static (compilation, ct) =>
                {
                    TargetFramework fmk = DetermineTargetFramework(compilation, out Version targetFrameworkVersion);
                    return (compilation, fmk, targetFrameworkVersion);
                });

            context.RegisterSourceOutput(
                compilationAndTargetFramework
                    .Combine(methodsToGenerate.Collect()),
                static (context, data) =>
                {
                    if (data.Left.targetFramework is TargetFramework.Unknown && data.Right.Any())
                    {
                        // We don't block source generation when the TFM is unknown.
                        // This allows a user to copy generated source and use it as a starting point
                        // for manual marshalling if desired.
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                GeneratorDiagnostics.TargetFrameworkNotSupported,
                                Location.None,
                                data.Left.targetFrameworkVersion));
                    }
                });

            IncrementalValueProvider<DllImportGeneratorOptions> stubOptions = context.AnalyzerConfigOptionsProvider
                .Select((options, ct) => new DllImportGeneratorOptions(options.GlobalOptions));

            IncrementalValueProvider<StubEnvironment> stubEnvironment = compilationAndTargetFramework
                .Combine(stubOptions)
                .Select(
                    static (data, ct) =>
                        new StubEnvironment(
                            data.Left.compilation,
                            data.Left.targetFramework,
                            data.Left.targetFrameworkVersion,
                            data.Left.compilation.SourceModule.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == TypeNames.System_Runtime_CompilerServices_SkipLocalsInitAttribute),
                            data.Right)
                );

            IncrementalValueProvider<(string, ImmutableArray<Diagnostic>)> methodSourceAndDiagnostics = methodsToGenerate
                .Combine(stubEnvironment)
                .Select(static (data, ct) => new
                {
                    data.Left.Syntax,
                    data.Left.Symbol,
                    Environment = data.Right
                })
                .Select(
                    (data, ct) =>
                    {
                        IncrementalTracker?.RecordExecutedStep(new IncrementalityTracker.ExecutedStepInfo(IncrementalityTracker.StepName.CalculateStubInformation, data));
                        return (data.Syntax, StubContext: CalculateStubInformation(data.Symbol, data.Environment, ct));
                    }
                )
                .WithComparer(Comparers.CalculatedContextWithSyntax)
                .Combine(stubOptions)
                .Select(
                    (data, ct) =>
                    {
                        IncrementalTracker?.RecordExecutedStep(new IncrementalityTracker.ExecutedStepInfo(IncrementalityTracker.StepName.GenerateSingleStub, data));
                        return GenerateSource(data.Left.StubContext, data.Left.Syntax, data.Right);
                    }
                )
                .WithComparer(Comparers.GeneratedSyntax)
                // Handle NormalizeWhitespace as a separate stage for incremental runs since it is an expensive operation.
                .Select(
                    (data, ct) =>
                    {
                        IncrementalTracker?.RecordExecutedStep(new IncrementalityTracker.ExecutedStepInfo(IncrementalityTracker.StepName.NormalizeWhitespace, data));
                        return (data.Item1.NormalizeWhitespace().ToFullString(), data.Item2);
                    })
                .Collect()
                .WithComparer(Comparers.GeneratedSourceSet)
                .Select((generatedSources, ct) =>
                {
                    IncrementalTracker?.RecordExecutedStep(new IncrementalityTracker.ExecutedStepInfo(IncrementalityTracker.StepName.ConcatenateStubs, generatedSources));
                    StringBuilder source = new();
                    // Mark in source that the file is auto-generated.
                    source.AppendLine("// <auto-generated/>");
                    ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
                    foreach ((string, ImmutableArray<Diagnostic>) generated in generatedSources)
                    {
                        source.AppendLine(generated.Item1);
                        diagnostics.AddRange(generated.Item2);
                    }
                    return (source: source.ToString(), diagnostics: diagnostics.ToImmutable());
                })
                .WithComparer(Comparers.GeneratedSource);

            context.RegisterSourceOutput(methodSourceAndDiagnostics,
                (context, data) =>
                {
                    IncrementalTracker?.RecordExecutedStep(new IncrementalityTracker.ExecutedStepInfo(IncrementalityTracker.StepName.OutputSourceFile, data));
                    foreach (Diagnostic diagnostic in data.Item2)
                    {
                        context.ReportDiagnostic(diagnostic);
                    }

                    context.AddSource("GeneratedDllImports.g.cs", data.Item1);
                });
        }

        private static List<AttributeSyntax> GenerateSyntaxForForwardedAttributes(AttributeData? suppressGCTransitionAttribute, AttributeData? unmanagedCallConvAttribute, AttributeData? defaultDllImportSearchPathsAttribute)
        {
            const string CallConvsField = "CallConvs";
            // Manually rehydrate the forwarded attributes with fully qualified types so we don't have to worry about any using directives.
            List<AttributeSyntax> attributes = new();

            if (suppressGCTransitionAttribute is not null)
            {
                attributes.Add(Attribute(ParseName(TypeNames.SuppressGCTransitionAttribute)));
            }
            if (unmanagedCallConvAttribute is not null)
            {
                AttributeSyntax unmanagedCallConvSyntax = Attribute(ParseName(TypeNames.UnmanagedCallConvAttribute));
                foreach (KeyValuePair<string, TypedConstant> arg in unmanagedCallConvAttribute.NamedArguments)
                {
                    if (arg.Key == CallConvsField)
                    {
                        InitializerExpressionSyntax callConvs = InitializerExpression(SyntaxKind.ArrayInitializerExpression);
                        foreach (TypedConstant callConv in arg.Value.Values)
                        {
                            callConvs = callConvs.AddExpressions(
                                TypeOfExpression(((ITypeSymbol)callConv.Value!).AsTypeSyntax()));
                        }

                        ArrayTypeSyntax arrayOfSystemType = ArrayType(ParseTypeName(TypeNames.System_Type), SingletonList(ArrayRankSpecifier()));

                        unmanagedCallConvSyntax = unmanagedCallConvSyntax.AddArgumentListArguments(
                            AttributeArgument(
                                ArrayCreationExpression(arrayOfSystemType)
                                .WithInitializer(callConvs))
                            .WithNameEquals(NameEquals(IdentifierName(CallConvsField))));
                    }
                }
                attributes.Add(unmanagedCallConvSyntax);
            }
            if (defaultDllImportSearchPathsAttribute is not null)
            {
                attributes.Add(
                    Attribute(ParseName(TypeNames.DefaultDllImportSearchPathsAttribute)).AddArgumentListArguments(
                        AttributeArgument(
                            CastExpression(ParseTypeName(TypeNames.DllImportSearchPath),
                                LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    Literal((int)defaultDllImportSearchPathsAttribute.ConstructorArguments[0].Value!))))));
            }
            return attributes;
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

        private static SyntaxTokenList AddToModifiers(SyntaxTokenList modifiers, SyntaxKind modifierToAdd)
        {
            if (modifiers.IndexOf(modifierToAdd) >= 0)
                return modifiers;

            int idx = modifiers.IndexOf(SyntaxKind.PartialKeyword);
            return idx >= 0
                ? modifiers.Insert(idx, Token(modifierToAdd))
                : modifiers.Add(Token(modifierToAdd));
        }

        private static TypeDeclarationSyntax CreateTypeDeclarationWithoutTrivia(TypeDeclarationSyntax typeDeclaration)
        {
            return TypeDeclaration(
                typeDeclaration.Kind(),
                typeDeclaration.Identifier)
                .WithTypeParameterList(typeDeclaration.TypeParameterList)
                .WithModifiers(StripTriviaFromModifiers(typeDeclaration.Modifiers));
        }

        private static MemberDeclarationSyntax PrintGeneratedSource(
            MethodDeclarationSyntax userDeclaredMethod,
            DllImportStubContext stub,
            BlockSyntax stubCode)
        {
            // Create stub function
            MethodDeclarationSyntax stubMethod = MethodDeclaration(stub.StubReturnType, userDeclaredMethod.Identifier)
                .AddAttributeLists(stub.AdditionalAttributes.ToArray())
                .WithModifiers(StripTriviaFromModifiers(userDeclaredMethod.Modifiers))
                .WithParameterList(ParameterList(SeparatedList(stub.StubParameters)))
                .WithBody(stubCode);

            MemberDeclarationSyntax toPrint = WrapMethodInContainingScopes(stub, stubMethod);

            return toPrint;
        }

        private static MemberDeclarationSyntax WrapMethodInContainingScopes(DllImportStubContext stub, MethodDeclarationSyntax stubMethod)
        {
            // Stub should have at least one containing type
            Debug.Assert(stub.StubContainingTypes.Any());

            // Add stub function and DllImport declaration to the first (innermost) containing
            MemberDeclarationSyntax containingType = CreateTypeDeclarationWithoutTrivia(stub.StubContainingTypes.First())
                .AddMembers(stubMethod);

            // Mark containing type as unsafe such that all the generated functions will be in an unsafe context.
            containingType = containingType.WithModifiers(AddToModifiers(containingType.Modifiers, SyntaxKind.UnsafeKeyword));

            // Add type to the remaining containing types (skipping the first which was handled above)
            foreach (TypeDeclarationSyntax typeDecl in stub.StubContainingTypes.Skip(1))
            {
                containingType = CreateTypeDeclarationWithoutTrivia(typeDecl)
                    .WithMembers(SingletonList(containingType));
            }

            MemberDeclarationSyntax toPrint = containingType;

            // Add type to the containing namespace
            if (stub.StubTypeNamespace is not null)
            {
                toPrint = NamespaceDeclaration(IdentifierName(stub.StubTypeNamespace))
                    .AddMembers(toPrint);
            }

            return toPrint;
        }

        private static TargetFramework DetermineTargetFramework(Compilation compilation, out Version version)
        {
            IAssemblySymbol systemAssembly = compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly;
            version = systemAssembly.Identity.Version;

            return systemAssembly.Identity.Name switch
            {
                // .NET Framework
                "mscorlib" => TargetFramework.Framework,
                // .NET Standard
                "netstandard" => TargetFramework.Standard,
                // .NET Core (when version < 5.0) or .NET
                "System.Runtime" or "System.Private.CoreLib" =>
                    (version.Major < 5) ? TargetFramework.Core : TargetFramework.Net,
                _ => TargetFramework.Unknown,
            };
        }

        private static GeneratedDllImportData? ProcessGeneratedDllImportAttribute(AttributeData attrData)
        {
            // Found the GeneratedDllImport, but it has an error so report the error.
            // This is most likely an issue with targeting an incorrect TFM.
            if (attrData.AttributeClass?.TypeKind is null or TypeKind.Error)
            {
                return null;
            }

            // Default values for these properties are based on the
            // documented semanatics of DllImportAttribute:
            //   - https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute
            DllImportMember userDefinedValues = DllImportMember.None;
            string? entryPoint = null;
            bool setLastError = false;

            StringMarshalling stringMarshalling = StringMarshalling.Custom;
            INamedTypeSymbol? stringMarshallingCustomType = null;

            // All other data on attribute is defined as NamedArguments.
            foreach (KeyValuePair<string, TypedConstant> namedArg in attrData.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    default:
                        // This should never occur in a released build,
                        // but can happen when evolving the ecosystem.
                        // Return null here to indicate invalid attribute data.
                        Debug.WriteLine($"An unknown member '{namedArg.Key}' was found on {attrData.AttributeClass}");
                        return null;
                    case nameof(GeneratedDllImportData.EntryPoint):
                        userDefinedValues |= DllImportMember.EntryPoint;
                        if (namedArg.Value.Value is not string)
                        {
                            return null;
                        }
                        entryPoint = (string)namedArg.Value.Value!;
                        break;
                    case nameof(GeneratedDllImportData.SetLastError):
                        userDefinedValues |= DllImportMember.SetLastError;
                        if (namedArg.Value.Value is not bool)
                        {
                            return null;
                        }
                        setLastError = (bool)namedArg.Value.Value!;
                        break;
                    case nameof(GeneratedDllImportData.StringMarshalling):
                        userDefinedValues |= DllImportMember.StringMarshalling;
                        // TypedConstant's Value property only contains primitive values.
                        if (namedArg.Value.Value is not int)
                        {
                            return null;
                        }
                        // A boxed primitive can be unboxed to an enum with the same underlying type.
                        stringMarshalling = (StringMarshalling)namedArg.Value.Value!;
                        break;
                    case nameof(GeneratedDllImportData.StringMarshallingCustomType):
                        userDefinedValues |= DllImportMember.StringMarshallingCustomType;
                        if (namedArg.Value.Value is not INamedTypeSymbol)
                        {
                            return null;
                        }
                        stringMarshallingCustomType = (INamedTypeSymbol)namedArg.Value.Value;
                        break;
                }
            }

            if (attrData.ConstructorArguments.Length == 0)
            {
                return null;
            }

            return new GeneratedDllImportData(attrData.ConstructorArguments[0].Value!.ToString())
            {
                IsUserDefined = userDefinedValues,
                EntryPoint = entryPoint,
                SetLastError = setLastError,
                StringMarshalling = stringMarshalling,
                StringMarshallingCustomType = stringMarshallingCustomType,
            };
        }

        private static IncrementalStubGenerationContext CalculateStubInformation(IMethodSymbol symbol, StubEnvironment environment, CancellationToken ct)
        {
            INamedTypeSymbol? lcidConversionAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.LCIDConversionAttribute);
            INamedTypeSymbol? suppressGCTransitionAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.SuppressGCTransitionAttribute);
            INamedTypeSymbol? unmanagedCallConvAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.UnmanagedCallConvAttribute);
            INamedTypeSymbol? defaultDllImportSearchPathsAttrType = environment.Compilation.GetTypeByMetadataName(TypeNames.DefaultDllImportSearchPathsAttribute);
            // Get any attributes of interest on the method
            AttributeData? generatedDllImportAttr = null;
            AttributeData? lcidConversionAttr = null;
            AttributeData? suppressGCTransitionAttribute = null;
            AttributeData? unmanagedCallConvAttribute = null;
            AttributeData? defaultDllImportSearchPathsAttribute = null;
            foreach (AttributeData attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass is not null
                    && attr.AttributeClass.ToDisplayString() == TypeNames.GeneratedDllImportAttribute)
                {
                    generatedDllImportAttr = attr;
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
                else if (defaultDllImportSearchPathsAttrType is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, defaultDllImportSearchPathsAttrType))
                {
                    defaultDllImportSearchPathsAttribute = attr;
                }
            }

            Debug.Assert(generatedDllImportAttr is not null);

            var generatorDiagnostics = new GeneratorDiagnostics();

            // Process the GeneratedDllImport attribute
            GeneratedDllImportData? stubDllImportData = ProcessGeneratedDllImportAttribute(generatedDllImportAttr!);

            if (stubDllImportData is null)
            {
                generatorDiagnostics.ReportConfigurationNotSupported(generatedDllImportAttr!, "Invalid syntax");
                stubDllImportData = new GeneratedDllImportData("INVALID_CSHARP_SYNTAX");
            }

            if (stubDllImportData.IsUserDefined.HasFlag(DllImportMember.StringMarshalling))
            {
                // User specified StringMarshalling.Custom without specifying StringMarshallingCustomType
                if (stubDllImportData.StringMarshalling == StringMarshalling.Custom && stubDllImportData.StringMarshallingCustomType is null)
                {
                    generatorDiagnostics.ReportInvalidStringMarshallingConfiguration(
                        generatedDllImportAttr, symbol.Name, Resources.InvalidStringMarshallingConfigurationMissingCustomType);
                }

                // User specified something other than StringMarshalling.Custom while specifying StringMarshallingCustomType
                if (stubDllImportData.StringMarshalling != StringMarshalling.Custom && stubDllImportData.StringMarshallingCustomType is not null)
                {
                    generatorDiagnostics.ReportInvalidStringMarshallingConfiguration(
                        generatedDllImportAttr, symbol.Name, Resources.InvalidStringMarshallingConfigurationNotCustom);
                }
            }

            if (lcidConversionAttr is not null)
            {
                // Using LCIDConversion with GeneratedDllImport is not supported
                generatorDiagnostics.ReportConfigurationNotSupported(lcidConversionAttr, nameof(TypeNames.LCIDConversionAttribute));
            }

            // Create the stub.
            var dllImportStub = DllImportStubContext.Create(symbol, stubDllImportData, environment, generatorDiagnostics, ct);

            List<AttributeSyntax> additionalAttributes = GenerateSyntaxForForwardedAttributes(suppressGCTransitionAttribute, unmanagedCallConvAttribute, defaultDllImportSearchPathsAttribute);
            return new IncrementalStubGenerationContext(environment, dllImportStub, additionalAttributes.ToImmutableArray(), stubDllImportData, generatorDiagnostics.Diagnostics.ToImmutableArray());
        }

        private (MemberDeclarationSyntax, ImmutableArray<Diagnostic>) GenerateSource(
            IncrementalStubGenerationContext dllImportStub,
            MethodDeclarationSyntax originalSyntax,
            DllImportGeneratorOptions options)
        {
            var diagnostics = new GeneratorDiagnostics();
            if (options.GenerateForwarders)
            {
                return (PrintForwarderStub(originalSyntax, dllImportStub, diagnostics), dllImportStub.Diagnostics.AddRange(diagnostics.Diagnostics));
            }

            // Generate stub code
            var stubGenerator = new PInvokeStubCodeGenerator(
                dllImportStub.Environment,
                dllImportStub.StubContext.ElementTypeInformation,
                dllImportStub.DllImportData.SetLastError && !options.GenerateForwarders,
                (elementInfo, ex) =>
                {
                    diagnostics.ReportMarshallingNotSupported(originalSyntax, elementInfo, ex.NotSupportedDetails);
                },
                dllImportStub.StubContext.GeneratorFactory);

            // Check if the generator should produce a forwarder stub - regular DllImport.
            // This is done if the signature is blittable or the target framework is not supported.
            if (stubGenerator.StubIsBasicForwarder
                || !stubGenerator.SupportsTargetFramework)
            {
                return (PrintForwarderStub(originalSyntax, dllImportStub, diagnostics), dllImportStub.Diagnostics.AddRange(diagnostics.Diagnostics));
            }

            ImmutableArray<AttributeSyntax> forwardedAttributes = dllImportStub.ForwardedAttributes;

            const string innerPInvokeName = "__PInvoke__";

            BlockSyntax code = stubGenerator.GeneratePInvokeBody(innerPInvokeName);

            LocalFunctionStatementSyntax dllImport = CreateTargetFunctionAsLocalStatement(
                stubGenerator,
                dllImportStub.StubContext.Options,
                dllImportStub.DllImportData,
                innerPInvokeName,
                originalSyntax.Identifier.Text);

            if (!forwardedAttributes.IsEmpty)
            {
                dllImport = dllImport.AddAttributeLists(AttributeList(SeparatedList(forwardedAttributes)));
            }

            dllImport = dllImport.WithLeadingTrivia(
                Comment("//"),
                Comment("// Local P/Invoke"),
                Comment("//"));
            code = code.AddStatements(dllImport);

            return (PrintGeneratedSource(originalSyntax, dllImportStub.StubContext, code), dllImportStub.Diagnostics.AddRange(diagnostics.Diagnostics));
        }

        private MemberDeclarationSyntax PrintForwarderStub(MethodDeclarationSyntax userDeclaredMethod, IncrementalStubGenerationContext stub, GeneratorDiagnostics diagnostics)
        {
            GeneratedDllImportData targetDllImportData = GetTargetDllImportDataFromStubData(
                stub.DllImportData,
                userDeclaredMethod.Identifier.ValueText,
                forwardAll: true);

            if (targetDllImportData.IsUserDefined.HasFlag(DllImportMember.StringMarshalling)
                && targetDllImportData.StringMarshalling != StringMarshalling.Utf16)
            {
                diagnostics.ReportCannotForwardToDllImport(
                    userDeclaredMethod,
                    $"{nameof(TypeNames.GeneratedDllImportAttribute)}{Type.Delimiter}{nameof(StringMarshalling)}",
                    $"{nameof(StringMarshalling)}{Type.Delimiter}{targetDllImportData.StringMarshalling}");

                targetDllImportData = targetDllImportData with { IsUserDefined = targetDllImportData.IsUserDefined & ~DllImportMember.StringMarshalling };
            }

            if (targetDllImportData.IsUserDefined.HasFlag(DllImportMember.StringMarshallingCustomType))
            {
                diagnostics.ReportCannotForwardToDllImport(
                    userDeclaredMethod,
                    $"{nameof(TypeNames.GeneratedDllImportAttribute)}{Type.Delimiter}{nameof(DllImportMember.StringMarshallingCustomType)}");

                targetDllImportData = targetDllImportData with { IsUserDefined = targetDllImportData.IsUserDefined & ~DllImportMember.StringMarshallingCustomType };
            }

            SyntaxTokenList modifiers = StripTriviaFromModifiers(userDeclaredMethod.Modifiers);
            modifiers = AddToModifiers(modifiers, SyntaxKind.ExternKeyword);
            // Create stub function
            MethodDeclarationSyntax stubMethod = MethodDeclaration(stub.StubContext.StubReturnType, userDeclaredMethod.Identifier)
                .WithModifiers(modifiers)
                .WithParameterList(ParameterList(SeparatedList(stub.StubContext.StubParameters)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .AddModifiers()
                .AddAttributeLists(
                    AttributeList(
                        SingletonSeparatedList(
                            CreateDllImportAttributeForTarget(targetDllImportData))));

            MemberDeclarationSyntax toPrint = WrapMethodInContainingScopes(stub.StubContext, stubMethod);

            return toPrint;
        }

        private static LocalFunctionStatementSyntax CreateTargetFunctionAsLocalStatement(
            PInvokeStubCodeGenerator stubGenerator,
            DllImportGeneratorOptions options,
            GeneratedDllImportData dllImportData,
            string stubTargetName,
            string stubMethodName)
        {
            Debug.Assert(!options.GenerateForwarders, "GenerateForwarders should have already been handled to use a forwarder stub");

            (ParameterListSyntax parameterList, TypeSyntax returnType, AttributeListSyntax returnTypeAttributes) = stubGenerator.GenerateTargetMethodSignatureData();
            LocalFunctionStatementSyntax localDllImport = LocalFunctionStatement(returnType, stubTargetName)
                .AddModifiers(
                    Token(SyntaxKind.ExternKeyword),
                    Token(SyntaxKind.StaticKeyword),
                    Token(SyntaxKind.UnsafeKeyword))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .WithAttributeLists(
                    SingletonList(AttributeList(
                        SingletonSeparatedList(
                            CreateDllImportAttributeForTarget(
                                GetTargetDllImportDataFromStubData(
                                    dllImportData,
                                    stubMethodName,
                                    forwardAll: false))))))
                .WithParameterList(parameterList);
            if (returnTypeAttributes is not null)
            {
                localDllImport = localDllImport.AddAttributeLists(returnTypeAttributes.WithTarget(AttributeTargetSpecifier(Token(SyntaxKind.ReturnKeyword))));
            }
            return localDllImport;
        }

        private static AttributeSyntax CreateDllImportAttributeForTarget(GeneratedDllImportData targetDllImportData)
        {
            var newAttributeArgs = new List<AttributeArgumentSyntax>
            {
                AttributeArgument(LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    Literal(targetDllImportData.ModuleName))),
                AttributeArgument(
                    NameEquals(nameof(DllImportAttribute.EntryPoint)),
                    null,
                    CreateStringExpressionSyntax(targetDllImportData.EntryPoint!)),
                AttributeArgument(
                    NameEquals(nameof(DllImportAttribute.ExactSpelling)),
                    null,
                    CreateBoolExpressionSyntax(true))
            };

            if (targetDllImportData.IsUserDefined.HasFlag(DllImportMember.StringMarshalling))
            {
                Debug.Assert(targetDllImportData.StringMarshalling == StringMarshalling.Utf16);
                NameEqualsSyntax name = NameEquals(nameof(DllImportAttribute.CharSet));
                ExpressionSyntax value = CreateEnumExpressionSyntax(CharSet.Unicode);
                newAttributeArgs.Add(AttributeArgument(name, null, value));
            }
            if (targetDllImportData.IsUserDefined.HasFlag(DllImportMember.SetLastError))
            {
                NameEqualsSyntax name = NameEquals(nameof(DllImportAttribute.SetLastError));
                ExpressionSyntax value = CreateBoolExpressionSyntax(targetDllImportData.SetLastError);
                newAttributeArgs.Add(AttributeArgument(name, null, value));
            }

            // Create new attribute
            return Attribute(
                ParseName(typeof(DllImportAttribute).FullName),
                AttributeArgumentList(SeparatedList(newAttributeArgs)));

            static ExpressionSyntax CreateBoolExpressionSyntax(bool trueOrFalse)
            {
                return LiteralExpression(
                    trueOrFalse
                        ? SyntaxKind.TrueLiteralExpression
                        : SyntaxKind.FalseLiteralExpression);
            }

            static ExpressionSyntax CreateStringExpressionSyntax(string str)
            {
                return LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    Literal(str));
            }

            static ExpressionSyntax CreateEnumExpressionSyntax<T>(T value) where T : Enum
            {
                return MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(typeof(T).FullName),
                    IdentifierName(value.ToString()));
            }
        }

        private static GeneratedDllImportData GetTargetDllImportDataFromStubData(GeneratedDllImportData dllImportData, string originalMethodName, bool forwardAll)
        {
            DllImportMember membersToForward = DllImportMember.All
                               // https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.setlasterror
                               // If SetLastError=true (default is false), the P/Invoke stub gets/caches the last error after invoking the native function.
                               & ~DllImportMember.SetLastError
                               // StringMarshalling does not have a direct mapping on DllImport. The generated code should handle string marshalling.
                               & ~DllImportMember.StringMarshalling;
            if (forwardAll)
            {
                membersToForward = DllImportMember.All;
            }

            var targetDllImportData = new GeneratedDllImportData(dllImportData.ModuleName)
            {
                EntryPoint = dllImportData.EntryPoint,
                SetLastError = dllImportData.SetLastError,
                StringMarshalling = dllImportData.StringMarshalling,
                IsUserDefined = dllImportData.IsUserDefined & membersToForward
            };

            // If the EntryPoint property is not set, we will compute and
            // add it based on existing semantics (i.e. method name).
            //
            // N.B. The export discovery logic is identical regardless of where
            // the name is defined (i.e. method name vs EntryPoint property).
            if (!targetDllImportData.IsUserDefined.HasFlag(DllImportMember.EntryPoint))
            {
                targetDllImportData = targetDllImportData with { EntryPoint = originalMethodName };
            }

            return targetDllImportData;
        }

        private static bool ShouldVisitNode(SyntaxNode syntaxNode)
        {
            // We only support C# method declarations.
            if (syntaxNode.Language != LanguageNames.CSharp
                || !syntaxNode.IsKind(SyntaxKind.MethodDeclaration))
            {
                return false;
            }

            var methodSyntax = (MethodDeclarationSyntax)syntaxNode;

            // Verify the method has no generic types or defined implementation
            // and is marked static and partial.
            if (methodSyntax.TypeParameterList is not null
                || methodSyntax.Body is not null
                || !methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
                || !methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return false;
            }

            // Verify that the types the method is declared in are marked partial.
            for (SyntaxNode? parentNode = methodSyntax.Parent; parentNode is TypeDeclarationSyntax typeDecl; parentNode = parentNode.Parent)
            {
                if (!typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return false;
                }
            }

            // Filter out methods with no attributes early.
            if (methodSyntax.AttributeLists.Count == 0)
            {
                return false;
            }

            return true;
        }
    }
}
