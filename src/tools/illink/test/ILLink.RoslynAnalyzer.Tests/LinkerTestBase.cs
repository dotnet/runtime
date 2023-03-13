// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ILLink.RoslynAnalyzer.Tests
{
	public abstract class LinkerTestBase : TestCaseUtils
	{
		protected abstract string TestSuiteName { get; }

		private static readonly (string, string)[] MSBuildProperties = UseMSBuildProperties (
			MSBuildPropertyOptionNames.EnableTrimAnalyzer,
			MSBuildPropertyOptionNames.EnableSingleFileAnalyzer,
			MSBuildPropertyOptionNames.EnableAotAnalyzer);

		protected Task RunTest ([CallerMemberName] string testName = "", bool allowMissingWarnings = false)
		{
			return RunTestFile (TestSuiteName, testName, allowMissingWarnings, MSBuildProperties);
		}
	}
}