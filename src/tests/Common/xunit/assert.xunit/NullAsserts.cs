#if XUNIT_NULLABLE
#nullable enable
#endif

using Xunit.Sdk;

#if XUNIT_NULLABLE
using System.Diagnostics.CodeAnalysis;
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
		/// Verifies that an object reference is not null.
		/// </summary>
		/// <param name="object">The object to be validated</param>
		/// <exception cref="NotNullException">Thrown when the object reference is null</exception>
#if XUNIT_NULLABLE
		public static void NotNull([NotNull] object? @object)
#else
		public static void NotNull(object @object)
#endif
		{
			if (@object == null)
				throw new NotNullException();
		}

		/// <summary>
		/// Verifies that an object reference is null.
		/// </summary>
		/// <param name="object">The object to be inspected</param>
		/// <exception cref="NullException">Thrown when the object reference is not null</exception>
#if XUNIT_NULLABLE
		public static void Null([MaybeNull] object? @object)
#else
		public static void Null(object @object)
#endif
		{
			if (@object != null)
				throw new NullException(@object);
		}
	}
}
