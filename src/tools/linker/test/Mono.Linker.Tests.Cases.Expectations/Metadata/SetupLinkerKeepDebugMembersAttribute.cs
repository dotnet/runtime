using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	[AttributeUsage (AttributeTargets.Class)]
	public class SetupLinkerKeepDebugMembersAttribute : BaseMetadataAttribute{
		public SetupLinkerKeepDebugMembersAttribute (string value)
		{
			if (string.IsNullOrEmpty (value))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (value));
		}
	}
}