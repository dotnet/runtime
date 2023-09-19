// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#define LAUNCH_DEBUGGER
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    /// <summary>
    /// Generates source code to optimize binding with ConfigurationBinder.
    /// </summary>
    [Generator]
    public sealed partial class ConfigurationBindingGenerator : IIncrementalGenerator
    {
        private static readonly string ProjectName = Emitter.s_assemblyName.Name;

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

            IncrementalValuesProvider<BinderInvocation?> inputCalls = context.SyntaxProvider
                .CreateSyntaxProvider(
                    (node, _) => BinderInvocation.IsCandidateSyntaxNode(node),
                    BinderInvocation.Create)
                .Where(invocation => invocation is not null);

            IncrementalValueProvider<(CompilationData?, ImmutableArray<BinderInvocation>)> inputData = compilationData.Combine(inputCalls.Collect());

            context.RegisterSourceOutput(inputData, (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

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
            if (parser.GetSourceGenerationSpec() is SourceGenerationSpec spec)
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
                // We don't have a CSharp21 value available yet. Polyfill the value here for forward compat, rather than use the LangugeVersion.Preview enum value.
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
