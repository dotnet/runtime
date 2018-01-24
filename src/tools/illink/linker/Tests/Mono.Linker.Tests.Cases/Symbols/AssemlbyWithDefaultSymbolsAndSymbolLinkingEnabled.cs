using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: KeptAttributeAttribute (typeof (System.Diagnostics.DebuggableAttribute))]

namespace Mono.Linker.Tests.Cases.Symbols {
	[IgnoreTestCase ("Will fix in follow on PR.  Fails on OSX due to mbd symbol linking being broken")]
	[SetupCompileArgument ("/debug:full")]
	[SetupLinkerLinkSymbols ("true")]
	[KeptSymbols ("test.exe")]
	public class AssemlbyWithDefaultSymbolsAndSymbolLinkingEnabled {
		static void Main ()
		{
		}
	}
}