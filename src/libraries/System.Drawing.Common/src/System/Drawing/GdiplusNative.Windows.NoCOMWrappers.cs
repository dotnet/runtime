// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal static partial class SafeNativeMethods
    {
        internal static unsafe partial class Gdip
        {
            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipLoadImageFromStream(Interop.Ole32.IStream stream, out IntPtr image);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipLoadImageFromStreamICM(Interop.Ole32.IStream stream, out IntPtr image);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSaveImageToStream(HandleRef image, Interop.Ole32.IStream stream, ref Guid classId, HandleRef encoderParams);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetMetafileHeaderFromStream(Interop.Ole32.IStream stream, IntPtr header);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateMetafileFromStream(Interop.Ole32.IStream stream, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileStream(Interop.Ole32.IStream stream, IntPtr referenceHdc, EmfType emfType, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileStream(Interop.Ole32.IStream stream, IntPtr referenceHdc, EmfType emfType, IntPtr pframeRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileStreamI(Interop.Ole32.IStream stream, IntPtr referenceHdc, EmfType emfType, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateBitmapFromStream(Interop.Ole32.IStream stream, out IntPtr bitmap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateBitmapFromStreamICM(Interop.Ole32.IStream stream, out IntPtr bitmap);
        }
    }
}
