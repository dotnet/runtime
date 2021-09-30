using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols
{
#if !WIN32
	// .NET Core type forwarders cause the assembly action to be
	// changed from "copy" to "save" (to remove references to removed
	// typeforwarders). However, saving the native PDB is only
	// supported on windows.
	[IgnoreTestCase ("Only supported on Windows")]
#endif
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetFramework, "Only supported on Windows on .NET Framework.")]
	[Reference ("Dependencies/LibraryWithPdb/LibraryWithPdb.dll")]
	[ReferenceDependency ("Dependencies/LibraryWithPdb/LibraryWithPdb.pdb")]
	[SetupLinkerLinkSymbols ("true")]
	[SetupLinkerAction ("copy", "LibraryWithPdb")]

	[KeptSymbols ("LibraryWithPdb.dll")]
	public class ReferenceWithPdbCopyActionAndSymbolLinkingEnabled
	{
		static void Main ()
		{
			LibraryWithPdb.SomeMethod ();
		}
	}
}