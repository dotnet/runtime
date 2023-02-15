using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NotKeptCtor
{
	[SetupLinkerAction ("copy", "base4")]
	[SetupCompileBefore ("base4.dll", new[] { typeof (TypeWithBaseInCopiedAssembly4_Base) })]
	[KeptMemberInAssembly ("base4.dll", typeof (TypeWithBaseInCopiedAssembly4_Base.Base), "Method()")]
	public class NeverInstantiatedTypeWithBaseInCopiedAssembly4
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
		[KeptBaseType (typeof (TypeWithBaseInCopiedAssembly4_Base.Base2))]
		class Foo : TypeWithBaseInCopiedAssembly4_Base.Base2
		{
			// This method can be removed because the type is never instantiated and the base class
			// overrides the original abstract method
			public override void Method ()
			{
			}
		}
	}
}