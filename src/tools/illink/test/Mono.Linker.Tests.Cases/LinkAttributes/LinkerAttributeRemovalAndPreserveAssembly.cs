// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
		new[] { "Dependencies/LinkerAttributeRemovalAndPreserveAssembly_Lib.cs" })]
	// https://github.com/dotnet/linker/issues/2358 - adding the descriptor currently causes nullref in the linker
	// resources: new object[] { new string[] { "Dependencies/LinkerAttributeRemovalAndPreserveAssembly_Lib.Descriptor.xml", "ILLink.Descriptors.xml" } })]

	[ExpectedNoWarnings]

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

	public class AttributeToRemoveAttribute : Attribute { }
}