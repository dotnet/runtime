using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class GenericInterfaceWithMethodOnNoInstanceCtor
	{
		public static void Main ()
		{
			IFoo<object> f = new OtherWithFoo ();
			f.Method (null);
			UsedToMarkTypeOnly (null);
		}

		[Kept]
		static void UsedToMarkTypeOnly (FooWithBase arg)
		{
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
			[Kept]
			public void Method (object arg)
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (BaseFoo))]
		[KeptInterface (typeof (IFoo<object>))]
		class FooWithBase : BaseFoo, IFoo<object>
		{
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