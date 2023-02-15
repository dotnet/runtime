// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the ExpectedWarning attributes.
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class EmptyArrayIntrinsicsDataFlow
	{
		static void Main ()
		{
			TestGetPublicParameterlessConstructorWithEmptyTypes ();
			TestGetPublicParameterlessConstructorWithArrayEmpty ();
			TestGetPublicParameterlessConstructorWithUnknownArray ();
			TestGetConstructorOverloads ();
		}

		[ExpectedWarning ("IL2080", nameof (Type.GetMethod))]
		static void TestGetPublicParameterlessConstructorWithEmptyTypes ()
		{
			s_typeWithKeptPublicParameterlessConstructor.GetConstructor (Type.EmptyTypes);
			s_typeWithKeptPublicParameterlessConstructor.GetMethod ("Foo");
		}

		[ExpectedWarning ("IL2080", nameof (Type.GetMethod))]
		static void TestGetPublicParameterlessConstructorWithArrayEmpty ()
		{
			s_typeWithKeptPublicParameterlessConstructor.GetConstructor (Array.Empty<Type> ());
			s_typeWithKeptPublicParameterlessConstructor.GetMethod ("Foo");
		}

		[ExpectedWarning ("IL2080", nameof (Type.GetConstructor))]
		static void TestGetPublicParameterlessConstructorWithUnknownArray ()
		{
			s_typeWithKeptPublicParameterlessConstructor.GetConstructor (s_localEmptyArrayInvisibleToAnalysis);
		}

		[ExpectedWarning ("IL2080", nameof (Type.GetMethod))]
		static void TestGetConstructorOverloads ()
		{
			s_typeWithKeptPublicParameterlessConstructor.GetConstructor (BindingFlags.Public, null, Type.EmptyTypes, null);
			s_typeWithKeptPublicParameterlessConstructor.GetConstructor (BindingFlags.Public, null, CallingConventions.Any, Type.EmptyTypes, null);
			s_typeWithKeptPublicParameterlessConstructor.GetMethod ("Foo");
		}

		static Type[] s_localEmptyArrayInvisibleToAnalysis = Type.EmptyTypes;

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		static Type s_typeWithKeptPublicParameterlessConstructor = typeof (EmptyArrayIntrinsicsDataFlow);
	}
}
