// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.LinkAttributes.Dependencies;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[SetupCompileBefore ("attributes.dll", new string[] { "Dependencies/TestRemoveDontRemoveAttributes.cs" },
		resources: new object[] { new string[] { "Dependencies/TestRemoveAttribute.xml", "ILLink.LinkAttributes.xml" } }, addAsReference: false)]
	[SetupCompileBefore ("library.dll", new string[] { "Dependencies/ReferencedAssemblyWithAttributes.cs" }, references: new string[] { "attributes.dll" })]
	[SetupLinkerAction ("copyused", "attributes")] // Ensure that assembly-level attributes are kept
	[IgnoreLinkAttributes (false)]
	[KeptAttributeInAssembly ("library.dll", "Mono.Linker.Tests.Cases.LinkAttributes.Dependencies.TestDontRemoveAttribute")]
	[RemovedAttributeInAssembly ("library.dll", "Mono.Linker.Tests.Cases.LinkAttributes.Dependencies.TestRemoveAttribute")]
	class EmbeddedLinkAttributesInReferencedAssembly_AssemblyLevel
	{
		public static void Main ()
		{
			ReferenceOtherAssembly ();
		}

		[Kept]
		public static void ReferenceOtherAssembly ()
		{
			var _1 = new ReferencedAssemblyWithAttributes ();
		}
	}
}
