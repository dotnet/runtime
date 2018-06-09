using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Advanced {
	class PreserveDependencyMethod {
		public static void Main ()
		{
			B.Method ();
			B.SameContext ();
			B.Broken ();
			B.Conditional ();
		}

		class B
		{
			[Kept]
			int field;

			[Kept]
			void Method2 (out sbyte arg)
			{
				arg = 1;
			}

			[Kept]
			[PreserveDependency ("Mono.Linker.Tests.Cases.Advanced.C.Dependency1()")]
			[PreserveDependency ("Mono.Linker.Tests.Cases.Advanced.C.Dependency2`1    (   T[]  ,   System.Int32  )  ")]
			[PreserveDependency ("Mono.Linker.Tests.Cases.Advanced.C.field")]
			[PreserveDependency ("Mono.Linker.Tests.Cases.Advanced.PreserveDependencyMethod+Nested.NextOne (Mono.Linker.Tests.Cases.Advanced.PreserveDependencyMethod+Nested&)")]
			public static void Method ()
			{
			}

			[Kept]
			[PreserveDependency ("field")]
			[PreserveDependency ("Method2 (System.SByte&)")]
			public static void SameContext ()
			{
			}

			[Kept]
			[PreserveDependency ("Mono.Linker.Tests.Cases.Advanced.C.Missing")]
			[PreserveDependency ("Mono.Linker.Tests.Cases.Advanced.C.Dependency2`1 (T, System.Int32, System.Object)")]
			[PreserveDependency ("")]
			public static void Broken ()
			{
			}

			[Kept]
			[PreserveDependency ("Mono.Linker.Tests.Cases.Advanced.C.ConditionalTest()", "don't have it")]
			public static void Conditional ()
			{
			}
		}

		class Nested
		{
			[Kept]
			private static void NextOne (ref Nested arg1)
			{
			}
		}
	}

	class C
	{
		[Kept]
		internal string field;

		[Kept]
		internal void Dependency1 ()
		{
		}

		internal void Dependency1 (long arg1)
		{
		}

		[Kept]
		internal void Dependency2<T> (T[] arg1, int arg2)
		{
		}

		internal void ConditionalTest ()
		{
		}
	}
}