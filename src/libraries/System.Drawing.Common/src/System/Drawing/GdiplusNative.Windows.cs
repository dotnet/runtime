// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
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

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
            // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable structs.
            [DllImport(LibraryName)]
            private static extern int GdiplusStartup(out IntPtr token, ref StartupInput input, out StartupOutput output);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreatePath(int brushMode, out SafeGraphicsPathHandle path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreatePath2(PointF* points, byte* types, int count, int brushMode, out SafeGraphicsPathHandle path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreatePath2I(Point* points, byte* types, int count, int brushMode, out SafeGraphicsPathHandle path);

            [DllImport(LibraryName)]
            internal static extern int GdipClonePath(SafeGraphicsPathHandle path, out SafeGraphicsPathHandle clonepath);

            [DllImport(LibraryName)]
            internal static extern int GdipDeletePath(IntPtr path);

            [DllImport(LibraryName)]
            internal static extern int GdipResetPath(SafeGraphicsPathHandle path);

            [DllImport(LibraryName)]
            internal static extern int GdipGetPointCount(SafeGraphicsPathHandle path, out int count);

            [DllImport(LibraryName)]
            internal static extern int GdipGetPathTypes(SafeGraphicsPathHandle path, byte[] types, int count);

            [DllImport(LibraryName)]
            internal static extern int GdipGetPathPoints(SafeGraphicsPathHandle path, PointF* points, int count);

            [DllImport(LibraryName)]
            internal static extern int GdipGetPathFillMode(SafeGraphicsPathHandle path, out FillMode fillmode);

            [DllImport(LibraryName)]
            internal static extern int GdipSetPathFillMode(SafeGraphicsPathHandle path, FillMode fillmode);

            [DllImport(LibraryName)]
            internal static extern int GdipGetPathData(SafeGraphicsPathHandle path, GpPathData* pathData);

            [DllImport(LibraryName)]
            internal static extern int GdipStartPathFigure(SafeGraphicsPathHandle path);

            [DllImport(LibraryName)]
            internal static extern int GdipClosePathFigure(SafeGraphicsPathHandle path);

            [DllImport(LibraryName)]
            internal static extern int GdipClosePathFigures(SafeGraphicsPathHandle path);

            [DllImport(LibraryName)]
            internal static extern int GdipSetPathMarker(SafeGraphicsPathHandle path);

            [DllImport(LibraryName)]
            internal static extern int GdipClearPathMarkers(SafeGraphicsPathHandle path);

            [DllImport(LibraryName)]
            internal static extern int GdipReversePath(SafeGraphicsPathHandle path);

            [DllImport(LibraryName)]
            internal static extern int GdipGetPathLastPoint(SafeGraphicsPathHandle path, out PointF lastPoint);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathLine(SafeGraphicsPathHandle path, float x1, float y1, float x2, float y2);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathLine2(SafeGraphicsPathHandle path, PointF* points, int count);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathArc(SafeGraphicsPathHandle path, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathBezier(SafeGraphicsPathHandle path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathBeziers(SafeGraphicsPathHandle path, PointF* points, int count);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathCurve(SafeGraphicsPathHandle path, PointF* points, int count);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathCurve2(SafeGraphicsPathHandle path, PointF* points, int count, float tension);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathCurve3(SafeGraphicsPathHandle path, PointF* points, int count, int offset, int numberOfSegments, float tension);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathClosedCurve(SafeGraphicsPathHandle path, PointF* points, int count);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathClosedCurve2(SafeGraphicsPathHandle path, PointF* points, int count, float tension);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathRectangle(SafeGraphicsPathHandle path, float x, float y, float width, float height);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathRectangles(SafeGraphicsPathHandle path, RectangleF* rects, int count);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathEllipse(SafeGraphicsPathHandle path, float x, float y, float width, float height);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathPie(SafeGraphicsPathHandle path, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathPolygon(SafeGraphicsPathHandle path, PointF* points, int count);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathPath(SafeGraphicsPathHandle path, SafeGraphicsPathHandle addingPath, bool connect);

            [DllImport(LibraryName, CharSet = CharSet.Unicode)]
            internal static extern int GdipAddPathString(SafeGraphicsPathHandle path, string s, int length, HandleRef fontFamily, int style, float emSize, ref RectangleF layoutRect, HandleRef format);

            [DllImport(LibraryName, CharSet = CharSet.Unicode)]
            internal static extern int GdipAddPathStringI(SafeGraphicsPathHandle path, string s, int length, HandleRef fontFamily, int style, float emSize, ref Rectangle layoutRect, HandleRef format);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathLineI(SafeGraphicsPathHandle path, int x1, int y1, int x2, int y2);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathLine2I(SafeGraphicsPathHandle path, Point* points, int count);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathArcI(SafeGraphicsPathHandle path, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathBezierI(SafeGraphicsPathHandle path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathBeziersI(SafeGraphicsPathHandle path, Point* points, int count);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathCurveI(SafeGraphicsPathHandle path, Point* points, int count);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathCurve2I(SafeGraphicsPathHandle path, Point* points, int count, float tension);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathCurve3I(SafeGraphicsPathHandle path, Point* points, int count, int offset, int numberOfSegments, float tension);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathClosedCurveI(SafeGraphicsPathHandle path, Point* points, int count);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathClosedCurve2I(SafeGraphicsPathHandle path, Point* points, int count, float tension);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathRectangleI(SafeGraphicsPathHandle path, int x, int y, int width, int height);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathRectanglesI(SafeGraphicsPathHandle path, Rectangle* rects, int count);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathEllipseI(SafeGraphicsPathHandle path, int x, int y, int width, int height);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathPieI(SafeGraphicsPathHandle path, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [DllImport(LibraryName)]
            internal static extern int GdipAddPathPolygonI(SafeGraphicsPathHandle path, Point* points, int count);

            [DllImport(LibraryName)]
            internal static extern int GdipTransformPath(SafeGraphicsPathHandle path, SafeMatrixHandle matrix);

            [DllImport(LibraryName)]
            internal static extern int GdipIsVisiblePathPoint(SafeGraphicsPathHandle path, float x, float y, HandleRef graphics, out bool result);

            [DllImport(LibraryName)]
            internal static extern int GdipIsVisiblePathPointI(SafeGraphicsPathHandle path, int x, int y, HandleRef graphics, out bool result);

            [DllImport(LibraryName)]
            internal static extern int GdipIsOutlineVisiblePathPoint(SafeGraphicsPathHandle path, float x, float y, SafePenHandle pen, HandleRef graphics, out bool result);

            [DllImport(LibraryName)]
            internal static extern int GdipIsOutlineVisiblePathPointI(SafeGraphicsPathHandle path, int x, int y, SafePenHandle pen, HandleRef graphics, out bool result);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipLoadImageFromStream(IntPtr stream, IntPtr* image);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipLoadImageFromStreamICM(IntPtr stream, IntPtr* image);

            [DllImport(LibraryName)]
            internal static extern int GdipCloneImage(HandleRef image, out IntPtr cloneimage);

            [DllImport(LibraryName, CharSet = CharSet.Unicode)]
            internal static extern int GdipSaveImageToFile(HandleRef image, string filename, ref Guid classId, HandleRef encoderParams);

            [DllImport(LibraryName)]
            internal static extern int GdipSaveImageToStream(HandleRef image, IntPtr stream, Guid* classId, HandleRef encoderParams);

            [DllImport(LibraryName)]
            internal static extern int GdipSaveAdd(HandleRef image, HandleRef encoderParams);

            [DllImport(LibraryName)]
            internal static extern int GdipSaveAddImage(HandleRef image, HandleRef newImage, HandleRef encoderParams);

            [DllImport(LibraryName)]
            internal static extern int GdipGetImageGraphicsContext(HandleRef image, out IntPtr graphics);

            [DllImport(LibraryName)]
            internal static extern int GdipGetImageBounds(HandleRef image, out RectangleF gprectf, out GraphicsUnit unit);

            [DllImport(LibraryName)]
            internal static extern int GdipGetImageThumbnail(HandleRef image, int thumbWidth, int thumbHeight, out IntPtr thumbImage, Image.GetThumbnailImageAbort? callback, IntPtr callbackdata);

            [DllImport(LibraryName)]
            internal static extern int GdipGetImagePalette(HandleRef image, IntPtr palette, int size);

            [DllImport(LibraryName)]
            internal static extern int GdipSetImagePalette(HandleRef image, IntPtr palette);

            [DllImport(LibraryName)]
            internal static extern int GdipGetImagePaletteSize(HandleRef image, out int size);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipImageForceValidation(IntPtr image);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateFromHDC2(IntPtr hdc, IntPtr hdevice, out IntPtr graphics);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateFromHWND(IntPtr hwnd, out IntPtr graphics);

            [DllImport(LibraryName)]
            internal static extern int GdipDeleteGraphics(HandleRef graphics);

            [DllImport(LibraryName)]
            internal static extern int GdipReleaseDC(HandleRef graphics, IntPtr hdc);

            [DllImport(LibraryName)]
            internal static extern int GdipGetNearestColor(HandleRef graphics, ref int color);

            [GeneratedDllImport(LibraryName)]
            internal static partial IntPtr GdipCreateHalftonePalette();

            [DllImport(LibraryName, SetLastError = true)]
            internal static extern int GdipDrawBeziers(HandleRef graphics, SafePenHandle pen, PointF* points, int count);

            [DllImport(LibraryName, SetLastError = true)]
            internal static extern int GdipDrawBeziersI(HandleRef graphics, SafePenHandle pen, Point* points, int count);

            [DllImport(LibraryName, SetLastError = true)]
            internal static extern int GdipFillPath(HandleRef graphics, SafeBrushHandle brush, SafeGraphicsPathHandle path);

            [DllImport(LibraryName)]
            internal static extern int GdipEnumerateMetafileDestPoint(HandleRef graphics, HandleRef metafile, ref PointF destPoint, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName)]
            internal static extern int GdipEnumerateMetafileDestPointI(HandleRef graphics, HandleRef metafile, ref Point destPoint, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName)]
            internal static extern int GdipEnumerateMetafileDestRect(HandleRef graphics, HandleRef metafile, ref RectangleF destRect, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName)]
            internal static extern int GdipEnumerateMetafileDestRectI(HandleRef graphics, HandleRef metafile, ref Rectangle destRect, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName)]
            internal static extern int GdipEnumerateMetafileDestPoints(HandleRef graphics, HandleRef metafile, PointF* destPoints, int count, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName)]
            internal static extern int GdipEnumerateMetafileDestPointsI(HandleRef graphics, HandleRef metafile, Point* destPoints, int count, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName)]
            internal static extern int GdipEnumerateMetafileSrcRectDestPoint(HandleRef graphics, HandleRef metafile, ref PointF destPoint, ref RectangleF srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName)]
            internal static extern int GdipEnumerateMetafileSrcRectDestPointI(HandleRef graphics, HandleRef metafile, ref Point destPoint, ref Rectangle srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName)]
            internal static extern int GdipEnumerateMetafileSrcRectDestRect(HandleRef graphics, HandleRef metafile, ref RectangleF destRect, ref RectangleF srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName)]
            internal static extern int GdipEnumerateMetafileSrcRectDestRectI(HandleRef graphics, HandleRef metafile, ref Rectangle destRect, ref Rectangle srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName)]
            internal static extern int GdipEnumerateMetafileSrcRectDestPoints(HandleRef graphics, HandleRef metafile, PointF* destPoints, int count, ref RectangleF srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName)]
            internal static extern int GdipEnumerateMetafileSrcRectDestPointsI(HandleRef graphics, HandleRef metafile, Point* destPoints, int count, ref Rectangle srcRect, GraphicsUnit pageUnit, Graphics.EnumerateMetafileProc callback, IntPtr callbackdata, HandleRef imageattributes);

            [DllImport(LibraryName)]
            internal static extern int GdipRestoreGraphics(HandleRef graphics, int state);

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
            // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable structs.
            [DllImport(LibraryName)]
            internal static extern int GdipGetMetafileHeaderFromWmf(IntPtr hMetafile, WmfPlaceableFileHeader wmfplaceable, [In] [Out] MetafileHeaderWmf metafileHeaderWmf);

            [DllImport(LibraryName)]
            internal static extern int GdipGetMetafileHeaderFromEmf(IntPtr hEnhMetafile, [In] [Out] MetafileHeaderEmf metafileHeaderEmf);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipGetMetafileHeaderFromFile(string filename, IntPtr header);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetMetafileHeaderFromStream(IntPtr stream, IntPtr header);

            [DllImport(LibraryName)]
            internal static extern int GdipGetMetafileHeaderFromMetafile(HandleRef metafile, IntPtr header);

            [DllImport(LibraryName)]
            internal static extern int GdipGetHemfFromMetafile(HandleRef metafile, out IntPtr hEnhMetafile);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateMetafileFromStream(IntPtr stream, IntPtr* metafile);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipRecordMetafileStream(IntPtr stream, IntPtr referenceHdc, EmfType emfType, RectangleF* frameRect, MetafileFrameUnit frameUnit, string? description, IntPtr* metafile);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipRecordMetafileStream(IntPtr stream, IntPtr referenceHdc, EmfType emfType, IntPtr pframeRect, MetafileFrameUnit frameUnit, string? description, IntPtr* metafile);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipRecordMetafileStreamI(IntPtr stream, IntPtr referenceHdc, EmfType emfType, Rectangle* frameRect, MetafileFrameUnit frameUnit, string? description, IntPtr* metafile);

            [DllImport(LibraryName)]
            internal static extern int GdipComment(HandleRef graphics, int sizeData, byte[] data);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipCreateFontFromLogfontW(IntPtr hdc, ref Interop.User32.LOGFONT lf, out IntPtr font);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromStream(IntPtr stream, IntPtr* bitmap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromStreamICM(IntPtr stream, IntPtr* bitmap);
        }
    }
}
