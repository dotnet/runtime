using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.LinkXml.Dependencies;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupCompileBefore ("Library.dll", new[] { "Dependencies/CanPreserveAnExportedType_Library.cs" })]
	// Add another assembly in that uses the forwarder just to make things a little more complex
	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/CanPreserveAnExportedType_Forwarder.cs" }, references: new[] { "Library.dll" })]

	[KeptMemberInAssembly ("Library.dll", typeof (CanPreserveAnExportedType_Library), "Field1", "Method()", ".ctor()")]
	[SetupLinkerDescriptorFile ("CanPreserveAnExportedType.xml")]
	[KeptTypeInAssembly ("Forwarder.dll", typeof (CanPreserveAnExportedType_Library))]
	class CanPreserveAnExportedType
	{
		public static void Main ()
		{
		}
	}
}
