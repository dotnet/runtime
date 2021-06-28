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
            internal static extern int GdipLoadImageFromStream(IntPtr stream, IntPtr* image);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipLoadImageFromStreamICM(IntPtr stream, IntPtr* image);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSaveImageToStream(HandleRef image, IntPtr stream, Guid* classId, HandleRef encoderParams);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetMetafileHeaderFromStream(IntPtr stream, IntPtr header);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateMetafileFromStream(IntPtr stream, IntPtr* metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileStream(IntPtr stream, IntPtr referenceHdc, EmfType emfType, RectangleF* frameRect, MetafileFrameUnit frameUnit, string? description, IntPtr* metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileStream(IntPtr stream, IntPtr referenceHdc, EmfType emfType, IntPtr pframeRect, MetafileFrameUnit frameUnit, string? description, IntPtr* metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileStreamI(IntPtr stream, IntPtr referenceHdc, EmfType emfType, Rectangle* frameRect, MetafileFrameUnit frameUnit, string? description, IntPtr* metafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateBitmapFromStream(IntPtr stream, IntPtr* bitmap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateBitmapFromStreamICM(IntPtr stream, IntPtr* bitmap);
        }
    }
}
