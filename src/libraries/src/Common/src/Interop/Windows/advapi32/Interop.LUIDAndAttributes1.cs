// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        internal unsafe partial struct InlineArray_LUID_AND_ATTRIBUTES_1
        {
            public LUID_AND_ATTRIBUTES this[uint index]
            {
                get
                {
                    if (index < 0
                                || index >= 1)
                        throw new global::System.IndexOutOfRangeException();
                    fixed (InlineArray_LUID_AND_ATTRIBUTES_1* pThis = &(this))
                        return ((LUID_AND_ATTRIBUTES*)pThis)[index];
                }
                set
                {
                    if (index < 0
                                || index >= 1)
                        throw new global::System.IndexOutOfRangeException();
                    fixed (InlineArray_LUID_AND_ATTRIBUTES_1* pThis = &(this))
                        ((LUID_AND_ATTRIBUTES*)pThis)[index] = value;
                }
            }
            public const int Length = 1; private LUID_AND_ATTRIBUTES _elem_0;
        }
    }
}
