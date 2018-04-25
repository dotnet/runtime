using Mono.Linker.Tests.Cases.Attributes.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.Mcs {
	/// <summary>
	/// This explicit mcs test exists because mcs did not add a reference prior to https://github.com/mono/mono/commit/f71b208ca7b41a2a97ca70b955df0c4c411ce8e5
	/// </summary>
	[IgnoreTestCase ("Can be enabled once the mcs contains https://github.com/mono/mono/commit/f71b208ca7b41a2a97ca70b955df0c4c411ce8e5")]
	[SetupCSharpCompilerToUse ("mcs")]
	[SetupCompileBefore ("LibraryWithType1.dll", new [] { typeof(TypeDefinedInReference) })]
	[SetupCompileBefore ("LibraryWithType2.dll", new [] { typeof(TypeDefinedInReference2) })]
	[SetupCompileBefore ("LibraryWithAttribute.dll", new [] { typeof(AttributeDefinedInReference) })]
	[KeptTypeInAssembly ("LibraryWithType1.dll", typeof (TypeDefinedInReference))]
	[KeptTypeInAssembly ("LibraryWithType2.dll", typeof (TypeDefinedInReference2))]
	[KeptMemberInAssembly ("LibraryWithAttribute.dll", typeof (AttributeDefinedInReference), ".ctor(System.Type[])")]
	public class OnlyTypeUsedInAssemblyIsTypeArrayOnAttributeCtorOnType {
		public static void Main()
		{
			var foo = new Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (AttributeDefinedInReference))]
		[AttributeDefinedInReference (new [] { typeof (TypeDefinedInReference), typeof (TypeDefinedInReference2) })]
		class Foo {
		}
	}
}