using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class GenericInterfaceWithMethodManyBases
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
		class BaseFoo2 : BaseFoo
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (BaseFoo2))]
		class BaseFoo3 : BaseFoo2
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (BaseFoo3))]
		class BaseFoo4 : BaseFoo3
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (BaseFoo4))]
		[KeptInterface (typeof (IFoo<object>))]
		class FooWithBase : BaseFoo4, IFoo<object>
		{
		}
	}
}