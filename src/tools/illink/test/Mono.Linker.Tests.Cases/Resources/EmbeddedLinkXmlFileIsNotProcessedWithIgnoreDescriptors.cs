using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Resources
{
	[IgnoreDescriptors (true)]
	[StripDescriptors (false)]

	[SetupCompileResource ("Dependencies/EmbeddedLinkXmlFileIsNotProcessedWithIgnoreDescriptors.xml", "ILLink.Descriptors.xml")]
	[SkipPeVerify]
	[KeptResource ("ILLink.Descriptors.xml")]
	public class EmbeddedLinkXmlFileIsNotProcessedWithIgnoreDescriptors
	{
		public static void Main ()
		{
		}

		public class Unused
		{
		}
	}
}
