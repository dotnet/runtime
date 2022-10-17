using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class GenericInterfaceWithPropertyGetter2
	{
		public static void Main ()
		{
			IFoo<object> f = new FooWithBase ();
			f.Property = new GenericType<object> ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class GenericType<T>
		{
		}

		[Kept]
		interface IFoo<T>
		{
			[Kept]
			GenericType<T> Property { get; [Kept] set; }
		}

		[Kept]
		[KeptMember (".ctor()")]
		class BaseFoo
		{
			[Kept]
			[KeptBackingField]
			public GenericType<object> Property { get; [Kept] set; }
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