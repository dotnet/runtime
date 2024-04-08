// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#define LAUNCH_DEBUGGER
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
