// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class InlineArraysTests : LinkerTestBase
	{
		protected override string TestSuiteName => "InlineArrays";

		[Fact]
		public Task InlineArray ()
		{
			return RunTest (nameof (InlineArray));
		}

	}
}
