// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Drawing
{
    internal static partial class SafeNativeMethods
    {
        internal static unsafe partial class Gdip
        {
            // Passing null SafeHandles in P/Invokes is not supported so we will do it the manual way.
            // TODO: Revisit this file when custom marshallers are available.
            internal static int GdipGetPathWorldBounds(HandleRef path, out RectangleF gprectf, SafeMatrixHandle? matrixOptional, SafePenHandle? penOptional)
            {
                bool releaseMatrix = false;
                bool releasePen = false;
                try
                {
                    IntPtr nativeMatrix = IntPtr.Zero;
                    if (matrixOptional != null)
                    {
                        matrixOptional.DangerousAddRef(ref releaseMatrix);
                        nativeMatrix = matrixOptional.DangerousGetHandle();
                    }

                    IntPtr nativePen = IntPtr.Zero;
                    if (penOptional != null)
                    {
                        penOptional.DangerousAddRef(ref releasePen);
                        nativePen = penOptional.DangerousGetHandle();
                    }

                    return __PInvoke__(path, out gprectf, nativeMatrix, nativePen);
                }
                finally
                {
                    if (releaseMatrix)
                    {
                        matrixOptional!.DangerousRelease();
                    }

                    if (releasePen)
                    {
                        penOptional!.DangerousRelease();
                    }
                }

                [DllImport(LibraryName, EntryPoint = nameof(GdipGetPathWorldBounds))]
                static extern int __PInvoke__(HandleRef path, out RectangleF gprectf, IntPtr nativeMatrix, IntPtr penOptional);
            }

            internal static int GdipFlattenPath(HandleRef path, SafeMatrixHandle? matrixOptional, float flatness)
            {
                bool releaseMatrix = false;
                try
                {
                    IntPtr nativeMatrix = IntPtr.Zero;
                    if (matrixOptional != null)
                    {
                        matrixOptional.DangerousAddRef(ref releaseMatrix);
                        nativeMatrix = matrixOptional.DangerousGetHandle();
                    }

                    return __PInvoke__(path, nativeMatrix, flatness);
                }
                finally
                {
                    if (releaseMatrix)
                    {
                        matrixOptional!.DangerousRelease();
                    }
                }

                [DllImport(LibraryName, EntryPoint = nameof(GdipFlattenPath))]
                static extern int __PInvoke__(HandleRef path, IntPtr nativeMatrix, float flatness);
            }

            internal static int GdipWidenPath(HandleRef path, SafePenHandle pen, SafeMatrixHandle? matrixOptional, float flatness)
            {
                bool releaseMatrix = false;
                try
                {
                    IntPtr nativeMatrix = IntPtr.Zero;
                    if (matrixOptional != null)
                    {
                        matrixOptional.DangerousAddRef(ref releaseMatrix);
                        nativeMatrix = matrixOptional.DangerousGetHandle();
                    }

                    return __PInvoke__(path, pen, nativeMatrix, flatness);
                }
                finally
                {
                    if (releaseMatrix)
                    {
                        matrixOptional!.DangerousRelease();
                    }
                }

                [DllImport(LibraryName, EntryPoint = nameof(GdipWidenPath))]
                static extern int __PInvoke__(HandleRef path, SafePenHandle pen, IntPtr nativeMatrix, float flatness);
            }

            internal static int GdipWarpPath(HandleRef path, SafeMatrixHandle? matrixOptional, PointF[] points, int count, float srcX, float srcY, float srcWidth, float srcHeight, WarpMode warpMode, float flatness)
            {
                bool releaseMatrix = false;
                try
                {
                    IntPtr nativeMatrix = IntPtr.Zero;
                    if (matrixOptional != null)
                    {
                        matrixOptional.DangerousAddRef(ref releaseMatrix);
                        nativeMatrix = matrixOptional.DangerousGetHandle();
                    }

                    return __PInvoke__(path, nativeMatrix, points, count, srcX, srcY, srcWidth, srcHeight, warpMode, flatness);
                }
                finally
                {
                    if (releaseMatrix)
                    {
                        matrixOptional!.DangerousRelease();
                    }
                }

                [DllImport(LibraryName, EntryPoint = nameof(GdipWarpPath))]
                static extern int __PInvoke__(HandleRef path, IntPtr nativeMatrix, PointF[] points, int count, float srcX, float srcY, float srcWidth, float srcHeight, WarpMode warpMode, float flatness);
            }

        }
    }
}
