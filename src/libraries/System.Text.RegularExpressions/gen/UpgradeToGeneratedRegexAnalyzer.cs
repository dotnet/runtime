// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace System.Text.RegularExpressions.Generator
{
    /// <summary>
    /// Roslyn analyzer that searches for invocations of the Regex constructors, or the
    /// Regex static methods and analyzes if the callsite could be using the Regex Generator instead.
    /// If so, it will emit an informational diagnostic to suggest use the Regex Generator.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UpgradeToGeneratedRegexAnalyzer : DiagnosticAnalyzer
    {
        private const string RegexTypeName = "System.Text.RegularExpressions.Regex";
        private const string GeneratedRegexTypeName = "System.Text.RegularExpressions.GeneratedRegexAttribute";

        internal const string PatternArgumentName = "pattern";
        internal const string OptionsArgumentName = "options";

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.UseRegexSourceGeneration);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(context =>
            {
                Compilation compilation = context.Compilation;

                // Validate that the project supports the Regex Source Generator based on target framework,
                // language version, etc.
                if (!ProjectSupportsRegexSourceGenerator(compilation, out INamedTypeSymbol? regexTypeSymbol))
                {
                    return;
                }

                // Pre-compute a hash with all of the method symbols that we want to analyze for possibly emitting
                // a diagnostic.
                HashSet<IMethodSymbol> staticMethodsToDetect = GetMethodSymbolHash(regexTypeSymbol,
                    new HashSet<string> { "Count", "EnumerateMatches", "IsMatch", "Match", "Matches", "Split", "Replace" });

                // Register analysis of calls to the Regex constructors
                context.RegisterOperationAction(context => AnalyzeObjectCreation(context, regexTypeSymbol), OperationKind.ObjectCreation);

                // Register analysis of calls to Regex static methods
                context.RegisterOperationAction(context => AnalyzeInvocation(context, regexTypeSymbol, staticMethodsToDetect), OperationKind.Invocation);
            });

            // Creates a HashSet of all of the method Symbols containing the static methods to analyze.
            static HashSet<IMethodSymbol> GetMethodSymbolHash(INamedTypeSymbol regexTypeSymbol, HashSet<string> methodNames)
            {
                // This warning is due to a false positive bug https://github.com/dotnet/roslyn-analyzers/issues/5804
                // This issue has now been fixed, but we are not yet consuming the fix and getting this package
                // as a transitive dependency from Microsoft.CodeAnalysis.CSharp.Workspaces. Once that dependency
                // is updated at the repo-level, we should come and remove the pragma disable.
                HashSet<IMethodSymbol> hash = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
                ImmutableArray<ISymbol> allMembers = regexTypeSymbol.GetMembers();

                foreach (ISymbol member in allMembers)
                {
                    if (member is IMethodSymbol method &&
                        method.IsStatic &&
                        methodNames.Contains(method.Name))
                    {
                        hash.Add(method);
                    }
                }

                return hash;
            }
        }

        /// <summary>
        /// Analyzes an invocation expression to see if the invocation is a call to one of the Regex static methods,
        /// and checks if they could be using the source generator instead.
        /// </summary>
        /// <param name="context">The compilation context representing the invocation.</param>
        private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol regexTypeSymbol, HashSet<IMethodSymbol> staticMethodsToDetect)
        {
            // Ensure the invocation is a Regex static method.
            IInvocationOperation invocationOperation = (IInvocationOperation)context.Operation;
            IMethodSymbol method = invocationOperation.TargetMethod;
            if (!method.IsStatic || !SymbolEqualityComparer.Default.Equals(method.ContainingType, regexTypeSymbol))
            {
                return;
            }

            // We need to save the parameters as properties so that we can save them onto the diagnostic so that the
            // code fixer can later use that property bag to generate the code fix and emit the GeneratedRegex attribute.
            if (staticMethodsToDetect.Contains(method))
            {
                // Validate that arguments pattern and options are constant and timeout was not passed in.
                if (!ValidateParameters(invocationOperation.Arguments))
                {
                    return;
                }

                // Report the diagnostic with a location that doesn't span the potentially multi-line pattern.
                SyntaxNode? syntaxNodeForDiagnostic = invocationOperation.Syntax;
                Debug.Assert(syntaxNodeForDiagnostic is not null);
                Location location = GetLocationBeforeArguments(syntaxNodeForDiagnostic);
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UseRegexSourceGeneration, location));
            }
        }

        /// <summary>
        /// Analyzes an object creation expression to see if the invocation is a call to one of the Regex constructors,
        /// and checks if they could be using the source generator instead.
        /// </summary>
        /// <param name="context">The object creation context.</param>
        private static void AnalyzeObjectCreation(OperationAnalysisContext context, INamedTypeSymbol regexTypeSymbol)
        {
            // Ensure the object creation is a call to the Regex constructor.
            IObjectCreationOperation operation = (IObjectCreationOperation)context.Operation;
            if (!SymbolEqualityComparer.Default.Equals(operation.Type, regexTypeSymbol))
            {
                return;
            }

            // If the constructor also has a timeout argument, then don't emit a diagnostic.
            if (operation.Arguments.Length > 2)
            {
                return;
            }

            if (!ValidateParameters(operation.Arguments))
            {
                return;
            }

            // Report the diagnostic with a location that doesn't span the potentially multi-line pattern.
            SyntaxNode? syntaxNodeForDiagnostic = operation.Syntax;
            Debug.Assert(syntaxNodeForDiagnostic is not null);
            Location location = GetLocationBeforeArguments(syntaxNodeForDiagnostic);
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UseRegexSourceGeneration, location));
        }

        /// <summary>
        /// Validates the operation arguments ensuring the pattern and options are constant values.
        /// Returns false if a timeout argument is used.
        /// </summary>
        private static bool ValidateParameters(ImmutableArray<IArgumentOperation> arguments)
        {
            const string timeoutArgumentName = "timeout";
            const string matchTimeoutArgumentName = "matchTimeout";

            if (arguments == null)
            {
                return false;
            }

            for (int i = 0; i < arguments.Length; i++)
            {
                IArgumentOperation argument = arguments[i];
                string? argumentName = argument.Parameter?.Name;

                // If the argument name is null (e.g. an __arglist), then we don't emit a diagnostic.
                if (argumentName is null)
                {
                    return false;
                }

                // If one of the arguments is a timeout, then we don't emit a diagnostic.
                if (argumentName.Equals(timeoutArgumentName, StringComparison.OrdinalIgnoreCase) ||
                    argumentName.Equals(matchTimeoutArgumentName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // If the argument is the pattern, then we validate that it is constant.
                if (argumentName.Equals(PatternArgumentName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!argument.Value.ConstantValue.HasValue)
                    {
                        return false;
                    }

                    try
                    {
                        _ = RegexParser.ParseOptionsInPattern((string)argument.Value.ConstantValue.Value!, RegexOptions.None);
                    }
                    catch (RegexParseException)
                    {
                        // Pattern contained something invalid like "\g" or "\xZZZZ" so we can't parse
                        // sufficiently to look for options in the pattern like "(?i)"
                        // so we won't be able to safely make the fix
                        return false;
                    }

                    continue;
                }

                // If the argument is the options, then we validate that it is constant, that it doesn't have RegexOptions.NonBacktracking, and we store the index.
                if (argumentName.Equals(OptionsArgumentName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!argument.Value.ConstantValue.HasValue)
                    {
                        return false;
                    }

                    RegexOptions value = (RegexOptions)(int)argument.Value.ConstantValue.Value!;
                    if ((value & RegexOptions.NonBacktracking) > 0)
                    {
                        return false;
                    }

                    continue;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets a location for the diagnostic that excludes the argument list.
        /// This prevents multi-line patterns from causing adornments on every line.
        /// </summary>
        private static Location GetLocationBeforeArguments(SyntaxNode syntaxNode)
        {
            // Get the span that ends just before the opening parenthesis of the argument list.
            // For invocations like Regex.IsMatch(input, pattern), this gives us just "Regex.IsMatch".
            // For object creations like new Regex(pattern), this gives us just "new Regex".
            // For target-typed new like new("pattern"), this gives us just "new".
            int? argumentListStart = syntaxNode switch
            {
                InvocationExpressionSyntax invocation => invocation.ArgumentList?.SpanStart,
                ObjectCreationExpressionSyntax creation => creation.ArgumentList?.SpanStart,
                ImplicitObjectCreationExpressionSyntax implicitCreation => implicitCreation.ArgumentList?.SpanStart,
                _ => null
            };

            if (argumentListStart is int end)
            {
                SyntaxTree? tree = syntaxNode.SyntaxTree;
                if (tree is not null)
                {
                    int start = syntaxNode.SpanStart;
                    if (end > start)
                    {
                        TextSpan span = TextSpan.FromBounds(start, end);
                        return Location.Create(tree, span);
                    }
                }
            }

            // Fall back to the full syntax location if we can't create a narrower span.
            return syntaxNode.GetLocation();
        }

        /// <summary>
        /// Ensures that the compilation can find the Regex and RegexAttribute types, and also validates that the
        /// LangVersion of the project is >= 10.0 (which is the current requirement for the Regex source generator.
        /// </summary>
        /// <param name="compilation">The compilation to be analyzed.</param>
        /// <param name="regexTypeSymbol">The resolved Regex type symbol</param>
        /// <returns><see langword="true"/> if source generator is supported in the project; otherwise, <see langword="false"/>.</returns>
        private static bool ProjectSupportsRegexSourceGenerator(Compilation compilation, [NotNullWhen(true)] out INamedTypeSymbol? regexTypeSymbol)
        {
            regexTypeSymbol = compilation.GetTypeByMetadataName(RegexTypeName);
            if (regexTypeSymbol == null)
            {
                return false;
            }

            INamedTypeSymbol? generatedRegexAttributeTypeSymbol = compilation.GetTypeByMetadataName(GeneratedRegexTypeName);
            if (generatedRegexAttributeTypeSymbol == null)
            {
                return false;
            }

            if (((CSharpCompilation)compilation).LanguageVersion <= (LanguageVersion)1000)
            {
                return false;
            }

            return true;
        }
    }
}
