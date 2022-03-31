// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[ExpectedNoWarnings]
	public class NestedTypesUsedViaReflection
	{
		public static void Main ()
		{
			TestGetNestedTypes ();
			TestByBindingFlags ();
			TestByUnknownBindingFlags (BindingFlags.Public);
			TestNullType ();
			TestNoValue ();
			TestDataFlowType ();
			TestDataFlowWithAnnotation (typeof (MyType));
			TestIgnoreCaseBindingFlags ();
			TestUnsupportedBindingFlags ();
		}

		[Kept]
		public static class NestedType { }

		[Kept]
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
		static void TestByBindingFlags ()
		{
			_ = typeof (NestedTypesUsedViaReflection).GetNestedTypes (BindingFlags.Public);
		}

		[Kept]
		static void TestByUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// Since the binding flags are not known linker should mark all nested types on the type
			_ = typeof (UnknownBindingFlags).GetNestedTypes (bindingFlags);
		}

		[Kept]
		static void TestNullType ()
		{
			Type type = null;
			_ = type.GetNestedTypes (BindingFlags.Public);
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			_ = noValue.GetNestedTypes (BindingFlags.Public);
		}

		[Kept]
		static Type FindType ()
		{
			return null;
		}

		[ExpectedWarning ("IL2075", "FindType", "GetNestedTypes")]
		[Kept]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			_ = type.GetNestedTypes (BindingFlags.Public);
		}

		[Kept]
		private static void TestDataFlowWithAnnotation ([KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))][DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)] Type type)
		{
			_ = type.GetNestedTypes (BindingFlags.Public | BindingFlags.Static);
		}

		[Kept]
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
