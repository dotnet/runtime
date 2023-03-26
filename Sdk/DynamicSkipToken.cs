#if XUNIT_NULLABLE
#nullable enable
#endif

namespace Xunit.Sdk
{
	static class DynamicSkipToken
	{
		/// <summary>
		/// The contract for exceptions which indicate that something should be skipped rather than
		/// failed is that exception message should start with this, and that any text following this
		/// will be treated as the skip reason (for example,
		/// "$XunitDynamicSkip$This code can only run on Linux") will result in a skipped test with
		/// the reason of "This code can only run on Linux".
		/// </summary>
		public const string Value = "$XunitDynamicSkip$";
	}
}
