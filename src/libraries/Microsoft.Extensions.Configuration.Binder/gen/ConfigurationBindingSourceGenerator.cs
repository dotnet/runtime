// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#define LAUNCH_DEBUGGER
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    /// <summary>
    /// Generates source code to optimize binding with ConfigurationBinder.
    /// </summary>
    [Generator]
    public sealed partial class ConfigurationBindingSourceGenerator : IIncrementalGenerator
    {
        private const string GeneratorProjectName = "Microsoft.Extensions.Configuration.Binder.SourceGeneration";

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

            IncrementalValuesProvider<BinderInvocationOperation> inputCalls = context.SyntaxProvider
                .CreateSyntaxProvider(
                    (node, _) => node is InvocationExpressionSyntax invocation,
                    BinderInvocationOperation.Create)
                .Where(operation => operation is not null);

            IncrementalValueProvider<(CompilationData?, ImmutableArray<BinderInvocationOperation>)> inputData = compilationData.Combine(inputCalls.Collect());

            context.RegisterSourceOutput(inputData, (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        /// <summary>
        /// Generates source code to optimize binding with ConfigurationBinder.
        /// </summary>
        private static void Execute(CompilationData compilationData, ImmutableArray<BinderInvocationOperation> inputCalls, SourceProductionContext context)
        {
            if (inputCalls.IsDefaultOrEmpty)
            {
                return;
            }

            if (compilationData?.LanguageVersionIsSupported != true)
            {
                context.ReportDiagnostic(Diagnostic.Create(Helpers.LanguageVersionNotSupported, location: null));
                return;
            }

            Parser parser = new(context, compilationData.TypeSymbols!);
            SourceGenerationSpec? spec = parser.GetSourceGenerationSpec(inputCalls);
            if (spec is not null)
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

        private enum BinderMethodKind
        {
            None = 0,
            Configure = 1,
            Get = 2,
            Bind = 3,
            GetValue = 4,
        }

        private sealed record BinderInvocationOperation()
        {
            public IInvocationOperation InvocationOperation { get; private set; }
            public BinderMethodKind Kind { get; private set; }
            public Location? Location { get; private set; }

            public static BinderInvocationOperation? Create(GeneratorSyntaxContext context, CancellationToken cancellationToken)
            {
                BinderMethodKind kind;
                if (context.Node is not InvocationExpressionSyntax invocationSyntax ||
                    invocationSyntax.Expression is not MemberAccessExpressionSyntax memberAccessSyntax ||
                    (kind = GetBindingMethodKind(memberAccessSyntax.Name.Identifier.ValueText)) is BinderMethodKind.None ||
                    context.SemanticModel.GetOperation(invocationSyntax, cancellationToken) is not IInvocationOperation operation)
                {
                    return null;
                }

                return new BinderInvocationOperation
                {
                    InvocationOperation = operation,
                    Kind = kind,
                    Location = invocationSyntax.GetLocation()
                };
            }

            private static BinderMethodKind GetBindingMethodKind(string name) =>
                name switch
                {
                    "Bind" => BinderMethodKind.Bind,
                    "Get" => BinderMethodKind.Get,
                    "GetValue" => BinderMethodKind.GetValue,
                    "Configure" => BinderMethodKind.Configure,
                    _ => default,

                };
        }
    }
}
