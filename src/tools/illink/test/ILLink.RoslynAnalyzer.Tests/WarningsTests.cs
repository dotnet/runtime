// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class WarningsTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Warnings";

		[Fact (Skip = "Analyzers are disabled entirely by SuppressTrimAnalysisWarnings or SuppressAotAnalysisWarnings")]
		public Task CanDisableWarningsByCategory ()
		{
			return RunTest ();
		}
	}
}
