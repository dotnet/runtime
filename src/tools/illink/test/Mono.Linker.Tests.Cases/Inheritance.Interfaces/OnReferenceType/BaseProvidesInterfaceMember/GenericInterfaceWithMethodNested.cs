using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class GenericInterfaceWithMethodNested
	{
		public static void Main ()
		{
			IFoo<IFoo<IFoo<IFoo<object>>>> f = new FooWithBase ();
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
			public void Method (IFoo<IFoo<IFoo<object>>> arg)
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (BaseFoo))]
		[KeptInterface (typeof (IFoo<IFoo<IFoo<IFoo<object>>>>))]
		class FooWithBase : BaseFoo, IFoo<IFoo<IFoo<IFoo<object>>>>
		{
		}
	}
}