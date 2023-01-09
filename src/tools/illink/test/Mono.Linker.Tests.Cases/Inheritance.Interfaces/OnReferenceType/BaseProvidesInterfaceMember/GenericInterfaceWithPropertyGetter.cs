using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class GenericInterfaceWithPropertyGetter
	{
		public static void Main ()
		{
			IFoo<object> f = new FooWithBase ();
			f.Property = new object ();
		}

		[Kept]
		interface IFoo<T>
		{
			[Kept]
			T Property { get; [Kept] set; }
		}

		[Kept]
		[KeptMember (".ctor()")]
		class BaseFoo
		{
			[Kept]
			[KeptBackingField]
			public object Property { get; [Kept] set; }
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