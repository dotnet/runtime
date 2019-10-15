// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    // This is a cut down copy of System.Collections.Specialized.BitVector32. The
    // reason this is here is because it is used rather intensively by Control and
    // WebControl. As a result, being able to inline this operations results in a
    // measurable performance gain, at the expense of some maintainability.
    internal struct SimpleBitVector32
    {
        internal SimpleBitVector32(int data)
        {
            Data = data;
        }

        internal int Data { get; private set; }

        internal bool this[int bit]
        {
            get { return (Data & bit) == bit; }
            set
            {
                int data = Data;
                if (value) Data = data | bit;
                else Data = data & ~bit;
            }
        }
    }
}