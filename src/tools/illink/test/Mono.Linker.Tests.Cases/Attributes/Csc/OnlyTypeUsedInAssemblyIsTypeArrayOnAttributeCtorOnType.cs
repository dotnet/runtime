using Mono.Linker.Tests.Cases.Attributes.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.Csc
{
	/// <summary>
	/// This explicit csc test exists to ensure that csc adds references in this scenario
	/// </summary>
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileBefore ("LibraryWithType1.dll", new[] { typeof (TypeDefinedInReference) })]
	[SetupCompileBefore ("LibraryWithType2.dll", new[] { typeof (TypeDefinedInReference2) })]
	[SetupCompileBefore ("LibraryWithAttribute.dll", new[] { typeof (AttributeDefinedInReference) })]
	[KeptTypeInAssembly ("LibraryWithType1.dll", typeof (TypeDefinedInReference))]
	[KeptTypeInAssembly ("LibraryWithType2.dll", typeof (TypeDefinedInReference2))]
	[KeptMemberInAssembly ("LibraryWithAttribute.dll", typeof (AttributeDefinedInReference), ".ctor(System.Type[])")]
	public class OnlyTypeUsedInAssemblyIsTypeArrayOnAttributeCtorOnType
	{
		public static void Main ()
		{
			var foo = new Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (AttributeDefinedInReference))]
		[AttributeDefinedInReference (new[] { typeof (TypeDefinedInReference), typeof (TypeDefinedInReference2) })]
		class Foo
		{
		}
	}
}