using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnValueType
{
	public class StructUsedFromConcreteTypeHasInterfaceMethodRemoved2
	{
		public static void Main ()
		{
			A a = new A ();
			a.Foo ();

			// If IFoo is marked for any reason then all of a sudden we do need to keep IFoo on A
			var tmp = typeof (IFoo).ToString ();
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		struct A : IFoo
		{
			[Kept]
			public void Foo ()
			{
			}
		}

		[Kept]
		public interface IFoo
		{
			void Foo ();
		}
	}
}