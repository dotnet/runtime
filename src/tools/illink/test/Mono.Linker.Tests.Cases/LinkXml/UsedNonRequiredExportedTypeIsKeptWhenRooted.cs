using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[IgnoreTestCase ("NativeAOT doesn't implement 'visible' rooting behavior", IgnoredBy = Tool.NativeAot)]
	[KeptAttributeAttribute (typeof (IgnoreTestCaseAttribute), By = Tool.Trimmer)]

	[SetupLinkerDescriptorFile ("UsedNonRequiredExportedTypeIsKeptWhenRooted.xml")]
	[SetupLinkerArgument ("-a", "libfwd.dll", "visible")]

	[SetupCompileBefore ("libfwd.dll", new[] { "Dependencies/UsedNonRequiredExportedTypeIsKeptWhenRooted_lib.cs" })]
	[SetupCompileAfter ("lib.dll", new[] { "Dependencies/UsedNonRequiredExportedTypeIsKeptWhenRooted_lib.cs" })]
	[SetupCompileAfter ("libfwd.dll", new[] { "Dependencies/UsedNonRequiredExportedTypeIsKeptWhenRooted_fwd.cs" }, references: new[] { "lib.dll" })]

	[KeptAssembly ("lib.dll")]
	[KeptAssembly ("libfwd.dll")]
	[KeptMemberInAssembly ("lib.dll", typeof (UsedNonRequiredExportedTypeIsKeptWhenRooted_Used), "field", ExpectationAssemblyName = "libfwd.dll")]
	[KeptMemberInAssembly ("lib.dll", typeof (UsedNonRequiredExportedTypeIsKeptWhenRooted_Used), "Method()", ExpectationAssemblyName = "libfwd.dll")]
	public class UsedNonRequiredExportedTypeIsKeptWhenRooted
	{
		public static void Main ()
		{
			var tmp = typeof (UsedNonRequiredExportedTypeIsKeptWhenRooted_Used).ToString ();
		}
	}
}
