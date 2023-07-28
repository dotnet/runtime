// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces
{
	public sealed partial class StaticInterfaceMethodsTests : LinkerTestBase
	{
		protected override string TestSuiteName => "Inheritance.Interfaces.StaticInterfaceMethods";

		[Fact]
		public Task BaseProvidesInterfaceMethod ()
		{
			return RunTest (allowMissingWarnings: false);
		}

		[Fact]
		public Task StaticAbstractInterfaceMethods ()
		{
			return RunTest (allowMissingWarnings: false);
		}

		[Fact]
		public Task StaticAbstractInterfaceMethodsLibrary ()
		{
			return RunTest (allowMissingWarnings: false);
		}

		[Fact]
		public Task StaticInterfaceMethodsInPreservedScope ()
		{
			return RunTest (allowMissingWarnings: false);
		}

		[Fact]
		public Task StaticVirtualInterfaceMethodsInPreservedScope ()
		{
			return RunTest (allowMissingWarnings: false);
		}

		[Fact]
		public Task StaticVirtualInterfaceMethodsInPreservedScopeLibrary ()
		{
			return RunTest (allowMissingWarnings: false);
		}

		[Fact]
		public Task StaticVirtualInterfaceMethodsLibrary ()
		{
			return RunTest (allowMissingWarnings: false);
		}

		[Fact]
		public Task UnusedInterfacesInPreservedScope ()
		{
			return RunTest (allowMissingWarnings: false);
		}

		[Fact]
		public Task UnusedStaticInterfaceMethods ()
		{
			return RunTest (allowMissingWarnings: false);
		}
	}
}
