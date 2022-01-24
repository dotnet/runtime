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
            // Add RegexGeneratorAttribute to the build.
            context.RegisterPostInitializationOutput(context => context.AddSource("RegexGeneratorAttribute.cs", RegexGeneratorAttributeSource));

            // Contains one entry per regex method, either the generated code for that regex method,
            // a diagnostic to fail with, or null if no action should be taken for that regex.
            IncrementalValueProvider<ImmutableArray<object?>> codeOrDiagnostics =
                context.SyntaxProvider

                // Find all MethodDeclarationSyntax nodes attributed with RegexGenerator
                .CreateSyntaxProvider(static (s, _) => IsSyntaxTargetForGeneration(s), static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null)

                // Pair each with the compilation
                .Combine(context.CompilationProvider)

                // Use a custom comparer that ignores the compilation. We want to avoid regenerating for regex methods
                // that haven't been changed, but any change to a regex method will change the Compilation, so we ignore
                // the Compilation for purposes of caching.
                .WithComparer(new LambdaComparer<(MethodDeclarationSyntax?, Compilation)>(
                    static (left, right) => EqualityComparer<MethodDeclarationSyntax>.Default.Equals(left.Item1, right.Item1),
                    static o => o.Item1?.GetHashCode() ?? 0))

                // Get the resulting code string or error Diagnostic for each MethodDeclarationSyntax/Compilation pair
                .Select((state, cancellationToken) =>
                {
                    Debug.Assert(state.Item1 is not null);
                    object? result = GetRegexTypeToEmit(state.Item2, state.Item1, cancellationToken);
                    return result is RegexType regexType ? EmitRegexType(regexType, state.Item2) : result;
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

        private sealed class LambdaComparer<T> : IEqualityComparer<T>
        {
            private readonly Func<T?, T?, bool> _equal;
            private readonly Func<T?, int> _getHashCode;

            public LambdaComparer(Func<T?, T?, bool> equal, Func<T?, int> getHashCode)
            {
                _equal = equal;
                _getHashCode = getHashCode;
            }

            public bool Equals(T? x, T? y) => _equal(x, y);

            public int GetHashCode(T obj) => _getHashCode(obj);
        }

        private const string RegexGeneratorAttributeSource =
@"#nullable enable
using global::System.Diagnostics;
using global::System.Diagnostics.CodeAnalysis;
using global::System.Threading;
 
namespace System.Text.RegularExpressions
{ 
    /// <summary>Instructs the System.Text.RegularExpressions source generator to generate an implementation of the specified regular expression.</summary>
    /// <remarks>The generator associated with this attribute only supports C#.  It only supplies an implementation when applied to static, partial, parameterless, non-generic methods that are typed to return <see cref=""Regex""/>.</remarks>
    [Conditional(""INCLUDE_SYSTEM_TEXT_REGULAREXPRESSIONS_REGEXGENERATORATTRIBUTE"")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class RegexGeneratorAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref=""RegexGeneratorAttribute"" /> with the specified pattern.</summary>
        /// <param name=""pattern"" > The regular expression pattern to match.</param>
        public RegexGeneratorAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string pattern) : this (pattern, RegexOptions.None) { }
 
        /// <summary>Initializes a new instance of the <see cref=""RegexGeneratorAttribute""/> with the specified pattern and options.</summary>
        /// <param name=""pattern"">The regular expression pattern to match.</param>
        /// <param name=""options"">A bitwise combination of the enumeration values that modify the regular expression.</param>
        public RegexGeneratorAttribute([StringSyntax(StringSyntaxAttribute.Regex, ""options"")] string pattern, RegexOptions options) : this (pattern, options, Timeout.Infinite) { }
 
        /// <summary>Initializes a new instance of the <see cref=""RegexGeneratorAttribute"" /> with the specified pattern, options, and timeout.</summary>
        /// <param name=""pattern""> The regular expression pattern to match.</param>
        /// <param name=""options"">A bitwise combination of the enumeration values that modify the regular expression.</param>
        /// <param name=""matchTimeoutMilliseconds""> A time-out interval (milliseconds), or <see cref=""Timeout.Infinite""/> to indicate that the method should not time out.</param>
        public RegexGeneratorAttribute([StringSyntax(StringSyntaxAttribute.Regex, ""options"")] string pattern, RegexOptions options, int matchTimeoutMilliseconds)
        {
            Pattern = pattern;
            Options = options;
            MatchTimeoutMilliseconds = matchTimeoutMilliseconds;
        }
 
        /// <summary>Gets the regular expression pattern to match.</summary>
        public string Pattern { get; }
 
        /// <summary>Gets a bitwise combination of the enumeration values that modify the regular expression.</summary>
        public RegexOptions Options { get; }
 
        /// <summary>Gets a time-out interval (milliseconds), or <see cref=""Timeout.Infinite"" /> to indicate that the method should not time out.</summary>
        public int MatchTimeoutMilliseconds { get; }
    }
}";
    }
}
