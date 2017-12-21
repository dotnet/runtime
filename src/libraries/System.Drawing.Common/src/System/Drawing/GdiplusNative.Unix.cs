// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal partial class SafeNativeMethods
    {
        internal unsafe partial class Gdip
        {
            private const string LibraryName = "libgdiplus";
            public static IntPtr Display = IntPtr.Zero;

            // Indicates whether X11 is available. It's available on Linux but not on recent macOS versions
            // When set to false, where Carbon Drawing is used instead.
            // macOS users can force X11 by setting the SYSTEM_DRAWING_COMMON_FORCE_X11 flag.
            public static bool UseX11Drawable { get; } =
                !RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                Environment.GetEnvironmentVariable("SYSTEM_DRAWING_COMMON_FORCE_X11") != null;

            private static IntPtr LoadNativeLibrary()
            {
                string libraryName;

                IntPtr lib = IntPtr.Zero;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    libraryName = "libgdiplus.dylib";
                    lib = Interop.Libdl.dlopen(libraryName, Interop.Libdl.RTLD_NOW);
                }
                else
                {
                    // Various Unix package managers have chosen different names for the "libgdiplus" shared library.
                    // The mono project, where libgdiplus originated, allowed both of the names below to be used, via
                    // a global configuration setting. We prefer the "unversioned" shared object name, and fallback to
                    // the name suffixed with ".0".
                    libraryName = "libgdiplus.so";
                    lib = Interop.Libdl.dlopen(libraryName, Interop.Libdl.RTLD_NOW);
                    if (lib == IntPtr.Zero)
                    {
                        lib = Interop.Libdl.dlopen("libgdiplus.so.0", Interop.Libdl.RTLD_NOW);
                    }
                }

                // If we couldn't find libgdiplus in the system search path, try to look for libgdiplus in the
                // NuGet package folders. This matches the DllImport behavior.
                if (lib == IntPtr.Zero)
                {
                    string[] searchDirectories = ((string)AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES")).Split(':');

                    foreach (var searchDirectory in searchDirectories)
                    {
                        var searchPath = Path.Combine(searchDirectory, libraryName);

                        lib = Interop.Libdl.dlopen(searchPath, Interop.Libdl.RTLD_NOW);

                        if (lib != IntPtr.Zero)
                        {
                            break;
                        }
                    }
                }

                // This function may return a null handle. If it does, individual functions loaded from it will throw a DllNotFoundException,
                // but not until an attempt is made to actually use the function (rather than load it). This matches how PInvokes behave.
                return lib;
            }

            private static IntPtr LoadFunctionPointer(IntPtr nativeLibraryHandle, string functionName) => Interop.Libdl.dlsym(nativeLibraryHandle, functionName);

            private static void PlatformInitialize()
            {
                LoadFunctionPointers();
            }

            private static void LoadFunctionPointers()
            {
                GdiplusStartup_ptr = FunctionWrapper.Load<GdiplusStartup_delegate>(s_gdipModule, "GdiplusStartup", LibraryName);
                GdiplusShutdown_ptr = FunctionWrapper.Load<GdiplusShutdown_delegate>(s_gdipModule, "GdiplusShutdown", LibraryName);
                GdipAlloc_ptr = FunctionWrapper.Load<GdipAlloc_delegate>(s_gdipModule, "GdipAlloc", LibraryName);
                GdipFree_ptr = FunctionWrapper.Load<GdipFree_delegate>(s_gdipModule, "GdipFree", LibraryName);
                GdipDeleteBrush_ptr = FunctionWrapper.Load<GdipDeleteBrush_delegate>(s_gdipModule, "GdipDeleteBrush", LibraryName);
                GdipGetBrushType_ptr = FunctionWrapper.Load<GdipGetBrushType_delegate>(s_gdipModule, "GdipGetBrushType", LibraryName);
                GdipCreateFromHDC_ptr = FunctionWrapper.Load<GdipCreateFromHDC_delegate>(s_gdipModule, "GdipCreateFromHDC", LibraryName);
                GdipDeleteGraphics_ptr = FunctionWrapper.Load<GdipDeleteGraphics_delegate>(s_gdipModule, "GdipDeleteGraphics", LibraryName);
                GdipRestoreGraphics_ptr = FunctionWrapper.Load<GdipRestoreGraphics_delegate>(s_gdipModule, "GdipRestoreGraphics", LibraryName);
                GdipSaveGraphics_ptr = FunctionWrapper.Load<GdipSaveGraphics_delegate>(s_gdipModule, "GdipSaveGraphics", LibraryName);
                GdipDrawArc_ptr = FunctionWrapper.Load<GdipDrawArc_delegate>(s_gdipModule, "GdipDrawArc", LibraryName);
                GdipDrawArcI_ptr = FunctionWrapper.Load<GdipDrawArcI_delegate>(s_gdipModule, "GdipDrawArcI", LibraryName);
                GdipDrawBezier_ptr = FunctionWrapper.Load<GdipDrawBezier_delegate>(s_gdipModule, "GdipDrawBezier", LibraryName);
                GdipDrawBezierI_ptr = FunctionWrapper.Load<GdipDrawBezierI_delegate>(s_gdipModule, "GdipDrawBezierI", LibraryName);
                GdipDrawEllipseI_ptr = FunctionWrapper.Load<GdipDrawEllipseI_delegate>(s_gdipModule, "GdipDrawEllipseI", LibraryName);
                GdipDrawEllipse_ptr = FunctionWrapper.Load<GdipDrawEllipse_delegate>(s_gdipModule, "GdipDrawEllipse", LibraryName);
                GdipDrawLine_ptr = FunctionWrapper.Load<GdipDrawLine_delegate>(s_gdipModule, "GdipDrawLine", LibraryName);
                GdipDrawLineI_ptr = FunctionWrapper.Load<GdipDrawLineI_delegate>(s_gdipModule, "GdipDrawLineI", LibraryName);
                GdipDrawLines_ptr = FunctionWrapper.Load<GdipDrawLines_delegate>(s_gdipModule, "GdipDrawLines", LibraryName);
                GdipDrawLinesI_ptr = FunctionWrapper.Load<GdipDrawLinesI_delegate>(s_gdipModule, "GdipDrawLinesI", LibraryName);
                GdipDrawPath_ptr = FunctionWrapper.Load<GdipDrawPath_delegate>(s_gdipModule, "GdipDrawPath", LibraryName);
                GdipDrawPie_ptr = FunctionWrapper.Load<GdipDrawPie_delegate>(s_gdipModule, "GdipDrawPie", LibraryName);
                GdipDrawPieI_ptr = FunctionWrapper.Load<GdipDrawPieI_delegate>(s_gdipModule, "GdipDrawPieI", LibraryName);
                GdipDrawPolygon_ptr = FunctionWrapper.Load<GdipDrawPolygon_delegate>(s_gdipModule, "GdipDrawPolygon", LibraryName);
                GdipDrawPolygonI_ptr = FunctionWrapper.Load<GdipDrawPolygonI_delegate>(s_gdipModule, "GdipDrawPolygonI", LibraryName);
                GdipDrawRectangle_ptr = FunctionWrapper.Load<GdipDrawRectangle_delegate>(s_gdipModule, "GdipDrawRectangle", LibraryName);
                GdipDrawRectangleI_ptr = FunctionWrapper.Load<GdipDrawRectangleI_delegate>(s_gdipModule, "GdipDrawRectangleI", LibraryName);
                GdipDrawRectangles_ptr = FunctionWrapper.Load<GdipDrawRectangles_delegate>(s_gdipModule, "GdipDrawRectangles", LibraryName);
                GdipDrawRectanglesI_ptr = FunctionWrapper.Load<GdipDrawRectanglesI_delegate>(s_gdipModule, "GdipDrawRectanglesI", LibraryName);
                GdipFillEllipseI_ptr = FunctionWrapper.Load<GdipFillEllipseI_delegate>(s_gdipModule, "GdipFillEllipseI", LibraryName);
                GdipFillEllipse_ptr = FunctionWrapper.Load<GdipFillEllipse_delegate>(s_gdipModule, "GdipFillEllipse", LibraryName);
                GdipFillPolygon_ptr = FunctionWrapper.Load<GdipFillPolygon_delegate>(s_gdipModule, "GdipFillPolygon", LibraryName);
                GdipFillPolygonI_ptr = FunctionWrapper.Load<GdipFillPolygonI_delegate>(s_gdipModule, "GdipFillPolygonI", LibraryName);
                GdipFillPolygon2_ptr = FunctionWrapper.Load<GdipFillPolygon2_delegate>(s_gdipModule, "GdipFillPolygon2", LibraryName);
                GdipFillPolygon2I_ptr = FunctionWrapper.Load<GdipFillPolygon2I_delegate>(s_gdipModule, "GdipFillPolygon2I", LibraryName);
                GdipFillRectangle_ptr = FunctionWrapper.Load<GdipFillRectangle_delegate>(s_gdipModule, "GdipFillRectangle", LibraryName);
                GdipFillRectangleI_ptr = FunctionWrapper.Load<GdipFillRectangleI_delegate>(s_gdipModule, "GdipFillRectangleI", LibraryName);
                GdipFillRectangles_ptr = FunctionWrapper.Load<GdipFillRectangles_delegate>(s_gdipModule, "GdipFillRectangles", LibraryName);
                GdipFillRectanglesI_ptr = FunctionWrapper.Load<GdipFillRectanglesI_delegate>(s_gdipModule, "GdipFillRectanglesI", LibraryName);
                GdipDrawString_ptr = FunctionWrapper.Load<GdipDrawString_delegate>(s_gdipModule, "GdipDrawString", LibraryName);
                GdipGetDC_ptr = FunctionWrapper.Load<GdipGetDC_delegate>(s_gdipModule, "GdipGetDC", LibraryName);
                GdipReleaseDC_ptr = FunctionWrapper.Load<GdipReleaseDC_delegate>(s_gdipModule, "GdipReleaseDC", LibraryName);
                GdipDrawImageRectI_ptr = FunctionWrapper.Load<GdipDrawImageRectI_delegate>(s_gdipModule, "GdipDrawImageRectI", LibraryName);
                GdipGetRenderingOrigin_ptr = FunctionWrapper.Load<GdipGetRenderingOrigin_delegate>(s_gdipModule, "GdipGetRenderingOrigin", LibraryName);
                GdipSetRenderingOrigin_ptr = FunctionWrapper.Load<GdipSetRenderingOrigin_delegate>(s_gdipModule, "GdipSetRenderingOrigin", LibraryName);
                GdipGraphicsClear_ptr = FunctionWrapper.Load<GdipGraphicsClear_delegate>(s_gdipModule, "GdipGraphicsClear", LibraryName);
                GdipDrawClosedCurve_ptr = FunctionWrapper.Load<GdipDrawClosedCurve_delegate>(s_gdipModule, "GdipDrawClosedCurve", LibraryName);
                GdipDrawClosedCurveI_ptr = FunctionWrapper.Load<GdipDrawClosedCurveI_delegate>(s_gdipModule, "GdipDrawClosedCurveI", LibraryName);
                GdipDrawClosedCurve2_ptr = FunctionWrapper.Load<GdipDrawClosedCurve2_delegate>(s_gdipModule, "GdipDrawClosedCurve2", LibraryName);
                GdipDrawClosedCurve2I_ptr = FunctionWrapper.Load<GdipDrawClosedCurve2I_delegate>(s_gdipModule, "GdipDrawClosedCurve2I", LibraryName);
                GdipDrawCurve_ptr = FunctionWrapper.Load<GdipDrawCurve_delegate>(s_gdipModule, "GdipDrawCurve", LibraryName);
                GdipDrawCurveI_ptr = FunctionWrapper.Load<GdipDrawCurveI_delegate>(s_gdipModule, "GdipDrawCurveI", LibraryName);
                GdipDrawCurve2_ptr = FunctionWrapper.Load<GdipDrawCurve2_delegate>(s_gdipModule, "GdipDrawCurve2", LibraryName);
                GdipDrawCurve2I_ptr = FunctionWrapper.Load<GdipDrawCurve2I_delegate>(s_gdipModule, "GdipDrawCurve2I", LibraryName);
                GdipDrawCurve3_ptr = FunctionWrapper.Load<GdipDrawCurve3_delegate>(s_gdipModule, "GdipDrawCurve3", LibraryName);
                GdipDrawCurve3I_ptr = FunctionWrapper.Load<GdipDrawCurve3I_delegate>(s_gdipModule, "GdipDrawCurve3I", LibraryName);
                GdipFillClosedCurve_ptr = FunctionWrapper.Load<GdipFillClosedCurve_delegate>(s_gdipModule, "GdipFillClosedCurve", LibraryName);
                GdipFillClosedCurveI_ptr = FunctionWrapper.Load<GdipFillClosedCurveI_delegate>(s_gdipModule, "GdipFillClosedCurveI", LibraryName);
                GdipFillClosedCurve2_ptr = FunctionWrapper.Load<GdipFillClosedCurve2_delegate>(s_gdipModule, "GdipFillClosedCurve2", LibraryName);
                GdipFillClosedCurve2I_ptr = FunctionWrapper.Load<GdipFillClosedCurve2I_delegate>(s_gdipModule, "GdipFillClosedCurve2I", LibraryName);
                GdipFillPie_ptr = FunctionWrapper.Load<GdipFillPie_delegate>(s_gdipModule, "GdipFillPie", LibraryName);
                GdipFillPieI_ptr = FunctionWrapper.Load<GdipFillPieI_delegate>(s_gdipModule, "GdipFillPieI", LibraryName);
                GdipFillPath_ptr = FunctionWrapper.Load<GdipFillPath_delegate>(s_gdipModule, "GdipFillPath", LibraryName);
                GdipGetNearestColor_ptr = FunctionWrapper.Load<GdipGetNearestColor_delegate>(s_gdipModule, "GdipGetNearestColor", LibraryName);
                GdipTransformPoints_ptr = FunctionWrapper.Load<GdipTransformPoints_delegate>(s_gdipModule, "GdipTransformPoints", LibraryName);
                GdipTransformPointsI_ptr = FunctionWrapper.Load<GdipTransformPointsI_delegate>(s_gdipModule, "GdipTransformPointsI", LibraryName);
                GdipSetCompositingMode_ptr = FunctionWrapper.Load<GdipSetCompositingMode_delegate>(s_gdipModule, "GdipSetCompositingMode", LibraryName);
                GdipGetCompositingMode_ptr = FunctionWrapper.Load<GdipGetCompositingMode_delegate>(s_gdipModule, "GdipGetCompositingMode", LibraryName);
                GdipSetCompositingQuality_ptr = FunctionWrapper.Load<GdipSetCompositingQuality_delegate>(s_gdipModule, "GdipSetCompositingQuality", LibraryName);
                GdipGetCompositingQuality_ptr = FunctionWrapper.Load<GdipGetCompositingQuality_delegate>(s_gdipModule, "GdipGetCompositingQuality", LibraryName);
                GdipSetInterpolationMode_ptr = FunctionWrapper.Load<GdipSetInterpolationMode_delegate>(s_gdipModule, "GdipSetInterpolationMode", LibraryName);
                GdipGetInterpolationMode_ptr = FunctionWrapper.Load<GdipGetInterpolationMode_delegate>(s_gdipModule, "GdipGetInterpolationMode", LibraryName);
                GdipGetDpiX_ptr = FunctionWrapper.Load<GdipGetDpiX_delegate>(s_gdipModule, "GdipGetDpiX", LibraryName);
                GdipGetDpiY_ptr = FunctionWrapper.Load<GdipGetDpiY_delegate>(s_gdipModule, "GdipGetDpiY", LibraryName);
                GdipGetPageUnit_ptr = FunctionWrapper.Load<GdipGetPageUnit_delegate>(s_gdipModule, "GdipGetPageUnit", LibraryName);
                GdipGetPageScale_ptr = FunctionWrapper.Load<GdipGetPageScale_delegate>(s_gdipModule, "GdipGetPageScale", LibraryName);
                GdipSetPageUnit_ptr = FunctionWrapper.Load<GdipSetPageUnit_delegate>(s_gdipModule, "GdipSetPageUnit", LibraryName);
                GdipSetPageScale_ptr = FunctionWrapper.Load<GdipSetPageScale_delegate>(s_gdipModule, "GdipSetPageScale", LibraryName);
                GdipSetPixelOffsetMode_ptr = FunctionWrapper.Load<GdipSetPixelOffsetMode_delegate>(s_gdipModule, "GdipSetPixelOffsetMode", LibraryName);
                GdipGetPixelOffsetMode_ptr = FunctionWrapper.Load<GdipGetPixelOffsetMode_delegate>(s_gdipModule, "GdipGetPixelOffsetMode", LibraryName);
                GdipSetSmoothingMode_ptr = FunctionWrapper.Load<GdipSetSmoothingMode_delegate>(s_gdipModule, "GdipSetSmoothingMode", LibraryName);
                GdipGetSmoothingMode_ptr = FunctionWrapper.Load<GdipGetSmoothingMode_delegate>(s_gdipModule, "GdipGetSmoothingMode", LibraryName);
                GdipSetTextContrast_ptr = FunctionWrapper.Load<GdipSetTextContrast_delegate>(s_gdipModule, "GdipSetTextContrast", LibraryName);
                GdipGetTextContrast_ptr = FunctionWrapper.Load<GdipGetTextContrast_delegate>(s_gdipModule, "GdipGetTextContrast", LibraryName);
                GdipSetTextRenderingHint_ptr = FunctionWrapper.Load<GdipSetTextRenderingHint_delegate>(s_gdipModule, "GdipSetTextRenderingHint", LibraryName);
                GdipGetTextRenderingHint_ptr = FunctionWrapper.Load<GdipGetTextRenderingHint_delegate>(s_gdipModule, "GdipGetTextRenderingHint", LibraryName);
                GdipFlush_ptr = FunctionWrapper.Load<GdipFlush_delegate>(s_gdipModule, "GdipFlush", LibraryName);
                GdipAddPathString_ptr = FunctionWrapper.Load<GdipAddPathString_delegate>(s_gdipModule, "GdipAddPathString", LibraryName);
                GdipAddPathStringI_ptr = FunctionWrapper.Load<GdipAddPathStringI_delegate>(s_gdipModule, "GdipAddPathStringI", LibraryName);
                GdipCreateFromHWND_ptr = FunctionWrapper.Load<GdipCreateFromHWND_delegate>(s_gdipModule, "GdipCreateFromHWND", LibraryName);
                GdipMeasureString_ptr = FunctionWrapper.Load<GdipMeasureString_delegate>(s_gdipModule, "GdipMeasureString", LibraryName);
                GdipMeasureCharacterRanges_ptr = FunctionWrapper.Load<GdipMeasureCharacterRanges_delegate>(s_gdipModule, "GdipMeasureCharacterRanges", LibraryName);
                GdipLoadImageFromFile_ptr = FunctionWrapper.Load<GdipLoadImageFromFile_delegate>(s_gdipModule, "GdipLoadImageFromFile", LibraryName);
                GdipCloneImage_ptr = FunctionWrapper.Load<GdipCloneImage_delegate>(s_gdipModule, "GdipCloneImage", LibraryName);
                GdipLoadImageFromFileICM_ptr = FunctionWrapper.Load<GdipLoadImageFromFileICM_delegate>(s_gdipModule, "GdipLoadImageFromFileICM", LibraryName);
                GdipCreateBitmapFromHBITMAP_ptr = FunctionWrapper.Load<GdipCreateBitmapFromHBITMAP_delegate>(s_gdipModule, "GdipCreateBitmapFromHBITMAP", LibraryName);
                GdipDisposeImage_ptr = FunctionWrapper.Load<GdipDisposeImage_delegate>(s_gdipModule, "GdipDisposeImage", LibraryName);
                GdipGetImageFlags_ptr = FunctionWrapper.Load<GdipGetImageFlags_delegate>(s_gdipModule, "GdipGetImageFlags", LibraryName);
                GdipGetImageType_ptr = FunctionWrapper.Load<GdipGetImageType_delegate>(s_gdipModule, "GdipGetImageType", LibraryName);
                GdipImageGetFrameDimensionsCount_ptr = FunctionWrapper.Load<GdipImageGetFrameDimensionsCount_delegate>(s_gdipModule, "GdipImageGetFrameDimensionsCount", LibraryName);
                GdipImageGetFrameDimensionsList_ptr = FunctionWrapper.Load<GdipImageGetFrameDimensionsList_delegate>(s_gdipModule, "GdipImageGetFrameDimensionsList", LibraryName);
                GdipGetImageHeight_ptr = FunctionWrapper.Load<GdipGetImageHeight_delegate>(s_gdipModule, "GdipGetImageHeight", LibraryName);
                GdipGetImageHorizontalResolution_ptr = FunctionWrapper.Load<GdipGetImageHorizontalResolution_delegate>(s_gdipModule, "GdipGetImageHorizontalResolution", LibraryName);
                GdipGetImagePaletteSize_ptr = FunctionWrapper.Load<GdipGetImagePaletteSize_delegate>(s_gdipModule, "GdipGetImagePaletteSize", LibraryName);
                GdipGetImagePalette_ptr = FunctionWrapper.Load<GdipGetImagePalette_delegate>(s_gdipModule, "GdipGetImagePalette", LibraryName);
                GdipSetImagePalette_ptr = FunctionWrapper.Load<GdipSetImagePalette_delegate>(s_gdipModule, "GdipSetImagePalette", LibraryName);
                GdipGetImageDimension_ptr = FunctionWrapper.Load<GdipGetImageDimension_delegate>(s_gdipModule, "GdipGetImageDimension", LibraryName);
                GdipGetImagePixelFormat_ptr = FunctionWrapper.Load<GdipGetImagePixelFormat_delegate>(s_gdipModule, "GdipGetImagePixelFormat", LibraryName);
                GdipGetPropertyCount_ptr = FunctionWrapper.Load<GdipGetPropertyCount_delegate>(s_gdipModule, "GdipGetPropertyCount", LibraryName);
                GdipGetPropertyIdList_ptr = FunctionWrapper.Load<GdipGetPropertyIdList_delegate>(s_gdipModule, "GdipGetPropertyIdList", LibraryName);
                GdipGetPropertySize_ptr = FunctionWrapper.Load<GdipGetPropertySize_delegate>(s_gdipModule, "GdipGetPropertySize", LibraryName);
                GdipGetAllPropertyItems_ptr = FunctionWrapper.Load<GdipGetAllPropertyItems_delegate>(s_gdipModule, "GdipGetAllPropertyItems", LibraryName);
                GdipGetImageRawFormat_ptr = FunctionWrapper.Load<GdipGetImageRawFormat_delegate>(s_gdipModule, "GdipGetImageRawFormat", LibraryName);
                GdipGetImageVerticalResolution_ptr = FunctionWrapper.Load<GdipGetImageVerticalResolution_delegate>(s_gdipModule, "GdipGetImageVerticalResolution", LibraryName);
                GdipGetImageWidth_ptr = FunctionWrapper.Load<GdipGetImageWidth_delegate>(s_gdipModule, "GdipGetImageWidth", LibraryName);
                GdipGetImageBounds_ptr = FunctionWrapper.Load<GdipGetImageBounds_delegate>(s_gdipModule, "GdipGetImageBounds", LibraryName);
                GdipGetEncoderParameterListSize_ptr = FunctionWrapper.Load<GdipGetEncoderParameterListSize_delegate>(s_gdipModule, "GdipGetEncoderParameterListSize", LibraryName);
                GdipGetEncoderParameterList_ptr = FunctionWrapper.Load<GdipGetEncoderParameterList_delegate>(s_gdipModule, "GdipGetEncoderParameterList", LibraryName);
                GdipImageGetFrameCount_ptr = FunctionWrapper.Load<GdipImageGetFrameCount_delegate>(s_gdipModule, "GdipImageGetFrameCount", LibraryName);
                GdipImageSelectActiveFrame_ptr = FunctionWrapper.Load<GdipImageSelectActiveFrame_delegate>(s_gdipModule, "GdipImageSelectActiveFrame", LibraryName);
                GdipGetPropertyItemSize_ptr = FunctionWrapper.Load<GdipGetPropertyItemSize_delegate>(s_gdipModule, "GdipGetPropertyItemSize", LibraryName);
                GdipGetPropertyItem_ptr = FunctionWrapper.Load<GdipGetPropertyItem_delegate>(s_gdipModule, "GdipGetPropertyItem", LibraryName);
                GdipRemovePropertyItem_ptr = FunctionWrapper.Load<GdipRemovePropertyItem_delegate>(s_gdipModule, "GdipRemovePropertyItem", LibraryName);
                GdipSetPropertyItem_ptr = FunctionWrapper.Load<GdipSetPropertyItem_delegate>(s_gdipModule, "GdipSetPropertyItem", LibraryName);
                GdipGetImageThumbnail_ptr = FunctionWrapper.Load<GdipGetImageThumbnail_delegate>(s_gdipModule, "GdipGetImageThumbnail", LibraryName);
                GdipImageRotateFlip_ptr = FunctionWrapper.Load<GdipImageRotateFlip_delegate>(s_gdipModule, "GdipImageRotateFlip", LibraryName);
                GdipSaveImageToFile_ptr = FunctionWrapper.Load<GdipSaveImageToFile_delegate>(s_gdipModule, "GdipSaveImageToFile", LibraryName);
                GdipSaveAdd_ptr = FunctionWrapper.Load<GdipSaveAdd_delegate>(s_gdipModule, "GdipSaveAdd", LibraryName);
                GdipSaveAddImage_ptr = FunctionWrapper.Load<GdipSaveAddImage_delegate>(s_gdipModule, "GdipSaveAddImage", LibraryName);
                GdipDrawImageI_ptr = FunctionWrapper.Load<GdipDrawImageI_delegate>(s_gdipModule, "GdipDrawImageI", LibraryName);
                GdipGetImageGraphicsContext_ptr = FunctionWrapper.Load<GdipGetImageGraphicsContext_delegate>(s_gdipModule, "GdipGetImageGraphicsContext", LibraryName);
                GdipDrawImage_ptr = FunctionWrapper.Load<GdipDrawImage_delegate>(s_gdipModule, "GdipDrawImage", LibraryName);
                GdipDrawImagePoints_ptr = FunctionWrapper.Load<GdipDrawImagePoints_delegate>(s_gdipModule, "GdipDrawImagePoints", LibraryName);
                GdipDrawImagePointsI_ptr = FunctionWrapper.Load<GdipDrawImagePointsI_delegate>(s_gdipModule, "GdipDrawImagePointsI", LibraryName);
                GdipDrawImageRectRectI_ptr = FunctionWrapper.Load<GdipDrawImageRectRectI_delegate>(s_gdipModule, "GdipDrawImageRectRectI", LibraryName);
                GdipDrawImageRectRect_ptr = FunctionWrapper.Load<GdipDrawImageRectRect_delegate>(s_gdipModule, "GdipDrawImageRectRect", LibraryName);
                GdipDrawImagePointsRectI_ptr = FunctionWrapper.Load<GdipDrawImagePointsRectI_delegate>(s_gdipModule, "GdipDrawImagePointsRectI", LibraryName);
                GdipDrawImagePointsRect_ptr = FunctionWrapper.Load<GdipDrawImagePointsRect_delegate>(s_gdipModule, "GdipDrawImagePointsRect", LibraryName);
                GdipDrawImageRect_ptr = FunctionWrapper.Load<GdipDrawImageRect_delegate>(s_gdipModule, "GdipDrawImageRect", LibraryName);
                GdipDrawImagePointRect_ptr = FunctionWrapper.Load<GdipDrawImagePointRect_delegate>(s_gdipModule, "GdipDrawImagePointRect", LibraryName);
                GdipDrawImagePointRectI_ptr = FunctionWrapper.Load<GdipDrawImagePointRectI_delegate>(s_gdipModule, "GdipDrawImagePointRectI", LibraryName);
                GdipCreatePath_ptr = FunctionWrapper.Load<GdipCreatePath_delegate>(s_gdipModule, "GdipCreatePath", LibraryName);
                GdipCreatePath2_ptr = FunctionWrapper.Load<GdipCreatePath2_delegate>(s_gdipModule, "GdipCreatePath2", LibraryName);
                GdipCreatePath2I_ptr = FunctionWrapper.Load<GdipCreatePath2I_delegate>(s_gdipModule, "GdipCreatePath2I", LibraryName);
                GdipClonePath_ptr = FunctionWrapper.Load<GdipClonePath_delegate>(s_gdipModule, "GdipClonePath", LibraryName);
                GdipDeletePath_ptr = FunctionWrapper.Load<GdipDeletePath_delegate>(s_gdipModule, "GdipDeletePath", LibraryName);
                GdipResetPath_ptr = FunctionWrapper.Load<GdipResetPath_delegate>(s_gdipModule, "GdipResetPath", LibraryName);
                GdipGetPointCount_ptr = FunctionWrapper.Load<GdipGetPointCount_delegate>(s_gdipModule, "GdipGetPointCount", LibraryName);
                GdipGetPathTypes_ptr = FunctionWrapper.Load<GdipGetPathTypes_delegate>(s_gdipModule, "GdipGetPathTypes", LibraryName);
                GdipGetPathPoints_ptr = FunctionWrapper.Load<GdipGetPathPoints_delegate>(s_gdipModule, "GdipGetPathPoints", LibraryName);
                GdipGetPathPointsI_ptr = FunctionWrapper.Load<GdipGetPathPointsI_delegate>(s_gdipModule, "GdipGetPathPointsI", LibraryName);
                GdipGetPathFillMode_ptr = FunctionWrapper.Load<GdipGetPathFillMode_delegate>(s_gdipModule, "GdipGetPathFillMode", LibraryName);
                GdipSetPathFillMode_ptr = FunctionWrapper.Load<GdipSetPathFillMode_delegate>(s_gdipModule, "GdipSetPathFillMode", LibraryName);
                GdipStartPathFigure_ptr = FunctionWrapper.Load<GdipStartPathFigure_delegate>(s_gdipModule, "GdipStartPathFigure", LibraryName);
                GdipClosePathFigure_ptr = FunctionWrapper.Load<GdipClosePathFigure_delegate>(s_gdipModule, "GdipClosePathFigure", LibraryName);
                GdipClosePathFigures_ptr = FunctionWrapper.Load<GdipClosePathFigures_delegate>(s_gdipModule, "GdipClosePathFigures", LibraryName);
                GdipSetPathMarker_ptr = FunctionWrapper.Load<GdipSetPathMarker_delegate>(s_gdipModule, "GdipSetPathMarker", LibraryName);
                GdipClearPathMarkers_ptr = FunctionWrapper.Load<GdipClearPathMarkers_delegate>(s_gdipModule, "GdipClearPathMarkers", LibraryName);
                GdipReversePath_ptr = FunctionWrapper.Load<GdipReversePath_delegate>(s_gdipModule, "GdipReversePath", LibraryName);
                GdipGetPathLastPoint_ptr = FunctionWrapper.Load<GdipGetPathLastPoint_delegate>(s_gdipModule, "GdipGetPathLastPoint", LibraryName);
                GdipAddPathLine_ptr = FunctionWrapper.Load<GdipAddPathLine_delegate>(s_gdipModule, "GdipAddPathLine", LibraryName);
                GdipAddPathLine2_ptr = FunctionWrapper.Load<GdipAddPathLine2_delegate>(s_gdipModule, "GdipAddPathLine2", LibraryName);
                GdipAddPathLine2I_ptr = FunctionWrapper.Load<GdipAddPathLine2I_delegate>(s_gdipModule, "GdipAddPathLine2I", LibraryName);
                GdipAddPathArc_ptr = FunctionWrapper.Load<GdipAddPathArc_delegate>(s_gdipModule, "GdipAddPathArc", LibraryName);
                GdipAddPathBezier_ptr = FunctionWrapper.Load<GdipAddPathBezier_delegate>(s_gdipModule, "GdipAddPathBezier", LibraryName);
                GdipAddPathBeziers_ptr = FunctionWrapper.Load<GdipAddPathBeziers_delegate>(s_gdipModule, "GdipAddPathBeziers", LibraryName);
                GdipAddPathCurve_ptr = FunctionWrapper.Load<GdipAddPathCurve_delegate>(s_gdipModule, "GdipAddPathCurve", LibraryName);
                GdipAddPathCurveI_ptr = FunctionWrapper.Load<GdipAddPathCurveI_delegate>(s_gdipModule, "GdipAddPathCurveI", LibraryName);
                GdipAddPathCurve2_ptr = FunctionWrapper.Load<GdipAddPathCurve2_delegate>(s_gdipModule, "GdipAddPathCurve2", LibraryName);
                GdipAddPathCurve2I_ptr = FunctionWrapper.Load<GdipAddPathCurve2I_delegate>(s_gdipModule, "GdipAddPathCurve2I", LibraryName);
                GdipAddPathCurve3_ptr = FunctionWrapper.Load<GdipAddPathCurve3_delegate>(s_gdipModule, "GdipAddPathCurve3", LibraryName);
                GdipAddPathCurve3I_ptr = FunctionWrapper.Load<GdipAddPathCurve3I_delegate>(s_gdipModule, "GdipAddPathCurve3I", LibraryName);
                GdipAddPathClosedCurve_ptr = FunctionWrapper.Load<GdipAddPathClosedCurve_delegate>(s_gdipModule, "GdipAddPathClosedCurve", LibraryName);
                GdipAddPathClosedCurveI_ptr = FunctionWrapper.Load<GdipAddPathClosedCurveI_delegate>(s_gdipModule, "GdipAddPathClosedCurveI", LibraryName);
                GdipAddPathClosedCurve2_ptr = FunctionWrapper.Load<GdipAddPathClosedCurve2_delegate>(s_gdipModule, "GdipAddPathClosedCurve2", LibraryName);
                GdipAddPathClosedCurve2I_ptr = FunctionWrapper.Load<GdipAddPathClosedCurve2I_delegate>(s_gdipModule, "GdipAddPathClosedCurve2I", LibraryName);
                GdipAddPathRectangle_ptr = FunctionWrapper.Load<GdipAddPathRectangle_delegate>(s_gdipModule, "GdipAddPathRectangle", LibraryName);
                GdipAddPathRectangles_ptr = FunctionWrapper.Load<GdipAddPathRectangles_delegate>(s_gdipModule, "GdipAddPathRectangles", LibraryName);
                GdipAddPathEllipse_ptr = FunctionWrapper.Load<GdipAddPathEllipse_delegate>(s_gdipModule, "GdipAddPathEllipse", LibraryName);
                GdipAddPathEllipseI_ptr = FunctionWrapper.Load<GdipAddPathEllipseI_delegate>(s_gdipModule, "GdipAddPathEllipseI", LibraryName);
                GdipAddPathPie_ptr = FunctionWrapper.Load<GdipAddPathPie_delegate>(s_gdipModule, "GdipAddPathPie", LibraryName);
                GdipAddPathPieI_ptr = FunctionWrapper.Load<GdipAddPathPieI_delegate>(s_gdipModule, "GdipAddPathPieI", LibraryName);
                GdipAddPathPolygon_ptr = FunctionWrapper.Load<GdipAddPathPolygon_delegate>(s_gdipModule, "GdipAddPathPolygon", LibraryName);
                GdipAddPathPath_ptr = FunctionWrapper.Load<GdipAddPathPath_delegate>(s_gdipModule, "GdipAddPathPath", LibraryName);
                GdipAddPathLineI_ptr = FunctionWrapper.Load<GdipAddPathLineI_delegate>(s_gdipModule, "GdipAddPathLineI", LibraryName);
                GdipAddPathArcI_ptr = FunctionWrapper.Load<GdipAddPathArcI_delegate>(s_gdipModule, "GdipAddPathArcI", LibraryName);
                GdipAddPathBezierI_ptr = FunctionWrapper.Load<GdipAddPathBezierI_delegate>(s_gdipModule, "GdipAddPathBezierI", LibraryName);
                GdipAddPathBeziersI_ptr = FunctionWrapper.Load<GdipAddPathBeziersI_delegate>(s_gdipModule, "GdipAddPathBeziersI", LibraryName);
                GdipAddPathPolygonI_ptr = FunctionWrapper.Load<GdipAddPathPolygonI_delegate>(s_gdipModule, "GdipAddPathPolygonI", LibraryName);
                GdipAddPathRectangleI_ptr = FunctionWrapper.Load<GdipAddPathRectangleI_delegate>(s_gdipModule, "GdipAddPathRectangleI", LibraryName);
                GdipAddPathRectanglesI_ptr = FunctionWrapper.Load<GdipAddPathRectanglesI_delegate>(s_gdipModule, "GdipAddPathRectanglesI", LibraryName);
                GdipFlattenPath_ptr = FunctionWrapper.Load<GdipFlattenPath_delegate>(s_gdipModule, "GdipFlattenPath", LibraryName);
                GdipTransformPath_ptr = FunctionWrapper.Load<GdipTransformPath_delegate>(s_gdipModule, "GdipTransformPath", LibraryName);
                GdipWarpPath_ptr = FunctionWrapper.Load<GdipWarpPath_delegate>(s_gdipModule, "GdipWarpPath", LibraryName);
                GdipWidenPath_ptr = FunctionWrapper.Load<GdipWidenPath_delegate>(s_gdipModule, "GdipWidenPath", LibraryName);
                GdipGetPathWorldBounds_ptr = FunctionWrapper.Load<GdipGetPathWorldBounds_delegate>(s_gdipModule, "GdipGetPathWorldBounds", LibraryName);
                GdipGetPathWorldBoundsI_ptr = FunctionWrapper.Load<GdipGetPathWorldBoundsI_delegate>(s_gdipModule, "GdipGetPathWorldBoundsI", LibraryName);
                GdipIsVisiblePathPoint_ptr = FunctionWrapper.Load<GdipIsVisiblePathPoint_delegate>(s_gdipModule, "GdipIsVisiblePathPoint", LibraryName);
                GdipIsVisiblePathPointI_ptr = FunctionWrapper.Load<GdipIsVisiblePathPointI_delegate>(s_gdipModule, "GdipIsVisiblePathPointI", LibraryName);
                GdipIsOutlineVisiblePathPoint_ptr = FunctionWrapper.Load<GdipIsOutlineVisiblePathPoint_delegate>(s_gdipModule, "GdipIsOutlineVisiblePathPoint", LibraryName);
                GdipIsOutlineVisiblePathPointI_ptr = FunctionWrapper.Load<GdipIsOutlineVisiblePathPointI_delegate>(s_gdipModule, "GdipIsOutlineVisiblePathPointI", LibraryName);
                GdipCreateFont_ptr = FunctionWrapper.Load<GdipCreateFont_delegate>(s_gdipModule, "GdipCreateFont", LibraryName);
                GdipDeleteFont_ptr = FunctionWrapper.Load<GdipDeleteFont_delegate>(s_gdipModule, "GdipDeleteFont", LibraryName);
                GdipGetLogFont_ptr = FunctionWrapper.Load<GdipGetLogFont_delegate>(s_gdipModule, "GdipGetLogFontW", LibraryName);
                GdipCreateFontFromDC_ptr = FunctionWrapper.Load<GdipCreateFontFromDC_delegate>(s_gdipModule, "GdipCreateFontFromDC", LibraryName);
                GdipCreateFontFromLogfont_ptr = FunctionWrapper.Load<GdipCreateFontFromLogfont_delegate>(s_gdipModule, "GdipCreateFontFromLogfontW", LibraryName);
                GdipCreateFontFromHfont_ptr = FunctionWrapper.Load<GdipCreateFontFromHfont_delegate>(s_gdipModule, "GdipCreateFontFromHfontA", LibraryName);
                GdipGetFontSize_ptr = FunctionWrapper.Load<GdipGetFontSize_delegate>(s_gdipModule, "GdipGetFontSize", LibraryName);
                GdipGetFontHeight_ptr = FunctionWrapper.Load<GdipGetFontHeight_delegate>(s_gdipModule, "GdipGetFontHeight", LibraryName);
                GdipGetFontHeightGivenDPI_ptr = FunctionWrapper.Load<GdipGetFontHeightGivenDPI_delegate>(s_gdipModule, "GdipGetFontHeightGivenDPI", LibraryName);
                GdipCreateMetafileFromFile_ptr = FunctionWrapper.Load<GdipCreateMetafileFromFile_delegate>(s_gdipModule, "GdipCreateMetafileFromFile", LibraryName);
                GdipCreateMetafileFromEmf_ptr = FunctionWrapper.Load<GdipCreateMetafileFromEmf_delegate>(s_gdipModule, "GdipCreateMetafileFromEmf", LibraryName);
                GdipCreateMetafileFromWmf_ptr = FunctionWrapper.Load<GdipCreateMetafileFromWmf_delegate>(s_gdipModule, "GdipCreateMetafileFromWmf", LibraryName);
                GdipGetMetafileHeaderFromFile_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromFile_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromFile", LibraryName);
                GdipGetMetafileHeaderFromMetafile_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromMetafile_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromMetafile", LibraryName);
                GdipGetMetafileHeaderFromEmf_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromEmf_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromEmf", LibraryName);
                GdipGetMetafileHeaderFromWmf_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromWmf_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromWmf", LibraryName);
                GdipGetHemfFromMetafile_ptr = FunctionWrapper.Load<GdipGetHemfFromMetafile_delegate>(s_gdipModule, "GdipGetHemfFromMetafile", LibraryName);
                GdipGetMetafileDownLevelRasterizationLimit_ptr = FunctionWrapper.Load<GdipGetMetafileDownLevelRasterizationLimit_delegate>(s_gdipModule, "GdipGetMetafileDownLevelRasterizationLimit", LibraryName);
                GdipSetMetafileDownLevelRasterizationLimit_ptr = FunctionWrapper.Load<GdipSetMetafileDownLevelRasterizationLimit_delegate>(s_gdipModule, "GdipSetMetafileDownLevelRasterizationLimit", LibraryName);
                GdipPlayMetafileRecord_ptr = FunctionWrapper.Load<GdipPlayMetafileRecord_delegate>(s_gdipModule, "GdipPlayMetafileRecord", LibraryName);
                GdipRecordMetafile_ptr = FunctionWrapper.Load<GdipRecordMetafile_delegate>(s_gdipModule, "GdipRecordMetafile", LibraryName);
                GdipRecordMetafileI_ptr = FunctionWrapper.Load<GdipRecordMetafileI_delegate>(s_gdipModule, "GdipRecordMetafileI", LibraryName);
                GdipRecordMetafileFileName_ptr = FunctionWrapper.Load<GdipRecordMetafileFileName_delegate>(s_gdipModule, "GdipRecordMetafileFileName", LibraryName);
                GdipRecordMetafileFileNameI_ptr = FunctionWrapper.Load<GdipRecordMetafileFileNameI_delegate>(s_gdipModule, "GdipRecordMetafileFileNameI", LibraryName);
                GdipCreateFromContext_macosx_ptr = FunctionWrapper.Load<GdipCreateFromContext_macosx_delegate>(s_gdipModule, "GdipCreateFromContext_macosx", LibraryName);
                GdipSetVisibleClip_linux_ptr = FunctionWrapper.Load<GdipSetVisibleClip_linux_delegate>(s_gdipModule, "GdipSetVisibleClip_linux", LibraryName);
                GdipCreateFromXDrawable_linux_ptr = FunctionWrapper.Load<GdipCreateFromXDrawable_linux_delegate>(s_gdipModule, "GdipCreateFromXDrawable_linux", LibraryName);
                GdipLoadImageFromDelegate_linux_ptr = FunctionWrapper.Load<GdipLoadImageFromDelegate_linux_delegate>(s_gdipModule, "GdipLoadImageFromDelegate_linux", LibraryName);
                GdipSaveImageToDelegate_linux_ptr = FunctionWrapper.Load<GdipSaveImageToDelegate_linux_delegate>(s_gdipModule, "GdipSaveImageToDelegate_linux", LibraryName);
                GdipCreateMetafileFromDelegate_linux_ptr = FunctionWrapper.Load<GdipCreateMetafileFromDelegate_linux_delegate>(s_gdipModule, "GdipCreateMetafileFromDelegate_linux", LibraryName);
                GdipGetMetafileHeaderFromDelegate_linux_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromDelegate_linux_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromDelegate_linux", LibraryName);
                GdipRecordMetafileFromDelegate_linux_ptr = FunctionWrapper.Load<GdipRecordMetafileFromDelegate_linux_delegate>(s_gdipModule, "GdipRecordMetafileFromDelegate_linux", LibraryName);
                GdipRecordMetafileFromDelegateI_linux_ptr = FunctionWrapper.Load<GdipRecordMetafileFromDelegateI_linux_delegate>(s_gdipModule, "GdipRecordMetafileFromDelegateI_linux", LibraryName);
                GdipGetPostScriptSavePage_ptr = FunctionWrapper.Load<GdipGetPostScriptSavePage_delegate>(s_gdipModule, "GdipGetPostScriptSavePage", LibraryName);
                GdipGetPostScriptGraphicsContext_ptr = FunctionWrapper.Load<GdipGetPostScriptGraphicsContext_delegate>(s_gdipModule, "GdipGetPostScriptGraphicsContext", LibraryName);
            }

            // Imported functions

            private delegate int GdiplusStartup_delegate(out IntPtr token, ref StartupInput input, out StartupOutput output);
            private static FunctionWrapper<GdiplusStartup_delegate> GdiplusStartup_ptr;
            internal static int GdiplusStartup(out IntPtr token, ref StartupInput input, out StartupOutput output) => (int)GdiplusStartup_ptr.Delegate(out token, ref input, out output);

            private delegate void GdiplusShutdown_delegate(ref ulong token);
            private static FunctionWrapper<GdiplusShutdown_delegate> GdiplusShutdown_ptr;
            internal static void GdiplusShutdown(ref ulong token) => GdiplusShutdown_ptr.Delegate(ref token);

            private delegate IntPtr GdipAlloc_delegate(int size);
            private static FunctionWrapper<GdipAlloc_delegate> GdipAlloc_ptr;
            internal static IntPtr GdipAlloc(int size) => GdipAlloc_ptr.Delegate(size);

            private delegate void GdipFree_delegate(IntPtr ptr);
            private static FunctionWrapper<GdipFree_delegate> GdipFree_ptr;
            internal static void GdipFree(IntPtr ptr) => GdipFree_ptr.Delegate(ptr);

            private delegate int GdipDeleteBrush_delegate(IntPtr brush);
            private static FunctionWrapper<GdipDeleteBrush_delegate> GdipDeleteBrush_ptr;
            internal static int GdipDeleteBrush(IntPtr brush) => GdipDeleteBrush_ptr.Delegate(brush);
            internal static int IntGdipDeleteBrush(HandleRef brush) => (int)GdipDeleteBrush_ptr.Delegate(brush.Handle);

            private delegate int GdipGetBrushType_delegate(IntPtr brush, out BrushType type);
            private static FunctionWrapper<GdipGetBrushType_delegate> GdipGetBrushType_ptr;
            internal static int GdipGetBrushType(IntPtr brush, out BrushType type) => GdipGetBrushType_ptr.Delegate(brush, out type);

            private delegate int GdipCreateFromHDC_delegate(IntPtr hDC, out IntPtr graphics);
            private static FunctionWrapper<GdipCreateFromHDC_delegate> GdipCreateFromHDC_ptr;
            internal static int GdipCreateFromHDC(IntPtr hDC, out IntPtr graphics) => GdipCreateFromHDC_ptr.Delegate(hDC, out graphics);

            private delegate int GdipDeleteGraphics_delegate(IntPtr graphics);
            private static FunctionWrapper<GdipDeleteGraphics_delegate> GdipDeleteGraphics_ptr;
            internal static int GdipDeleteGraphics(IntPtr graphics) => GdipDeleteGraphics_ptr.Delegate(graphics);
            internal static int IntGdipDeleteGraphics(HandleRef graphics) => (int)GdipDeleteGraphics_ptr.Delegate(graphics.Handle);

            private delegate int GdipRestoreGraphics_delegate(IntPtr graphics, uint graphicsState);
            private static FunctionWrapper<GdipRestoreGraphics_delegate> GdipRestoreGraphics_ptr;
            internal static int GdipRestoreGraphics(IntPtr graphics, uint graphicsState) => GdipRestoreGraphics_ptr.Delegate(graphics, graphicsState);

            private delegate int GdipSaveGraphics_delegate(IntPtr graphics, out uint state);
            private static FunctionWrapper<GdipSaveGraphics_delegate> GdipSaveGraphics_ptr;
            internal static int GdipSaveGraphics(IntPtr graphics, out uint state) => GdipSaveGraphics_ptr.Delegate(graphics, out state);

            private delegate int GdipDrawArc_delegate(IntPtr graphics, IntPtr pen, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawArc_delegate> GdipDrawArc_ptr;
            internal static int GdipDrawArc(IntPtr graphics, IntPtr pen, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipDrawArc_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipDrawArcI_delegate(IntPtr graphics, IntPtr pen, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawArcI_delegate> GdipDrawArcI_ptr;
            internal static int GdipDrawArcI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipDrawArcI_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipDrawBezier_delegate(IntPtr graphics, IntPtr pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);
            private static FunctionWrapper<GdipDrawBezier_delegate> GdipDrawBezier_ptr;
            internal static int GdipDrawBezier(IntPtr graphics, IntPtr pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) => GdipDrawBezier_ptr.Delegate(graphics, pen, x1, y1, x2, y2, x3, y3, x4, y4);

            private delegate int GdipDrawBezierI_delegate(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);
            private static FunctionWrapper<GdipDrawBezierI_delegate> GdipDrawBezierI_ptr;
            internal static int GdipDrawBezierI(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4) => GdipDrawBezierI_ptr.Delegate(graphics, pen, x1, y1, x2, y2, x3, y3, x4, y4);

            private delegate int GdipDrawEllipseI_delegate(IntPtr graphics, IntPtr pen, int x, int y, int width, int height);
            private static FunctionWrapper<GdipDrawEllipseI_delegate> GdipDrawEllipseI_ptr;
            internal static int GdipDrawEllipseI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height) => GdipDrawEllipseI_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate int GdipDrawEllipse_delegate(IntPtr graphics, IntPtr pen, float x, float y, float width, float height);
            private static FunctionWrapper<GdipDrawEllipse_delegate> GdipDrawEllipse_ptr;
            internal static int GdipDrawEllipse(IntPtr graphics, IntPtr pen, float x, float y, float width, float height) => GdipDrawEllipse_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate int GdipDrawLine_delegate(IntPtr graphics, IntPtr pen, float x1, float y1, float x2, float y2);
            private static FunctionWrapper<GdipDrawLine_delegate> GdipDrawLine_ptr;
            internal static int GdipDrawLine(IntPtr graphics, IntPtr pen, float x1, float y1, float x2, float y2) => GdipDrawLine_ptr.Delegate(graphics, pen, x1, y1, x2, y2);

            private delegate int GdipDrawLineI_delegate(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2);
            private static FunctionWrapper<GdipDrawLineI_delegate> GdipDrawLineI_ptr;
            internal static int GdipDrawLineI(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2) => GdipDrawLineI_ptr.Delegate(graphics, pen, x1, y1, x2, y2);

            private delegate int GdipDrawLines_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count);
            private static FunctionWrapper<GdipDrawLines_delegate> GdipDrawLines_ptr;
            internal static int GdipDrawLines(IntPtr graphics, IntPtr pen, PointF[] points, int count) => GdipDrawLines_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawLinesI_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count);
            private static FunctionWrapper<GdipDrawLinesI_delegate> GdipDrawLinesI_ptr;
            internal static int GdipDrawLinesI(IntPtr graphics, IntPtr pen, Point[] points, int count) => GdipDrawLinesI_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawPath_delegate(IntPtr graphics, IntPtr pen, IntPtr path);
            private static FunctionWrapper<GdipDrawPath_delegate> GdipDrawPath_ptr;
            internal static int GdipDrawPath(IntPtr graphics, IntPtr pen, IntPtr path) => GdipDrawPath_ptr.Delegate(graphics, pen, path);

            private delegate int GdipDrawPie_delegate(IntPtr graphics, IntPtr pen, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawPie_delegate> GdipDrawPie_ptr;
            internal static int GdipDrawPie(IntPtr graphics, IntPtr pen, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipDrawPie_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipDrawPieI_delegate(IntPtr graphics, IntPtr pen, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawPieI_delegate> GdipDrawPieI_ptr;
            internal static int GdipDrawPieI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipDrawPieI_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipDrawPolygon_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count);
            private static FunctionWrapper<GdipDrawPolygon_delegate> GdipDrawPolygon_ptr;
            internal static int GdipDrawPolygon(IntPtr graphics, IntPtr pen, PointF[] points, int count) => GdipDrawPolygon_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawPolygonI_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count);
            private static FunctionWrapper<GdipDrawPolygonI_delegate> GdipDrawPolygonI_ptr;
            internal static int GdipDrawPolygonI(IntPtr graphics, IntPtr pen, Point[] points, int count) => GdipDrawPolygonI_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawRectangle_delegate(IntPtr graphics, IntPtr pen, float x, float y, float width, float height);
            private static FunctionWrapper<GdipDrawRectangle_delegate> GdipDrawRectangle_ptr;
            internal static int GdipDrawRectangle(IntPtr graphics, IntPtr pen, float x, float y, float width, float height) => GdipDrawRectangle_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate int GdipDrawRectangleI_delegate(IntPtr graphics, IntPtr pen, int x, int y, int width, int height);
            private static FunctionWrapper<GdipDrawRectangleI_delegate> GdipDrawRectangleI_ptr;
            internal static int GdipDrawRectangleI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height) => GdipDrawRectangleI_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate int GdipDrawRectangles_delegate(IntPtr graphics, IntPtr pen, RectangleF[] rects, int count);
            private static FunctionWrapper<GdipDrawRectangles_delegate> GdipDrawRectangles_ptr;
            internal static int GdipDrawRectangles(IntPtr graphics, IntPtr pen, RectangleF[] rects, int count) => GdipDrawRectangles_ptr.Delegate(graphics, pen, rects, count);

            private delegate int GdipDrawRectanglesI_delegate(IntPtr graphics, IntPtr pen, Rectangle[] rects, int count);
            private static FunctionWrapper<GdipDrawRectanglesI_delegate> GdipDrawRectanglesI_ptr;
            internal static int GdipDrawRectanglesI(IntPtr graphics, IntPtr pen, Rectangle[] rects, int count) => GdipDrawRectanglesI_ptr.Delegate(graphics, pen, rects, count);

            private delegate int GdipFillEllipseI_delegate(IntPtr graphics, IntPtr pen, int x, int y, int width, int height);
            private static FunctionWrapper<GdipFillEllipseI_delegate> GdipFillEllipseI_ptr;
            internal static int GdipFillEllipseI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height) => GdipFillEllipseI_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate int GdipFillEllipse_delegate(IntPtr graphics, IntPtr pen, float x, float y, float width, float height);
            private static FunctionWrapper<GdipFillEllipse_delegate> GdipFillEllipse_ptr;
            internal static int GdipFillEllipse(IntPtr graphics, IntPtr pen, float x, float y, float width, float height) => GdipFillEllipse_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate int GdipFillPolygon_delegate(IntPtr graphics, IntPtr brush, PointF[] points, int count, FillMode fillMode);
            private static FunctionWrapper<GdipFillPolygon_delegate> GdipFillPolygon_ptr;
            internal static int GdipFillPolygon(IntPtr graphics, IntPtr brush, PointF[] points, int count, FillMode fillMode) => GdipFillPolygon_ptr.Delegate(graphics, brush, points, count, fillMode);

            private delegate int GdipFillPolygonI_delegate(IntPtr graphics, IntPtr brush, Point[] points, int count, FillMode fillMode);
            private static FunctionWrapper<GdipFillPolygonI_delegate> GdipFillPolygonI_ptr;
            internal static int GdipFillPolygonI(IntPtr graphics, IntPtr brush, Point[] points, int count, FillMode fillMode) => GdipFillPolygonI_ptr.Delegate(graphics, brush, points, count, fillMode);

            private delegate int GdipFillPolygon2_delegate(IntPtr graphics, IntPtr brush, PointF[] points, int count);
            private static FunctionWrapper<GdipFillPolygon2_delegate> GdipFillPolygon2_ptr;
            internal static int GdipFillPolygon2(IntPtr graphics, IntPtr brush, PointF[] points, int count) => GdipFillPolygon2_ptr.Delegate(graphics, brush, points, count);

            private delegate int GdipFillPolygon2I_delegate(IntPtr graphics, IntPtr brush, Point[] points, int count);
            private static FunctionWrapper<GdipFillPolygon2I_delegate> GdipFillPolygon2I_ptr;
            internal static int GdipFillPolygon2I(IntPtr graphics, IntPtr brush, Point[] points, int count) => GdipFillPolygon2I_ptr.Delegate(graphics, brush, points, count);

            private delegate int GdipFillRectangle_delegate(IntPtr graphics, IntPtr brush, float x1, float y1, float x2, float y2);
            private static FunctionWrapper<GdipFillRectangle_delegate> GdipFillRectangle_ptr;
            internal static int GdipFillRectangle(IntPtr graphics, IntPtr brush, float x1, float y1, float x2, float y2) => GdipFillRectangle_ptr.Delegate(graphics, brush, x1, y1, x2, y2);

            private delegate int GdipFillRectangleI_delegate(IntPtr graphics, IntPtr brush, int x1, int y1, int x2, int y2);
            private static FunctionWrapper<GdipFillRectangleI_delegate> GdipFillRectangleI_ptr;
            internal static int GdipFillRectangleI(IntPtr graphics, IntPtr brush, int x1, int y1, int x2, int y2) => GdipFillRectangleI_ptr.Delegate(graphics, brush, x1, y1, x2, y2);

            private delegate int GdipFillRectangles_delegate(IntPtr graphics, IntPtr brush, RectangleF[] rects, int count);
            private static FunctionWrapper<GdipFillRectangles_delegate> GdipFillRectangles_ptr;
            internal static int GdipFillRectangles(IntPtr graphics, IntPtr brush, RectangleF[] rects, int count) => GdipFillRectangles_ptr.Delegate(graphics, brush, rects, count);

            private delegate int GdipFillRectanglesI_delegate(IntPtr graphics, IntPtr brush, Rectangle[] rects, int count);
            private static FunctionWrapper<GdipFillRectanglesI_delegate> GdipFillRectanglesI_ptr;
            internal static int GdipFillRectanglesI(IntPtr graphics, IntPtr brush, Rectangle[] rects, int count) => GdipFillRectanglesI_ptr.Delegate(graphics, brush, rects, count);

            private delegate int GdipDrawString_delegate(IntPtr graphics, [MarshalAs(UnmanagedType.LPWStr)]string text, int len, IntPtr font, ref RectangleF rc, IntPtr format, IntPtr brush);
            private static FunctionWrapper<GdipDrawString_delegate> GdipDrawString_ptr;
            internal static int GdipDrawString(IntPtr graphics, string text, int len, IntPtr font, ref RectangleF rc, IntPtr format, IntPtr brush) => GdipDrawString_ptr.Delegate(graphics, text, len, font, ref rc, format, brush);

            private delegate int GdipGetDC_delegate(IntPtr graphics, out IntPtr hdc);
            private static FunctionWrapper<GdipGetDC_delegate> GdipGetDC_ptr;
            internal static int GdipGetDC(IntPtr graphics, out IntPtr hdc) => GdipGetDC_ptr.Delegate(graphics, out hdc);

            private delegate int GdipReleaseDC_delegate(IntPtr graphics, IntPtr hdc);
            private static FunctionWrapper<GdipReleaseDC_delegate> GdipReleaseDC_ptr;
            internal static int GdipReleaseDC(IntPtr graphics, IntPtr hdc) => GdipReleaseDC_ptr.Delegate(graphics, hdc);
            internal static int IntGdipReleaseDC(HandleRef graphics, HandleRef hdc) => (int)GdipReleaseDC_ptr.Delegate(graphics.Handle, hdc.Handle);

            private delegate int GdipDrawImageRectI_delegate(IntPtr graphics, IntPtr image, int x, int y, int width, int height);
            private static FunctionWrapper<GdipDrawImageRectI_delegate> GdipDrawImageRectI_ptr;
            internal static int GdipDrawImageRectI(IntPtr graphics, IntPtr image, int x, int y, int width, int height) => GdipDrawImageRectI_ptr.Delegate(graphics, image, x, y, width, height);

            private delegate int GdipGetRenderingOrigin_delegate(IntPtr graphics, out int x, out int y);
            private static FunctionWrapper<GdipGetRenderingOrigin_delegate> GdipGetRenderingOrigin_ptr;
            internal static int GdipGetRenderingOrigin(IntPtr graphics, out int x, out int y) => GdipGetRenderingOrigin_ptr.Delegate(graphics, out x, out y);

            private delegate int GdipSetRenderingOrigin_delegate(IntPtr graphics, int x, int y);
            private static FunctionWrapper<GdipSetRenderingOrigin_delegate> GdipSetRenderingOrigin_ptr;
            internal static int GdipSetRenderingOrigin(IntPtr graphics, int x, int y) => GdipSetRenderingOrigin_ptr.Delegate(graphics, x, y);

            private delegate int GdipGraphicsClear_delegate(IntPtr graphics, int argb);
            private static FunctionWrapper<GdipGraphicsClear_delegate> GdipGraphicsClear_ptr;
            internal static int GdipGraphicsClear(IntPtr graphics, int argb) => GdipGraphicsClear_ptr.Delegate(graphics, argb);

            private delegate int GdipDrawClosedCurve_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count);
            private static FunctionWrapper<GdipDrawClosedCurve_delegate> GdipDrawClosedCurve_ptr;
            internal static int GdipDrawClosedCurve(IntPtr graphics, IntPtr pen, PointF[] points, int count) => GdipDrawClosedCurve_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawClosedCurveI_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count);
            private static FunctionWrapper<GdipDrawClosedCurveI_delegate> GdipDrawClosedCurveI_ptr;
            internal static int GdipDrawClosedCurveI(IntPtr graphics, IntPtr pen, Point[] points, int count) => GdipDrawClosedCurveI_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawClosedCurve2_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count, float tension);
            private static FunctionWrapper<GdipDrawClosedCurve2_delegate> GdipDrawClosedCurve2_ptr;
            internal static int GdipDrawClosedCurve2(IntPtr graphics, IntPtr pen, PointF[] points, int count, float tension) => GdipDrawClosedCurve2_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate int GdipDrawClosedCurve2I_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count, float tension);
            private static FunctionWrapper<GdipDrawClosedCurve2I_delegate> GdipDrawClosedCurve2I_ptr;
            internal static int GdipDrawClosedCurve2I(IntPtr graphics, IntPtr pen, Point[] points, int count, float tension) => GdipDrawClosedCurve2I_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate int GdipDrawCurve_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count);
            private static FunctionWrapper<GdipDrawCurve_delegate> GdipDrawCurve_ptr;
            internal static int GdipDrawCurve(IntPtr graphics, IntPtr pen, PointF[] points, int count) => GdipDrawCurve_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawCurveI_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count);
            private static FunctionWrapper<GdipDrawCurveI_delegate> GdipDrawCurveI_ptr;
            internal static int GdipDrawCurveI(IntPtr graphics, IntPtr pen, Point[] points, int count) => GdipDrawCurveI_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawCurve2_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count, float tension);
            private static FunctionWrapper<GdipDrawCurve2_delegate> GdipDrawCurve2_ptr;
            internal static int GdipDrawCurve2(IntPtr graphics, IntPtr pen, PointF[] points, int count, float tension) => GdipDrawCurve2_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate int GdipDrawCurve2I_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count, float tension);
            private static FunctionWrapper<GdipDrawCurve2I_delegate> GdipDrawCurve2I_ptr;
            internal static int GdipDrawCurve2I(IntPtr graphics, IntPtr pen, Point[] points, int count, float tension) => GdipDrawCurve2I_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate int GdipDrawCurve3_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipDrawCurve3_delegate> GdipDrawCurve3_ptr;
            internal static int GdipDrawCurve3(IntPtr graphics, IntPtr pen, PointF[] points, int count, int offset, int numberOfSegments, float tension) => GdipDrawCurve3_ptr.Delegate(graphics, pen, points, count, offset, numberOfSegments, tension);

            private delegate int GdipDrawCurve3I_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipDrawCurve3I_delegate> GdipDrawCurve3I_ptr;
            internal static int GdipDrawCurve3I(IntPtr graphics, IntPtr pen, Point[] points, int count, int offset, int numberOfSegments, float tension) => GdipDrawCurve3I_ptr.Delegate(graphics, pen, points, count, offset, numberOfSegments, tension);

            private delegate int GdipFillClosedCurve_delegate(IntPtr graphics, IntPtr brush, PointF[] points, int count);
            private static FunctionWrapper<GdipFillClosedCurve_delegate> GdipFillClosedCurve_ptr;
            internal static int GdipFillClosedCurve(IntPtr graphics, IntPtr brush, PointF[] points, int count) => GdipFillClosedCurve_ptr.Delegate(graphics, brush, points, count);

            private delegate int GdipFillClosedCurveI_delegate(IntPtr graphics, IntPtr brush, Point[] points, int count);
            private static FunctionWrapper<GdipFillClosedCurveI_delegate> GdipFillClosedCurveI_ptr;
            internal static int GdipFillClosedCurveI(IntPtr graphics, IntPtr brush, Point[] points, int count) => GdipFillClosedCurveI_ptr.Delegate(graphics, brush, points, count);

            private delegate int GdipFillClosedCurve2_delegate(IntPtr graphics, IntPtr brush, PointF[] points, int count, float tension, FillMode fillMode);
            private static FunctionWrapper<GdipFillClosedCurve2_delegate> GdipFillClosedCurve2_ptr;
            internal static int GdipFillClosedCurve2(IntPtr graphics, IntPtr brush, PointF[] points, int count, float tension, FillMode fillMode) => GdipFillClosedCurve2_ptr.Delegate(graphics, brush, points, count, tension, fillMode);

            private delegate int GdipFillClosedCurve2I_delegate(IntPtr graphics, IntPtr brush, Point[] points, int count, float tension, FillMode fillMode);
            private static FunctionWrapper<GdipFillClosedCurve2I_delegate> GdipFillClosedCurve2I_ptr;
            internal static int GdipFillClosedCurve2I(IntPtr graphics, IntPtr brush, Point[] points, int count, float tension, FillMode fillMode) => GdipFillClosedCurve2I_ptr.Delegate(graphics, brush, points, count, tension, fillMode);

            private delegate int GdipFillPie_delegate(IntPtr graphics, IntPtr brush, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipFillPie_delegate> GdipFillPie_ptr;
            internal static int GdipFillPie(IntPtr graphics, IntPtr brush, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipFillPie_ptr.Delegate(graphics, brush, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipFillPieI_delegate(IntPtr graphics, IntPtr brush, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipFillPieI_delegate> GdipFillPieI_ptr;
            internal static int GdipFillPieI(IntPtr graphics, IntPtr brush, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipFillPieI_ptr.Delegate(graphics, brush, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipFillPath_delegate(IntPtr graphics, IntPtr brush, IntPtr path);
            private static FunctionWrapper<GdipFillPath_delegate> GdipFillPath_ptr;
            internal static int GdipFillPath(IntPtr graphics, IntPtr brush, IntPtr path) => GdipFillPath_ptr.Delegate(graphics, brush, path);

            private delegate int GdipGetNearestColor_delegate(IntPtr graphics, out int argb);
            private static FunctionWrapper<GdipGetNearestColor_delegate> GdipGetNearestColor_ptr;
            internal static int GdipGetNearestColor(IntPtr graphics, out int argb) => GdipGetNearestColor_ptr.Delegate(graphics, out argb);

            private delegate int GdipTransformPoints_delegate(IntPtr graphics, CoordinateSpace destSpace, CoordinateSpace srcSpace, IntPtr points, int count);
            private static FunctionWrapper<GdipTransformPoints_delegate> GdipTransformPoints_ptr;
            internal static int GdipTransformPoints(IntPtr graphics, CoordinateSpace destSpace, CoordinateSpace srcSpace, IntPtr points, int count) => GdipTransformPoints_ptr.Delegate(graphics, destSpace, srcSpace, points, count);

            private delegate int GdipTransformPointsI_delegate(IntPtr graphics, CoordinateSpace destSpace, CoordinateSpace srcSpace, IntPtr points, int count);
            private static FunctionWrapper<GdipTransformPointsI_delegate> GdipTransformPointsI_ptr;
            internal static int GdipTransformPointsI(IntPtr graphics, CoordinateSpace destSpace, CoordinateSpace srcSpace, IntPtr points, int count) => GdipTransformPointsI_ptr.Delegate(graphics, destSpace, srcSpace, points, count);

            private delegate int GdipSetCompositingMode_delegate(IntPtr graphics, CompositingMode compositingMode);
            private static FunctionWrapper<GdipSetCompositingMode_delegate> GdipSetCompositingMode_ptr;
            internal static int GdipSetCompositingMode(IntPtr graphics, CompositingMode compositingMode) => GdipSetCompositingMode_ptr.Delegate(graphics, compositingMode);

            private delegate int GdipGetCompositingMode_delegate(IntPtr graphics, out CompositingMode compositingMode);
            private static FunctionWrapper<GdipGetCompositingMode_delegate> GdipGetCompositingMode_ptr;
            internal static int GdipGetCompositingMode(IntPtr graphics, out CompositingMode compositingMode) => GdipGetCompositingMode_ptr.Delegate(graphics, out compositingMode);

            private delegate int GdipSetCompositingQuality_delegate(IntPtr graphics, CompositingQuality compositingQuality);
            private static FunctionWrapper<GdipSetCompositingQuality_delegate> GdipSetCompositingQuality_ptr;
            internal static int GdipSetCompositingQuality(IntPtr graphics, CompositingQuality compositingQuality) => GdipSetCompositingQuality_ptr.Delegate(graphics, compositingQuality);

            private delegate int GdipGetCompositingQuality_delegate(IntPtr graphics, out CompositingQuality compositingQuality);
            private static FunctionWrapper<GdipGetCompositingQuality_delegate> GdipGetCompositingQuality_ptr;
            internal static int GdipGetCompositingQuality(IntPtr graphics, out CompositingQuality compositingQuality) => GdipGetCompositingQuality_ptr.Delegate(graphics, out compositingQuality);

            private delegate int GdipSetInterpolationMode_delegate(IntPtr graphics, InterpolationMode interpolationMode);
            private static FunctionWrapper<GdipSetInterpolationMode_delegate> GdipSetInterpolationMode_ptr;
            internal static int GdipSetInterpolationMode(IntPtr graphics, InterpolationMode interpolationMode) => GdipSetInterpolationMode_ptr.Delegate(graphics, interpolationMode);

            private delegate int GdipGetInterpolationMode_delegate(IntPtr graphics, out InterpolationMode interpolationMode);
            private static FunctionWrapper<GdipGetInterpolationMode_delegate> GdipGetInterpolationMode_ptr;
            internal static int GdipGetInterpolationMode(IntPtr graphics, out InterpolationMode interpolationMode) => GdipGetInterpolationMode_ptr.Delegate(graphics, out interpolationMode);

            private delegate int GdipGetDpiX_delegate(IntPtr graphics, out float dpi);
            private static FunctionWrapper<GdipGetDpiX_delegate> GdipGetDpiX_ptr;
            internal static int GdipGetDpiX(IntPtr graphics, out float dpi) => GdipGetDpiX_ptr.Delegate(graphics, out dpi);

            private delegate int GdipGetDpiY_delegate(IntPtr graphics, out float dpi);
            private static FunctionWrapper<GdipGetDpiY_delegate> GdipGetDpiY_ptr;
            internal static int GdipGetDpiY(IntPtr graphics, out float dpi) => GdipGetDpiY_ptr.Delegate(graphics, out dpi);

            private delegate int GdipGetPageUnit_delegate(IntPtr graphics, out GraphicsUnit unit);
            private static FunctionWrapper<GdipGetPageUnit_delegate> GdipGetPageUnit_ptr;
            internal static int GdipGetPageUnit(IntPtr graphics, out GraphicsUnit unit) => GdipGetPageUnit_ptr.Delegate(graphics, out unit);

            private delegate int GdipGetPageScale_delegate(IntPtr graphics, out float scale);
            private static FunctionWrapper<GdipGetPageScale_delegate> GdipGetPageScale_ptr;
            internal static int GdipGetPageScale(IntPtr graphics, out float scale) => GdipGetPageScale_ptr.Delegate(graphics, out scale);

            private delegate int GdipSetPageUnit_delegate(IntPtr graphics, GraphicsUnit unit);
            private static FunctionWrapper<GdipSetPageUnit_delegate> GdipSetPageUnit_ptr;
            internal static int GdipSetPageUnit(IntPtr graphics, GraphicsUnit unit) => GdipSetPageUnit_ptr.Delegate(graphics, unit);

            private delegate int GdipSetPageScale_delegate(IntPtr graphics, float scale);
            private static FunctionWrapper<GdipSetPageScale_delegate> GdipSetPageScale_ptr;
            internal static int GdipSetPageScale(IntPtr graphics, float scale) => GdipSetPageScale_ptr.Delegate(graphics, scale);

            private delegate int GdipSetPixelOffsetMode_delegate(IntPtr graphics, PixelOffsetMode pixelOffsetMode);
            private static FunctionWrapper<GdipSetPixelOffsetMode_delegate> GdipSetPixelOffsetMode_ptr;
            internal static int GdipSetPixelOffsetMode(IntPtr graphics, PixelOffsetMode pixelOffsetMode) => GdipSetPixelOffsetMode_ptr.Delegate(graphics, pixelOffsetMode);

            private delegate int GdipGetPixelOffsetMode_delegate(IntPtr graphics, out PixelOffsetMode pixelOffsetMode);
            private static FunctionWrapper<GdipGetPixelOffsetMode_delegate> GdipGetPixelOffsetMode_ptr;
            internal static int GdipGetPixelOffsetMode(IntPtr graphics, out PixelOffsetMode pixelOffsetMode) => GdipGetPixelOffsetMode_ptr.Delegate(graphics, out pixelOffsetMode);

            private delegate int GdipSetSmoothingMode_delegate(IntPtr graphics, SmoothingMode smoothingMode);
            private static FunctionWrapper<GdipSetSmoothingMode_delegate> GdipSetSmoothingMode_ptr;
            internal static int GdipSetSmoothingMode(IntPtr graphics, SmoothingMode smoothingMode) => GdipSetSmoothingMode_ptr.Delegate(graphics, smoothingMode);

            private delegate int GdipGetSmoothingMode_delegate(IntPtr graphics, out SmoothingMode smoothingMode);
            private static FunctionWrapper<GdipGetSmoothingMode_delegate> GdipGetSmoothingMode_ptr;
            internal static int GdipGetSmoothingMode(IntPtr graphics, out SmoothingMode smoothingMode) => GdipGetSmoothingMode_ptr.Delegate(graphics, out smoothingMode);

            private delegate int GdipSetTextContrast_delegate(IntPtr graphics, int contrast);
            private static FunctionWrapper<GdipSetTextContrast_delegate> GdipSetTextContrast_ptr;
            internal static int GdipSetTextContrast(IntPtr graphics, int contrast) => GdipSetTextContrast_ptr.Delegate(graphics, contrast);

            private delegate int GdipGetTextContrast_delegate(IntPtr graphics, out int contrast);
            private static FunctionWrapper<GdipGetTextContrast_delegate> GdipGetTextContrast_ptr;
            internal static int GdipGetTextContrast(IntPtr graphics, out int contrast) => GdipGetTextContrast_ptr.Delegate(graphics, out contrast);

            private delegate int GdipSetTextRenderingHint_delegate(IntPtr graphics, TextRenderingHint mode);
            private static FunctionWrapper<GdipSetTextRenderingHint_delegate> GdipSetTextRenderingHint_ptr;
            internal static int GdipSetTextRenderingHint(IntPtr graphics, TextRenderingHint mode) => GdipSetTextRenderingHint_ptr.Delegate(graphics, mode);

            private delegate int GdipGetTextRenderingHint_delegate(IntPtr graphics, out TextRenderingHint mode);
            private static FunctionWrapper<GdipGetTextRenderingHint_delegate> GdipGetTextRenderingHint_ptr;
            internal static int GdipGetTextRenderingHint(IntPtr graphics, out TextRenderingHint mode) => GdipGetTextRenderingHint_ptr.Delegate(graphics, out mode);

            private delegate int GdipFlush_delegate(IntPtr graphics, FlushIntention intention);
            private static FunctionWrapper<GdipFlush_delegate> GdipFlush_ptr;
            internal static int GdipFlush(IntPtr graphics, FlushIntention intention) => GdipFlush_ptr.Delegate(graphics, intention);

            private delegate int GdipAddPathString_delegate(IntPtr path, [MarshalAs(UnmanagedType.LPWStr)]string s, int lenght, IntPtr family, int style, float emSize, ref RectangleF layoutRect, IntPtr format);
            private static FunctionWrapper<GdipAddPathString_delegate> GdipAddPathString_ptr;
            internal static int GdipAddPathString(IntPtr path, string s, int lenght, IntPtr family, int style, float emSize, ref RectangleF layoutRect, IntPtr format) => GdipAddPathString_ptr.Delegate(path, s, lenght, family, style, emSize, ref layoutRect, format);

            private delegate int GdipAddPathStringI_delegate(IntPtr path, [MarshalAs(UnmanagedType.LPWStr)]string s, int lenght, IntPtr family, int style, float emSize, ref Rectangle layoutRect, IntPtr format);
            private static FunctionWrapper<GdipAddPathStringI_delegate> GdipAddPathStringI_ptr;
            internal static int GdipAddPathStringI(IntPtr path, string s, int lenght, IntPtr family, int style, float emSize, ref Rectangle layoutRect, IntPtr format) => GdipAddPathStringI_ptr.Delegate(path, s, lenght, family, style, emSize, ref layoutRect, format);

            private delegate int GdipCreateFromHWND_delegate(IntPtr hwnd, out IntPtr graphics);
            private static FunctionWrapper<GdipCreateFromHWND_delegate> GdipCreateFromHWND_ptr;
            internal static int GdipCreateFromHWND(IntPtr hwnd, out IntPtr graphics) => GdipCreateFromHWND_ptr.Delegate(hwnd, out graphics);

            private delegate int GdipMeasureString_delegate(IntPtr graphics, [MarshalAs(UnmanagedType.LPWStr)]string str, int length, IntPtr font, ref RectangleF layoutRect, IntPtr stringFormat, out RectangleF boundingBox, int* codepointsFitted, int* linesFilled);
            private static FunctionWrapper<GdipMeasureString_delegate> GdipMeasureString_ptr;
            internal static int GdipMeasureString(IntPtr graphics, string str, int length, IntPtr font, ref RectangleF layoutRect, IntPtr stringFormat, out RectangleF boundingBox, int* codepointsFitted, int* linesFilled) => GdipMeasureString_ptr.Delegate(graphics, str, length, font, ref layoutRect, stringFormat, out boundingBox, codepointsFitted, linesFilled);

            private delegate int GdipMeasureCharacterRanges_delegate(IntPtr graphics, [MarshalAs(UnmanagedType.LPWStr)]string str, int length, IntPtr font, ref RectangleF layoutRect, IntPtr stringFormat, int regcount, out IntPtr regions);
            private static FunctionWrapper<GdipMeasureCharacterRanges_delegate> GdipMeasureCharacterRanges_ptr;
            internal static int GdipMeasureCharacterRanges(IntPtr graphics, string str, int length, IntPtr font, ref RectangleF layoutRect, IntPtr stringFormat, int regcount, out IntPtr regions) => GdipMeasureCharacterRanges_ptr.Delegate(graphics, str, length, font, ref layoutRect, stringFormat, regcount, out regions);

            private delegate int GdipLoadImageFromFile_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr image);
            private static FunctionWrapper<GdipLoadImageFromFile_delegate> GdipLoadImageFromFile_ptr;
            internal static int GdipLoadImageFromFile(string filename, out IntPtr image) => GdipLoadImageFromFile_ptr.Delegate(filename, out image);

            private delegate int GdipCloneImage_delegate(IntPtr image, out IntPtr imageclone);
            private static FunctionWrapper<GdipCloneImage_delegate> GdipCloneImage_ptr;
            internal static int GdipCloneImage(IntPtr image, out IntPtr imageclone) => GdipCloneImage_ptr.Delegate(image, out imageclone);

            private delegate int GdipLoadImageFromFileICM_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr image);
            private static FunctionWrapper<GdipLoadImageFromFileICM_delegate> GdipLoadImageFromFileICM_ptr;
            internal static int GdipLoadImageFromFileICM(string filename, out IntPtr image) => GdipLoadImageFromFileICM_ptr.Delegate(filename, out image);

            private delegate int GdipDisposeImage_delegate(IntPtr image);
            private static FunctionWrapper<GdipDisposeImage_delegate> GdipDisposeImage_ptr;
            internal static int GdipDisposeImage(IntPtr image) => GdipDisposeImage_ptr.Delegate(image);
            internal static int IntGdipDisposeImage(HandleRef image) => (int)GdipDisposeImage_ptr.Delegate(image.Handle);

            private delegate int GdipGetImageFlags_delegate(IntPtr image, out int flag);
            private static FunctionWrapper<GdipGetImageFlags_delegate> GdipGetImageFlags_ptr;
            internal static int GdipGetImageFlags(IntPtr image, out int flag) => GdipGetImageFlags_ptr.Delegate(image, out flag);

            private delegate int GdipGetImageType_delegate(IntPtr image, out ImageType type);
            private static FunctionWrapper<GdipGetImageType_delegate> GdipGetImageType_ptr;
            internal static int GdipGetImageType(IntPtr image, out ImageType type) => GdipGetImageType_ptr.Delegate(image, out type);

            private delegate int GdipImageGetFrameDimensionsCount_delegate(IntPtr image, out uint count);
            private static FunctionWrapper<GdipImageGetFrameDimensionsCount_delegate> GdipImageGetFrameDimensionsCount_ptr;
            internal static int GdipImageGetFrameDimensionsCount(IntPtr image, out uint count) => GdipImageGetFrameDimensionsCount_ptr.Delegate(image, out count);

            private delegate int GdipImageGetFrameDimensionsList_delegate(IntPtr image, [Out] Guid[] dimensionIDs, uint count);
            private static FunctionWrapper<GdipImageGetFrameDimensionsList_delegate> GdipImageGetFrameDimensionsList_ptr;
            internal static int GdipImageGetFrameDimensionsList(IntPtr image, [Out] Guid[] dimensionIDs, uint count) => GdipImageGetFrameDimensionsList_ptr.Delegate(image, dimensionIDs, count);

            private delegate int GdipGetImageHeight_delegate(IntPtr image, out uint height);
            private static FunctionWrapper<GdipGetImageHeight_delegate> GdipGetImageHeight_ptr;
            internal static int GdipGetImageHeight(IntPtr image, out uint height) => GdipGetImageHeight_ptr.Delegate(image, out height);

            private delegate int GdipGetImageHorizontalResolution_delegate(IntPtr image, out float resolution);
            private static FunctionWrapper<GdipGetImageHorizontalResolution_delegate> GdipGetImageHorizontalResolution_ptr;
            internal static int GdipGetImageHorizontalResolution(IntPtr image, out float resolution) => GdipGetImageHorizontalResolution_ptr.Delegate(image, out resolution);

            private delegate int GdipGetImagePaletteSize_delegate(IntPtr image, out int size);
            private static FunctionWrapper<GdipGetImagePaletteSize_delegate> GdipGetImagePaletteSize_ptr;
            internal static int GdipGetImagePaletteSize(IntPtr image, out int size) => GdipGetImagePaletteSize_ptr.Delegate(image, out size);

            private delegate int GdipGetImagePalette_delegate(IntPtr image, IntPtr palette, int size);
            private static FunctionWrapper<GdipGetImagePalette_delegate> GdipGetImagePalette_ptr;
            internal static int GdipGetImagePalette(IntPtr image, IntPtr palette, int size) => GdipGetImagePalette_ptr.Delegate(image, palette, size);

            private delegate int GdipSetImagePalette_delegate(IntPtr image, IntPtr palette);
            private static FunctionWrapper<GdipSetImagePalette_delegate> GdipSetImagePalette_ptr;
            internal static int GdipSetImagePalette(IntPtr image, IntPtr palette) => GdipSetImagePalette_ptr.Delegate(image, palette);

            private delegate int GdipGetImageDimension_delegate(IntPtr image, out float width, out float height);
            private static FunctionWrapper<GdipGetImageDimension_delegate> GdipGetImageDimension_ptr;
            internal static int GdipGetImageDimension(IntPtr image, out float width, out float height) => GdipGetImageDimension_ptr.Delegate(image, out width, out height);

            private delegate int GdipGetImagePixelFormat_delegate(IntPtr image, out PixelFormat format);
            private static FunctionWrapper<GdipGetImagePixelFormat_delegate> GdipGetImagePixelFormat_ptr;
            internal static int GdipGetImagePixelFormat(IntPtr image, out PixelFormat format) => GdipGetImagePixelFormat_ptr.Delegate(image, out format);

            private delegate int GdipGetPropertyCount_delegate(IntPtr image, out uint propNumbers);
            private static FunctionWrapper<GdipGetPropertyCount_delegate> GdipGetPropertyCount_ptr;
            internal static int GdipGetPropertyCount(IntPtr image, out uint propNumbers) => GdipGetPropertyCount_ptr.Delegate(image, out propNumbers);

            private delegate int GdipGetPropertyIdList_delegate(IntPtr image, uint propNumbers, [Out] int[] list);
            private static FunctionWrapper<GdipGetPropertyIdList_delegate> GdipGetPropertyIdList_ptr;
            internal static int GdipGetPropertyIdList(IntPtr image, uint propNumbers, [Out] int[] list) => GdipGetPropertyIdList_ptr.Delegate(image, propNumbers, list);

            private delegate int GdipGetPropertySize_delegate(IntPtr image, out int bufferSize, out int propNumbers);
            private static FunctionWrapper<GdipGetPropertySize_delegate> GdipGetPropertySize_ptr;
            internal static int GdipGetPropertySize(IntPtr image, out int bufferSize, out int propNumbers) => GdipGetPropertySize_ptr.Delegate(image, out bufferSize, out propNumbers);

            private delegate int GdipGetAllPropertyItems_delegate(IntPtr image, int bufferSize, int propNumbers, IntPtr items);
            private static FunctionWrapper<GdipGetAllPropertyItems_delegate> GdipGetAllPropertyItems_ptr;
            internal static int GdipGetAllPropertyItems(IntPtr image, int bufferSize, int propNumbers, IntPtr items) => GdipGetAllPropertyItems_ptr.Delegate(image, bufferSize, propNumbers, items);

            private delegate int GdipGetImageRawFormat_delegate(IntPtr image, out Guid format);
            private static FunctionWrapper<GdipGetImageRawFormat_delegate> GdipGetImageRawFormat_ptr;
            internal static int GdipGetImageRawFormat(IntPtr image, out Guid format) => GdipGetImageRawFormat_ptr.Delegate(image, out format);

            private delegate int GdipGetImageVerticalResolution_delegate(IntPtr image, out float resolution);
            private static FunctionWrapper<GdipGetImageVerticalResolution_delegate> GdipGetImageVerticalResolution_ptr;
            internal static int GdipGetImageVerticalResolution(IntPtr image, out float resolution) => GdipGetImageVerticalResolution_ptr.Delegate(image, out resolution);

            private delegate int GdipGetImageWidth_delegate(IntPtr image, out uint width);
            private static FunctionWrapper<GdipGetImageWidth_delegate> GdipGetImageWidth_ptr;
            internal static int GdipGetImageWidth(IntPtr image, out uint width) => GdipGetImageWidth_ptr.Delegate(image, out width);

            private delegate int GdipGetImageBounds_delegate(IntPtr image, out RectangleF source, ref GraphicsUnit unit);
            private static FunctionWrapper<GdipGetImageBounds_delegate> GdipGetImageBounds_ptr;
            internal static int GdipGetImageBounds(IntPtr image, out RectangleF source, ref GraphicsUnit unit) => GdipGetImageBounds_ptr.Delegate(image, out source, ref unit);

            private delegate int GdipGetEncoderParameterListSize_delegate(IntPtr image, ref Guid encoder, out uint size);
            private static FunctionWrapper<GdipGetEncoderParameterListSize_delegate> GdipGetEncoderParameterListSize_ptr;
            internal static int GdipGetEncoderParameterListSize(IntPtr image, ref Guid encoder, out uint size) => GdipGetEncoderParameterListSize_ptr.Delegate(image, ref encoder, out size);

            private delegate int GdipGetEncoderParameterList_delegate(IntPtr image, ref Guid encoder, uint size, IntPtr buffer);
            private static FunctionWrapper<GdipGetEncoderParameterList_delegate> GdipGetEncoderParameterList_ptr;
            internal static int GdipGetEncoderParameterList(IntPtr image, ref Guid encoder, uint size, IntPtr buffer) => GdipGetEncoderParameterList_ptr.Delegate(image, ref encoder, size, buffer);

            private delegate int GdipImageGetFrameCount_delegate(IntPtr image, ref Guid guidDimension, out uint count);
            private static FunctionWrapper<GdipImageGetFrameCount_delegate> GdipImageGetFrameCount_ptr;
            internal static int GdipImageGetFrameCount(IntPtr image, ref Guid guidDimension, out uint count) => GdipImageGetFrameCount_ptr.Delegate(image, ref guidDimension, out count);

            private delegate int GdipImageSelectActiveFrame_delegate(IntPtr image, ref Guid guidDimension, int frameIndex);
            private static FunctionWrapper<GdipImageSelectActiveFrame_delegate> GdipImageSelectActiveFrame_ptr;
            internal static int GdipImageSelectActiveFrame(IntPtr image, ref Guid guidDimension, int frameIndex) => GdipImageSelectActiveFrame_ptr.Delegate(image, ref guidDimension, frameIndex);

            private delegate int GdipGetPropertyItemSize_delegate(IntPtr image, int propertyID, out int propertySize);
            private static FunctionWrapper<GdipGetPropertyItemSize_delegate> GdipGetPropertyItemSize_ptr;
            internal static int GdipGetPropertyItemSize(IntPtr image, int propertyID, out int propertySize) => GdipGetPropertyItemSize_ptr.Delegate(image, propertyID, out propertySize);

            private delegate int GdipGetPropertyItem_delegate(IntPtr image, int propertyID, int propertySize, IntPtr buffer);
            private static FunctionWrapper<GdipGetPropertyItem_delegate> GdipGetPropertyItem_ptr;
            internal static int GdipGetPropertyItem(IntPtr image, int propertyID, int propertySize, IntPtr buffer) => GdipGetPropertyItem_ptr.Delegate(image, propertyID, propertySize, buffer);

            private delegate int GdipRemovePropertyItem_delegate(IntPtr image, int propertyId);
            private static FunctionWrapper<GdipRemovePropertyItem_delegate> GdipRemovePropertyItem_ptr;
            internal static int GdipRemovePropertyItem(IntPtr image, int propertyId) => GdipRemovePropertyItem_ptr.Delegate(image, propertyId);

            private delegate int GdipSetPropertyItem_delegate(IntPtr image, GdipPropertyItem* propertyItem);
            private static FunctionWrapper<GdipSetPropertyItem_delegate> GdipSetPropertyItem_ptr;
            internal static int GdipSetPropertyItem(IntPtr image, GdipPropertyItem* propertyItem) => GdipSetPropertyItem_ptr.Delegate(image, propertyItem);

            private delegate int GdipGetImageThumbnail_delegate(IntPtr image, uint width, uint height, out IntPtr thumbImage, IntPtr callback, IntPtr callBackData);
            private static FunctionWrapper<GdipGetImageThumbnail_delegate> GdipGetImageThumbnail_ptr;
            internal static int GdipGetImageThumbnail(IntPtr image, uint width, uint height, out IntPtr thumbImage, IntPtr callback, IntPtr callBackData) => GdipGetImageThumbnail_ptr.Delegate(image, width, height, out thumbImage, callback, callBackData);

            private delegate int GdipImageRotateFlip_delegate(IntPtr image, RotateFlipType rotateFlipType);
            private static FunctionWrapper<GdipImageRotateFlip_delegate> GdipImageRotateFlip_ptr;
            internal static int GdipImageRotateFlip(IntPtr image, RotateFlipType rotateFlipType) => GdipImageRotateFlip_ptr.Delegate(image, rotateFlipType);

            private delegate int GdipSaveImageToFile_delegate(IntPtr image, [MarshalAs(UnmanagedType.LPWStr)]string filename, ref Guid encoderClsID, IntPtr encoderParameters);
            private static FunctionWrapper<GdipSaveImageToFile_delegate> GdipSaveImageToFile_ptr;
            internal static int GdipSaveImageToFile(IntPtr image, string filename, ref Guid encoderClsID, IntPtr encoderParameters) => GdipSaveImageToFile_ptr.Delegate(image, filename, ref encoderClsID, encoderParameters);

            private delegate int GdipSaveAdd_delegate(IntPtr image, IntPtr encoderParameters);
            private static FunctionWrapper<GdipSaveAdd_delegate> GdipSaveAdd_ptr;
            internal static int GdipSaveAdd(IntPtr image, IntPtr encoderParameters) => GdipSaveAdd_ptr.Delegate(image, encoderParameters);

            private delegate int GdipSaveAddImage_delegate(IntPtr image, IntPtr imagenew, IntPtr encoderParameters);
            private static FunctionWrapper<GdipSaveAddImage_delegate> GdipSaveAddImage_ptr;
            internal static int GdipSaveAddImage(IntPtr image, IntPtr imagenew, IntPtr encoderParameters) => GdipSaveAddImage_ptr.Delegate(image, imagenew, encoderParameters);

            private delegate int GdipDrawImageI_delegate(IntPtr graphics, IntPtr image, int x, int y);
            private static FunctionWrapper<GdipDrawImageI_delegate> GdipDrawImageI_ptr;
            internal static int GdipDrawImageI(IntPtr graphics, IntPtr image, int x, int y) => GdipDrawImageI_ptr.Delegate(graphics, image, x, y);

            private delegate int GdipGetImageGraphicsContext_delegate(IntPtr image, out IntPtr graphics);
            private static FunctionWrapper<GdipGetImageGraphicsContext_delegate> GdipGetImageGraphicsContext_ptr;
            internal static int GdipGetImageGraphicsContext(IntPtr image, out IntPtr graphics) => GdipGetImageGraphicsContext_ptr.Delegate(image, out graphics);

            private delegate int GdipDrawImage_delegate(IntPtr graphics, IntPtr image, float x, float y);
            private static FunctionWrapper<GdipDrawImage_delegate> GdipDrawImage_ptr;
            internal static int GdipDrawImage(IntPtr graphics, IntPtr image, float x, float y) => GdipDrawImage_ptr.Delegate(graphics, image, x, y);

            private delegate int GdipDrawImagePoints_delegate(IntPtr graphics, IntPtr image, PointF[] destPoints, int count);
            private static FunctionWrapper<GdipDrawImagePoints_delegate> GdipDrawImagePoints_ptr;
            internal static int GdipDrawImagePoints(IntPtr graphics, IntPtr image, PointF[] destPoints, int count) => GdipDrawImagePoints_ptr.Delegate(graphics, image, destPoints, count);

            private delegate int GdipDrawImagePointsI_delegate(IntPtr graphics, IntPtr image, Point[] destPoints, int count);
            private static FunctionWrapper<GdipDrawImagePointsI_delegate> GdipDrawImagePointsI_ptr;
            internal static int GdipDrawImagePointsI(IntPtr graphics, IntPtr image, Point[] destPoints, int count) => GdipDrawImagePointsI_ptr.Delegate(graphics, image, destPoints, count);

            private delegate int GdipDrawImageRectRectI_delegate(IntPtr graphics, IntPtr image, int dstx, int dsty, int dstwidth, int dstheight, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);
            private static FunctionWrapper<GdipDrawImageRectRectI_delegate> GdipDrawImageRectRectI_ptr;
            internal static int GdipDrawImageRectRectI(IntPtr graphics, IntPtr image, int dstx, int dsty, int dstwidth, int dstheight, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData) => GdipDrawImageRectRectI_ptr.Delegate(graphics, image, dstx, dsty, dstwidth, dstheight, srcx, srcy, srcwidth, srcheight, srcUnit, imageattr, callback, callbackData);

            private delegate int GdipDrawImageRectRect_delegate(IntPtr graphics, IntPtr image, float dstx, float dsty, float dstwidth, float dstheight, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);
            private static FunctionWrapper<GdipDrawImageRectRect_delegate> GdipDrawImageRectRect_ptr;
            internal static int GdipDrawImageRectRect(IntPtr graphics, IntPtr image, float dstx, float dsty, float dstwidth, float dstheight, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData) => GdipDrawImageRectRect_ptr.Delegate(graphics, image, dstx, dsty, dstwidth, dstheight, srcx, srcy, srcwidth, srcheight, srcUnit, imageattr, callback, callbackData);

            private delegate int GdipDrawImagePointsRectI_delegate(IntPtr graphics, IntPtr image, Point[] destPoints, int count, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);
            private static FunctionWrapper<GdipDrawImagePointsRectI_delegate> GdipDrawImagePointsRectI_ptr;
            internal static int GdipDrawImagePointsRectI(IntPtr graphics, IntPtr image, Point[] destPoints, int count, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData) => GdipDrawImagePointsRectI_ptr.Delegate(graphics, image, destPoints, count, srcx, srcy, srcwidth, srcheight, srcUnit, imageattr, callback, callbackData);

            private delegate int GdipDrawImagePointsRect_delegate(IntPtr graphics, IntPtr image, PointF[] destPoints, int count, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);
            private static FunctionWrapper<GdipDrawImagePointsRect_delegate> GdipDrawImagePointsRect_ptr;
            internal static int GdipDrawImagePointsRect(IntPtr graphics, IntPtr image, PointF[] destPoints, int count, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData) => GdipDrawImagePointsRect_ptr.Delegate(graphics, image, destPoints, count, srcx, srcy, srcwidth, srcheight, srcUnit, imageattr, callback, callbackData);

            private delegate int GdipDrawImageRect_delegate(IntPtr graphics, IntPtr image, float x, float y, float width, float height);
            private static FunctionWrapper<GdipDrawImageRect_delegate> GdipDrawImageRect_ptr;
            internal static int GdipDrawImageRect(IntPtr graphics, IntPtr image, float x, float y, float width, float height) => GdipDrawImageRect_ptr.Delegate(graphics, image, x, y, width, height);

            private delegate int GdipDrawImagePointRect_delegate(IntPtr graphics, IntPtr image, float x, float y, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit);
            private static FunctionWrapper<GdipDrawImagePointRect_delegate> GdipDrawImagePointRect_ptr;
            internal static int GdipDrawImagePointRect(IntPtr graphics, IntPtr image, float x, float y, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit) => GdipDrawImagePointRect_ptr.Delegate(graphics, image, x, y, srcx, srcy, srcwidth, srcheight, srcUnit);

            private delegate int GdipDrawImagePointRectI_delegate(IntPtr graphics, IntPtr image, int x, int y, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit);
            private static FunctionWrapper<GdipDrawImagePointRectI_delegate> GdipDrawImagePointRectI_ptr;
            internal static int GdipDrawImagePointRectI(IntPtr graphics, IntPtr image, int x, int y, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit) => GdipDrawImagePointRectI_ptr.Delegate(graphics, image, x, y, srcx, srcy, srcwidth, srcheight, srcUnit);

            private delegate int GdipCreatePath_delegate(FillMode brushMode, out IntPtr path);
            private static FunctionWrapper<GdipCreatePath_delegate> GdipCreatePath_ptr;
            internal static int GdipCreatePath(FillMode brushMode, out IntPtr path) => GdipCreatePath_ptr.Delegate(brushMode, out path);

            private delegate int GdipCreatePath2_delegate(PointF[] points, byte[] types, int count, FillMode brushMode, out IntPtr path);
            private static FunctionWrapper<GdipCreatePath2_delegate> GdipCreatePath2_ptr;
            internal static int GdipCreatePath2(PointF[] points, byte[] types, int count, FillMode brushMode, out IntPtr path) => GdipCreatePath2_ptr.Delegate(points, types, count, brushMode, out path);

            private delegate int GdipCreatePath2I_delegate(Point[] points, byte[] types, int count, FillMode brushMode, out IntPtr path);
            private static FunctionWrapper<GdipCreatePath2I_delegate> GdipCreatePath2I_ptr;
            internal static int GdipCreatePath2I(Point[] points, byte[] types, int count, FillMode brushMode, out IntPtr path) => GdipCreatePath2I_ptr.Delegate(points, types, count, brushMode, out path);

            private delegate int GdipClonePath_delegate(IntPtr path, out IntPtr clonePath);
            private static FunctionWrapper<GdipClonePath_delegate> GdipClonePath_ptr;
            internal static int GdipClonePath(IntPtr path, out IntPtr clonePath) => GdipClonePath_ptr.Delegate(path, out clonePath);

            private delegate int GdipDeletePath_delegate(IntPtr path);
            private static FunctionWrapper<GdipDeletePath_delegate> GdipDeletePath_ptr;
            internal static int GdipDeletePath(IntPtr path) => GdipDeletePath_ptr.Delegate(path);
            internal static int IntGdipDeletePath(HandleRef path) => (int)GdipDeletePath_ptr.Delegate(path.Handle);

            private delegate int GdipResetPath_delegate(IntPtr path);
            private static FunctionWrapper<GdipResetPath_delegate> GdipResetPath_ptr;
            internal static int GdipResetPath(IntPtr path) => GdipResetPath_ptr.Delegate(path);

            private delegate int GdipGetPointCount_delegate(IntPtr path, out int count);
            private static FunctionWrapper<GdipGetPointCount_delegate> GdipGetPointCount_ptr;
            internal static int GdipGetPointCount(IntPtr path, out int count) => GdipGetPointCount_ptr.Delegate(path, out count);

            private delegate int GdipGetPathTypes_delegate(IntPtr path, [Out] byte[] types, int count);
            private static FunctionWrapper<GdipGetPathTypes_delegate> GdipGetPathTypes_ptr;
            internal static int GdipGetPathTypes(IntPtr path, [Out] byte[] types, int count) => GdipGetPathTypes_ptr.Delegate(path, types, count);

            private delegate int GdipGetPathPoints_delegate(IntPtr path, [Out] PointF[] points, int count);
            private static FunctionWrapper<GdipGetPathPoints_delegate> GdipGetPathPoints_ptr;
            internal static int GdipGetPathPoints(IntPtr path, [Out] PointF[] points, int count) => GdipGetPathPoints_ptr.Delegate(path, points, count);

            private delegate int GdipGetPathPointsI_delegate(IntPtr path, [Out] Point[] points, int count);
            private static FunctionWrapper<GdipGetPathPointsI_delegate> GdipGetPathPointsI_ptr;
            internal static int GdipGetPathPointsI(IntPtr path, [Out] Point[] points, int count) => GdipGetPathPointsI_ptr.Delegate(path, points, count);

            private delegate int GdipGetPathFillMode_delegate(IntPtr path, out FillMode fillMode);
            private static FunctionWrapper<GdipGetPathFillMode_delegate> GdipGetPathFillMode_ptr;
            internal static int GdipGetPathFillMode(IntPtr path, out FillMode fillMode) => GdipGetPathFillMode_ptr.Delegate(path, out fillMode);

            private delegate int GdipSetPathFillMode_delegate(IntPtr path, FillMode fillMode);
            private static FunctionWrapper<GdipSetPathFillMode_delegate> GdipSetPathFillMode_ptr;
            internal static int GdipSetPathFillMode(IntPtr path, FillMode fillMode) => GdipSetPathFillMode_ptr.Delegate(path, fillMode);

            private delegate int GdipStartPathFigure_delegate(IntPtr path);
            private static FunctionWrapper<GdipStartPathFigure_delegate> GdipStartPathFigure_ptr;
            internal static int GdipStartPathFigure(IntPtr path) => GdipStartPathFigure_ptr.Delegate(path);

            private delegate int GdipClosePathFigure_delegate(IntPtr path);
            private static FunctionWrapper<GdipClosePathFigure_delegate> GdipClosePathFigure_ptr;
            internal static int GdipClosePathFigure(IntPtr path) => GdipClosePathFigure_ptr.Delegate(path);

            private delegate int GdipClosePathFigures_delegate(IntPtr path);
            private static FunctionWrapper<GdipClosePathFigures_delegate> GdipClosePathFigures_ptr;
            internal static int GdipClosePathFigures(IntPtr path) => GdipClosePathFigures_ptr.Delegate(path);

            private delegate int GdipSetPathMarker_delegate(IntPtr path);
            private static FunctionWrapper<GdipSetPathMarker_delegate> GdipSetPathMarker_ptr;
            internal static int GdipSetPathMarker(IntPtr path) => GdipSetPathMarker_ptr.Delegate(path);

            private delegate int GdipClearPathMarkers_delegate(IntPtr path);
            private static FunctionWrapper<GdipClearPathMarkers_delegate> GdipClearPathMarkers_ptr;
            internal static int GdipClearPathMarkers(IntPtr path) => GdipClearPathMarkers_ptr.Delegate(path);

            private delegate int GdipReversePath_delegate(IntPtr path);
            private static FunctionWrapper<GdipReversePath_delegate> GdipReversePath_ptr;
            internal static int GdipReversePath(IntPtr path) => GdipReversePath_ptr.Delegate(path);

            private delegate int GdipGetPathLastPoint_delegate(IntPtr path, out PointF lastPoint);
            private static FunctionWrapper<GdipGetPathLastPoint_delegate> GdipGetPathLastPoint_ptr;
            internal static int GdipGetPathLastPoint(IntPtr path, out PointF lastPoint) => GdipGetPathLastPoint_ptr.Delegate(path, out lastPoint);

            private delegate int GdipAddPathLine_delegate(IntPtr path, float x1, float y1, float x2, float y2);
            private static FunctionWrapper<GdipAddPathLine_delegate> GdipAddPathLine_ptr;
            internal static int GdipAddPathLine(IntPtr path, float x1, float y1, float x2, float y2) => GdipAddPathLine_ptr.Delegate(path, x1, y1, x2, y2);

            private delegate int GdipAddPathLine2_delegate(IntPtr path, PointF[] points, int count);
            private static FunctionWrapper<GdipAddPathLine2_delegate> GdipAddPathLine2_ptr;
            internal static int GdipAddPathLine2(IntPtr path, PointF[] points, int count) => GdipAddPathLine2_ptr.Delegate(path, points, count);

            private delegate int GdipAddPathLine2I_delegate(IntPtr path, Point[] points, int count);
            private static FunctionWrapper<GdipAddPathLine2I_delegate> GdipAddPathLine2I_ptr;
            internal static int GdipAddPathLine2I(IntPtr path, Point[] points, int count) => GdipAddPathLine2I_ptr.Delegate(path, points, count);

            private delegate int GdipAddPathArc_delegate(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathArc_delegate> GdipAddPathArc_ptr;
            internal static int GdipAddPathArc(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipAddPathArc_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipAddPathBezier_delegate(IntPtr path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);
            private static FunctionWrapper<GdipAddPathBezier_delegate> GdipAddPathBezier_ptr;
            internal static int GdipAddPathBezier(IntPtr path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) => GdipAddPathBezier_ptr.Delegate(path, x1, y1, x2, y2, x3, y3, x4, y4);

            private delegate int GdipAddPathBeziers_delegate(IntPtr path, PointF[] points, int count);
            private static FunctionWrapper<GdipAddPathBeziers_delegate> GdipAddPathBeziers_ptr;
            internal static int GdipAddPathBeziers(IntPtr path, PointF[] points, int count) => GdipAddPathBeziers_ptr.Delegate(path, points, count);

            private delegate int GdipAddPathCurve_delegate(IntPtr path, PointF[] points, int count);
            private static FunctionWrapper<GdipAddPathCurve_delegate> GdipAddPathCurve_ptr;
            internal static int GdipAddPathCurve(IntPtr path, PointF[] points, int count) => GdipAddPathCurve_ptr.Delegate(path, points, count);

            private delegate int GdipAddPathCurveI_delegate(IntPtr path, Point[] points, int count);
            private static FunctionWrapper<GdipAddPathCurveI_delegate> GdipAddPathCurveI_ptr;
            internal static int GdipAddPathCurveI(IntPtr path, Point[] points, int count) => GdipAddPathCurveI_ptr.Delegate(path, points, count);

            private delegate int GdipAddPathCurve2_delegate(IntPtr path, PointF[] points, int count, float tension);
            private static FunctionWrapper<GdipAddPathCurve2_delegate> GdipAddPathCurve2_ptr;
            internal static int GdipAddPathCurve2(IntPtr path, PointF[] points, int count, float tension) => GdipAddPathCurve2_ptr.Delegate(path, points, count, tension);

            private delegate int GdipAddPathCurve2I_delegate(IntPtr path, Point[] points, int count, float tension);
            private static FunctionWrapper<GdipAddPathCurve2I_delegate> GdipAddPathCurve2I_ptr;
            internal static int GdipAddPathCurve2I(IntPtr path, Point[] points, int count, float tension) => GdipAddPathCurve2I_ptr.Delegate(path, points, count, tension);

            private delegate int GdipAddPathCurve3_delegate(IntPtr path, PointF[] points, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipAddPathCurve3_delegate> GdipAddPathCurve3_ptr;
            internal static int GdipAddPathCurve3(IntPtr path, PointF[] points, int count, int offset, int numberOfSegments, float tension) => GdipAddPathCurve3_ptr.Delegate(path, points, count, offset, numberOfSegments, tension);

            private delegate int GdipAddPathCurve3I_delegate(IntPtr path, Point[] points, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipAddPathCurve3I_delegate> GdipAddPathCurve3I_ptr;
            internal static int GdipAddPathCurve3I(IntPtr path, Point[] points, int count, int offset, int numberOfSegments, float tension) => GdipAddPathCurve3I_ptr.Delegate(path, points, count, offset, numberOfSegments, tension);

            private delegate int GdipAddPathClosedCurve_delegate(IntPtr path, PointF[] points, int count);
            private static FunctionWrapper<GdipAddPathClosedCurve_delegate> GdipAddPathClosedCurve_ptr;
            internal static int GdipAddPathClosedCurve(IntPtr path, PointF[] points, int count) => GdipAddPathClosedCurve_ptr.Delegate(path, points, count);

            private delegate int GdipAddPathClosedCurveI_delegate(IntPtr path, Point[] points, int count);
            private static FunctionWrapper<GdipAddPathClosedCurveI_delegate> GdipAddPathClosedCurveI_ptr;
            internal static int GdipAddPathClosedCurveI(IntPtr path, Point[] points, int count) => GdipAddPathClosedCurveI_ptr.Delegate(path, points, count);

            private delegate int GdipAddPathClosedCurve2_delegate(IntPtr path, PointF[] points, int count, float tension);
            private static FunctionWrapper<GdipAddPathClosedCurve2_delegate> GdipAddPathClosedCurve2_ptr;
            internal static int GdipAddPathClosedCurve2(IntPtr path, PointF[] points, int count, float tension) => GdipAddPathClosedCurve2_ptr.Delegate(path, points, count, tension);

            private delegate int GdipAddPathClosedCurve2I_delegate(IntPtr path, Point[] points, int count, float tension);
            private static FunctionWrapper<GdipAddPathClosedCurve2I_delegate> GdipAddPathClosedCurve2I_ptr;
            internal static int GdipAddPathClosedCurve2I(IntPtr path, Point[] points, int count, float tension) => GdipAddPathClosedCurve2I_ptr.Delegate(path, points, count, tension);

            private delegate int GdipAddPathRectangle_delegate(IntPtr path, float x, float y, float width, float height);
            private static FunctionWrapper<GdipAddPathRectangle_delegate> GdipAddPathRectangle_ptr;
            internal static int GdipAddPathRectangle(IntPtr path, float x, float y, float width, float height) => GdipAddPathRectangle_ptr.Delegate(path, x, y, width, height);

            private delegate int GdipAddPathRectangles_delegate(IntPtr path, RectangleF[] rects, int count);
            private static FunctionWrapper<GdipAddPathRectangles_delegate> GdipAddPathRectangles_ptr;
            internal static int GdipAddPathRectangles(IntPtr path, RectangleF[] rects, int count) => GdipAddPathRectangles_ptr.Delegate(path, rects, count);

            private delegate int GdipAddPathEllipse_delegate(IntPtr path, float x, float y, float width, float height);
            private static FunctionWrapper<GdipAddPathEllipse_delegate> GdipAddPathEllipse_ptr;
            internal static int GdipAddPathEllipse(IntPtr path, float x, float y, float width, float height) => GdipAddPathEllipse_ptr.Delegate(path, x, y, width, height);

            private delegate int GdipAddPathEllipseI_delegate(IntPtr path, int x, int y, int width, int height);
            private static FunctionWrapper<GdipAddPathEllipseI_delegate> GdipAddPathEllipseI_ptr;
            internal static int GdipAddPathEllipseI(IntPtr path, int x, int y, int width, int height) => GdipAddPathEllipseI_ptr.Delegate(path, x, y, width, height);

            private delegate int GdipAddPathPie_delegate(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathPie_delegate> GdipAddPathPie_ptr;
            internal static int GdipAddPathPie(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipAddPathPie_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipAddPathPieI_delegate(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathPieI_delegate> GdipAddPathPieI_ptr;
            internal static int GdipAddPathPieI(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipAddPathPieI_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipAddPathPolygon_delegate(IntPtr path, PointF[] points, int count);
            private static FunctionWrapper<GdipAddPathPolygon_delegate> GdipAddPathPolygon_ptr;
            internal static int GdipAddPathPolygon(IntPtr path, PointF[] points, int count) => GdipAddPathPolygon_ptr.Delegate(path, points, count);

            private delegate int GdipAddPathPath_delegate(IntPtr path, IntPtr addingPath, bool connect);
            private static FunctionWrapper<GdipAddPathPath_delegate> GdipAddPathPath_ptr;
            internal static int GdipAddPathPath(IntPtr path, IntPtr addingPath, bool connect) => GdipAddPathPath_ptr.Delegate(path, addingPath, connect);

            private delegate int GdipAddPathLineI_delegate(IntPtr path, int x1, int y1, int x2, int y2);
            private static FunctionWrapper<GdipAddPathLineI_delegate> GdipAddPathLineI_ptr;
            internal static int GdipAddPathLineI(IntPtr path, int x1, int y1, int x2, int y2) => GdipAddPathLineI_ptr.Delegate(path, x1, y1, x2, y2);

            private delegate int GdipAddPathArcI_delegate(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathArcI_delegate> GdipAddPathArcI_ptr;
            internal static int GdipAddPathArcI(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipAddPathArcI_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipAddPathBezierI_delegate(IntPtr path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);
            private static FunctionWrapper<GdipAddPathBezierI_delegate> GdipAddPathBezierI_ptr;
            internal static int GdipAddPathBezierI(IntPtr path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4) => GdipAddPathBezierI_ptr.Delegate(path, x1, y1, x2, y2, x3, y3, x4, y4);

            private delegate int GdipAddPathBeziersI_delegate(IntPtr path, Point[] points, int count);
            private static FunctionWrapper<GdipAddPathBeziersI_delegate> GdipAddPathBeziersI_ptr;
            internal static int GdipAddPathBeziersI(IntPtr path, Point[] points, int count) => GdipAddPathBeziersI_ptr.Delegate(path, points, count);

            private delegate int GdipAddPathPolygonI_delegate(IntPtr path, Point[] points, int count);
            private static FunctionWrapper<GdipAddPathPolygonI_delegate> GdipAddPathPolygonI_ptr;
            internal static int GdipAddPathPolygonI(IntPtr path, Point[] points, int count) => GdipAddPathPolygonI_ptr.Delegate(path, points, count);

            private delegate int GdipAddPathRectangleI_delegate(IntPtr path, int x, int y, int width, int height);
            private static FunctionWrapper<GdipAddPathRectangleI_delegate> GdipAddPathRectangleI_ptr;
            internal static int GdipAddPathRectangleI(IntPtr path, int x, int y, int width, int height) => GdipAddPathRectangleI_ptr.Delegate(path, x, y, width, height);

            private delegate int GdipAddPathRectanglesI_delegate(IntPtr path, Rectangle[] rects, int count);
            private static FunctionWrapper<GdipAddPathRectanglesI_delegate> GdipAddPathRectanglesI_ptr;
            internal static int GdipAddPathRectanglesI(IntPtr path, Rectangle[] rects, int count) => GdipAddPathRectanglesI_ptr.Delegate(path, rects, count);

            private delegate int GdipFlattenPath_delegate(IntPtr path, IntPtr matrix, float floatness);
            private static FunctionWrapper<GdipFlattenPath_delegate> GdipFlattenPath_ptr;
            internal static int GdipFlattenPath(IntPtr path, IntPtr matrix, float floatness) => GdipFlattenPath_ptr.Delegate(path, matrix, floatness);

            private delegate int GdipTransformPath_delegate(IntPtr path, IntPtr matrix);
            private static FunctionWrapper<GdipTransformPath_delegate> GdipTransformPath_ptr;
            internal static int GdipTransformPath(IntPtr path, IntPtr matrix) => GdipTransformPath_ptr.Delegate(path, matrix);

            private delegate int GdipWarpPath_delegate(IntPtr path, IntPtr matrix, PointF[] points, int count, float srcx, float srcy, float srcwidth, float srcheight, WarpMode mode, float flatness);
            private static FunctionWrapper<GdipWarpPath_delegate> GdipWarpPath_ptr;
            internal static int GdipWarpPath(IntPtr path, IntPtr matrix, PointF[] points, int count, float srcx, float srcy, float srcwidth, float srcheight, WarpMode mode, float flatness) => GdipWarpPath_ptr.Delegate(path, matrix, points, count, srcx, srcy, srcwidth, srcheight, mode, flatness);

            private delegate int GdipWidenPath_delegate(IntPtr path, IntPtr pen, IntPtr matrix, float flatness);
            private static FunctionWrapper<GdipWidenPath_delegate> GdipWidenPath_ptr;
            internal static int GdipWidenPath(IntPtr path, IntPtr pen, IntPtr matrix, float flatness) => GdipWidenPath_ptr.Delegate(path, pen, matrix, flatness);

            private delegate int GdipGetPathWorldBounds_delegate(IntPtr path, out RectangleF bounds, IntPtr matrix, IntPtr pen);
            private static FunctionWrapper<GdipGetPathWorldBounds_delegate> GdipGetPathWorldBounds_ptr;
            internal static int GdipGetPathWorldBounds(IntPtr path, out RectangleF bounds, IntPtr matrix, IntPtr pen) => GdipGetPathWorldBounds_ptr.Delegate(path, out bounds, matrix, pen);

            private delegate int GdipGetPathWorldBoundsI_delegate(IntPtr path, out Rectangle bounds, IntPtr matrix, IntPtr pen);
            private static FunctionWrapper<GdipGetPathWorldBoundsI_delegate> GdipGetPathWorldBoundsI_ptr;
            internal static int GdipGetPathWorldBoundsI(IntPtr path, out Rectangle bounds, IntPtr matrix, IntPtr pen) => GdipGetPathWorldBoundsI_ptr.Delegate(path, out bounds, matrix, pen);

            private delegate int GdipIsVisiblePathPoint_delegate(IntPtr path, float x, float y, IntPtr graphics, out bool result);
            private static FunctionWrapper<GdipIsVisiblePathPoint_delegate> GdipIsVisiblePathPoint_ptr;
            internal static int GdipIsVisiblePathPoint(IntPtr path, float x, float y, IntPtr graphics, out bool result) => GdipIsVisiblePathPoint_ptr.Delegate(path, x, y, graphics, out result);

            private delegate int GdipIsVisiblePathPointI_delegate(IntPtr path, int x, int y, IntPtr graphics, out bool result);
            private static FunctionWrapper<GdipIsVisiblePathPointI_delegate> GdipIsVisiblePathPointI_ptr;
            internal static int GdipIsVisiblePathPointI(IntPtr path, int x, int y, IntPtr graphics, out bool result) => GdipIsVisiblePathPointI_ptr.Delegate(path, x, y, graphics, out result);

            private delegate int GdipIsOutlineVisiblePathPoint_delegate(IntPtr path, float x, float y, IntPtr pen, IntPtr graphics, out bool result);
            private static FunctionWrapper<GdipIsOutlineVisiblePathPoint_delegate> GdipIsOutlineVisiblePathPoint_ptr;
            internal static int GdipIsOutlineVisiblePathPoint(IntPtr path, float x, float y, IntPtr pen, IntPtr graphics, out bool result) => GdipIsOutlineVisiblePathPoint_ptr.Delegate(path, x, y, pen, graphics, out result);

            private delegate int GdipIsOutlineVisiblePathPointI_delegate(IntPtr path, int x, int y, IntPtr pen, IntPtr graphics, out bool result);
            private static FunctionWrapper<GdipIsOutlineVisiblePathPointI_delegate> GdipIsOutlineVisiblePathPointI_ptr;
            internal static int GdipIsOutlineVisiblePathPointI(IntPtr path, int x, int y, IntPtr pen, IntPtr graphics, out bool result) => GdipIsOutlineVisiblePathPointI_ptr.Delegate(path, x, y, pen, graphics, out result);

            private delegate int GdipCreateFont_delegate(IntPtr fontFamily, float emSize, FontStyle style, GraphicsUnit unit, out IntPtr font);
            private static FunctionWrapper<GdipCreateFont_delegate> GdipCreateFont_ptr;
            internal static int GdipCreateFont(IntPtr fontFamily, float emSize, FontStyle style, GraphicsUnit unit, out IntPtr font) => GdipCreateFont_ptr.Delegate(fontFamily, emSize, style, unit, out font);

            private delegate int GdipDeleteFont_delegate(IntPtr font);
            private static FunctionWrapper<GdipDeleteFont_delegate> GdipDeleteFont_ptr;
            internal static int GdipDeleteFont(IntPtr font) => GdipDeleteFont_ptr.Delegate(font);
            internal static int IntGdipDeleteFont(HandleRef font) => (int)GdipDeleteFont_ptr.Delegate(font.Handle);

#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
            private delegate int GdipGetLogFont_delegate(IntPtr font, IntPtr graphics, [MarshalAs(UnmanagedType.AsAny), Out] object logfontA);
#pragma warning restore CS0618
            private static FunctionWrapper<GdipGetLogFont_delegate> GdipGetLogFont_ptr;
            internal static int GdipGetLogFont(IntPtr font, IntPtr graphics, [Out] object logfontA) => GdipGetLogFont_ptr.Delegate(font, graphics, logfontA);

            private delegate int GdipCreateFontFromDC_delegate(IntPtr hdc, out IntPtr font);
            private static FunctionWrapper<GdipCreateFontFromDC_delegate> GdipCreateFontFromDC_ptr;
            internal static int GdipCreateFontFromDC(IntPtr hdc, out IntPtr font) => GdipCreateFontFromDC_ptr.Delegate(hdc, out font);

            private delegate int GdipCreateFontFromLogfont_delegate(IntPtr hdc, ref LOGFONT lf, out IntPtr ptr);
            private static FunctionWrapper<GdipCreateFontFromLogfont_delegate> GdipCreateFontFromLogfont_ptr;
            internal static int GdipCreateFontFromLogfont(IntPtr hdc, ref LOGFONT lf, out IntPtr ptr) => GdipCreateFontFromLogfont_ptr.Delegate(hdc, ref lf, out ptr);

            private delegate int GdipCreateFontFromHfont_delegate(IntPtr hdc, out IntPtr font, ref LOGFONT lf);
            private static FunctionWrapper<GdipCreateFontFromHfont_delegate> GdipCreateFontFromHfont_ptr;
            internal static int GdipCreateFontFromHfont(IntPtr hdc, out IntPtr font, ref LOGFONT lf) => GdipCreateFontFromHfont_ptr.Delegate(hdc, out font, ref lf);

            private delegate int GdipGetFontSize_delegate(IntPtr font, out float size);
            private static FunctionWrapper<GdipGetFontSize_delegate> GdipGetFontSize_ptr;
            internal static int GdipGetFontSize(IntPtr font, out float size) => GdipGetFontSize_ptr.Delegate(font, out size);

            private delegate int GdipGetFontHeight_delegate(IntPtr font, IntPtr graphics, out float height);
            private static FunctionWrapper<GdipGetFontHeight_delegate> GdipGetFontHeight_ptr;
            internal static int GdipGetFontHeight(IntPtr font, IntPtr graphics, out float height) => GdipGetFontHeight_ptr.Delegate(font, graphics, out height);

            private delegate int GdipGetFontHeightGivenDPI_delegate(IntPtr font, float dpi, out float height);
            private static FunctionWrapper<GdipGetFontHeightGivenDPI_delegate> GdipGetFontHeightGivenDPI_ptr;
            internal static int GdipGetFontHeightGivenDPI(IntPtr font, float dpi, out float height) => GdipGetFontHeightGivenDPI_ptr.Delegate(font, dpi, out height);

            private delegate int GdipCreateMetafileFromFile_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromFile_delegate> GdipCreateMetafileFromFile_ptr;
            internal static int GdipCreateMetafileFromFile(string filename, out IntPtr metafile) => GdipCreateMetafileFromFile_ptr.Delegate(filename, out metafile);

            private delegate int GdipCreateMetafileFromEmf_delegate(IntPtr hEmf, bool deleteEmf, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromEmf_delegate> GdipCreateMetafileFromEmf_ptr;
            internal static int GdipCreateMetafileFromEmf(IntPtr hEmf, bool deleteEmf, out IntPtr metafile) => GdipCreateMetafileFromEmf_ptr.Delegate(hEmf, deleteEmf, out metafile);

            private delegate int GdipCreateMetafileFromWmf_delegate(IntPtr hWmf, bool deleteWmf, WmfPlaceableFileHeader wmfPlaceableFileHeader, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromWmf_delegate> GdipCreateMetafileFromWmf_ptr;
            internal static int GdipCreateMetafileFromWmf(IntPtr hWmf, bool deleteWmf, WmfPlaceableFileHeader wmfPlaceableFileHeader, out IntPtr metafile) => GdipCreateMetafileFromWmf_ptr.Delegate(hWmf, deleteWmf, wmfPlaceableFileHeader, out metafile);

            private delegate int GdipGetMetafileHeaderFromFile_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromFile_delegate> GdipGetMetafileHeaderFromFile_ptr;
            internal static int GdipGetMetafileHeaderFromFile(string filename, IntPtr header) => GdipGetMetafileHeaderFromFile_ptr.Delegate(filename, header);

            private delegate int GdipGetMetafileHeaderFromMetafile_delegate(IntPtr metafile, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromMetafile_delegate> GdipGetMetafileHeaderFromMetafile_ptr;
            internal static int GdipGetMetafileHeaderFromMetafile(IntPtr metafile, IntPtr header) => GdipGetMetafileHeaderFromMetafile_ptr.Delegate(metafile, header);

            private delegate int GdipGetMetafileHeaderFromEmf_delegate(IntPtr hEmf, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromEmf_delegate> GdipGetMetafileHeaderFromEmf_ptr;
            internal static int GdipGetMetafileHeaderFromEmf(IntPtr hEmf, IntPtr header) => GdipGetMetafileHeaderFromEmf_ptr.Delegate(hEmf, header);

            private delegate int GdipGetMetafileHeaderFromWmf_delegate(IntPtr hWmf, IntPtr wmfPlaceableFileHeader, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromWmf_delegate> GdipGetMetafileHeaderFromWmf_ptr;
            internal static int GdipGetMetafileHeaderFromWmf(IntPtr hWmf, IntPtr wmfPlaceableFileHeader, IntPtr header) => GdipGetMetafileHeaderFromWmf_ptr.Delegate(hWmf, wmfPlaceableFileHeader, header);

            private delegate int GdipGetHemfFromMetafile_delegate(IntPtr metafile, out IntPtr hEmf);
            private static FunctionWrapper<GdipGetHemfFromMetafile_delegate> GdipGetHemfFromMetafile_ptr;
            internal static int GdipGetHemfFromMetafile(IntPtr metafile, out IntPtr hEmf) => GdipGetHemfFromMetafile_ptr.Delegate(metafile, out hEmf);

            private delegate int GdipGetMetafileDownLevelRasterizationLimit_delegate(IntPtr metafile, ref uint metafileRasterizationLimitDpi);
            private static FunctionWrapper<GdipGetMetafileDownLevelRasterizationLimit_delegate> GdipGetMetafileDownLevelRasterizationLimit_ptr;
            internal static int GdipGetMetafileDownLevelRasterizationLimit(IntPtr metafile, ref uint metafileRasterizationLimitDpi) => GdipGetMetafileDownLevelRasterizationLimit_ptr.Delegate(metafile, ref metafileRasterizationLimitDpi);

            private delegate int GdipSetMetafileDownLevelRasterizationLimit_delegate(IntPtr metafile, uint metafileRasterizationLimitDpi);
            private static FunctionWrapper<GdipSetMetafileDownLevelRasterizationLimit_delegate> GdipSetMetafileDownLevelRasterizationLimit_ptr;
            internal static int GdipSetMetafileDownLevelRasterizationLimit(IntPtr metafile, uint metafileRasterizationLimitDpi) => GdipSetMetafileDownLevelRasterizationLimit_ptr.Delegate(metafile, metafileRasterizationLimitDpi);

            private delegate int GdipPlayMetafileRecord_delegate(IntPtr metafile, EmfPlusRecordType recordType, int flags, int dataSize, byte[] data);
            private static FunctionWrapper<GdipPlayMetafileRecord_delegate> GdipPlayMetafileRecord_ptr;
            internal static int GdipPlayMetafileRecord(IntPtr metafile, EmfPlusRecordType recordType, int flags, int dataSize, byte[] data) => GdipPlayMetafileRecord_ptr.Delegate(metafile, recordType, flags, dataSize, data);

            private delegate int GdipRecordMetafile_delegate(IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafile_delegate> GdipRecordMetafile_ptr;
            internal static int GdipRecordMetafile(IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafile_ptr.Delegate(hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate int GdipRecordMetafileI_delegate(IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileI_delegate> GdipRecordMetafileI_ptr;
            internal static int GdipRecordMetafileI(IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileI_ptr.Delegate(hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate int GdipRecordMetafileFileName_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileFileName_delegate> GdipRecordMetafileFileName_ptr;
            internal static int GdipRecordMetafileFileName(string filename, IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileFileName_ptr.Delegate(filename, hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate int GdipRecordMetafileFileNameI_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileFileNameI_delegate> GdipRecordMetafileFileNameI_ptr;
            internal static int GdipRecordMetafileFileNameI(string filename, IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileFileNameI_ptr.Delegate(filename, hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate int GdipCreateFromContext_macosx_delegate(IntPtr cgref, int width, int height, out IntPtr graphics);
            private static FunctionWrapper<GdipCreateFromContext_macosx_delegate> GdipCreateFromContext_macosx_ptr;
            internal static int GdipCreateFromContext_macosx(IntPtr cgref, int width, int height, out IntPtr graphics) => GdipCreateFromContext_macosx_ptr.Delegate(cgref, width, height, out graphics);

            private delegate int GdipSetVisibleClip_linux_delegate(IntPtr graphics, ref Rectangle rect);
            private static FunctionWrapper<GdipSetVisibleClip_linux_delegate> GdipSetVisibleClip_linux_ptr;
            internal static int GdipSetVisibleClip_linux(IntPtr graphics, ref Rectangle rect) => GdipSetVisibleClip_linux_ptr.Delegate(graphics, ref rect);

            private delegate int GdipCreateFromXDrawable_linux_delegate(IntPtr drawable, IntPtr display, out IntPtr graphics);
            private static FunctionWrapper<GdipCreateFromXDrawable_linux_delegate> GdipCreateFromXDrawable_linux_ptr;
            internal static int GdipCreateFromXDrawable_linux(IntPtr drawable, IntPtr display, out IntPtr graphics) => GdipCreateFromXDrawable_linux_ptr.Delegate(drawable, display, out graphics);

            // Stream functions for non-Win32 (libgdiplus specific)
            private delegate int GdipLoadImageFromDelegate_linux_delegate(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr image);
            private static FunctionWrapper<GdipLoadImageFromDelegate_linux_delegate> GdipLoadImageFromDelegate_linux_ptr;
            internal static int GdipLoadImageFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr image)
                => GdipLoadImageFromDelegate_linux_ptr.Delegate(getHeader, getBytes, putBytes, doSeek, close, size, out image);

            private delegate int GdipSaveImageToDelegate_linux_delegate(IntPtr image, StreamGetBytesDelegate getBytes,
                StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek, StreamCloseDelegate close,
                StreamSizeDelegate size, ref Guid encoderClsID, IntPtr encoderParameters);
            private static FunctionWrapper<GdipSaveImageToDelegate_linux_delegate> GdipSaveImageToDelegate_linux_ptr;
            internal static int GdipSaveImageToDelegate_linux(IntPtr image, StreamGetBytesDelegate getBytes,
                StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek, StreamCloseDelegate close,
                StreamSizeDelegate size, ref Guid encoderClsID, IntPtr encoderParameters)
                => GdipSaveImageToDelegate_linux_ptr.Delegate(image, getBytes, putBytes, doSeek, close, size, ref encoderClsID, encoderParameters);

            private delegate int GdipCreateMetafileFromDelegate_linux_delegate(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromDelegate_linux_delegate> GdipCreateMetafileFromDelegate_linux_ptr;
            internal static int GdipCreateMetafileFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr metafile)
                => GdipCreateMetafileFromDelegate_linux_ptr.Delegate(getHeader, getBytes, putBytes, doSeek, close, size, out metafile);

            private delegate int GdipGetMetafileHeaderFromDelegate_linux_delegate(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromDelegate_linux_delegate> GdipGetMetafileHeaderFromDelegate_linux_ptr;
            internal static int GdipGetMetafileHeaderFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr header)
                => GdipGetMetafileHeaderFromDelegate_linux_ptr.Delegate(getHeader, getBytes, putBytes, doSeek, close, size, header);

            private delegate int GdipRecordMetafileFromDelegate_linux_delegate(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref RectangleF frameRect,
                MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)] string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileFromDelegate_linux_delegate> GdipRecordMetafileFromDelegate_linux_ptr;
            internal static int GdipRecordMetafileFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref RectangleF frameRect,
                MetafileFrameUnit frameUnit, string description, out IntPtr metafile)
                => GdipRecordMetafileFromDelegate_linux_ptr.Delegate(getHeader, getBytes, putBytes, doSeek, close, size, hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate int GdipRecordMetafileFromDelegateI_linux_delegate(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref Rectangle frameRect,
                MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)] string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileFromDelegateI_linux_delegate> GdipRecordMetafileFromDelegateI_linux_ptr;
            internal static int GdipRecordMetafileFromDelegateI_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref Rectangle frameRect,
                MetafileFrameUnit frameUnit, string description, out IntPtr metafile)
                => GdipRecordMetafileFromDelegateI_linux_ptr.Delegate(getHeader, getBytes, putBytes, doSeek, close, size, hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate int GdipGetPostScriptGraphicsContext_delegate(string filename, int width, int height, double dpix, double dpiy, ref IntPtr graphics);
            private static FunctionWrapper<GdipGetPostScriptGraphicsContext_delegate> GdipGetPostScriptGraphicsContext_ptr;
            internal static int GdipGetPostScriptGraphicsContext([MarshalAs(UnmanagedType.LPStr)]string filename, int width, int height, double dpix, double dpiy, ref IntPtr graphics)
                => GdipGetPostScriptGraphicsContext_ptr.Delegate(filename, width, height, dpix, dpiy, ref graphics);

            private delegate int GdipGetPostScriptSavePage_delegate(IntPtr graphics);
            private static FunctionWrapper<GdipGetPostScriptSavePage_delegate> GdipGetPostScriptSavePage_ptr;
            internal static int GdipGetPostScriptSavePage(IntPtr graphics) => GdipGetPostScriptSavePage_ptr.Delegate(graphics);
        }
    }

    // These are unix-only
    internal delegate int StreamGetHeaderDelegate(IntPtr buf, int bufsz);
    internal delegate int StreamGetBytesDelegate(IntPtr buf, int bufsz, bool peek);
    internal delegate long StreamSeekDelegate(int offset, int whence);
    internal delegate int StreamPutBytesDelegate(IntPtr buf, int bufsz);
    internal delegate void StreamCloseDelegate();
    internal delegate long StreamSizeDelegate();
}
