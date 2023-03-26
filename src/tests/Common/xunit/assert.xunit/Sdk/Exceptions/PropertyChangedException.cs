#if XUNIT_NULLABLE
#nullable enable
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when code unexpectedly fails change a property.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class PropertyChangedException : XunitException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="PropertyChangedException"/> class. Call this constructor
		/// when no exception was thrown.
		/// </summary>
		/// <param name="propertyName">The name of the property that was expected to be changed.</param>
		public PropertyChangedException(string propertyName) :
			base($"Assert.PropertyChanged failure: Property {propertyName} was not set")
		{ }
	}
}
