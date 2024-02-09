// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            DiagnosticDescriptorHelper.Create(
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

                    TargetFrameworkSettings targetFramework = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.GetTargetFrameworkSettings();

                    StubEnvironment env = new StubEnvironment(
                        context.Compilation,
                        context.Compilation.GetEnvironmentFlags());

                    context.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, libraryImportAttrType, env, targetFramework), SymbolKind.Method);
                });
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol libraryImportAttrType, StubEnvironment env, TargetFrameworkSettings tf)
        {
            var method = (IMethodSymbol)context.Symbol;

            // Check if method is a DllImport
            DllImportData? dllImportData = method.GetDllImportData();
            if (dllImportData == null)
                return;

            if (dllImportData.ThrowOnUnmappableCharacter == true)
            {
                // LibraryImportGenerator doesn't support ThrowOnUnmappableCharacter = true
                return;
            }

            // LibraryImportGenerator doesn't support BestFitMapping = true
            if (IsBestFitMapping(method, dllImportData))
            {
                return;
            }

            if (method.IsVararg)
            {
                // LibraryImportGenerator doesn't support varargs
                return;
            }

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
            GeneratorDiagnosticsBag diagnostics = new(new DiagnosticDescriptorProvider(), new MethodSignatureDiagnosticLocations((MethodDeclarationSyntax)method.DeclaringSyntaxReferences[0].GetSyntax()), SR.ResourceManager, typeof(FxResources.Microsoft.Interop.LibraryImportGenerator.SR));
            AttributeData dllImportAttribute = method.GetAttributes().First(attr => attr.AttributeClass.ToDisplayString() == TypeNames.DllImportAttribute);
            SignatureContext targetSignatureContext = SignatureContext.Create(
                method,
                LibraryImportGeneratorHelpers.CreateMarshallingInfoParser(env, tf, diagnostics, method, CreateInteropAttributeDataFromDllImport(dllImportData), dllImportAttribute),
                env,
                new CodeEmitOptions(SkipInit: tf.TargetFramework == TargetFramework.Net),
                typeof(ConvertToLibraryImportAnalyzer).Assembly);

            var factory = LibraryImportGeneratorHelpers.CreateGeneratorResolver(tf, new LibraryImportGeneratorOptions(context.Options.AnalyzerConfigOptionsProvider.GlobalOptions), env.EnvironmentFlags);

            bool mayRequireAdditionalWork = diagnostics.Diagnostics.Any();
            bool anyExplicitlyUnsupportedInfo = false;

            var stubCodeContext = new ManagedToNativeStubCodeContext("return", "nativeReturn");

            var forwarder = new Forwarder();
            // We don't actually need the bound generators. We just need them to be attempted to be bound to determine if the generator will be able to bind them.
            BoundGenerators generators = BoundGenerators.Create(targetSignatureContext.ElementTypeInformation, new CallbackGeneratorResolver((info, context) =>
            {
                if (s_unsupportedTypeNames.Contains(info.ManagedType.FullTypeName))
                {
                    anyExplicitlyUnsupportedInfo = true;
                    return ResolvedGenerator.Resolved(forwarder);
                }
                if (HasUnsupportedMarshalAsInfo(info))
                {
                    anyExplicitlyUnsupportedInfo = true;
                    return ResolvedGenerator.Resolved(forwarder);
                }
                return factory.Create(info, stubCodeContext);
            }), stubCodeContext, forwarder, out var bindingFailures);

            mayRequireAdditionalWork |= bindingFailures.Any(d => d.IsFatal);

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

            context.ReportDiagnostic(method.CreateDiagnosticInfo(ConvertToLibraryImport, properties.ToImmutable(), method.Name).ToDiagnostic());
        }

        private static bool IsBestFitMapping(IMethodSymbol method, DllImportData? dllImportData)
        {
            if (dllImportData.BestFitMapping.HasValue)
            {
                return dllImportData.BestFitMapping.Value;
            }

            AttributeData? bestFitMappingContainingType = method.ContainingType.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.ToDisplayString() == TypeNames.System_Runtime_InteropServices_BestFitMappingAttribute);
            if (bestFitMappingContainingType is not null)
            {
                return bestFitMappingContainingType.ConstructorArguments[0].Value is true;
            }

            AttributeData? bestFitMappingContainingAssembly = method.ContainingAssembly.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.ToDisplayString() == TypeNames.System_Runtime_InteropServices_BestFitMappingAttribute);
            if (bestFitMappingContainingAssembly is not null)
            {
                return bestFitMappingContainingAssembly.ConstructorArguments[0].Value is true;
            }

            return false;
        }

        private static bool HasUnsupportedMarshalAsInfo(TypePositionInfo info)
        {
            if (info.MarshallingAttributeInfo is not MarshalAsInfo(UnmanagedType unmanagedType, _))
                return false;

            return !Enum.IsDefined(typeof(UnmanagedType), unmanagedType)
                || unmanagedType == UnmanagedType.CustomMarshaler
                || unmanagedType == UnmanagedType.Interface
                || unmanagedType == UnmanagedType.IDispatch
                || unmanagedType == UnmanagedType.IInspectable
                || unmanagedType == UnmanagedType.IUnknown
                || unmanagedType == UnmanagedType.SafeArray;
        }

        private static InteropAttributeCompilationData CreateInteropAttributeDataFromDllImport(DllImportData dllImportData)
        {
            InteropAttributeCompilationData interopData = new();
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

        private sealed class CallbackGeneratorResolver : IMarshallingGeneratorResolver
        {
            private readonly Func<TypePositionInfo, StubCodeContext, ResolvedGenerator> _func;

            public CallbackGeneratorResolver(Func<TypePositionInfo, StubCodeContext, ResolvedGenerator> func)
            {
                _func = func;
            }

            public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context) => _func(info, context);
        }
    }
}
