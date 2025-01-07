using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Tracing.Individual
{
	[DumpDependencies]
	[SetupLinkerArgument ("--reduced-tracing", "true")]
	// Avoid excessive output from core assemblies
	[SetupLinkerTrimMode ("skip")]

	// Need to define a custom name so that the trimmer outputs in uncompressed format, which is more useful for making assertions
	[SetupLinkerArgument ("--dependencies-file", "linker-dependencies.xml")]
	public class CanEnableReducedTracing
	{
		public static void Main ()
		{
		}
	}
}
