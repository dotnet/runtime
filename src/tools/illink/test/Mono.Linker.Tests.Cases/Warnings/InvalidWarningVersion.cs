using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[ExpectNonZeroExitCode (1)]
	[IgnoreTestCase ("Ignore in NativeAOT, see https://github.com/dotnet/runtime/issues/82447", IgnoredBy = Tool.NativeAot)]
	[SetupLinkerArgument ("--verbose")]
	[SetupLinkerArgument ("--warn", "invalid")]
	[LogContains ("IL1016")]
	[NoLinkedOutput]
	public class InvalidWarningVersion
	{
		public static void Main ()
		{
		}
	}
}
