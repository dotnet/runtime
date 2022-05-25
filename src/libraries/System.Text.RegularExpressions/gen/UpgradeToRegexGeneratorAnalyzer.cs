// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace System.Text.RegularExpressions.Generator
{
    /// <summary>
    /// Roslyn analyzer that searches for invocations of the Regex constructors, or the
    /// Regex static methods and analyzes if the callsite could be using the Regex Generator instead.
    /// If so, it will emit an informational diagnostic to suggest use the Regex Generator.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UpgradeToRegexGeneratorAnalyzer : DiagnosticAnalyzer
    {
        private const string RegexTypeName = "System.Text.RegularExpressions.Regex";
        private const string RegexGeneratorTypeName = "System.Text.RegularExpressions.RegexGeneratorAttribute";

        internal const string PatternIndexName = "PatternIndex";
        internal const string RegexOptionsIndexName = "RegexOptionsIndex";

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.UseRegexSourceGeneration);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(async compilationContext =>
            {
                Compilation compilation = compilationContext.Compilation;

                // Validate that the project supports the Regex Source Generator based on target framework,
                // language version, etc.
                if (!ProjectSupportsRegexSourceGenerator(compilation, out INamedTypeSymbol? regexTypeSymbol))
                {
                    return;
                }

                // Validate that the project is not using top-level statements, since if it were, the code-fixer
                // can't easily convert to the source generator without having to make the program not use top-level
                // statements any longer.
                if (await ProjectUsesTopLevelStatements(compilation, compilationContext.CancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                // Pre-compute a hash with all of the method symbols that we want to analyze for possibly emitting
                // a diagnostic.
                HashSet<IMethodSymbol> staticMethodsToDetect = GetMethodSymbolHash(regexTypeSymbol,
                    new HashSet<string> { "Count", "EnumerateMatches", "IsMatch", "Match", "Matches", "Split", "Replace" });

                // Register analysis of calls to the Regex constructors
                compilationContext.RegisterOperationAction(context => AnalyzeObjectCreation(context, regexTypeSymbol), OperationKind.ObjectCreation);

                // Register analysis of calls to Regex static methods
                compilationContext.RegisterOperationAction(context => AnalyzeInvocation(context, regexTypeSymbol, staticMethodsToDetect), OperationKind.Invocation);
            });

            // Creates a HashSet of all of the method Symbols containing the static methods to analyze.
            static HashSet<IMethodSymbol> GetMethodSymbolHash(INamedTypeSymbol regexTypeSymbol, HashSet<string> methodNames)
            {
                // This warning is due to a false positive bug https://github.com/dotnet/roslyn-analyzers/issues/5804
                // This issue has now been fixed, but we are not yet consuming the fix and getting this package
                // as a transitive dependency from Microsoft.CodeAnalysis.CSharp.Workspaces. Once that dependency
                // is updated at the repo-level, we should come and remove the pragma disable.
#pragma warning disable RS1024 // Compare symbols correctly
                HashSet<IMethodSymbol> hash = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly
                ImmutableArray<ISymbol> allMembers = regexTypeSymbol.GetMembers();

                foreach(ISymbol member in allMembers)
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
            // code fixer can later use that property bag to generate the code fix and emit the RegexGenerator attribute.
            if (staticMethodsToDetect.Contains(method))
            {
                string? patternArgumentIndex = null;
                string? optionsArgumentIndex = null;

                // Validate that arguments pattern and options are constant and timeout was not passed in.
                if (!TryValidateParametersAndExtractArgumentIndices(invocationOperation.Arguments, ref patternArgumentIndex, ref optionsArgumentIndex))
                {
                    return;
                }

                // Create the property bag.
                ImmutableDictionary<string, string?> properties = ImmutableDictionary.CreateRange(new[]
                {
                    new KeyValuePair<string, string?>(PatternIndexName, patternArgumentIndex),
                    new KeyValuePair<string, string?>(RegexOptionsIndexName, optionsArgumentIndex)
                });

                // Report the diagnostic.
                SyntaxNode? syntaxNodeForDiagnostic = invocationOperation.Syntax;
                Debug.Assert(syntaxNodeForDiagnostic != null);
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UseRegexSourceGeneration, syntaxNodeForDiagnostic.GetLocation(), properties));
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

            string? patternArgumentIndex = null;
            string? optionsArgumentIndex = null;

            if (!TryValidateParametersAndExtractArgumentIndices(operation.Arguments, ref patternArgumentIndex, ref optionsArgumentIndex))
            {
                return;
            }

            // Create the property bag.
            ImmutableDictionary<string, string?> properties = ImmutableDictionary.CreateRange(new[]
            {
                new KeyValuePair<string, string?>(PatternIndexName, patternArgumentIndex),
                new KeyValuePair<string, string?>(RegexOptionsIndexName, optionsArgumentIndex)
            });

            // Report the diagnostic.
            SyntaxNode? syntaxNodeForDiagnostic = operation.Syntax;
            Debug.Assert(syntaxNodeForDiagnostic is not null);
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UseRegexSourceGeneration, syntaxNodeForDiagnostic.GetLocation(), properties));
        }

        /// <summary>
        /// Validates the operation arguments ensuring they all have constant values, and if so it stores the argument
        /// indices for the pattern and options. If timeout argument was used, then this returns false.
        /// </summary>
        private static bool TryValidateParametersAndExtractArgumentIndices(ImmutableArray<IArgumentOperation> arguments, ref string? patternArgumentIndex, ref string? optionsArgumentIndex)
        {
            const string timeoutArgumentName = "timeout";
            const string matchTimeoutArgumentName = "matchTimeout";
            const string patternArgumentName = "pattern";
            const string optionsArgumentName = "options";

            if (arguments == null)
            {
                return false;
            }

            for (int i = 0; i < arguments.Length; i++)
            {
                IArgumentOperation argument = arguments[i];
                string argumentName = argument.Parameter.Name;

                // If one of the arguments is a timeout, then we don't emit a diagnostic.
                if (argumentName.Equals(timeoutArgumentName, StringComparison.OrdinalIgnoreCase) ||
                    argumentName.Equals(matchTimeoutArgumentName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // If the argument is the pattern, then we validate that it is constant and we store the index.
                if (argumentName.Equals(patternArgumentName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!IsConstant(argument))
                    {
                        return false;
                    }

                    patternArgumentIndex = i.ToString();
                    continue;
                }

                // If the argument is the options, then we validate that it is constant, that it doesn't have RegexOptions.NonBacktracking, and we store the index.
                if (argumentName.Equals(optionsArgumentName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!IsConstant(argument))
                    {
                        return false;
                    }

                    RegexOptions value = (RegexOptions)((int)argument.Value.ConstantValue.Value);
                    if ((value & RegexOptions.NonBacktracking) > 0)
                    {
                        return false;
                    }

                    optionsArgumentIndex = i.ToString();
                    continue;
                }
            }

            return true;
        }

        /// <summary>
        /// Ensures that the input to the constructor or invocation is constant at compile time
        /// which is a requirement in order to be able to use the source generator.
        /// </summary>
        /// <param name="argument">The argument to be analyzed.</param>
        /// <returns><see langword="true"/> if the argument is constant; otherwise, <see langword="false"/>.</returns>
        private static bool IsConstant(IArgumentOperation argument)
            => argument.Value.ConstantValue.HasValue;

        /// <summary>
        /// Detects whether or not the current project is using top-level statements.
        /// </summary>
        private static async Task<bool> ProjectUsesTopLevelStatements(Compilation compilation, CancellationToken cancellationToken)
        {
            SyntaxNode? root = await compilation.SyntaxTrees.FirstOrDefault().GetRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
            {
                return false;
            }

            return root.DescendantNodesAndSelf().Where(node => node.IsKind(SyntaxKind.GlobalStatement)).Any();
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

            INamedTypeSymbol regexGeneratorAttributeTypeSymbol = compilation.GetTypeByMetadataName(RegexGeneratorTypeName);
            if (regexGeneratorAttributeTypeSymbol == null)
            {
                return false;
            }

            if (compilation.SyntaxTrees.FirstOrDefault().Options is CSharpParseOptions options && options.LanguageVersion <= (LanguageVersion)1000)
            {
                return false;
            }

            return true;
        }
    }
}
