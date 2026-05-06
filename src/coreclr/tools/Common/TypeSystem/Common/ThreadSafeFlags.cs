// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Interlocked = System.Threading.Interlocked;

namespace Internal.TypeSystem
{
    public struct ThreadSafeFlags
    {
        private volatile int _value;

        public int Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasFlags(int value)
        {
            return (_value & value) == value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFlags(int flagsToAdd)
        {
            var originalFlags = _value;
            while (Interlocked.CompareExchange(ref _value, originalFlags | flagsToAdd, originalFlags) != originalFlags)
            {
                originalFlags = _value;
            }
        }
    }
}
