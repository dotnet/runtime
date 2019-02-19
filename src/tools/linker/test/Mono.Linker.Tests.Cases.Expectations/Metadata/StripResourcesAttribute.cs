using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	public sealed class StripResourcesAttribute : BaseMetadataAttribute {
		public readonly bool Value;

		public StripResourcesAttribute (bool value)
		{
			Value = value;
		}
	}
}
