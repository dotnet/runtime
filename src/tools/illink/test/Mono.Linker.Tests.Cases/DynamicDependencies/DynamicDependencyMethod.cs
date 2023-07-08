using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	class DynamicDependencyMethod
	{
		public static void Main ()
		{
			new B (); // Needed to avoid lazy body marking stubbing

			B.Method ();
			B.SameContext ();
			B.Broken ();
			B.Conditional ();
#if NATIVEAOT
			ReferenceViaReflection.Test ();
#endif
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
			[DynamicDependency ("Dependency1()", typeof (C))]
			[DynamicDependency ("Dependency2``1(``0[],System.Int32", typeof (C))]
			[DynamicDependency ("Dependency3", typeof (C))]
			[DynamicDependency ("RecursiveDependency", typeof (C))]
			[DynamicDependency ("#ctor()", typeof (C))] // To avoid lazy body marking stubbing
			[DynamicDependency ("field", typeof (C))]
			[DynamicDependency ("NextOne(Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.Nested@)", typeof (Nested))]
			[DynamicDependency ("#cctor()", typeof (Nested))]
			// Dependency on a property itself should be expressed as a dependency on one or both accessor methods
			[DynamicDependency ("get_Property()", typeof (C))]
			[DynamicDependency ("get_Property2", typeof (C))]
			[DynamicDependency ("M``1(Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.Complex.S{" +
				"Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.Complex.G{" +
					"Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.Complex.A,``0}}" +
					"[0:,0:,0:][][][0:,0:]@)", typeof (Complex))]
			public static void Method ()
			{
			}

			[Kept]
			[DynamicDependency ("field")]
			[DynamicDependency ("Method2(System.SByte@)")]
			public static void SameContext ()
			{
			}

			[Kept]

			[ExpectedWarning ("IL2037", "MissingMethod", "'Mono.Linker.Tests.Cases.DynamicDependencies.C'")]
			[DynamicDependency ("MissingMethod", typeof (C))]

			[ExpectedWarning ("IL2037", "Dependency2``1(``0,System.Int32,System.Object)", "'Mono.Linker.Tests.Cases.DynamicDependencies.C'")]
			[DynamicDependency ("Dependency2``1(``0,System.Int32,System.Object)", typeof (C))]

			[ExpectedWarning ("IL2037", "''", "'Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.B'")]
			[DynamicDependency ("")]

			[ExpectedWarning ("IL2037", "#ctor()", "'Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyMethod.NestedStruct'")]
			[DynamicDependency ("#ctor()", typeof (NestedStruct))]

			[ExpectedWarning ("IL2037", "#cctor()", "'Mono.Linker.Tests.Cases.DynamicDependencies.C'")]
			[DynamicDependency ("#cctor()", typeof (C))]

			[ExpectedWarning ("IL2036", "NonExistentType")]
			[DynamicDependency ("method", "NonExistentType", "test")]
			public static void Broken ()
			{
			}

			[Kept]
			[DynamicDependency ("ConditionalTest()", typeof (C), Condition = "don't have it")]
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

		class Complex
		{
			[Kept]
			public struct S<T> { }
			[Kept]
			public class A { }
			[Kept]
			public class G<T, U> { }

			[Kept]
			public void M<V> (ref S<G<A, V>>[,][][][,,] a)
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
		internal void Dependency3 (string str)
		{
		}

		[Kept]
		[DynamicDependency ("#ctor", typeof (NestedInC))]
		internal void RecursiveDependency ()
		{
		}

		[KeptMember (".ctor()")]
		class NestedInC
		{
		}

		[Kept]
		[KeptBackingField]
		internal string Property { [Kept] get; set; }

		[Kept]
		[KeptBackingField]
		internal string Property2 { [Kept] get; set; }

		// For now, Condition has no effect: https://github.com/dotnet/linker/issues/1231
		[Kept]
		internal void ConditionalTest ()
		{
		}
	}
#if NATIVEAOT
	abstract class ReferenceViaReflection
	{
		[Kept]
		[DynamicDependency (nameof (TargetMethodViaReflection))]
		public static void SourceMethodViaReflection () { }

		[Kept]
		private static void TargetMethodViaReflection () { }


		[Kept]
		public static void Test ()
		{
			var i = new Impl (); // Avoid removal of non-implemented abstract methods

			typeof (ReferenceViaReflection).RequiresPublicMethods ();
			typeof (AbstractMethods).RequiresPublicMethods ();
		}

		[KeptMember (".ctor()")]
		private abstract class AbstractMethods
		{
			[Kept]
			[DynamicDependency (nameof (TargetMethod))]
			public abstract void SourceAbstractViaReflection ();

			[Kept]
			private static void TargetMethod () { }
		}

		[KeptMember (".ctor()")]
		private class Impl : AbstractMethods
		{
			[Kept]
			public override void SourceAbstractViaReflection () { }
		}
	}
#endif
}
