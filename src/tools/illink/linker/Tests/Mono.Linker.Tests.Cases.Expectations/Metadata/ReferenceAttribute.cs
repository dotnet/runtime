using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	[AttributeUsage (AttributeTargets.Class)]
	public class ReferenceAttribute : BaseMetadataAttribute {
		public readonly string Value;

		public ReferenceAttribute (string value)
		{
			Value = value;
		}
	}
}
