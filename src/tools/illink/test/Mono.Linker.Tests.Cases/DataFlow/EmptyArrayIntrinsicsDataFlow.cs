using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the UnrecognizedReflectionAccessPattern attributes.
	[SkipKeptItemsValidation]
	class EmptyArrayIntrinsicsDataFlow
	{
		static void Main ()
		{
			TestGetPublicParameterlessConstructorWithEmptyTypes ();
			TestGetPublicParameterlessConstructorWithArrayEmpty ();
			TestGetPublicParameterlessConstructorWithUnknownArray ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetMethod), new Type[] { typeof (string) })]
		static void TestGetPublicParameterlessConstructorWithEmptyTypes ()
		{
			s_typeWithKeptPublicParameterlessConstructor.GetConstructor (Type.EmptyTypes);
			s_typeWithKeptPublicParameterlessConstructor.GetMethod ("Foo");
		}

		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetMethod), new Type[] { typeof (string) })]
		static void TestGetPublicParameterlessConstructorWithArrayEmpty ()
		{
			s_typeWithKeptPublicParameterlessConstructor.GetConstructor (Array.Empty<Type> ());
			s_typeWithKeptPublicParameterlessConstructor.GetMethod ("Foo");
		}

		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (Type[]) })]
		static void TestGetPublicParameterlessConstructorWithUnknownArray ()
		{
			s_typeWithKeptPublicParameterlessConstructor.GetConstructor (s_localEmptyArrayInvisibleToAnalysis);
		}

		static Type[] s_localEmptyArrayInvisibleToAnalysis = Type.EmptyTypes;

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		static Type s_typeWithKeptPublicParameterlessConstructor = typeof (EmptyArrayIntrinsicsDataFlow);
	}
}
