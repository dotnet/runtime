using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class GenericInterfaceWithGenericMethod2
	{
		public static void Main ()
		{
			IFoo<object> f = new FooWithBase ();
			var result = f.Method<object> (null);
		}

		[Kept]
		[KeptMember (".ctor()")]
		class GenericType<T>
		{
		}

		[Kept]
		interface IFoo<TType>
		{
			[Kept]
			GenericType<TMethod> Method<TMethod> (GenericType<TType> arg);
		}

		[Kept]
		[KeptMember (".ctor()")]
		class BaseFoo
		{
			[Kept]
			public GenericType<TMethod> Method<TMethod> (GenericType<object> arg)
			{
				return new GenericType<TMethod> ();
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