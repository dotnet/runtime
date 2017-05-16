// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Configuration
{
    // This is a multithreadsafe version of System.Collections.Specialized.BitVector32.
    internal struct SafeBitVector32
    {
        private volatile int _data;

        internal SafeBitVector32(int data)
        {
            _data = data;
        }

        internal bool this[int bit]
        {
            get
            {
                int data = _data;
                return (data & bit) == bit;
            }
            set
            {
                for (;;)
                {
                    int oldData = _data;
                    int newData;
                    if (value) newData = oldData | bit;
                    else newData = oldData & ~bit;

#pragma warning disable 0420
                    int result = Interlocked.CompareExchange(ref _data, newData, oldData);
#pragma warning restore 0420

                    if (result == oldData) break;
                }
            }
        }
    }
}