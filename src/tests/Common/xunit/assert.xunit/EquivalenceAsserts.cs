#if XUNIT_NULLABLE
#nullable enable
#endif

using Xunit.Internal;

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
		/// Verifies that two objects are equivalent, using a default comparer. This comparison is done
		/// without regard to type, and only inspects public property and field values for individual
		/// equality. Deep equivalence tests (meaning, property or fields which are themselves complex
		/// types) are supported. With strict mode off, object comparison allows <paramref name="actual"/>
		/// to have extra public members that aren't part of <paramref name="expected"/>, and collection
		/// comparison allows <paramref name="actual"/> to have more data in it than is present in
		/// <paramref name="expected"/>; with strict mode on, those rules are tightened to require exact
		/// member list (for objects) or data (for collections).
		/// </summary>
		/// <param name="expected">The expected value</param>
		/// <param name="actual">The actual value</param>
		/// <param name="strict">A flag which enables strict comparison mode</param>
		public static void Equivalent(
#if XUNIT_NULLABLE
			object? expected,
			object? actual,
#else
			object expected,
			object actual,
#endif
			bool strict = false)
		{
			var ex = AssertHelper.VerifyEquivalence(expected, actual, strict);
			if (ex != null)
				throw ex;
		}
	}
}
