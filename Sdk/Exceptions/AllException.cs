#if XUNIT_NULLABLE
#nullable enable
#endif

using System;
using System.Collections.Generic;
using System.Linq;

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when an All assertion has one or more items fail an assertion.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class AllException : XunitException
	{
#if XUNIT_NULLABLE
		readonly IReadOnlyList<Tuple<int, object?, Exception>> errors;
#else
		readonly IReadOnlyList<Tuple<int, object, Exception>> errors;
#endif
		readonly int totalItems;

		/// <summary>
		/// Creates a new instance of the <see cref="AllException"/> class.
		/// </summary>
		/// <param name="totalItems">The total number of items that were in the collection.</param>
		/// <param name="errors">The list of errors that occurred during the test pass.</param>
		public AllException(
#if XUNIT_NULLABLE
			int totalItems,
			Tuple<int, object?, Exception>[] errors) :
#else
			int totalItems,
			Tuple<int, object, Exception>[] errors) :
#endif
				base("Assert.All() Failure")
		{
			this.errors = errors;
			this.totalItems = totalItems;
		}

		/// <summary>
		/// The errors that occurred during execution of the test.
		/// </summary>
		public IReadOnlyList<Exception> Failures =>
			errors.Select(t => t.Item3).ToList();

		/// <inheritdoc/>
		public override string Message
		{
			get
			{
				var formattedErrors = errors.Select(error =>
				{
					var indexString = $"[{error.Item1}]: ";
					var spaces = Environment.NewLine + "".PadRight(indexString.Length);

					return $"{indexString}Item: {error.Item2?.ToString()?.Replace(Environment.NewLine, spaces)}{spaces}{error.Item3.ToString().Replace(Environment.NewLine, spaces)}";
				});

				return $"{base.Message}: {errors.Count} out of {totalItems} items in the collection did not pass.{Environment.NewLine}{string.Join(Environment.NewLine, formattedErrors)}";
			}
		}
	}
}
