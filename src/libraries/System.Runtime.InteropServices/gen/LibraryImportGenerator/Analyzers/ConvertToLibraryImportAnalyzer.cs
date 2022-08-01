// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConvertToLibraryImportAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Interoperability";

        public static readonly DiagnosticDescriptor ConvertToLibraryImport =
            new DiagnosticDescriptor(
                Ids.ConvertToLibraryImport,
                GetResourceString(nameof(SR.ConvertToLibraryImportTitle)),
                GetResourceString(nameof(SR.ConvertToLibraryImportMessage)),
                Category,
                DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConvertToLibraryImportDescription)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ConvertToLibraryImport);

        public const string CharSet = nameof(CharSet);
        public const string ExactSpelling = nameof(ExactSpelling);
        public const string MayRequireAdditionalWork = nameof(MayRequireAdditionalWork);

        private static readonly HashSet<string> s_unsupportedTypeNames = new()
        {
            "global::System.Runtime.InteropServices.CriticalHandle",
            "global::System.Runtime.InteropServices.HandleRef",
            "global::System.Text.StringBuilder"
        };

        public override void Initialize(AnalysisContext context)
        {
            // Don't analyze generated code
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(
                context =>
                {
                    // Nothing to do if the LibraryImportAttribute is not in the compilation
                    INamedTypeSymbol? libraryImportAttrType = context.Compilation.GetBestTypeByMetadataName(TypeNames.LibraryImportAttribute);
                    if (libraryImportAttrType == null)
                        return;

                    context.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, libraryImportAttrType), SymbolKind.Method);
                });
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol libraryImportAttrType)
        {
            var method = (IMethodSymbol)context.Symbol;

            // Check if method is a DllImport
            DllImportData? dllImportData = method.GetDllImportData();
            if (dllImportData == null)
                return;

            // Ignore methods already marked LibraryImport
            // This can be the case when the generator creates an extern partial function for blittable signatures.
            foreach (AttributeData attr in method.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, libraryImportAttrType))
                {
                    return;
                }
            }

            // Ignore methods with unsupported returns
            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
                return;

            // Use the DllImport attribute data and the method signature to do some of the work the generator will do after conversion.
            // If any diagnostics or failures to marshal are reported, then mark this diagnostic with a property signifying that it may require
            // later user work.
            AnyDiagnosticsSink diagnostics = new();
            StubEnvironment env = context.Compilation.CreateStubEnvironment();
            SignatureContext targetSignatureContext = SignatureContext.Create(method, CreateInteropAttributeDataFromDllImport(dllImportData), env, diagnostics, typeof(ConvertToLibraryImportAnalyzer).Assembly);

            var generatorFactoryKey = LibraryImportGeneratorHelpers.CreateGeneratorFactory(env, new LibraryImportGeneratorOptions(context.Options.AnalyzerConfigOptionsProvider.GlobalOptions));

            bool mayRequireAdditionalWork = diagnostics.AnyDiagnostics;
            bool anyExplicitlyUnsupportedInfo = false;

            var stubCodeContext = new ManagedToNativeStubCodeContext(env, "return", "nativeReturn");

            var forwarder = new Forwarder();
            // We don't actually need the bound generators. We just need them to be attempted to be bound to determine if the generator will be able to bind them.
            _ = new BoundGenerators(targetSignatureContext.ElementTypeInformation, info =>
            {
                if (s_unsupportedTypeNames.Contains(info.ManagedType.FullTypeName))
                {
                    anyExplicitlyUnsupportedInfo = true;
                    return forwarder;
                }
                if (HasUnsupportedMarshalAsInfo(info))
                {
                    anyExplicitlyUnsupportedInfo = true;
                    return forwarder;
                }
                try
                {
                    return generatorFactoryKey.GeneratorFactory.Create(info, stubCodeContext);
                }
                catch (MarshallingNotSupportedException)
                {
                    mayRequireAdditionalWork = true;
                    return forwarder;
                }
            });

            if (anyExplicitlyUnsupportedInfo)
            {
                // If we have any parameters/return value with an explicitly unsupported marshal type or marshalling info,
                // don't offer the fix. The amount of work for the user to get to pairity would be too expensive.
                return;
            }

            ImmutableDictionary<string, string>.Builder properties = ImmutableDictionary.CreateBuilder<string, string>();

            properties.Add(CharSet, dllImportData.CharacterSet.ToString());
            properties.Add(ExactSpelling, dllImportData.ExactSpelling.ToString());
            properties.Add(MayRequireAdditionalWork, mayRequireAdditionalWork.ToString());

            context.ReportDiagnostic(method.CreateDiagnostic(ConvertToLibraryImport, properties.ToImmutable(), method.Name));
        }

        private static bool HasUnsupportedMarshalAsInfo(TypePositionInfo info)
        {
            if (info.MarshallingAttributeInfo is not MarshalAsInfo(UnmanagedType unmanagedType, _))
                return false;

            return !System.Enum.IsDefined(typeof(UnmanagedType), unmanagedType)
                || unmanagedType == UnmanagedType.CustomMarshaler
                || unmanagedType == UnmanagedType.Interface
                || unmanagedType == UnmanagedType.IDispatch
                || unmanagedType == UnmanagedType.IInspectable
                || unmanagedType == UnmanagedType.IUnknown
                || unmanagedType == UnmanagedType.SafeArray;
        }

        private static InteropAttributeData CreateInteropAttributeDataFromDllImport(DllImportData dllImportData)
        {
            InteropAttributeData interopData = new();
            if (dllImportData.SetLastError)
            {
                interopData = interopData with { IsUserDefined = interopData.IsUserDefined | InteropAttributeMember.SetLastError, SetLastError = true };
            }
            if (dllImportData.CharacterSet != System.Runtime.InteropServices.CharSet.None)
            {
                // Treat all strings as UTF-16 for the purposes of determining if we can marshal the parameters of this signature. We'll handle a more accurate conversion in the fixer.
                interopData = interopData with { IsUserDefined = interopData.IsUserDefined | InteropAttributeMember.StringMarshalling, StringMarshalling = StringMarshalling.Utf16 };
            }
            return interopData;
        }

        private sealed class AnyDiagnosticsSink : IGeneratorDiagnostics
        {
            public bool AnyDiagnostics { get; private set; }
            public void ReportConfigurationNotSupported(AttributeData attributeData, string configurationName, string? unsupportedValue) => AnyDiagnostics = true;
            public void ReportInvalidMarshallingAttributeInfo(AttributeData attributeData, string reasonResourceName, params string[] reasonArgs) => AnyDiagnostics = true;
        }
    }
}
