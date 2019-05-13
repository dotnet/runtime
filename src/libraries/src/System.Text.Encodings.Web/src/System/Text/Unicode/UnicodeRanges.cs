// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.Unicode
{
    /// <summary>
    /// Contains predefined <see cref="UnicodeRange"/> instances which correspond to blocks
    /// from the Unicode 7.0 specification.
    /// </summary>
    public static partial class UnicodeRanges
    {
        /// <summary>
        /// An empty <see cref="UnicodeRange"/>. This range contains no code points.
        /// </summary>
        public static UnicodeRange None => _none ?? CreateEmptyRange(ref _none);
        private static UnicodeRange _none;

        /// <summary>
        /// A <see cref="UnicodeRange"/> which contains all characters in the Unicode Basic
        /// Multilingual Plane (U+0000..U+FFFF).
        /// </summary>
        public static UnicodeRange All => _all ?? CreateRange(ref _all, '\u0000', '\uFFFF');
        private static UnicodeRange _all;

        [MethodImpl(MethodImplOptions.NoInlining)] // the caller should be inlined, not this method
        private static UnicodeRange CreateEmptyRange(ref UnicodeRange range)
        {
            // If the range hasn't been created, create it now.
            // It's ok if two threads race and one overwrites the other's 'range' value.
            Volatile.Write(ref range, new UnicodeRange(0, 0));
            return range;
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // the caller should be inlined, not this method
        private static UnicodeRange CreateRange(ref UnicodeRange range, char first, char last)
        {
            // If the range hasn't been created, create it now.
            // It's ok if two threads race and one overwrites the other's 'range' value.
            Volatile.Write(ref range, UnicodeRange.Create(first, last));
            return range;
        }
    }
}
