// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#define LAUNCH_DEBUGGER
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    /// <summary>
    /// Generates source code to optimize binding with ConfigurationBinder.
    /// </summary>
    [Generator]
    public sealed partial class ConfigurationBindingGenerator : IIncrementalGenerator
    {
        private static readonly string ProjectName = Emitter.s_assemblyName.Name!;

        public const string GenSpecTrackingName = nameof(SourceGenerationSpec);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
#if LAUNCH_DEBUGGER
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }
#endif
            IncrementalValueProvider<CompilationData?> compilationData =
                context.CompilationProvider
                    .Select((compilation, _) => compilation.Options is CSharpCompilationOptions options
                        ? new CompilationData((CSharpCompilation)compilation)
                        : null);

            IncrementalValueProvider<(SourceGenerationSpec?, ImmutableEquatableArray<DiagnosticInfo>?)> genSpec = context.SyntaxProvider
                .CreateSyntaxProvider(
                    (node, _) => BinderInvocation.IsCandidateSyntaxNode(node),
                    BinderInvocation.Create)
                .Where(invocation => invocation is not null)
                .Collect()
                .Combine(compilationData)
                .Select((tuple, cancellationToken) =>
                {
                    if (tuple.Right is not CompilationData compilationData)
                    {
                        return (null, null);
                    }

                    try
                    {
                        Parser parser = new(compilationData);
                        SourceGenerationSpec? spec = parser.GetSourceGenerationSpec(tuple.Left, cancellationToken);
                        ImmutableEquatableArray<DiagnosticInfo>? diagnostics = parser.Diagnostics?.ToImmutableEquatableArray();
                        return (spec, diagnostics);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                })
                .WithTrackingName(GenSpecTrackingName);

            context.RegisterSourceOutput(genSpec, ReportDiagnosticsAndEmitSource);

            if (!s_hasInitializedInterceptorVersion)
            {
                InterceptorVersion = DetermineInterceptableVersion();
                s_hasInitializedInterceptorVersion = true;
            }
        }

        internal static int s_interceptorVersion;
        internal static int InterceptorVersion
        {
            get
            {
                Debug.Assert(s_hasInitializedInterceptorVersion);
                return s_interceptorVersion;
            }
            set
            {
                s_interceptorVersion = value;
            }
        }

        // Used with v1 interceptor lightup approach:
        private static bool s_hasInitializedInterceptorVersion;
        internal static Func<SemanticModel, InvocationExpressionSyntax, CancellationToken, object>? GetInterceptableLocationFunc { get; private set; }
        internal static MethodInfo? InterceptableLocationVersionGetDisplayLocation { get; private set; }
        internal static MethodInfo? InterceptableLocationDataGetter { get; private set; }
        internal static MethodInfo? InterceptableLocationVersionGetter { get; private set; }

        internal static int DetermineInterceptableVersion()
        {
            MethodInfo? getInterceptableLocationMethod = null;
            int? interceptableVersion = null;

#if UPDATE_BASELINES
            const string envVarName = "InterceptableAttributeVersion";

#pragma warning disable RS1035 // Do not use APIs banned for analyzers
            string? interceptableVersionString = Environment.GetEnvironmentVariable(envVarName);
#pragma warning restore RS1035
            if (interceptableVersionString is not null)
            {
                if (int.TryParse(interceptableVersionString, out int version))
                {
                    interceptableVersion = version;
                }
            }

            if (interceptableVersion is null || interceptableVersion == 1)
#endif
            {
                getInterceptableLocationMethod = typeof(Microsoft.CodeAnalysis.CSharp.CSharpExtensions).GetMethod(
                    "GetInterceptableLocation",
                    BindingFlags.Static | BindingFlags.Public,
                    binder: null,
                    new Type[] { typeof(SemanticModel), typeof(InvocationExpressionSyntax), typeof(CancellationToken) },
                    modifiers: Array.Empty<ParameterModifier>());

                interceptableVersion = getInterceptableLocationMethod is null ? 0 : 1;
            }

            if (interceptableVersion == 1)
            {
                GetInterceptableLocationFunc = (Func<SemanticModel, InvocationExpressionSyntax, CancellationToken, object>)
                    getInterceptableLocationMethod.CreateDelegate(typeof(Func<SemanticModel, InvocationExpressionSyntax, CancellationToken, object>), target: null);

                Type? interceptableLocationType = typeof(Microsoft.CodeAnalysis.CSharp.CSharpExtensions).Assembly.GetType("Microsoft.CodeAnalysis.CSharp.InterceptableLocation");
                InterceptableLocationVersionGetDisplayLocation = interceptableLocationType.GetMethod("GetDisplayLocation", BindingFlags.Instance | BindingFlags.Public);
                InterceptableLocationVersionGetter = interceptableLocationType.GetProperty("Version", BindingFlags.Instance | BindingFlags.Public).GetGetMethod();
                InterceptableLocationDataGetter = interceptableLocationType.GetProperty("Data", BindingFlags.Instance | BindingFlags.Public).GetGetMethod();
            }

            return interceptableVersion.Value;
        }

        /// <summary>
        /// Instrumentation helper for unit tests.
        /// </summary>
        public Action<SourceGenerationSpec>? OnSourceEmitting { get; init; }

        private void ReportDiagnosticsAndEmitSource(SourceProductionContext sourceProductionContext, (SourceGenerationSpec? SourceGenerationSpec, ImmutableEquatableArray<DiagnosticInfo>? Diagnostics) input)
        {
            if (input.Diagnostics is ImmutableEquatableArray<DiagnosticInfo> diagnostics)
            {
                foreach (DiagnosticInfo diagnostic in diagnostics)
                {
                    sourceProductionContext.ReportDiagnostic(diagnostic.CreateDiagnostic());
                }
            }

            if (input.SourceGenerationSpec is SourceGenerationSpec spec)
            {
                OnSourceEmitting?.Invoke(spec);
                Emitter emitter = new(spec);
                emitter.Emit(sourceProductionContext);
            }
        }

        internal sealed class CompilationData
        {
            public bool LanguageVersionIsSupported { get; }
            public KnownTypeSymbols? TypeSymbols { get; }

            public CompilationData(CSharpCompilation compilation)
            {
                // We don't have a CSharp21 value available yet. Polyfill the value here for forward compat, rather than use the LanguageVersion.Preview enum value.
                // https://github.com/dotnet/roslyn/blob/168689931cb4e3150641ec2fb188a64ce4b3b790/src/Compilers/CSharp/Portable/LanguageVersion.cs#L218-L232
                const int LangVersion_CSharp12 = 1200;
                LanguageVersionIsSupported = (int)compilation.LanguageVersion >= LangVersion_CSharp12;

                if (LanguageVersionIsSupported)
                {
                    TypeSymbols = new KnownTypeSymbols(compilation);
                }
            }
        }
    }
}
