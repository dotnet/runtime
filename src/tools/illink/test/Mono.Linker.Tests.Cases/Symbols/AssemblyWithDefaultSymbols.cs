using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Symbols
{
	[SetupCompileArgument ("/debug:full")]
	[SetupLinkerLinkSymbols ("false")]
	[RemovedSymbols ("test.exe")]
	public class AssemblyWithDefaultSymbols
	{
		static void Main ()
		{
		}
	}
}