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
			Branch_NullValueNode ();
			Branch_SystemTypeValueNode_UnknownStringValue ();
			Branch_MethodParameterValueNode (typeof (ExpressionFieldString), "Foo");
			Branch_UnrecognizedPatterns ();
			// TODO
			Expression.Field(null, typeof(ADerived), "_protectedFieldOnBase");
			Expression.Field(null, typeof(ADerived), "_publicFieldOnBase");
		}

		[Kept]
		static void Branch_SystemTypeValueNode_KnownStringValue_NonStatic ()
		{
			TestFieldName (0);
			TestFieldName (1);
			TestType (0);
			TestType (1);
			StaticFieldExpected ();
		}

		[Kept]
		static void Branch_NullValueNode ()
		{
			var expr = Expression.Field (null, (Type)null, "TestName1");
		}

		#region RecognizedReflectionAccessPatterns
		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Field), new Type [] { typeof (Expression), typeof (Type), typeof (string) },
			typeof (ExpressionFieldString), nameof (TestOnlyStatic1))]
		[Kept]
		static void Branch_SystemTypeValueNode_KnownStringValue_SaticOnly ()
		{
			var expr = Expression.Field (null, typeof (ExpressionFieldString), "TestOnlyStatic1");
		}

		[UnrecognizedReflectionAccessPattern ( // Expression.Field (Expression, Type, null);
				typeof (Expression), nameof (Expression.Field), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Field), new Type [] { typeof (Expression), typeof (Type), typeof (string) },
			typeof (ExpressionFieldString), nameof (TestName2))]
		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Field), new Type [] { typeof (Expression), typeof (Type), typeof (string) },
			typeof (ExpressionFieldString), nameof (TestName3))]
		[Kept]
		static void TestFieldName (int i)
		{
			string FieldName = null;
			switch (i) {
				case 0:
					FieldName = "TestName2";
					break;
				case 1:
					FieldName = "TestName3";
					break;
				default:
					break;
			}

			Expression.Field (Expression.Parameter (typeof (int), "somename"), typeof (ExpressionFieldString), FieldName);
		}

		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Field), new Type [] { typeof (Expression), typeof (Type), typeof (string) },
			typeof (A), "Foo")]
		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Field), new Type [] { typeof (Expression), typeof (Type), typeof (string) },
			typeof (B), "Foo")]
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
		#endregion

		#region UnrecognizedReflectionAccessPatterns
		[UnrecognizedReflectionAccessPattern (
				typeof (Expression), nameof (Expression.Field), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
		[Kept]
		static void StaticFieldExpected ()
		{
			var expr = Expression.Field (null, typeof (ExpressionFieldString), "TestOnlyStatic2");
		}


		[UnrecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Field), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
		[Kept]
		static void Branch_SystemTypeValueNode_UnknownStringValue ()
		{
			var expr = Expression.Field (null, typeof (ExpressionFieldString), UnknownString ());
		}

		[UnrecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Field), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
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
		#endregion

		#region Helpers
		[Kept]
		private static int TestOnlyStatic1;

		private int TestOnlyStatic2;

		private int TestName1;

		[Kept]
		private int TestName2;

		[Kept]
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
		#endregion
	}

	[Kept]
	class ABase
	{
		// [Kept] - TODO - should be kept: https://github.com/mono/linker/issues/1042
		protected static bool _protectedFieldOnBase;

		// [Kept] - TODO - should be kept: https://github.com/mono/linker/issues/1042
		public static bool _publicFieldOnBase;
	}

	[Kept]
	[KeptBaseType (typeof (ABase))]
	class ADerived : ABase
	{
	}
}
