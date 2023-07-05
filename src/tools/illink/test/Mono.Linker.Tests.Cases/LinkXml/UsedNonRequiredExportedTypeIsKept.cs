using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("UsedNonRequiredExportedTypeIsKept.xml")]

	[SetupCompileBefore ("libfwd.dll", new[] { "Dependencies/UsedNonRequiredExportedTypeIsKept_lib.cs" })]
	[SetupCompileAfter ("lib.dll", new[] { "Dependencies/UsedNonRequiredExportedTypeIsKept_lib.cs" })]
	[SetupCompileAfter ("libfwd.dll", new[] { "Dependencies/UsedNonRequiredExportedTypeIsKept_fwd.cs" }, references: new[] { "lib.dll" })]

	// Note that forwarders which are referenced from within a descriptor XML file are kept -- any exported type referenced through a type
	// name string should be kept.
	[KeptMemberInAssembly ("libfwd.dll", typeof (UsedNonRequiredExportedTypeIsKept_Used1), "field")]
	[KeptMemberInAssembly ("libfwd.dll", typeof (UsedNonRequiredExportedTypeIsKept_Used2), "Method()")]
	[KeptMemberInAssembly ("libfwd.dll", typeof (UsedNonRequiredExportedTypeIsKept_Used3), "Method()")]
	[KeptAssembly ("lib.dll")]

	public class UsedNonRequiredExportedTypeIsKept
	{
		public static void Main ()
		{
			var tmp = typeof (UsedNonRequiredExportedTypeIsKept_Used1).ToString ();
			tmp = typeof (UsedNonRequiredExportedTypeIsKept_Used2).ToString ();
			tmp = typeof (UsedNonRequiredExportedTypeIsKept_Used3).ToString ();
		}
	}
}