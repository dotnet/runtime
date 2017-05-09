using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	[AttributeUsage (AttributeTargets.Class)]
	public class CoreLinkAttribute : BaseMetadataAttribute {
		public readonly string Value;

		public CoreLinkAttribute (string value)
		{
			Value = value;
		}
	}
}