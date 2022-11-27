// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.LinkAttributes.Dependencies;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[IgnoreDescriptors (false)]

	// The test verifies that removed attributes which are later on kept due to descriptors correctly warn.
	// The setup is:
	//  - test assembly with the AttributeToRemoveAttribute type
	//  - link attributes.xml which marks the attribute for removal (applied early, in this case via command line, but could be a embedded in the test assembly)
	//  - the attribute is used by the test assembly
	//  - another assembly lib.dll is added and is referenced (after the attribute is used/marked)
	//  - This new assembly has a descriptor which marks the entire test assembly (note that it marks the TEST assembly)
	//  - This marking causes the warning, as it's an explicit request to keep the attribute which was supposed to be removed

	[SetupLinkAttributesFile ("LinkerAttributeRemovalAndPreserveAssembly.LinkAttributes.xml")]

	[SetupCompileBefore (
		"lib.dll",
		new[] { "Dependencies/LinkerAttributeRemovalAndPreserveAssembly_Lib.cs" },
		resources: new object[] { new string[] { "Dependencies/LinkerAttributeRemovalAndPreserveAssembly_Lib.Descriptor.xml", "ILLink.Descriptors.xml" } })]

	[ExpectedWarning ("IL2045", FileName = "ILLink.Descriptors.xml in lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken")]
	[ExpectedNoWarnings]

	[KeptMember (".ctor()")]
	class LinkerAttributeRemovalAndPreserveAssembly
	{
		public static void Main ()
		{
			TestAttributeRemoval ();
		}

		[AttributeToRemoveAttribute]
		[Kept]
		static void TestAttributeRemoval ()
		{
			Used.Use ();
		}
	}

	// The attribute is kept (partially) because we found out about it too late
	// It does report a warning though
	[Kept]
	[KeptBaseType (typeof (Attribute))]
	[KeptMember (".ctor()")]
	public class AttributeToRemoveAttribute : Attribute { }
}
