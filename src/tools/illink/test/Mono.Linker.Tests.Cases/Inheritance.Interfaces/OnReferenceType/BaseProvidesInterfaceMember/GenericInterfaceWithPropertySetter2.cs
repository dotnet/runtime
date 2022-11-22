using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class GenericInterfaceWithPropertySetter2
	{
		public static void Main ()
		{
			IFoo<object> f = new FooWithBase ();
			var tmp = f.Property;
		}

		[Kept]
		class GenericType<T>
		{
		}

		[Kept]
		interface IFoo<T>
		{
			[Kept]
			GenericType<T> Property { [Kept] get; set; }
		}

		[Kept]
		[KeptMember (".ctor()")]
		class BaseFoo
		{
			[Kept]
			[KeptBackingField]
			public GenericType<object> Property { [Kept] get; set; }
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