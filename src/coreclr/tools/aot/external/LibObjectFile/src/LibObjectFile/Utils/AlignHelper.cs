// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace LibObjectFile.Utils
{
    /// <summary>
    /// Helper class to perform alignment.
    /// </summary>
    public static class AlignHelper
    {
        /// <summary>
        /// Returns <c>true</c> if alignment is a power of 2.
        /// </summary>
        /// <param name="align">The alignment</param>
        /// <returns><c>true</c> if it is a power of 2.</returns>
        public static bool IsPowerOfTwo(ulong align)
        {
            return (align & (align - 1)) == 0;
        }

        /// <summary>
        /// Aligns a value to the required alignment.
        /// </summary>
        /// <param name="value">The value to align.</param>
        /// <param name="align">The alignment.</param>
        /// <returns>The value aligned or unchanged it is was already aligned.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong AlignToUpper(ulong value, ulong align)
        {
            if (align == 0) throw new ArgumentOutOfRangeException(nameof(align), "Alignment must be > 0");
            if (!IsPowerOfTwo(align)) throw new ArgumentOutOfRangeException(nameof(align), "Alignment must be a power of 2");

            var nextValue = ((value + align - 1) / align) * align;
            return nextValue;
        }
    }
}