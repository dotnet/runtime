using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	[AttributeUsage (AttributeTargets.Class)]
	public class SandboxDependencyAttribute : BaseMetadataAttribute {
		public readonly string RelativePathToFile;

		public SandboxDependencyAttribute (string relativePathToFile)
		{
			RelativePathToFile = relativePathToFile;
		}
	}
}