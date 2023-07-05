// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Interop
{
	public sealed class PInvokeTests : LinkerTestBase
	{
		protected override string TestSuiteName => "Interop/PInvoke";


		[Fact]
		public Task UnusedDefaultConstructorIsRemoved ()
		{
			return RunTest ();
		}

		[Fact]
		public Task UnusedFieldsOfTypesPassedByRefAreNotRemoved ()
		{
			return RunTest ();
		}

		[Fact]
		public Task DefaultConstructorOfReturnTypeIsNotRemoved ()
		{
			return RunTest ();
		}

		[Fact]
		public Task UnusedDefaultConstructorOfTypePassedByRefIsNotRemoved ()
		{
			return RunTest ();
		}

		[Fact]
		public Task UnusedFieldsOfTypesAreNotRemoved ()
		{
			return RunTest ();
		}

		[Fact]
		public Task UnusedPInvoke ()
		{
			return RunTest ();
		}
	}
}