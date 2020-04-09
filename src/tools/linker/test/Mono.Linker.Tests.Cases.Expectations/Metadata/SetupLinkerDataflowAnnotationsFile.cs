using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SetupLinkerDataflowAnnotationsFile : BaseMetadataAttribute
	{
		public SetupLinkerDataflowAnnotationsFile (string relativePathToFile, string destinationFileName = null)
		{
		}
	}
}
