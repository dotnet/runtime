using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnValueType.NoKeptCtor
{
	public class InterfaceCanBeRemovedFromClassWithOnlyStaticMethodUsedWithCctor
	{
		public static void Main ()
		{
			IUsedInterface p = new UsedClass ();
			StaticMethodOnlyUsed.StaticMethod ();
			p.Foo ();
		}

		[Kept]
		interface IUsedInterface
		{
			[Kept]
			void Foo ();
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
		struct StaticMethodOnlyUsed : IUsedInterface
		{
			[Kept]
			static StaticMethodOnlyUsed ()
			{
				UsedByStaticCctor ();
			}

			[Kept]
			static void UsedByStaticCctor ()
			{
			}

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