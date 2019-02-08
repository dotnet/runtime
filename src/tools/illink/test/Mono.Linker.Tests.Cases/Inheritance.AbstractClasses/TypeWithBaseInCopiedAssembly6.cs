using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses {
	[SetupLinkerAction ("copy", "base")]
	[SetupCompileBefore ("base.dll", new [] { typeof (TypeWithBaseInCopiedAssembly6_Base)})]
	[KeptMemberInAssembly ("base.dll", typeof (TypeWithBaseInCopiedAssembly6_Base.Base), "Method()")]
	[KeptMemberInAssembly ("base.dll", typeof (TypeWithBaseInCopiedAssembly6_Base.IBase), "Method()")]
	public class TypeWithBaseInCopiedAssembly6 {
		public static void Main ()
		{
			Helper (new Foo ());
		}

		[Kept]
		static void Helper (TypeWithBaseInCopiedAssembly6_Base.IBase arg)
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (TypeWithBaseInCopiedAssembly6_Base.Base))]
		[KeptInterface (typeof (TypeWithBaseInCopiedAssembly6_Base.IBase))]
		class Foo : TypeWithBaseInCopiedAssembly6_Base.Base, TypeWithBaseInCopiedAssembly6_Base.IBase {
			[Kept]
			public override void Method ()
			{
			}
		}
	}
}