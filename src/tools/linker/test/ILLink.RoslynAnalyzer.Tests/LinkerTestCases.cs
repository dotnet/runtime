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
			case "TestCovariantReturnCallOnDerived":
			case "TestRequiresUnreferencedCodeOnlyThroughReflection":
			case "TestStaticCctorRequiresUnreferencedCode":
			case "TestStaticCtorMarkingIsTriggeredByFieldAccess":
			case "TestTypeWhichOverridesMethodVirtualMethodRequiresUnreferencedCode":
			case "TestRequiresInMethodFromCopiedAssembly":
			case "TestRequiresThroughReflectionInMethodFromCopiedAssembly":
			case "TestStaticCtorTriggeredByMethodCall":
			case "TestTypeIsBeforeFieldInit":
				return;
			}

			RunTest (m, attrs, UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer));
		}
	}
}
