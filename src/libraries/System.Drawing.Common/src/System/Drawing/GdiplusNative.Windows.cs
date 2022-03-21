// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.GeneratedMarshalling;
#endif

namespace System.Drawing
{
    internal static partial class SafeNativeMethods
    {
        internal static unsafe partial class Gdip
        {
            private const string LibraryName = "gdiplus.dll";

            private static void PlatformInitialize()
            {
            }

            // Imported functions
            [LibraryImport(LibraryName)]
            private static partial int GdiplusStartup(out IntPtr token, in StartupInputEx input, out StartupOutput output);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreatePath(int brushMode, out IntPtr path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreatePath2(PointF* points, byte* types, int count, int brushMode, out IntPtr path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreatePath2I(Point* points, byte* types, int count, int brushMode, out IntPtr path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipClonePath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, out IntPtr clonepath);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeletePath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipResetPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPointCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, out int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathTypes(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, byte[] types, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathPoints(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathFillMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, out FillMode fillmode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPathFillMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, FillMode fillmode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathData(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, GpPathData* pathData);

            [LibraryImport(LibraryName)]
            internal static partial int GdipStartPathFigure(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipClosePathFigure(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipClosePathFigures(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPathMarker(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipClearPathMarkers(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipReversePath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathLastPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, out PointF lastPoint);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathLine(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x1, float y1, float x2, float y2);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathLine2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathArc(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathBezier(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathBeziers(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathCurve(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathCurve2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count, float tension);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathCurve3(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count, int offset, int numberOfSegments, float tension);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathClosedCurve(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathClosedCurve2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count, float tension);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathRectangle(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x, float y, float width, float height);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathRectangles(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, RectangleF* rects, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathEllipse(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x, float y, float width, float height);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathPie(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathPolygon(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef addingPath, [MarshalAs(UnmanagedType.Bool)] bool connect);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipAddPathString(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, string s, int length,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontFamily, int style, float emSize, ref RectangleF layoutRect,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipAddPathStringI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, string s, int length,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontFamily, int style, float emSize, ref Rectangle layoutRect,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathLineI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x1, int y1, int x2, int y2);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathLine2I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathArcI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathBezierI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathBeziersI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathCurveI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathCurve2I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count, float tension);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathCurve3I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count, int offset, int numberOfSegments, float tension);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathClosedCurveI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathClosedCurve2I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count, float tension);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathRectangleI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x, int y, int width, int height);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathRectanglesI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Rectangle* rects, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathEllipseI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x, int y, int width, int height);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathPieI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathPolygonI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipFlattenPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrixfloat, float flatness);

            [LibraryImport(LibraryName)]
            internal static partial int GdipWidenPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, float flatness);

            [LibraryImport(LibraryName)]
            internal static partial int GdipWarpPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, PointF* points, int count, float srcX, float srcY, float srcWidth, float srcHeight, WarpMode warpMode, float flatness);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTransformPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathWorldBounds(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, out RectangleF gprectf,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsVisiblePathPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x, float y,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, [MarshalAs(UnmanagedType.Bool)] out bool result);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsVisiblePathPointI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x, int y,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, [MarshalAs(UnmanagedType.Bool)] out bool result);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsOutlineVisiblePathPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x, float y,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, [MarshalAs(UnmanagedType.Bool)] out bool result);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsOutlineVisiblePathPointI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x, int y,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, [MarshalAs(UnmanagedType.Bool)] out bool result);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeleteBrush(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush);

            [LibraryImport(LibraryName)]
            internal static partial int GdipLoadImageFromStream(IntPtr stream, IntPtr* image);

            [LibraryImport(LibraryName)]
            internal static partial int GdipLoadImageFromStreamICM(IntPtr stream, IntPtr* image);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCloneImage(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out IntPtr cloneimage);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipSaveImageToFile(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, string filename, ref Guid classId,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef encoderParams);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSaveImageToStream(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, IntPtr stream, Guid* classId,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef encoderParams);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSaveAdd(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef encoderParams);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSaveAddImage(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef newImage,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef encoderParams);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageGraphicsContext(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out IntPtr graphics);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageBounds(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out RectangleF gprectf, out GraphicsUnit unit);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageThumbnail(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int thumbWidth, int thumbHeight, out IntPtr thumbImage, Image.GetThumbnailImageAbort? callback, IntPtr callbackdata);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImagePalette(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, IntPtr palette, int size);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetImagePalette(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, IntPtr palette);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImagePaletteSize(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out int size);

            [LibraryImport(LibraryName)]
            internal static partial int GdipImageForceValidation(IntPtr image);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateFromHDC2(IntPtr hdc, IntPtr hdevice, out IntPtr graphics);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateFromHWND(IntPtr hwnd, out IntPtr graphics);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeleteGraphics(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics);

            [LibraryImport(LibraryName)]
            internal static partial int GdipReleaseDC(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, IntPtr hdc);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetNearestColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, ref int color);

            [LibraryImport(LibraryName)]
            internal static partial IntPtr GdipCreateHalftonePalette();

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawBeziers(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawBeziersI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileDestPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref PointF destPoint, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileDestPointI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref Point destPoint, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileDestRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref RectangleF destRect, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileDestRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref Rectangle destRect, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileDestPoints(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, PointF* destPoints, int count, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileDestPointsI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, Point* destPoints, int count, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileSrcRectDestPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref PointF destPoint, ref RectangleF srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileSrcRectDestPointI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref Point destPoint, ref Rectangle srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileSrcRectDestRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref RectangleF destRect, ref RectangleF srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileSrcRectDestRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref Rectangle destRect, ref Rectangle srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileSrcRectDestPoints(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, PointF* destPoints, int count, ref RectangleF srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileSrcRectDestPointsI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, Point* destPoints, int count, ref Rectangle srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipRestoreGraphics(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int state);

            [LibraryImport(LibraryName, EntryPoint = "GdipGetMetafileHeaderFromWmf")]
            private static partial int GdipGetMetafileHeaderFromWmf_Internal(IntPtr hMetafile,
#if NET7_0_OR_GREATER
                [MarshalUsing(typeof(WmfPlaceableFileHeader.PinningMarshaller))]
#endif
                WmfPlaceableFileHeader wmfplaceable,
#if NET7_0_OR_GREATER
                [MarshalUsing(typeof(MetafileHeaderWmf.InPlaceMarshaller))]
                ref MetafileHeaderWmf metafileHeaderWmf
#else
                MetafileHeaderWmf metafileHeaderWmf
#endif
                );

            internal static int GdipGetMetafileHeaderFromWmf(IntPtr hMetafile,
                WmfPlaceableFileHeader wmfplaceable,
                MetafileHeaderWmf metafileHeaderWmf
                )
            {
                return GdipGetMetafileHeaderFromWmf_Internal(hMetafile,
                    wmfplaceable,
#if NET7_0_OR_GREATER
                    ref
#endif
                    metafileHeaderWmf
                    );
            }

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetMetafileHeaderFromEmf(IntPtr hEnhMetafile, MetafileHeaderEmf metafileHeaderEmf);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipGetMetafileHeaderFromFile(string filename, IntPtr header);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetMetafileHeaderFromStream(IntPtr stream, IntPtr header);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetMetafileHeaderFromMetafile(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, IntPtr header);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetHemfFromMetafile(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, out IntPtr hEnhMetafile);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateMetafileFromStream(IntPtr stream, IntPtr* metafile);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipRecordMetafileStream(IntPtr stream, IntPtr referenceHdc, EmfType emfType, RectangleF* frameRect, MetafileFrameUnit frameUnit, string? description, IntPtr* metafile);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipRecordMetafileStream(IntPtr stream, IntPtr referenceHdc, EmfType emfType, IntPtr pframeRect, MetafileFrameUnit frameUnit, string? description, IntPtr* metafile);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipRecordMetafileStreamI(IntPtr stream, IntPtr referenceHdc, EmfType emfType, Rectangle* frameRect, MetafileFrameUnit frameUnit, string? description, IntPtr* metafile);

            [LibraryImport(LibraryName)]
            internal static partial int GdipComment(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int sizeData, byte[] data);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateFontFromLogfontW(IntPtr hdc, ref Interop.User32.LOGFONT lf, out IntPtr font);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromStream(IntPtr stream, IntPtr* bitmap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromStreamICM(IntPtr stream, IntPtr* bitmap);
        }
    }
}
