using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NotKeptCtor
{
	[SetupLinkerAction ("copy", "base2")]
	[SetupCompileBefore ("base2.dll", new[] { typeof (TypeWithBaseInCopiedAssembly2_Base) })]
	[KeptMemberInAssembly ("base2.dll", typeof (TypeWithBaseInCopiedAssembly2_Base.Base), "Method()")]
	[KeptMemberInAssembly ("base2.dll", typeof (TypeWithBaseInCopiedAssembly2_Base.IBase), "Method()")]
	public class NeverInstantiatedTypeWithBaseInCopiedAssembly2
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
		[KeptBaseType (typeof (TypeWithBaseInCopiedAssembly2_Base.Base))]
		class Foo : TypeWithBaseInCopiedAssembly2_Base.Base
		{
			[Kept]
			public override void Method ()
			{
			}
		}
	}
}