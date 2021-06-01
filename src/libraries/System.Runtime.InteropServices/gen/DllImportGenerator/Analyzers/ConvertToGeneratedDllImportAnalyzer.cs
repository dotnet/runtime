using System.Collections.Immutable;
using System.Diagnostics;
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

        public readonly static DiagnosticDescriptor ConvertToGeneratedDllImport =
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

                    INamedTypeSymbol? dllImportAttrType = compilationContext.Compilation.GetTypeByMetadataName(typeof(DllImportAttribute).FullName);
                    if (dllImportAttrType == null)
                        return;

                    compilationContext.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, dllImportAttrType), SymbolKind.Method);
                });
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol dllImportAttrType)
        {
            var method = (IMethodSymbol)context.Symbol;

            // Check if method is a DllImport
            DllImportData? dllImportData = method.GetDllImportData();
            if (dllImportData == null)
                return;

            // Ignore QCalls
            if (dllImportData.ModuleName == "QCall")
                return;

            if (RequiresMarshalling(method, dllImportData, dllImportAttrType))
            {
                context.ReportDiagnostic(method.CreateDiagnostic(ConvertToGeneratedDllImport, method.Name));
            }
        }

        private static bool RequiresMarshalling(IMethodSymbol method, DllImportData dllImportData, INamedTypeSymbol dllImportAttrType)
        {
            // SetLastError=true requires marshalling
            if (dllImportData.SetLastError)
                return true;

            // Check if return value requires marshalling
            if (!method.ReturnsVoid && !method.ReturnType.IsConsideredBlittable())
                return true;

            // Check if parameters require marshalling
            foreach (IParameterSymbol paramType in method.Parameters)
            {
                if (paramType.RefKind != RefKind.None)
                    return true;

                if (!paramType.Type.IsConsideredBlittable())
                    return true;
            }

            // DllImportData does not expose all information (e.g. PreserveSig), so we still need to get the attribute data
            AttributeData? dllImportAttr = null;
            foreach (AttributeData attr in method.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, dllImportAttrType))
                    continue;

                dllImportAttr = attr;
                break;
            }

            Debug.Assert(dllImportAttr != null);
            foreach (var namedArg in dllImportAttr!.NamedArguments)
            {
                if (namedArg.Key != nameof(DllImportAttribute.PreserveSig))
                    continue;

                // PreserveSig=false requires marshalling
                if (!(bool)namedArg.Value.Value!)
                    return true;
            }

            return false;
        }
    }
}
