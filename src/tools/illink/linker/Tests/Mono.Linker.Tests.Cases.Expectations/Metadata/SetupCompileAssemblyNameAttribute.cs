using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false)]
	public class SetupCompileAssemblyNameAttribute : BaseMetadataAttribute {
		public SetupCompileAssemblyNameAttribute (string outputName)
		{
			if (string.IsNullOrEmpty (outputName))
				throw new ArgumentNullException (nameof (outputName));
		}
	}
}