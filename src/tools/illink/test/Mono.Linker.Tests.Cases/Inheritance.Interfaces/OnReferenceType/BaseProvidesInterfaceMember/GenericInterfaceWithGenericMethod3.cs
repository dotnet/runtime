using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class GenericInterfaceWithGenericMethod3
	{
		public static void Main ()
		{
			IFoo<object> f = new FooWithBase ();
			f.Method<object> (null, null);
		}

		[Kept]
		interface IFoo<TType>
		{
			[Kept]
			void Method<TMethod> (TType arg, TMethod arg2);
		}

		[Kept]
		[KeptMember (".ctor()")]
		class BaseFoo
		{
			[Kept]
			public void Method<TMethod> (object arg, TMethod arg2)
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