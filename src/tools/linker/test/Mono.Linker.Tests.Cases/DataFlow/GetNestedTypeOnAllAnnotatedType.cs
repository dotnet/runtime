// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
			TestIfElse (1, typeof (TestType), typeof (TestType));
			TestSwitchAllValid (1, typeof (TestType));
			TestOnKnownTypeOnly ();
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
