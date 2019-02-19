using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	public sealed class SkipUnresolvedAttribute : BaseMetadataAttribute {
		public readonly bool Value;

		public SkipUnresolvedAttribute (bool value)
		{
			Value = value;
		}
	}
}
