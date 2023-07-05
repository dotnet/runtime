using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class SimpleMethodOnNoInstanceCtor2
	{
		public static void Main ()
		{
			IFoo f = new OtherWithFoo ();
			f.Method ();
			FooWithBase.UsedToMarkTypeOnly ();
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			void Method ();
		}

		[Kept]
		class BaseFoo
		{
			public void Method ()
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (BaseFoo))]
		class FooWithBase : BaseFoo, IFoo
		{
			[Kept]
			public static void UsedToMarkTypeOnly ()
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		class OtherWithFoo : IFoo
		{
			[Kept]
			public void Method ()
			{
			}
		}
	}
}