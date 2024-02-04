// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
	ILLink.RoslynAnalyzer.DynamicallyAccessedMembersAnalyzer,
	ILLink.CodeFix.RequiresDynamicCodeCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class RequiresDynamicCodeAnalyzerTests
	{
		static readonly string dynamicCodeAttribute = @"
#nullable enable

namespace System.Diagnostics.CodeAnalysis
{
	[AttributeUsage (AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
	public sealed class RequiresDynamicCodeAttribute : Attribute
	{
		public RequiresDynamicCodeAttribute (string message)
		{
			Message = message;
		}

		public string Message { get; }

		public string? Url { get; set; }
	}
}";

		static Task VerifyRequiresDynamicCodeCodeFix (
			string source,
			string fixedSource,
			DiagnosticResult[] baselineExpected,
			DiagnosticResult[] fixedExpected,
			int? numberOfIterations = null)
		{
			var test = new VerifyCS.Test {
				TestCode = source + dynamicCodeAttribute,
				FixedCode = fixedSource + dynamicCodeAttribute
			};
			test.ExpectedDiagnostics.AddRange (baselineExpected);
			test.TestState.AnalyzerConfigFiles.Add (
						("/.editorconfig", SourceText.From (@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableAotAnalyzer} = true")));
			if (numberOfIterations != null) {
				test.NumberOfIncrementalIterations = numberOfIterations;
				test.NumberOfFixAllIterations = numberOfIterations;
			}
			test.FixedState.ExpectedDiagnostics.AddRange (fixedExpected);
			return test.RunAsync ();
		}

		[Fact]
		public async Task SimpleDiagnosticFix ()
		{
			var test = $$"""
			using System.Diagnostics.CodeAnalysis;

			public class C
			{
				[RequiresDynamicCodeAttribute("message")]
				public int M1() => 0;

				int M2() => M1();
			}
			class D
			{
				public int M3(C c) => c.M1();

				public class E
				{
					public int M4(C c) => c.M1();
				}
			}
			public class E
			{
				public class F
				{
					public int M5(C c) => c.M1();
				}
			}
			""";

			var fixtest = $$"""
			using System.Diagnostics.CodeAnalysis;

			public class C
			{
				[RequiresDynamicCodeAttribute("message")]
				public int M1() => 0;

			    [RequiresDynamicCode("Calls C.M1()")]
			    int M2() => M1();
			}
			class D
			{
			    [RequiresDynamicCode("Calls C.M1()")]
			    public int M3(C c) => c.M1();

				public class E
				{
			        [RequiresDynamicCode("Calls C.M1()")]
			        public int M4(C c) => c.M1();
				}
			}
			public class E
			{
				public class F
				{
			        [RequiresDynamicCode()]
			        public int M5(C c) => c.M1();
				}
			}
			""";

			await VerifyRequiresDynamicCodeCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(8,14): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when AOT compiling. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(8, 14, 8, 18).WithArguments("C.M1()", " message.", ""),
					// /0/Test0.cs(12,24): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when AOT compiling. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(12, 24, 12, 30).WithArguments("C.M1()", " message.", ""),
					// /0/Test0.cs(16,25): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when AOT compiling. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(16, 25, 16, 31).WithArguments("C.M1()", " message.", ""),
					// /0/Test0.cs(23,25): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when AOT compiling. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(23, 25, 23, 31).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: new[] {
				// /0/Test0.cs(26,10): error CS7036: There is no argument given that corresponds to the required formal parameter 'message' of 'RequiresDynamicCodeAttribute.RequiresDynamicCodeAttribute(string)'
				DiagnosticResult.CompilerError("CS7036").WithSpan(26, 10, 26, 31).WithArguments("message", "System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute.RequiresDynamicCodeAttribute(string)"),
				});
		}


		[Fact]
		public Task FixInLambda ()
		{
			var src = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class C
			{
				[RequiresDynamicCodeAttribute("message")]
				public int M1() => 0;

				Action M2()
				{
					return () => M1();
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(11,16): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(11, 16, 11, 20).WithArguments("C.M1()", " message.", "")
			};
			// No fix available inside a lambda, requires manual code change since attribute cannot
			// be applied
			return VerifyRequiresDynamicCodeCodeFix (src, src, diag, diag);
		}

		[Fact]
		public Task FixInLocalFunc ()
		{
			var src = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class C
			{
				[RequiresDynamicCodeAttribute("message")]
				public int M1() => 0;

				Action M2()
				{
					void Wrapper () => M1();
					return Wrapper;
				}
			}
			""";
			var fix = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class C
			{
				[RequiresDynamicCodeAttribute("message")]
				public int M1() => 0;

			    [RequiresDynamicCode("Calls Wrapper()")]
			    Action M2()
				{
			        [RequiresDynamicCode("Calls C.M1()")] void Wrapper () => M1();
					return Wrapper;
				}
			}
			""";
			// Roslyn currently doesn't simplify the attribute name properly, see https://github.com/dotnet/roslyn/issues/52039
			return VerifyRequiresDynamicCodeCodeFix (
				source: src,
				fixedSource: fix,
				baselineExpected: new[] {
					// /0/Test0.cs(11,22): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(11, 22, 11, 26).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> (),
				// The default iterations for the codefix is the number of diagnostics (1 in this case)
				// but since the codefixer introduces a new diagnostic in the first iteration, it needs
				// to run twice, so we need to set the number of iterations to 2.
				numberOfIterations: 2);
		}

		[Fact]
		public Task FixInCtor ()
		{
			var src = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class C
			{
				[RequiresDynamicCodeAttribute("message")]
				public static int M1() => 0;

				public C() => M1();
			}
			""";
			var fix = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class C
			{
				[RequiresDynamicCodeAttribute("message")]
				public static int M1() => 0;

			    [RequiresDynamicCode()]
			    public C() => M1();
			}
			""";
			// Roslyn currently doesn't simplify the attribute name properly, see https://github.com/dotnet/roslyn/issues/52039
			return VerifyRequiresDynamicCodeCodeFix (
				source: src,
				fixedSource: fix,
				baselineExpected: new[] {
					// /0/Test0.cs(9,16): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(9, 16, 9, 20).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(9,6): error CS7036: There is no argument given that corresponds to the required formal parameter 'message' of 'RequiresDynamicCodeAttribute.RequiresDynamicCodeAttribute(string)'
					DiagnosticResult.CompilerError("CS7036").WithSpan(9, 6, 9, 27).WithArguments("message", "System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute.RequiresDynamicCodeAttribute(string)")
				});
		}

		[Fact]
		public Task FixInPropertyDecl ()
		{
			var src = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class C
			{
				[RequiresDynamicCodeAttribute("message")]
				public int M1() => 0;

				int M2 => M1();
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(9,12): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(9, 12, 9, 16).WithArguments("C.M1()", " message.", "")
			};
			// Can't apply RDC on properties at the moment
			return VerifyRequiresDynamicCodeCodeFix (src, src, diag, diag);
		}

		[Fact]
		public Task FixInPropertyAccessor ()
		{
			var src = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class C
			{
				[RequiresDynamicCodeAttribute("message")]
				public int M1() => 0;

				public int field;

				private int M2 {
					get { return M1(); }
					set { field = M1(); }
				}
			}
			""";
			var fix = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class C
			{
				[RequiresDynamicCodeAttribute("message")]
				public int M1() => 0;

				public int field;

				private int M2 {
			        [RequiresDynamicCode("Calls C.M1()")]
			        get { return M1(); }

			        [RequiresDynamicCode("Calls C.M1()")]
			        set { field = M1(); }
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(12,16): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(12, 16, 12, 20).WithArguments("C.M1()", " message.", ""),
				// /0/Test0.cs(13,17): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(13, 17, 13, 21).WithArguments("C.M1()", " message.", "")
			};
			return VerifyRequiresDynamicCodeCodeFix (src, fix, diag, Array.Empty<DiagnosticResult> ());
		}
	}
}
