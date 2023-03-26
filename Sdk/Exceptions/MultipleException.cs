#if XUNIT_NULLABLE
#nullable enable
#endif

using System;
using System.Collections.Generic;
using System.Linq;

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when multiple assertions failed via <see cref="Assert.Multiple"/>.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class MultipleException : XunitException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MultipleException"/> class.
		/// </summary>
		public MultipleException(IEnumerable<Exception> innerExceptions) :
			base("Multiple failures were encountered:")
		{
			if (innerExceptions == null)
				throw new ArgumentNullException(nameof(innerExceptions));

			InnerExceptions = innerExceptions.ToList();
		}

		/// <summary>
		/// Gets the list of inner exceptions that were thrown.
		/// </summary>
		public IReadOnlyCollection<Exception> InnerExceptions { get; }

		/// <inheritdoc/>
#if XUNIT_NULLABLE
		public override string? StackTrace =>
#else
		public override string StackTrace =>
#endif
			"Inner stack traces:";
	}
}
