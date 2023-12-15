// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Interop.PInvoke
{
	public class IndividualTests : LinkerTestBase
	{
		protected override string TestSuiteName => "Interop/PInvoke/Individual";

		[Fact]
		public Task CanOutputPInvokes ()
		{
			return RunTest ();
		}
	}
}