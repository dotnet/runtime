// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#define LAUNCH_DEBUGGER
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    /// <summary>
    /// Generates source code to optimize binding with ConfigurationBinder.
    /// </summary>
    [Generator]
    public sealed partial class ConfigurationBindingGenerator : IIncrementalGenerator
    {
        internal const string ProjectName = "Microsoft.Extensions.Configuration.Binder.SourceGeneration";

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

            IncrementalValuesProvider<BinderInvocation> inputCalls = context.SyntaxProvider
                .CreateSyntaxProvider(
                    (node, _) => node is InvocationExpressionSyntax invocation,
                    BinderInvocation.Create)
                .Where(operation => operation is not null);

            IncrementalValueProvider<(CompilationData?, ImmutableArray<BinderInvocation>)> inputData = compilationData.Combine(inputCalls.Collect());

            context.RegisterSourceOutput(inputData, (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        /// <summary>
        /// Generates source code to optimize binding with ConfigurationBinder.
        /// </summary>
        private static void Execute(CompilationData compilationData, ImmutableArray<BinderInvocation> inputCalls, SourceProductionContext context)
        {
            if (inputCalls.IsDefaultOrEmpty)
            {
                return;
            }

            if (compilationData?.LanguageVersionIsSupported is not true)
            {
                context.ReportDiagnostic(Diagnostic.Create(Parser.Diagnostics.LanguageVersionNotSupported, location: null));
                return;
            }

            Parser parser = new(context, compilationData.TypeSymbols!, inputCalls);
            if (parser.GetSourceGenerationSpec() is SourceGenerationSpec { } spec)
            {
                Emitter emitter = new(context, spec);
                emitter.Emit();
            }
        }

        private sealed record CompilationData
        {
            public bool LanguageVersionIsSupported { get; }
            public KnownTypeSymbols? TypeSymbols { get; }

            public CompilationData(CSharpCompilation compilation)
            {
                LanguageVersionIsSupported = compilation.LanguageVersion >= LanguageVersion.CSharp11;
                if (LanguageVersionIsSupported)
                {
                    TypeSymbols = new KnownTypeSymbols(compilation);
                }
            }
        }
    }
}
