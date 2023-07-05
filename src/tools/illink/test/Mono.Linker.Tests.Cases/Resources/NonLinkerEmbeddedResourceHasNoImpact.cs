using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Resources
{
	[IgnoreDescriptors (false)]

	[SetupCompileResource ("Dependencies/NonLinkerEmbeddedResourceHasNoImpact.xml", "ILLink.Descriptors.xml")]
	[SkipPeVerify]
	[KeptResource ("ILLink.Descriptors.xml")]
	public class NonLinkerEmbeddedResourceHasNoImpact
	{
		public static void Main ()
		{
		}
	}
}
