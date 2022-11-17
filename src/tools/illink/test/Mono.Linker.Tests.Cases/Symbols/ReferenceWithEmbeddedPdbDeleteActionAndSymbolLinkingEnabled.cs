using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols
{
	[SetupCompileBefore ("LibraryWithEmbeddedPdbSymbols.dll", new[] { "Dependencies/LibraryWithEmbeddedPdbSymbols.cs" }, additionalArguments: "/debug:embedded", compilerToUse: "csc")]
	[SetupLinkerLinkSymbols ("true")]

	[RemovedAssembly ("LibraryWithEmbeddedPdbSymbols.dll")]
	[RemovedSymbols ("LibraryWithEmbeddedPdbSymbols.dll")]
	public class ReferenceWithEmbeddedPdbDeleteActionAndSymbolLinkingEnabled
	{
		static void Main ()
		{
		}

		/// <summary>
		/// By not using this method we will cause the linker to delete the reference
		/// </summary>
		static void UnusedCodePath ()
		{
			LibraryWithEmbeddedPdbSymbols.SomeMethod ();
		}
	}
}