using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NotKeptCtor
{
	[SetupLinkerAction ("copy", "base5")]
	[SetupCompileBefore ("base5.dll", new[] { typeof (TypeWithBaseInCopiedAssembly4_Base) })]
	[KeptMemberInAssembly ("base5.dll", typeof (TypeWithBaseInCopiedAssembly4_Base.Base), "Method()")]
	public class NeverInstantiatedTypeWithBaseInCopiedAssembly5
	{
		public static void Main ()
		{
			Helper (null, null);
		}

		[Kept]
		static void Helper (Foo arg, Bar arg2)
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
		[Kept]
		[KeptBaseType (typeof (TypeWithBaseInCopiedAssembly4_Base.Base2))]
		class Bar : TypeWithBaseInCopiedAssembly4_Base.Base2
		{
			// This method can be removed because the type is never instantiated and the base class
			// overrides the original abstract method
			public override void Method ()
			{
			}
		}
	}
}