// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpAnalyzerVerifier<
	ILLink.RoslynAnalyzer.RequiresUnreferencedCodeAnalyzer>;

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
				VerifyCS.Diagnostic ().WithSpan (8, 17, 8, 21).WithArguments ("C.M1()", "message", ""));
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
				VerifyCS.Diagnostic ().WithSpan (8, 3, 8, 31).WithArguments ("C.RequiresWithMessageAndUrl()", "Message for --RequiresWithMessageAndUrl--", "https://helpurl")
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
				VerifyCS.Diagnostic ().WithSpan (8, 7, 8, 23).WithArguments ("C.PropertyRequires.get", "Message for --getter PropertyRequires--", "")
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
				VerifyCS.Diagnostic ().WithSpan (8, 3, 8, 19).WithArguments ("C.PropertyRequires.set", "Message for --setter PropertyRequires--", "")
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
				VerifyCS.Diagnostic ().WithSpan (8, 29, 8, 47).WithArguments ("C.TypeIsBeforeFieldInit.AnnotatedMethod()", "Message from --TypeIsBeforeFieldInit.AnnotatedMethod--", "")
				);
		}
	}
}
