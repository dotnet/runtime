// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	class GetNestedTypeOnAllAnnotatedType
	{
		static void Main ()
		{
			TestOnAllAnnotatedParameter (typeof (TestType));
			TestOnNonAllAnnotatedParameter (typeof (TestType));
			TestWithBindingFlags (typeof (TestType));
			TestWithUnknownBindingFlags (BindingFlags.Public, typeof (TestType));
			TestUnsupportedBindingFlags (typeof (TestType));
			TestWithNull ();
			TestWithEmptyInput ();
			TestIfElse (1, typeof (TestType), typeof (TestType));
			TestSwitchAllValid (1, typeof (TestType));
			TestOnKnownTypeOnly ();
			TestOnKnownTypeWithNullName ();
			TestOnKnownTypeWithUnknownName ("noname");
			TestWithKnownTypeAndNameWhichDoesntExist ();
		}

		static void TestOnAllAnnotatedParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (TestType.NestedType));
			nestedType.RequiresAll ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestOnNonAllAnnotatedParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (TestType.NestedType));
			nestedType.RequiresAll ();
		}

		static void TestWithBindingFlags ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (TestType.NestedType), BindingFlags.Public);
			nestedType.RequiresAll ();
		}

		static void TestWithUnknownBindingFlags (BindingFlags bindingFlags, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (TestType.NestedType), bindingFlags);
			nestedType.RequiresAll ();
		}

		static void TestUnsupportedBindingFlags ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (TestType.NestedType), BindingFlags.IgnoreCase);
			nestedType.RequiresAll ();
		}

		static void TestWithNull ()
		{
			Type parentType = null;
			var nestedType = parentType.GetNestedType (nameof (TestType.NestedType));
			nestedType.RequiresAll ();
		}

		static void TestWithEmptyInput ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle); // Throws at runtime -> tracked as empty value set
			noValue.GetNestedType (nameof (TestType.NestedType)).RequiresAll (); // No warning - empty input
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestIfElse (int number, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentWithAll, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)] Type parentWithoutAll)
		{
			Type typeOfParent;
			if (number == 1) {
				typeOfParent = parentWithAll;
			} else {
				typeOfParent = parentWithoutAll;
			}
			var nestedType = typeOfParent.GetNestedType (nameof (TestType.NestedType));
			nestedType.RequiresAll ();
		}

		static void TestSwitchAllValid (int number, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentWithAll)
		{
			Type typeOfParent = number switch {
				1 => parentWithAll,
				2 => null,
				3 => typeof (TestType)
			};

			var nestedType = typeOfParent.GetNestedType (nameof (TestType.NestedType));
			nestedType.RequiresAll ();
		}

		static void TestOnKnownTypeOnly ()
		{
			typeof (TestType).GetNestedType (nameof (TestType.NestedType)).RequiresAll ();
		}

		static void TestOnKnownTypeWithNullName ()
		{
			typeof (TestType).GetNestedType (null).RequiresAll ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestOnKnownTypeWithUnknownName (string name)
		{
			// WARN - we will preserve the nested type, but not as a whole, just the type itself, so it can't fullfil the All annotation
			typeof (TestType).GetNestedType (name).RequiresAll ();
		}

		static void TestWithKnownTypeAndNameWhichDoesntExist ()
		{
			// Should not warn since we can statically determine that GetNestedType will return null so there's no problem with trimming
			typeof (TestType).GetNestedType ("NonExisting").RequiresAll ();
		}

		class TestType
		{
			public class NestedType
			{
				NestedType () { }
				public static int PublicStaticInt;
				public void Method () { }
				int Prop { get; set; }
			}
		}
	}
}
