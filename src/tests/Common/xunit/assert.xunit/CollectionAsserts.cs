#if XUNIT_NULLABLE
#nullable enable
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit.Sdk;

#if XUNIT_VALUETASK
using System.Threading.Tasks;
#endif

namespace Xunit
{
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	partial class Assert
	{
		/// <summary>
		/// Verifies that all items in the collection pass when executed against
		/// action.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="collection">The collection</param>
		/// <param name="action">The action to test each item against</param>
		/// <exception cref="AllException">Thrown when the collection contains at least one non-matching element</exception>
		public static void All<T>(
			IEnumerable<T> collection,
			Action<T> action)
		{
			GuardArgumentNotNull(nameof(collection), collection);
			GuardArgumentNotNull(nameof(action), action);

			All(collection, (item, index) => action(item));
		}

		/// <summary>
		/// Verifies that all items in the collection pass when executed against
		/// action. The item index is provided to the action, in addition to the item.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="collection">The collection</param>
		/// <param name="action">The action to test each item against</param>
		/// <exception cref="AllException">Thrown when the collection contains at least one non-matching element</exception>
		public static void All<T>(
			IEnumerable<T> collection,
			Action<T, int> action)
		{
			GuardArgumentNotNull(nameof(collection), collection);
			GuardArgumentNotNull(nameof(action), action);

#if XUNIT_NULLABLE
			var errors = new Stack<Tuple<int, object?, Exception>>();
#else
			var errors = new Stack<Tuple<int, object, Exception>>();
#endif
			var idx = 0;

			foreach (var item in collection)
			{
				try
				{
					action(item, idx);
				}
				catch (Exception ex)
				{
#if XUNIT_NULLABLE
					errors.Push(new Tuple<int, object?, Exception>(idx, item, ex));
#else
					errors.Push(new Tuple<int, object, Exception>(idx, item, ex));
#endif
				}

				++idx;
			}

			if (errors.Count > 0)
				throw new AllException(idx, errors.ToArray());
		}

#if XUNIT_VALUETASK
		/// <summary>
		/// Verifies that all items in the collection pass when executed against
		/// action.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="collection">The collection</param>
		/// <param name="action">The action to test each item against</param>
		/// <exception cref="AllException">Thrown when the collection contains at least one non-matching element</exception>
		public static async ValueTask AllAsync<T>(
			IEnumerable<T> collection,
			Func<T, ValueTask> action)
		{
			GuardArgumentNotNull(nameof(collection), collection);
			GuardArgumentNotNull(nameof(action), action);

			await AllAsync(collection, async (item, index) => await action(item));
		}

		/// <summary>
		/// Verifies that all items in the collection pass when executed against
		/// action. The item index is provided to the action, in addition to the item.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="collection">The collection</param>
		/// <param name="action">The action to test each item against</param>
		/// <exception cref="AllException">Thrown when the collection contains at least one non-matching element</exception>
		public static async ValueTask AllAsync<T>(
			IEnumerable<T> collection,
			Func<T, int, ValueTask> action)
		{
			GuardArgumentNotNull(nameof(collection), collection);
			GuardArgumentNotNull(nameof(action), action);

#if XUNIT_NULLABLE
			var errors = new Stack<Tuple<int, object?, Exception>>();
#else
			var errors = new Stack<Tuple<int, object, Exception>>();
#endif
			var idx = 0;

			foreach (var item in collection)
			{
				try
				{
					await action(item, idx);
				}
				catch (Exception ex)
				{
#if XUNIT_NULLABLE
					errors.Push(new Tuple<int, object?, Exception>(idx, item, ex));
#else
					errors.Push(new Tuple<int, object, Exception>(idx, item, ex));
#endif
				}

				++idx;
			}

			if (errors.Count > 0)
				throw new AllException(idx, errors.ToArray());
		}
#endif

		/// <summary>
		/// Verifies that a collection contains exactly a given number of elements, which meet
		/// the criteria provided by the element inspectors.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="collection">The collection to be inspected</param>
		/// <param name="elementInspectors">The element inspectors, which inspect each element in turn. The
		/// total number of element inspectors must exactly match the number of elements in the collection.</param>
		public static void Collection<T>(
			IEnumerable<T> collection,
			params Action<T>[] elementInspectors)
		{
			GuardArgumentNotNull(nameof(collection), collection);
			GuardArgumentNotNull(nameof(elementInspectors), elementInspectors);

			var elements = collection.ToArray();
			var expectedCount = elementInspectors.Length;
			var actualCount = elements.Length;

			if (expectedCount != actualCount)
				throw new CollectionException(collection, expectedCount, actualCount);

			for (var idx = 0; idx < actualCount; idx++)
			{
				try
				{
					elementInspectors[idx](elements[idx]);
				}
				catch (Exception ex)
				{
					throw new CollectionException(collection, expectedCount, actualCount, idx, ex);
				}
			}
		}

#if XUNIT_VALUETASK
		/// <summary>
		/// Verifies that a collection contains exactly a given number of elements, which meet
		/// the criteria provided by the element inspectors.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="collection">The collection to be inspected</param>
		/// <param name="elementInspectors">The element inspectors, which inspect each element in turn. The
		/// total number of element inspectors must exactly match the number of elements in the collection.</param>
		public static async ValueTask CollectionAsync<T>(
			IEnumerable<T> collection,
			params Func<T, ValueTask>[] elementInspectors)
		{
			GuardArgumentNotNull(nameof(collection), collection);
			GuardArgumentNotNull(nameof(elementInspectors), elementInspectors);

			var elements = collection.ToArray();
			var expectedCount = elementInspectors.Length;
			var actualCount = elements.Length;

			if (expectedCount != actualCount)
				throw new CollectionException(collection, expectedCount, actualCount);

			for (var idx = 0; idx < actualCount; idx++)
				try
				{
					await elementInspectors[idx](elements[idx]);
				}
				catch (Exception ex)
				{
					throw new CollectionException(collection, expectedCount, actualCount, idx, ex);
				}
		}
#endif

		/// <summary>
		/// Verifies that a collection contains a given object.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="expected">The object expected to be in the collection</param>
		/// <param name="collection">The collection to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the object is not present in the collection</exception>
		public static void Contains<T>(
			T expected,
			IEnumerable<T> collection)
		{
			GuardArgumentNotNull(nameof(collection), collection);

			// If an equality comparer is not explicitly provided, call into ICollection<T>.Contains or
			// IReadOnlyCollection<T>.Contains which may use the collection's equality comparer for types
			// like HashSet and Dictionary. HashSet and Dictionary are normally handled explicitly, but
			// the developer may end up in the IEnumerable<> override because the variable is not an explicit
			// enough type.
			var readWriteCollection = collection as ICollection<T>;
			if (readWriteCollection != null)
			{
				if (readWriteCollection.Contains(expected))
					return;
			}
			else
			{
				var readOnlyCollection = collection as IReadOnlyCollection<T>;
				if (readOnlyCollection != null && readOnlyCollection.Contains(expected))
					return;
			}

			Contains(expected, collection, GetEqualityComparer<T>());
		}

		/// <summary>
		/// Verifies that a collection contains a given object, using an equality comparer.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="expected">The object expected to be in the collection</param>
		/// <param name="collection">The collection to be inspected</param>
		/// <param name="comparer">The comparer used to equate objects in the collection with the expected object</param>
		/// <exception cref="ContainsException">Thrown when the object is not present in the collection</exception>
		public static void Contains<T>(
			T expected,
			IEnumerable<T> collection,
			IEqualityComparer<T> comparer)
		{
			GuardArgumentNotNull(nameof(collection), collection);
			GuardArgumentNotNull(nameof(comparer), comparer);

			if (collection.Contains(expected, comparer))
				return;

			throw new ContainsException(expected, collection);
		}

		/// <summary>
		/// Verifies that a collection contains a given object.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="collection">The collection to be inspected</param>
		/// <param name="filter">The filter used to find the item you're ensuring the collection contains</param>
		/// <exception cref="ContainsException">Thrown when the object is not present in the collection</exception>
		public static void Contains<T>(
			IEnumerable<T> collection,
			Predicate<T> filter)
		{
			GuardArgumentNotNull(nameof(collection), collection);
			GuardArgumentNotNull(nameof(filter), filter);

			foreach (var item in collection)
				if (filter(item))
					return;

			throw new ContainsException("(filter expression)", collection);
		}

		/// <summary>
		/// Verifies that a collection contains each object only once.
		/// </summary>
		/// <typeparam name="T">The type of the object to be compared</typeparam>
		/// <param name="collection">The collection to be inspected</param>
		/// <exception cref="ContainsDuplicateException">Thrown when an object is present inside the container more than once</exception>
		public static void Distinct<T>(IEnumerable<T> collection) =>
			Distinct<T>(collection, EqualityComparer<T>.Default);

		/// <summary>
		/// Verifies that a collection contains each object only once.
		/// </summary>
		/// <typeparam name="T">The type of the object to be compared</typeparam>
		/// <param name="collection">The collection to be inspected</param>
		/// <param name="comparer">The comparer used to equate objects in the collection with the expected object</param>
		/// <exception cref="ContainsDuplicateException">Thrown when an object is present inside the container more than once</exception>
		public static void Distinct<T>(
			IEnumerable<T> collection,
			IEqualityComparer<T> comparer)
		{
			GuardArgumentNotNull(nameof(collection), collection);
			GuardArgumentNotNull(nameof(comparer), comparer);

			var set = new HashSet<T>(comparer);

			foreach (var x in collection)
				if (!set.Add(x))
					throw new ContainsDuplicateException(x, collection);
		}

		/// <summary>
		/// Verifies that a collection does not contain a given object.
		/// </summary>
		/// <typeparam name="T">The type of the object to be compared</typeparam>
		/// <param name="expected">The object that is expected not to be in the collection</param>
		/// <param name="collection">The collection to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the object is present inside the container</exception>
		public static void DoesNotContain<T>(
			T expected,
			IEnumerable<T> collection)
		{
			GuardArgumentNotNull(nameof(collection), collection);

			// If an equality comparer is not explicitly provided, call into ICollection<T>.Contains or
			// IReadOnlyCollection<T>.Contains which may use the collection's equality comparer for types
			// like HashSet and Dictionary. HashSet and Dictionary are normally handled explicitly, but
			// the developer may end up in the IEnumerable<> override because the variable is not an explicit
			// enough type.
			var readWriteCollection = collection as ICollection<T>;
			if (readWriteCollection != null)
			{
				if (readWriteCollection.Contains(expected))
					throw new DoesNotContainException(expected, collection);
			}
			else
			{
				var readOnlyCollection = collection as IReadOnlyCollection<T>;
				if (readOnlyCollection != null && readOnlyCollection.Contains(expected))
					throw new DoesNotContainException(expected, collection);
			}

			DoesNotContain(expected, collection, GetEqualityComparer<T>());
		}

		/// <summary>
		/// Verifies that a collection does not contain a given object, using an equality comparer.
		/// </summary>
		/// <typeparam name="T">The type of the object to be compared</typeparam>
		/// <param name="expected">The object that is expected not to be in the collection</param>
		/// <param name="collection">The collection to be inspected</param>
		/// <param name="comparer">The comparer used to equate objects in the collection with the expected object</param>
		/// <exception cref="DoesNotContainException">Thrown when the object is present inside the container</exception>
		public static void DoesNotContain<T>(
			T expected,
			IEnumerable<T> collection,
			IEqualityComparer<T> comparer)
		{
			GuardArgumentNotNull(nameof(collection), collection);
			GuardArgumentNotNull(nameof(comparer), comparer);

			if (!collection.Contains(expected, comparer))
				return;

			throw new DoesNotContainException(expected, collection);
		}

		/// <summary>
		/// Verifies that a collection does not contain a given object.
		/// </summary>
		/// <typeparam name="T">The type of the object to be compared</typeparam>
		/// <param name="collection">The collection to be inspected</param>
		/// <param name="filter">The filter used to find the item you're ensuring the collection does not contain</param>
		/// <exception cref="DoesNotContainException">Thrown when the object is present inside the container</exception>
		public static void DoesNotContain<T>(
			IEnumerable<T> collection,
			Predicate<T> filter)
		{
			GuardArgumentNotNull(nameof(collection), collection);
			GuardArgumentNotNull(nameof(filter), filter);

			foreach (var item in collection)
				if (filter(item))
					throw new DoesNotContainException("(filter expression)", collection);
		}

		/// <summary>
		/// Verifies that a collection is empty.
		/// </summary>
		/// <param name="collection">The collection to be inspected</param>
		/// <exception cref="ArgumentNullException">Thrown when the collection is null</exception>
		/// <exception cref="EmptyException">Thrown when the collection is not empty</exception>
		public static void Empty(IEnumerable collection)
		{
			GuardArgumentNotNull(nameof(collection), collection);

			var enumerator = collection.GetEnumerator();
			try
			{
				if (enumerator.MoveNext())
					throw new EmptyException(collection);
			}
			finally
			{
				(enumerator as IDisposable)?.Dispose();
			}
		}

		/// <summary>
		/// Verifies that two sequences are equivalent, using a default comparer.
		/// </summary>
		/// <typeparam name="T">The type of the objects to be compared</typeparam>
		/// <param name="expected">The expected value</param>
		/// <param name="actual">The value to be compared against</param>
		/// <exception cref="EqualException">Thrown when the objects are not equal</exception>
		public static void Equal<T>(
#if XUNIT_NULLABLE
			IEnumerable<T>? expected,
			IEnumerable<T>? actual) =>
#else
			IEnumerable<T> expected,
			IEnumerable<T> actual) =>
#endif
				Equal(expected, actual, GetEqualityComparer<IEnumerable<T>>());

		/// <summary>
		/// Verifies that two sequences are equivalent, using a custom equatable comparer.
		/// </summary>
		/// <typeparam name="T">The type of the objects to be compared</typeparam>
		/// <param name="expected">The expected value</param>
		/// <param name="actual">The value to be compared against</param>
		/// <param name="comparer">The comparer used to compare the two objects</param>
		/// <exception cref="EqualException">Thrown when the objects are not equal</exception>
		public static void Equal<T>(
#if XUNIT_NULLABLE
			IEnumerable<T>? expected,
			IEnumerable<T>? actual,
#else
			IEnumerable<T> expected,
			IEnumerable<T> actual,
#endif
			IEqualityComparer<T> comparer) =>
				Equal(expected, actual, GetEqualityComparer<IEnumerable<T>>(new AssertEqualityComparerAdapter<T>(comparer)));

		/// <summary>
		/// Verifies that a collection is not empty.
		/// </summary>
		/// <param name="collection">The collection to be inspected</param>
		/// <exception cref="ArgumentNullException">Thrown when a null collection is passed</exception>
		/// <exception cref="NotEmptyException">Thrown when the collection is empty</exception>
		public static void NotEmpty(IEnumerable collection)
		{
			GuardArgumentNotNull(nameof(collection), collection);

			var enumerator = collection.GetEnumerator();
			try
			{
				if (!enumerator.MoveNext())
					throw new NotEmptyException();
			}
			finally
			{
				(enumerator as IDisposable)?.Dispose();
			}
		}

		/// <summary>
		/// Verifies that two sequences are not equivalent, using a default comparer.
		/// </summary>
		/// <typeparam name="T">The type of the objects to be compared</typeparam>
		/// <param name="expected">The expected object</param>
		/// <param name="actual">The actual object</param>
		/// <exception cref="NotEqualException">Thrown when the objects are equal</exception>
		public static void NotEqual<T>(
#if XUNIT_NULLABLE
			IEnumerable<T>? expected,
			IEnumerable<T>? actual) =>
#else
			IEnumerable<T> expected,
			IEnumerable<T> actual) =>
#endif
				NotEqual(expected, actual, GetEqualityComparer<IEnumerable<T>>());

		/// <summary>
		/// Verifies that two sequences are not equivalent, using a custom equality comparer.
		/// </summary>
		/// <typeparam name="T">The type of the objects to be compared</typeparam>
		/// <param name="expected">The expected object</param>
		/// <param name="actual">The actual object</param>
		/// <param name="comparer">The comparer used to compare the two objects</param>
		/// <exception cref="NotEqualException">Thrown when the objects are equal</exception>
		public static void NotEqual<T>(
#if XUNIT_NULLABLE
			IEnumerable<T>? expected,
			IEnumerable<T>? actual,
#else
			IEnumerable<T> expected,
			IEnumerable<T> actual,
#endif
			IEqualityComparer<T> comparer) =>
				NotEqual(expected, actual, GetEqualityComparer<IEnumerable<T>>(new AssertEqualityComparerAdapter<T>(comparer)));

		/// <summary>
		/// Verifies that the given collection contains only a single
		/// element of the given type.
		/// </summary>
		/// <param name="collection">The collection.</param>
		/// <returns>The single item in the collection.</returns>
		/// <exception cref="SingleException">Thrown when the collection does not contain
		/// exactly one element.</exception>
#if XUNIT_NULLABLE
		public static object? Single(IEnumerable collection)
#else
		public static object Single(IEnumerable collection)
#endif
		{
			GuardArgumentNotNull(nameof(collection), collection);

			return Single(collection.Cast<object>());
		}

		/// <summary>
		/// Verifies that the given collection contains only a single
		/// element of the given value. The collection may or may not
		/// contain other values.
		/// </summary>
		/// <param name="collection">The collection.</param>
		/// <param name="expected">The value to find in the collection.</param>
		/// <returns>The single item in the collection.</returns>
		/// <exception cref="SingleException">Thrown when the collection does not contain
		/// exactly one element.</exception>
		public static void Single(
			IEnumerable collection,
#if XUNIT_NULLABLE
			object? expected)
#else
			object expected)
#endif
		{
			GuardArgumentNotNull(nameof(collection), collection);

			GetSingleResult(collection.Cast<object>(), item => object.Equals(item, expected), ArgumentFormatter.Format(expected));
		}

		/// <summary>
		/// Verifies that the given collection contains only a single
		/// element of the given type.
		/// </summary>
		/// <typeparam name="T">The collection type.</typeparam>
		/// <param name="collection">The collection.</param>
		/// <returns>The single item in the collection.</returns>
		/// <exception cref="SingleException">Thrown when the collection does not contain
		/// exactly one element.</exception>
		public static T Single<T>(IEnumerable<T> collection)
		{
			GuardArgumentNotNull(nameof(collection), collection);

			return GetSingleResult(collection, null, null);
		}

		/// <summary>
		/// Verifies that the given collection contains only a single
		/// element of the given type which matches the given predicate. The
		/// collection may or may not contain other values which do not
		/// match the given predicate.
		/// </summary>
		/// <typeparam name="T">The collection type.</typeparam>
		/// <param name="collection">The collection.</param>
		/// <param name="predicate">The item matching predicate.</param>
		/// <returns>The single item in the filtered collection.</returns>
		/// <exception cref="SingleException">Thrown when the filtered collection does
		/// not contain exactly one element.</exception>
		public static T Single<T>(
			IEnumerable<T> collection,
			Predicate<T> predicate)
		{
			GuardArgumentNotNull(nameof(collection), collection);
			GuardArgumentNotNull(nameof(predicate), predicate);

			return GetSingleResult(collection, predicate, "(filter expression)");
		}

		static T GetSingleResult<T>(
			IEnumerable<T> collection,
#if XUNIT_NULLABLE
			Predicate<T>? predicate,
			string? expectedArgument)
#else
			Predicate<T> predicate,
			string expectedArgument)
#endif
		{
			var count = 0;
			var result = default(T);

			foreach (var item in collection)
				if (predicate == null || predicate(item))
					if (++count == 1)
						result = item;

			switch (count)
			{
				case 0:
					throw SingleException.Empty(expectedArgument);
				case 1:
#if XUNIT_NULLABLE
					return result!;
#else
					return result;
#endif
				default:
					throw SingleException.MoreThanOne(count, expectedArgument);
			}
		}
	}
}
