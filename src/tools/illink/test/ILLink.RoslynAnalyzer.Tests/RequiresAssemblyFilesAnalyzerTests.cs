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
	ILLink.RoslynAnalyzer.RequiresAssemblyFilesAnalyzer,
	ILLink.CodeFix.RequiresAssemblyFilesCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class RequiresAssemblyFilesAnalyzerTests
	{
		static Task VerifyRequiresAssemblyFilesAnalyzer (string source, params DiagnosticResult[] expected)
		{
			return VerifyRequiresAssemblyFilesAnalyzer (source, null, expected);
		}

		static async Task VerifyRequiresAssemblyFilesAnalyzer (string source, IEnumerable<MetadataReference>? additionalReferences, params DiagnosticResult[] expected)
		{

			await VerifyCS.VerifyAnalyzerAsync (source,
				TestCaseUtils.UseMSBuildProperties (MSBuildPropertyOptionNames.EnableSingleFileAnalyzer),
				additionalReferences ?? Array.Empty<MetadataReference> (),
				expected);
		}

		static Task VerifyRequiresAssemblyFilesCodeFix (
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
build_property.{MSBuildPropertyOptionNames.EnableSingleFileAnalyzer} = true")));
			if (numberOfIterations != null) {
				test.NumberOfIncrementalIterations = numberOfIterations;
				test.NumberOfFixAllIterations = numberOfIterations;
			}
			test.FixedState.ExpectedDiagnostics.AddRange (fixedExpected);
			return test.RunAsync ();
		}

		[Fact]
		public Task SimpleDiagnosticOnEvent ()
		{
			var TestRequiresAssemblyFieldsOnEvent = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

class C
{
	[RequiresAssemblyFiles]
	event System.EventHandler? E;

	void M()
	{
		var handler = E;
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestRequiresAssemblyFieldsOnEvent,
				// (12,17): warning IL3002: Using member 'C.E' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app.
				VerifyCS.Diagnostic (DiagnosticId.RequiresAssemblyFiles).WithSpan (12, 17, 12, 18).WithArguments ("C.E", "", ""));
		}

		[Fact]
		public Task SimpleDiagnosticOnProperty ()
		{
			var TestRequiresAssemblyFilesOnProperty = @"
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

class C
{
	[RequiresAssemblyFiles]
	bool P { get; set; }

	void M()
	{
		P = false;
		List<bool> b = new List<bool> { P };
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestRequiresAssemblyFilesOnProperty,
				// (11,3): warning IL3002: Using member 'C.P' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app.
				VerifyCS.Diagnostic (DiagnosticId.RequiresAssemblyFiles).WithSpan (12, 3, 12, 4).WithArguments ("C.P", "", ""),
				// (13,12): warning IL3002: Using member 'C.P' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app.
				VerifyCS.Diagnostic (DiagnosticId.RequiresAssemblyFiles).WithSpan (13, 35, 13, 36).WithArguments ("C.P", "", ""));
		}

		[Fact]
		public Task CallDangerousMethodInsideProperty ()
		{
			var TestRequiresAssemblyFilesOnMethodInsideProperty = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	bool field;

	[RequiresAssemblyFiles]
	bool P {
		get {
			return field;
		}
		set {
			CallDangerousMethod ();
			field = value;
		}
	}

	[RequiresAssemblyFiles]
	void CallDangerousMethod () {}

	void M ()
	{
		P = false;
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestRequiresAssemblyFilesOnMethodInsideProperty,
				// (24,3): warning IL3002: Using member 'C.P' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app.
				VerifyCS.Diagnostic (DiagnosticId.RequiresAssemblyFiles).WithSpan (24, 3, 24, 4).WithArguments ("C.P", "", ""));
		}

		[Fact]
		public Task RequiresAssemblyFilesWithUrlOnly ()
		{
			var TestRequiresAssemblyFilesWithMessageAndUrl = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	[RequiresAssemblyFiles (Url = ""https://helpurl"")]
	void M1()
	{
	}

	void M2()
	{
		M1();
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestRequiresAssemblyFilesWithMessageAndUrl,
				// (13,3): warning IL3002: Using member 'C.M1()' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app. https://helpurl
				VerifyCS.Diagnostic (DiagnosticId.RequiresAssemblyFiles).WithSpan (13, 3, 13, 7).WithArguments ("C.M1()", "", " https://helpurl"));
		}

		[Fact]
		public Task NoDiagnosticIfMethodNotCalled ()
		{
			var TestNoDiagnosticIfMethodNotCalled = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	[RequiresAssemblyFiles]
	void M() { }
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestNoDiagnosticIfMethodNotCalled);
		}

		[Fact]
		public Task NoDiagnosticIsProducedIfCallerIsAnnotated ()
		{
			var TestNoDiagnosticIsProducedIfCallerIsAnnotated = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	void M1()
	{
		M2();
	}

	[RequiresAssemblyFiles (""Warn from M2"")]
	void M2()
	{
		M3();
	}

	[RequiresAssemblyFiles (""Warn from M3"")]
	void M3()
	{
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestNoDiagnosticIsProducedIfCallerIsAnnotated,
				// (8,3): warning IL3002: Using member 'C.M2()' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app. Warn from M2.
				VerifyCS.Diagnostic (DiagnosticId.RequiresAssemblyFiles).WithSpan (8, 3, 8, 7).WithArguments ("C.M2()", " Warn from M2.", ""));
		}

		[Fact]
		public Task GetExecutingAssemblyLocation ()
		{
			const string src = @"
using System.Reflection;
class C
{
    public string M() => Assembly.GetExecutingAssembly().Location;
}";

			return VerifyRequiresAssemblyFilesAnalyzer (src,
				// (5,26): warning IL3000: 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
				VerifyCS.Diagnostic (DiagnosticId.AvoidAssemblyLocationInSingleFile).WithSpan (5, 26, 5, 66).WithArguments ("System.Reflection.Assembly.Location"));
		}

		[Fact]
		public Task GetAssemblyLocationViaAssemblyProperties ()
		{
			var src = @"
using System.Reflection;
class C
{
    public void M()
    {
        var a = Assembly.GetExecutingAssembly();
        _ = a.Location;
        // below methods are marked as obsolete in 5.0
        // _ = a.CodeBase;
        // _ = a.EscapedCodeBase;
    }
}";
			return VerifyRequiresAssemblyFilesAnalyzer (src,
				// (8,13): warning IL3000: 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
				VerifyCS.Diagnostic (DiagnosticId.AvoidAssemblyLocationInSingleFile).WithSpan (8, 13, 8, 23).WithArguments ("System.Reflection.Assembly.Location")
			);
		}

		[Fact]
		public Task CallKnownDangerousAssemblyMethods ()
		{
			var src = @"
using System.Reflection;
class C
{
    public void M()
    {
        var a = Assembly.GetExecutingAssembly();
        _ = a.GetFile(""/some/file/path"");
        _ = a.GetFiles();
    }
}";
			return VerifyRequiresAssemblyFilesAnalyzer (src,
				// (8,13): warning IL3001: Assemblies embedded in a single-file app cannot have additional files in the manifest.
				VerifyCS.Diagnostic (DiagnosticId.AvoidAssemblyGetFilesInSingleFile).WithSpan (8, 13, 8, 41).WithArguments ("System.Reflection.Assembly.GetFile(String)"),
				// (9,13): warning IL3001: Assemblies embedded in a single-file app cannot have additional files in the manifest.
				VerifyCS.Diagnostic (DiagnosticId.AvoidAssemblyGetFilesInSingleFile).WithSpan (9, 13, 9, 25).WithArguments ("System.Reflection.Assembly.GetFiles()")
				);
		}

		[Fact]
		public Task CallKnownDangerousAssemblyNameAttributes ()
		{
			var src = @"
using System.Reflection;
class C
{
    public void M()
    {
        var a = Assembly.GetExecutingAssembly().GetName();
        _ = a.CodeBase;
        _ = a.EscapedCodeBase;
    }
}";
			return VerifyRequiresAssemblyFilesAnalyzer (src,
				// (8,13): warning IL3002: Using member 'System.Reflection.AssemblyName.CodeBase.get' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app. The code will return an empty string for assemblies embedded in a single-file app.
				VerifyCS.Diagnostic (DiagnosticId.RequiresAssemblyFiles).WithSpan (8, 13, 8, 23).WithArguments ("System.Reflection.AssemblyName.CodeBase.get", " The code will return an empty string for assemblies embedded in a single-file app.", ""),
				// (8,13): warning IL3000: 'System.Reflection.AssemblyName.CodeBase' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
				VerifyCS.Diagnostic (DiagnosticId.AvoidAssemblyLocationInSingleFile).WithSpan (8, 13, 8, 23).WithArguments ("System.Reflection.AssemblyName.CodeBase"),
				// (9,13): warning IL3000: 'System.Reflection.AssemblyName.EscapedCodeBase' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
				VerifyCS.Diagnostic (DiagnosticId.AvoidAssemblyLocationInSingleFile).WithSpan (9, 13, 9, 30).WithArguments ("System.Reflection.AssemblyName.EscapedCodeBase")
				);
		}

		[Fact]
		public Task GetAssemblyLocationFalsePositive ()
		{
			// This is an OK use of Location and GetFile since these assemblies were loaded from
			// a file, but the analyzer is conservative
			var src = @"
using System.Reflection;
class C
{
    public void M()
    {
        var a = Assembly.LoadFrom(""/some/path/not/in/bundle"");
        _ = a.Location;
        _ = a.GetFiles();
    }
}";
			return VerifyRequiresAssemblyFilesAnalyzer (src,
				// (8,13): warning IL3000: 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
				VerifyCS.Diagnostic (DiagnosticId.AvoidAssemblyLocationInSingleFile).WithSpan (8, 13, 8, 23).WithArguments ("System.Reflection.Assembly.Location"),
				// (9,13): warning IL3001: Assemblies embedded in a single-file app cannot have additional files in the manifest.
				VerifyCS.Diagnostic (DiagnosticId.AvoidAssemblyGetFilesInSingleFile).WithSpan (9, 13, 9, 25).WithArguments ("System.Reflection.Assembly.GetFiles()")
				);
		}

		[Fact]
		public Task PublishSingleFileIsNotSet ()
		{
			var src = @"
using System.Reflection;
class C
{
    public void M()
    {
        var a = Assembly.GetExecutingAssembly().Location;
    }
}";
			// If 'PublishSingleFile' is not set to true, no diagnostics should be produced by the analyzer. This will
			// effectively verify that the number of produced diagnostics matches the number of expected ones (zero).
			return VerifyCS.VerifyAnalyzerAsync (src);
		}

		[Fact]
		public Task SupressWarningsWithRequiresAssemblyFiles ()
		{
			const string src = @"
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
class C
{
    [RequiresAssemblyFiles]
    public void M()
    {
        var a = Assembly.GetExecutingAssembly();
        _ = a.Location;
        var b = Assembly.GetExecutingAssembly();
        _ = b.GetFile(""/some/file/path"");
        _ = b.GetFiles();
    }
}";

			return VerifyRequiresAssemblyFilesAnalyzer (src);
		}

		[Fact]
		public Task RequiresAssemblyFilesDiagnosticFix ()
		{
			var test = @"
using System.Diagnostics.CodeAnalysis;
public class C
{
    [RequiresAssemblyFiles(""message"")]
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
    [RequiresAssemblyFiles(""message"")]
    public int M1() => 0;
    [RequiresAssemblyFiles(""Calls C.M1()"")]
    int M2() => M1();
}
class D
{
    [RequiresAssemblyFiles(""Calls C.M1()"")]
    public int M3(C c) => c.M1();
    public class E
    {
        [RequiresAssemblyFiles(""Calls C.M1()"")]
        public int M4(C c) => c.M1();
    }
}
public class E
{
    public class F
    {
        [RequiresAssemblyFiles()]
        public int M5(C c) => c.M1();
    }
}
";
			return VerifyRequiresAssemblyFilesCodeFix (
				test,
				fixtest,
				baselineExpected: new[] {
				// /0/Test0.cs(7,17): warning IL3002: Using method 'C.M1()' which has 'RequiresAssemblyFilesAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic (DiagnosticId.RequiresAssemblyFiles).WithSpan (7, 17, 7, 21).WithArguments ("C.M1()", " message.", ""),
				// /0/Test0.cs(11,27): warning IL3002: Using method 'C.M1()' which has 'RequiresAssemblyFilesAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic (DiagnosticId.RequiresAssemblyFiles).WithSpan (11, 27, 11, 33).WithArguments ("C.M1()", " message.", ""),
				// /0/Test0.cs(14,31): warning IL3002: Using method 'C.M1()' which has 'RequiresAssemblyFilesAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic (DiagnosticId.RequiresAssemblyFiles).WithSpan (14, 31, 14, 37).WithArguments ("C.M1()", " message.", ""),
				// /0/Test0.cs(21,31): warning IL3002: Using method 'C.M1()' which has 'RequiresAssemblyFilesAttribute' can break functionality when trimming application code. message.
				VerifyCS.Diagnostic (DiagnosticId.RequiresAssemblyFiles).WithSpan (21, 31, 21, 37).WithArguments ("C.M1()", " message.", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public Task FixInSingleFileSpecialCases ()
		{
			var test = @"
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
public class C
{
    public static Assembly assembly = Assembly.LoadFrom(""/some/path/not/in/bundle"");
    public string M1() => assembly.Location;
    public void M2() {
        _ = assembly.GetFiles();
    }
}
";
			var fixtest = @"
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
public class C
{
    public static Assembly assembly = Assembly.LoadFrom(""/some/path/not/in/bundle"");

    [RequiresAssemblyFiles()]
    public string M1() => assembly.Location;

    [RequiresAssemblyFiles()]
    public void M2() {
        _ = assembly.GetFiles();
    }
}
";
			return VerifyRequiresAssemblyFilesCodeFix (
				test,
				fixtest,
				baselineExpected: new[] {
				// /0/Test0.cs(7,27): warning IL3000: 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
				VerifyCS.Diagnostic (DiagnosticId.AvoidAssemblyLocationInSingleFile).WithSpan (7, 27, 7, 44).WithArguments ("System.Reflection.Assembly.Location", "", ""),
				// /0/Test0.cs(9,13): warning IL3001: 'System.Reflection.Assembly.GetFiles()' will throw for assemblies embedded in a single-file app
				VerifyCS.Diagnostic (DiagnosticId.AvoidAssemblyGetFilesInSingleFile).WithSpan (9, 13, 9, 32).WithArguments("System.Reflection.Assembly.GetFiles()", "", ""),
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public Task FixInPropertyDecl ()
		{
			var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresAssemblyFiles(""message"")]
    public int M1() => 0;

    int M2 => M1();
}";
			var fix = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresAssemblyFiles(""message"")]
    public int M1() => 0;

    [RequiresAssemblyFiles(""Calls C.M1()"")]
    int M2 => M1();
}";
			return VerifyRequiresAssemblyFilesCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(10,15): warning IL3002: Using member 'C.M1()' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresAssemblyFiles).WithSpan(10, 15, 10, 19).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public Task FixInField ()
		{
			const string src = @"
using System;
using System.Diagnostics.CodeAnalysis;
class C
{
    public static Lazy<C> _default = new Lazy<C>(InitC);
    public static C Default => _default.Value;

    [RequiresAssemblyFiles]
    public static C InitC() {
        C cObject = new C();
        return cObject;
    }
}";
			var fixtest = @"
using System;
using System.Diagnostics.CodeAnalysis;
class C
{
    public static Lazy<C> _default = new Lazy<C>(InitC);
    public static C Default => _default.Value;

    [RequiresAssemblyFiles]
    public static C InitC() {
        C cObject = new C();
        return cObject;
    }
}";

			return VerifyRequiresAssemblyFilesCodeFix (
				src,
				fixtest,
				baselineExpected: new[] {
				// /0/Test0.cs(6,50): warning IL3002: Using member 'C.InitC()' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app.
				VerifyCS.Diagnostic (DiagnosticId.RequiresAssemblyFiles).WithSpan (6, 50, 6, 55).WithArguments ("C.InitC()", "", ""),
				},
				fixedExpected: new[] {
				// /0/Test0.cs(6,50): warning IL3002: Using member 'C.InitC()' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app.
				VerifyCS.Diagnostic (DiagnosticId.RequiresAssemblyFiles).WithSpan (6, 50, 6, 55).WithArguments ("C.InitC()", "", ""),
				});
		}

		[Fact]
		public Task FixInLocalFunc ()
		{
			var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresAssemblyFiles(""message"")]
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
    [RequiresAssemblyFiles(""message"")]
    public int M1() => 0;

    [RequiresAssemblyFiles(""Calls Wrapper()"")]
    Action M2()
    {
        [global::System.Diagnostics.CodeAnalysis.RequiresAssemblyFilesAttribute(""Calls C.M1()"")] void Wrapper () => M1();
        return Wrapper;
    }
}";
			// Roslyn currently doesn't simplify the attribute name properly, see https://github.com/dotnet/roslyn/issues/52039
			return VerifyRequiresAssemblyFilesCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(12,28): warning IL3002: Using member 'C.M1()' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresAssemblyFiles).WithSpan(12, 28, 12, 32).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> (),
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
    [RequiresAssemblyFiles(""message"")]
    public int M1() => 0;

    public C () => M1();
}";
			var fix = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresAssemblyFiles(""message"")]
    public int M1() => 0;

    [RequiresAssemblyFiles()]
    public C () => M1();
}";
			return VerifyRequiresAssemblyFilesCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(10,15): warning IL3002: Using member 'C.M1()' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresAssemblyFiles).WithSpan(10, 20, 10, 24).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public Task FixInEvent ()
		{
			var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresAssemblyFiles(""message"")]
    public int M1() => 0;

    public event EventHandler E1
    {
        add
        {
            var a = M1();
        }
        remove { }
    }
}";
			var fix = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresAssemblyFiles(""message"")]
    public int M1() => 0;

    [RequiresAssemblyFiles()]
    public event EventHandler E1
    {
        add
        {
            var a = M1();
        }
        remove { }
    }
}";
			return VerifyRequiresAssemblyFilesCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(14,21): warning IL3002: Using method 'C.M1()' which has 'RequiresAssemblyFilesAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(DiagnosticId.RequiresAssemblyFiles).WithSpan(14, 21, 14, 25).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}
	}
}
