// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[SetupLinkAttributesFile ("LinkerAttributeRemovalWithOverride.xml")]
	[SetupLinkerDescriptorFile ("OverrideAttributeRemoval.xml")]
	[IgnoreLinkAttributes (false)]
	[KeptMember (".ctor()")]
	class OverrideAttributeRemoval
	{
		public static void Main ()
		{
			var instance = new OverrideAttributeRemoval ();
			instance._fieldWithCustomAttribute = null;
			string value = instance.methodWithCustomAttribute ("parameter");
		}
		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type _fieldWithCustomAttribute;

		[Kept]
		[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		private string methodWithCustomAttribute (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
			string parameterWithCustomAttribute)
		{
			return "this is a return value";
		}
	}
}
