using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	[KeptDelegateCacheField ("0", nameof (EventMethod))]
	public class SimpleEvent
	{
		public static void Main ()
		{
			IFoo f = new FooWithBase ();
			f.Foo += EventMethod;
		}

		[Kept]
		static void EventMethod ()
		{
		}

		[Kept]
		[KeptMember ("Invoke()")]
		[KeptMember (".ctor(System.Object,System.IntPtr)")]
		[KeptBaseType (typeof (System.MulticastDelegate))]
		delegate void CustomDelegate ();

		[Kept]
		interface IFoo
		{
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			event CustomDelegate Foo;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class BaseFoo
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event CustomDelegate Foo;
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