// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.Interop;

namespace Microsoft.Interop.Analyzers
{
    /// <summary>
    /// Analyzer that reports diagnostics for LibraryImport methods.
    /// This analyzer runs the same diagnostic logic as LibraryImportGenerator
    /// but reports diagnostics separately from the source generator.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LibraryImportDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(
                GeneratorDiagnostics.InvalidAttributedMethodSignature,
                GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingModifiers,
                GeneratorDiagnostics.InvalidStringMarshallingConfiguration,
                GeneratorDiagnostics.ParameterTypeNotSupported,
                GeneratorDiagnostics.ReturnTypeNotSupported,
                GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails,
                GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails,
                GeneratorDiagnostics.ParameterConfigurationNotSupported,
                GeneratorDiagnostics.ReturnConfigurationNotSupported,
                GeneratorDiagnostics.MarshalAsParameterConfigurationNotSupported,
                GeneratorDiagnostics.MarshalAsReturnConfigurationNotSupported,
                GeneratorDiagnostics.ConfigurationNotSupported,
                GeneratorDiagnostics.ConfigurationValueNotSupported,
                GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported,
                GeneratorDiagnostics.CannotForwardToDllImport,
                GeneratorDiagnostics.RequiresAllowUnsafeBlocks,
                GeneratorDiagnostics.UnnecessaryParameterMarshallingInfo,
                GeneratorDiagnostics.UnnecessaryReturnMarshallingInfo,
                GeneratorDiagnostics.SizeOfInCollectionMustBeDefinedAtCallOutParam,
                GeneratorDiagnostics.SizeOfInCollectionMustBeDefinedAtCallReturnValue,
                GeneratorDiagnostics.LibraryImportUsageDoesNotFollowBestPractices);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(context =>
            {
                // Nothing to do if the LibraryImportAttribute is not in the compilation
                if (context.Compilation.GetBestTypeByMetadataName(TypeNames.LibraryImportAttribute) is null)
                    return;

                StubEnvironment env = new StubEnvironment(
                    context.Compilation,
                    context.Compilation.GetEnvironmentFlags());

                context.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, env), SymbolKind.Method);
            });
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context, StubEnvironment env)
        {
            IMethodSymbol method = (IMethodSymbol)context.Symbol;

            // Only analyze methods with LibraryImportAttribute
            AttributeData? libraryImportAttr = null;
            foreach (AttributeData attr in method.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == TypeNames.LibraryImportAttribute)
                {
                    libraryImportAttr = attr;
                    break;
                }
            }

            if (libraryImportAttr is null)
                return;

            // Find the method syntax
            foreach (SyntaxReference syntaxRef in method.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax(context.CancellationToken) is MethodDeclarationSyntax methodSyntax)
                {
                    AnalyzeMethodSyntax(context, methodSyntax, method, libraryImportAttr, env);
                    break;
                }
            }
        }

        private static void AnalyzeMethodSyntax(
            SymbolAnalysisContext context,
            MethodDeclarationSyntax methodSyntax,
            IMethodSymbol method,
            AttributeData libraryImportAttr,
            StubEnvironment env)
        {
            // Check for invalid method signature
            DiagnosticInfo? invalidMethodDiagnostic = GetDiagnosticIfInvalidMethodForGeneration(methodSyntax, method);
            if (invalidMethodDiagnostic is not null)
            {
                context.ReportDiagnostic(invalidMethodDiagnostic.ToDiagnostic());
                return; // Don't continue analysis if the method is invalid
            }

            // Check for unsafe blocks requirement
            if (context.Compilation.Options is not CSharpCompilationOptions { AllowUnsafe: true })
            {
                context.ReportDiagnostic(DiagnosticInfo.Create(GeneratorDiagnostics.RequiresAllowUnsafeBlocks, null).ToDiagnostic());
            }

            // Get generator options
            LibraryImportGeneratorOptions options = new(context.Options.AnalyzerConfigOptionsProvider.GlobalOptions);

            // Calculate stub information and collect diagnostics
            var diagnostics = CalculateDiagnostics(methodSyntax, method, libraryImportAttr, env, options, context.CancellationToken);

            foreach (DiagnosticInfo diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
        }

        private static ImmutableArray<DiagnosticInfo> CalculateDiagnostics(
            MethodDeclarationSyntax originalSyntax,
            IMethodSymbol symbol,
            AttributeData libraryImportAttr,
            StubEnvironment environment,
            LibraryImportGeneratorOptions options,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var locations = new MethodSignatureDiagnosticLocations(originalSyntax);
            var generatorDiagnostics = new GeneratorDiagnosticsBag(
                new DiagnosticDescriptorProvider(),
                locations,
                SR.ResourceManager,
                typeof(FxResources.Microsoft.Interop.LibraryImportGenerator.SR));

            // Process the LibraryImport attribute
            LibraryImportCompilationData libraryImportData =
                ProcessLibraryImportAttribute(libraryImportAttr) ??
                new LibraryImportCompilationData("INVALID_CSHARP_SYNTAX");

            if (libraryImportData.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshalling))
            {
                // User specified StringMarshalling.Custom without specifying StringMarshallingCustomType
                if (libraryImportData.StringMarshalling == StringMarshalling.Custom && libraryImportData.StringMarshallingCustomType is null)
                {
                    generatorDiagnostics.ReportInvalidStringMarshallingConfiguration(
                        libraryImportAttr, symbol.Name, SR.InvalidStringMarshallingConfigurationMissingCustomType);
                }

                // User specified something other than StringMarshalling.Custom while specifying StringMarshallingCustomType
                if (libraryImportData.StringMarshalling != StringMarshalling.Custom && libraryImportData.StringMarshallingCustomType is not null)
                {
                    generatorDiagnostics.ReportInvalidStringMarshallingConfiguration(
                        libraryImportAttr, symbol.Name, SR.InvalidStringMarshallingConfigurationNotCustom);
                }
            }

            // Check for unsupported LCIDConversion attribute
            INamedTypeSymbol? lcidConversionAttrType = environment.LcidConversionAttrType;
            if (lcidConversionAttrType is not null)
            {
                foreach (AttributeData attr in symbol.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, lcidConversionAttrType))
                    {
                        generatorDiagnostics.ReportConfigurationNotSupported(attr, nameof(TypeNames.LCIDConversionAttribute));
                        break;
                    }
                }
            }

            // Create the signature context to collect marshalling-related diagnostics
            var signatureContext = SignatureContext.Create(
                symbol,
                DefaultMarshallingInfoParser.Create(environment, generatorDiagnostics, symbol, libraryImportData, libraryImportAttr),
                environment,
                new CodeEmitOptions(SkipInit: true),
                typeof(LibraryImportGenerator).Assembly);

            // If forwarders are not being generated, we need to run stub generation logic to get those diagnostics too
            if (!options.GenerateForwarders)
            {
                IMarshallingGeneratorResolver resolver = DefaultMarshallingGeneratorResolver.Create(
                    environment.EnvironmentFlags,
                    MarshalDirection.ManagedToUnmanaged,
                    TypeNames.LibraryImportAttribute_ShortName,
                    []);

                // Check marshalling generators - this collects diagnostics for marshalling issues
                _ = new ManagedToNativeStubGenerator(
                    signatureContext.ElementTypeInformation,
                    LibraryImportData.From(libraryImportData).SetLastError,
                    generatorDiagnostics,
                    resolver,
                    new CodeEmitOptions(SkipInit: true));
            }

            return generatorDiagnostics.Diagnostics.ToImmutableArray();
        }

        private static LibraryImportCompilationData? ProcessLibraryImportAttribute(AttributeData attrData)
        {
            // Found the LibraryImport, but it has an error so report the error.
            // This is most likely an issue with targeting an incorrect TFM.
            if (attrData.AttributeClass?.TypeKind is null or TypeKind.Error)
            {
                return null;
            }

            if (attrData.ConstructorArguments.Length == 0)
            {
                return null;
            }

            ImmutableDictionary<string, TypedConstant> namedArguments = ImmutableDictionary.CreateRange(attrData.NamedArguments);

            string? entryPoint = null;
            if (namedArguments.TryGetValue(nameof(LibraryImportCompilationData.EntryPoint), out TypedConstant entryPointValue))
            {
                if (entryPointValue.Value is not string)
                {
                    return null;
                }
                entryPoint = (string)entryPointValue.Value!;
            }

            return new LibraryImportCompilationData(attrData.ConstructorArguments[0].Value!.ToString())
            {
                EntryPoint = entryPoint,
            }.WithValuesFromNamedArguments(namedArguments);
        }

        private static DiagnosticInfo? GetDiagnosticIfInvalidMethodForGeneration(MethodDeclarationSyntax methodSyntax, IMethodSymbol method)
        {
            // Verify the method has no generic types or defined implementation
            // and is marked static and partial.
            if (methodSyntax.TypeParameterList is not null
                || methodSyntax.Body is not null
                || !methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
                || !methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.InvalidAttributedMethodSignature, methodSyntax.Identifier.GetLocation(), method.Name);
            }

            // Verify that the types the method is declared in are marked partial.
            if (methodSyntax.Parent is TypeDeclarationSyntax typeDecl && !typeDecl.IsInPartialContext(out var nonPartialIdentifier))
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingModifiers, methodSyntax.Identifier.GetLocation(), method.Name, nonPartialIdentifier);
            }

            // Verify the method does not have a ref return
            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.ReturnConfigurationNotSupported, methodSyntax.Identifier.GetLocation(), "ref return", method.ToDisplayString());
            }

            return null;
        }
    }
}
