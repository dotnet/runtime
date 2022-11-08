using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnValueType.NoKeptCtor
{
	public class InterfaceTypeRemovedWhenOnlyUsedByClassWithOnlyStaticMethodMultiple
	{
		public static void Main ()
		{
			IUsedInterface p = new UsedClass ();
			StaticMethodOnlyUsed.StaticMethod ();
			p.Foo ();

			// We are testing removing interfaces when no instance is created, we need to mark the interface types
			// so that we don't end up testing unused interface types being swept
			var tmp = typeof (IRemovable1).ToString ();
			tmp = typeof (IRemovable2).ToString ();
		}

		[Kept]
		interface IUsedInterface
		{
			[Kept]
			void Foo ();
		}

		[Kept] // Could be removed in the future with improved handling of value types
		interface IRemovable1
		{
		}

		[Kept] // Could be removed in the future with improved handling of value types
		interface IRemovable2
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IUsedInterface))]
		class UsedClass : IUsedInterface
		{
			[Kept]
			public void Foo ()
			{
			}
		}

		[Kept]
		[KeptInterface (typeof (IUsedInterface))] // Could be removed in the future with improved handling of value types
		[KeptInterface (typeof (IRemovable1))] // Could be removed in the future with improved handling of value types
		[KeptInterface (typeof (IRemovable2))] // Could be removed in the future with improved handling of value types
		struct StaticMethodOnlyUsed : IUsedInterface, IRemovable1, IRemovable2
		{
			[Kept] // Could be removed in the future with improved handling of value types
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