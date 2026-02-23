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
                INamedTypeSymbol? libraryImportAttrType = context.Compilation.GetBestTypeByMetadataName(TypeNames.LibraryImportAttribute);
                if (libraryImportAttrType is null)
                    return;

                StubEnvironment env = new StubEnvironment(
                    context.Compilation,
                    context.Compilation.GetEnvironmentFlags());

                // Get generator options once per compilation
                LibraryImportGeneratorOptions options = new(context.Options.AnalyzerConfigOptionsProvider.GlobalOptions);

                // Track if we found any LibraryImport methods to report RequiresAllowUnsafeBlocks once
                int foundLibraryImportMethod = 0;
                bool unsafeEnabled = context.Compilation.Options is CSharpCompilationOptions { AllowUnsafe: true };

                context.RegisterSymbolAction(symbolContext =>
                {
                    if (AnalyzeMethod(symbolContext, env, libraryImportAttrType, options))
                    {
                        Interlocked.Exchange(ref foundLibraryImportMethod, 1);
                    }
                }, SymbolKind.Method);

                // Report RequiresAllowUnsafeBlocks once per compilation if there are LibraryImport methods and unsafe is not enabled
                context.RegisterCompilationEndAction(endContext =>
                {
                    if (Volatile.Read(ref foundLibraryImportMethod) != 0 && !unsafeEnabled)
                    {
                        endContext.ReportDiagnostic(DiagnosticInfo.Create(GeneratorDiagnostics.RequiresAllowUnsafeBlocks, null).ToDiagnostic());
                    }
                });
            });
        }

        /// <summary>
        /// Analyzes a method for LibraryImport diagnostics.
        /// </summary>
        /// <returns>True if the method has LibraryImportAttribute, false otherwise.</returns>
        private static bool AnalyzeMethod(SymbolAnalysisContext context, StubEnvironment env, INamedTypeSymbol libraryImportAttrType, LibraryImportGeneratorOptions options)
        {
            IMethodSymbol method = (IMethodSymbol)context.Symbol;

            // Only analyze methods with LibraryImportAttribute
            AttributeData? libraryImportAttr = null;
            foreach (AttributeData attr in method.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, libraryImportAttrType))
                {
                    libraryImportAttr = attr;
                    break;
                }
            }

            if (libraryImportAttr is null)
                return false;

            // Find the method syntax
            foreach (SyntaxReference syntaxRef in method.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax(context.CancellationToken) is MethodDeclarationSyntax methodSyntax)
                {
                    AnalyzeMethodSyntax(context, methodSyntax, method, libraryImportAttr, env, options);
                    break;
                }
            }

            return true;
        }

        private static void AnalyzeMethodSyntax(
            SymbolAnalysisContext context,
            MethodDeclarationSyntax methodSyntax,
            IMethodSymbol method,
            AttributeData libraryImportAttr,
            StubEnvironment env,
            LibraryImportGeneratorOptions options)
        {
            // Check for invalid method signature
            DiagnosticInfo? invalidMethodDiagnostic = GetDiagnosticIfInvalidMethodForGeneration(methodSyntax, method);
            if (invalidMethodDiagnostic is not null)
            {
                context.ReportDiagnostic(invalidMethodDiagnostic.ToDiagnostic());
                return; // Don't continue analysis if the method is invalid
            }

            // Note: RequiresAllowUnsafeBlocks is reported once per compilation in Initialize method

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
            LibraryImportCompilationData? libraryImportData = ProcessLibraryImportAttribute(libraryImportAttr);

            // If we can't parse the attribute, we have an invalid compilation - stop processing
            if (libraryImportData is null)
            {
                return generatorDiagnostics.Diagnostics.ToImmutableArray();
            }

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

        /// <summary>
        /// Checks if a method is invalid for generation and returns a diagnostic if so.
        /// </summary>
        /// <returns>A diagnostic if the method is invalid, null otherwise.</returns>
        internal static DiagnosticInfo? GetDiagnosticIfInvalidMethodForGeneration(MethodDeclarationSyntax methodSyntax, IMethodSymbol method)
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
