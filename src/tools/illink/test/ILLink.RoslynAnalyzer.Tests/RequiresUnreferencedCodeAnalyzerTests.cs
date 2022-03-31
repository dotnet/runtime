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
	ILLink.RoslynAnalyzer.RequiresUnreferencedCodeAnalyzer,
	ILLink.CodeFix.RequiresUnreferencedCodeCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class RequiresUnreferencedCodeAnalyzerTests
	{
		static readonly DiagnosticDescriptor dynamicInvocationDiagnosticDescriptor = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCode, new DiagnosticString ("DynamicTypeInvocation"));

		static Task VerifyRequiresUnreferencedCodeAnalyzer (string source, params DiagnosticResult[] expected) =>
			VerifyRequiresUnreferencedCodeAnalyzer (source, null, expected);

		static async Task VerifyRequiresUnreferencedCodeAnalyzer (string source, IEnumerable<MetadataReference>? additionalReferences, params DiagnosticResult[] expected) =>
			await VerifyCS.VerifyAnalyzerAsync (source,
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
				FixedCode = fixedSource,
				ReferenceAssemblies = TestCaseUtils.Net6PreviewAssemblies
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
		public async Task SimpleDiagnosticFix ()
		{
			var test = @"
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
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
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    [RequiresUnreferencedCode(""Calls C.M1()"")]
    int M2() => M1();
}
class D
{
    [RequiresUnreferencedCode(""Calls C.M1()"")]
    public int M3(C c) => c.M1();

    public class E
    {
        [RequiresUnreferencedCode(""Calls C.M1()"")]
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
";

			await VerifyRequiresUnreferencedCodeCodeFix (test, fixtest, new[] {
	// /0/Test0.cs(9,17): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
	VerifyCS.Diagnostic (DiagnosticId.RequiresUnreferencedCode).WithSpan (9, 17, 9, 21).WithArguments ("C.M1()", " message.", ""),
	// /0/Test0.cs(13,27): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
	VerifyCS.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan(13, 27, 13, 33).WithArguments("C.M1()", " message.", ""),
	// /0/Test0.cs(17,31): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
	VerifyCS.Diagnostic (DiagnosticId.RequiresUnreferencedCode).WithSpan (17, 31, 17, 37).WithArguments ("C.M1()", " message.", ""),
	// /0/Test0.cs(24,31): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
	VerifyCS.Diagnostic (DiagnosticId.RequiresUnreferencedCode).WithSpan (24, 31, 24, 37).WithArguments ("C.M1()", " message.", "")
			}, new[] {
	// /0/Test0.cs(27,10): error CS7036: There is no argument given that corresponds to the required formal parameter 'message' of 'RequiresUnreferencedCodeAttribute.RequiresUnreferencedCodeAttribute(string)'
	DiagnosticResult.CompilerError("CS7036").WithSpan(27, 10, 27, 36).WithArguments("message", "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute.RequiresUnreferencedCodeAttribute(string)"),
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
    [RequiresUnreferencedCodeAttribute(""message"")]
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
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    Action M2()
    {
        return () => M1();
    }
}";
			// No fix available inside a lambda, requries manual code change since attribute cannot
			// be applied
			return VerifyRequiresUnreferencedCodeCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(12,22): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan(12, 22, 12, 26).WithArguments("C.M1()", " message.", "")
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
    [RequiresUnreferencedCodeAttribute(""message"")]
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
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    [RequiresUnreferencedCode(""Calls Wrapper()"")]
    Action M2()
    {
        [global::System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute(""Calls C.M1()"")] void Wrapper () => M1();
        return Wrapper;
    }
}";
			// Roslyn currently doesn't simplify the attribute name properly, see https://github.com/dotnet/roslyn/issues/52039
			return VerifyRequiresUnreferencedCodeCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(12,28): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan(12, 28, 12, 32).WithArguments("C.M1()", " message.", "")
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
    [RequiresUnreferencedCodeAttribute(""message"")]
    public static int M1() => 0;

    public C() => M1();
}";
			var fix = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    public static int M1() => 0;

    [RequiresUnreferencedCode()]
    public C() => M1();
}";
			// Roslyn currently doesn't simplify the attribute name properly, see https://github.com/dotnet/roslyn/issues/52039
			return VerifyRequiresUnreferencedCodeCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(10,19): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan(10, 19, 10, 23).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(10,6): error CS7036: There is no argument given that corresponds to the required formal parameter 'message' of 'RequiresUnreferencedCodeAttribute.RequiresUnreferencedCodeAttribute(string)'
					DiagnosticResult.CompilerError("CS7036").WithSpan(10, 6, 10, 32).WithArguments("message", "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute.RequiresUnreferencedCodeAttribute(string)"),
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
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    int M2 => M1();
}";
			var fix = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    int M2 => M1();
}";
			// Can't apply RUC on properties at the moment
			return VerifyRequiresUnreferencedCodeCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(10,15): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan(10, 15, 10, 19).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(10,15): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan(10, 15, 10, 19).WithArguments("C.M1()", " message.", "")
				});
		}

		[Fact]
		public Task InvocationOnDynamicType ()
		{
			var source = @"
using System;
class C
{
	static void M0 ()
	{
		dynamic dynamicField = ""Some string"";
		Console.WriteLine (dynamicField);
	}

	static void M1 ()
	{
		MethodWithDynamicArgDoNothing (0);
		MethodWithDynamicArgDoNothing (""Some string"");
		MethodWithDynamicArg(-1);
	}

	static void MethodWithDynamicArgDoNothing (dynamic arg)
	{
	}

	static void MethodWithDynamicArg (dynamic arg)
	{
		arg.MethodWithDynamicArg (arg);
	}
}";

			return VerifyRequiresUnreferencedCodeAnalyzer (source,
				// (8,3): warning IL2026: Invoking members on dynamic types is not trimming safe. Types or members might have been removed by the trimmer.
				VerifyCS.Diagnostic (dynamicInvocationDiagnosticDescriptor).WithSpan (8, 3, 8, 35),
				// (24,3): warning IL2026: Invoking members on dynamic types is not trimming safe. Types or members might have been removed by the trimmer.
				VerifyCS.Diagnostic (dynamicInvocationDiagnosticDescriptor).WithSpan (24, 3, 24, 33));
		}

		[Fact]
		public Task DynamicInRequiresUnreferencedCodeClass ()
		{
			var source = @"
using System.Diagnostics.CodeAnalysis;

[RequiresUnreferencedCode(""message"")]
class ClassWithRequires
{
	public static void MethodWithDynamicArg (dynamic arg)
	{
		arg.DynamicInvocation ();
	}
}
";

			return VerifyRequiresUnreferencedCodeAnalyzer (source);
		}

		[Fact]
		public Task InvocationOnDynamicTypeInMethodWithRUCDoesNotWarnTwoTimes ()
		{
			var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
class C
{
	[RequiresUnreferencedCode (""We should only see the warning related to this annotation, and none about the dynamic type."")]
	static void M0 ()
	{
		dynamic dynamicField = ""Some string"";
		Console.WriteLine (dynamicField);
	}
}";

			return VerifyRequiresUnreferencedCodeAnalyzer (source);
		}

		[Fact]
		public Task TestMakeGenericMethodUsage ()
		{
			var source = @"
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

class C
{
	static void M1 (MethodInfo methodInfo)
	{
		methodInfo.MakeGenericMethod (typeof (C));
	}

	[RequiresUnreferencedCode (""Message from RUC"")]
	static void M2 (MethodInfo methodInfo)
	{
		methodInfo.MakeGenericMethod (typeof (C));
	}
}";

			return VerifyRequiresUnreferencedCodeAnalyzer (source);
		}

		[Fact]
		public Task TestMakeGenericTypeUsage ()
		{
			var source = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
	static void M1 (Type t)
	{
		typeof (Nullable<>).MakeGenericType (typeof (C));
	}

	[RequiresUnreferencedCode (""Message from RUC"")]
	static void M2 (Type t)
	{
		typeof (Nullable<>).MakeGenericType (typeof (C));
	}
}";

			return VerifyRequiresUnreferencedCodeAnalyzer (source);
		}

		[Fact]
		public Task VerifyThatAnalysisOfFieldsDoesNotNullRef ()
		{
			var source = @"
using System.Diagnostics.CodeAnalysis;

[DynamicallyAccessedMembers (field)]
class C
{
	public const DynamicallyAccessedMemberTypes field = DynamicallyAccessedMemberTypes.PublicMethods;
}";

			return VerifyRequiresUnreferencedCodeAnalyzer (source);
		}

		[Fact]
		public Task TestPropertyAssignmentInAssemblyAttribute ()
		{
			var source = @"
using System;
[assembly: MyAttribute (Value = 5)]

class MyAttribute : Attribute
{
	public int Value { get; set; }
}
";
			return VerifyRequiresUnreferencedCodeAnalyzer (source);
		}
	}
}
