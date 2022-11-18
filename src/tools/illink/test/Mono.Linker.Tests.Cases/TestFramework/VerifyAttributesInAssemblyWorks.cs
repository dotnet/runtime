using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TestFramework.Dependencies;

namespace Mono.Linker.Tests.Cases.TestFramework
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	// Put the attribute types in a different assembly than where we will assert since that is a more complex use case
	[SetupCompileBefore ("base.dll", new[] { "Dependencies/VerifyAttributesInAssemblyWorks_Base.cs" })]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/VerifyAttributesInAssemblyWorks_Lib.cs" }, new[] { "base.dll" })]

	[KeptAttributeInAssembly ("library.dll", typeof (VerifyAttributesInAssemblyWorks_Base.ForAssertingKeptAttribute))]
	[KeptAttributeInAssembly ("library.dll", typeof (VerifyAttributesInAssemblyWorks_Base.ForAssertingKeptAttribute), typeof (VerifyAttributesInAssemblyWorks_Lib.TypeWithKeptAttribute))]
	[KeptAttributeInAssembly ("library.dll", typeof (VerifyAttributesInAssemblyWorks_Base.ForAssertingKeptAttribute), typeof (VerifyAttributesInAssemblyWorks_Lib.TypeWithKeptAttribute), "Field")]
	[KeptAttributeInAssembly ("library.dll", typeof (VerifyAttributesInAssemblyWorks_Base.ForAssertingKeptAttribute), typeof (VerifyAttributesInAssemblyWorks_Lib.TypeWithKeptAttribute), "Property")]
	[KeptAttributeInAssembly ("library.dll", typeof (VerifyAttributesInAssemblyWorks_Base.ForAssertingKeptAttribute), typeof (VerifyAttributesInAssemblyWorks_Lib.TypeWithKeptAttribute), "Method()")]

	[RemovedAttributeInAssembly ("library.dll", typeof (VerifyAttributesInAssemblyWorks_Base.ForAssertingRemoveAttribute))]
	[RemovedAttributeInAssembly ("library.dll", typeof (VerifyAttributesInAssemblyWorks_Base.ForAssertingRemoveAttribute), typeof (VerifyAttributesInAssemblyWorks_Lib.TypeWithRemovedAttribute))]
	[RemovedAttributeInAssembly ("library.dll", typeof (VerifyAttributesInAssemblyWorks_Base.ForAssertingRemoveAttribute), typeof (VerifyAttributesInAssemblyWorks_Lib.TypeWithRemovedAttribute), "Field")]
	[RemovedAttributeInAssembly ("library.dll", typeof (VerifyAttributesInAssemblyWorks_Base.ForAssertingRemoveAttribute), typeof (VerifyAttributesInAssemblyWorks_Lib.TypeWithRemovedAttribute), "Property")]
	[RemovedAttributeInAssembly ("library.dll", typeof (VerifyAttributesInAssemblyWorks_Base.ForAssertingRemoveAttribute), typeof (VerifyAttributesInAssemblyWorks_Lib.TypeWithRemovedAttribute), "Method()")]
	public class VerifyAttributesInAssemblyWorks
	{
		public static void Main ()
		{
			// Use the attribute type we want to keep
			var tmp = typeof (VerifyAttributesInAssemblyWorks_Base.ForAssertingKeptAttribute).ToString ();

			// Now use the members of both types
			VerifyAttributesInAssemblyWorks_Lib.TypeWithKeptAttribute.Field = 1;
			VerifyAttributesInAssemblyWorks_Lib.TypeWithKeptAttribute.Property = 1;
			VerifyAttributesInAssemblyWorks_Lib.TypeWithKeptAttribute.Method ();

			VerifyAttributesInAssemblyWorks_Lib.TypeWithRemovedAttribute.Field = 1;
			VerifyAttributesInAssemblyWorks_Lib.TypeWithRemovedAttribute.Property = 1;
			VerifyAttributesInAssemblyWorks_Lib.TypeWithRemovedAttribute.Method ();
		}
	}
}