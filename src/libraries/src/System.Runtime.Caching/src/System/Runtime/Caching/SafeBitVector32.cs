// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.                                                         

using System;
using System.Threading;

namespace System.Runtime.Caching
{
    //
    // This is a multithreadsafe version of System.Collections.Specialized.BitVector32.
    //
    [Serializable]
    internal struct SafeBitVector32
    {
        private volatile int _data;

        internal bool this[int bit]
        {
            get
            {
                int data = _data;
                return (data & bit) == bit;
            }
            set
            {
                for (; ;)
                {
                    int oldData = _data;
                    int newData;
                    if (value)
                    {
                        newData = oldData | bit;
                    }
                    else
                    {
                        newData = oldData & ~bit;
                    }

                    int result = Interlocked.CompareExchange(ref _data, newData, oldData);
                    if (result == oldData)
                    {
                        break;
                    }
                }
            }
        }

        internal bool ChangeValue(int bit, bool value)
        {
            for (; ;)
            {
                int oldData = _data;
                int newData;
                if (value)
                {
                    newData = oldData | bit;
                }
                else
                {
                    newData = oldData & ~bit;
                }

                if (oldData == newData)
                {
                    return false;
                }

                int result = Interlocked.CompareExchange(ref _data, newData, oldData);
                if (result == oldData)
                {
                    return true;
                }
            }
        }
    }
}

