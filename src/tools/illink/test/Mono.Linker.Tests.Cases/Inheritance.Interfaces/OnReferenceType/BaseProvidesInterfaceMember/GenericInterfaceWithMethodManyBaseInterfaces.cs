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

		interface IFoo2<T> : IFoo<T>
		{
		}

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
		class FooWithBase : BaseFoo, IFoo3<object>
		{
		}
	}
}