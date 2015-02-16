// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Security;
using System.Diagnostics.Contracts;

namespace System.Globalization
{
    internal partial class FormatProvider
    {
        private partial class Number
        {
            [StructLayout(LayoutKind.Sequential)]
            internal unsafe struct NumberBuffer
            {
                public Int32 precision;
                public Int32 scale;
                public Boolean sign;

                public char* digits
                {
                    [SecurityCritical]
                    get
                    {
                        return overrideDigits;
                    }
                }

                [SecurityCritical]
                public char* overrideDigits; // used for BigNumber support which can't be limited to 32 characters.
            }
        }
    }
}

