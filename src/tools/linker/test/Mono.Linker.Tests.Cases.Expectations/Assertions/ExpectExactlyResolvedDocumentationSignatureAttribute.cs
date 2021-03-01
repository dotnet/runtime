namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	/// Asserts that the given documentation signature string resolves to the
	// member with this attribute, and only that member.
	public class ExpectExactlyResolvedDocumentationSignatureAttribute : BaseMemberAssertionAttribute
	{
		public ExpectExactlyResolvedDocumentationSignatureAttribute (string input)
		{
		}
	}
}