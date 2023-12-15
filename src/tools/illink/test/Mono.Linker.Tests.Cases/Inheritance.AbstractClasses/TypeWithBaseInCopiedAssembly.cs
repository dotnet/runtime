using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses
{
	[SetupLinkerAction ("copy", "base")]
	[SetupCompileBefore ("base.dll", new[] { typeof (TypeWithBaseInCopiedAssembly_Base) })]
	[KeptMemberInAssembly ("base.dll", typeof (TypeWithBaseInCopiedAssembly_Base.Base), "Method()")]
	public class TypeWithBaseInCopiedAssembly
	{
		public static void Main ()
		{
			new Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (TypeWithBaseInCopiedAssembly_Base.Base))]
		class Foo : TypeWithBaseInCopiedAssembly_Base.Base
		{
			[Kept]
			public override void Method ()
			{
			}
		}
	}
}