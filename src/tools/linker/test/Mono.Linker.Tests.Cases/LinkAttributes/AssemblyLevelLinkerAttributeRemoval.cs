// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[SetupLinkAttributesFile ("AssemblyLevelLinkerAttributeRemoval.xml")]
	[IgnoreLinkAttributes (false)]

	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AssemblyLevelLinkerAttributeRemoval_Lib.cs" })]

	[RemovedTypeInAssembly ("library.dll", "Mono.Linker.Tests.Cases.TestAttributeLib.MyAttribute")]
	class AssemblyLevelLinkerAttributeRemoval
	{
		public static void Main ()
		{
			new Mono.Linker.Tests.Cases.TestAttributeLib.Foo ();
		}
	}
}
