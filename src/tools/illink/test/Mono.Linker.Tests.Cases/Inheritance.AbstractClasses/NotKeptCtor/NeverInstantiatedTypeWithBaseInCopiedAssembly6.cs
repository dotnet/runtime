using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NotKeptCtor
{
	[SetupLinkerAction ("copy", "base6")]
	[SetupCompileBefore ("base6.dll", new[] { typeof (TypeWithBaseInCopiedAssembly6_Base) })]
	[KeptMemberInAssembly ("base6.dll", typeof (TypeWithBaseInCopiedAssembly6_Base.Base), "Method()")]
	[KeptMemberInAssembly ("base6.dll", typeof (TypeWithBaseInCopiedAssembly6_Base.IBase), "Method()")]
	public class NeverInstantiatedTypeWithBaseInCopiedAssembly6
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
		[KeptBaseType (typeof (TypeWithBaseInCopiedAssembly6_Base.Base))]
		class Foo : TypeWithBaseInCopiedAssembly6_Base.Base, TypeWithBaseInCopiedAssembly6_Base.IBase
		{
			// Must be kept because there is an abstract base method
			[Kept]
			public override void Method ()
			{
			}
		}
	}
}