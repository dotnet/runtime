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
		static string UnknownString ()
		{
			return "unknownstring";
		}

		[Kept]
		static void Branch_SystemTypeValueNode_KnownStringValue_NonStatic ()
		{
			var expr = Expression.Field (Expression.Parameter (typeof (int), "somename"), typeof (ExpressionFieldString), "TestName1");
		}

		[Kept]
		static void Branch_SystemTypeValueNode_KnownStringValue_SaticOnly ()
		{
			var expr = Expression.Field (null, typeof (ExpressionFieldString), "TestOnlyStatic1");
			expr = Expression.Field (null, typeof (ExpressionFieldString), "TestOnlyStatic2");
		}

		[Kept]
		static void Branch_SystemTypeValueNode_UnknownStringValue ()
		{
			var expr = Expression.Field (null, typeof (ExpressionFieldString), UnknownString ());
		}

		[Kept]
		static void Branch_NullValueNode ()
		{
			var expr = Expression.Field (null, UnknownString () == "unknownstring" ? null : typeof (ExpressionFieldString), "TestName1");
		}

		[Kept]
		static void Branch_MethodParameterValueNode (Type T, string S)
		{
			var expr = Expression.Field (null, T, "TestName1");
			expr = Expression.Field (null, typeof (ExpressionFieldString), S);
		}

		[Kept]
		static void Branch_UnrecognizedPatterns ()
		{
			var expr = Expression.Field (null, Type.GetType ("Foo"), "TestName1");
		}
	}
}
