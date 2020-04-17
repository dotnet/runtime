using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SetupLinkerAttributeDefinitionsFile : BaseMetadataAttribute
	{
		public SetupLinkerAttributeDefinitionsFile (string relativePathToFile, string destinationFileName = null)
		{
		}
	}
}
