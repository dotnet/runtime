using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Resources {
	[SetupLinkerCoreAction ("link")]
	[IncludeBlacklistStep (false)]

	// We need to rename the resource so that it matches the name of an assembly being processed.  This is a requriement of the black list step
	[SetupCompileResource ("Dependencies/EmbeddedLinkXmlFileIsNotProcessedWhenBlacklistStepIsDisabled.xml", "test.xml")]
	[SkipPeVerify]
	[KeptResource ("test.xml")]
	public class EmbeddedLinkXmlFileIsNotProcessedWhenBlacklistStepIsDisabled {
		public static void Main ()
		{
		}

		public class Unused {
		}
	}
}
