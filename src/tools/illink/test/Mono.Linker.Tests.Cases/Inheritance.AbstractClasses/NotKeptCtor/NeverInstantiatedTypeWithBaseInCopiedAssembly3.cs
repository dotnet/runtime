using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NotKeptCtor
{
	[SetupLinkerAction ("copy", "base3")]
	[SetupCompileBefore ("base3.dll", new[] { typeof (TypeWithBaseInCopiedAssembly3_Base) })]
	[KeptMemberInAssembly ("base3.dll", typeof (TypeWithBaseInCopiedAssembly3_Base.Base), "Method()")]
	public class NeverInstantiatedTypeWithBaseInCopiedAssembly3
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
		[KeptBaseType (typeof (Base2))]
		class Foo : Base2
		{
			// This method can be removed because the type is never instantiated and the base class
			// will keep it's override in order to keep the IL valid
			public override void Method ()
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (TypeWithBaseInCopiedAssembly3_Base.Base))]
		class Base2 : TypeWithBaseInCopiedAssembly3_Base.Base
		{
			[Kept]
			public override void Method ()
			{
			}
		}
	}
}