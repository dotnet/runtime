// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class MethodByRefParameterDataFlow
	{
		public static void Main ()
		{
			Type typeWithMethods = _fieldWithMethods;

			TestAssignStaticToAnnotatedRefParameter (ref typeWithMethods);
			TestAssignParameterToAnnotatedRefParameter (ref typeWithMethods, typeof (TestType));
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type _fieldWithMethods = null;

		[ExpectedWarning ("IL2026", "Message for --TestType.Requires--")]

		// https://github.com/dotnet/linker/issues/2158
		// The type.GetMethods call generates a warning because we're not able to correctly track the value of the "this".
		// (there's a ldind.ref insruction here which we currently don't handle and the "this" becomes unknown)
		[ExpectedWarning ("IL2065")]
		static void TestAssignStaticToAnnotatedRefParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] ref Type type)
		{
			type = typeof (TestTypeWithRequires);
			type.GetMethods (); // Should not warn
		}

		// The warning message is REALLY confusing (basically wrong) since it talks about "calling the method with wrong argument"
		// which is definitely not the case here.
		[ExpectedWarning ("IL2067", "typeWithFields")]

		// https://github.com/dotnet/linker/issues/2158
		// The type.GetMethods call generates a warning because we're not able to correctly track the value of the "this".
		// (there's a ldind.ref insruction here which we currently don't handle and the "this" becomes unknown)
		[ExpectedWarning ("IL2065")]
		static void TestAssignParameterToAnnotatedRefParameter (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] ref Type type,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type typeWithFields)
		{
			type = typeWithFields; // Should warn
			type.GetMethods (); // Should not warn
		}

		class TestTypeWithRequires
		{
			[RequiresUnreferencedCode ("Message for --TestType.Requires--")]
			public static void Requires () { }
		}

		class TestType
		{
		}
	}
}