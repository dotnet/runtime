using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class SimpleMethod
	{
		public static void Main ()
		{
			IFoo f = new FooWithBase ();
			f.Method ();
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			void Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class BaseFoo
		{
			[Kept]
			public void Method ()
			{
			}
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