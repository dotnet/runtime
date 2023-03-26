#if XUNIT_NULLABLE
#nullable enable
#endif

using System;
using System.Collections;
using System.Collections.Generic;

namespace Xunit.Sdk
{
	/// <summary>
	/// A class that wraps <see cref="IEqualityComparer{T}"/> to create <see cref="IEqualityComparer"/>.
	/// </summary>
	/// <typeparam name="T">The type that is being compared.</typeparam>
	class AssertEqualityComparerAdapter<T> : IEqualityComparer
	{
		readonly IEqualityComparer<T> innerComparer;

		/// <summary>
		/// Initializes a new instance of the <see cref="AssertEqualityComparerAdapter{T}"/> class.
		/// </summary>
		/// <param name="innerComparer">The comparer that is being adapted.</param>
		public AssertEqualityComparerAdapter(IEqualityComparer<T> innerComparer)
		{
			if (innerComparer == null)
				throw new ArgumentNullException(nameof(innerComparer));

			this.innerComparer = innerComparer;
		}

		/// <inheritdoc/>
		public new bool Equals(
#if XUNIT_NULLABLE
			object? x,
			object? y) =>
				innerComparer.Equals((T?)x, (T?)y);
#else
			object x,
			object y) =>
				innerComparer.Equals((T)x, (T)y);
#endif

		/// <inheritdoc/>
		public int GetHashCode(object obj)
		{
			throw new NotImplementedException();
		}
	}
}
