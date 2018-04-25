using Mono.Linker.Tests.Cases.Attributes.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: KeptAttributeAttribute (typeof (AttributeDefinedInReference))]
[assembly: AttributeDefinedInReference (typeof (TypeDefinedInReference))]

namespace Mono.Linker.Tests.Cases.Attributes.Mcs {
	/// <summary>
	/// This explicit mcs test exists because mcs did not add a reference prior to https://github.com/mono/mono/commit/f71b208ca7b41a2a97ca70b955df0c4c411ce8e5
	/// </summary>
	[IgnoreTestCase ("Can be enabled once the mcs contains https://github.com/mono/mono/commit/f71b208ca7b41a2a97ca70b955df0c4c411ce8e5")]
	[SetupCSharpCompilerToUse ("mcs")]
	[SetupCompileBefore ("LibraryWithType.dll", new [] { typeof(TypeDefinedInReference) })]
	[SetupCompileBefore ("LibraryWithAttribute.dll", new [] { typeof (AttributeDefinedInReference) })]
	[KeptTypeInAssembly ("LibraryWithType.dll", typeof (TypeDefinedInReference))]
	[RemovedMemberInAssembly ("LibraryWithType.dll", typeof (TypeDefinedInReference), "Unused()")]
	[KeptMemberInAssembly ("LibraryWithAttribute.dll", typeof (AttributeDefinedInReference), ".ctor(System.Type)")]
	[KeptTypeInAssembly ("LibraryWithAttribute.dll", typeof (AttributeDefinedInReference_OtherType))]
	public class OnlyTypeUsedInAssemblyIsTypeOnAttributeCtorOnAssemblyOtherTypesInAttributeAssemblyUsed {
		public static void Main ()
		{
			// Use something in the attribute assembly so that the special behavior of not preserving a reference if the only thing that is marked
			// are attributes is not trigged
			AttributeDefinedInReference_OtherType.Method();
		}
	}
}