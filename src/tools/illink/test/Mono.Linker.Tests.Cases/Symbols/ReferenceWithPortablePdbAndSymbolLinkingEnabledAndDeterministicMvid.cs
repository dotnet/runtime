using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols {
	[IgnoreTestCase ("Requires cecil updated with fix for https://github.com/jbevain/cecil/issues/583")]
	[SetupCompileBefore ("LibraryWithPortablePdbSymbols.dll", new[] { "Dependencies/LibraryWithPortablePdbSymbols.cs" }, additionalArguments: "/debug:portable", compilerToUse: "csc")]
	[SetupLinkerLinkSymbols ("true")]
	[SetupLinkerArgument ("--deterministic")]

	[KeptSymbols ("LibraryWithPortablePdbSymbols.dll")]

	[KeptMemberInAssembly ("LibraryWithPortablePdbSymbols.dll", typeof (LibraryWithPortablePdbSymbols), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithPortablePdbSymbols.dll", typeof (LibraryWithPortablePdbSymbols), "NotUsed()")]
	public class ReferenceWithPortablePdbAndSymbolLinkingEnabledAndDeterministicMvid {
		static void Main()
		{
			LibraryWithPortablePdbSymbols.SomeMethod ();
		}
	}
}