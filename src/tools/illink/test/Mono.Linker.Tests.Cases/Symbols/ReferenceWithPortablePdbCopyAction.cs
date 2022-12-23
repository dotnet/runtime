using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols
{
	[SetupCompileBefore ("LibraryWithPortablePdbSymbols.dll", new[] { "Dependencies/LibraryWithPortablePdbSymbols.cs" }, additionalArguments: "/debug:portable", compilerToUse: "csc")]
	[SetupLinkerLinkSymbols ("false")]
	[SetupLinkerAction ("copy", "LibraryWithPortablePdbSymbols")]

	[RemovedSymbols ("LibraryWithPortablePdbSymbols.dll")]
	public class ReferenceWithPortablePdbCopyAction
	{
		static void Main ()
		{
			LibraryWithPortablePdbSymbols.SomeMethod ();
		}
	}
}