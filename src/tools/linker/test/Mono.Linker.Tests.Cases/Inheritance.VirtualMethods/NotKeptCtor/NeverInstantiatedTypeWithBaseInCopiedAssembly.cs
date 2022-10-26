using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.VirtualMethods.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.VirtualMethods.NotKeptCtor
{
	[SetupLinkerAction ("copy", "base")]
	[SetupCompileBefore ("base.dll", new[] { typeof (TypeWithBaseInCopiedAssembly_Base) })]
	[KeptMemberInAssembly ("base.dll", typeof (TypeWithBaseInCopiedAssembly_Base), "Method()")]
	public class NeverInstantiatedTypeWithBaseInCopiedAssembly
	{
		public static void Main ()
		{
			Helper (null);
		}

		[Kept]
		static void Helper (Foo arg)
		{
		}

		[Kept]
		[KeptBaseType (typeof (TypeWithBaseInCopiedAssembly_Base))]
		class Foo : TypeWithBaseInCopiedAssembly_Base
		{
			// It's safe to remove this because the type is never instantiated and the base method is virtual
			public override void Method ()
			{
			}
		}
	}
}