namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	public sealed class IncludeBlacklistStepAttribute : BaseMetadataAttribute {
		public readonly string Value;

		public IncludeBlacklistStepAttribute (string value)
		{
			Value = value;
		}
	}
}
