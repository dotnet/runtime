// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.DataFlow.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]

	[SetupCompileBefore ("base.dll", new[] { "Dependencies/MemberTypesAllBaseTypeAssembly.cs" })]
	[KeptAssembly ("base.dll")]
	[SetupLinkerAction ("link", "base")]
	[SetupLinkerAction ("copy", "test")]

	[KeptTypeInAssembly ("base.dll", "Mono.Linker.Tests.Cases.DataFlow.Dependencies.MemberTypesAllBaseType")]
	[KeptMemberInAssembly ("base.dll", "Mono.Linker.Tests.Cases.DataFlow.Dependencies.MemberTypesAllBaseType", new string[] {
		".cctor()",
		".ctor()",
		".ctor(System.Boolean)",
		"PublicMethod()",
		"PrivateMethod()",
		"PublicStaticMethod()",
		"PrivateStaticMethod()",
		"PublicField",
		"PrivateField",
		"PublicStaticField",
		"PrivateStaticField",
		"PublicProperty",
		"get_PublicProperty()",
		"set_PublicProperty(System.Boolean)",
		"PrivateProperty",
		"get_PrivateProperty()",
		"set_PrivateProperty(System.Boolean)",
		"PublicStaticProperty",
		"get_PublicStaticProperty()",
		"set_PublicStaticProperty(System.Boolean)",
		"PrivateStaticProperty",
		"get_PrivateStaticProperty()",
		"set_PrivateStaticProperty(System.Boolean)",
		"PublicEvent",
		"add_PublicEvent(System.EventHandler`1<System.EventArgs>)",
		"remove_PublicEvent(System.EventHandler`1<System.EventArgs>)",
		"PrivateEvent",
		"add_PrivateEvent(System.EventHandler`1<System.EventArgs>)",
		"remove_PrivateEvent(System.EventHandler`1<System.EventArgs>)",
		"PublicStaticEvent",
		"add_PublicStaticEvent(System.EventHandler`1<System.EventArgs>)",
		"remove_PublicStaticEvent(System.EventHandler`1<System.EventArgs>)",
		"PrivateStaticEvent",
		"add_PrivateStaticEvent(System.EventHandler`1<System.EventArgs>)",
		"remove_PrivateStaticEvent(System.EventHandler`1<System.EventArgs>)",
	})]

	[KeptTypeInAssembly ("base.dll", "Mono.Linker.Tests.Cases.DataFlow.Dependencies.MemberTypesAllBaseType/PublicNestedType")]
	[KeptMemberInAssembly ("base.dll", "Mono.Linker.Tests.Cases.DataFlow.Dependencies.MemberTypesAllBaseType/PublicNestedType", new string[] { "PrivateMethod()" })]

	[KeptTypeInAssembly ("base.dll", "Mono.Linker.Tests.Cases.DataFlow.Dependencies.MemberTypesAllBaseType/PrivateNestedType")]
	[KeptMemberInAssembly ("base.dll", "Mono.Linker.Tests.Cases.DataFlow.Dependencies.MemberTypesAllBaseType/PrivateNestedType", new string[] { "PrivateMethod()" })]

	// https://github.com/dotnet/runtime/issues/78752
	[KeptMember (".ctor()", By = Tool.Trimmer)]
	public class MemberTypesAllOnCopyAssembly
	{
		public static void Main ()
		{
			typeof (TestType).RequiresAll ();
		}

		[Kept]
		[KeptBaseType (typeof (MemberTypesAllBaseType))]
		[KeptMember (".ctor()")]
		class TestType : MemberTypesAllBaseType
		{
		}
	}
}
