// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
	ILLink.RoslynAnalyzer.RequiresDynamicCodeAnalyzer,
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
				FixedCode = fixedSource + dynamicCodeAttribute,
				ReferenceAssemblies = TestCaseUtils.Net6PreviewAssemblies
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
			var test = @"
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresDynamicCodeAttribute(""message"")]
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
";

			var fixtest = @"
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresDynamicCodeAttribute(""message"")]
    public int M1() => 0;

    [RequiresDynamicCode(""Calls C.M1()"")]
    int M2() => M1();
}
class D
{
    [RequiresDynamicCode(""Calls C.M1()"")]
    public int M3(C c) => c.M1();

    public class E
    {
        [RequiresDynamicCode(""Calls C.M1()"")]
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
";

			await VerifyRequiresDynamicCodeCodeFix (test, fixtest, new[] {
				// /0/Test0.cs(9,17): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(9, 17, 9, 21).WithArguments("C.M1()", " message.", ""),
				// /0/Test0.cs(13,27): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(13, 27, 13, 33).WithArguments("C.M1()", " message.", ""),
				// /0/Test0.cs(17,31): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(17, 31, 17, 37).WithArguments("C.M1()", " message.", ""),
				// /0/Test0.cs(24,31): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(24, 31, 24, 37).WithArguments("C.M1()", " message.", "")
			}, new[] {
				// /0/Test0.cs(27,10): error CS7036: There is no argument given that corresponds to the required formal parameter 'message' of 'RequiresDynamicCodeAttribute.RequiresDynamicCodeAttribute(string)'
				DiagnosticResult.CompilerError("CS7036").WithSpan(27, 10, 27, 31).WithArguments("message", "System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute.RequiresDynamicCodeAttribute(string)"),
			}
	);
		}


		[Fact]
		public Task FixInLambda ()
		{
			var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresDynamicCodeAttribute(""message"")]
    public int M1() => 0;

    Action M2()
    {
        return () => M1();
    }
}";
			var fix = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresDynamicCodeAttribute(""message"")]
    public int M1() => 0;

    Action M2()
    {
        return () => M1();
    }
}";
			// No fix available inside a lambda, requries manual code change since attribute cannot
			// be applied
			return VerifyRequiresDynamicCodeCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(12,22): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(12, 22, 12, 26).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public Task FixInLocalFunc ()
		{
			var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresDynamicCodeAttribute(""message"")]
    public int M1() => 0;

    Action M2()
    {
        void Wrapper () => M1();
        return Wrapper;
    }
}";
			var fix = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresDynamicCodeAttribute(""message"")]
    public int M1() => 0;

    [RequiresDynamicCode(""Calls Wrapper()"")]
    Action M2()
    {
        [global::System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute(""Calls C.M1()"")] void Wrapper () => M1();
        return Wrapper;
    }
}";
			// Roslyn currently doesn't simplify the attribute name properly, see https://github.com/dotnet/roslyn/issues/52039
			return VerifyRequiresDynamicCodeCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(12,28): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(12, 28, 12, 32).WithArguments("C.M1()", " message.", "")
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
			var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresDynamicCodeAttribute(""message"")]
    public static int M1() => 0;

    public C() => M1();
}";
			var fix = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresDynamicCodeAttribute(""message"")]
    public static int M1() => 0;

    [RequiresDynamicCode()]
    public C() => M1();
}";
			// Roslyn currently doesn't simplify the attribute name properly, see https://github.com/dotnet/roslyn/issues/52039
			return VerifyRequiresDynamicCodeCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(10,19): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(10, 19, 10, 23).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(10,6): error CS7036: There is no argument given that corresponds to the required formal parameter 'message' of 'RequiresDynamicCodeAttribute.RequiresDynamicCodeAttribute(string)'
					DiagnosticResult.CompilerError("CS7036").WithSpan(10, 6, 10, 27).WithArguments("message", "System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute.RequiresDynamicCodeAttribute(string)")
				});
		}

		[Fact]
		public Task FixInPropertyDecl ()
		{
			var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresDynamicCodeAttribute(""message"")]
    public int M1() => 0;

    int M2 => M1();
}";
			var fix = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresDynamicCodeAttribute(""message"")]
    public int M1() => 0;

    int M2 => M1();
}";
			// Can't apply RDC on properties at the moment
			return VerifyRequiresDynamicCodeCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(10,15): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(10, 15, 10, 19).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(10,15): warning IL3050: Using member 'C.M1()' which has 'RequiresDynamicCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresDynamicCode).WithSpan(10, 15, 10, 19).WithArguments("C.M1()", " message.", "")
				});
		}
	}
}
