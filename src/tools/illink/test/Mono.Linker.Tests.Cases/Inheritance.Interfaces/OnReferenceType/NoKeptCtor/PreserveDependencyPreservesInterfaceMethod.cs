using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtor
{
	/// <summary>
	/// The interface can still be removed in this case because PreserveDependency is just preserving Foo() on the current type
	/// </summary>
	[SetupCompileBefore ("FakeSystemAssembly.dll", new[] { "../../../PreserveDependencies/Dependencies/PreserveDependencyAttribute.cs" })]
	public class PreserveDependencyPreservesInterfaceMethod
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
			[PreserveDependency ("Foo")]
			public static void StaticMethod ()
			{
			}
		}
	}
}