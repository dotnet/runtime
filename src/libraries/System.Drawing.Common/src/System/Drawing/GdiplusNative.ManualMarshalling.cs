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
            internal static int GdipGetPathWorldBounds(SafeGraphicsPathHandle path, out RectangleF gprectf, SafeMatrixHandle? matrix, SafePenHandle? pen)
            {
                bool releaseMatrix = false;
                bool releasePen = false;
                try
                {
                    IntPtr nativeMatrix = IntPtr.Zero;
                    if (matrix != null)
                    {
                        matrix.DangerousAddRef(ref releaseMatrix);
                        nativeMatrix = matrix.DangerousGetHandle();
                    }

                    IntPtr nativePen = IntPtr.Zero;
                    if (pen != null)
                    {
                        pen.DangerousAddRef(ref releasePen);
                        nativePen = pen.DangerousGetHandle();
                    }

                    return __PInvoke__(path, out gprectf, nativeMatrix, nativePen);
                }
                finally
                {
                    if (releaseMatrix)
                    {
                        matrix!.DangerousRelease();
                    }

                    if (releasePen)
                    {
                        pen!.DangerousRelease();
                    }
                }

                [DllImport(LibraryName, EntryPoint = nameof(GdipGetPathWorldBounds))]
                static extern int __PInvoke__(SafeGraphicsPathHandle path, out RectangleF gprectf, IntPtr matrix, IntPtr pen);
            }

            internal static int GdipFlattenPath(SafeGraphicsPathHandle path, SafeMatrixHandle? matrix, float flatness)
            {
                bool releaseMatrix = false;
                try
                {
                    IntPtr nativeMatrix = IntPtr.Zero;
                    if (matrix != null)
                    {
                        matrix.DangerousAddRef(ref releaseMatrix);
                        nativeMatrix = matrix.DangerousGetHandle();
                    }

                    return __PInvoke__(path, nativeMatrix, flatness);
                }
                finally
                {
                    if (releaseMatrix)
                    {
                        matrix!.DangerousRelease();
                    }
                }

                [DllImport(LibraryName, EntryPoint = nameof(GdipFlattenPath))]
                static extern int __PInvoke__(SafeGraphicsPathHandle path, IntPtr matrix, float flatness);
            }

            internal static int GdipWidenPath(SafeGraphicsPathHandle path, SafePenHandle pen, SafeMatrixHandle? matrix, float flatness)
            {
                bool releaseMatrix = false;
                try
                {
                    IntPtr nativeMatrix = IntPtr.Zero;
                    if (matrix != null)
                    {
                        matrix.DangerousAddRef(ref releaseMatrix);
                        nativeMatrix = matrix.DangerousGetHandle();
                    }

                    return __PInvoke__(path, pen, nativeMatrix, flatness);
                }
                finally
                {
                    if (releaseMatrix)
                    {
                        matrix!.DangerousRelease();
                    }
                }

                [DllImport(LibraryName, EntryPoint = nameof(GdipWidenPath))]
                static extern int __PInvoke__(SafeGraphicsPathHandle path, SafePenHandle pen, IntPtr matrix, float flatness);
            }

            internal static int GdipWarpPath(SafeGraphicsPathHandle path, SafeMatrixHandle? matrix, PointF[] points, int count, float srcX, float srcY, float srcWidth, float srcHeight, WarpMode warpMode, float flatness)
            {
                bool releaseMatrix = false;
                try
                {
                    IntPtr nativeMatrix = IntPtr.Zero;
                    if (matrix != null)
                    {
                        matrix.DangerousAddRef(ref releaseMatrix);
                        nativeMatrix = matrix.DangerousGetHandle();
                    }

                    return __PInvoke__(path, nativeMatrix, points, count, srcX, srcY, srcWidth, srcHeight, warpMode, flatness);
                }
                finally
                {
                    if (releaseMatrix)
                    {
                        matrix!.DangerousRelease();
                    }
                }

                [DllImport(LibraryName, EntryPoint = nameof(GdipWarpPath))]
                static extern int __PInvoke__(SafeGraphicsPathHandle path, IntPtr matrix, PointF[] points, int count, float srcX, float srcY, float srcWidth, float srcHeight, WarpMode warpMode, float flatness);
            }

            internal static int GdipCreateCustomLineCap(SafeGraphicsPathHandle? fillPath, SafeGraphicsPathHandle? strokePath, LineCap baseCap, float baseInset, out IntPtr customCap)
            {
                bool releaseFillPath = false;
                bool releaseStrokePath = false;
                try
                {
                    IntPtr nativeFillPath = IntPtr.Zero;
                    if (fillPath != null)
                    {
                        fillPath.DangerousAddRef(ref releaseFillPath);
                        nativeFillPath = fillPath.DangerousGetHandle();
                    }

                    IntPtr nativeStrokePath = IntPtr.Zero;
                    if (strokePath != null)
                    {
                        strokePath.DangerousAddRef(ref releaseStrokePath);
                        nativeStrokePath = strokePath.DangerousGetHandle();
                    }

                    return __PInvoke__(nativeFillPath, nativeStrokePath, baseCap, baseInset, out customCap);
                }
                finally
                {
                    if (releaseFillPath)
                    {
                        fillPath!.DangerousRelease();
                    }

                    if (releaseStrokePath)
                    {
                        strokePath!.DangerousRelease();
                    }
                }

                [DllImport(LibraryName, EntryPoint = nameof(GdipCreateCustomLineCap))]
                static extern int __PInvoke__(IntPtr fillpath, IntPtr strokepath, LineCap baseCap, float baseInset, out IntPtr customCap);
            }

            internal static int GdipCreatePathIter(out IntPtr pathIter, SafeGraphicsPathHandle? path)
            {
                bool releasePath = false;
                try
                {
                    IntPtr nativePath = IntPtr.Zero;
                    if (path != null)
                    {
                        path.DangerousAddRef(ref releasePath);
                        nativePath = path.DangerousGetHandle();
                    }

                    return __PInvoke__(out pathIter, nativePath);
                }
                finally
                {
                    if (releasePath)
                    {
                        path!.DangerousRelease();
                    }
                }

                [DllImport(LibraryName, EntryPoint = nameof(GdipCreatePathIter))]
                static extern int __PInvoke__(out IntPtr pathIter, IntPtr path);
            }

            internal static int GdipPathIterNextSubpathPath(HandleRef pathIter, out int resultCount, SafeGraphicsPathHandle? path, out bool isClosed)
            {
                bool releasePath = false;
                try
                {
                    IntPtr nativePath = IntPtr.Zero;
                    if (path != null)
                    {
                        path.DangerousAddRef(ref releasePath);
                        nativePath = path.DangerousGetHandle();
                    }

                    return __PInvoke__(pathIter, out resultCount, nativePath, out isClosed);
                }
                finally
                {
                    if (releasePath)
                    {
                        path!.DangerousRelease();
                    }
                }

                [DllImport(LibraryName, EntryPoint = nameof(GdipPathIterNextSubpathPath))]
                static extern int __PInvoke__(HandleRef pathIter, out int resultCount, IntPtr path, out bool isClosed);
            }

            internal static int GdipPathIterNextMarkerPath(HandleRef pathIter, out int resultCount, SafeGraphicsPathHandle? path)
            {
                bool releasePath = false;
                try
                {
                    IntPtr nativePath = IntPtr.Zero;
                    if (path != null)
                    {
                        path.DangerousAddRef(ref releasePath);
                        nativePath = path.DangerousGetHandle();
                    }

                    return __PInvoke__(pathIter, out resultCount, nativePath);
                }
                finally
                {
                    if (releasePath)
                    {
                        path!.DangerousRelease();
                    }
                }

                [DllImport(LibraryName, EntryPoint = nameof(GdipPathIterNextMarkerPath))]
                static extern int __PInvoke__(HandleRef pathIter, out int resultCount, IntPtr path);
            }
        }
    }
}
