using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Resources
{
	[IgnoreDescriptors (false)]
	[SetupCompileResource ("Dependencies/EmbeddedLinkXmlFileIsNotProcessedIfNameDoesNotMatchAnAssembly.xml", "NotMatchingAnAssemblyName.xml")]
	[SkipPeVerify]
	[KeptResource ("NotMatchingAnAssemblyName.xml")]
	public class EmbeddedLinkXmlFileIsNotProcessedIfNameDoesNotMatchAnAssembly
	{
		public static void Main ()
		{
		}

		public class Unused
		{
		}
	}
}
