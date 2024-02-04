// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyCSUSM = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
	ILLink.RoslynAnalyzer.DynamicallyAccessedMembersAnalyzer,
	ILLink.CodeFix.UnconditionalSuppressMessageCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class UnconditionalSuppressMessageCodeFixTests
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

		static Task VerifyUnconditionalSuppressMessageCodeFixWithRUC (
			string source,
			string fixedSource,
			DiagnosticResult[] baselineExpected,
			DiagnosticResult[] fixedExpected)
		{
			var test = new VerifyCSUSM.Test {
				TestCode = source,
				FixedCode = fixedSource
			};
			test.ExpectedDiagnostics.AddRange (baselineExpected);
			test.TestState.AnalyzerConfigFiles.Add (
						("/.editorconfig", SourceText.From (@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableTrimAnalyzer} = true")));
			test.FixedState.ExpectedDiagnostics.AddRange (fixedExpected);
			return test.RunAsync ();
		}

		static Task VerifyUnconditionalSuppressMessageCodeFixWithRAF (
			string source,
			string fixedSource,
			DiagnosticResult[] baselineExpected,
			DiagnosticResult[] fixedExpected)
		{
			var test = new VerifyCSUSM.Test {
				TestCode = source,
				FixedCode = fixedSource
			};
			test.ExpectedDiagnostics.AddRange (baselineExpected);
			test.TestState.AnalyzerConfigFiles.Add (
						("/.editorconfig", SourceText.From (@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableSingleFileAnalyzer} = true")));
			test.FixedState.ExpectedDiagnostics.AddRange (fixedExpected);
			return test.RunAsync ();
		}

		static Task VerifyUnconditionalSuppressMessageCodeFixWithRDC (
			string source,
			string fixedSource,
			DiagnosticResult[] baselineExpected,
			DiagnosticResult[] fixedExpected)
		{
			var test = new VerifyCSUSM.Test {
				TestCode = source + dynamicCodeAttribute,
				FixedCode = fixedSource + dynamicCodeAttribute
			};
			test.ExpectedDiagnostics.AddRange (baselineExpected);
			test.TestState.AnalyzerConfigFiles.Add (
						("/.editorconfig", SourceText.From (@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableAotAnalyzer} = true")));
			test.FixedState.ExpectedDiagnostics.AddRange (fixedExpected);
			return test.RunAsync ();
		}

		[Fact]
		public Task SuppressRequiresUnreferencedCodeFixer ()
		{
			var test = @"
using System.Diagnostics.CodeAnalysis;
public class C
{
    [RequiresUnreferencedCode(""message"")]
    public int M1() => 0;
    int M2() => M1();
}";
			var fixtest = @"
using System.Diagnostics.CodeAnalysis;
public class C
{
    [RequiresUnreferencedCode(""message"")]
    public int M1() => 0;
    [UnconditionalSuppressMessage(""Trimming"", ""IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code"", Justification = ""<Pending>"")]
    int M2() => M1();
}";
			return VerifyUnconditionalSuppressMessageCodeFixWithRUC (
				test,
				fixtest,
				baselineExpected: new[] {
				// /0/Test0.cs(7,17): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCSUSM.Diagnostic (DiagnosticId.RequiresUnreferencedCode).WithSpan (7, 17, 7, 21).WithArguments ("C.M1()", " message.", ""),
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public Task SuppressRequiresAssemblyFilesFixer ()
		{
			var test = @"
using System.Diagnostics.CodeAnalysis;
public class C
{
    [RequiresAssemblyFiles(""message"")]
    public int M1() => 0;
    int M2() => M1();
}";
			var fixtest = @"
using System.Diagnostics.CodeAnalysis;
public class C
{
    [RequiresAssemblyFiles(""message"")]
    public int M1() => 0;
    [UnconditionalSuppressMessage(""SingleFile"", ""IL3002:Avoid calling members marked with 'RequiresAssemblyFilesAttribute' when publishing as a single-file"", Justification = ""<Pending>"")]
    int M2() => M1();
}";
			return VerifyUnconditionalSuppressMessageCodeFixWithRAF (
				test,
				fixtest,
				baselineExpected: new[] {
				// /0/Test0.cs(7,17): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCSUSM.Diagnostic (DiagnosticId.RequiresAssemblyFiles).WithSpan (7, 17, 7, 21).WithArguments ("C.M1()", " message.", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public Task SuppressRequiresDynamicCodeFixer ()
		{
			var test = @"
using System.Diagnostics.CodeAnalysis;
public class C
{
    [RequiresDynamicCode(""message"")]
    public int M1() => 0;
    int M2() => M1();
}";
			var fixtest = @"
using System.Diagnostics.CodeAnalysis;
public class C
{
    [RequiresDynamicCode(""message"")]
    public int M1() => 0;
    [UnconditionalSuppressMessage(""AOT"", ""IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling."", Justification = ""<Pending>"")]
    int M2() => M1();
}";
			return VerifyUnconditionalSuppressMessageCodeFixWithRDC (
				test,
				fixtest,
				baselineExpected: new[] {
				// /0/Test0.cs(7,17): warning IL3050: Members annotated with 'RequiresDynamicCodeAttribute' require dynamic access otherwise can break functionality when trimming application code. message.
				VerifyCSUSM.Diagnostic (DiagnosticId.RequiresDynamicCode).WithSpan (7, 17, 7, 21).WithArguments ("C.M1()", " message.", "")
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

    [UnconditionalSuppressMessage(""SingleFile"", ""IL3000:Avoid accessing Assembly file path when publishing as a single file"", Justification = ""<Pending>"")]
    public string M1() => assembly.Location;

    [UnconditionalSuppressMessage(""SingleFile"", ""IL3001:Avoid accessing Assembly file path when publishing as a single file"", Justification = ""<Pending>"")]
    public void M2() {
        _ = assembly.GetFiles();
    }
}
";
			return VerifyUnconditionalSuppressMessageCodeFixWithRAF (
				test,
				fixtest,
				baselineExpected: new[] {
				// /0/Test0.cs(7,27): warning IL3000: 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
				VerifyCSUSM.Diagnostic(DiagnosticId.AvoidAssemblyLocationInSingleFile).WithSpan (7, 27, 7, 44).WithArguments ("System.Reflection.Assembly.Location.get", "", ""),
				// /0/Test0.cs(9,13): warning IL3001: 'System.Reflection.Assembly.GetFiles()' will throw for assemblies embedded in a single-file app
				VerifyCSUSM.Diagnostic(DiagnosticId.AvoidAssemblyGetFilesInSingleFile).WithSpan (9, 13, 9, 32).WithArguments("System.Reflection.Assembly.GetFiles()", "", ""),
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

    [UnconditionalSuppressMessage(""Trimming"", ""IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code"", Justification = ""<Pending>"")]
    int M2 => M1();
}";
			return VerifyUnconditionalSuppressMessageCodeFixWithRUC (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(10,15): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCSUSM.Diagnostic (DiagnosticId.RequiresUnreferencedCode).WithSpan(10, 15, 10, 19).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
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
			        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
			        get { return M1(); }

			        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
			        set { field = M1(); }
				}
			}
			""";
			var diag = new[] {
				// /0/Test0.cs(12,16): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCSUSM.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan(12, 16, 12, 20).WithArguments("C.M1()", " message.", ""),
				// /0/Test0.cs(13,17): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCSUSM.Diagnostic(DiagnosticId.RequiresUnreferencedCode).WithSpan(13, 17, 13, 21).WithArguments("C.M1()", " message.", "")
			};
			return VerifyUnconditionalSuppressMessageCodeFixWithRUC (src, fix, diag, Array.Empty<DiagnosticResult> ());
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
    [UnconditionalSuppressMessage(""SingleFile"", ""IL3002:Avoid calling members marked with 'RequiresAssemblyFilesAttribute' when publishing as a single-file"", Justification = ""<Pending>"")]
    public static Lazy<C> _default = new Lazy<C>(InitC);
    public static C Default => _default.Value;

    [RequiresAssemblyFiles]
    public static C InitC() {
        C cObject = new C();
        return cObject;
    }
}";
			return VerifyUnconditionalSuppressMessageCodeFixWithRAF (
				src,
				fixtest,
				baselineExpected: new[] {
				// /0/Test0.cs(6,50): warning IL3002: Using member 'C.InitC()' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app.
				VerifyCSUSM.Diagnostic (DiagnosticId.RequiresAssemblyFiles).WithSpan (6, 50, 6, 55).WithArguments ("C.InitC()", "", ""),
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

    Action M2()
    {
        [UnconditionalSuppressMessage(""Trimming"", ""IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code"", Justification = ""<Pending>"")] void Wrapper () => M1();
        return Wrapper;
    }
}";
			// Roslyn currently doesn't simplify the attribute name properly, see https://github.com/dotnet/roslyn/issues/52039
			return VerifyUnconditionalSuppressMessageCodeFixWithRUC (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(12,28): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCSUSM.Diagnostic (DiagnosticId.RequiresUnreferencedCode).WithSpan(12, 28, 12, 32).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
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
    public int M1() => 0;

    public C () => M1();
}";
			var fix = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    [UnconditionalSuppressMessage(""Trimming"", ""IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code"", Justification = ""<Pending>"")]
    public C () => M1();
}";
			return VerifyUnconditionalSuppressMessageCodeFixWithRUC (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(10,15): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCSUSM.Diagnostic (DiagnosticId.RequiresUnreferencedCode).WithSpan(10, 20, 10, 24).WithArguments("C.M1()", " message.", "")
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
    [RequiresUnreferencedCodeAttribute(""message"")]
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
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    public event EventHandler E1
    {
        [UnconditionalSuppressMessage(""Trimming"", ""IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code"", Justification = ""<Pending>"")]
        add
        {
            var a = M1();
        }
        remove { }
    }
}";
			return VerifyUnconditionalSuppressMessageCodeFixWithRUC (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(14,21): warning IL2026: Using member 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCSUSM.Diagnostic (DiagnosticId.RequiresUnreferencedCode).WithSpan(14, 21, 14, 25).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}
	}
}
