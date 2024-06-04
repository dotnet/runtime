// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Mono.Linker.Tests
{
	[TestFixture]
	public class CecilVersionCheck
	{
		[TestCase]
		public void CecilPackageVersionMatchesAssemblyVersion()
		{
			string cecilPackageVersion = (string)AppContext.GetData("Mono.Linker.Tests.CecilPackageVersion")!;
			// Assume that the test assembly builds against the same cecil as ILLink.
			var cecilAssemblyVersion = Assembly
				.GetExecutingAssembly()
				.GetReferencedAssemblies()
				.Single(an => an.Name == "Mono.Cecil")
				.Version;

			Assert.AreEqual(cecilPackageVersion.AsSpan(0,6).ToString(), cecilAssemblyVersion.ToString(3));
		}
	}
}
