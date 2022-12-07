using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.PreserveDependencies
{
	[SetupCompileBefore ("FakeSystemAssembly.dll", new[] { "Dependencies/PreserveDependencyAttribute.cs" })]
	[LogContains ("Could not resolve dependency type 'Mono.Linker.Tests.Cases.PreserveDependencies.MissingType'")]
	[LogContains ("Could not resolve dependency member 'MissingMethod' declared in type 'Mono.Linker.Tests.Cases.PreserveDependencies.C'")]
	[LogContains ("Could not resolve dependency member 'Dependency2`1' declared in type 'Mono.Linker.Tests.Cases.PreserveDependencies.C'")]
	[LogContains ("Could not resolve dependency member '' declared in type 'Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyMethod.B'")]
	[LogContains ("Could not resolve dependency member '.ctor' declared in type 'Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyMethod.NestedStruct'")]
	[LogContains ("Could not resolve dependency member '.cctor' declared in type 'Mono.Linker.Tests.Cases.PreserveDependencies.C'")]
	class PreserveDependencyMethod
	{
		public static void Main ()
		{
			new B (); // Needed to avoid lazy body marking stubbing

			B.Method ();
			B.SameContext ();
			B.Broken ();
			B.Conditional ();

			TestRequiresInPreserveDependency ();
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
			[PreserveDependency (".cctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyMethod+Nested")]
			// Dependency on a property itself should be expressed as a dependency on one or both accessor methods
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
			[PreserveDependency ("MissingType", "Mono.Linker.Tests.Cases.PreserveDependencies.MissingType")]
			[PreserveDependency ("MissingMethod", "Mono.Linker.Tests.Cases.PreserveDependencies.C")]
			[PreserveDependency ("Dependency2`1 (T, System.Int32, System.Object)", "Mono.Linker.Tests.Cases.PreserveDependencies.C")]
			[PreserveDependency ("")]
			[PreserveDependency (".ctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.PreserveDependencyMethod+NestedStruct")]
			[PreserveDependency (".cctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.C")]
			public static void Broken ()
			{
			}

			[Kept]
			[PreserveDependency ("ConditionalTest()", "Mono.Linker.Tests.Cases.PreserveDependencies.C", Condition = "don't have it")]
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

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode ("Message for --RequiresUnreferencedCodeInPreserveDependency--")]
		static void RequiresUnreferencedCodeInPreserveDependency ()
		{
		}

		[Kept]
		[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeInPreserveDependency--")]
		[PreserveDependency ("RequiresUnreferencedCodeInPreserveDependency")]
		static void TestRequiresInPreserveDependency ()
		{
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