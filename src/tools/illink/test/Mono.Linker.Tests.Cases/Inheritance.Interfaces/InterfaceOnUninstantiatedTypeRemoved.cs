using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces
{
	public class InterfaceOnUninstantiatedTypeRemoved
	{
		public static void Main ()
		{
			A a = HelperToMarkA ();
			a.Foo ();

			StandaloneHelperToMarkIFoo ();
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			void Foo ();
		}

		[Kept]
		class A : IFoo
		{
			[Kept]
			public void Foo ()
			{
			}
		}

		[Kept]
		static A HelperToMarkA ()
		{
			return null;
		}

		[Kept]
		static void StandaloneHelperToMarkIFoo ()
		{
			// Reference IFoo outside Main to prevent it from being
			// kept by the body stack logic
			IFoo i = HelperToMarkIFoo ();
			i.Foo ();
		}

		[Kept]
		static IFoo HelperToMarkIFoo ()
		{
			return null;
		}
	}
}
