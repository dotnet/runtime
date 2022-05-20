// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
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
    public class UpgradeToRegexGeneratorAnalyzer : DiagnosticAnalyzer
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

            context.RegisterCompilationStartAction(compilationContext =>
            {
                Compilation compilation = compilationContext.Compilation;

                if (!ProjectSupportsRegexSourceGenerator(compilation, out INamedTypeSymbol? regexTypeSymbol))
                {
                    return;
                }

                // Register analysis of calls to the Regex constructors
                compilationContext.RegisterOperationAction(context => AnalyzeObjectCreation(context, regexTypeSymbol), OperationKind.ObjectCreation);

                // Register analysis of calls to Regex static methods
                compilationContext.RegisterOperationAction(context => AnalyzeInvocation(context, regexTypeSymbol), OperationKind.Invocation);
            });
        }

        /// <summary>
        /// Analyzes an invocation expression to see if the invocation is a call to one of the Regex static methods,
        /// and checks if they could be using the source generator instead.
        /// </summary>
        /// <param name="context">The compilation context representing the invocation.</param>
        private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol regexTypeSymbol)
        {
            // Ensure the invocation is a Regex static method.
            IInvocationOperation invocationOperation = (IInvocationOperation)context.Operation;
            IMethodSymbol method = invocationOperation.TargetMethod;
            if (!method.IsStatic || !SymbolEqualityComparer.Default.Equals(method.ContainingType, regexTypeSymbol))
            {
                return;
            }

            // Depending on the static method being called, we need to save the parameters as properties so that we can save them onto the diagnostic so that the
            // code fixer can later use that property bag to generate the code fix and emit the RegexGenerator attribute.
            // Most static methods have the same parameter overloads which are covered by the first if block. Replace static method takes extra parameters so that one
            // is treated specially.
            if (method.Name is "IsMatch" or "Match" or "Matches" or "Split" or "Count" or "EnumerateMatches")
            {
                // if the static method invocation has a timeout, then don't emit a diagnostic.
                if (invocationOperation.Arguments.Length > 3)
                {
                    return;
                }

                for (int i = 1; i < invocationOperation.Arguments.Length; i++)
                {
                    // Ensure that all inputs to the static method are constant.
                    if (!IsConstant(invocationOperation.Arguments[i]))
                    {
                        return;
                    }
                }

                // Create the property bag.
                ImmutableDictionary<string, string?> properties = ImmutableDictionary.CreateRange(new[]
                {
                    new KeyValuePair<string, string?>(PatternIndexName, "1"),
                    new KeyValuePair<string, string?>(RegexOptionsIndexName, invocationOperation.Arguments.Length > 2 ? "2" : null)
                });

                // Report the diagnostic.
                SyntaxNode? syntaxNodeForDiagnostic = invocationOperation.Syntax;
                Debug.Assert(syntaxNodeForDiagnostic != null);
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UseRegexSourceGeneration, syntaxNodeForDiagnostic.GetLocation(), properties));
            }
            else if (method.Name is "Replace")
            {
                // if the static method invocation has a timeout, then don't emit a diagnostic.
                if (invocationOperation.Arguments.Length > 4)
                {
                    return;
                }

                for (int i = 1; i < invocationOperation.Arguments.Length; i++)
                {
                    // Skip the third parameter as that is the parameter to be used as replacement and doesn't affect the source generator.
                    if (i == 2)
                    {
                        continue;
                    }

                    // Ensure that all inputs to the static method are constant.
                    if (!IsConstant(invocationOperation.Arguments[i]))
                    {
                        return;
                    }
                }

                // Create the property bag.
                ImmutableDictionary<string, string?> properties = ImmutableDictionary.CreateRange(new[]
                {
                    new KeyValuePair<string, string?>(PatternIndexName, "1"),
                    new KeyValuePair<string, string?>(RegexOptionsIndexName, invocationOperation.Arguments.Length > 3 ? "3" : null)
                });

                // Report the diagnostic.
                SyntaxNode? syntaxNodeForDiagnostic = invocationOperation.Syntax;
                Debug.Assert(syntaxNodeForDiagnostic is not null);
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

            // Ensure that all inputs to the constructor are constant.
            foreach (IArgumentOperation argument in operation.Arguments)
            {
                if (!IsConstant(argument))
                {
                    return;
                }
            }

            // Create the property bag.
            ImmutableDictionary<string, string?> properties = ImmutableDictionary.CreateRange(new[]
            {
                new KeyValuePair<string, string?>(PatternIndexName, "0"),
                new KeyValuePair<string, string?>(RegexOptionsIndexName, operation.Arguments.Length > 1 ? "1" : null)
            });

            // Report the diagnostic.
            SyntaxNode? syntaxNodeForDiagnostic = operation.Syntax;
            Debug.Assert(syntaxNodeForDiagnostic is not null);
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UseRegexSourceGeneration, syntaxNodeForDiagnostic.GetLocation(), properties));
        }

        /// <summary>
        /// Ensures that the input to the constructor or invocation is constant at compile time
        /// which is a requirement in order to be able to use the source generator.
        /// </summary>
        /// <param name="argument">The argument to be analyzed.</param>
        /// <returns><see langword="true"/> if the argument is constant; otherwise, <see langword="false"/>.</returns>
        private static bool IsConstant(IArgumentOperation argument)
        {
            IOperation valueOperation = argument.Value;
            if (valueOperation.ConstantValue.HasValue)
            {
                return true;
            }

            return false;
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
