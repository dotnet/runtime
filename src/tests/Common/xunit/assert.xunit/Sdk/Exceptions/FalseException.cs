#if XUNIT_NULLABLE
#nullable enable
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when a value is unexpectedly true.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class FalseException : AssertActualExpectedException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="FalseException"/> class.
		/// </summary>
		/// <param name="userMessage">The user message to be display, or <c>null</c> for the default message</param>
		/// <param name="value">The actual value</param>
		public FalseException(
#if XUNIT_NULLABLE
			string? userMessage,
#else
			string userMessage,
#endif
			bool? value) :
				base("False", value?.ToString() ?? "(null)", userMessage ?? "Assert.False() Failure")
		{ }
	}
}
