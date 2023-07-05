using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	/// <summary>
	/// Interface must remain in this case due to the possible types on the stack
	/// </summary>
	public class SimpleMethodOnNoInstanceCtor
	{
		public static void Main ()
		{
			IFoo f = new OtherWithFoo ();
			f.Method ();
			UsedToMarkTypeOnly (null);
		}

		[Kept]
		static void UsedToMarkTypeOnly (FooWithBase arg)
		{
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
			[Kept]
			public void Method ()
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (BaseFoo))]
		[KeptInterface (typeof (IFoo))]
		class FooWithBase : BaseFoo, IFoo
		{
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