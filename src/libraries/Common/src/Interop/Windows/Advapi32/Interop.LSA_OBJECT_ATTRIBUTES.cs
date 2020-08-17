// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal sealed class LSA_OBJECT_ATTRIBUTES
        {
            internal int Length;
            private readonly IntPtr _rootDirectory;
            private readonly IntPtr _objectName;
            internal int Attributes;
            private readonly IntPtr _securityDescriptor;
            private readonly IntPtr _securityQualityOfService;

            public LSA_OBJECT_ATTRIBUTES()
            {
                Length = 0;
                _rootDirectory = (IntPtr)0;
                _objectName = (IntPtr)0;
                Attributes = 0;
                _securityDescriptor = (IntPtr)0;
                _securityQualityOfService = (IntPtr)0;
            }
        }
    }
}
