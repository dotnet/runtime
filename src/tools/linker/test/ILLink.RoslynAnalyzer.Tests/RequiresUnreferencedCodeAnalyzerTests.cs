// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
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
		static Task VerifyRequiresUnreferencedCodeAnalyzer (string source, params DiagnosticResult[] expected)
		{
			return VerifyCS.VerifyAnalyzerAsync (source,
				TestCaseUtils.UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer),
				expected);
		}

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
				ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
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
		public Task SimpleDiagnostic ()
		{
			var TestRequiresWithMessageOnlyOnMethod = @"
using System.Diagnostics.CodeAnalysis;

class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    int M1() => 0;
    int M2() => M1();
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (TestRequiresWithMessageOnlyOnMethod,
				// (8,17): warning IL2026: Using method 'C.M1()' which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. message.
				VerifyCS.Diagnostic ().WithSpan (8, 17, 8, 21).WithArguments ("C.M1()", " message.", ""));
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

    [RequiresUnreferencedCode(""Calls M1"")]
    int M2() => M1();
}
class D
{
    [RequiresUnreferencedCode(""Calls M1"")]
    public int M3(C c) => c.M1();

    public class E
    {
        [RequiresUnreferencedCode(""Calls M1"")]
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
	// /0/Test0.cs(9,17): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
	VerifyCS.Diagnostic ().WithSpan (9, 17, 9, 21).WithArguments ("C.M1()", " message.", ""),
	// /0/Test0.cs(13,27): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
	VerifyCS.Diagnostic().WithSpan(13, 27, 13, 33).WithArguments("C.M1()", " message.", ""),
	// /0/Test0.cs(17,31): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
	VerifyCS.Diagnostic ().WithSpan (17, 31, 17, 37).WithArguments ("C.M1()", " message.", ""),
	// /0/Test0.cs(24,31): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
	VerifyCS.Diagnostic ().WithSpan (24, 31, 24, 37).WithArguments ("C.M1()", " message.", "")
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
					// /0/Test0.cs(12,22): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic().WithSpan(12, 22, 12, 26).WithArguments("C.M1()", " message.", "")
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

    [RequiresUnreferencedCode(""Calls Wrapper"")]
    Action M2()
    {
        [global::System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute(""Calls M1"")] void Wrapper () => M1();
        return Wrapper;
    }
}";
			// Roslyn currently doesn't simplify the attribute name properly, see https://github.com/dotnet/roslyn/issues/52039
			return VerifyRequiresUnreferencedCodeCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(12,28): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic().WithSpan(12, 28, 12, 32).WithArguments("C.M1()", " message.", "")
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
					// /0/Test0.cs(10,19): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic().WithSpan(10, 19, 10, 23).WithArguments("C.M1()", " message.", "")
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
					// /0/Test0.cs(10,15): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic().WithSpan(10, 15, 10, 19).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(10,15): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic().WithSpan(10, 15, 10, 19).WithArguments("C.M1()", " message.", "")
				});
		}

		[Fact]
		public Task TestRequiresWithMessageAndUrlOnMethod ()
		{
			var MessageAndUrlOnMethod = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	static void TestRequiresWithMessageAndUrlOnMethod ()
	{
		RequiresWithMessageAndUrl ();
	}
	[RequiresUnreferencedCode (""Message for --RequiresWithMessageAndUrl--"", Url = ""https://helpurl"")]
	static void RequiresWithMessageAndUrl ()
	{
	}
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (MessageAndUrlOnMethod,
				// (8,3): warning IL2026: Using method 'C.RequiresWithMessageAndUrl()' which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. Message for --RequiresWithMessageAndUrl--. https://helpurl
				VerifyCS.Diagnostic ().WithSpan (8, 3, 8, 31).WithArguments ("C.RequiresWithMessageAndUrl()", " Message for --RequiresWithMessageAndUrl--.", " https://helpurl")
				);
		}

		[Fact]
		public Task TestRequiresOnPropertyGetter ()
		{
			var PropertyRequires = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	static void TestRequiresOnPropertyGetter ()
	{
		_ = PropertyRequires;
	}

	static int PropertyRequires {
		[RequiresUnreferencedCode (""Message for --getter PropertyRequires--"")]
		get { return 42; }
	}
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (PropertyRequires,
				// (8,7): warning IL2026: Using method 'C.PropertyRequires.get' which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. Message for --getter PropertyRequires--.
				VerifyCS.Diagnostic ().WithSpan (8, 7, 8, 23).WithArguments ("C.PropertyRequires.get", " Message for --getter PropertyRequires--.", "")
				);
		}

		[Fact]
		public Task TestRequiresOnPropertySetter ()
		{
			var PropertyRequires = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	static void TestRequiresOnPropertySetter ()
	{
		PropertyRequires = 0;
	}

	static int PropertyRequires {
		[RequiresUnreferencedCode (""Message for --setter PropertyRequires--"")]
		set { }
	}
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (PropertyRequires,
				// (8,3): warning IL2026: Using method 'C.PropertyRequires.set' which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. Message for --setter PropertyRequires--.
				VerifyCS.Diagnostic ().WithSpan (8, 3, 8, 19).WithArguments ("C.PropertyRequires.set", " Message for --setter PropertyRequires--.", "")
				);
		}

		[Fact]
		public Task TestStaticCctorRequiresUnreferencedCode ()
		{
			var src = @"
using System.Diagnostics.CodeAnalysis;

class StaticCtor
{
	[RequiresUnreferencedCode (""Message for --TestStaticCtor--"")]
	static StaticCtor ()
	{
	}

	static void TestStaticCctorRequiresUnreferencedCode ()
	{
		_ = new StaticCtor ();
	}
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (src,
				// (13,7): warning IL2026: Using method 'StaticCtor.StaticCtor()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --TestStaticCtor--.
				VerifyCS.Diagnostic ().WithSpan (13, 7, 13, 24).WithArguments ("StaticCtor.StaticCtor()", " Message for --TestStaticCtor--.", "")
				);
		}

		[Fact]
		public Task StaticCtorTriggeredByFieldAccess ()
		{
			var src = @"
using System.Diagnostics.CodeAnalysis;

class StaticCtorTriggeredByFieldAccess
{
	public static int field;

	[RequiresUnreferencedCode (""Message for --StaticCtorTriggeredByFieldAccess.Cctor--"")]
	static StaticCtorTriggeredByFieldAccess ()
	{
		field = 0;
	}
}
class C
{
	static void TestStaticCtorMarkingIsTriggeredByFieldAccess ()
	{
		var x = StaticCtorTriggeredByFieldAccess.field + 1;
	}
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (src,
				// (18,11): warning IL2026: Using method 'StaticCtorTriggeredByFieldAccess.StaticCtorTriggeredByFieldAccess()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --StaticCtorTriggeredByFieldAccess.Cctor--.
				VerifyCS.Diagnostic ().WithSpan (18, 11, 18, 49).WithArguments ("StaticCtorTriggeredByFieldAccess.StaticCtorTriggeredByFieldAccess()", " Message for --StaticCtorTriggeredByFieldAccess.Cctor--.", "")
				);
		}

		[Fact]
		public Task TestStaticCtorTriggeredByMethodCall ()
		{
			var src = @"
using System.Diagnostics.CodeAnalysis;

class StaticCtorTriggeredByMethodCall
{
	[RequiresUnreferencedCode (""Message for --StaticCtorTriggeredByMethodCall.Cctor--"")]
	static StaticCtorTriggeredByMethodCall ()
	{
	}

	[RequiresUnreferencedCode (""Message for --StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--"")]
	public void TriggerStaticCtorMarking ()
	{
	}
}

class C
{
	static void TestStaticCtorTriggeredByMethodCall ()
	{
		new StaticCtorTriggeredByMethodCall ().TriggerStaticCtorMarking ();
	}
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (src,
				// (21,3): warning IL2026: Using method 'StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--.
				VerifyCS.Diagnostic ().WithSpan (21, 3, 21, 69).WithArguments ("StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking()", " Message for --StaticCtorTriggeredByMethodCall.TriggerStaticCtorMarking--.", ""),
				// (21,3): warning IL2026: Using method 'StaticCtorTriggeredByMethodCall.StaticCtorTriggeredByMethodCall()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --StaticCtorTriggeredByMethodCall.Cctor--.
				VerifyCS.Diagnostic ().WithSpan (21, 3, 21, 41).WithArguments ("StaticCtorTriggeredByMethodCall.StaticCtorTriggeredByMethodCall()", " Message for --StaticCtorTriggeredByMethodCall.Cctor--.", "")
				);
		}

		[Fact]
		public Task TypeIsBeforeFieldInit ()
		{
			var TypeIsBeforeFieldInit = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	class TypeIsBeforeFieldInit
	{
		public static int field = AnnotatedMethod ();

		[RequiresUnreferencedCode (""Message from --TypeIsBeforeFieldInit.AnnotatedMethod--"")]
		public static int AnnotatedMethod () => 42;
	}

	static void TestTypeIsBeforeFieldInit ()
	{
		var x = TypeIsBeforeFieldInit.field + 42;
	}
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (TypeIsBeforeFieldInit,
				// (8,29): warning IL2026: Using method 'C.TypeIsBeforeFieldInit.AnnotatedMethod()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message from --TypeIsBeforeFieldInit.AnnotatedMethod--.
				VerifyCS.Diagnostic ().WithSpan (8, 29, 8, 47).WithArguments ("C.TypeIsBeforeFieldInit.AnnotatedMethod()", " Message from --TypeIsBeforeFieldInit.AnnotatedMethod--.", "")
				);
		}

		[Fact]
		public Task LazyDelegateWithRequiresUnreferencedCode ()
		{
			const string src = @"
using System;
using System.Diagnostics.CodeAnalysis;
class C
{
    public static Lazy<C> _default = new Lazy<C>(InitC);
    public static C Default => _default.Value;

    [RequiresUnreferencedCode (""Message from --C.InitC--"")]
    public static C InitC() {
        C cObject = new C();
        return cObject;
    }
}";

			return VerifyRequiresUnreferencedCodeAnalyzer (src,
				// (6,50): warning IL2026: Using method 'C.InitC()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message from --C.InitC--.
				VerifyCS.Diagnostic ().WithSpan (6, 50, 6, 55).WithArguments ("C.InitC()", " Message from --C.InitC--.", ""));
		}

		[Fact]
		public Task ActionDelegateWithRequiresAssemblyFiles ()
		{
			const string src = @"
using System;
using System.Diagnostics.CodeAnalysis;
class C
{
    [RequiresUnreferencedCode (""Message from --C.M1--"")]
    public static void M1() { }
    public static void M2()
    {
        Action a = M1;
        Action b = () => M1();
    }
}";

			return VerifyRequiresUnreferencedCodeAnalyzer (src,
				// (10,20): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message from --C.M1--.
				VerifyCS.Diagnostic ().WithSpan (10, 20, 10, 22).WithArguments ("C.M1()", " Message from --C.M1--.", ""),
				// (11,26): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message from --C.M1--.
				VerifyCS.Diagnostic ().WithSpan (11, 26, 11, 30).WithArguments ("C.M1()", " Message from --C.M1--.", ""));
		}
	}
}
