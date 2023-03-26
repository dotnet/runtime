#if XUNIT_SKIP

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
		/// Skips the current test. Used when determining whether a test should be skipped
		/// happens at runtime rather than at discovery time.
		/// </summary>
		/// <param name="reason">The message to indicate why the test was skipped</param>
#if XUNIT_NULLABLE
		[DoesNotReturn]
#endif
		public static void Skip(string reason)
		{
			GuardArgumentNotNull(nameof(reason), reason);

			throw new SkipException(reason);
		}

		/// <summary>
		/// Will skip the current test unless <paramref name="condition"/> evaluates to <c>true</c>.
		/// </summary>
		/// <param name="condition">When <c>true</c>, the test will continue to run; when <c>false</c>,
		/// the test will be skipped</param>
		/// <param name="reason">The message to indicate why the test was skipped</param>
		public static void SkipUnless(
#if XUNIT_NULLABLE
			[DoesNotReturnIf(false)] bool condition,
#else
			bool condition,
#endif
			string reason)
		{
			GuardArgumentNotNull(nameof(reason), reason);

			if (!condition)
				throw new SkipException(reason);
		}

		/// <summary>
		/// Will skip the current test when <paramref name="condition"/> evaluates to <c>true</c>.
		/// </summary>
		/// <param name="condition">When <c>true</c>, the test will be skipped; when <c>false</c>,
		/// the test will continue to run</param>
		/// <param name="reason">The message to indicate why the test was skipped</param>
		public static void SkipWhen(
#if XUNIT_NULLABLE
			[DoesNotReturnIf(true)] bool condition,
#else
			bool condition,
#endif
			string reason)
		{
			GuardArgumentNotNull(nameof(reason), reason);

			if (condition)
				throw new SkipException(reason);
		}
	}
}

#endif
