// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
	ILLink.RoslynAnalyzer.DynamicallyAccessedMembersAnalyzer,
	ILLink.CodeFix.RequiresUnreferencedCodeCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class RequiresUnreferencedCodeAnalyzerTests
	{
		static readonly DiagnosticDescriptor dynamicInvocationDiagnosticDescriptor = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCode, new DiagnosticString ("DynamicTypeInvocation"));

		static Task VerifyRequiresUnreferencedCodeAnalyzer (string source, params DiagnosticResult[] expected) =>
			VerifyRequiresUnreferencedCodeAnalyzer (source, null, expected);

		static async Task VerifyRequiresUnreferencedCodeAnalyzer (string source, IEnumerable<MetadataReference>? additionalReferences, params DiagnosticResult[] expected) =>
			await VerifyCS.VerifyAnalyzerAsync (
				source,
				consoleApplication: false,
				TestCaseUtils.UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer),
				additionalReferences ?? Array.Empty<MetadataReference> (),
				expected);

		static Task VerifyRequiresUnreferencedCodeCodeFix (
			string source,
			string fixedSource,
			DiagnosticResult[] baselineExpected,
			DiagnosticResult[] fixedExpected,
			int? numberOfIterations = null)
		{
			var test = new VerifyCS.Test {
				TestCode = source,
				FixedCode = fixedSource
			};
			test.ExpectedDiagnostics.AddRange (baselineExpected);
			test.TestState.AnalyzerConfigFiles.Add (
						("/.editorconfig", SourceText.From (@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableTrimAnalyzer} = true")));
			if (numberOfIterations != null) {
				test.NumberOfIncrementalIterations = numberOfIterations;
				test.NumberOfFixAllIterations = numberOfIterations;
			}
			test.FixedState.ExpectedDiagnostics.AddRange (fixedExpected);
			return test.RunAsync ();
		}


		[Fact]
		public async Task WarningInArgument ()
		{
			var test = $$"""
			using System.Diagnostics.CodeAnalysis;
			public class C
			{
				[RequiresUnreferencedCode("message")]
				public int M1() => 0;
				public void M2(int x)
				{
				}
				public void M3() => M2(M1());
			}
			""";
			var fixtest = $$"""
			using System.Diagnostics.CodeAnalysis;
			public class C
			{
				[RequiresUnreferencedCode("message")]
				public int M1() => 0;
				public void M2(int x)
				{
				}

			    [RequiresUnreferencedCode()]
			    public void M3() => M2(M1());
			}
			""";
			await VerifyRequiresUnreferencedCodeCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(9,25): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan(9, 25, 9, 29).WithArguments("C.M1()", " message.", ""),
				},
				fixedExpected: new[] {
					// /0/Test0.cs(10,3): error CS7036: There is no argument given that corresponds to the required formal parameter 'message' of 'RequiresUnreferencedCodeAttribute.RequiresUnreferencedCodeAttribute(string)'
					DiagnosticResult.CompilerError("CS7036").WithSpan(10, 6, 10, 32).WithArguments("message", "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute.RequiresUnreferencedCodeAttribute(string)"),
				});
		}

		[Fact]
		public async Task SimpleDiagnosticFix ()
		{
			var test = $$"""
			using System.Diagnostics.CodeAnalysis;

			public class C
			{
				[RequiresUnreferencedCodeAttribute("message")]
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
				[RequiresUnreferencedCodeAttribute("message")]
				public int M1() => 0;

			    [RequiresUnreferencedCode("Calls C.M1()")]
			    int M2() => M1();
			}
			class D
			{
			    [RequiresUnreferencedCode("Calls C.M1()")]
			    public int M3(C c) => c.M1();

				public class E
				{
			        [RequiresUnreferencedCode("Calls C.M1()")]
			        public int M4(C c) => c.M1();
				}
			}
			public class E
			{
				public class F
				{
			        [RequiresUnreferencedCode()]
			        public int M5(C c) => c.M1();
				}
			}
			""";

			await VerifyRequiresUnreferencedCodeCodeFix (
				source: test,
				fixedSource: fixtest,
				baselineExpected: new[] {
					// /0/Test0.cs(8,14): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic (DiagnosticId.RequiresUnreferencedCode).WithSpan (8, 14, 8, 18).WithArguments ("C.M1()", " message.", ""),
					// /0/Test0.cs(12,24): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan (12, 24, 12, 30).WithArguments("C.M1()", " message.", ""),
					// /0/Test0.cs(16,25): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic (DiagnosticId.RequiresUnreferencedCode).WithSpan (16, 25, 16, 31).WithArguments ("C.M1()", " message.", ""),
					// /0/Test0.cs(23,25): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic (DiagnosticId.RequiresUnreferencedCode).WithSpan (23, 25, 23, 31).WithArguments ("C.M1()", " message.", "")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(26,10): error CS7036: There is no argument given that corresponds to the required formal parameter 'message' of 'RequiresUnreferencedCodeAttribute.RequiresUnreferencedCodeAttribute(string)'
					DiagnosticResult.CompilerError("CS7036").WithSpan(26, 10, 26, 36).WithArguments("message", "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute.RequiresUnreferencedCodeAttribute(string)"),
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
				[RequiresUnreferencedCodeAttribute("message")]
				public int M1() => 0;

				Action M2()
				{
					return () => M1();
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(11,16): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan(11, 16, 11, 20).WithArguments("C.M1()", " message.", "")
			};
			// No fix available inside a lambda, requires manual code change since attribute cannot
			// be applied
			return VerifyRequiresUnreferencedCodeCodeFix (src, src, diag, diag);
		}

		[Fact]
		public Task FixInLocalFunc ()
		{
			var src = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class C
			{
				[RequiresUnreferencedCodeAttribute("message")]
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
				[RequiresUnreferencedCodeAttribute("message")]
				public int M1() => 0;

			    [RequiresUnreferencedCode("Calls Wrapper()")]
			    Action M2()
				{
			        [RequiresUnreferencedCode("Calls C.M1()")] void Wrapper () => M1();
					return Wrapper;
				}
			}
			""";
			// Roslyn currently doesn't simplify the attribute name properly, see https://github.com/dotnet/roslyn/issues/52039
			return VerifyRequiresUnreferencedCodeCodeFix (
				source: src,
				fixedSource: fix,
				baselineExpected: new[] {
					// /0/Test0.cs(11,22): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan(11, 22, 11, 26).WithArguments("C.M1()", " message.", "")
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
				[RequiresUnreferencedCodeAttribute("message")]
				public static int M1() => 0;

				public C() => M1();
			}
			""";
			var fix = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class C
			{
				[RequiresUnreferencedCodeAttribute("message")]
				public static int M1() => 0;

			    [RequiresUnreferencedCode()]
			    public C() => M1();
			}
			""";
			// Roslyn currently doesn't simplify the attribute name properly, see https://github.com/dotnet/roslyn/issues/52039
			return VerifyRequiresUnreferencedCodeCodeFix (
				source: src,
				fixedSource: fix,
				baselineExpected: new[] {
					// /0/Test0.cs(9,16): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan(9, 16, 9, 20).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(9,3): error CS7036: There is no argument given that corresponds to the required formal parameter 'message' of 'RequiresUnreferencedCodeAttribute.RequiresUnreferencedCodeAttribute(string)'
					DiagnosticResult.CompilerError ("CS7036").WithSpan (9, 6, 9, 32).WithArguments ("message", "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute.RequiresUnreferencedCodeAttribute(string)")
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
				[RequiresUnreferencedCodeAttribute("message")]
				public int M1() => 0;

				int M2 => M1();
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(10,15): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan(9, 12, 9, 16).WithArguments("C.M1()", " message.", "")
			};
			// Can't apply RUC on properties at the moment
			return VerifyRequiresUnreferencedCodeCodeFix (src, src, diag, diag);
		}

		[Fact]
		public Task FixInPropertyAccessor ()
		{
			var src = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class C
			{
				[RequiresUnreferencedCodeAttribute("message")]
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
				[RequiresUnreferencedCodeAttribute("message")]
				public int M1() => 0;

				public int field;

				private int M2 {
			        [RequiresUnreferencedCode("Calls C.M1()")]
			        get { return M1(); }

			        [RequiresUnreferencedCode("Calls C.M1()")]
			        set { field = M1(); }
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(12,16): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan(12, 16, 12, 20).WithArguments("C.M1()", " message.", ""),
				// /0/Test0.cs(13,17): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan(13, 17, 13, 21).WithArguments("C.M1()", " message.", "")
			};
			return VerifyRequiresUnreferencedCodeCodeFix (src, fix, diag, Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public Task TestMakeGenericMethodUsage ()
		{
			var source = $$"""
			using System.Diagnostics.CodeAnalysis;
			using System.Reflection;

			class C
			{
				static void M1 (MethodInfo methodInfo)
				{
					methodInfo.MakeGenericMethod (typeof (C));
				}

				[RequiresUnreferencedCode ("Message from RUC")]
				static void M2 (MethodInfo methodInfo)
				{
					methodInfo.MakeGenericMethod (typeof (C));
				}
			}
			""";

			return VerifyRequiresUnreferencedCodeAnalyzer (source,
				// (8,3): warning IL2060: Call to 'System.Reflection.MethodInfo.MakeGenericMethod(params Type[])' can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic method.
				VerifyCS.Diagnostic (DiagnosticId.MakeGenericMethod).WithSpan (8, 3, 8, 44).WithArguments ("System.Reflection.MethodInfo.MakeGenericMethod(params Type[])"));
		}

		[Fact]
		public Task TestMakeGenericTypeUsage ()
		{
			var source = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				static void M1 (Type t)
				{
					typeof (Nullable<>).MakeGenericType (typeof (C));
				}

				[RequiresUnreferencedCode ("Message from RUC")]
				static void M2 (Type t)
				{
					typeof (Nullable<>).MakeGenericType (typeof (C));
				}
			}
			""";

			return VerifyRequiresUnreferencedCodeAnalyzer (source);
		}

		[Fact]
		public Task VerifyThatAnalysisOfFieldsDoesNotNullRef ()
		{
			var source = $$"""
			using System.Diagnostics.CodeAnalysis;

			[DynamicallyAccessedMembers (field)]
			class C
			{
				public const DynamicallyAccessedMemberTypes field = DynamicallyAccessedMemberTypes.PublicMethods;
			}
			""";

			return VerifyRequiresUnreferencedCodeAnalyzer (source);
		}

		[Fact]
		public Task TestPropertyAssignmentInAssemblyAttribute ()
		{
			var source = $$"""
			using System;
			[assembly: MyAttribute (Value = 5)]

			class MyAttribute : Attribute
			{
				public int Value { get; set; }
			}
			""";
			return VerifyRequiresUnreferencedCodeAnalyzer (source);
		}
	}
}
