using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Symbols
{
	[SetupCompileArgument ("/debug:full")]
	[SetupLinkerLinkSymbols ("true")]
	[KeptSymbols ("test.exe")]
	public class AssemblyWithDefaultSymbolsAndSymbolLinkingEnabled
	{
		static void Main ()
		{
		}
	}
}