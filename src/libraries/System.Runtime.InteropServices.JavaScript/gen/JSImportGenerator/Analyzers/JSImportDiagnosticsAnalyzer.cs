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
    /// Analyzer that reports diagnostics for <see cref="System.Runtime.InteropServices.JavaScript.JSImportAttribute"/> methods.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class JSImportDiagnosticsAnalyzer : JSInteropDiagnosticsAnalyzer
    {
        protected override string AttributeMetadataName => Constants.JSImportAttribute;
        protected override DiagnosticDescriptor InvalidSignatureDescriptor => GeneratorDiagnostics.InvalidImportAttributedMethodSignature;
        protected override DiagnosticDescriptor ContainingTypeMissingModifiersDescriptor => GeneratorDiagnostics.InvalidImportAttributedMethodContainingTypeMissingModifiers;
        protected override DiagnosticDescriptor RequiresAllowUnsafeBlocksDescriptor => GeneratorDiagnostics.JSImportRequiresAllowUnsafeBlocks;
        protected override bool RequiresImplementation => false;

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

            JSImportData? jsImportData = ProcessJSImportAttribute(attr);
            if (jsImportData is null)
            {
                generatorDiagnostics.ReportConfigurationNotSupported(attr, "Invalid syntax");
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
    }
}
