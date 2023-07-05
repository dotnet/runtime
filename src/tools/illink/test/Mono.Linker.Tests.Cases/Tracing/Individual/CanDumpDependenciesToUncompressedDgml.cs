using System;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Tracing.Individual
{

	[SetupLinkerArgument ("--dump-dependencies")]
	[SetupLinkerArgument ("--dependencies-file-format", "Dgml")]
	[SetupLinkerArgument ("--dependencies-file", "linker-dependencies.dgml")]
	public class CanDumpDependenciesToUncompressedDgml
	{
		public static void Main ()
		{
		}
	}
}