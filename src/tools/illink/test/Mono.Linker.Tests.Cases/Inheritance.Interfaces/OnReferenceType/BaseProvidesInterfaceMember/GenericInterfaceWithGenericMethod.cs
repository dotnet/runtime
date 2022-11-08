using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class GenericInterfaceWithGenericMethod
	{
		public static void Main ()
		{
			IFoo<object> f = new FooWithBase ();
			var result = f.Method<object> (null);
		}

		[Kept]
		interface IFoo<TType>
		{
			[Kept]
			TMethod Method<TMethod> (TType arg);
		}

		[Kept]
		[KeptMember (".ctor()")]
		class BaseFoo
		{
			[Kept]
			public TMethod Method<TMethod> (object arg)
			{
				return default (TMethod);
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