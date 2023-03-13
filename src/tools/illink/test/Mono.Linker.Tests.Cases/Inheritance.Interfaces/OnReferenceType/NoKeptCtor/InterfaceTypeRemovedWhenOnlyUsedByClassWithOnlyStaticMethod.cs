using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtor
{
	public class InterfaceTypeRemovedWhenOnlyUsedByClassWithOnlyStaticMethod
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
			public void Foo ()
			{
			}

			[Kept]
			public static void StaticMethod ()
			{
			}
		}
	}
}