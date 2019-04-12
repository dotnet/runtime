using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.PreserveDependencies {
	class PreserveDependencyMethod {
		public static void Main ()
		{
			new B (); // Needed to avoid lazy body marking stubbing
			
			B.Method ();
			B.SameContext ();
			B.Broken ();
			B.Conditional ();
		}

		[KeptMember (".ctor()")]
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
			[PreserveDependency (".ctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.C")] // To avoid lazy body marking stubbing
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

	[KeptMember (".ctor()")]
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