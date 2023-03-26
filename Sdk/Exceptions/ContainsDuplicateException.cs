#if XUNIT_NULLABLE
#nullable enable
#endif

using System.Collections;

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when <see cref="Assert.Distinct{T}(System.Collections.Generic.IEnumerable{T})" />
	/// or <see cref="Assert.Distinct{T}(System.Collections.Generic.IEnumerable{T}, System.Collections.Generic.IEqualityComparer{T})" />
	/// finds a duplicate entry in the collection
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class ContainsDuplicateException : XunitException
	{

		/// <summary>
		/// Creates a new instance of the <see cref="ContainsDuplicateException"/> class.
		/// </summary>
		/// <param name="duplicateObject">The object that was present twice in the collection.</param>
		/// <param name="collection">The collection that was checked for duplicate entries.</param>
		public ContainsDuplicateException(
#if XUNIT_NULLABLE
			object? duplicateObject,
			IEnumerable collection) :
#else
			object duplicateObject,
			IEnumerable collection) :
#endif
				base("Assert.Distinct() Failure")
		{
			DuplicateObject = duplicateObject;
			Collection = collection;
		}

		/// <summary>
		/// Gets the collection that was checked for duplicate entries.
		/// </summary>
		public IEnumerable Collection { get; }

		/// <summary>
		/// Gets the object that was present more than once in the collection.
		/// </summary>
#if XUNIT_NULLABLE
		public object? DuplicateObject { get; }
#else
		public object DuplicateObject { get; }
#endif

		/// <inheritdoc/>
		public override string Message =>
			$"{base.Message}: The item {ArgumentFormatter.Format(DuplicateObject)} occurs multiple times in {ArgumentFormatter.Format(Collection)}.";
	}
}
