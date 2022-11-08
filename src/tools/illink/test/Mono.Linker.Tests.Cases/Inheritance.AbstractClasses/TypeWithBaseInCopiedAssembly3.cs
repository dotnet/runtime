using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses
{
	[SetupLinkerAction ("copy", "base3")]
	[SetupCompileBefore ("base3.dll", new[] { typeof (TypeWithBaseInCopiedAssembly3_Base) })]
	[KeptMemberInAssembly ("base3.dll", typeof (TypeWithBaseInCopiedAssembly3_Base.Base), "Method()")]
	public class TypeWithBaseInCopiedAssembly3
	{
		public static void Main ()
		{
			new Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base2))]
		class Foo : Base2
		{
			[Kept]
			public override void Method ()
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
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