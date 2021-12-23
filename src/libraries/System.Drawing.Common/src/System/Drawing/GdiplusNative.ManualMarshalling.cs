// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Drawing
{
    internal static partial class SafeNativeMethods
    {
        internal static unsafe partial class Gdip
        {
            [DllImport(LibraryName, EntryPoint = nameof(GdipGetPathWorldBounds))]
            private static extern int GdipGetPathWorldBounds_impl(HandleRef path, out RectangleF gprectf, HandleRef matrix, IntPtr penOptional);

            // Passing null SafeHandles in P/Invokes is not supported so we will do it the manual way.
            internal static int GdipGetPathWorldBounds(HandleRef path, out RectangleF gprectf, HandleRef matrix, SafePenHandle? penOptional)
            {
                bool releasePen = false;
                try
                {
                    IntPtr nativePen = IntPtr.Zero;
                    if (penOptional != null)
                    {
                        penOptional.DangerousAddRef(ref releasePen);
                        nativePen = penOptional.DangerousGetHandle();
                    }

                    return GdipGetPathWorldBounds_impl(path, out gprectf, matrix, nativePen);
                }
                finally
                {
                    if (releasePen)
                    {
                        penOptional!.DangerousRelease();
                    }
                }
            }
        }
    }
}
