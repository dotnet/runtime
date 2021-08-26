using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

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
		static void Branch_NullValueNode ()
		{
			Expression.New (5 + 7 == 12 ? null : typeof (RemovedType));
		}

		#region RecognizedReflectionAccessPatterns
		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.New), new Type[] { typeof (Type) }, typeof (A), "A", new Type[0])]
		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.New), new Type[] { typeof (Type) }, typeof (B), "B", new Type[0])]
		[Kept]
		static void Branch_SystemTypeValueNode (int i)
		{
			Type T = (Type) null;
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
		#endregion

		#region UnrecognizedReflectionAccessPatterns
		[UnrecognizedReflectionAccessPattern (typeof (Expression), nameof (Expression.New), new Type[] { typeof (Type) }, messageCode: "IL2067")]
		[Kept]
		static void Branch_MethodParameterValueNode (Type T)
		{
			Expression.New (T);
		}

		[UnrecognizedReflectionAccessPattern (typeof (Expression), nameof (Expression.New), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		[UnrecognizedReflectionAccessPattern (typeof (Expression), nameof (Expression.New), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		[Kept]
		static void Branch_UnrecognizedPatterns ()
		{
			Expression.New (Type.GetType ("RemovedType"));
			Expression.New (GetType ());
		}
		#endregion

		#region Helpers
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
		#endregion
	}
}
