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
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.GeneratedMarshalling;
#endif

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

                IntPtr lib;
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
            [GeneratedDllImport(LibraryName)]
            internal static partial int GdiplusStartup(out IntPtr token, in StartupInputEx input, out StartupOutput output);

            [GeneratedDllImport(LibraryName)]
            internal static partial void GdiplusShutdown(ref ulong token);

            [GeneratedDllImport(LibraryName)]
            internal static partial IntPtr GdipAlloc(int size);

            [GeneratedDllImport(LibraryName)]
            internal static partial void GdipFree(IntPtr ptr);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDeleteBrush(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetBrushType(IntPtr brush, out BrushType type);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDeleteGraphics(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipRestoreGraphics(IntPtr graphics, uint graphicsState);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipReleaseDC(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hdc);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipFillPath(IntPtr graphics, IntPtr brush, IntPtr path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetNearestColor(IntPtr graphics, out int argb);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipAddPathString(IntPtr path, string s, int lenght, IntPtr family, int style, float emSize, ref RectangleF layoutRect, IntPtr format);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipAddPathStringI(IntPtr path, string s, int lenght, IntPtr family, int style, float emSize, ref Rectangle layoutRect, IntPtr format);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateFromHWND(IntPtr hwnd, out IntPtr graphics);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCloneImage(IntPtr image, out IntPtr imageclone);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImagePaletteSize(IntPtr image, out int size);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImagePalette(IntPtr image, IntPtr palette, int size);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetImagePalette(IntPtr image, IntPtr palette);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageBounds(IntPtr image, out RectangleF source, ref GraphicsUnit unit);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageThumbnail(IntPtr image, uint width, uint height, out IntPtr thumbImage, IntPtr callback, IntPtr callBackData);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipSaveImageToFile(IntPtr image, string filename, ref Guid encoderClsID, IntPtr encoderParameters);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSaveAdd(IntPtr image, IntPtr encoderParameters);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSaveAddImage(IntPtr image, IntPtr imagenew, IntPtr encoderParameters);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageGraphicsContext(IntPtr image, out IntPtr graphics);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreatePath(FillMode brushMode, out IntPtr path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreatePath2(PointF[] points, byte[] types, int count, FillMode brushMode, out IntPtr path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreatePath2I(Point[] points, byte[] types, int count, FillMode brushMode, out IntPtr path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipClonePath(IntPtr path, out IntPtr clonePath);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDeletePath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipResetPath(IntPtr path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPointCount(IntPtr path, out int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathTypes(IntPtr path, byte[] types, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathPoints(IntPtr path, PointF[] points, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathPointsI(IntPtr path, Point[] points, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathFillMode(IntPtr path, out FillMode fillMode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPathFillMode(IntPtr path, FillMode fillMode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipStartPathFigure(IntPtr path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipClosePathFigure(IntPtr path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipClosePathFigures(IntPtr path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPathMarker(IntPtr path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipClearPathMarkers(IntPtr path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipReversePath(IntPtr path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathLastPoint(IntPtr path, out PointF lastPoint);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathLine(IntPtr path, float x1, float y1, float x2, float y2);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathLine2(IntPtr path, PointF[] points, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathLine2I(IntPtr path, Point[] points, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathArc(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathBezier(IntPtr path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathBeziers(IntPtr path, PointF[] points, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathCurve(IntPtr path, PointF[] points, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathCurveI(IntPtr path, Point[] points, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathCurve2(IntPtr path, PointF[] points, int count, float tension);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathCurve2I(IntPtr path, Point[] points, int count, float tension);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathCurve3(IntPtr path, PointF[] points, int count, int offset, int numberOfSegments, float tension);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathCurve3I(IntPtr path, Point[] points, int count, int offset, int numberOfSegments, float tension);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathClosedCurve(IntPtr path, PointF[] points, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathClosedCurveI(IntPtr path, Point[] points, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathClosedCurve2(IntPtr path, PointF[] points, int count, float tension);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathClosedCurve2I(IntPtr path, Point[] points, int count, float tension);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathRectangle(IntPtr path, float x, float y, float width, float height);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathRectangles(IntPtr path, RectangleF[] rects, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathEllipse(IntPtr path, float x, float y, float width, float height);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathEllipseI(IntPtr path, int x, int y, int width, int height);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathPie(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathPieI(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathPolygon(IntPtr path, PointF[] points, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathPath(IntPtr path, IntPtr addingPath, bool connect);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathLineI(IntPtr path, int x1, int y1, int x2, int y2);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathArcI(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathBezierI(IntPtr path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathBeziersI(IntPtr path, Point[] points, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathPolygonI(IntPtr path, Point[] points, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathRectangleI(IntPtr path, int x, int y, int width, int height);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipAddPathRectanglesI(IntPtr path, Rectangle[] rects, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipFlattenPath(IntPtr path, IntPtr matrix, float floatness);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipTransformPath(IntPtr path, IntPtr matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipWarpPath(IntPtr path, IntPtr matrix, PointF[] points, int count, float srcx, float srcy, float srcwidth, float srcheight, WarpMode mode, float flatness);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipWidenPath(IntPtr path, IntPtr pen, IntPtr matrix, float flatness);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathWorldBounds(IntPtr path, out RectangleF bounds, IntPtr matrix, IntPtr pen);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathWorldBoundsI(IntPtr path, out Rectangle bounds, IntPtr matrix, IntPtr pen);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsVisiblePathPoint(IntPtr path, float x, float y, IntPtr graphics, out bool result);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsVisiblePathPointI(IntPtr path, int x, int y, IntPtr graphics, out bool result);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsOutlineVisiblePathPoint(IntPtr path, float x, float y, IntPtr pen, IntPtr graphics, out bool result);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsOutlineVisiblePathPointI(IntPtr path, int x, int y, IntPtr pen, IntPtr graphics, out bool result);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateFontFromLogfont(IntPtr hdc, ref Interop.User32.LOGFONT lf, out IntPtr ptr);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateFontFromHfont(IntPtr hdc, out IntPtr font, ref Interop.User32.LOGFONT lf);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipGetMetafileHeaderFromFile(string filename, IntPtr header);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetMetafileHeaderFromMetafile(IntPtr metafile, IntPtr header);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetMetafileHeaderFromEmf(IntPtr hEmf, IntPtr header);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetMetafileHeaderFromWmf(IntPtr hWmf, IntPtr wmfPlaceableFileHeader, IntPtr header);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetHemfFromMetafile(IntPtr metafile, out IntPtr hEmf);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetMetafileDownLevelRasterizationLimit(IntPtr metafile, ref uint metafileRasterizationLimitDpi);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetMetafileDownLevelRasterizationLimit(IntPtr metafile, uint metafileRasterizationLimitDpi);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateFromContext_macosx(IntPtr cgref, int width, int height, out IntPtr graphics);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetVisibleClip_linux(IntPtr graphics, ref Rectangle rect);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateFromXDrawable_linux(IntPtr drawable, IntPtr display, out IntPtr graphics);

            // Stream functions for non-Win32 (libgdiplus specific)
            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipLoadImageFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr image);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSaveImageToDelegate_linux(IntPtr image, StreamGetBytesDelegate getBytes,
                StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek, StreamCloseDelegate close,
                StreamSizeDelegate size, ref Guid encoderClsID, IntPtr encoderParameters);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateMetafileFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr metafile);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetMetafileHeaderFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr header);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipRecordMetafileFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref RectangleF frameRect,
                MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipRecordMetafileFromDelegateI_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref Rectangle frameRect,
                MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPostScriptGraphicsContext(
                [MarshalAs(UnmanagedType.LPStr)] string filename,
                int width, int height, double dpix, double dpiy, ref IntPtr graphics);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPostScriptSavePage(IntPtr graphics);
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
