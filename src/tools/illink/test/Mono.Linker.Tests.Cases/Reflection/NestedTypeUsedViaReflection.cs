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
			ByName.Test ();
			PrivateByName.Test ();
			NullName.Test ();
			EmptyName.Test ();
			NoValueName.Test ();
			WithBindingFlags.Test ();
			UnknownBindingFlags.Test (BindingFlags.Public);
			UnknownBindingFlagsAndName.Test (BindingFlags.Public, "DoesntMatter");
			NonExistingName.Test ();
			TestNullType ();
			TestNoValue ();
			IgnoreCaseBindingFlags.Test ();
			FailIgnoreCaseBindingFlags.Test ();
			UnsupportedBindingFlags.Test ();

			MemberOnNestedType.Test ();
		}

		[Kept]
		static class ByName
		{
			[Kept]
			public static class NestedType { }

			[Kept]
			public static void Test ()
			{
				_ = typeof (ByName).GetNestedType (nameof (NestedType));
			}
		}

		static class PrivateByName
		{
			static class PrivateUnreferencedNestedType { }

			[Kept]
			public static void Test ()
			{
				_ = typeof (PrivateByName).GetNestedType (nameof (PrivateUnreferencedNestedType)); // This will not mark the nested type as GetNestedType(string) only returns public
				_ = typeof (PrivateByName).GetNestedType (nameof (PrivateUnreferencedNestedType), BindingFlags.Public);
			}
		}

		static class NullName
		{
			public static class UnusedNestedType { }

			[Kept]
			public static void Test ()
			{
				_ = typeof (NullName).GetNestedType (null);
			}
		}

		static class EmptyName
		{
			public static class UnusedNestedType { }

			[Kept]
			public static void Test ()
			{
				_ = typeof (EmptyName).GetNestedType (string.Empty);
			}
		}

		static class NoValueName
		{
			public static class UnusedNestedType { }

			[Kept]
			public static void Test ()
			{
				Type t = null;
				string noValue = t.AssemblyQualifiedName;
				var method = typeof (NoValueName).GetNestedType (noValue);
			}
		}

		static class WithBindingFlags
		{
			[Kept]
			public static class PublicNestedType { }

			[Kept]
			private static class PrivateNestedType { }

			[Kept]
			protected static class ProtectedNestedType { }

			public static class UnusedPublicNestedType { }

			[Kept]
			public static void Test ()
			{
				_ = typeof (WithBindingFlags).GetNestedType (nameof (PrivateNestedType), BindingFlags.NonPublic);
				_ = typeof (WithBindingFlags).GetNestedType (nameof (PublicNestedType), BindingFlags.Public);
				_ = typeof (WithBindingFlags).GetNestedType (nameof (ProtectedNestedType), BindingFlags.NonPublic);
			}
		}

		static class UnknownBindingFlags
		{
			[Kept]
			public static class PublicNestedType { }

			[Kept]
			public static class AnotherPublicNestedType { }

			[Kept]
			private static class PrivateNestedType { }

			[Kept]
			protected static class ProtectedNestedType { }

			[Kept]
			public static void Test (BindingFlags bindingFlags)
			{
				// Since the binding flags are not known linker should mark all nested types on the type
				_ = typeof (UnknownBindingFlags).GetNestedType (nameof (PublicNestedType), bindingFlags);
			}
		}

		static class UnknownBindingFlagsAndName
		{
			[Kept]
			public static class PublicNestedType { }

			[Kept]
			public static class AnotherPublicNestedType { }

			[Kept]
			private static class PrivateNestedType { }

			[Kept]
			protected static class ProtectedNestedType { }

			[Kept]
			public static void Test (BindingFlags bindingFlags, string name)
			{
				// Since the binding flags and name are not known linker should mark all nested types on the type
				_ = typeof (UnknownBindingFlagsAndName).GetNestedType (name, bindingFlags);
			}
		}

		static class NonExistingName
		{
			public static class UnusedNestedType { }


			[Kept]
			public static void Test ()
			{
				_ = typeof (NonExistingName).GetNestedType ("NonExisting");
			}
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
		private class MemberOnNestedType
		{
			[Kept]
			public static class PublicNestedTypeWithMembers
			{
				public static void UnusedMethod () { }

				[Kept]
				public static void UsedMethod () { }
			}

			[Kept]
			public static void Test ()
			{
				typeof (MemberOnNestedType).GetNestedType (nameof (PublicNestedTypeWithMembers)).GetMethod (nameof (PublicNestedTypeWithMembers.UsedMethod));
			}
		}

		[Kept]
		static class IgnoreCaseBindingFlags
		{
			[Kept]
			public static class IgnoreCasePublicNestedType { }

			[Kept]
			public static class MarkedDueToIgnoreCase { }

			[Kept]
			public static void Test ()
			{
				_ = typeof (IgnoreCaseBindingFlags).GetNestedType ("ignorecasepublicnestedtype", BindingFlags.IgnoreCase | BindingFlags.Public);
			}
		}

		[Kept]
		private class FailIgnoreCaseBindingFlags
		{
			public static class FailIgnoreCasePublicNestedType { }

			[Kept]
			public static void Test ()
			{
				_ = typeof (FailIgnoreCaseBindingFlags).GetNestedType ("failignorecasepublicnestedtype", BindingFlags.Public);
			}
		}

		[Kept]
		private class UnsupportedBindingFlags
		{
			[Kept]
			public static class SuppressChangeTypeNestedType { }

			[Kept]
			private static class MarkedDueToSuppressChangeType { }

			[Kept]
			public static void Test ()
			{
				_ = typeof (UnsupportedBindingFlags).GetNestedType ("SuppressChangeTypeNestedType", BindingFlags.SuppressChangeType);
			}
		}
	}
}
