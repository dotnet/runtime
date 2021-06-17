// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal static partial class SafeNativeMethods
    {
        internal unsafe partial class Gdip
        {
            internal const string LibraryName = "libgdiplus";
            public static IntPtr Display = IntPtr.Zero;

            // Indicates whether X11 is available. It's available on Linux but not on recent macOS versions
            // When set to false, where Carbon Drawing is used instead.
            // macOS users can force X11 by setting the SYSTEM_DRAWING_COMMON_FORCE_X11 flag.
            public static bool UseX11Drawable { get; } =
                !RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                Environment.GetEnvironmentVariable("SYSTEM_DRAWING_COMMON_FORCE_X11") != null;

            internal static IntPtr LoadNativeLibrary()
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                IntPtr lib = IntPtr.Zero;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    if (!NativeLibrary.TryLoad("libgdiplus.dylib", assembly, default, out lib))
                    {
                        // homebrew install location
                        if (!NativeLibrary.TryLoad("/usr/local/lib/libgdiplus.dylib", assembly, default, out lib))
                        {
                            // macports install location
                            NativeLibrary.TryLoad("/opt/local/lib/libgdiplus.dylib", assembly, default, out lib);
                        }
                    }
                }
                else
                {
                    // Various Unix package managers have chosen different names for the "libgdiplus" shared library.
                    // The mono project, where libgdiplus originated, allowed both of the names below to be used, via
                    // a global configuration setting. We prefer the "unversioned" shared object name, and fallback to
                    // the name suffixed with ".0".
                    if (!NativeLibrary.TryLoad("libgdiplus.so", assembly, default, out lib))
                    {
                        NativeLibrary.TryLoad("libgdiplus.so.0", assembly, default, out lib);
                    }
                }

                // This function may return a null handle. If it does, individual functions loaded from it will throw a DllNotFoundException,
                // but not until an attempt is made to actually use the function (rather than load it). This matches how PInvokes behave.
                return lib;
            }

            private static void PlatformInitialize()
            {
                LibraryResolver.EnsureRegistered();
            }

            // Imported functions

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdiplusStartup(out IntPtr token, ref StartupInput input, out StartupOutput output);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern void GdiplusShutdown(ref ulong token);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern IntPtr GdipAlloc(int size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern void GdipFree(IntPtr ptr);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeleteBrush(HandleRef brush);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetBrushType(IntPtr brush, out BrushType type);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeleteGraphics(HandleRef graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipRestoreGraphics(IntPtr graphics, uint graphicsState);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipReleaseDC(HandleRef graphics, HandleRef hdc);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillPath(IntPtr graphics, IntPtr brush, IntPtr path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetNearestColor(IntPtr graphics, out int argb);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipAddPathString(IntPtr path, string s, int lenght, IntPtr family, int style, float emSize, ref RectangleF layoutRect, IntPtr format);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipAddPathStringI(IntPtr path, string s, int lenght, IntPtr family, int style, float emSize, ref Rectangle layoutRect, IntPtr format);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateFromHWND(IntPtr hwnd, out IntPtr graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCloneImage(IntPtr image, out IntPtr imageclone);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImagePaletteSize(IntPtr image, out int size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImagePalette(IntPtr image, IntPtr palette, int size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetImagePalette(IntPtr image, IntPtr palette);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageBounds(IntPtr image, out RectangleF source, ref GraphicsUnit unit);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageThumbnail(IntPtr image, uint width, uint height, out IntPtr thumbImage, IntPtr callback, IntPtr callBackData);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipSaveImageToFile(IntPtr image, string filename, ref Guid encoderClsID, IntPtr encoderParameters);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSaveAdd(IntPtr image, IntPtr encoderParameters);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSaveAddImage(IntPtr image, IntPtr imagenew, IntPtr encoderParameters);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageGraphicsContext(IntPtr image, out IntPtr graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreatePath(FillMode brushMode, out IntPtr path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreatePath2(PointF[] points, byte[] types, int count, FillMode brushMode, out IntPtr path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreatePath2I(Point[] points, byte[] types, int count, FillMode brushMode, out IntPtr path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipClonePath(IntPtr path, out IntPtr clonePath);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeletePath(HandleRef path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipResetPath(IntPtr path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPointCount(IntPtr path, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathTypes(IntPtr path, [Out] byte[] types, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathPoints(IntPtr path, [Out] PointF[] points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathPointsI(IntPtr path, [Out] Point[] points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathFillMode(IntPtr path, out FillMode fillMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPathFillMode(IntPtr path, FillMode fillMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipStartPathFigure(IntPtr path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipClosePathFigure(IntPtr path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipClosePathFigures(IntPtr path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPathMarker(IntPtr path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipClearPathMarkers(IntPtr path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipReversePath(IntPtr path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathLastPoint(IntPtr path, out PointF lastPoint);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathLine(IntPtr path, float x1, float y1, float x2, float y2);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathLine2(IntPtr path, PointF[] points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathLine2I(IntPtr path, Point[] points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathArc(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathBezier(IntPtr path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathBeziers(IntPtr path, PointF[] points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathCurve(IntPtr path, PointF[] points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathCurveI(IntPtr path, Point[] points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathCurve2(IntPtr path, PointF[] points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathCurve2I(IntPtr path, Point[] points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathCurve3(IntPtr path, PointF[] points, int count, int offset, int numberOfSegments, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathCurve3I(IntPtr path, Point[] points, int count, int offset, int numberOfSegments, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathClosedCurve(IntPtr path, PointF[] points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathClosedCurveI(IntPtr path, Point[] points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathClosedCurve2(IntPtr path, PointF[] points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathClosedCurve2I(IntPtr path, Point[] points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathRectangle(IntPtr path, float x, float y, float width, float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathRectangles(IntPtr path, RectangleF[] rects, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathEllipse(IntPtr path, float x, float y, float width, float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathEllipseI(IntPtr path, int x, int y, int width, int height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathPie(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathPieI(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathPolygon(IntPtr path, PointF[] points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathPath(IntPtr path, IntPtr addingPath, bool connect);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathLineI(IntPtr path, int x1, int y1, int x2, int y2);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathArcI(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathBezierI(IntPtr path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathBeziersI(IntPtr path, Point[] points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathPolygonI(IntPtr path, Point[] points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathRectangleI(IntPtr path, int x, int y, int width, int height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipAddPathRectanglesI(IntPtr path, Rectangle[] rects, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFlattenPath(IntPtr path, IntPtr matrix, float floatness);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTransformPath(IntPtr path, IntPtr matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipWarpPath(IntPtr path, IntPtr matrix, PointF[] points, int count, float srcx, float srcy, float srcwidth, float srcheight, WarpMode mode, float flatness);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipWidenPath(IntPtr path, IntPtr pen, IntPtr matrix, float flatness);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathWorldBounds(IntPtr path, out RectangleF bounds, IntPtr matrix, IntPtr pen);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathWorldBoundsI(IntPtr path, out Rectangle bounds, IntPtr matrix, IntPtr pen);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsVisiblePathPoint(IntPtr path, float x, float y, IntPtr graphics, out bool result);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsVisiblePathPointI(IntPtr path, int x, int y, IntPtr graphics, out bool result);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsOutlineVisiblePathPoint(IntPtr path, float x, float y, IntPtr pen, IntPtr graphics, out bool result);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsOutlineVisiblePathPointI(IntPtr path, int x, int y, IntPtr pen, IntPtr graphics, out bool result);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateFontFromLogfont(IntPtr hdc, ref Interop.User32.LOGFONT lf, out IntPtr ptr);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateFontFromHfont(IntPtr hdc, out IntPtr font, ref Interop.User32.LOGFONT lf);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipGetMetafileHeaderFromFile(string filename, IntPtr header);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetMetafileHeaderFromMetafile(IntPtr metafile, IntPtr header);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetMetafileHeaderFromEmf(IntPtr hEmf, IntPtr header);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetMetafileHeaderFromWmf(IntPtr hWmf, IntPtr wmfPlaceableFileHeader, IntPtr header);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetHemfFromMetafile(IntPtr metafile, out IntPtr hEmf);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetMetafileDownLevelRasterizationLimit(IntPtr metafile, ref uint metafileRasterizationLimitDpi);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetMetafileDownLevelRasterizationLimit(IntPtr metafile, uint metafileRasterizationLimitDpi);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateFromContext_macosx(IntPtr cgref, int width, int height, out IntPtr graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetVisibleClip_linux(IntPtr graphics, ref Rectangle rect);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateFromXDrawable_linux(IntPtr drawable, IntPtr display, out IntPtr graphics);

            // Stream functions for non-Win32 (libgdiplus specific)
            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipLoadImageFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr image);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSaveImageToDelegate_linux(IntPtr image, StreamGetBytesDelegate getBytes,
                StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek, StreamCloseDelegate close,
                StreamSizeDelegate size, ref Guid encoderClsID, IntPtr encoderParameters);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateMetafileFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetMetafileHeaderFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr header);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref RectangleF frameRect,
                MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileFromDelegateI_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref Rectangle frameRect,
                MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPostScriptGraphicsContext(
                [MarshalAs(UnmanagedType.LPStr)] string filename,
                int width, int height, double dpix, double dpiy, ref IntPtr graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPostScriptSavePage(IntPtr graphics);
        }
    }

    // These are unix-only
    internal unsafe delegate int StreamGetHeaderDelegate(byte* buf, int bufsz);
    internal unsafe delegate int StreamGetBytesDelegate(byte* buf, int bufsz, bool peek);
    internal delegate long StreamSeekDelegate(int offset, int whence);
    internal unsafe delegate int StreamPutBytesDelegate(byte* buf, int bufsz);
    internal delegate void StreamCloseDelegate();
    internal delegate long StreamSizeDelegate();
}
