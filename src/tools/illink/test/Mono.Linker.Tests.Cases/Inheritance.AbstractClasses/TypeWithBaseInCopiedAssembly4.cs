using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses
{
	[SetupLinkerAction ("copy", "base4")]
	[SetupCompileBefore ("base4.dll", new[] { typeof (TypeWithBaseInCopiedAssembly4_Base) })]
	[KeptMemberInAssembly ("base4.dll", typeof (TypeWithBaseInCopiedAssembly4_Base.Base), "Method()")]
	public class TypeWithBaseInCopiedAssembly4
	{
		public static void Main ()
		{
			new Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (TypeWithBaseInCopiedAssembly4_Base.Base2))]
		class Foo : TypeWithBaseInCopiedAssembly4_Base.Base2
		{
			[Kept]
			public override void Method ()
			{
			}
		}
	}
}