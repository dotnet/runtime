// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

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
			RequiresAll (nestedType);
		}

		[UnrecognizedReflectionAccessPattern (typeof (GetNestedTypeOnAllAnnotatedType), nameof (RequiresAll), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		static void TestOnNonAllAnnotatedParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (NestedType));
			RequiresAll (nestedType);
		}

		[RecognizedReflectionAccessPattern]
		static void TestWithBindingFlags ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (NestedType), BindingFlags.Public);
			RequiresAll (nestedType);
		}

		[RecognizedReflectionAccessPattern]
		static void TestWithUnknownBindingFlags (BindingFlags bindingFlags, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (NestedType), bindingFlags);
			RequiresAll (nestedType);
		}

		[RecognizedReflectionAccessPattern]
		static void TestUnsupportedBindingFlags ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentType)
		{
			var nestedType = parentType.GetNestedType (nameof (NestedType), BindingFlags.IgnoreCase);
			RequiresAll (nestedType);
		}

		[RecognizedReflectionAccessPattern]
		static void TestWithNull ()
		{
			Type parentType = null;
			var nestedType = parentType.GetNestedType (nameof (NestedType));
			RequiresAll (nestedType);
		}

		[UnrecognizedReflectionAccessPattern (typeof (GetNestedTypeOnAllAnnotatedType), nameof (RequiresAll), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		static void TestIfElse (int number, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentWithAll, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)] Type parentWithoutAll)
		{
			Type typeOfParent;
			if (number == 1) {
				typeOfParent = parentWithAll;
			} else {
				typeOfParent = parentWithoutAll;
			}
			var nestedType = typeOfParent.GetNestedType (nameof (NestedType));
			RequiresAll (nestedType);
		}

		[RecognizedReflectionAccessPattern]
		static void TestSwitchAllValid (int number, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parentWithAll)
		{
			Type typeOfParent = number switch
			{
				1 => parentWithAll,
				2 => null,
				3 => typeof (GetNestedTypeOnAllAnnotatedType)
			};

			var nestedType = typeOfParent.GetNestedType (nameof (NestedType));
			RequiresAll (nestedType);
		}

		[RecognizedReflectionAccessPattern]
		static void TestOnKnownTypeOnly ()
		{
			RequiresAll (typeof (GetNestedTypeOnAllAnnotatedType).GetNestedType (nameof (NestedType)));
		}

		static void RequiresAll ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
		{
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
