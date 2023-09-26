// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("E4461914-4202-479F-8427-620E915F84B9")]
    internal partial interface IPointProvider
    {
        [PreserveSig]
        Point GetPoint();

        [PreserveSig]
        [return:MarshalAs(UnmanagedType.Error)]
        HResult SetPoint(Point point);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HResult
    {
        public int Value;
    }
}
