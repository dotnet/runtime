namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	/// Asserts that the member to which this attribute is applied has the given
	/// documentation signature.
	public class ExpectGeneratedDocumentationSignatureAttribute : BaseMemberAssertionAttribute
	{
		public ExpectGeneratedDocumentationSignatureAttribute (string expected)
		{
		}
	}
}