namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	/// Asserts that the given documentation signature string resolves to the
	// member with this attribute.
	public class ExpectResolvedDocumentationSignatureAttribute : BaseMemberAssertionAttribute
	{
		public ExpectResolvedDocumentationSignatureAttribute (string input)
		{
		}
	}
}