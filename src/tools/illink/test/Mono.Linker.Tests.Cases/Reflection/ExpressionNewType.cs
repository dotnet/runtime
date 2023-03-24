using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[Reference ("System.Core.dll")]
	[ExpectedNoWarnings]
	public class ExpressionNewType
	{
		public static void Main ()
		{
			Branch_SystemTypeValueNode (0);
			Branch_SystemTypeValueNode (1);
			Branch_NullValueNode ();
			Branch_MethodParameterValueNode (typeof (C));
			Branch_UnrecognizedPatterns ();
			TestNullType ();
			TestNoValue ();
		}

		[Kept]
		static void Branch_NullValueNode ()
		{
			Expression.New (5 + 7 == 12 ? null : typeof (RemovedType));
		}

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

		[ExpectedWarning ("IL2067", nameof (Expression) + "." + nameof (Expression.New))]
		[Kept]
		static void Branch_MethodParameterValueNode (Type T)
		{
			Expression.New (T);
		}

		[ExpectedWarning ("IL2072", nameof (Expression) + "." + nameof (Expression.New), nameof (ExpressionNewType) + "." + nameof (ExpressionNewType.GetType))]
		[Kept]
		static void Branch_UnrecognizedPatterns ()
		{
			// Note that "RemovedType" will not resolve here since the type declared below with the same name is nested type and so its real name is "ExpressionNewType+RemovedType"
			// This should not warn - we choose to not warn if we can't resolve a type (and anything which it's used for)
			Expression.New (Type.GetType ("RemovedType"));
			Expression.New (GetType ());
		}

		[Kept]
		static void TestNullType ()
		{
			Type t = null;
			Expression.New (t);
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			Expression.New (noValue);
		}

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
