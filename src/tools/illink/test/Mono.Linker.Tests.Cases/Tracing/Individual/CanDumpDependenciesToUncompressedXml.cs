using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Tracing.Individual
{
	[DumpDependencies]
	[SetupLinkerArgument ("--dependencies-file", "linker-dependencies.xml")]
	public class CanDumpDependenciesToUncompressedXml
	{
		public static void Main ()
		{
		}
	}
}
