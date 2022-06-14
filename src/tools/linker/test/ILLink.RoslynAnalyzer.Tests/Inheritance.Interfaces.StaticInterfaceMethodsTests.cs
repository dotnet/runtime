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
		public Task StaticAbstractInterfaceMethods ()
		{
			return RunTest (nameof (StaticAbstractInterfaceMethods));
		}

		[Fact]
		public Task StaticAbstractInterfaceMethodsLibrary ()
		{
			return RunTest (nameof (StaticAbstractInterfaceMethodsLibrary));
		}
	}
}