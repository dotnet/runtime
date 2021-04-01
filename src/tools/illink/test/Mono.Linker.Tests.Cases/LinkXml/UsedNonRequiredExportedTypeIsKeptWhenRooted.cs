using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("UsedNonRequiredExportedTypeIsKeptWhenRooted.xml")]
	[SetupLinkerArgument ("-a", "lib.dll", "visible")]

	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/UsedNonRequiredExportedTypeIsKeptWhenRooted_lib.cs" })]
	[SetupCompileAfter ("libfwd.dll", new[] { "Dependencies/UsedNonRequiredExportedTypeIsKeptWhenRooted_lib.cs" })]
	[SetupCompileAfter ("lib.dll", new[] { "Dependencies/UsedNonRequiredExportedTypeIsKeptWhenRooted_fwd.cs" }, references: new[] { "libfwd.dll" })]

	[KeptAssembly ("libfwd.dll")]
	[KeptAssembly ("lib.dll")]
	[KeptMemberInAssembly ("libfwd.dll", typeof (UsedNonRequiredExportedTypeIsKeptWhenRooted_Used), "field", ExpectationAssemblyName = "lib.dll")]
	[KeptMemberInAssembly ("libfwd.dll", typeof (UsedNonRequiredExportedTypeIsKeptWhenRooted_Used), "Method()", ExpectationAssemblyName = "lib.dll")]
	public class UsedNonRequiredExportedTypeIsKeptWhenRooted
	{
		public static void Main ()
		{
			var tmp = typeof (UsedNonRequiredExportedTypeIsKeptWhenRooted_Used).ToString ();
		}
	}
}