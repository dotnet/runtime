using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.PreserveDependencies
{
#if !NETCOREAPP
	[IgnoreTestCase ("This test checks that PreserveDependency correctly issues a warning on .NET Core where it is deprecated.")]
#endif
	[SetupCompileBefore ("FakeSystemAssembly.dll", new[] { "Dependencies/PreserveDependencyAttribute.cs" })]
	[LogContains ("IL2033: Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyDeprecated.B.Method(): PreserveDependencyAttribute is deprecated. Use DynamicDependencyAttribute instead.")]
	[LogContains ("IL2033: Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyDeprecated.B.SameContext(): PreserveDependencyAttribute is deprecated. Use DynamicDependencyAttribute instead.")]
	[LogContains ("IL2033: Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyDeprecated.B.Broken(): PreserveDependencyAttribute is deprecated. Use DynamicDependencyAttribute instead.")]
	[LogContains ("IL2033: Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyDeprecated.B.Conditional(): PreserveDependencyAttribute is deprecated. Use DynamicDependencyAttribute instead.")]
	class PreserveDependencyDeprecated
	{
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
			[PreserveDependency ("Dependency1()", "Mono.Linker.Tests.Cases.PreserveDependencies.D")]
			[PreserveDependency ("Dependency2`1    (   T[]  ,   System.Int32  )  ", "Mono.Linker.Tests.Cases.PreserveDependencies.D")]
			[PreserveDependency (".ctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.D")] // To avoid lazy body marking stubbing
			[PreserveDependency ("field", "Mono.Linker.Tests.Cases.PreserveDependencies.D")]
			[PreserveDependency ("NextOne (Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyDeprecated+Nested&)", "Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyDeprecated+Nested")]
			[PreserveDependency (".cctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyDeprecated+Nested")]
			// Dependency on a property itself should be expressed as a dependency on one or both accessor methods
			[PreserveDependency ("get_Property()", "Mono.Linker.Tests.Cases.PreserveDependencies.D")]
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
			[PreserveDependency ("MissingType", "Mono.Linker.Tests.Cases.PreserveDependencies.MissingType")]
			[PreserveDependency ("MissingMethod", "Mono.Linker.Tests.Cases.PreserveDependencies.D")]
			[PreserveDependency ("Dependency2`1 (T, System.Int32, System.Object)", "Mono.Linker.Tests.Cases.PreserveDependencies.D")]
			[PreserveDependency ("")]
			[PreserveDependency (".ctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyDeprecated+NestedStruct")]
			[PreserveDependency (".cctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.D")]
			public static void Broken ()
			{
			}

			[Kept]
			[PreserveDependency ("ConditionalTest()", "Mono.Linker.Tests.Cases.PreserveDependencies.D", Condition = "don't have it")]
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

			[Kept]
			static Nested ()
			{

			}
		}

		struct NestedStruct
		{
			public string Name;

			public NestedStruct (string name)
			{
				Name = name;
			}
		}
	}

	[KeptMember (".ctor()")]
	class D
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
