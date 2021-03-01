namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	public sealed class StripDescriptorsAttribute : BaseMetadataAttribute
	{
		public readonly bool Value;

		public StripDescriptorsAttribute (bool value)
		{
			Value = value;
		}
	}
}
