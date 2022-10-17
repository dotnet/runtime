using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class GenericInterfaceWithMethod2
	{
		public static void Main ()
		{
			IFoo<object> f = new FooWithBase ();
			f.Method (null);
		}

		[Kept]
		class GenericType<T>
		{
		}

		[Kept]
		interface IFoo<T>
		{
			[Kept]
			void Method (GenericType<T> arg);
		}

		[Kept]
		[KeptMember (".ctor()")]
		class BaseFoo
		{
			[Kept]
			public void Method (GenericType<object> arg)
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (BaseFoo))]
		[KeptInterface (typeof (IFoo<object>))]
		class FooWithBase : BaseFoo, IFoo<object>
		{
		}
	}
}