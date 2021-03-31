// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security;

namespace System.Globalization
{
    internal static partial class FormatProvider
    {
        private static partial class Number
        {
            [StructLayout(LayoutKind.Sequential)]
            internal unsafe struct NumberBuffer
            {
                public int precision;
                public int scale;
                public bool sign;

                public char* digits
                {
                    get
                    {
                        return overrideDigits;
                    }
                }

                public char* overrideDigits; // Used for BigNumber support which can't be limited to 32 characters.
            }
        }
    }
}
