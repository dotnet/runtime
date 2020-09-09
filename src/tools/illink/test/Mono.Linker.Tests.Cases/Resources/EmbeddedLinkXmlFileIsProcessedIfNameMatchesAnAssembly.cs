using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Resources
{
	[IgnoreDescriptors (false)]

	// Rename the resource so that it matches the name of an assembly being processed.
	[SetupCompileResource ("Dependencies/EmbeddedLinkXmlFileIsProcessedIfNameMatchesAnAssembly.xml", "test.xml")]
	[SkipPeVerify]
	public class EmbeddedLinkXmlFileIsProcessedIfNameMatchesAnAssembly
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