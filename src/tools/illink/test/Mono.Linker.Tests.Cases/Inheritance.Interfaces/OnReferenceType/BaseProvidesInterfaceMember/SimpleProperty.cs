using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class SimpleProperty
	{
		public static void Main ()
		{
			IFoo f = new FooWithBase ();
			f.Property = 1;
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			int Property { get; [Kept] set; }
		}

		[Kept]
		[KeptMember (".ctor()")]
		class BaseFoo
		{
			[Kept]
			[KeptBackingField]
			public int Property { get; [Kept] set; }
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (BaseFoo))]
		[KeptInterface (typeof (IFoo))]
		class FooWithBase : BaseFoo, IFoo
		{
		}
	}
}