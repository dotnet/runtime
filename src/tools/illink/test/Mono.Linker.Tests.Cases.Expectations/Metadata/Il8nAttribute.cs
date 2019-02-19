using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	[AttributeUsage (AttributeTargets.Class)]
	public sealed class Il8nAttribute : BaseMetadataAttribute {
		public readonly string Value;

		public Il8nAttribute (string value)
		{
			Value = value;
		}
	}
}
