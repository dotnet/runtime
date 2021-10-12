namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	public sealed class IgnoreLinkAttributesAttribute : BaseMetadataAttribute
	{
		public readonly bool Value;

		public IgnoreLinkAttributesAttribute (bool value)
		{
			Value = value;
		}
	}
}
