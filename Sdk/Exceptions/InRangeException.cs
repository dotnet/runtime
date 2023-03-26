#if XUNIT_NULLABLE
#nullable enable
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when a value is unexpectedly not in the given range.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class InRangeException : XunitException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="InRangeException"/> class.
		/// </summary>
		/// <param name="actual">The actual object value</param>
		/// <param name="low">The low value of the range</param>
		/// <param name="high">The high value of the range</param>
		public InRangeException(
#if XUNIT_NULLABLE
			object? actual,
			object? low,
			object? high) :
#else
			object actual,
			object low,
			object high) :
#endif
				base("Assert.InRange() Failure")
		{
			Low = low?.ToString();
			High = high?.ToString();
			Actual = actual?.ToString();
		}

		/// <summary>
		/// Gets the actual object value
		/// </summary>
#if XUNIT_NULLABLE
		public string? Actual { get; }
#else
		public string Actual { get; }
#endif

		/// <summary>
		/// Gets the high value of the range
		/// </summary>
#if XUNIT_NULLABLE
		public string? High { get; }
#else
		public string High { get; }
#endif

		/// <summary>
		/// Gets the low value of the range
		/// </summary>
#if XUNIT_NULLABLE
		public string? Low { get; }
#else
		public string Low { get; }
#endif

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <returns>The error message that explains the reason for the exception, or an empty string("").</returns>
		public override string Message =>
			$"{base.Message}\r\nRange:  ({Low} - {High})\r\nActual: {Actual ?? "(null)"}";
	}
}
