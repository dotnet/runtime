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
		/// Indicates that the test should immediately fail.
		/// </summary>
		/// <param name="message">The failure message</param>
#if XUNIT_NULLABLE
		[DoesNotReturn]
#endif
		public static void Fail(string message)
		{
			GuardArgumentNotNull(nameof(message), message);

			throw new FailException(message);
		}
	}
}
