// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Text
{
    internal ref partial struct ValueStringBuilder
    {
        /// <summary>
        /// Ensures that the builder is terminated with a NUL character.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureTerminated()
        {
            EnsureCapacity(_pos + 1);
            _chars[_pos] = '\0';
        }
    }
}
