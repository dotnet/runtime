#if XUNIT_NULLABLE
#nullable enable
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when the value is unexpectedly not of the exact given type.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class IsTypeException : AssertActualExpectedException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="IsTypeException"/> class.
		/// </summary>
		/// <param name="expectedTypeName">The expected type name</param>
		/// <param name="actualTypeName">The actual type name</param>
		public IsTypeException(
#if XUNIT_NULLABLE
			string? expectedTypeName,
			string? actualTypeName) :
#else
			string expectedTypeName,
			string actualTypeName) :
#endif
				base(expectedTypeName, actualTypeName, "Assert.IsType() Failure")
		{ }
	}
}
