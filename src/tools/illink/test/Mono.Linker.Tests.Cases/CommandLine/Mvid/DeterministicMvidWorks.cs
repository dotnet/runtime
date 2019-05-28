using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CommandLine.Mvid {
	[SetupLinkerArgument ("--deterministic")]
	public class DeterministicMvidWorks {
		public static void Main ()
		{
			Method ();
		}

		[Kept]
		static void Method ()
		{
		}
	}
}