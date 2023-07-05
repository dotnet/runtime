// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Interop.PInvoke
{
	public sealed class WarningsTests : LinkerTestBase
	{
		protected override string TestSuiteName => "Interop/PInvoke/Warnings";

		[Fact]
		public Task ComPInvokeWarning ()
		{
			return RunTest ();
		}
	}
}