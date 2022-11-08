using Mono.Linker.Tests.Cases.Attributes.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.Csc
{
	/// <summary>
	/// This explicit csc test exists to ensure that csc adds references in this scenario
	/// </summary>
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileBefore ("LibraryWithType.dll", new[] { typeof (TypeDefinedInReference) })]
	[SetupCompileBefore ("LibraryWithAttribute.dll", new[] { typeof (AttributeDefinedInReference) })]
	[KeptTypeInAssembly ("LibraryWithType.dll", typeof (TypeDefinedInReference))]
	[RemovedMemberInAssembly ("LibraryWithType.dll", typeof (TypeDefinedInReference), "Unused()")]
	[KeptMemberInAssembly ("LibraryWithAttribute.dll", typeof (AttributeDefinedInReference), ".ctor(System.Type)")]
	public class OnlyTypeUsedInAssemblyIsTypeOnAttributeCtorOnProperty
	{
		public static void Main ()
		{
			var foo = new Foo ();
			foo.Property = 1;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Foo
		{
			[Kept]
			[KeptBackingField]
			[KeptAttributeAttribute (typeof (AttributeDefinedInReference))]
			[AttributeDefinedInReference (typeof (TypeDefinedInReference))]
			public int Property { get; [Kept] set; }
		}
	}
}