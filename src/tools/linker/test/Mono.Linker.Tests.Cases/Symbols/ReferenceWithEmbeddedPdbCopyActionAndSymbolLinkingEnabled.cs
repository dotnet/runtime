using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols
{
	[SetupCompileBefore ("LibraryWithEmbeddedPdbSymbols.dll", new[] { "Dependencies/LibraryWithEmbeddedPdbSymbols.cs" }, additionalArguments: "/debug:embedded", compilerToUse: "csc")]
	[SetupLinkerLinkSymbols ("true")]
	[SetupLinkerAction ("copy", "LibraryWithEmbeddedPdbSymbols")]

	[KeptSymbols ("LibraryWithEmbeddedPdbSymbols.dll")]
	public class ReferenceWithEmbeddedPdbCopyActionAndSymbolLinkingEnabled
	{
		static void Main ()
		{
			LibraryWithEmbeddedPdbSymbols.SomeMethod ();
		}
	}
}