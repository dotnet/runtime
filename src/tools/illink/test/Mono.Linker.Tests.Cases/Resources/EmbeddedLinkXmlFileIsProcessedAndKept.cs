using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Resources
{
	[SetupLinkerCoreAction ("link")]
	[IncludeBlacklistStep (true)]
	[StripResources (false)]

	// We need to rename the resource so that it matches the name of an assembly being processed.  This is a requriement of the black list step
	[SetupCompileResource ("Dependencies/EmbeddedLinkXmlFileIsProcessedAndKept.xml", "test.xml")]
	[SkipPeVerify]
	[KeptResource ("test.xml")]
	public class EmbeddedLinkXmlFileIsProcessedAndKept {
		public static void Main ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class Unused {
		}
	}
}
