using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtor
{
	[SetupLinkerDescriptorFile ("UnusedTypeWithPreserveMethods.xml")]
	public class UnusedTypeWithPreserveMethods
	{
		public static void Main ()
		{
		}

		interface IFoo
		{
			void Foo ();
		}

		interface IBar
		{
			void Bar ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class A : IBar, IFoo
		{
			[Kept]
			public void Foo ()
			{
			}

			[Kept]
			public void Bar ()
			{
			}
		}
	}
}