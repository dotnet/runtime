#if XUNIT_NULLABLE
#nullable enable
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when the collection did not contain exactly the given number element.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class AssertCollectionCountException : XunitException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AssertCollectionCountException"/> class.
		/// </summary>
		/// <param name="expectedCount">The expected number of items in the collection.</param>
		/// <param name="actualCount">The actual number of items in the collection.</param>
		public AssertCollectionCountException(
			int expectedCount,
			int actualCount) :
				base($"The collection contained {actualCount} matching element(s) instead of {expectedCount}.")
		{ }
	}
}
