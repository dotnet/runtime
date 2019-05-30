using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols {
	[SetupCompileBefore ("LibraryWithPortablePdbSymbols.dll", new[] { "Dependencies/LibraryWithPortablePdbSymbols.cs" }, additionalArguments: "/debug:portable", compilerToUse: "csc")]
	[SetupLinkerLinkSymbols ("true")]
	[SetupLinkerArgument ("--new-mvid", "true")]

	[KeptSymbols ("LibraryWithPortablePdbSymbols.dll")]

	[KeptMemberInAssembly ("LibraryWithPortablePdbSymbols.dll", typeof (LibraryWithPortablePdbSymbols), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithPortablePdbSymbols.dll", typeof (LibraryWithPortablePdbSymbols), "NotUsed()")]
	public class ReferenceWithPortablePdbAndSymbolLinkingEnabledAndNewMvid {
		static void Main()
		{
			LibraryWithPortablePdbSymbols.SomeMethod ();
		}
	}
}