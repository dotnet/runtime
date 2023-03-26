#if XUNIT_NULLABLE
#nullable enable
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when the user calls <see cref="Assert"/>.<see cref="Assert.Fail(string)"/>.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class FailException : XunitException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="FailException"/> class.
		/// </summary>
		/// <param name="message">The user's failure message.</param>
		public FailException(string message) :
			base($"Assert.Fail(): {message}")
		{ }
	}
}
