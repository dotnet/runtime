#if XUNIT_NULLABLE
#nullable enable
#endif

using System;

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
		/// <summary/>
		internal static void GuardArgumentNotNull(
			string argName,
#if XUNIT_NULLABLE
			[NotNull] object? argValue)
#else
			object argValue)
#endif
		{
			if (argValue == null)
				throw new ArgumentNullException(argName);
		}
	}
}
