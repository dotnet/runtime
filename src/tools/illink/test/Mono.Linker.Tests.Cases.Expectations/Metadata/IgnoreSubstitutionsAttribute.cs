namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	public sealed class IgnoreSubstitutionsAttribute : BaseMetadataAttribute
	{
		public readonly bool Value;

		public IgnoreSubstitutionsAttribute (bool value)
		{
			Value = value;
		}
	}
}
