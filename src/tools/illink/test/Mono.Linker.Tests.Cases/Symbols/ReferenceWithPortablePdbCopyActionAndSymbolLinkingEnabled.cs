using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols
{
	[SetupCompileBefore ("LibraryWithPortablePdbSymbols.dll", new[] { "Dependencies/LibraryWithPortablePdbSymbols.cs" }, additionalArguments: "/debug:portable", compilerToUse: "csc")]
	[SetupLinkerLinkSymbols ("true")]
	[SetupLinkerAction ("copy", "LibraryWithPortablePdbSymbols")]

	[KeptSymbols ("LibraryWithPortablePdbSymbols.dll")]
	public class ReferenceWithPortablePdbCopyActionAndSymbolLinkingEnabled
	{
		static void Main ()
		{
			LibraryWithPortablePdbSymbols.SomeMethod ();
		}
	}
}