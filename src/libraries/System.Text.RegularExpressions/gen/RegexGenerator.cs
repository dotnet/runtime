// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[assembly: System.Resources.NeutralResourcesLanguage("en-us")]

namespace System.Text.RegularExpressions.Generator
{
    /// <summary>Generates C# source code to implement regular expressions.</summary>
    [Generator(LanguageNames.CSharp)]
    public partial class RegexGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // To avoid invalidating the generator's output when anything from the compilation
            // changes, we will extract from it the only thing we care about: whether unsafe
            // code is allowed.
            IncrementalValueProvider<bool> allowUnsafeProvider =
                context.CompilationProvider
                .Select((x, _) => x.Options is CSharpCompilationOptions { AllowUnsafe: true });

            // Contains one entry per regex method, either the generated code for that regex method,
            // a diagnostic to fail with, or null if no action should be taken for that regex.
            IncrementalValueProvider<ImmutableArray<object?>> codeOrDiagnostics =
                context.SyntaxProvider

                // Find all MethodDeclarationSyntax nodes attributed with RegexGenerator and gather the required information
                .CreateSyntaxProvider(IsSyntaxTargetForGeneration, GetSemanticTargetForGeneration)
                .Where(static m => m is not null)

                // Pair each with whether unsafe code is allowed
                .Combine(allowUnsafeProvider)

                // Get the resulting code string or error Diagnostic for
                // each MethodDeclarationSyntax/allow-unsafe-blocks pair
                .Select((state, _) =>
                {
                    Debug.Assert(state.Left is not null);
                    return state.Left is RegexType regexType ? EmitRegexType(regexType, state.Right) : state.Left;
                })
                .Collect();

            // When there something to output, take all the generated strings and concatenate them to output,
            // and raise all of the created diagnostics.
            context.RegisterSourceOutput(codeOrDiagnostics, static (context, results) =>
            {
                var code = new List<string>(s_headers.Length + results.Length);

                // Add file header and required usings
                code.AddRange(s_headers);

                foreach (object? result in results)
                {
                    switch (result)
                    {
                        case Diagnostic d:
                            context.ReportDiagnostic(d);
                            break;

                        case ValueTuple<string, ImmutableArray<Diagnostic>> t:
                            code.Add(t.Item1);
                            foreach (Diagnostic d in t.Item2)
                            {
                                context.ReportDiagnostic(d);
                            }
                            break;
                    }
                }

                context.AddSource("RegexGenerator.g.cs", string.Join(Environment.NewLine, code));
            });
        }
    }
}
