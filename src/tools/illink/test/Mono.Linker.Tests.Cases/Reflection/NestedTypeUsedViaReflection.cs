// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	public class NestedTypeUsedViaReflection
	{
		public static void Main ()
		{
			TestByName ();
			TestPrivateByName ();
			TestByBindingFlags ();
			TestByUnknownBindingFlags (BindingFlags.Public);
			TestNonExistingName ();
			TestNullType ();
			TestIgnoreCaseBindingFlags ();
			TestFailIgnoreCaseBindingFlags ();
			TestUnsupportedBindingFlags ();
		}

		[Kept]
		public static class NestedType { }

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetNestedType), new Type[] { typeof (string) },
			typeof (NestedTypeUsedViaReflection.NestedType), null, (Type[]) null)]
		static void TestByName ()
		{
			_ = typeof (NestedTypeUsedViaReflection).GetNestedType (nameof (NestedType));
		}

		static class PrivateUnreferencedNestedType { }

		[Kept]
		static void TestPrivateByName ()
		{
			_ = typeof (NestedTypeUsedViaReflection).GetNestedType (nameof (PrivateUnreferencedNestedType)); // This will not mark the nested type as GetNestedType(string) only returns public
			_ = typeof (NestedTypeUsedViaReflection).GetNestedType (nameof (PrivateUnreferencedNestedType), BindingFlags.Public);
		}

		[Kept]
		public static class PublicNestedType { }

		[Kept]
		private static class PrivateNestedType { }

		[Kept]
		protected static class ProtectedNestedType { }

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetNestedType), new Type[] { typeof (string), typeof (BindingFlags) },
			typeof (NestedTypeUsedViaReflection.PrivateNestedType), null, (Type[]) null)]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetNestedType), new Type[] { typeof (string), typeof (BindingFlags) },
			typeof (NestedTypeUsedViaReflection.PublicNestedType), null, (Type[]) null)]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetNestedType), new Type[] { typeof (string), typeof (BindingFlags) },
			typeof (NestedTypeUsedViaReflection.ProtectedNestedType), null, (Type[]) null)]
		static void TestByBindingFlags ()
		{
			_ = typeof (NestedTypeUsedViaReflection).GetNestedType (nameof (PrivateNestedType), BindingFlags.NonPublic);
			_ = typeof (NestedTypeUsedViaReflection).GetNestedType (nameof (PublicNestedType), BindingFlags.Public);
			_ = typeof (NestedTypeUsedViaReflection).GetNestedType (nameof (ProtectedNestedType), BindingFlags.NonPublic);
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetNestedType), new Type[] { typeof (string), typeof (BindingFlags) },
			typeof (UnknownBindingFlags.PublicNestedType), null, (Type[]) null)]
		static void TestByUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// Since the binding flags are not known linker should mark all nested types on the type
			_ = typeof (UnknownBindingFlags).GetNestedType (nameof (PublicNestedType), bindingFlags);
		}

		[Kept]
		static void TestNonExistingName ()
		{
			_ = typeof (NestedTypeUsedViaReflection).GetNestedType ("NonExisting");
		}

		[Kept]
		static void TestNullType ()
		{
			Type type = null;
			_ = type.GetNestedType ("NestedType");
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetNestedType), new Type[] { typeof (string), typeof (BindingFlags) },
			typeof (IgnoreCaseClass.IgnoreCasePublicNestedType), null, (Type[]) null)]
		static void TestIgnoreCaseBindingFlags ()
		{
			_ = typeof (IgnoreCaseClass).GetNestedType ("ignorecasepublicnestedtype", BindingFlags.IgnoreCase | BindingFlags.Public);
		}

		[Kept]
		static void TestFailIgnoreCaseBindingFlags ()
		{
			_ = typeof (FailIgnoreCaseClass).GetNestedType ("failignorecasepublicnestedtype", BindingFlags.Public);
		}

		[Kept]
		static void TestUnsupportedBindingFlags ()
		{
			_ = typeof (SuppressChangeTypeClass).GetNestedType ("SuppressChangeTypeNestedType", BindingFlags.SuppressChangeType);
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
		private class IgnoreCaseClass
		{
			[Kept]
			public static class IgnoreCasePublicNestedType { }

			[Kept]
			public static class MarkedDueToIgnoreCase { }
		}

		[Kept]
		private class FailIgnoreCaseClass
		{
			public static class FailIgnoreCasePublicNestedType { }
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
