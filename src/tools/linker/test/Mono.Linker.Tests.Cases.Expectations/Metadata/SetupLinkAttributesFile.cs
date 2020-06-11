using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SetupLinkAttributesFile : BaseMetadataAttribute
	{
		public SetupLinkAttributesFile (string relativePathToFile, string destinationFileName = null)
		{
		}
	}
}
