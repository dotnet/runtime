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
			Branch_SystemTypeValueNode (0);
			Branch_SystemTypeValueNode (1);
			Branch_NullValueNode ();
			Branch_MethodParameterValueNode (typeof (C));
			Branch_UnrecognizedPatterns ();
		}

		[Kept]
		class A
		{
			[Kept]
			A () { }
		}

		[Kept]
		class B
		{
			[Kept]
			B () { }
		}

		[Kept]
		class C { }

		[Kept]
		class D { }

		class RemovedType { }

		[Kept]
		static Type GetType ()
		{
			return typeof (D);
		}

		[Kept]
		static void Branch_SystemTypeValueNode (int i)
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

			Expression.New (T);
		}

		[Kept]
		static void Branch_NullValueNode ()
		{
			Expression.New (5 + 7 == 12 ? null : typeof (RemovedType));
		}

		[UnrecognizedReflectionAccessPattern (typeof (Expression), nameof (Expression.New), new Type [] { typeof (Type) })]
		[Kept]
		static void Branch_MethodParameterValueNode (Type T)
		{
			Expression.New (T);
		}

		[UnrecognizedReflectionAccessPattern (typeof (Expression), nameof (Expression.New), new Type [] { typeof (Type) })]
		[Kept]
		static void Branch_UnrecognizedPatterns ()
		{
			Expression.New (Type.GetType ("RemovedType"));
			Expression.New (GetType ());
		}
	}
}
