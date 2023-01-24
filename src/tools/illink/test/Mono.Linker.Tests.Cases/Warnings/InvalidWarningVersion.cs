using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings
{
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
