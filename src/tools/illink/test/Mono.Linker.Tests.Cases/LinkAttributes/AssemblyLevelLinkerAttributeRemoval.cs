// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
