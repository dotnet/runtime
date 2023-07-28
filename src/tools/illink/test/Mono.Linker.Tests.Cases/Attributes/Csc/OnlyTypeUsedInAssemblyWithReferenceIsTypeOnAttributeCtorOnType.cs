using Mono.Linker.Tests.Cases.Attributes.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.Csc
{
	/// <summary>
	/// This explicit csc test exists to ensure that csc adds references in this scenario
	/// </summary>
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileBefore ("LibraryWithAttribute.dll", new[] { typeof (AttributeDefinedInReference) })]
	[SetupCompileBefore ("LibraryWithType.dll", new[] { typeof (TypeDefinedInReference) })]
	[SetupCompileBefore ("LibraryWithTypeAndReference.dll", new[] { typeof (TypeDefinedInReferenceWithReference) }, new[] { "LibraryWithType.dll", "LibraryWithAttribute.dll" }, compilerToUse: "csc")]
	[KeptTypeInAssembly ("LibraryWithTypeAndReference.dll", typeof (TypeDefinedInReferenceWithReference))]
	[RemovedMemberInAssembly ("LibraryWithTypeAndReference.dll", typeof (TypeDefinedInReferenceWithReference), "Unused()")]
	[KeptTypeInAssembly ("LibraryWithType.dll", typeof (TypeDefinedInReference))]
	[RemovedMemberInAssembly ("LibraryWithType.dll", typeof (TypeDefinedInReference), "Unused()")]
	[KeptMemberInAssembly ("LibraryWithAttribute.dll", typeof (AttributeDefinedInReference), ".ctor(System.Type)")]
	public class OnlyTypeUsedInAssemblyWithReferenceIsTypeOnAttributeCtorOnType
	{
		public static void Main ()
		{
			var foo = new Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (AttributeDefinedInReference))]
		[AttributeDefinedInReference (typeof (TypeDefinedInReferenceWithReference))]
		class Foo
		{
		}
	}
}