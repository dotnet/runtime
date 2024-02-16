using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.LinkXml.Dependencies;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupCompileBefore ("Library.dll", new[] { "Dependencies/CanPreserveAnExportedType_Library.cs" })]
	// Add another assembly in that uses the forwarder just to make things a little more complex
	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/CanPreserveAnExportedType_Forwarder.cs" }, references: new[] { "Library.dll" })]

	// NativeAOT doesn't have a concept of type forwarders in the compiled app, everything is fully resolved
	[KeptMemberInAssembly ("Library.dll", typeof (CanPreserveAnExportedType_Library), "Field1", "Method()", ".ctor()", Tool = Tool.Trimmer)]
	[KeptTypeInAssembly ("Forwarder.dll", typeof (CanPreserveAnExportedType_Library), Tool = Tool.Trimmer)]
	[SetupLinkerDescriptorFile ("CanPreserveExportedTypesUsingRegex.xml")]
	class CanPreserveExportedTypesUsingRegex
	{
		public static void Main ()
		{
		}
	}
}
