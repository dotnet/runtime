namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	public sealed class IgnoreDescriptorsAttribute : BaseMetadataAttribute
	{
		public readonly bool Value;

		public IgnoreDescriptorsAttribute (bool value)
		{
			Value = value;
		}
	}
}
