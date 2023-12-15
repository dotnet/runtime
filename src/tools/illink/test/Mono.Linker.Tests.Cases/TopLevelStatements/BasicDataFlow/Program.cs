// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

[assembly: ExpectedNoWarnings]
[assembly: ExpectedWarning ("IL2072", CompilerGeneratedCode = true)]

RequirePublicFields (GetNone ());
TopLevelClass.Test();

[Kept]
static Type GetNone () => null;

[Kept]
static void RequirePublicFields (
	[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
	[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type t) {}

class TopLevelClass {

	[Kept]
	[ExpectedWarning ("IL2072")]
	public static void Test() => RequirePublicFields (GetNone ());

	[Kept]
	static Type GetNone () => null;

	[Kept]
	static void RequirePublicFields (
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type t) {}
}
