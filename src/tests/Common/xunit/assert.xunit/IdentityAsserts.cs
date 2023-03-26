#if XUNIT_NULLABLE
#nullable enable
#endif

using Xunit.Sdk;

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
		/// Verifies that two objects are not the same instance.
		/// </summary>
		/// <param name="expected">The expected object instance</param>
		/// <param name="actual">The actual object instance</param>
		/// <exception cref="NotSameException">Thrown when the objects are the same instance</exception>
		public static void NotSame(
#if XUNIT_NULLABLE
			object? expected,
			object? actual)
#else
			object expected,
			object actual)
#endif
		{
			if (object.ReferenceEquals(expected, actual))
				throw new NotSameException();
		}

		/// <summary>
		/// Verifies that two objects are the same instance.
		/// </summary>
		/// <param name="expected">The expected object instance</param>
		/// <param name="actual">The actual object instance</param>
		/// <exception cref="SameException">Thrown when the objects are not the same instance</exception>
		public static void Same(
#if XUNIT_NULLABLE
			object? expected,
			object? actual)
#else
			object expected,
			object actual)
#endif
		{
			if (!object.ReferenceEquals(expected, actual))
				throw new SameException(expected, actual);
		}
	}
}
