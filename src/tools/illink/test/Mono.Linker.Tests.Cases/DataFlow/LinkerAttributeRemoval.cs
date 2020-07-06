// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SetupLinkAttributesFile ("LinkerAttributeRemoval.xml")]
	[IgnoreLinkAttributes (false)]
	[KeptMember (".ctor()")]
	[LogContains ("IL2045: Mono.Linker.Tests.Cases.DataFlow.LinkerAttributeRemoval.TestType(): Custom Attribute System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute is " +
		"being referenced in code but the linker was instructed to remove all instances of this attribute. If the attribute instances are necessary make sure to either remove " +
		"the linker attribute XML portion which removes the attribute instances, or to override this use the linker XML descriptor to keep the attribute type (which in turn keeps all of its instances).")]
	class LinkerAttributeRemoval
	{
		public static void Main ()
		{
			var instance = new LinkerAttributeRemoval ();
			instance._fieldWithCustomAttribute = null;
			string value = instance.methodWithCustomAttribute ("parameter");
			TestType ();
		}
		[Kept]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.DefaultConstructor)]
		Type _fieldWithCustomAttribute;

		[Kept]
		[KeptAttributeAttribute (typeof (TestDontRemoveAttribute))]
		[TestDontRemoveAttribute]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		private string methodWithCustomAttribute ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] string parameterWithCustomAttribute)
		{
			return "this is a return value";
		}

		[Kept]
		public static void TestType ()
		{
			const string reflectionTypeKeptString = "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute";
			var typeKept = Type.GetType (reflectionTypeKeptString, false);
		}
	}

	[KeptBaseType (typeof (System.Attribute))]
	class TestDontRemoveAttribute : Attribute
	{
		[Kept]
		public TestDontRemoveAttribute ()
		{
		}
	}
}
