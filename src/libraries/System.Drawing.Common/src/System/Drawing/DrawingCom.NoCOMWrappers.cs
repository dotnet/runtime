// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Internal;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal static partial class DrawingCom
    {
        internal static IStreamWrapper GetComWrapper(GPStream stream)
        {
            return new IStreamWrapper(Marshal.GetComInterfaceForObject<GPStream, Interop.Ole32.IStream>(stream));
        }
    }
}
