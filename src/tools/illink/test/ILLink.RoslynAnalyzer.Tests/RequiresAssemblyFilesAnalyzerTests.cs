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
				VerifyCS.Diagnostic ().WithSpan (12, 17, 12, 18).WithArguments ("C.E", "", ""));
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
				VerifyCS.Diagnostic ().WithSpan (13, 3, 13, 7).WithArguments ("C.M1()", "", ""));
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
				VerifyCS.Diagnostic ().WithSpan (12, 3, 12, 4).WithArguments ("C.P", "", ""),
				// (13,12): warning IL3002: Using member 'C.P' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app.
				VerifyCS.Diagnostic ().WithSpan (13, 35, 13, 36).WithArguments ("C.P", "", ""));
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
				VerifyCS.Diagnostic ().WithSpan (13, 3, 13, 7).WithArguments ("C.M1()", " Message from attribute.", " https://helpurl"));
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
				VerifyCS.Diagnostic ().WithSpan (13, 3, 13, 7).WithArguments ("C.M1()", "", " https://helpurl"));
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
				VerifyCS.Diagnostic ().WithSpan (8, 3, 8, 7).WithArguments ("C.M2()", " Warn from M2.", ""));
		}
	}
}
