using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[Reference ("System.Core.dll")]
	public class ExpressionFieldString
	{
		public static void Main ()
		{
			Branch_SystemTypeValueNode_KnownStringValue_NonStatic ();
			Branch_SystemTypeValueNode_KnownStringValue_SaticOnly ();
			Branch_SystemTypeValueNode_UnknownStringValue ();
			Branch_NullValueNode ();
			Branch_MethodParameterValueNode (typeof (ExpressionFieldString), "Foo");
			Branch_UnrecognizedPatterns ();
		}

		[Kept]
		private static int TestOnlyStatic1;

		private int TestOnlyStatic2;

		[Kept]
		private int TestName1;

		[Kept]
		private int TestName2;

		private int TestName3;

		private int TestName4;

		private int TestName5;

		[Kept]
		class A
		{
			[Kept]
			static int Foo;
		}

		[Kept]
		class B
		{
			[Kept]
			static int Foo;
		}

		[Kept]
		static string UnknownString ()
		{
			return "unknownstring";
		}

		[Kept]
		static void Branch_SystemTypeValueNode_KnownStringValue_NonStatic ()
		{
			TestFieldName (0);
			TestFieldName (1);
			TestType (0);
			TestType (1);
		}

		[Kept]
		static void TestFieldName (int i)
		{
			string FieldName = string.Empty;
			switch (i) {
				case 0:
					FieldName = "TestName1";
					break;
				case 1:
					FieldName = "TestName2";
					break;
				default:
					break;
			}

			Expression.Field (Expression.Parameter (typeof (int), "somename"), typeof (ExpressionFieldString), FieldName);
		}

		[Kept]
		static void TestType (int i)
		{
			Type T = (Type)null;
			switch (i) {
				case 0:
					T = typeof (A);
					break;
				case 1:
					T = typeof (B);
					break;
				default:
					break;
			}

			Expression.Field (null, T, "Foo");
		}

		[Kept]
		static void Branch_SystemTypeValueNode_KnownStringValue_SaticOnly ()
		{
			var expr = Expression.Field (null, typeof (ExpressionFieldString), "TestOnlyStatic1");
			expr = Expression.Field (null, typeof (ExpressionFieldString), "TestOnlyStatic2");
		}

		[UnrecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Field), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
		[Kept]
		static void Branch_SystemTypeValueNode_UnknownStringValue ()
		{
			var expr = Expression.Field (null, typeof (ExpressionFieldString), UnknownString ());
		}

		[Kept]
		static void Branch_NullValueNode ()
		{
			var expr = Expression.Field (null, (Type)null, "TestName3");
		}

		[UnrecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Field), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
		[Kept]
		static void Branch_MethodParameterValueNode (Type T, string s)
		{
			var expr = Expression.Field (null, T, "TestName4");
			expr = Expression.Field (null, typeof (ExpressionFieldString), s);
		}

		[UnrecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Field), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
		[Kept]
		static void Branch_UnrecognizedPatterns ()
		{
			var expr = Expression.Field (null, Type.GetType ("Foo"), "TestName5");
		}
	}
}
