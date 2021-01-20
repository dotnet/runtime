using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("UsedNonRequiredExportedTypeIsKept.xml")]

	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/UsedNonRequiredExportedTypeIsKept_lib.cs" })]
	[SetupCompileAfter ("libfwd.dll", new[] { "Dependencies/UsedNonRequiredExportedTypeIsKept_lib.cs" })]
	[SetupCompileAfter ("lib.dll", new[] { "Dependencies/UsedNonRequiredExportedTypeIsKept_fwd.cs" }, references: new[] { "libfwd.dll" })]

	[KeptAssembly ("libfwd.dll")]
	[KeptMemberInAssembly ("libfwd.dll", typeof (UsedNonRequiredExportedTypeIsKept_Used1), "field", ExpectationAssemblyName = "lib.dll")]
	[KeptMemberInAssembly ("libfwd.dll", typeof (UsedNonRequiredExportedTypeIsKept_Used2), "Method()", ExpectationAssemblyName = "lib.dll")]
	[KeptMemberInAssembly ("libfwd.dll", typeof (UsedNonRequiredExportedTypeIsKept_Used3), "Method()", ExpectationAssemblyName = "lib.dll")]
	[RemovedAssembly ("lib.dll")]
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