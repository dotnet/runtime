using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SetupCompileArgumentAttribute : BaseMetadataAttribute {
		public SetupCompileArgumentAttribute (string value)
		{
			if (string.IsNullOrEmpty (value))
				throw new ArgumentNullException (nameof(value));
		}
	}
}
