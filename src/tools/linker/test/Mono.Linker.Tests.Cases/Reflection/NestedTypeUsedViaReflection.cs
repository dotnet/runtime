// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[ExpectedNoWarnings]
	public class NestedTypeUsedViaReflection
	{
		public static void Main ()
		{
			TestByName ();
			TestPrivateByName ();
			TestNullName ();
			TestEmptyName ();
			TestNoValueName ();
			TestByBindingFlags ();
			TestByUnknownBindingFlags (BindingFlags.Public);
			TestByUnknownBindingFlagsAndName (BindingFlags.Public, "DoesntMatter");
			TestNonExistingName ();
			TestNullType ();
			TestNoValue ();
			TestIgnoreCaseBindingFlags ();
			TestFailIgnoreCaseBindingFlags ();
			TestUnsupportedBindingFlags ();
		}

		[Kept]
		public static class NestedType { }

		[Kept]
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
		static void TestNullName ()
		{
			_ = typeof (NestedTypeUsedViaReflection).GetNestedType (null);
		}

		[Kept]
		static void TestEmptyName ()
		{
			_ = typeof (NestedTypeUsedViaReflection).GetNestedType (string.Empty);
		}

		[Kept]
		static void TestNoValueName ()
		{
			Type t = null;
			string noValue = t.AssemblyQualifiedName;
			var method = typeof (NestedTypeUsedViaReflection).GetNestedType (noValue);
		}

		[Kept]
		public static class PublicNestedType { }

		[Kept]
		private static class PrivateNestedType { }

		[Kept]
		protected static class ProtectedNestedType { }

		[Kept]
		static void TestByBindingFlags ()
		{
			_ = typeof (NestedTypeUsedViaReflection).GetNestedType (nameof (PrivateNestedType), BindingFlags.NonPublic);
			_ = typeof (NestedTypeUsedViaReflection).GetNestedType (nameof (PublicNestedType), BindingFlags.Public);
			_ = typeof (NestedTypeUsedViaReflection).GetNestedType (nameof (ProtectedNestedType), BindingFlags.NonPublic);
		}

		[Kept]
		static void TestByUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// Since the binding flags are not known linker should mark all nested types on the type
			_ = typeof (UnknownBindingFlags).GetNestedType (nameof (PublicNestedType), bindingFlags);
		}

		[Kept]
		static void TestByUnknownBindingFlagsAndName (BindingFlags bindingFlags, string name)
		{
			// Since the binding flags and name are not known linker should mark all nested types on the type
			_ = typeof (UnknownBindingFlagsAndName).GetNestedType (name, bindingFlags);
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
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			var method = noValue.GetNestedType ("NestedType");
		}

		[Kept]
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
		private class UnknownBindingFlagsAndName
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
