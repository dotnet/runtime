using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[Reference ("System.Core.dll")]
	[ExpectedNoWarnings]
	public class ExpressionFieldString
	{
		[ExpectedWarning ("IL2110", nameof (StaticWithDAM))]
		[ExpectedWarning ("IL2110", "_publicFieldOnBase")]
		[ExpectedWarning ("IL2072", nameof (Expression) + "." + nameof (Expression.Field))]
		public static void Main ()
		{
			Expression.Field (Expression.Parameter (typeof (int), ""), typeof (ExpressionFieldString), "InstanceField");
			Expression.Field (null, typeof (ExpressionFieldString), "StaticField");
			Expression.Field (null, typeof (ExpressionFieldString), "StaticWithDAM"); // IL2110
			Expression.Field (null, typeof (Derived), "_protectedFieldOnBase");
			Expression.Field (null, typeof (Derived), "_publicFieldOnBase"); // IL2110
			UnknownType.Test ();
			UnknownTypeNoAnnotation.Test ();
			UnknownString.Test ();
			Expression.Field (null, GetType (), "This string will not be reached"); // IL2072
			TestNullType ();
			TestNoValue ();
			TestNullString ();
			TestEmptyString ();
			TestNoValueString ();
		}

		[Kept]
		private int InstanceField;

		[Kept]
		static private int StaticField;

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static private Type StaticWithDAM;

		private int UnusedField;

		public static int StaticField1 { get => StaticField; set => StaticField = value; }

		[Kept]
		static Type GetType ()
		{
			return typeof (int);
		}

		[Kept]
		class UnknownType
		{
			[Kept]
			public static int Field1;

			[Kept]
			private int Field2;

			[Kept]
			public static void Test ()
			{
				Expression.Field (null, GetType (), "This string will not be reached");
			}

			[Kept]
			[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
			static Type GetType ()
			{
				return typeof (UnknownType);
			}
		}

		[Kept]
		class UnknownTypeNoAnnotation
		{
			public static int Field1;
			private int Field2;

			[ExpectedWarning ("IL2072", "'type'")]
			[Kept]
			public static void Test ()
			{
				Expression.Field (null, GetType (), "This string will not be reached");
			}

			[Kept]
			static Type GetType ()
			{
				return typeof (UnknownType);
			}
		}

		[Kept]
		class UnknownString
		{
			[Kept]
			private static int Field1;

			[Kept]
			public int Field2;

			[Kept]
			public static void Test ()
			{
				Expression.Field (null, typeof (UnknownString), GetString ());
			}

			[Kept]
			static string GetString ()
			{
				return "UnknownString";
			}
		}

		[Kept]
		static void TestNullType ()
		{
			Expression.Field (null, null, "This string will not be reached");
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			Expression.Field (null, noValue, "This string will not be reached");
		}

		[Kept]
		static void TestNullString ()
		{
			Expression.Field (null, typeof (Base), null);
		}

		[Kept]
		static void TestEmptyString ()
		{
			Expression.Field (null, typeof (Base), string.Empty);
		}

		[Kept]
		static void TestNoValueString ()
		{
			Type t = null;
			string noValue = t.AssemblyQualifiedName;
			Expression.Field (null, typeof (Base), noValue);
		}

		[Kept]
		class Base
		{
			[Kept]
			protected static bool _protectedFieldOnBase;

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public static Type _publicFieldOnBase;
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Derived : Base
		{
		}
	}
}
