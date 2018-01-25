using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false)]
	public class SetupCSharpCompilerToUseAttribute : BaseMetadataAttribute {
		public SetupCSharpCompilerToUseAttribute (string name)
		{
			if (string.IsNullOrEmpty (name))
				throw new ArgumentNullException (nameof (name));
		}
	}
}
