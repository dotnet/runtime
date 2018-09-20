using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.PreserveDependencies {
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
			[PreserveDependency ("Dependency1()", "Mono.Linker.Tests.Cases.PreserveDependencies.C")]
			[PreserveDependency ("Dependency2`1    (   T[]  ,   System.Int32  )  ", "Mono.Linker.Tests.Cases.PreserveDependencies.C")]
			[PreserveDependency ("field", "Mono.Linker.Tests.Cases.PreserveDependencies.C")]
			[PreserveDependency ("NextOne (Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyMethod+Nested&)", "Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyMethod+Nested")]
			[PreserveDependency ("Property", "Mono.Linker.Tests.Cases.PreserveDependencies.C")]
			[PreserveDependency ("get_Property()", "Mono.Linker.Tests.Cases.PreserveDependencies.C")]
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
			[PreserveDependency ("Missing", "Mono.Linker.Tests.Cases.Advanced.C")]
			[PreserveDependency ("Dependency2`1 (T, System.Int32, System.Object)", "Mono.Linker.Tests.Cases.Advanced.C")]
			[PreserveDependency ("")]
			public static void Broken ()
			{
			}

			[Kept]
			[PreserveDependency ("ConditionalTest()", "Mono.Linker.Tests.Cases.Advanced.C", Condition = "don't have it")]
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

		[Kept]
		[KeptBackingField]
		internal string Property { [Kept] get; set; }

		internal void ConditionalTest ()
		{
		}
	}
}