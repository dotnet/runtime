// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // https://msdn.microsoft.com/en-us/library/windows/hardware/ff557749.aspx
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct OBJECT_ATTRIBUTES
        {
            internal uint Length;
            internal IntPtr RootDirectory;
            internal UNICODE_STRING* ObjectName;
            internal uint Attributes;
            internal IntPtr SecurityDescriptor;
            internal IntPtr SecurityQualityOfService;

            internal unsafe OBJECT_ATTRIBUTES(UNICODE_STRING* objectName, uint attributes)
            {
                Length = (uint)sizeof(OBJECT_ATTRIBUTES);
                RootDirectory = IntPtr.Zero;
                ObjectName = objectName;
                Attributes = attributes;
                SecurityDescriptor = IntPtr.Zero;
                SecurityQualityOfService = IntPtr.Zero;
            }
        }
    }
}
