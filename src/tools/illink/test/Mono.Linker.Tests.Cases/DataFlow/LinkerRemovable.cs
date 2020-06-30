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
	[SetupLinkAttributesFile ("LinkerRemovable.xml")]
	[IgnoreLinkAttributes (false)]
	[KeptMember (".ctor()")]
	[LogContains ("IL2044: Mono.Linker.Tests.Cases.DataFlow.LinkerRemovable::TestType(): Custom Attribute System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute is being referenced in code after LinkerRemovableAttribute was used on the Custom Attribute type")]
	class LinkerRemovable
	{
		public static void Main ()
		{
			var instance = new LinkerRemovable ();
			instance._fieldWithCustomAttribute = null;
			string value = instance.methodWithCustomAttribute ("parameter");
			TestType ();
		}
		[Kept]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.DefaultConstructor)]
		Type _fieldWithCustomAttribute;

		[Kept]
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
}
