using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	public class InterfaceRemovedWhenMethodUsedDirectly
	{
		public static void Main ()
		{
			var f = new FooWithBase ();
			f.Method ();
		}

		interface IFoo
		{
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
		class FooWithBase : BaseFoo, IFoo
		{
		}
	}
}