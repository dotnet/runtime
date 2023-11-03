using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtor
{
	/// <summary>
	/// The interface can still be removed in this case because PreserveDependency is just preserving Foo() on the current type
	/// </summary>
	public class DynamicDependencyPreservesInterfaceMethod
	{
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		interface IUnusedInterface
		{
			void Foo ();
		}

		[Kept]
		class StaticMethodOnlyUsed : IUnusedInterface
		{
			[Kept]
			public void Foo ()
			{
			}

			[Kept]
			[DynamicDependency ("Foo")]
			public static void StaticMethod ()
			{
			}
		}
	}
}