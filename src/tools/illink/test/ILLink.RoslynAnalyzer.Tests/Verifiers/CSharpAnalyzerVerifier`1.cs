// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.RoslynAnalyzer.Tests
{
	public static partial class CSharpAnalyzerVerifier<TAnalyzer>
		where TAnalyzer : DiagnosticAnalyzer, new()
	{
		/// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.Diagnostic()"/>
		public static DiagnosticResult Diagnostic ()
			=> CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic ();

		/// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.Diagnostic(string)"/>
		public static DiagnosticResult Diagnostic (string diagnosticId)
			=> CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic (diagnosticId);

		/// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.Diagnostic(DiagnosticDescriptor)"/>
		public static DiagnosticResult Diagnostic (DiagnosticDescriptor descriptor)
			=> CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic (descriptor);

		public static DiagnosticResult Diagnostic (DiagnosticId diagnosticId)
			=> CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic (DiagnosticDescriptors.GetDiagnosticDescriptor (diagnosticId));

		/// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
		public static async Task VerifyAnalyzerAsync (
			string src,
			bool consoleApplication,
			(string, string)[]? analyzerOptions = null,
			IEnumerable<MetadataReference>? additionalReferences = null,
			params DiagnosticResult[] expected)
		{
			var (comp, _, exceptionDiagnostics) = TestCaseCompilation.CreateCompilation (src, consoleApplication, analyzerOptions, additionalReferences);
			var diags = (await comp.GetAllDiagnosticsAsync ()).AddRange (exceptionDiagnostics);
			var analyzers = ImmutableArray.Create<DiagnosticAnalyzer> (new TAnalyzer ());
			VerifyDiagnosticResults (diags, analyzers, expected, DefaultVerifier);
		}

		private static readonly IVerifier DefaultVerifier = new DefaultVerifier ();

		/// <summary>
		/// Gets the default full name of the first source file added for a test.
		/// </summary>
		private static string DefaultFilePath => "";

		/// <summary>
		/// Gets or sets the timeout to use when matching expected and actual diagnostics. The default value is 2
		/// seconds.
		/// </summary>
		private static TimeSpan MatchDiagnosticsTimeout => TimeSpan.FromSeconds (2);


		/// <summary>
		/// Checks each of the actual <see cref="Diagnostic"/>s found and compares them with the corresponding
		/// <see cref="DiagnosticResult"/> in the array of expected results. <see cref="Diagnostic"/>s are considered
		/// equal only if the <see cref="DiagnosticResult.Spans"/>, <see cref="DiagnosticResult.Id"/>,
		/// <see cref="DiagnosticResult.Severity"/>, and <see cref="DiagnosticResult.Message"/> of the
		/// <see cref="DiagnosticResult"/> match the actual <see cref="Diagnostic"/>.
		/// </summary>
		/// <param name="actualResults">The <see cref="Diagnostic"/>s found by the compiler after running the analyzer
		/// on the source code.</param>
		/// <param name="analyzers">The analyzers that have been run on the sources.</param>
		/// <param name="expectedResults">A collection of <see cref="DiagnosticResult"/>s describing the expected
		/// diagnostics for the sources.</param>
		/// <param name="verifier">The verifier to use for test assertions.</param>
		internal static void VerifyDiagnosticResults (IEnumerable<Diagnostic> actualResults, ImmutableArray<DiagnosticAnalyzer> analyzers, DiagnosticResult[] expectedResults, IVerifier verifier)
		{
			var matchedDiagnostics = MatchDiagnostics (actualResults.ToArray (), expectedResults);
			verifier.Equal (actualResults.Count (), matchedDiagnostics.Count (x => x.actual is object), $"{nameof (MatchDiagnostics)} failed to include all actual diagnostics in the result");
			verifier.Equal (expectedResults.Length, matchedDiagnostics.Count (x => x.expected is object), $"{nameof (MatchDiagnostics)} failed to include all expected diagnostics in the result");

			actualResults = matchedDiagnostics.Select (x => x.actual).WhereNotNull ();
			expectedResults = matchedDiagnostics.Where (x => x.expected is object).Select (x => x.expected.GetValueOrDefault ()).ToArray ();

			var expectedCount = expectedResults.Length;
			var actualCount = actualResults.Count ();

			var diagnosticsOutput = actualResults.Any () ? FormatDiagnostics (analyzers, DefaultFilePath, actualResults.ToArray ()) : "    NONE.";
			var message = $"Mismatch between number of diagnostics returned, expected \"{expectedCount}\" actual \"{actualCount}\"\r\n\r\nDiagnostics:\r\n{diagnosticsOutput}\r\n";
			verifier.Equal (expectedCount, actualCount, message);

			for (var i = 0; i < expectedResults.Length; i++) {
				var actual = actualResults.ElementAt (i);
				var expected = expectedResults[i];

				if (!expected.HasLocation) {
					message = FormatVerifierMessage (analyzers, actual, expected, "Expected a project diagnostic with no location:");
					verifier.Equal (Location.None, actual.Location, message);
				} else {
					VerifyDiagnosticLocation (analyzers, actual, expected, actual.Location, expected.Spans[0], verifier);
					if (!expected.Options.HasFlag (DiagnosticOptions.IgnoreAdditionalLocations)) {
						var additionalLocations = actual.AdditionalLocations.ToArray ();

						message = FormatVerifierMessage (analyzers, actual, expected, $"Expected {expected.Spans.Length - 1} additional locations but got {additionalLocations.Length} for Diagnostic:");
						verifier.Equal (expected.Spans.Length - 1, additionalLocations.Length, message);

						for (var j = 0; j < additionalLocations.Length; ++j) {
							VerifyDiagnosticLocation (analyzers, actual, expected, additionalLocations[j], expected.Spans[j + 1], verifier);
						}
					}
				}

				message = FormatVerifierMessage (analyzers, actual, expected, $"Expected diagnostic id to be \"{expected.Id}\" was \"{actual.Id}\"");
				verifier.Equal (expected.Id, actual.Id, message);

				if (!expected.Options.HasFlag (DiagnosticOptions.IgnoreSeverity)) {
					message = FormatVerifierMessage (analyzers, actual, expected, $"Expected diagnostic severity to be \"{expected.Severity}\" was \"{actual.Severity}\"");
					verifier.Equal (expected.Severity, actual.Severity, message);
				}

				if (expected.Message != null) {
					message = FormatVerifierMessage (analyzers, actual, expected, $"Expected diagnostic message to be \"{expected.Message}\" was \"{actual.GetMessage ()}\"");
					verifier.Equal (expected.Message, actual.GetMessage (), message);
				} else if (expected.MessageArguments?.Length > 0) {
					message = FormatVerifierMessage (analyzers, actual, expected, $"Expected diagnostic message arguments to match");
					verifier.SequenceEqual (
						expected.MessageArguments.Select (argument => argument?.ToString () ?? string.Empty),
						GetArguments (actual).Select (argument => argument?.ToString () ?? string.Empty),
						StringComparer.Ordinal,
						message);
				}
			}
		}

		internal static string FormatVerifierMessage (ImmutableArray<DiagnosticAnalyzer> analyzers, Diagnostic actual, DiagnosticResult expected, string message)
		{
			return $"{message}{Environment.NewLine}" +
				$"{Environment.NewLine}" +
				$"Expected diagnostic:{Environment.NewLine}" +
				$"    {FormatDiagnostics (analyzers, DefaultFilePath, expected)}{Environment.NewLine}" +
				$"Actual diagnostic:{Environment.NewLine}" +
				$"    {FormatDiagnostics (analyzers, DefaultFilePath, actual)}{Environment.NewLine}";
		}

		/// <summary>
		/// Helper method to <see cref="VerifyDiagnosticResults"/> that checks the location of a
		/// <see cref="Diagnostic"/> and compares it with the location described by a
		/// <see cref="FileLinePositionSpan"/>.
		/// </summary>
		/// <param name="analyzers">The analyzer that have been run on the sources.</param>
		/// <param name="diagnostic">The diagnostic that was found in the code.</param>
		/// <param name="expectedDiagnostic">The expected diagnostic.</param>
		/// <param name="actual">The location of the diagnostic found in the code.</param>
		/// <param name="expected">The <see cref="FileLinePositionSpan"/> describing the expected location of the
		/// diagnostic.</param>
		/// <param name="verifier">The verifier to use for test assertions.</param>
		private static void VerifyDiagnosticLocation (ImmutableArray<DiagnosticAnalyzer> analyzers, Diagnostic diagnostic, DiagnosticResult expectedDiagnostic, Location actual, DiagnosticLocation expected, IVerifier verifier)
		{
			var actualSpan = actual.GetLineSpan ();

			var assert = actualSpan.Path == expected.Span.Path || (actualSpan.Path?.Contains ("Test0.") == true && expected.Span.Path.Contains ("Test."));

			var message = FormatVerifierMessage (analyzers, diagnostic, expectedDiagnostic, $"Expected diagnostic to be in file \"{expected.Span.Path}\" was actually in file \"{actualSpan.Path}\"");
			verifier.True (assert, message);

			VerifyLinePosition (analyzers, diagnostic, expectedDiagnostic, actualSpan.StartLinePosition, expected.Span.StartLinePosition, "start", verifier);
			if (!expected.Options.HasFlag (DiagnosticLocationOptions.IgnoreLength)) {
				VerifyLinePosition (analyzers, diagnostic, expectedDiagnostic, actualSpan.EndLinePosition, expected.Span.EndLinePosition, "end", verifier);
			}
		}

		private static void VerifyLinePosition (ImmutableArray<DiagnosticAnalyzer> analyzers, Diagnostic diagnostic, DiagnosticResult expectedDiagnostic, LinePosition actualLinePosition, LinePosition expectedLinePosition, string positionText, IVerifier verifier)
		{
			var message = FormatVerifierMessage (analyzers, diagnostic, expectedDiagnostic, $"Expected diagnostic to {positionText} on line \"{expectedLinePosition.Line + 1}\" was actually on line \"{actualLinePosition.Line + 1}\"");
			verifier.Equal (
				expectedLinePosition.Line,
				actualLinePosition.Line,
				message);

			message = FormatVerifierMessage (analyzers, diagnostic, expectedDiagnostic, $"Expected diagnostic to {positionText} at column \"{expectedLinePosition.Character + 1}\" was actually at column \"{actualLinePosition.Character + 1}\"");
			verifier.Equal (
				expectedLinePosition.Character,
				actualLinePosition.Character,
				message);
		}

		/// <summary>
		/// Helper method to format a <see cref="Diagnostic"/> into an easily readable string.
		/// </summary>
		/// <param name="analyzers">The analyzers that this verifier tests.</param>
		/// <param name="defaultFilePath">The default file path for diagnostics.</param>
		/// <param name="diagnostics">A collection of <see cref="DiagnosticResult"/>s to be formatted.</param>
		/// <returns>The <paramref name="diagnostics"/> formatted as a string.</returns>
		private static string FormatDiagnostics (ImmutableArray<DiagnosticAnalyzer> analyzers, string defaultFilePath, params DiagnosticResult[] diagnostics)
		{
			var builder = new StringBuilder ();
			for (var i = 0; i < diagnostics.Length; ++i) {
				var diagnosticsId = diagnostics[i].Id;

				builder.Append ("// ").AppendLine (diagnostics[i].ToString ());

				var applicableAnalyzer = analyzers.FirstOrDefault (a => a.SupportedDiagnostics.Any (dd => dd.Id == diagnosticsId));
				if (applicableAnalyzer != null) {
					var analyzerType = applicableAnalyzer.GetType ();
					var rule = diagnostics[i].HasLocation &&
						applicableAnalyzer.SupportedDiagnostics.Length == 1 ? string.Empty : GetDiagnosticIdArgumentString (diagnosticsId);

					if (!diagnostics[i].HasLocation) {
						builder.Append ($"new DiagnosticResult({rule})");
					} else {
						builder.Append ($"VerifyCS.Diagnostic({rule})");
					}
				} else {
					builder.Append (
						diagnostics[i].Severity switch {
							DiagnosticSeverity.Error => $"{nameof (DiagnosticResult)}.{nameof (DiagnosticResult.CompilerError)}(\"{diagnostics[i].Id}\")",
							DiagnosticSeverity.Warning => $"{nameof (DiagnosticResult)}.{nameof (DiagnosticResult.CompilerWarning)}(\"{diagnostics[i].Id}\")",
							var severity => $"new {nameof (DiagnosticResult)}(\"{diagnostics[i].Id}\", {nameof (DiagnosticSeverity)}.{severity})",
						});
				}

				if (!diagnostics[i].HasLocation) {
					// No additional location data needed
				} else {
					foreach (var span in diagnostics[i].Spans) {
						AppendLocation (span);
						if (diagnostics[i].Options.HasFlag (DiagnosticOptions.IgnoreAdditionalLocations)) {
							break;
						}
					}
				}

				var arguments = diagnostics[i].MessageArguments;
				if (arguments?.Length > 0) {
					builder.Append ($".{nameof (DiagnosticResult.WithArguments)}(");
					builder.Append (string.Join (", ", arguments.Select (a => "\"" + a?.ToString () + "\"")));
					builder.Append (")");
				}

				builder.AppendLine (",");
			}

			return builder.ToString ();

			// Local functions
			void AppendLocation (DiagnosticLocation location)
			{
				var pathString = location.Span.Path == defaultFilePath ? string.Empty : $"\"{location.Span.Path}\", ";
				var linePosition = location.Span.StartLinePosition;

				if (location.Options.HasFlag (DiagnosticLocationOptions.IgnoreLength)) {
					builder.Append ($".WithLocation({pathString}{linePosition.Line + 1}, {linePosition.Character + 1})");
				} else {
					var endLinePosition = location.Span.EndLinePosition;
					builder.Append ($".WithSpan({pathString}{linePosition.Line + 1}, {linePosition.Character + 1}, {endLinePosition.Line + 1}, {endLinePosition.Character + 1})");
				}
			}
		}


		/// <summary>
		/// Helper method to format a <see cref="Diagnostic"/> into an easily readable string.
		/// </summary>
		/// <param name="analyzers">The analyzers that this verifier tests.</param>
		/// <param name="defaultFilePath">The default file path for diagnostics.</param>
		/// <param name="diagnostics">A collection of <see cref="Diagnostic"/>s to be formatted.</param>
		/// <returns>The <paramref name="diagnostics"/> formatted as a string.</returns>
		private static string FormatDiagnostics (ImmutableArray<DiagnosticAnalyzer> analyzers, string defaultFilePath, params Diagnostic[] diagnostics)
		{
			var builder = new StringBuilder ();
			for (var i = 0; i < diagnostics.Length; ++i) {
				var diagnosticsId = diagnostics[i].Id;
				var location = diagnostics[i].Location;

				builder.Append ("// ").AppendLine (diagnostics[i].ToString ());

				var applicableAnalyzer = analyzers.FirstOrDefault (a => a.SupportedDiagnostics.Any (dd => dd.Id == diagnosticsId));
				if (applicableAnalyzer != null) {
					var analyzerType = applicableAnalyzer.GetType ();
					var rule = location != Location.None && location.IsInSource &&
						applicableAnalyzer.SupportedDiagnostics.Length == 1 ? string.Empty : GetDiagnosticIdArgumentString (diagnosticsId);

					if (location == Location.None || !location.IsInSource) {
						builder.Append ($"new DiagnosticResult({rule})");
					} else {
						builder.Append ($"VerifyCS.Diagnostic({rule})");
					}
				} else {
					builder.Append (
						diagnostics[i].Severity switch {
							DiagnosticSeverity.Error => $"{nameof (DiagnosticResult)}.{nameof (DiagnosticResult.CompilerError)}(\"{diagnostics[i].Id}\")",
							DiagnosticSeverity.Warning => $"{nameof (DiagnosticResult)}.{nameof (DiagnosticResult.CompilerWarning)}(\"{diagnostics[i].Id}\")",
							var severity => $"new {nameof (DiagnosticResult)}(\"{diagnostics[i].Id}\", {nameof (DiagnosticSeverity)}.{severity})",
						});
				}

				if (location == Location.None) {
					// No additional location data needed
				} else {
					AppendLocation (diagnostics[i].Location);
					foreach (var additionalLocation in diagnostics[i].AdditionalLocations) {
						AppendLocation (additionalLocation);
					}
				}

				var arguments = GetArguments (diagnostics[i]);
				if (arguments.Count > 0) {
					builder.Append ($".{nameof (DiagnosticResult.WithArguments)}(");
					builder.Append (string.Join (", ", arguments.Select (a => "\"" + a?.ToString () + "\"")));
					builder.Append (")");
				}

				builder.AppendLine (",");
			}

			return builder.ToString ();

			// Local functions
			void AppendLocation (Location location)
			{
				var lineSpan = location.GetLineSpan ();
				var pathString = location.IsInSource && lineSpan.Path == defaultFilePath ? string.Empty : $"\"{lineSpan.Path}\", ";
				var linePosition = lineSpan.StartLinePosition;
				var endLinePosition = lineSpan.EndLinePosition;
				builder.Append ($".WithSpan({pathString}{linePosition.Line + 1}, {linePosition.Character + 1}, {endLinePosition.Line + 1}, {endLinePosition.Character + 1})");
			}
		}

		/// <summary>
		/// Match actual diagnostics with expected diagnostics.
		/// </summary>
		/// <remarks>
		/// <para>While each actual diagnostic contains complete information about the diagnostic (location, severity,
		/// message, etc.), the expected diagnostics sometimes contain partial information. It is therefore possible for
		/// an expected diagnostic to match more than one actual diagnostic, while another expected diagnostic with more
		/// complete information only matches a single specific actual diagnostic.</para>
		///
		/// <para>This method attempts to find a best matching of actual and expected diagnostics.</para>
		/// </remarks>
		/// <param name="actualResults">The actual diagnostics reported by analysis.</param>
		/// <param name="expectedResults">The expected diagnostics.</param>
		/// <returns>
		/// <para>A collection of matched diagnostics, with the following characteristics:</para>
		///
		/// <list type="bullet">
		/// <item><description>Every element of <paramref name="actualResults"/> will appear exactly once as the first element of an item in the result.</description></item>
		/// <item><description>Every element of <paramref name="expectedResults"/> will appear exactly once as the second element of an item in the result.</description></item>
		/// <item><description>An item in the result which specifies both a <see cref="Diagnostic"/> and a <see cref="DiagnosticResult"/> indicates a matched pair, i.e. the actual and expected results are believed to refer to the same diagnostic.</description></item>
		/// <item><description>An item in the result which specifies only a <see cref="Diagnostic"/> indicates an actual diagnostic for which no matching expected diagnostic was found.</description></item>
		/// <item><description>An item in the result which specifies only a <see cref="DiagnosticResult"/> indicates an expected diagnostic for which no matching actual diagnostic was found.</description></item>
		///
		/// <para>If no exact match is found (all actual diagnostics are matched to an expected diagnostic without
		/// errors), this method is <em>allowed</em> to attempt fall-back matching using a strategy intended to minimize
		/// the total number of mismatched pairs.</para>
		/// </list>
		/// </returns>
		private static ImmutableArray<(Diagnostic? actual, DiagnosticResult? expected)> MatchDiagnostics (Diagnostic[] actualResults, DiagnosticResult[] expectedResults)
		{
			var actualIds = actualResults.Select (result => result.Id).ToImmutableArray ();
			var actualResultLocations = actualResults.Select (result => (location: result.Location.GetLineSpan (), additionalLocations: result.AdditionalLocations.Select (location => location.GetLineSpan ()).ToImmutableArray ())).ToImmutableArray ();
			var actualArguments = actualResults.Select (actual => GetArguments (actual).Select (argument => argument?.ToString () ?? string.Empty).ToImmutableArray ()).ToImmutableArray ();

			expectedResults = expectedResults.ToOrderedArray ();
			var expectedArguments = expectedResults.Select (expected => expected.MessageArguments?.Select (argument => argument?.ToString () ?? string.Empty).ToImmutableArray () ?? ImmutableArray<string>.Empty).ToImmutableArray ();

			// Initialize the best match to a trivial result where everything is unmatched. This will be updated if/when
			// better matches are found.
			var bestMatchCount = MatchQuality.RemainingUnmatched (actualResults.Length + expectedResults.Length);
			var bestMatch = actualResults.Select (result => ((Diagnostic?) result, default (DiagnosticResult?))).Concat (expectedResults.Select (result => (default (Diagnostic?), (DiagnosticResult?) result))).ToImmutableArray ();

			var builder = ImmutableArray.CreateBuilder<(Diagnostic? actual, DiagnosticResult? expected)> ();
			var usedExpected = new bool[expectedResults.Length];

			// The recursive match algorithm is not optimized, so use a timeout to ensure it completes in a reasonable
			// time if a correct match isn't found.
			using var cancellationTokenSource = new CancellationTokenSource (MatchDiagnosticsTimeout);

			try {
				_ = RecursiveMatch (0, actualResults.Length, 0, expectedArguments.Length, MatchQuality.Full, usedExpected);
			} catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested) {
				// Continue with the best match we have
			}

			return bestMatch;

			// Match items using recursive backtracking. Returns the distance the best match under this path is from an
			// ideal result of 0 (1:1 matching of actual and expected results). Currently the distance is calculated as
			// the sum of the match values:
			//
			// * Fully-matched items have a value of MatchQuality.Full.
			// * Partially-matched items have a value between MatchQuality.Full and MatchQuality.None (exclusive).
			// * Fully-unmatched items have a value of MatchQuality.None.
			MatchQuality RecursiveMatch (int firstActualIndex, int remainingActualItems, int firstExpectedIndex, int remainingExpectedItems, MatchQuality unmatchedActualResults, bool[] usedExpected)
			{
				var matchedOnEntry = actualResults.Length - remainingActualItems;
				var bestPossibleUnmatchedExpected = MatchQuality.RemainingUnmatched (Math.Abs (remainingActualItems - remainingExpectedItems));
				var bestPossible = unmatchedActualResults + bestPossibleUnmatchedExpected;

				if (firstActualIndex == actualResults.Length) {
					// We reached the end of the actual diagnostics. Any remaning unmatched expected diagnostics should
					// be added to the end. If this path produced a better result than the best known path so far,
					// update the best match to this one.
					var totalUnmatched = unmatchedActualResults + MatchQuality.RemainingUnmatched (remainingExpectedItems);

					// Avoid manipulating the builder if we know the current path is no better than the previous best.
					if (totalUnmatched < bestMatchCount) {
						var addedCount = 0;

						// Add the remaining unmatched expected diagnostics
						for (var i = firstExpectedIndex; i < expectedResults.Length; i++) {
							if (!usedExpected[i]) {
								addedCount++;
								builder.Add ((null, (DiagnosticResult?) expectedResults[i]));
							}
						}

						bestMatchCount = totalUnmatched;
						bestMatch = builder.ToImmutable ();

						for (var i = 0; i < addedCount; i++) {
							builder.RemoveAt (builder.Count - 1);
						}
					}

					return totalUnmatched;
				}

				cancellationTokenSource.Token.ThrowIfCancellationRequested ();

				var currentBest = unmatchedActualResults + MatchQuality.RemainingUnmatched (remainingActualItems + remainingExpectedItems);
				for (var i = firstExpectedIndex; i < expectedResults.Length; i++) {
					if (usedExpected[i]) {
						continue;
					}

					var (lineSpan, additionalLineSpans) = actualResultLocations[firstActualIndex];
					var matchValue = GetMatchValue (actualResults[firstActualIndex], actualIds[firstActualIndex], lineSpan, additionalLineSpans, actualArguments[firstActualIndex], expectedResults[i], expectedArguments[i]);
					if (matchValue == MatchQuality.None) {
						continue;
					}

					try {
						usedExpected[i] = true;
						builder.Add ((actualResults[firstActualIndex], expectedResults[i]));
						var bestResultWithCurrentMatch = RecursiveMatch (firstActualIndex + 1, remainingActualItems - 1, i == firstExpectedIndex ? firstExpectedIndex + 1 : firstExpectedIndex, remainingExpectedItems - 1, unmatchedActualResults + matchValue, usedExpected);
						currentBest = Min (bestResultWithCurrentMatch, currentBest);
						if (currentBest == bestPossible) {
							// Return immediately if we know the current actual result cannot be paired with a different
							// expected result to produce a better match.
							return bestPossible;
						}
					} finally {
						usedExpected[i] = false;
						builder.RemoveAt (builder.Count - 1);
					}
				}

				if (currentBest > unmatchedActualResults) {
					// We might be able to improve the results by leaving the current actual diagnostic unmatched
					try {
						builder.Add ((actualResults[firstActualIndex], null));
						var bestResultWithCurrentUnmatched = RecursiveMatch (firstActualIndex + 1, remainingActualItems - 1, firstExpectedIndex, remainingExpectedItems, unmatchedActualResults + MatchQuality.None, usedExpected);
						return Min (bestResultWithCurrentUnmatched, currentBest);
					} finally {
						builder.RemoveAt (builder.Count - 1);
					}
				}

				Debug.Assert (currentBest == unmatchedActualResults, $"Assertion failure: {currentBest} == {unmatchedActualResults}");
				return currentBest;
			}

			static MatchQuality Min (MatchQuality val1, MatchQuality val2)
				=> val2 < val1 ? val2 : val1;

			static MatchQuality GetMatchValue (Diagnostic diagnostic, string diagnosticId, FileLinePositionSpan lineSpan, ImmutableArray<FileLinePositionSpan> additionalLineSpans, ImmutableArray<string> actualArguments, DiagnosticResult diagnosticResult, ImmutableArray<string> expectedArguments)
			{
				// A full match automatically gets the value MatchQuality.Full. A partial match gets a "point" for each
				// of the following elements:
				//
				// 1. Diagnostic span start
				// 2. Diagnostic span end
				// 3. Diagnostic ID
				//
				// A partial match starts at MatchQuality.None, with a point deduction for each of the above matching
				// items.
				var isLocationMatch = IsLocationMatch (diagnostic, lineSpan, additionalLineSpans, diagnosticResult, out var matchSpanStart, out var matchSpanEnd);
				var isIdMatch = diagnosticId == diagnosticResult.Id;
				if (isLocationMatch
					&& isIdMatch
					&& IsSeverityMatch (diagnostic, diagnosticResult)
					&& IsMessageMatch (diagnostic, actualArguments, diagnosticResult, expectedArguments)) {
					return MatchQuality.Full;
				}

				var points = (matchSpanStart ? 1 : 0) + (matchSpanEnd ? 1 : 0) + (isIdMatch ? 1 : 0);
				if (points == 0) {
					return MatchQuality.None;
				}

				return new MatchQuality (4 - points);
			}

			static bool IsLocationMatch (Diagnostic diagnostic, FileLinePositionSpan lineSpan, ImmutableArray<FileLinePositionSpan> additionalLineSpans, DiagnosticResult diagnosticResult, out bool matchSpanStart, out bool matchSpanEnd)
			{
				if (!diagnosticResult.HasLocation) {
					matchSpanStart = false;
					matchSpanEnd = false;
					return Equals (Location.None, diagnostic.Location);
				} else {
					if (!IsLocationMatch2 (diagnostic.Location, lineSpan, diagnosticResult.Spans[0], out matchSpanStart, out matchSpanEnd)) {
						return false;
					}

					if (diagnosticResult.Options.HasFlag (DiagnosticOptions.IgnoreAdditionalLocations)) {
						return true;
					}

					var additionalLocations = diagnostic.AdditionalLocations.ToArray ();
					if (additionalLocations.Length != diagnosticResult.Spans.Length - 1) {
						// Number of additional locations does not match expected result
						return false;
					}

					for (var i = 0; i < additionalLocations.Length; i++) {
						if (!IsLocationMatch2 (additionalLocations[i], additionalLineSpans[i], diagnosticResult.Spans[i + 1], out _, out _)) {
							return false;
						}
					}

					return true;
				}
			}

			static bool IsLocationMatch2 (Location actual, FileLinePositionSpan actualSpan, DiagnosticLocation expected, out bool matchSpanStart, out bool matchSpanEnd)
			{
				matchSpanStart = actualSpan.StartLinePosition == expected.Span.StartLinePosition;
				matchSpanEnd = expected.Options.HasFlag (DiagnosticLocationOptions.IgnoreLength)
					|| actualSpan.EndLinePosition == expected.Span.EndLinePosition;

				var assert = actualSpan.Path == expected.Span.Path || (actualSpan.Path?.Contains ("Test0.") == true && expected.Span.Path.Contains ("Test."));
				if (!assert) {
					// Expected diagnostic to be in file "{expected.Span.Path}" was actually in file "{actualSpan.Path}"
					return false;
				}

				if (!matchSpanStart || !matchSpanEnd) {
					return false;
				}

				return true;
			}

			static bool IsSeverityMatch (Diagnostic actual, DiagnosticResult expected)
			{
				if (expected.Options.HasFlag (DiagnosticOptions.IgnoreSeverity)) {
					return true;
				}

				return actual.Severity == expected.Severity;
			}

			static bool IsMessageMatch (Diagnostic actual, ImmutableArray<string> actualArguments, DiagnosticResult expected, ImmutableArray<string> expectedArguments)
			{
				if (expected.Message is null) {
					if (expected.MessageArguments?.Length > 0) {
						return actualArguments.SequenceEqual (expectedArguments);
					}

					return true;
				}

				return string.Equals (expected.Message, actual.GetMessage ());
			}
		}
		private static string GetDiagnosticIdArgumentString (string diagnosticId) => $"DiagnosticId.{(DiagnosticId) Int32.Parse (diagnosticId.Substring (2))}";

		internal readonly struct MatchQuality : IComparable<MatchQuality>, IEquatable<MatchQuality>
		{
			public static readonly MatchQuality Full = new MatchQuality (0);
			public static readonly MatchQuality None = new MatchQuality (4);

			private readonly int _value;

			public MatchQuality (int value)
			{
				ArgumentOutOfRangeException.ThrowIfNegative (value);

				_value = value;
			}

			public static MatchQuality operator + (MatchQuality left, MatchQuality right)
				=> new MatchQuality (left._value + right._value);

			public static MatchQuality operator - (MatchQuality left, MatchQuality right)
				=> new MatchQuality (left._value - right._value);

			public static bool operator == (MatchQuality left, MatchQuality right)
				=> left.Equals (right);

			public static bool operator != (MatchQuality left, MatchQuality right)
				=> !left.Equals (right);

			public static bool operator < (MatchQuality left, MatchQuality right)
				=> left._value < right._value;

			public static bool operator <= (MatchQuality left, MatchQuality right)
				=> left._value <= right._value;

			public static bool operator > (MatchQuality left, MatchQuality right)
				=> left._value > right._value;

			public static bool operator >= (MatchQuality left, MatchQuality right)
				=> left._value >= right._value;

			public static MatchQuality RemainingUnmatched (int count)
				=> new MatchQuality (None._value * count);

			public int CompareTo (MatchQuality other)
				=> _value.CompareTo (other._value);

			public override bool Equals (object? obj)
				=> obj is MatchQuality quality && Equals (quality);

			public bool Equals (MatchQuality other)
				=> _value == other._value;

			public override int GetHashCode ()
				=> _value;
		}

		internal static IReadOnlyList<object?> GetArguments (Diagnostic diagnostic)
		{
			return (IReadOnlyList<object?>?) diagnostic.GetType ().GetProperty ("Arguments", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue (diagnostic)
				?? Array.Empty<object> ();
		}
	}

	internal static class IEnumerableExtensions
	{
		private static readonly Func<object?, bool> s_notNullTest = x => x is object;

		public static DiagnosticResult[] ToOrderedArray (this IEnumerable<DiagnosticResult> diagnosticResults)
		{
			return diagnosticResults
				.OrderBy (diagnosticResult => diagnosticResult.Spans.FirstOrDefault ().Span.Path, StringComparer.Ordinal)
				.ThenBy (diagnosticResult => diagnosticResult.Spans.FirstOrDefault ().Span.Span.Start.Line)
				.ThenBy (diagnosticResult => diagnosticResult.Spans.FirstOrDefault ().Span.Span.Start.Character)
				.ThenBy (diagnosticResult => diagnosticResult.Spans.FirstOrDefault ().Span.Span.End.Line)
				.ThenBy (diagnosticResult => diagnosticResult.Spans.FirstOrDefault ().Span.Span.End.Character)
				.ThenBy (diagnosticResult => diagnosticResult.Id, StringComparer.Ordinal)
				.ToArray ();
		}

		internal static IEnumerable<T> WhereNotNull<T> (this IEnumerable<T?> source)
			where T : class
		{
			return source.Where<T?> (s_notNullTest)!;
		}

		public static T? SingleOrNull<T> (this IEnumerable<T> source)
			where T : struct
		{
			return source.Select (value => (T?) value).SingleOrDefault ();
		}
	}
}
