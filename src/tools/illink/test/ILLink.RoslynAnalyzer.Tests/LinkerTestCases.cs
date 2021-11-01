// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
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
		[MemberData (nameof (TestCaseUtils.GetTestData), parameters: nameof (RequiresCapability))]
		public void RequiresCapability (string m)
		{
			RunTest (nameof (RequiresCapability), m, UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer));
		}

		[Theory]
		[MemberData (nameof (TestCaseUtils.GetTestData), parameters: nameof (Interop))]
		public void Interop (string m)
		{
			RunTest (nameof (Interop), m, UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer));
		}

		[Theory]
		[MemberData (nameof (TestCaseUtils.GetTestData), parameters: nameof (DataFlow))]
		public void DataFlow (string m)
		{
			var shouldRun = (TestCase testCase) => {
				var testSyntaxRoot = testCase.MemberSyntax.SyntaxTree.GetRoot ();
				var testCaseClass = testSyntaxRoot.DescendantNodes ().OfType<ClassDeclarationSyntax> ().First ();
				// Double-check that this is the right class. It should have a Main() method.
				var testCaseMain = testCaseClass.DescendantNodes ().OfType<MethodDeclarationSyntax> ().First ();
				if (testCaseMain.Identifier.ValueText != "Main")
					throw new NotImplementedException ();

				switch (testCaseClass.Identifier.ValueText) {
				case "MemberTypesRelationships":
				case "MethodParametersDataFlow":
				case "MethodReturnParameterDataFlow":
					return true;
				default:
					return false;
				}
			};

			RunTest (nameof (DataFlow), m, UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer), shouldRun);
		}
	}
}
