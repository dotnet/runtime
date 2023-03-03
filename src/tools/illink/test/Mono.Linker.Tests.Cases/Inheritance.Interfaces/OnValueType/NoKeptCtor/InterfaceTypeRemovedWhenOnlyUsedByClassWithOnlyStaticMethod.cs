using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnValueType.NoKeptCtor
{
	public class InterfaceTypeRemovedWhenOnlyUsedByClassWithOnlyStaticMethod
	{
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();

			// We are testing removing interfaces when no instance is created, we need to mark the interface types
			// so that we don't end up testing unused interface types being swept
			var tmp = typeof (IUsedInterface).ToString ();
		}

		[Kept] // Could be removed in the future with improved handling of value types
		interface IUsedInterface
		{
			void Foo ();
		}

		[Kept]
		[KeptInterface (typeof (IUsedInterface))] // Could be removed in the future with improved handling of value types
		struct StaticMethodOnlyUsed : IUsedInterface
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