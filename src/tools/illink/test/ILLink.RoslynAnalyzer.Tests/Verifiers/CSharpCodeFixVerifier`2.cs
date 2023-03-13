// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace ILLink.RoslynAnalyzer.Tests
{
	/// <summary>
	/// A default verifier for diagnostic analyzers with code fixes.
	/// </summary>
	/// <typeparam name="TAnalyzer">The <see cref="DiagnosticAnalyzer"/> to test.</typeparam>
	/// <typeparam name="TCodeFix">The <see cref="CodeFixProvider"/> to test.</typeparam>
	/// <typeparam name="TTest">The test implementation to use.</typeparam>
	public partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
		   where TAnalyzer : DiagnosticAnalyzer, new()
		   where TCodeFix : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, new()
	{
		public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
		{
			public Test ()
			{
				SolutionTransforms.Add ((solution, projectId) => {
					var compilationOptions = solution.GetProject (projectId)!.CompilationOptions;
					compilationOptions = compilationOptions!.WithSpecificDiagnosticOptions (
						compilationOptions.SpecificDiagnosticOptions.SetItems (CSharpVerifierHelper.NullableWarnings));
					solution = solution.WithProjectCompilationOptions (projectId, compilationOptions);

					return solution;
				});
			}
		}

		/// <inheritdoc cref="AnalyzerVerifier{TAnalyzer}.Diagnostic()"/>
		public static DiagnosticResult Diagnostic ()
			=> CSharpAnalyzerVerifier<TAnalyzer>.Diagnostic ();

		/// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest}.Diagnostic(string)"/>
		public static DiagnosticResult Diagnostic (string diagnosticId)
			=> CSharpAnalyzerVerifier<TAnalyzer>.Diagnostic (diagnosticId);

		public static DiagnosticResult Diagnostic (DiagnosticId diagnosticId)
			=> CSharpAnalyzerVerifier<TAnalyzer>.Diagnostic (DiagnosticDescriptors.GetDiagnosticDescriptor (diagnosticId));

		/// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest}.Diagnostic(DiagnosticDescriptor)"/>
		public static DiagnosticResult Diagnostic (DiagnosticDescriptor descriptor)
			=> CSharpAnalyzerVerifier<TAnalyzer>.Diagnostic (descriptor);

		/// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
		public static Task VerifyAnalyzerAsync (string source, (string, string)[]? analyzerOptions = null, IEnumerable<MetadataReference>? additionalReferences = null, params DiagnosticResult[] expected)
			=> CSharpAnalyzerVerifier<TAnalyzer>.VerifyAnalyzerAsync (source, analyzerOptions, additionalReferences, expected);

		/// <summary>
		/// Verifies the analyzer provides diagnostics which, in combination with the code fix, produce the expected
		/// fixed code.
		/// </summary>
		/// <param name="source">The source text to test. Any diagnostics are defined in markup.</param>
		/// <param name="fixedSource">The expected fixed source text. Any remaining diagnostics are defined in markup.</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task VerifyCodeFixAsync (string source, string fixedSource)
			=> VerifyCodeFixAsync (source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

		/// <summary>
		/// Verifies the analyzer provides diagnostics which, in combination with the code fix, produce the expected
		/// fixed code.
		/// </summary>
		/// <param name="source">The source text to test, which may include markup syntax.</param>
		/// <param name="expected">The expected diagnostic. This diagnostic is in addition to any diagnostics defined in
		/// markup.</param>
		/// <param name="fixedSource">The expected fixed source text. Any remaining diagnostics are defined in markup.</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task VerifyCodeFixAsync (string source, DiagnosticResult expected, string fixedSource)
			=> VerifyCodeFixAsync (source, new[] { expected }, fixedSource);

		/// <summary>
		/// Verifies the analyzer provides diagnostics which, in combination with the code fix, produce the expected
		/// fixed code.
		/// </summary>
		/// <param name="source">The source text to test, which may include markup syntax.</param>
		/// <param name="expected">The expected diagnostics. These diagnostics are in addition to any diagnostics
		/// defined in markup.</param>
		/// <param name="fixedSource">The expected fixed source text. Any remaining diagnostics are defined in markup.</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		public static Task VerifyCodeFixAsync (string source, DiagnosticResult[] expected, string fixedSource)
		{
			var test = new Test {
				TestCode = source,
				FixedCode = fixedSource,
			};

			test.ExpectedDiagnostics.AddRange (expected);
			return test.RunAsync (CancellationToken.None);
		}
	}
}
