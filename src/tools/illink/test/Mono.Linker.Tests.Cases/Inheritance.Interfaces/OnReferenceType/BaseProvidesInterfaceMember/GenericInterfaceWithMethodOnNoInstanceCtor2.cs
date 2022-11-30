using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	/// <summary>
	/// Interface must remain in this case due to the possible types on the stack
	/// </summary>
	public class GenericInterfaceWithMethodOnNoInstanceCtor2
	{
		public static void Main ()
		{
			IFoo<object> f = new OtherWithFoo ();
			f.Method (null);
			FooWithBase.UsedToMarkTypeOnly ();
		}

		[Kept]
		interface IFoo<T>
		{
			[Kept]
			void Method (T arg);
		}

		[Kept]
		class BaseFoo
		{
			public void Method (object arg)
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (BaseFoo))]
		class FooWithBase : BaseFoo, IFoo<object>
		{
			[Kept]
			public static void UsedToMarkTypeOnly ()
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo<object>))]
		class OtherWithFoo : IFoo<object>
		{
			[Kept]
			public void Method (object arg)
			{
			}
		}
	}
}