using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	[KeptDelegateCacheField ("0", nameof (EventMethod))]
	public class GenericInterfaceWithEvent
	{
		public static void Main ()
		{
			IFoo<object> f = new ClassFoo ();
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
		delegate void CustomDelegate<T> ();

		[Kept]
		interface IFoo<T>
		{
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			event CustomDelegate<T> Foo;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class BaseFoo
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event CustomDelegate<object> Foo;
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (BaseFoo))]
		[KeptInterface (typeof (IFoo<object>))]
		class ClassFoo : BaseFoo, IFoo<object>
		{
		}
	}
}