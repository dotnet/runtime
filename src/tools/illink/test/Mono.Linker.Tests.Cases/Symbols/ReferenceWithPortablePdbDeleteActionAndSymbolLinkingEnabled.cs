using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols
{
	[SetupCompileBefore ("LibraryWithPortablePdbSymbols.dll", new[] { "Dependencies/LibraryWithPortablePdbSymbols.cs" }, additionalArguments: new[] { "/debug:portable" }, compilerToUse: "csc")]
	[SetupLinkerLinkSymbols ("true")]

	[RemovedAssembly ("LibraryWithPortablePdbSymbols.dll")]
	[RemovedSymbols ("LibraryWithPortablePdbSymbols.dll")]
	public class ReferenceWithPortablePdbDeleteActionAndSymbolLinkingEnabled
	{
		static void Main ()
		{
		}

		/// <summary>
		/// By not using this method we will cause the trimmer to delete the reference
		/// </summary>
		static void UnusedCodePath ()
		{
			LibraryWithPortablePdbSymbols.SomeMethod ();
		}
	}
}