using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SetupLinkerDescriptorFile : BaseMetadataAttribute
	{
		public SetupLinkerDescriptorFile (string relativePathToFile, string destinationFileName = null)
		{
		}
	}
}
