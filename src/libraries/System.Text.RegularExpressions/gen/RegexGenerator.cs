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
            IncrementalValueProvider<ImmutableArray<object?>> typesAndCompilation =
                context.SyntaxProvider

                // Find all MethodDeclarationSyntax nodes attributed with RegexGenerator
                .CreateSyntaxProvider(static (s, _) => IsSyntaxTargetForGeneration(s), static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null)

                // Pair each with the compilation
                .Combine(context.CompilationProvider)

                // Use a custom comparer that ignores the compilation so that it doesn't interface with the generators caching of results based on MethodDeclarationSyntax
                .WithComparer(new LambdaComparer<(MethodDeclarationSyntax, Compilation)>(static (left, right) => left.Item1.Equals(left.Item2), static o => o.Item1.GetHashCode()))

                // Get the resulting code string or error Diagnostic for each MethodDeclarationSyntax/Compilation pair
                .Select((state, cancellationToken) =>
                {
                    object? result = GetRegexTypeToEmit(state.Item2, state.Item1, cancellationToken);
                    return result is RegexType regexType ? EmitRegexType(regexType) : result;
                })
                .Collect();

            // When there something to output, take all the generated strings and concatenate them to output,
            // and raise all of the created diagnostics.
            context.RegisterSourceOutput(typesAndCompilation, static (context, results) =>
            {
                var code = new List<string>(s_headersAndUsings.Length + results.Length);

                // Add file header and required usings
                code.AddRange(s_headersAndUsings);

                foreach (object? result in results)
                {
                    switch (result)
                    {
                        case Diagnostic d:
                            context.ReportDiagnostic(d);
                            break;

                        case string s:
                            code.Add(s);
                            break;
                    }
                }

                context.AddSource("RegexGenerator.g.cs", string.Join(Environment.NewLine, code));
            });
        }

        private sealed class LambdaComparer<T> : IEqualityComparer<T>
        {
            private readonly Func<T, T, bool> _equal;
            private readonly Func<T, int> _getHashCode;

            public LambdaComparer(Func<T, T, bool> equal, Func<T, int> getHashCode)
            {
                _equal = equal;
                _getHashCode = getHashCode;
            }

            public bool Equals(T x, T y) => _equal(x, y);

            public int GetHashCode(T obj) => _getHashCode(obj);
        }
    }
}
