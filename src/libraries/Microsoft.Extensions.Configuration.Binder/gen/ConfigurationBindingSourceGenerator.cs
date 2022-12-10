// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#define LAUNCH_DEBUGGER
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    /// <summary>
    /// Generates source code to optimize binding with ConfigurationBinder.
    /// </summary>
    [Generator]
    public sealed partial class ConfigurationBindingSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<InvocationExpressionSyntax> inputCalls = context.SyntaxProvider.CreateSyntaxProvider(
                (node, _) => Parser.IsInputCall(node),
                (syntaxContext, _) => (InvocationExpressionSyntax)syntaxContext.Node);

            IncrementalValueProvider<(Compilation, ImmutableArray<InvocationExpressionSyntax>)> compilationAndClasses =
                context.CompilationProvider.Combine(inputCalls.Collect());

            context.RegisterSourceOutput(compilationAndClasses, (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        /// <summary>
        /// Generates source code to optimize binding with ConfigurationBinder.
        /// </summary>
        private static void Execute(Compilation compilation, ImmutableArray<InvocationExpressionSyntax> inputCalls, SourceProductionContext context)
        {
#if LAUNCH_DEBUGGER
            #pragma warning disable IDE0055
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }
            try
            {
#endif
            if (inputCalls.IsDefaultOrEmpty)
            {
                return;
            }

            Parser parser = new(context, compilation);
            SourceGenerationSpec? spec = parser.GetSourceGenerationSpec(inputCalls, context.CancellationToken);
            if (spec is not null)
            {
                Emitter emitter = new(context, spec);
                emitter.Emit();
            }
#if LAUNCH_DEBUGGER
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debugger.Break();
                throw ex;
            }
            #pragma warning restore
#endif
        }
    }
}
