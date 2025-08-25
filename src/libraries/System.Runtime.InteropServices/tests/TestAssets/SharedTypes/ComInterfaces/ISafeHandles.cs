// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Win32.SafeHandles;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper), Guid("0A52B77C-E08B-4274-A1F4-1A2BF2C07E60")]
    internal partial interface ISafeFileHandle
    {
        void Method(SafeFileHandle p);
        void MethodIn(in SafeFileHandle p);
        void MethodOut(out SafeFileHandle p);
        void MethodRef(ref SafeFileHandle p);
    }
}
