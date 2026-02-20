// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Interop.JavaScript
{
    /// <summary>
    /// Analyzer that reports diagnostics for JSImport and JSExport methods.
    /// This analyzer runs the same diagnostic logic as JSImportGenerator and JSExportGenerator
    /// but reports diagnostics separately from the source generators.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class JSImportExportDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(
                GeneratorDiagnostics.InvalidImportAttributedMethodSignature,
                GeneratorDiagnostics.InvalidImportAttributedMethodContainingTypeMissingModifiers,
                GeneratorDiagnostics.InvalidExportAttributedMethodSignature,
                GeneratorDiagnostics.InvalidExportAttributedMethodContainingTypeMissingModifiers,
                GeneratorDiagnostics.ParameterTypeNotSupported,
                GeneratorDiagnostics.ReturnTypeNotSupported,
                GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails,
                GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails,
                GeneratorDiagnostics.ParameterConfigurationNotSupported,
                GeneratorDiagnostics.ReturnConfigurationNotSupported,
                GeneratorDiagnostics.ConfigurationNotSupported,
                GeneratorDiagnostics.ConfigurationValueNotSupported,
                GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported,
                GeneratorDiagnostics.JSImportRequiresAllowUnsafeBlocks,
                GeneratorDiagnostics.JSExportRequiresAllowUnsafeBlocks);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(context =>
            {
                INamedTypeSymbol? jsImportAttrType = context.Compilation.GetTypeByMetadataName(Constants.JSImportAttribute);
                INamedTypeSymbol? jsExportAttrType = context.Compilation.GetTypeByMetadataName(Constants.JSExportAttribute);

                if (jsImportAttrType is null && jsExportAttrType is null)
                    return;

                StubEnvironment env = new StubEnvironment(
                    context.Compilation,
                    context.Compilation.GetEnvironmentFlags());

                int foundImportMethod = 0;
                int foundExportMethod = 0;
                bool unsafeEnabled = context.Compilation.Options is CSharpCompilationOptions { AllowUnsafe: true };

                context.RegisterSymbolAction(symbolContext =>
                {
                    IMethodSymbol method = (IMethodSymbol)symbolContext.Symbol;

                    if (jsImportAttrType is not null)
                    {
                        foreach (AttributeData attr in method.GetAttributes())
                        {
                            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, jsImportAttrType))
                            {
                                if (AnalyzeImportMethod(symbolContext, method, attr, env))
                                {
                                    Interlocked.Exchange(ref foundImportMethod, 1);
                                }
                                break;
                            }
                        }
                    }

                    if (jsExportAttrType is not null)
                    {
                        foreach (AttributeData attr in method.GetAttributes())
                        {
                            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, jsExportAttrType))
                            {
                                if (AnalyzeExportMethod(symbolContext, method, env))
                                {
                                    Interlocked.Exchange(ref foundExportMethod, 1);
                                }
                                break;
                            }
                        }
                    }
                }, SymbolKind.Method);

                context.RegisterCompilationEndAction(endContext =>
                {
                    if (!unsafeEnabled)
                    {
                        if (Volatile.Read(ref foundImportMethod) != 0)
                        {
                            endContext.ReportDiagnostic(DiagnosticInfo.Create(GeneratorDiagnostics.JSImportRequiresAllowUnsafeBlocks, null).ToDiagnostic());
                        }
                        if (Volatile.Read(ref foundExportMethod) != 0)
                        {
                            endContext.ReportDiagnostic(DiagnosticInfo.Create(GeneratorDiagnostics.JSExportRequiresAllowUnsafeBlocks, null).ToDiagnostic());
                        }
                    }
                });
            });
        }

        private static bool AnalyzeImportMethod(SymbolAnalysisContext context, IMethodSymbol method, AttributeData jsImportAttr, StubEnvironment env)
        {
            foreach (SyntaxReference syntaxRef in method.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax(context.CancellationToken) is MethodDeclarationSyntax methodSyntax)
                {
                    DiagnosticInfo? invalidMethodDiagnostic = GetDiagnosticIfInvalidImportMethodForGeneration(methodSyntax, method);
                    if (invalidMethodDiagnostic is not null)
                    {
                        context.ReportDiagnostic(invalidMethodDiagnostic.ToDiagnostic());
                        return true;
                    }

                    foreach (DiagnosticInfo diagnostic in CalculateImportDiagnostics(methodSyntax, method, jsImportAttr, env, context.CancellationToken))
                    {
                        context.ReportDiagnostic(diagnostic.ToDiagnostic());
                    }
                    break;
                }
            }
            return true;
        }

        private static bool AnalyzeExportMethod(SymbolAnalysisContext context, IMethodSymbol method, StubEnvironment env)
        {
            foreach (SyntaxReference syntaxRef in method.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax(context.CancellationToken) is MethodDeclarationSyntax methodSyntax)
                {
                    DiagnosticInfo? invalidMethodDiagnostic = GetDiagnosticIfInvalidExportMethodForGeneration(methodSyntax, method);
                    if (invalidMethodDiagnostic is not null)
                    {
                        context.ReportDiagnostic(invalidMethodDiagnostic.ToDiagnostic());
                        return true;
                    }

                    foreach (DiagnosticInfo diagnostic in CalculateExportDiagnostics(methodSyntax, method, env, context.CancellationToken))
                    {
                        context.ReportDiagnostic(diagnostic.ToDiagnostic());
                    }
                    break;
                }
            }
            return true;
        }

        private static ImmutableArray<DiagnosticInfo> CalculateImportDiagnostics(
            MethodDeclarationSyntax originalSyntax,
            IMethodSymbol symbol,
            AttributeData jsImportAttr,
            StubEnvironment environment,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var locations = new MethodSignatureDiagnosticLocations(originalSyntax);
            var generatorDiagnostics = new GeneratorDiagnosticsBag(new DescriptorProvider(), locations, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.JavaScript.JSImportGenerator.SR));

            JSImportData? jsImportData = ProcessJSImportAttribute(jsImportAttr);
            if (jsImportData is null)
            {
                generatorDiagnostics.ReportConfigurationNotSupported(jsImportAttr, "Invalid syntax");
                return generatorDiagnostics.Diagnostics.ToImmutableArray();
            }

            var signatureContext = JSSignatureContext.Create(symbol, environment, generatorDiagnostics, ct);

            _ = new ManagedToNativeStubGenerator(
                signatureContext.SignatureContext.ElementTypeInformation,
                setLastError: false,
                generatorDiagnostics,
                new CompositeMarshallingGeneratorResolver(
                    new NoSpanAndTaskMixingResolver(),
                    new JSGeneratorResolver()),
                new CodeEmitOptions(SkipInit: true));

            return generatorDiagnostics.Diagnostics.ToImmutableArray();
        }

        private static ImmutableArray<DiagnosticInfo> CalculateExportDiagnostics(
            MethodDeclarationSyntax originalSyntax,
            IMethodSymbol symbol,
            StubEnvironment environment,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var locations = new MethodSignatureDiagnosticLocations(originalSyntax);
            var generatorDiagnostics = new GeneratorDiagnosticsBag(new DescriptorProvider(), locations, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.JavaScript.JSImportGenerator.SR));

            var signatureContext = JSSignatureContext.Create(symbol, environment, generatorDiagnostics, ct);

            _ = new UnmanagedToManagedStubGenerator(
                signatureContext.SignatureContext.ElementTypeInformation,
                generatorDiagnostics,
                new CompositeMarshallingGeneratorResolver(
                    new NoSpanAndTaskMixingResolver(),
                    new JSGeneratorResolver()));

            return generatorDiagnostics.Diagnostics.ToImmutableArray();
        }

        private static JSImportData? ProcessJSImportAttribute(AttributeData attrData)
        {
            if (attrData.AttributeClass?.TypeKind is null or TypeKind.Error)
                return null;

            if (attrData.ConstructorArguments.Length == 1)
                return new JSImportData(attrData.ConstructorArguments[0].Value!.ToString(), null);
            if (attrData.ConstructorArguments.Length == 2)
                return new JSImportData(attrData.ConstructorArguments[0].Value!.ToString(), attrData.ConstructorArguments[1].Value!.ToString());
            return null;
        }

        /// <summary>
        /// Checks if a JSImport method is invalid for generation and returns a diagnostic if so.
        /// </summary>
        /// <returns>A diagnostic if the method is invalid, null otherwise.</returns>
        internal static DiagnosticInfo? GetDiagnosticIfInvalidImportMethodForGeneration(MethodDeclarationSyntax methodSyntax, IMethodSymbol method)
        {
            // Verify the method has no generic types or defined implementation
            // and is marked static and partial.
            if (methodSyntax.TypeParameterList is not null
                || methodSyntax.Body is not null
                || !methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
                || !methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.InvalidImportAttributedMethodSignature, methodSyntax.Identifier.GetLocation(), method.Name);
            }

            // Verify that the types the method is declared in are marked partial.
            for (SyntaxNode? parentNode = methodSyntax.Parent; parentNode is TypeDeclarationSyntax typeDecl; parentNode = parentNode.Parent)
            {
                if (!typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return DiagnosticInfo.Create(GeneratorDiagnostics.InvalidImportAttributedMethodContainingTypeMissingModifiers, methodSyntax.Identifier.GetLocation(), method.Name, typeDecl.Identifier);
                }
            }

            // Verify the method does not have a ref return
            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.ReturnConfigurationNotSupported, methodSyntax.Identifier.GetLocation(), "ref return", method.ToDisplayString());
            }

            return null;
        }

        /// <summary>
        /// Checks if a JSExport method is invalid for generation and returns a diagnostic if so.
        /// </summary>
        /// <returns>A diagnostic if the method is invalid, null otherwise.</returns>
        internal static DiagnosticInfo? GetDiagnosticIfInvalidExportMethodForGeneration(MethodDeclarationSyntax methodSyntax, IMethodSymbol method)
        {
            // Verify the method has no generic types or defined implementation
            // and is marked static and partial.
            if (methodSyntax.TypeParameterList is not null
                || (methodSyntax.Body is null && methodSyntax.ExpressionBody is null)
                || !methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
                || methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.InvalidExportAttributedMethodSignature, methodSyntax.Identifier.GetLocation(), method.Name);
            }

            // Verify that the types the method is declared in are marked partial.
            for (SyntaxNode? parentNode = methodSyntax.Parent; parentNode is TypeDeclarationSyntax typeDecl; parentNode = parentNode.Parent)
            {
                if (!typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return DiagnosticInfo.Create(GeneratorDiagnostics.InvalidExportAttributedMethodContainingTypeMissingModifiers, methodSyntax.Identifier.GetLocation(), method.Name, typeDecl.Identifier);
                }
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
