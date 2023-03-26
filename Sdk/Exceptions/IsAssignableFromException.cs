#if XUNIT_NULLABLE
#nullable enable
#endif

using System;

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when the value is unexpectedly not of the given type or a derived type.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class IsAssignableFromException : AssertActualExpectedException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="IsTypeException"/> class.
		/// </summary>
		/// <param name="expected">The expected type</param>
		/// <param name="actual">The actual object value</param>
		public IsAssignableFromException(
			Type expected,
#if XUNIT_NULLABLE
			object? actual) :
#else
			object actual) :
#endif
				base(expected, actual?.GetType(), "Assert.IsAssignableFrom() Failure")
		{ }
	}
}
