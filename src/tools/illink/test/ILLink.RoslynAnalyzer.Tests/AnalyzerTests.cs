// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpAnalyzerVerifier<
	ILLink.RoslynAnalyzer.RequiresUnreferencedCodeAnalyzer>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class AnalyzerTests
	{
		[Fact]
		public Task SimpleDiagnostic ()
		{
			var src = @"
using System.Diagnostics.CodeAnalysis;

class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    int M1() => 0;
    int M2() => M1();
}";
			return VerifyCS.VerifyAnalyzerAsync (src,
				// (8,17): warning IL2026: Calling 'System.Int32 C::M1()' which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. message.
				VerifyCS.Diagnostic ().WithSpan (8, 17, 8, 21).WithArguments ("C.M1()", "message")
				);
		}
	}
}
