// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpAnalyzerVerifier<
	ILLink.RoslynAnalyzer.AvoidAssemblyLocationInSingleFile>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class AvoidAssemblyLocationInSingleFileTests
	{
		static Task VerifySingleFileAnalyzer (string source, params DiagnosticResult[] expected)
		{
			return VerifyCS.VerifyAnalyzerAsync (source,
				TestCaseUtils.UseMSBuildProperties (MSBuildPropertyOptionNames.EnableSingleFileAnalyzer),
				expected);
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

			return VerifySingleFileAnalyzer (src,
				// (5,26): warning IL3000: 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
				VerifyCS.Diagnostic (AvoidAssemblyLocationInSingleFile.IL3000).WithSpan (5, 26, 5, 66).WithArguments ("System.Reflection.Assembly.Location"));
		}

		[Fact]
		public Task AssemblyProperties ()
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
			return VerifySingleFileAnalyzer (src,
				// (8,13): warning IL3000: 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
				VerifyCS.Diagnostic (AvoidAssemblyLocationInSingleFile.IL3000).WithSpan (8, 13, 8, 23).WithArguments ("System.Reflection.Assembly.Location")
			);
		}

		[Fact]
		public Task AssemblyMethods ()
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
			return VerifySingleFileAnalyzer (src,
				// (8,13): warning IL3001: Assemblies embedded in a single-file app cannot have additional files in the manifest.
				VerifyCS.Diagnostic (AvoidAssemblyLocationInSingleFile.IL3001).WithSpan (8, 13, 8, 41).WithArguments ("System.Reflection.Assembly.GetFile(string)"),
				// (9,13): warning IL3001: Assemblies embedded in a single-file app cannot have additional files in the manifest.
				VerifyCS.Diagnostic (AvoidAssemblyLocationInSingleFile.IL3001).WithSpan (9, 13, 9, 25).WithArguments ("System.Reflection.Assembly.GetFiles()")
				);
		}

		[Fact]
		public Task AssemblyNameAttributes ()
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
			return VerifySingleFileAnalyzer (src,
				// (8,13): warning IL3000: 'System.Reflection.AssemblyName.CodeBase' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
				VerifyCS.Diagnostic (AvoidAssemblyLocationInSingleFile.IL3000).WithSpan (8, 13, 8, 23).WithArguments ("System.Reflection.AssemblyName.CodeBase"),
				// (9,13): warning IL3000: 'System.Reflection.AssemblyName.EscapedCodeBase' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
				VerifyCS.Diagnostic (AvoidAssemblyLocationInSingleFile.IL3000).WithSpan (9, 13, 9, 30).WithArguments ("System.Reflection.AssemblyName.EscapedCodeBase")
				);
		}

		[Fact]
		public Task FalsePositive ()
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
			return VerifySingleFileAnalyzer (src,
				// (8,13): warning IL3000: 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
				VerifyCS.Diagnostic (AvoidAssemblyLocationInSingleFile.IL3000).WithSpan (8, 13, 8, 23).WithArguments ("System.Reflection.Assembly.Location"),
				// (9,13): warning IL3001: Assemblies embedded in a single-file app cannot have additional files in the manifest.
				VerifyCS.Diagnostic (AvoidAssemblyLocationInSingleFile.IL3001).WithSpan (9, 13, 9, 25).WithArguments ("System.Reflection.Assembly.GetFiles()")
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
	}
}
