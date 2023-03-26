#if XUNIT_NULLABLE
#nullable enable
#endif

using System.Collections.Generic;
using Xunit.Sdk;

#if XUNIT_IMMUTABLE_COLLECTIONS
using System.Collections.Immutable;
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
		/// Verifies that the set contains the given object.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="expected">The object expected to be in the set</param>
		/// <param name="set">The set to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the object is not present in the set</exception>
		public static void Contains<T>(
			T expected,
			ISet<T> set)
		{
			GuardArgumentNotNull(nameof(set), set);

			// Do not forward to DoesNotContain(expected, set.Keys) as we want the default SDK behavior
			if (!set.Contains(expected))
				throw new ContainsException(expected, set);
		}

#if NET5_0_OR_GREATER
		/// <summary>
		/// Verifies that the read-only set contains the given object.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="expected">The object expected to be in the set</param>
		/// <param name="set">The set to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the object is not present in the set</exception>
		public static void Contains<T>(
			T expected,
			IReadOnlySet<T> set)
		{
			GuardArgumentNotNull(nameof(set), set);

			// Do not forward to DoesNotContain(expected, set.Keys) as we want the default SDK behavior
			if (!set.Contains(expected))
				throw new ContainsException(expected, set);
		}
#endif

		/// <summary>
		/// Verifies that the hashset contains the given object.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="expected">The object expected to be in the set</param>
		/// <param name="set">The set to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the object is not present in the set</exception>
		public static void Contains<T>(
			T expected,
			HashSet<T> set) =>
				Contains(expected, (ISet<T>)set);

#if XUNIT_IMMUTABLE_COLLECTIONS
		/// <summary>
		/// Verifies that the immutable hashset contains the given object.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="expected">The object expected to be in the set</param>
		/// <param name="set">The set to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the object is not present in the set</exception>
		public static void Contains<T>(
			T expected,
			ImmutableHashSet<T> set) =>
				Contains(expected, (ISet<T>)set);
#endif

		/// <summary>
		/// Verifies that the set does not contain the given item.
		/// </summary>
		/// <typeparam name="T">The type of the object to be compared</typeparam>
		/// <param name="expected">The object that is expected not to be in the set</param>
		/// <param name="set">The set to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the object is present inside the set</exception>
		public static void DoesNotContain<T>(
			T expected,
			ISet<T> set)
		{
			GuardArgumentNotNull(nameof(set), set);

			if (set.Contains(expected))
				throw new DoesNotContainException(expected, set);
		}

#if NET5_0_OR_GREATER
		/// <summary>
		/// Verifies that the read-only set does not contain the given item.
		/// </summary>
		/// <typeparam name="T">The type of the object to be compared</typeparam>
		/// <param name="expected">The object that is expected not to be in the set</param>
		/// <param name="set">The set to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the object is present inside the container</exception>
		public static void DoesNotContain<T>(
			T expected,
			IReadOnlySet<T> set)
		{
			GuardArgumentNotNull(nameof(set), set);

			if (set.Contains(expected))
				throw new DoesNotContainException(expected, set);
		}
#endif

		/// <summary>
		/// Verifies that the hashset does not contain the given item.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="expected">The object expected to be in the set</param>
		/// <param name="set">The set to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the object is not present in the set</exception>
		public static void DoesNotContain<T>(
			T expected,
			HashSet<T> set) =>
				DoesNotContain(expected, (ISet<T>)set);

#if XUNIT_IMMUTABLE_COLLECTIONS
		/// <summary>
		/// Verifies that the immutable hashset does not contain the given item.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="expected">The object expected to be in the set</param>
		/// <param name="set">The set to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the object is not present in the set</exception>
		public static void DoesNotContain<T>(
			T expected,
			ImmutableHashSet<T> set) =>
				DoesNotContain(expected, (ISet<T>)set);
#endif

		/// <summary>
		/// Verifies that a set is a proper subset of another set.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="expectedSuperset">The expected superset</param>
		/// <param name="actual">The set expected to be a proper subset</param>
		/// <exception cref="ContainsException">Thrown when the actual set is not a proper subset of the expected set</exception>
		public static void ProperSubset<T>(
			ISet<T> expectedSuperset,
#if XUNIT_NULLABLE
			ISet<T>? actual)
#else
			ISet<T> actual)
#endif
		{
			GuardArgumentNotNull(nameof(expectedSuperset), expectedSuperset);

			if (actual == null || !actual.IsProperSubsetOf(expectedSuperset))
				throw new ProperSubsetException(expectedSuperset, actual);
		}

		/// <summary>
		/// Verifies that a set is a proper superset of another set.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="expectedSubset">The expected subset</param>
		/// <param name="actual">The set expected to be a proper superset</param>
		/// <exception cref="ContainsException">Thrown when the actual set is not a proper superset of the expected set</exception>
		public static void ProperSuperset<T>(
			ISet<T> expectedSubset,
#if XUNIT_NULLABLE
			ISet<T>? actual)
#else
			ISet<T> actual)
#endif
		{
			GuardArgumentNotNull(nameof(expectedSubset), expectedSubset);

			if (actual == null || !actual.IsProperSupersetOf(expectedSubset))
				throw new ProperSupersetException(expectedSubset, actual);
		}

		/// <summary>
		/// Verifies that a set is a subset of another set.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="expectedSuperset">The expected superset</param>
		/// <param name="actual">The set expected to be a subset</param>
		/// <exception cref="ContainsException">Thrown when the actual set is not a subset of the expected set</exception>
		public static void Subset<T>(
			ISet<T> expectedSuperset,
#if XUNIT_NULLABLE
			ISet<T>? actual)
#else
			ISet<T> actual)
#endif
		{
			GuardArgumentNotNull(nameof(expectedSuperset), expectedSuperset);

			if (actual == null || !actual.IsSubsetOf(expectedSuperset))
				throw new SubsetException(expectedSuperset, actual);
		}

		/// <summary>
		/// Verifies that a set is a superset of another set.
		/// </summary>
		/// <typeparam name="T">The type of the object to be verified</typeparam>
		/// <param name="expectedSubset">The expected subset</param>
		/// <param name="actual">The set expected to be a superset</param>
		/// <exception cref="ContainsException">Thrown when the actual set is not a superset of the expected set</exception>
		public static void Superset<T>(
			ISet<T> expectedSubset,
#if XUNIT_NULLABLE
			ISet<T>? actual)
#else
			ISet<T> actual)
#endif
		{
			GuardArgumentNotNull(nameof(expectedSubset), expectedSubset);

			if (actual == null || !actual.IsSupersetOf(expectedSubset))
				throw new SupersetException(expectedSubset, actual);
		}
	}
}
