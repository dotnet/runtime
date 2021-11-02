// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
		[MemberData (nameof (TestCaseUtils.GetTestData), parameters: nameof (RequiresCapability))]
		public void RequiresCapability (string m)
		{
			RunTest (nameof (RequiresCapability), m, UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer, MSBuildPropertyOptionNames.EnableSingleFileAnalyzer));
		}

		[Theory]
		[MemberData (nameof (TestCaseUtils.GetTestData), parameters: nameof (Interop))]
		public void Interop (string m)
		{
			RunTest (nameof (Interop), m, UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer));
		}
	}
}
