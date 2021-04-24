using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpAnalyzerVerifier<
	ILLink.RoslynAnalyzer.RequiresAssemblyFilesAnalyzer>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class RequiresAssemblyFilesAnalyzerTests
	{
		static Task VerifyRequiresAssemblyFilesAnalyzer (string source, params DiagnosticResult[] expected)
		{
			return VerifyCS.VerifyAnalyzerAsync (source,
				TestCaseUtils.UseMSBuildProperties (MSBuildPropertyOptionNames.EnableSingleFileAnalyzer),
				expected);
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
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (12, 17, 12, 18).WithArguments ("C.E", "", ""));
		}

		[Fact]
		public Task SimpleDiagnosticOnMethod ()
		{
			var TestRequiresAssemblyFilesOnMethod = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	[RequiresAssemblyFiles]
	void M1()
	{
	}

	void M2()
	{
		M1();
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestRequiresAssemblyFilesOnMethod,
				// (13,3): warning IL3002: Using member 'C.M1()' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app.
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (13, 3, 13, 7).WithArguments ("C.M1()", "", ""));
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
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (12, 3, 12, 4).WithArguments ("C.P", "", ""),
				// (13,12): warning IL3002: Using member 'C.P' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app.
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (13, 35, 13, 36).WithArguments ("C.P", "", ""));
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
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (24, 3, 24, 4).WithArguments ("C.P", "", ""));
		}

		[Fact]
		public Task RequiresAssemblyFilesWithMessageAndUrl ()
		{
			var TestRequiresAssemblyFilesWithMessageAndUrl = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	[RequiresAssemblyFiles (Message = ""Message from attribute"", Url = ""https://helpurl"")]
	void M1()
	{
	}

	void M2()
	{
		M1();
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestRequiresAssemblyFilesWithMessageAndUrl,
				// (13,3): warning IL3002: Using member 'C.M1()' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app. Message from attribute. https://helpurl
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (13, 3, 13, 7).WithArguments ("C.M1()", " Message from attribute.", " https://helpurl"));
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
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (13, 3, 13, 7).WithArguments ("C.M1()", "", " https://helpurl"));
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

	[RequiresAssemblyFiles (Message = ""Warn from M2"")]
	void M2()
	{
		M3();
	}

	[RequiresAssemblyFiles (Message = ""Warn from M3"")]
	void M3()
	{
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestNoDiagnosticIsProducedIfCallerIsAnnotated,
				// (8,3): warning IL3002: Using member 'C.M2()' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app. Warn from M2.
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (8, 3, 8, 7).WithArguments ("C.M2()", " Warn from M2.", ""));
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
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3000).WithSpan (5, 26, 5, 66).WithArguments ("System.Reflection.Assembly.Location"));
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
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3000).WithSpan (8, 13, 8, 23).WithArguments ("System.Reflection.Assembly.Location")
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
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3001).WithSpan (8, 13, 8, 41).WithArguments ("System.Reflection.Assembly.GetFile(string)"),
				// (9,13): warning IL3001: Assemblies embedded in a single-file app cannot have additional files in the manifest.
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3001).WithSpan (9, 13, 9, 25).WithArguments ("System.Reflection.Assembly.GetFiles()")
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
				// (8,13): warning IL3000: 'System.Reflection.AssemblyName.CodeBase' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3000).WithSpan (8, 13, 8, 23).WithArguments ("System.Reflection.AssemblyName.CodeBase"),
				// (9,13): warning IL3000: 'System.Reflection.AssemblyName.EscapedCodeBase' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3000).WithSpan (9, 13, 9, 30).WithArguments ("System.Reflection.AssemblyName.EscapedCodeBase")
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
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3000).WithSpan (8, 13, 8, 23).WithArguments ("System.Reflection.Assembly.Location"),
				// (9,13): warning IL3001: Assemblies embedded in a single-file app cannot have additional files in the manifest.
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3001).WithSpan (9, 13, 9, 25).WithArguments ("System.Reflection.Assembly.GetFiles()")
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
		public Task LazyDelegateWithRequiresAssemblyFiles ()
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

			return VerifyRequiresAssemblyFilesAnalyzer (src,
				// (6,50): warning IL3002: Using member 'C.InitC()' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app.
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (6, 50, 6, 55).WithArguments ("C.InitC()", "", ""));
		}

		[Fact]
		public Task ActionDelegateWithRequiresAssemblyFiles ()
		{
			const string src = @"
using System;
using System.Diagnostics.CodeAnalysis;
class C
{
    [RequiresAssemblyFiles]
    public static void M1() { }
    public static void M2()
    {
        Action a = M1;
        Action b = () => M1();
    }
}";

			return VerifyRequiresAssemblyFilesAnalyzer (src,
				// (10,20): warning IL3002: Using member 'C.M1()' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app.
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (10, 20, 10, 22).WithArguments ("C.M1()", "", ""),
				// (11,26): warning IL3002: Using member 'C.M1()' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app.
				VerifyCS.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (11, 26, 11, 30).WithArguments ("C.M1()", "", ""));
		}
	}
}
