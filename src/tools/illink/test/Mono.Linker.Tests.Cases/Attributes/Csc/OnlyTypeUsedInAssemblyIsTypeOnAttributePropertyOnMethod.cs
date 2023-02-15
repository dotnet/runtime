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
	[KeptMemberInAssembly ("LibraryWithAttribute.dll", typeof (AttributeDefinedInReference), ".ctor()")]
	[KeptMemberInAssembly ("LibraryWithAttribute.dll", typeof (AttributeDefinedInReference), "set_PropertyType(System.Type)")]
	public class OnlyTypeUsedInAssemblyIsTypeOnAttributePropertyOnMethod
	{
		public static void Main ()
		{
			var foo = new Foo ();
			foo.Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Foo
		{
			[Kept]
			[KeptAttributeAttribute (typeof (AttributeDefinedInReference))]
			[AttributeDefinedInReference (PropertyType = typeof (TypeDefinedInReference))]
			public void Method ()
			{
			}
		}
	}
}