using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Resources {
	// I'm not sure why it was decided all link xml resources should be skipped when core link is set to skip, but that's the behavior
	// that exists today and I don't have a reason to change it right now
	[SetupLinkerCoreAction ("skip")]
	[IncludeBlacklistStep (true)]

	// We need to rename the resource so that it matches the name of an assembly being processed.  This is a requriement of the black list step
	[SetupCompileResource ("Dependencies/EmbeddedLinkXmlFileIsNotProcessedWhenCoreLinkActionIsSkip.xml", "test.xml")]
	[SkipPeVerify]
	[KeptResource ("test.xml")]
	public class EmbeddedLinkXmlFileIsNotProcessedWhenCoreLinkActionIsSkip {
		public static void Main ()
		{
		}

		public class Unused {
		}
	}
}
