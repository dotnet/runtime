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
	[RecognizedReflectionAccessPattern]
	[SkipKeptItemsValidation]
	class GetNestedTypeOnAllAnnotatedType
	{
		[RecognizedReflectionAccessPattern]
		static void Main ()
		{
			TestOnAllAnnotatedParameter (typeof (GetNestedTypeOnAllAnnotatedType));
			TestOnNonAllAnnotatedParameter (typeof (GetNestedTypeOnAllAnnotatedType));
			TestWithBindingFlags (typeof (GetNestedTypeOnAllAnnotatedType));
			TestWithUnknownBindingFlags (BindingFlags.Public, typeof (GetNestedTypeOnAllAnnotatedType));
			TestUnsupportedBindingFlags (typeof (GetNestedTypeOnAllAnnotatedType));
			TestWithNull ();
			TestIfElse (1, typeof (GetNestedTypeOnAllAnnotatedType), typeof (GetNestedTypeOnAllAnnotatedType));
			TestSwitchAllValid (1, typeof (GetNestedTypeOnAllAnnotatedType));
			TestOnKnownTypeOnly ();
		}

		[RecognizedReflectionAccessPattern]
		static void TestOnAllAnnotatedParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (NestedType));
			nestedType.RequiresAll ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresAll), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		static void TestOnNonAllAnnotatedParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (NestedType));
			nestedType.RequiresAll ();
		}

		[RecognizedReflectionAccessPattern]
		static void TestWithBindingFlags ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (NestedType), BindingFlags.Public);
			nestedType.RequiresAll ();
		}

		[RecognizedReflectionAccessPattern]
		static void TestWithUnknownBindingFlags (BindingFlags bindingFlags, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (NestedType), bindingFlags);
			nestedType.RequiresAll ();
		}

		[RecognizedReflectionAccessPattern]
		static void TestUnsupportedBindingFlags ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (NestedType), BindingFlags.IgnoreCase);
			nestedType.RequiresAll ();
		}

		[RecognizedReflectionAccessPattern]
		static void TestWithNull ()
		{
			Type parentType = null;
			var nestedType = parentType.GetNestedType (nameof (NestedType));
			nestedType.RequiresAll ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresAll), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		static void TestIfElse (int number, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentWithAll, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)] Type parentWithoutAll)
		{
			Type typeOfParent;
			if (number == 1) {
				typeOfParent = parentWithAll;
			} else {
				typeOfParent = parentWithoutAll;
			}
			var nestedType = typeOfParent.GetNestedType (nameof (NestedType));
			nestedType.RequiresAll ();
		}

		[RecognizedReflectionAccessPattern]
		static void TestSwitchAllValid (int number, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentWithAll)
		{
			Type typeOfParent = number switch {
				1 => parentWithAll,
				2 => null,
				3 => typeof (GetNestedTypeOnAllAnnotatedType)
			};

			var nestedType = typeOfParent.GetNestedType (nameof (NestedType));
			nestedType.RequiresAll ();
		}

		[RecognizedReflectionAccessPattern]
		static void TestOnKnownTypeOnly ()
		{
			typeof (GetNestedTypeOnAllAnnotatedType).GetNestedType (nameof (NestedType)).RequiresAll ();
		}

		private class NestedType
		{
			NestedType () { }
			public static int PublicStaticInt;
			public void Method () { }
			int Prop { get; set; }
		}
	}
}
