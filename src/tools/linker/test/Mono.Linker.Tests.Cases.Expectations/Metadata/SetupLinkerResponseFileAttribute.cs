using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SetupLinkerResponseFileAttribute : BaseMetadataAttribute {
		public SetupLinkerResponseFileAttribute(string relativePathToFile, string destinationFileName = null)
		{
			if (string.IsNullOrEmpty (relativePathToFile))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (relativePathToFile));
		}
	}
}