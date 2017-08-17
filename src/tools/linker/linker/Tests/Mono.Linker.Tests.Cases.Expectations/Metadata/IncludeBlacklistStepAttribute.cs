namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	public sealed class IncludeBlacklistStepAttribute : BaseMetadataAttribute {
		public readonly bool Value;

		public IncludeBlacklistStepAttribute (bool value)
		{
			Value = value;
		}
	}
}
