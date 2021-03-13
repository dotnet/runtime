// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace System
{
	public class MockCorelibAttributeToRemove : Attribute
	{
	}
}

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[IgnoreLinkAttributes (false)]
	[SetupLinkerTrimMode ("link")] // Ensure that corelib gets linked so that its attribtues are processed
	[SetupLinkerArgument ("--skip-unresolved", "true")] // Allow unresolved references to types missing from mock corelib
	[SetupCompileBefore (PlatformAssemblies.CoreLib, new string[] { "Dependencies/MockCorelib.cs" },
		resources: new object[] { new string[] { "Dependencies/MockCorelib.xml", "ILLink.LinkAttributes.xml" } },
		defines: new[] { "INCLUDE_MOCK_CORELIB" })]
	[SkipPeVerify]
	[RemovedAttributeInAssembly ("System.Private.CoreLib", "System.MockCorelibAttributeToRemove")]
	[RemovedTypeInAssembly ("System.Private.CoreLib", "System.MockCorelibAttributeToRemove")]
	class EmbeddedLinkAttributesInCorelib
	{
		public static void Main ()
		{
			AttributedMethod ();
			var _ = new AttributedClass ();
			var _2 = new UsedCorelibType ();
		}

		[Kept]
		[MockCorelibAttributeToRemove]
		public static void AttributedMethod ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[MockCorelibAttributeToRemove]
		public class AttributedClass
		{
		}
	}
}
