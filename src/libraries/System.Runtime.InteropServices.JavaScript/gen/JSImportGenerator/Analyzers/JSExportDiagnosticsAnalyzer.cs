// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Interop.JavaScript
{
    /// <summary>
    /// Analyzer that reports diagnostics for <see cref="System.Runtime.InteropServices.JavaScript.JSExportAttribute"/> methods.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class JSExportDiagnosticsAnalyzer : JSInteropDiagnosticsAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(
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
                GeneratorDiagnostics.JSExportRequiresAllowUnsafeBlocks);

        protected override string AttributeMetadataName => Constants.JSExportAttribute;
        protected override DiagnosticDescriptor InvalidSignatureDescriptor => GeneratorDiagnostics.InvalidExportAttributedMethodSignature;
        protected override DiagnosticDescriptor ContainingTypeMissingModifiersDescriptor => GeneratorDiagnostics.InvalidExportAttributedMethodContainingTypeMissingModifiers;
        protected override DiagnosticDescriptor RequiresAllowUnsafeBlocksDescriptor => GeneratorDiagnostics.JSExportRequiresAllowUnsafeBlocks;
        protected override bool RequiresImplementation => true;

        protected override ImmutableArray<DiagnosticInfo> CalculateDiagnostics(
            MethodDeclarationSyntax originalSyntax,
            IMethodSymbol symbol,
            AttributeData attr,
            StubEnvironment environment,
            System.Threading.CancellationToken ct)
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
    }
}
