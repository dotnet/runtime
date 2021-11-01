// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	public class NestedTypesUsedViaReflection
	{
		public static void Main ()
		{
			TestGetNestedTypes ();
			TestByBindingFlags ();
			TestByUnknownBindingFlags (BindingFlags.Public);
			TestNullType ();
			TestDataFlowType ();
			TestDataFlowWithAnnotation (typeof (MyType));
			TestIgnoreCaseBindingFlags ();
			TestUnsupportedBindingFlags ();
		}

		[Kept]
		public static class NestedType { }

		[Kept]
		[RecognizedReflectionAccessPattern]
		static void TestGetNestedTypes ()
		{
			_ = typeof (NestedTypesUsedViaReflection).GetNestedType (nameof (NestedType));
		}

		static class PrivateUnreferencedNestedType { }

		[Kept]
		public static class PublicNestedType { }

		private static class PrivateNestedType { }

		protected static class ProtectedNestedType { }

		[Kept]
		[RecognizedReflectionAccessPattern]
		static void TestByBindingFlags ()
		{
			_ = typeof (NestedTypesUsedViaReflection).GetNestedTypes (BindingFlags.Public);
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		static void TestByUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// Since the binding flags are not known linker should mark all nested types on the type
			_ = typeof (UnknownBindingFlags).GetNestedTypes (bindingFlags);
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		static void TestNullType ()
		{
			Type type = null;
			_ = type.GetNestedTypes (BindingFlags.Public);
		}

		[Kept]
		static Type FindType ()
		{
			return null;
		}

		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetNestedTypes), new Type[] { typeof (BindingFlags) },
			messageCode: "IL2075", message: new string[] { "FindType", "GetNestedTypes" })]
		[Kept]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			_ = type.GetNestedTypes (BindingFlags.Public);
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		private static void TestDataFlowWithAnnotation ([KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))][DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)] Type type)
		{
			_ = type.GetNestedTypes (BindingFlags.Public | BindingFlags.Static);
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		static void TestIgnoreCaseBindingFlags ()
		{
			_ = typeof (IgnoreCaseClass).GetNestedTypes (BindingFlags.IgnoreCase | BindingFlags.Public);
		}

		[Kept]
		static void TestUnsupportedBindingFlags ()
		{
			_ = typeof (SuppressChangeTypeClass).GetNestedTypes (BindingFlags.SuppressChangeType);
		}

		[Kept]
		private class UnknownBindingFlags
		{
			[Kept]
			public static class PublicNestedType { }

			[Kept]
			private static class PrivateNestedType { }
		}

		[Kept]
		private class MyType
		{
			[Kept]
			public static class publicNestedType { }

			private static class privateNestedType { }
		}

		[Kept]
		private class IgnoreCaseClass
		{
			[Kept]
			public static class IgnoreCasePublicNestedType { }

			[Kept]
			private static class MarkedDueToIgnoreCase { }
		}

		[Kept]
		private class SuppressChangeTypeClass
		{
			[Kept]
			public static class SuppressChangeTypeNestedType { }

			[Kept]
			private static class MarkedDueToSuppressChangeType { }
		}
	}
}
