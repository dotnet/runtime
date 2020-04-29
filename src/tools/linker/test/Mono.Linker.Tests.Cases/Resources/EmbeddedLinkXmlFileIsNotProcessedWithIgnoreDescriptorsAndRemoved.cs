using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Resources
{
	[SetupLinkerCoreAction ("link")]
	[IgnoreDescriptors (true)]
	[StripDescriptors (true)]

	// We need to rename the resource so that it matches the name of an assembly being processed.  This is a requriement of the black list step
	[SetupCompileResource ("Dependencies/EmbeddedLinkXmlFileIsNotProcessedWithIgnoreDescriptorsAndRemoved.xml", "test.xml")]
	[SkipPeVerify]
	[RemovedResourceInAssembly ("test.exe", "test.xml")]
	public class EmbeddedLinkXmlFileIsNotProcessedWithIgnoreDescriptorsAndRemoved
	{
		public static void Main ()
		{
		}

		public class Unused
		{
		}
	}
}
