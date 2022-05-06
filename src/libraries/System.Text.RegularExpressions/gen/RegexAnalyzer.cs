// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
    public class RegexAnalyzer : DiagnosticAnalyzer
    {
        // private members
        private const string RegexTypeName = "System.Text.RegularExpressions.Regex";
        private const string RegexGeneratorTypeName = "System.Text.RegularExpressions.RegexGeneratorAttribute";
        private const string TimeSpanTypeName = "System.TimeSpan";
        private const string TimeoutTypeName = "System.Threading.Timeout";

        // internal members
        internal const string PatternIndexName = "PatternIndex";
        internal const string RegexOptionsIndexName = "RegexOptionsIndex";
        internal const string RegexTimeoutIndexName = "RegexTimeoutIndex";
        internal const string RegexTimeoutName = "RegexTimeout";
        internal const string DiagnosticId = "SYSLIB1046";

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.UseRegexSourceGeneration);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register analysis of calls to the Regex constructors
            context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);

            // Register analysis of calls to Regex static methods
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        /// <summary>
        /// Analyzes an invocation expression to see if the invocation is a call to one of the Regex static methods,
        /// and checks if they could be using the source generator instead.
        /// </summary>
        /// <param name="context">The operation context representing the invocation.</param>
        private void AnalyzeInvocation(OperationAnalysisContext context)
        {
            // Ensure source generator is supported.
            if (!ProjectSupportsRegexSourceGenerator(context.Operation))
            {
                return;
            }

            // Ensure the invocation is a Regex static method.
            IInvocationOperation invocationOperation = (IInvocationOperation)context.Operation;
            IMethodSymbol method = invocationOperation.TargetMethod;
            if (!method.IsStatic || !SymbolEqualityComparer.Default.Equals(method.ContainingType, context.Compilation.GetTypeByMetadataName(RegexTypeName)))
            {
                return;
            }

            // Depending on the static method being called, we need to save the parameters as properties so that we can save them onto the diagnostic so that the
            // code fixer can later use that property bag to generate the code fix and emit the RegexGenerator attribute.
            // Most static methods have the same parameter overloads which are covered by the first if block. Replace static method takes extra parameters so that one
            // is treated specially.
            if (method.Name is "IsMatch" or "Match" or "Matches" or "Split" or "Count" or "EnumerateMatches")
            {
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
                    new KeyValuePair<string, string?>(RegexOptionsIndexName, invocationOperation.Arguments.Length > 2 ? "2" : null),
                    new KeyValuePair<string, string?>(RegexTimeoutIndexName, invocationOperation.Arguments.Length > 3 ? "3" : null),
                    new KeyValuePair<string, string?>(RegexTimeoutName, invocationOperation.Arguments.Length > 3 ? CalculateMillisecondsFromTimeSpan(invocationOperation.Arguments[3].Value)?.ToString(CultureInfo.InvariantCulture) : null),
                });

                // Report the diagnostic.
                SyntaxNode? syntaxNodeForDiagnostic = invocationOperation.Syntax.ChildNodes().FirstOrDefault();
                Debug.Assert(syntaxNodeForDiagnostic != null);
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UseRegexSourceGeneration, syntaxNodeForDiagnostic.GetLocation(), properties));
            }
            else if (method.Name is "Replace")
            {
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
                    new KeyValuePair<string, string?>(RegexOptionsIndexName, invocationOperation.Arguments.Length > 3 ? "3" : null),
                    new KeyValuePair<string, string?>(RegexTimeoutIndexName, invocationOperation.Arguments.Length > 4 ? "4" : null),
                    new KeyValuePair<string, string?>(RegexTimeoutName, invocationOperation.Arguments.Length > 4 ? CalculateMillisecondsFromTimeSpan(invocationOperation.Arguments[4].Value)?.ToString(CultureInfo.InvariantCulture) : null),
                });

                // Report the diagnostic.
                SyntaxNode? syntaxNodeForDiagnostic = invocationOperation.Syntax.ChildNodes().FirstOrDefault();
                Debug.Assert(syntaxNodeForDiagnostic is not null);
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UseRegexSourceGeneration, syntaxNodeForDiagnostic.GetLocation(), properties));
            }
        }

        /// <summary>
        /// Analyzes an object creation expression to see if the invocation is a call to one of the Regex constructors,
        /// and checks if they could be using the source generator instead.
        /// </summary>
        /// <param name="context">The object creation context.</param>
        private void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            // Ensure source generator is supported.
            if (!ProjectSupportsRegexSourceGenerator(context.Operation))
            {
                return;
            }

            // Ensure the object creation is a call to the Regex constructor.
            IObjectCreationOperation operation = (IObjectCreationOperation)context.Operation;
            if (!SymbolEqualityComparer.Default.Equals(operation.Type, context.Compilation.GetTypeByMetadataName(RegexTypeName)))
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
                new KeyValuePair<string, string?>(RegexOptionsIndexName, operation.Arguments.Length > 1 ? "1" : null),
                new KeyValuePair<string, string?>(RegexTimeoutIndexName, operation.Arguments.Length > 2 ? "2" : null),
                new KeyValuePair<string, string?>(RegexTimeoutName, operation.Arguments.Length > 2 ? CalculateMillisecondsFromTimeSpan(operation.Arguments[2].Value)?.ToString(CultureInfo.InvariantCulture) : null),
            });

            // Report the diagnostic.
            SyntaxNode? syntaxNodeForDiagnostic = operation.Syntax.ChildNodes().FirstOrDefault()!.Parent;
            Debug.Assert(syntaxNodeForDiagnostic is not null);
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UseRegexSourceGeneration, syntaxNodeForDiagnostic.GetLocation(), properties));
        }

        /// <summary>
        /// Calculates the constant total milliseconds represented by a timespan operation.
        /// </summary>
        /// <remarks>
        /// RegEx constructors and static methods optionally take a <see cref="TimeSpan"/> parameter to represent when it should timeout to avoid
        /// catastrophic backtracking, but the RegexGeneratorAttribute can't take a non-constant parameter so it instead takes an optional int milliseconds.
        /// This method is in charge of verifying if that timespan parameter has a constant value, and if it does, it makes the transformation from Timespan
        /// to milliseconds.
        /// </remarks>
        /// <param name="operation">The operation representing the parameter.</param>
        /// <returns>The total milliseconds in case the TimeSpan value is constant, or <see langword="null"/> if there is no constant value.</returns>
        private static int? CalculateMillisecondsFromTimeSpan(IOperation operation)
        {
            return CalculateMillisecondsFromTimeSpan(operation, 1d);

            static int? CalculateMillisecondsFromTimeSpan(IOperation operation, double factor)
            {
                Compilation compilation = operation.SemanticModel!.Compilation;

                const double TicksToMilliseconds = 1d / TimeSpan.TicksPerMillisecond;
                const double SecondsToMilliseconds = 1000;
                const double MinutesToMilliseconds = 60 * 1000;
                const double HoursToMilliseconds = 60 * 60 * 1000;
                const double DaysToMilliseconds = 24 * 60 * 60 * 1000;

                // If there are implicit conversion operations, we unwrap them all.
                operation = UnwrapImplicitConversionOperations(operation);

                // If we've reached a ConstantValue, then we just multiply it by the passed in factor and return.
                if (operation.ConstantValue.HasValue)
                {
                    if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is long int64Value)
                    {
                        return (int)(int64Value * factor);
                    }

                    if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is int int32Value)
                    {
                        return (int)(int32Value * factor);
                    }

                    if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is double doubleValue)
                    {
                        return (int)(doubleValue * factor);
                    }
                }

                // Case: using the default keyword.
                if (operation is IDefaultValueOperation)
                {
                    return 0;
                }

                // Cases: TimeSpan.FromTicks, TimeSpan.FromMilliseconds, TimeSpan.FromSeconds, TimeSpan.FromMinutes, TimeSpan.FromHours, TimeSpan.FromDays
                if (operation is IInvocationOperation invocationOperation)
                {
                    IMethodSymbol method = invocationOperation.TargetMethod;
                    if (method.IsStatic && SymbolEqualityComparer.Default.Equals(method.ContainingType, compilation.GetTypeByMetadataName(TimeSpanTypeName)))
                    {
                        return method.Name switch
                        {
                            "FromTicks" => CalculateMillisecondsFromTimeSpan(invocationOperation.Arguments[0].Value, TicksToMilliseconds),
                            "FromMilliseconds" => CalculateMillisecondsFromTimeSpan(invocationOperation.Arguments[0].Value, 1),
                            "FromSeconds" => CalculateMillisecondsFromTimeSpan(invocationOperation.Arguments[0].Value, SecondsToMilliseconds),
                            "FromMinutes" => CalculateMillisecondsFromTimeSpan(invocationOperation.Arguments[0].Value, MinutesToMilliseconds),
                            "FromHours" => CalculateMillisecondsFromTimeSpan(invocationOperation.Arguments[0].Value, HoursToMilliseconds),
                            "FromDays" => CalculateMillisecondsFromTimeSpan(invocationOperation.Arguments[0].Value, DaysToMilliseconds),
                            _ => null,
                        };
                    }

                    return null;
                }

                if (operation is IFieldReferenceOperation fieldReferenceOperation)
                {
                    // Cases: TimeSpan.Zero, TimeSpan.MinValue, TimeSpan.MaxValue
                    ISymbol member = fieldReferenceOperation.Member;
                    if (member.IsStatic && SymbolEqualityComparer.Default.Equals(member.ContainingType, compilation.GetTypeByMetadataName(TimeSpanTypeName)))
                    {
                        return member.Name switch
                        {
                            "Zero" => 0,
                            "MinValue" => (int)TimeSpan.MinValue.TotalMilliseconds,
                            "MaxValue" => (int)TimeSpan.MaxValue.TotalMilliseconds,
                            _ => null,
                        };
                    }

                    // Cases: Regex.InfiniteMatchTimeout
                    if (member.IsStatic && SymbolEqualityComparer.Default.Equals(member.ContainingType, compilation.GetTypeByMetadataName(RegexTypeName)))
                    {
                        return member.Name switch
                        {
                            "InfiniteMatchTimeout" => -1,
                            _ => null,
                        };
                    }

                    // Cases: Timeout.InfiniteTimeSpan, Timeout.Infinite
                    if (member.IsStatic && SymbolEqualityComparer.Default.Equals(member.ContainingType, compilation.GetTypeByMetadataName(TimeoutTypeName)))
                    {
                        return member.Name switch
                        {
                            "InfiniteTimeSpan" => -1,
                            "Infinite" => -1,
                            _ => null,
                        };
                    }

                    return null;
                }

                // Cases: Instantiating a new TimeSpan instance via a call to one of the TimeSpan constructors.
                if (operation is IObjectCreationOperation objectCreationOperation)
                {
                    if (SymbolEqualityComparer.Default.Equals(objectCreationOperation.Type, compilation.GetTypeByMetadataName(TimeSpanTypeName)))
                    {
                        switch (objectCreationOperation.Arguments.Length)
                        {
                            case 1: // new TimeSpan(long ticks)
                                return CalculateMillisecondsFromTimeSpan(objectCreationOperation.Arguments[0].Value, 1d / TimeSpan.TicksPerMillisecond);

                            case 3: // new TimeSpan(int hours, int minutes, int seconds)
                                return AddValues(
                                    CalculateMillisecondsFromTimeSpan(objectCreationOperation.Arguments[0].Value, HoursToMilliseconds),
                                    CalculateMillisecondsFromTimeSpan(objectCreationOperation.Arguments[1].Value, MinutesToMilliseconds),
                                    CalculateMillisecondsFromTimeSpan(objectCreationOperation.Arguments[2].Value, SecondsToMilliseconds)
                                    );

                            case 4: // new TimeSpan(int days, int hours, int minutes, int seconds)
                                return AddValues(
                                    CalculateMillisecondsFromTimeSpan(objectCreationOperation.Arguments[0].Value, DaysToMilliseconds),
                                    CalculateMillisecondsFromTimeSpan(objectCreationOperation.Arguments[1].Value, HoursToMilliseconds),
                                    CalculateMillisecondsFromTimeSpan(objectCreationOperation.Arguments[2].Value, MinutesToMilliseconds),
                                    CalculateMillisecondsFromTimeSpan(objectCreationOperation.Arguments[3].Value, SecondsToMilliseconds)
                                    );

                            case 5: // new TimeSpan(int days, int hours, int minutes, int seconds, int milliseconds)
                                return AddValues(
                                    CalculateMillisecondsFromTimeSpan(objectCreationOperation.Arguments[0].Value, DaysToMilliseconds),
                                    CalculateMillisecondsFromTimeSpan(objectCreationOperation.Arguments[1].Value, HoursToMilliseconds),
                                    CalculateMillisecondsFromTimeSpan(objectCreationOperation.Arguments[2].Value, MinutesToMilliseconds),
                                    CalculateMillisecondsFromTimeSpan(objectCreationOperation.Arguments[3].Value, SecondsToMilliseconds),
                                    CalculateMillisecondsFromTimeSpan(objectCreationOperation.Arguments[4].Value, 1)
                                    );
                        }

                        return null;
                    }
                }

                return null;

                // Helper method that adds all the passed in Nullable<int>
                static int? AddValues(params int?[] values)
                {
                    int result = 0;
                    foreach (int? value in values)
                    {
                        if (!value.HasValue)
                        {
                            return null;
                        }

                        result += value.GetValueOrDefault();
                    }

                    return result;
                }

                // Helper method that unwraps all of the implicit operations
                static IOperation UnwrapImplicitConversionOperations(IOperation operation)
                {
                    if (operation is IConversionOperation conversionOperation && conversionOperation.IsImplicit)
                    {
                        return UnwrapImplicitConversionOperations(conversionOperation.Operand);
                    }

                    return operation;
                }
            }
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

            Compilation compilation = argument.SemanticModel!.Compilation;
            if (SymbolEqualityComparer.Default.Equals(valueOperation.Type, compilation.GetTypeByMetadataName(TimeSpanTypeName)))
            {
                return CalculateMillisecondsFromTimeSpan(valueOperation).HasValue;
            }

            return false;
        }

        /// <summary>
        /// Ensures that the compilation can find the Regex and RegexAttribute types, and also validates that the
        /// LangVersion of the project is >= 10.0 (which is the current requirement for the Regex source generator.
        /// </summary>
        /// <param name="operation">The operation to be analyzed.</param>
        /// <returns><see langword="true"/> if source generator is supported in the project; otherwise, <see langword="false"/>.</returns>
        private static bool ProjectSupportsRegexSourceGenerator(IOperation operation)
        {
            Compilation compilation = operation.SemanticModel!.Compilation;
            INamedTypeSymbol? regexSymbol = compilation.GetTypeByMetadataName(RegexTypeName);
            if (regexSymbol == null)
            {
                return false;
            }

            INamedTypeSymbol? regexGeneratorAttributeSymbol = compilation.GetTypeByMetadataName(RegexGeneratorTypeName);
            if (regexGeneratorAttributeSymbol == null)
            {
                return false;
            }

            if (operation.Syntax.SyntaxTree.Options is CSharpParseOptions options && options.LanguageVersion <= (LanguageVersion)1000)
            {
                return false;
            }

            return true;
        }
    }
}
