using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtor
{
	[SetupLinkerDescriptorFile ("UnusedTypeHasExplicitInterfaceMethodPreservedViaXml.xml")]
	public class UnusedTypeHasExplicitInterfaceMethodPreservedViaXml
	{
		public static void Main ()
		{
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			void Foo ();
		}

		interface IBar
		{
			void Bar ();
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		class A : IBar, IFoo
		{

			// Because an explicit interface method was preserved via xml, we need to now mark the interface implementation
			[Kept]
			void IFoo.Foo ()
			{
			}

			void IBar.Bar ()
			{
			}
		}
	}
}