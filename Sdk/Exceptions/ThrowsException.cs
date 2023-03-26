#if XUNIT_NULLABLE
#nullable enable
#endif

using System;

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when code unexpectedly fails to throw an exception.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class ThrowsException : AssertActualExpectedException
	{
#if XUNIT_NULLABLE
		readonly string? stackTrace = null;
#else
		readonly string stackTrace = null;
#endif

		/// <summary>
		/// Creates a new instance of the <see cref="ThrowsException"/> class. Call this constructor
		/// when no exception was thrown.
		/// </summary>
		/// <param name="expectedType">The type of the exception that was expected</param>
		public ThrowsException(Type expectedType)
			: this(expectedType, "(No exception was thrown)", null, null, null)
		{ }

		/// <summary>
		/// Creates a new instance of the <see cref="ThrowsException"/> class. Call this constructor
		/// when an exception of the wrong type was thrown.
		/// </summary>
		/// <param name="expectedType">The type of the exception that was expected</param>
		/// <param name="actual">The actual exception that was thrown</param>
		public ThrowsException(Type expectedType, Exception actual)
#if XUNIT_NULLABLE
			: this(expectedType, ArgumentFormatter.Format(actual.GetType())!, actual.Message, actual.StackTrace, actual)
#else
			: this(expectedType, ArgumentFormatter.Format(actual.GetType()), actual.Message, actual.StackTrace, actual)
#endif
		{ }

		/// <summary>
		/// THIS CONSTRUCTOR IS FOR UNIT TESTING PURPOSES ONLY.
		/// </summary>
#if XUNIT_NULLABLE
		protected ThrowsException(Type expected, string actual, string? actualMessage, string? stackTrace, Exception? innerException)
#else
		protected ThrowsException(Type expected, string actual, string actualMessage, string stackTrace, Exception innerException)
#endif
			: base(
				expected,
				actual + (actualMessage == null ? "" : ": " + actualMessage),
				"Assert.Throws() Failure",
				null,
				null,
				innerException
			)
		{
			this.stackTrace = stackTrace;
		}

		/// <summary>
		/// Gets a string representation of the frames on the call stack at the time the current exception was thrown.
		/// </summary>
		/// <returns>A string that describes the contents of the call stack, with the most recent method call appearing first.</returns>
#if XUNIT_NULLABLE
		public override string? StackTrace => stackTrace ?? base.StackTrace;
#else
		public override string StackTrace => stackTrace ?? base.StackTrace;
#endif
	}
}
