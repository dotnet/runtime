using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System.Linq.Expressions;
using System;
using System.Linq;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System.Runtime.CompilerServices;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[Reference ("System.Core.dll")]
	public class ExpressionNewType
	{
		public static void Main ()
		{
			Branch_SystemTypeValueNode ();
			Branch_NullValueNode ();
			Branch_MethodParameterValueNode (typeof (T1));
			Branch_UnrecognizedPatterns ();
		}

		[Kept]
		static Type GetType ()
		{
			return typeof (T1);
		}

		[Kept]
		static int Helper1 ()
		{
			return 5;
		}

		[Kept]
		static int Helper2 ()
		{
			return 7;
		}

		[Kept]
		class T1
		{
			[Kept]
			internal T1 ()
			{
			}
		}

		class T2
		{
		}

		[Kept]
		static void Branch_SystemTypeValueNode ()
		{
			var expr = Expression.New (typeof (T1));
		}

		[UnrecognizedReflectionAccessPattern (typeof (Expression), nameof (Expression.New), new Type [] { typeof (Type) })]
		[Kept]
		static void Branch_NullValueNode ()
		{
			var expr = Expression.New (Helper1 () + Helper2 () == 12 ? null : typeof (T1));
		}

		[UnrecognizedReflectionAccessPattern (typeof (Expression), nameof (Expression.New), new Type [] { typeof (Type) })]
		[Kept]
		static void Branch_MethodParameterValueNode (Type T)
		{
			var expr = Expression.New (T);
		}

		[UnrecognizedReflectionAccessPattern (typeof (Expression), nameof (Expression.New) , new Type [] { typeof (Type) })]
		[Kept]
		static void Branch_UnrecognizedPatterns ()
		{
			var expr = Expression.New (Type.GetType ("T1"));
			expr = Expression.New (GetType ());
		}
	}
}
