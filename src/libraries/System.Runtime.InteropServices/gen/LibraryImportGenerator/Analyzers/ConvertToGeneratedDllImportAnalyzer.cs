// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConvertToGeneratedDllImportAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Interoperability";

        private static readonly string[] s_unsupportedTypeNames = new string[]
        {
            "System.Runtime.InteropServices.CriticalHandle",
            "System.Runtime.InteropServices.HandleRef",
            "System.Text.StringBuilder"
        };

        public static readonly DiagnosticDescriptor ConvertToGeneratedDllImport =
            new DiagnosticDescriptor(
                Ids.ConvertToGeneratedDllImport,
                GetResourceString(nameof(Resources.ConvertToGeneratedDllImportTitle)),
                GetResourceString(nameof(Resources.ConvertToGeneratedDllImportMessage)),
                Category,
                DiagnosticSeverity.Info,
                isEnabledByDefault: false,
                description: GetResourceString(nameof(Resources.ConvertToGeneratedDllImportDescription)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ConvertToGeneratedDllImport);

        public override void Initialize(AnalysisContext context)
        {
            // Don't analyze generated code
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(
                compilationContext =>
                {
                    // Nothing to do if the GeneratedDllImportAttribute is not in the compilation
                    INamedTypeSymbol? generatedDllImportAttrType = compilationContext.Compilation.GetTypeByMetadataName(TypeNames.GeneratedDllImportAttribute);
                    if (generatedDllImportAttrType == null)
                        return;

                    INamedTypeSymbol? marshalAsAttrType = compilationContext.Compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute);

                    var knownUnsupportedTypes = new List<ITypeSymbol>(s_unsupportedTypeNames.Length);
                    foreach (string typeName in s_unsupportedTypeNames)
                    {
                        INamedTypeSymbol? unsupportedType = compilationContext.Compilation.GetTypeByMetadataName(typeName);
                        if (unsupportedType != null)
                        {
                            knownUnsupportedTypes.Add(unsupportedType);
                        }
                    }

                    compilationContext.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, knownUnsupportedTypes, marshalAsAttrType), SymbolKind.Method);
                });
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context, List<ITypeSymbol> knownUnsupportedTypes, INamedTypeSymbol? marshalAsAttrType)
        {
            var method = (IMethodSymbol)context.Symbol;

            // Check if method is a DllImport
            DllImportData? dllImportData = method.GetDllImportData();
            if (dllImportData == null)
                return;

            // Ignore methods already marked GeneratedDllImport
            // This can be the case when the generator creates an extern partial function for blittable signatures.
            foreach (AttributeData attr in method.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == TypeNames.GeneratedDllImportAttribute)
                {
                    return;
                }
            }

            // Ignore methods with unsupported parameters
            foreach (IParameterSymbol parameter in method.Parameters)
            {
                if (knownUnsupportedTypes.Contains(parameter.Type)
                    || HasUnsupportedUnmanagedTypeValue(parameter.GetAttributes(), marshalAsAttrType))
                {
                    return;
                }
            }

            // Ignore methods with unsupported returns
            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
                return;

            if (knownUnsupportedTypes.Contains(method.ReturnType) || HasUnsupportedUnmanagedTypeValue(method.GetReturnTypeAttributes(), marshalAsAttrType))
                return;

            context.ReportDiagnostic(method.CreateDiagnostic(ConvertToGeneratedDllImport, method.Name));
        }

        private static bool HasUnsupportedUnmanagedTypeValue(ImmutableArray<AttributeData> attributes, INamedTypeSymbol? marshalAsAttrType)
        {
            if (marshalAsAttrType == null)
                return false;

            AttributeData? marshalAsAttr = null;
            foreach (AttributeData attr in attributes)
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, marshalAsAttrType))
                {
                    marshalAsAttr = attr;
                    break;
                }
            }

            if (marshalAsAttr == null || marshalAsAttr.ConstructorArguments.IsEmpty)
                return false;

            object unmanagedTypeObj = marshalAsAttr.ConstructorArguments[0].Value!;
            UnmanagedType unmanagedType = unmanagedTypeObj is short unmanagedTypeAsShort
                ? (UnmanagedType)unmanagedTypeAsShort
                : (UnmanagedType)unmanagedTypeObj;

            return !System.Enum.IsDefined(typeof(UnmanagedType), unmanagedType)
                || unmanagedType == UnmanagedType.CustomMarshaler
                || unmanagedType == UnmanagedType.Interface
                || unmanagedType == UnmanagedType.IDispatch
                || unmanagedType == UnmanagedType.IInspectable
                || unmanagedType == UnmanagedType.IUnknown
                || unmanagedType == UnmanagedType.SafeArray;
        }
    }
}
