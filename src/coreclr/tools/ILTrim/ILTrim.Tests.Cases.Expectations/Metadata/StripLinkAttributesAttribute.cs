namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	public sealed class StripLinkAttributesAttribute : BaseMetadataAttribute
	{
		public readonly bool Value;

		public StripLinkAttributesAttribute (bool value)
		{
			Value = value;
		}
	}
}
