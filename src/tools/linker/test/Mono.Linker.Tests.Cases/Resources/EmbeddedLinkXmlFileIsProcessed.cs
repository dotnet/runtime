using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Resources {
	[SetupLinkerCoreAction ("link")]
	[IncludeBlacklistStep (true)]

	// We need to rename the resource so that it matches the name of an assembly being processed.  This is a requriement of the black list step
	[SetupCompileResource ("Dependencies/EmbeddedLinkXmlFileIsProcessed.xml", "test.xml")]
	[SkipPeVerify]
	public class EmbeddedLinkXmlFileIsProcessed {
		public static void Main ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class Unused {
		}
	}
}
