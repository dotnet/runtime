// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable BCL0015 // We know these APIs are only on Windows

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal unsafe static partial class SafeNativeMethods
    {
        internal static partial class Gdip
        {
            private const string LibraryName = "gdiplus.dll";

            private static void PlatformInitialize()
            {
            }

            // Imported functions

            [DllImport(LibraryName, ExactSpelling = true)]
            private static extern int GdiplusStartup(out IntPtr token, ref StartupInput input, out StartupOutput output);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreatePath(int brushMode, out IntPtr path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreatePath2(PointF* points, byte* types, int count, int brushMode, out IntPtr path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreatePath2I(Point* points, byte* types, int count, int brushMode, out IntPtr path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipClonePath(HandleRef path, out IntPtr clonepath);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeletePath(HandleRef path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipResetPath(HandleRef path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPointCount(HandleRef path, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathTypes(HandleRef path, byte[] types, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathPoints(HandleRef path, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathFillMode(HandleRef path, out FillMode fillmode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPathFillMode(HandleRef path, FillMode fillmode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathData(HandleRef path, GpPathData* pathData);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipStartPathFigure(HandleRef path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipClosePathFigure(HandleRef path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipClosePathFigures(HandleRef path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPathMarker(HandleRef path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipClearPathMarkers(HandleRef path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipReversePath(HandleRef path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathLastPoint(HandleRef path, out PointF lastPoint);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathLine(HandleRef path, float x1, float y1, float x2, float y2);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathLine2(HandleRef path, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathArc(HandleRef path, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathBezier(HandleRef path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathBeziers(HandleRef path, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathCurve(HandleRef path, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathCurve2(HandleRef path, PointF* points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathCurve3(HandleRef path, PointF* points, int count, int offset, int numberOfSegments, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathClosedCurve(HandleRef path, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathClosedCurve2(HandleRef path, PointF* points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathRectangle(HandleRef path, float x, float y, float width, float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathRectangles(HandleRef path, RectangleF* rects, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathEllipse(HandleRef path, float x, float y, float width, float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathPie(HandleRef path, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathPolygon(HandleRef path, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathPath(HandleRef path, HandleRef addingPath, bool connect);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipAddPathString(HandleRef path, string s, int length, HandleRef fontFamily, int style, float emSize, ref RectangleF layoutRect, HandleRef format);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipAddPathStringI(HandleRef path, string s, int length, HandleRef fontFamily, int style, float emSize, ref Rectangle layoutRect, HandleRef format);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathLineI(HandleRef path, int x1, int y1, int x2, int y2);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathLine2I(HandleRef path, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathArcI(HandleRef path, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathBezierI(HandleRef path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathBeziersI(HandleRef path, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathCurveI(HandleRef path, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathCurve2I(HandleRef path, Point* points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathCurve3I(HandleRef path, Point* points, int count, int offset, int numberOfSegments, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathClosedCurveI(HandleRef path, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathClosedCurve2I(HandleRef path, Point* points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathRectangleI(HandleRef path, int x, int y, int width, int height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathRectanglesI(HandleRef path, Rectangle* rects, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathEllipseI(HandleRef path, int x, int y, int width, int height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathPieI(HandleRef path, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathPolygonI(HandleRef path, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFlattenPath(HandleRef path, HandleRef matrixfloat, float flatness);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipWidenPath(HandleRef path, HandleRef pen, HandleRef matrix, float flatness);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipWarpPath(HandleRef path, HandleRef matrix, PointF* points, int count, float srcX, float srcY, float srcWidth, float srcHeight, WarpMode warpMode, float flatness);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTransformPath(HandleRef path, HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathWorldBounds(HandleRef path, out RectangleF gprectf, HandleRef matrix, HandleRef pen);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsVisiblePathPoint(HandleRef path, float x, float y, HandleRef graphics, out bool result);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsVisiblePathPointI(HandleRef path, int x, int y, HandleRef graphics, out bool result);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsOutlineVisiblePathPoint(HandleRef path, float x, float y, HandleRef pen, HandleRef graphics, out bool result);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsOutlineVisiblePathPointI(HandleRef path, int x, int y, HandleRef pen, HandleRef graphics, out bool result);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeleteBrush(HandleRef brush);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipLoadImageFromStream(Interop.Ole32.IStream stream, out IntPtr image);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipLoadImageFromFile(string filename, out IntPtr image);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipLoadImageFromStreamICM(Interop.Ole32.IStream stream, out IntPtr image);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipLoadImageFromFileICM(string filename, out IntPtr image);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCloneImage(HandleRef image, out IntPtr cloneimage);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDisposeImage(HandleRef image);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipSaveImageToFile(HandleRef image, string filename, ref Guid classId, HandleRef encoderParams);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSaveImageToStream(HandleRef image, Interop.Ole32.IStream stream, ref Guid classId, HandleRef encoderParams);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSaveAdd(HandleRef image, HandleRef encoderParams);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSaveAddImage(HandleRef image, HandleRef newImage, HandleRef encoderParams);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageGraphicsContext(HandleRef image, out IntPtr graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageBounds(HandleRef image, out RectangleF gprectf, out GraphicsUnit unit);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageType(HandleRef image, out int type);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageThumbnail(HandleRef image, int thumbWidth, int thumbHeight, out IntPtr thumbImage, Image.GetThumbnailImageAbort callback, IntPtr callbackdata);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetEncoderParameterListSize(HandleRef image, ref Guid clsid, out int size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetEncoderParameterList(HandleRef image, ref Guid clsid, int size, IntPtr buffer);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImagePalette(HandleRef image, IntPtr palette, int size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetImagePalette(HandleRef image, IntPtr palette);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImagePaletteSize(HandleRef image, out int size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPropertyCount(HandleRef image, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPropertyIdList(HandleRef image, int count, int[] list);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPropertyItemSize(HandleRef image, int propid, out int size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPropertyItem(HandleRef image, int propid, int size, IntPtr buffer);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPropertySize(HandleRef image, out int totalSize, ref int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetAllPropertyItems(HandleRef image, int totalSize, int count, IntPtr buffer);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPropertyItem(HandleRef image, PropertyItemInternal propitem);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipImageForceValidation(HandleRef image);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateFromHDC(HandleRef hdc, out IntPtr graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateFromHDC2(HandleRef hdc, HandleRef hdevice, out IntPtr graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateFromHWND(HandleRef hwnd, out IntPtr graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeleteGraphics(HandleRef graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipReleaseDC(HandleRef graphics, HandleRef hdc);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTransformPoints(HandleRef graphics, int destSpace, int srcSpace, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTransformPointsI(HandleRef graphics, int destSpace, int srcSpace, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetNearestColor(HandleRef graphics, ref int color);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern IntPtr GdipCreateHalftonePalette();

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawLine(HandleRef graphics, HandleRef pen, float x1, float y1, float x2, float y2);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawLineI(HandleRef graphics, HandleRef pen, int x1, int y1, int x2, int y2);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawLines(HandleRef graphics, HandleRef pen, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawLinesI(HandleRef graphics, HandleRef pen, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawArc(HandleRef graphics, HandleRef pen, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawArcI(HandleRef graphics, HandleRef pen, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawBezier(HandleRef graphics, HandleRef pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawBeziers(HandleRef graphics, HandleRef pen, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawBeziersI(HandleRef graphics, HandleRef pen, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawRectangle(HandleRef graphics, HandleRef pen, float x, float y, float width, float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawRectangleI(HandleRef graphics, HandleRef pen, int x, int y, int width, int height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawRectangles(HandleRef graphics, HandleRef pen, RectangleF* rects, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawRectanglesI(HandleRef graphics, HandleRef pen, Rectangle* rects, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawEllipse(HandleRef graphics, HandleRef pen, float x, float y, float width, float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawEllipseI(HandleRef graphics, HandleRef pen, int x, int y, int width, int height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawPie(HandleRef graphics, HandleRef pen, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawPieI(HandleRef graphics, HandleRef pen, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawPolygon(HandleRef graphics, HandleRef pen, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawPolygonI(HandleRef graphics, HandleRef pen, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawPath(HandleRef graphics, HandleRef pen, HandleRef path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawCurve(HandleRef graphics, HandleRef pen, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawCurveI(HandleRef graphics, HandleRef pen, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawCurve2(HandleRef graphics, HandleRef pen, PointF* points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawCurve2I(HandleRef graphics, HandleRef pen, Point* points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawCurve3(HandleRef graphics, HandleRef pen, PointF* points, int count, int offset, int numberOfSegments, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawCurve3I(HandleRef graphics, HandleRef pen, Point* points, int count, int offset, int numberOfSegments, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawClosedCurve(HandleRef graphics, HandleRef pen, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawClosedCurveI(HandleRef graphics, HandleRef pen, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawClosedCurve2(HandleRef graphics, HandleRef pen, PointF* points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawClosedCurve2I(HandleRef graphics, HandleRef pen, Point* points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGraphicsClear(HandleRef graphics, int argb);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillRectangle(HandleRef graphics, HandleRef brush, float x, float y, float width, float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillRectangleI(HandleRef graphics, HandleRef brush, int x, int y, int width, int height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillRectangles(HandleRef graphics, HandleRef brush, RectangleF* rects, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillRectanglesI(HandleRef graphics, HandleRef brush, Rectangle* rects, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillPolygon(HandleRef graphics, HandleRef brush, PointF* points, int count, FillMode brushMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillPolygonI(HandleRef graphics, HandleRef brush, Point* points, int count, FillMode brushMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillEllipse(HandleRef graphics, HandleRef brush, float x, float y, float width, float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillEllipseI(HandleRef graphics, HandleRef brush, int x, int y, int width, int height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillPie(HandleRef graphics, HandleRef brush, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillPieI(HandleRef graphics, HandleRef brush, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillPath(HandleRef graphics, HandleRef brush, HandleRef path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillClosedCurve(HandleRef graphics, HandleRef brush, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillClosedCurveI(HandleRef graphics, HandleRef brush, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillClosedCurve2(HandleRef graphics, HandleRef brush, PointF* points, int count, float tension, FillMode mode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillClosedCurve2I(HandleRef graphics, HandleRef brush, Point* points, int count, float tension, FillMode mode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImage(HandleRef graphics, HandleRef image, float x, float y);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImageI(HandleRef graphics, HandleRef image, int x, int y);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImageRect(HandleRef graphics, HandleRef image, float x, float y, float width, float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImageRectI(HandleRef graphics, HandleRef image, int x, int y, int width, int height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImagePoints(HandleRef graphics, HandleRef image, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImagePointsI(HandleRef graphics, HandleRef image, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImagePointRect(HandleRef graphics, HandleRef image, float x, float y, float srcx, float srcy, float srcwidth, float srcheight, int srcunit);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImagePointRectI(HandleRef graphics, HandleRef image, int x, int y, int srcx, int srcy, int srcwidth, int srcheight, int srcunit);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImageRectRect(HandleRef graphics, HandleRef image, float dstx, float dsty, float dstwidth, float dstheight, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort callback, HandleRef callbackdata);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImageRectRectI(HandleRef graphics, HandleRef image, int dstx, int dsty, int dstwidth, int dstheight, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort callback, HandleRef callbackdata);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImagePointsRect(HandleRef graphics, HandleRef image, PointF* points, int count, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort callback, HandleRef callbackdata);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImagePointsRectI(HandleRef graphics, HandleRef image, Point* points, int count, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort callback, HandleRef callbackdata);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileDestPoint(HandleRef graphics, HandleRef metafile, ref PointF destPoint, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileDestPointI(HandleRef graphics, HandleRef metafile, ref Point destPoint, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileDestRect(HandleRef graphics, HandleRef metafile, ref RectangleF destRect, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileDestRectI(HandleRef graphics, HandleRef metafile, ref Rectangle destRect, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileDestPoints(HandleRef graphics, HandleRef metafile, PointF* destPoints, int count, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileDestPointsI(HandleRef graphics, HandleRef metafile, Point* destPoints, int count, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileSrcRectDestPoint(HandleRef graphics, HandleRef metafile, ref PointF destPoint, ref RectangleF srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileSrcRectDestPointI(HandleRef graphics, HandleRef metafile, ref Point destPoint, ref Rectangle srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileSrcRectDestRect(HandleRef graphics, HandleRef metafile, ref RectangleF destRect, ref RectangleF srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileSrcRectDestRectI(HandleRef graphics, HandleRef metafile, ref Rectangle destRect, ref Rectangle srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileSrcRectDestPoints(HandleRef graphics, HandleRef metafile, PointF* destPoints, int count, ref RectangleF srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileSrcRectDestPointsI(HandleRef graphics, HandleRef metafile, Point* destPoints, int count, ref Rectangle srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipPlayMetafileRecord(HandleRef graphics, EmfPlusRecordType recordType, int flags, int dataSize, byte[] data);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSaveGraphics(HandleRef graphics, out int state);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipRestoreGraphics(HandleRef graphics, int state);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetMetafileHeaderFromWmf(HandleRef hMetafile, WmfPlaceableFileHeader wmfplaceable, [In] [Out] MetafileHeaderWmf metafileHeaderWmf);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetMetafileHeaderFromEmf(HandleRef hEnhMetafile, [In] [Out] MetafileHeaderEmf metafileHeaderEmf);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipGetMetafileHeaderFromFile(string filename, IntPtr header);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetMetafileHeaderFromStream(Interop.Ole32.IStream stream, IntPtr header);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetMetafileHeaderFromMetafile(HandleRef metafile, IntPtr header);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetHemfFromMetafile(HandleRef metafile, out IntPtr hEnhMetafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateMetafileFromWmf(HandleRef hMetafile, bool deleteWmf, WmfPlaceableFileHeader wmfplacealbeHeader, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateMetafileFromEmf(HandleRef hEnhMetafile, bool deleteEmf, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipCreateMetafileFromFile(string file, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateMetafileFromStream(Interop.Ole32.IStream stream, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipRecordMetafile(HandleRef referenceHdc, EmfType emfType, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipRecordMetafile(HandleRef referenceHdc, EmfType emfType, HandleRef pframeRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipRecordMetafileI(HandleRef referenceHdc, EmfType emfType, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileFileName(string fileName, HandleRef referenceHdc, EmfType emfType, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileFileName(string fileName, HandleRef referenceHdc, EmfType emfType, HandleRef pframeRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileFileNameI(string fileName, HandleRef referenceHdc, EmfType emfType, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileStream(Interop.Ole32.IStream stream, HandleRef referenceHdc, EmfType emfType, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileStream(Interop.Ole32.IStream stream, HandleRef referenceHdc, EmfType emfType, HandleRef pframeRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileStreamI(Interop.Ole32.IStream stream, HandleRef referenceHdc, EmfType emfType, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipComment(HandleRef graphics, int sizeData, byte[] data);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateFontFromDC(HandleRef hdc, ref IntPtr font);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipCreateFontFromLogfontW(HandleRef hdc, ref LOGFONT lf, out IntPtr font);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipDrawString(HandleRef graphics, string textString, int length, HandleRef font, ref RectangleF layoutRect, HandleRef stringFormat, HandleRef brush);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipMeasureString(HandleRef graphics, string textString, int length, HandleRef font, ref RectangleF layoutRect, HandleRef stringFormat, ref RectangleF boundingBox, out int codepointsFitted, out int linesFilled);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipMeasureCharacterRanges(HandleRef graphics, string textString, int length, HandleRef font, ref RectangleF layoutRect, HandleRef stringFormat, int characterCount, [In] [Out] IntPtr[] region);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateBitmapFromStream(Interop.Ole32.IStream stream, out IntPtr bitmap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateBitmapFromStreamICM(Interop.Ole32.IStream stream, out IntPtr bitmap);
        }
    }
}
