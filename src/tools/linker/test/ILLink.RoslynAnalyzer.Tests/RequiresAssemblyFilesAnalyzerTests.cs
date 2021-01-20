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
			// TODO: Remove this once we have the new attribute in the runtime.
			source = @"namespace System.Diagnostics.CodeAnalysis
{
#nullable enable
    [AttributeUsage(AttributeTargets.Constructor |
                    AttributeTargets.Event |
                    AttributeTargets.Method |
                    AttributeTargets.Property,
                    Inherited = false,
                    AllowMultiple = false)]
    public sealed class RequiresAssemblyFilesAttribute : Attribute
    {
			public RequiresAssemblyFilesAttribute() { }
			public string? Message { get; set; }
			public string? Url { get; set; }
	}
}" + Environment.NewLine + source;
			return VerifyCS.VerifyAnalyzerAsync (source,
				TestCaseUtils.UseMSBuildProperties (MSBuildPropertyOptionNames.PublishSingleFile),
				expected);
		}

		[Fact]
		public Task SimpleDiagnosticOnEvent ()
		{
			var TestRequiresAssemblyFieldsOnEvent = @"
class C
{
	[System.Diagnostics.CodeAnalysis.RequiresAssemblyFiles]
	event System.EventHandler? E;

	void M()
	{
		var handler = E;
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestRequiresAssemblyFieldsOnEvent,
				VerifyCS.Diagnostic ().WithSpan (25, 17, 25, 18).WithArguments ("C.E"));
		}

		[Fact]
		public Task SimpleDiagnosticOnMethod ()
		{
			var TestRequiresAssemblyFilesOnMethod = @"
class C
{
	[System.Diagnostics.CodeAnalysis.RequiresAssemblyFiles]
	void M1()
	{
	}

	void M2()
	{
		M1();
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestRequiresAssemblyFilesOnMethod,
				VerifyCS.Diagnostic ().WithSpan (27, 3, 27, 7).WithArguments ("C.M2()"));
		}

		[Fact]
		public Task SimpleDiagnosticOnProperty ()
		{
			var TestRequiresAssemblyFilesOnProperty = @"
class C
{
	[System.Diagnostics.CodeAnalysis.RequiresAssemblyFiles]
	bool P { get; set; }

	void M()
	{
		P = false;
		bool b = P;
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestRequiresAssemblyFilesOnProperty,
				VerifyCS.Diagnostic ().WithSpan (25, 3, 25, 4).WithArguments ("C.P"),
				VerifyCS.Diagnostic ().WithSpan (26, 12, 26, 13).WithArguments ("C.P"));
		}

		[Fact]
		public Task RequiresAssemblyFilesWithMessageAndUrl ()
		{
			var TestRequiresAssemblyFilesWithMessageAndUrl = @"
class C
{
	[System.Diagnostics.CodeAnalysis.RequiresAssemblyFiles (Message = ""Message from attribute"", Url = ""https://helpurl"")]
	void M1()
	{
	}

	void M2()
	{
		M1();
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestRequiresAssemblyFilesWithMessageAndUrl,
				VerifyCS.Diagnostic ().WithSpan (27, 3, 27, 7).WithArguments ("C.M2()", "Message from attribute", "https://helpurl"));
		}

		[Fact]
		public Task NoDiagnosticIfMethodNotCalled ()
		{
			var TestNoDiagnosticIfMethodNotCalled = @"
class C
{
	[System.Diagnostics.CodeAnalysis.RequiresAssemblyFiles]
	void M() { }
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestNoDiagnosticIfMethodNotCalled);
		}

		[Fact]
		public Task NoDiagnosticIsProducedIfCallerIsAnnotated ()
		{
			var TestNoDiagnosticIsProducedIfCallerIsAnnotated = @"
class C
{
	void M1()
	{
		M2();
	}

	[System.Diagnostics.CodeAnalysis.RequiresAssemblyFiles (Message = ""Warn from M2"")]
	void M2()
	{
		M3();
	}

	[System.Diagnostics.CodeAnalysis.RequiresAssemblyFiles (Message = ""Warn from M3"")]
	void M3()
	{
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestNoDiagnosticIsProducedIfCallerIsAnnotated,
				VerifyCS.Diagnostic ().WithSpan (22, 3, 22, 7).WithArguments ("C.M2()", "Warn from M2"));
		}
	}
}
