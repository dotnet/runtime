// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	/// <summary>
	/// Test cases stored in files
	/// </summary>
	public class LinkerTestCases : TestCaseUtils
	{
		[Theory]
		[MemberData (nameof (GetTestData), parameters: nameof (RequiresCapability))]
		public void RequiresCapability (MethodDeclarationSyntax m, List<AttributeSyntax> attrs)
		{
			switch (m.Identifier.ValueText) {
			case "MethodWithDuplicateRequiresAttribute":
			case "TestRequiresUnreferencedCodeOnlyThroughReflection":
			case "TestRequiresInMethodFromCopiedAssembly":
			case "TestRequiresThroughReflectionInMethodFromCopiedAssembly":
			// There is a discrepancy between the way linker and the analyzer represent the location of the error,
			// linker will point to the method caller and the analyzer will point to a line of code.
			// The TestTypeIsBeforeFieldInit scenario is supported by the analyzer, just the diagnostic message is different
			// We verify the analyzer generating the right diagnostic in RequiresUnreferencedCodeAnalyzerTests.cs
			case "TestTypeIsBeforeFieldInit":
				return;
			}

			RunTest (m, attrs, UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer));
		}
	}
}
