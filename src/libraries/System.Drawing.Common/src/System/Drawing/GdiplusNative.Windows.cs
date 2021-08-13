// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.Runtime.InteropServices;

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
            internal static extern int GdipLoadImageFromStream(IntPtr stream, IntPtr* image);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipLoadImageFromStreamICM(IntPtr stream, IntPtr* image);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCloneImage(HandleRef image, out IntPtr cloneimage);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipSaveImageToFile(HandleRef image, string filename, ref Guid classId, HandleRef encoderParams);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSaveImageToStream(HandleRef image, IntPtr stream, Guid* classId, HandleRef encoderParams);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSaveAdd(HandleRef image, HandleRef encoderParams);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSaveAddImage(HandleRef image, HandleRef newImage, HandleRef encoderParams);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageGraphicsContext(HandleRef image, out IntPtr graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageBounds(HandleRef image, out RectangleF gprectf, out GraphicsUnit unit);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageThumbnail(HandleRef image, int thumbWidth, int thumbHeight, out IntPtr thumbImage, Image.GetThumbnailImageAbort? callback, IntPtr callbackdata);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImagePalette(HandleRef image, IntPtr palette, int size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetImagePalette(HandleRef image, IntPtr palette);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImagePaletteSize(HandleRef image, out int size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipImageForceValidation(IntPtr image);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateFromHDC2(IntPtr hdc, IntPtr hdevice, out IntPtr graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateFromHWND(IntPtr hwnd, out IntPtr graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeleteGraphics(HandleRef graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipReleaseDC(HandleRef graphics, IntPtr hdc);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetNearestColor(HandleRef graphics, ref int color);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern IntPtr GdipCreateHalftonePalette();

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawBeziers(HandleRef graphics, HandleRef pen, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawBeziersI(HandleRef graphics, HandleRef pen, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillPath(HandleRef graphics, HandleRef brush, HandleRef path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileDestPoint(HandleRef graphics, HandleRef metafile, ref PointF destPoint, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileDestPointI(HandleRef graphics, HandleRef metafile, ref Point destPoint, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileDestRect(HandleRef graphics, HandleRef metafile, ref RectangleF destRect, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileDestRectI(HandleRef graphics, HandleRef metafile, ref Rectangle destRect, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileDestPoints(HandleRef graphics, HandleRef metafile, PointF* destPoints, int count, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileDestPointsI(HandleRef graphics, HandleRef metafile, Point* destPoints, int count, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileSrcRectDestPoint(HandleRef graphics, HandleRef metafile, ref PointF destPoint, ref RectangleF srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileSrcRectDestPointI(HandleRef graphics, HandleRef metafile, ref Point destPoint, ref Rectangle srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileSrcRectDestRect(HandleRef graphics, HandleRef metafile, ref RectangleF destRect, ref RectangleF srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileSrcRectDestRectI(HandleRef graphics, HandleRef metafile, ref Rectangle destRect, ref Rectangle srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileSrcRectDestPoints(HandleRef graphics, HandleRef metafile, PointF* destPoints, int count, ref RectangleF srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEnumerateMetafileSrcRectDestPointsI(HandleRef graphics, HandleRef metafile, Point* destPoints, int count, ref Rectangle srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipRestoreGraphics(HandleRef graphics, int state);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetMetafileHeaderFromWmf(IntPtr hMetafile, WmfPlaceableFileHeader wmfplaceable, [In] [Out] MetafileHeaderWmf metafileHeaderWmf);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetMetafileHeaderFromEmf(IntPtr hEnhMetafile, [In] [Out] MetafileHeaderEmf metafileHeaderEmf);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipGetMetafileHeaderFromFile(string filename, IntPtr header);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetMetafileHeaderFromStream(IntPtr stream, IntPtr header);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetMetafileHeaderFromMetafile(HandleRef metafile, IntPtr header);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetHemfFromMetafile(HandleRef metafile, out IntPtr hEnhMetafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateMetafileFromStream(IntPtr stream, IntPtr* metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileStream(IntPtr stream, IntPtr referenceHdc, EmfType emfType, RectangleF* frameRect, MetafileFrameUnit frameUnit, string? description, IntPtr* metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileStream(IntPtr stream, IntPtr referenceHdc, EmfType emfType, IntPtr pframeRect, MetafileFrameUnit frameUnit, string? description, IntPtr* metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileStreamI(IntPtr stream, IntPtr referenceHdc, EmfType emfType, Rectangle* frameRect, MetafileFrameUnit frameUnit, string? description, IntPtr* metafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipComment(HandleRef graphics, int sizeData, byte[] data);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipCreateFontFromLogfontW(IntPtr hdc, ref Interop.User32.LOGFONT lf, out IntPtr font);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateBitmapFromStream(IntPtr stream, IntPtr* bitmap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateBitmapFromStreamICM(IntPtr stream, IntPtr* bitmap);
        }
    }
}
