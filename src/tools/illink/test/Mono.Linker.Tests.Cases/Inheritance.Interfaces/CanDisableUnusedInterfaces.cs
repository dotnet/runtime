using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces
{
	[SetupLinkerArgument ("--disable-opt", "unusedinterfaces")]
	public class CanDisableUnusedInterfaces
	{
		public static void Main ()
		{
			IFoo i = new A ();
			i.Foo ();
		}
		[Kept]
		interface IFoo
		{
			[Kept]
			void Foo ();
		}
		[Kept]
		interface IBar
		{
			// interface methods may still be removed
			void Bar ();
		}
		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		[KeptInterface (typeof (IBar))]
		class A : IFoo, IBar
		{
			[Kept]
			public void Foo ()
			{
			}
			public void Bar ()
			{
			}
		}
	}
}
