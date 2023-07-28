// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.LinkAttributes.Dependencies;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[SetupCompileBefore ("attributes.dll", new string[] { "Dependencies/TestRemoveDontRemoveAttributes.cs" },
		resources: new object[] { new string[] { "Dependencies/TestRemoveAttribute.xml", "ILLink.LinkAttributes.xml" } })]
	[IgnoreLinkAttributes (false)]
	class EmbeddedLinkAttributesInReferencedAssembly
	{
		public static void Main ()
		{
			TestAttributeRemoval ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TestDontRemoveAttribute))]
		[TestDontRemove]
		[TestRemove]
		public static void TestAttributeRemoval () { }
	}
}
