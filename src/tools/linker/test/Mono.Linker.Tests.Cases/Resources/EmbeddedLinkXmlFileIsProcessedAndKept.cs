using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Resources
{
	[IgnoreDescriptors (false)]
	[StripDescriptors (false)]

	[SetupCompileResource ("Dependencies/EmbeddedLinkXmlFileIsProcessedAndKept.xml", "ILLink.Descriptors.xml")]
	[SkipPeVerify]
	[KeptResource ("ILLink.Descriptors.xml")]
	public class EmbeddedLinkXmlFileIsProcessedAndKept
	{
		public static void Main ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class Unused
		{
		}
	}
}
