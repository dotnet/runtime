using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class GenericInterfaceWithMethodManyBaseInterfaces
	{
		public static void Main ()
		{
			IFoo<object> f = new FooWithBase ();
			f.Method (null);
		}

		[Kept]
		interface IFoo<T>
		{
			[Kept]
			void Method (T arg);
		}

		[Kept] // Should be able to be removed
		[KeptInterface (typeof (IFoo<>))]
		interface IFoo2<T> : IFoo<T>
		{
		}

		[Kept] // Should be able to be removed
		[KeptInterface (typeof (IFoo<>))]
		[KeptInterface (typeof (IFoo2<>))]
		interface IFoo3<T> : IFoo2<T>
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		class BaseFoo
		{
			[Kept]
			public void Method (object arg)
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (BaseFoo))]
		[KeptInterface (typeof (IFoo<object>))]
		[KeptInterface (typeof (IFoo2<object>))] // Should be removable
		[KeptInterface (typeof (IFoo3<object>))] // Should be removable
		class FooWithBase : BaseFoo, IFoo3<object>
		{
		}
	}
}
