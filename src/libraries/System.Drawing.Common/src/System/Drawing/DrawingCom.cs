// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal sealed partial class DrawingCom
    {
        internal readonly struct IStreamWrapper : IDisposable
        {
            public readonly IntPtr Ptr;

            public IStreamWrapper(IntPtr ptr)
            {
                Ptr = ptr;
            }

            public void Dispose()
            {
                Marshal.Release(Ptr);
            }
        }
    }
}
