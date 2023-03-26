#if XUNIT_NULLABLE
#nullable enable
#endif

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
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
		/// Runs multiple checks, collecting the exceptions from each one, and then bundles all failures
		/// up into a single assertion failure.
		/// </summary>
		/// <param name="checks">The individual assertions to run, as actions.</param>
		public static void Multiple(params Action[] checks)
		{
			if (checks == null || checks.Length == 0)
				return;

			var exceptions = new List<Exception>();

			foreach (var check in checks)
				try
				{
					check();
				}
				catch (Exception ex)
				{
					exceptions.Add(ex);
				}

			if (exceptions.Count == 0)
				return;
			if (exceptions.Count == 1)
				ExceptionDispatchInfo.Capture(exceptions[0]).Throw();

			throw new MultipleException(exceptions);
		}
	}
}
