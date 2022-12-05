using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CommandLine.Mvid
{
	[SetupLinkerArgument ("--new-mvid", "false")]
	public class RetainMvid
	{
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