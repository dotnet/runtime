using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.VirtualMethods.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.VirtualMethods
{
	[SetupLinkerAction ("copy", "base")]
	[SetupCompileBefore ("base.dll", new[] { typeof (TypeWithBaseInCopiedAssembly_Base) })]
	[KeptMemberInAssembly ("base.dll", typeof (TypeWithBaseInCopiedAssembly_Base), "Method()")]
	public class NeverInstantiatedTypeWithBaseInCopiedAssembly
	{
		public static void Main ()
		{
			new Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (TypeWithBaseInCopiedAssembly_Base))]
		class Foo : TypeWithBaseInCopiedAssembly_Base
		{
			[Kept]
			public override void Method ()
			{
			}
		}
	}
}