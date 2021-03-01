namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	public sealed class StripSubstitutionsAttribute : BaseMetadataAttribute
	{
		public readonly bool Value;

		public StripSubstitutionsAttribute (bool value)
		{
			Value = value;
		}
	}
}
