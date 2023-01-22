using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses
{
	[SetupLinkerAction ("copy", "base5")]
	[SetupCompileBefore ("base5.dll", new[] { typeof (TypeWithBaseInCopiedAssembly4_Base) })]
	[KeptMemberInAssembly ("base5.dll", typeof (TypeWithBaseInCopiedAssembly4_Base.Base), "Method()")]
	public class TypeWithBaseInCopiedAssembly5
	{
		public static void Main ()
		{
			new Foo ();
			new Bar ();
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

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (TypeWithBaseInCopiedAssembly4_Base.Base2))]
		class Bar : TypeWithBaseInCopiedAssembly4_Base.Base2
		{
			[Kept]
			public override void Method ()
			{
			}
		}
	}
}