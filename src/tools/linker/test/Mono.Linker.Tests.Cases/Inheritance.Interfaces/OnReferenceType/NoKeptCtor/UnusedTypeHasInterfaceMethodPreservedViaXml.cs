using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtor
{
	[SetupLinkerDescriptorFile ("UnusedTypeHasInterfaceMethodPreservedViaXml.xml")]
	public class UnusedTypeHasInterfaceMethodPreservedViaXml
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
		class A : IBar, IFoo
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