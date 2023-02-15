using Mono.Linker.Tests.Cases.Attributes.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: KeptAttributeAttribute (typeof (AttributeDefinedInReference))]
[assembly: AttributeDefinedInReference (typeof (TypeDefinedInReference))]

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
	[KeptTypeInAssembly ("LibraryWithAttribute.dll", typeof (AttributeDefinedInReference_OtherType))]
	public class OnlyTypeUsedInAssemblyIsTypeOnAttributeCtorOnAssemblyOtherTypesInAttributeAssemblyUsed
	{
		public static void Main ()
		{
			// Use something in the attribute assembly so that the special behavior of not preserving a reference if the only thing that is marked
			// are attributes is not trigged
			AttributeDefinedInReference_OtherType.Method ();
		}
	}
}