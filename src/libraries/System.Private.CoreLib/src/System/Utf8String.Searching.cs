// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System
{
    public sealed partial class Utf8String
    {
        /// <summary>
        /// Attempts to locate the target <paramref name="value"/> within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public bool TryFind(char value, out Range range) => this.AsSpanSkipNullCheck().TryFind(value, out range);

        /// <summary>
        /// Attempts to locate the target <paramref name="value"/> within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public bool TryFind(char value, StringComparison comparisonType, out Range range) => this.AsSpanSkipNullCheck().TryFind(value, comparisonType, out range);

        /// <summary>
        /// Attempts to locate the target <paramref name="value"/> within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public bool TryFind(Rune value, out Range range) => this.AsSpanSkipNullCheck().TryFind(value, out range);

        /// <summary>
        /// Attempts to locate the target <paramref name="value"/> within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public bool TryFind(Rune value, StringComparison comparisonType, out Range range) => this.AsSpanSkipNullCheck().TryFind(value, comparisonType, out range);

        /// <summary>
        /// Attempts to locate the target <paramref name="value"/> within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public bool TryFind(Utf8String value, out Range range)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return this.AsSpanSkipNullCheck().TryFind(value, out range);
        }

        /// <summary>
        /// Attempts to locate the target <paramref name="value"/> within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public bool TryFind(Utf8String value, StringComparison comparisonType, out Range range)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return this.AsSpanSkipNullCheck().TryFind(value, comparisonType, out range);
        }

        /// <summary>
        /// Attempts to locate the last occurrence of the target <paramref name="value"/> within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public bool TryFindLast(char value, out Range range) => this.AsSpanSkipNullCheck().TryFindLast(value, out range);

        /// <summary>
        /// Attempts to locate the last occurrence of the target <paramref name="value"/> within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public bool TryFindLast(char value, StringComparison comparisonType, out Range range) => this.AsSpanSkipNullCheck().TryFindLast(value, comparisonType, out range);

        /// <summary>
        /// Attempts to locate the last occurrence of the target <paramref name="value"/> within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public bool TryFindLast(Rune value, out Range range) => this.AsSpanSkipNullCheck().TryFindLast(value, out range);

        /// <summary>
        /// Attempts to locate the last occurrence of the target <paramref name="value"/> within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public bool TryFindLast(Rune value, StringComparison comparisonType, out Range range) => this.AsSpanSkipNullCheck().TryFindLast(value, comparisonType, out range);

        /// <summary>
        /// Attempts to locate the last occurrence of the target <paramref name="value"/> within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public bool TryFindLast(Utf8String value, out Range range)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return this.AsSpanSkipNullCheck().TryFindLast(value, out range);
        }

        /// <summary>
        /// Attempts to locate the last occurrence of the target <paramref name="value"/> within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8String"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public bool TryFindLast(Utf8String value, StringComparison comparisonType, out Range range)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return this.AsSpanSkipNullCheck().TryFindLast(value, comparisonType, out range);
        }
    }
}
