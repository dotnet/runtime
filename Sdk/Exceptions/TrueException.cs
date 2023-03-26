#if XUNIT_NULLABLE
#nullable enable
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when a value is unexpectedly false.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class TrueException : AssertActualExpectedException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="TrueException"/> class.
		/// </summary>
		/// <param name="userMessage">The user message to be displayed, or null for the default message</param>
		/// <param name="value">The actual value</param>
		public TrueException(
#if XUNIT_NULLABLE
			string? userMessage,
#else
			string userMessage,
#endif
			bool? value) :
				base("True", value?.ToString() ?? "(null)", userMessage ?? "Assert.True() Failure")
		{ }
	}
}
