// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace System.Drawing
{
    internal partial class SafeNativeMethods
    {
        internal unsafe partial class Gdip
        {
            private static IntPtr LoadNativeLibrary()
            {
                // Various Unix package managers have chosen different names for the "libgdiplus" shared library.
                // The mono project, where libgdiplus originated, allowed both of the names below to be used, via
                // a global configuration setting. We prefer the "unversioned" shared object name, and fallback to
                // the name suffixed with ".0".
                IntPtr lib = Interop.Libdl.dlopen("libgdiplus.so", Interop.Libdl.RTLD_NOW);
                if (lib == IntPtr.Zero)
                {
                    lib = Interop.Libdl.dlopen("libgdiplus.so.0", Interop.Libdl.RTLD_NOW);
                    if (lib == IntPtr.Zero)
                    {
                        throw new DllNotFoundException(SR.LibgdiplusNotFound);
                    }
                }

                return lib;
            }

            private static IntPtr LoadFunctionPointer(IntPtr nativeLibraryHandle, string functionName) => Interop.Libdl.dlsym(nativeLibraryHandle, functionName);

            internal static void CheckStatus(Status status) => GDIPlus.CheckStatus(status);

            private static void LoadPlatformFunctionPointers()
            {
                GdiplusStartup_ptr = LoadFunction<GdiplusStartup_delegate>("GdiplusStartup");
                GdiplusShutdown_ptr = LoadFunction<GdiplusShutdown_delegate>("GdiplusShutdown");
                GdipAlloc_ptr = LoadFunction<GdipAlloc_delegate>("GdipAlloc");
                GdipFree_ptr = LoadFunction<GdipFree_delegate>("GdipFree");
                GdipDeleteBrush_ptr = LoadFunction<GdipDeleteBrush_delegate>("GdipDeleteBrush");
                GdipGetBrushType_ptr = LoadFunction<GdipGetBrushType_delegate>("GdipGetBrushType");
                GdipCreateFromHDC_ptr = LoadFunction<GdipCreateFromHDC_delegate>("GdipCreateFromHDC");
                GdipDeleteGraphics_ptr = LoadFunction<GdipDeleteGraphics_delegate>("GdipDeleteGraphics");
                GdipRestoreGraphics_ptr = LoadFunction<GdipRestoreGraphics_delegate>("GdipRestoreGraphics");
                GdipSaveGraphics_ptr = LoadFunction<GdipSaveGraphics_delegate>("GdipSaveGraphics");
                GdipDrawArc_ptr = LoadFunction<GdipDrawArc_delegate>("GdipDrawArc");
                GdipDrawArcI_ptr = LoadFunction<GdipDrawArcI_delegate>("GdipDrawArcI");
                GdipDrawBezier_ptr = LoadFunction<GdipDrawBezier_delegate>("GdipDrawBezier");
                GdipDrawBezierI_ptr = LoadFunction<GdipDrawBezierI_delegate>("GdipDrawBezierI");
                GdipDrawEllipseI_ptr = LoadFunction<GdipDrawEllipseI_delegate>("GdipDrawEllipseI");
                GdipDrawEllipse_ptr = LoadFunction<GdipDrawEllipse_delegate>("GdipDrawEllipse");
                GdipDrawLine_ptr = LoadFunction<GdipDrawLine_delegate>("GdipDrawLine");
                GdipDrawLineI_ptr = LoadFunction<GdipDrawLineI_delegate>("GdipDrawLineI");
                GdipDrawLines_ptr = LoadFunction<GdipDrawLines_delegate>("GdipDrawLines");
                GdipDrawLinesI_ptr = LoadFunction<GdipDrawLinesI_delegate>("GdipDrawLinesI");
                GdipDrawPath_ptr = LoadFunction<GdipDrawPath_delegate>("GdipDrawPath");
                GdipDrawPie_ptr = LoadFunction<GdipDrawPie_delegate>("GdipDrawPie");
                GdipDrawPieI_ptr = LoadFunction<GdipDrawPieI_delegate>("GdipDrawPieI");
                GdipDrawPolygon_ptr = LoadFunction<GdipDrawPolygon_delegate>("GdipDrawPolygon");
                GdipDrawPolygonI_ptr = LoadFunction<GdipDrawPolygonI_delegate>("GdipDrawPolygonI");
                GdipDrawRectangle_ptr = LoadFunction<GdipDrawRectangle_delegate>("GdipDrawRectangle");
                GdipDrawRectangleI_ptr = LoadFunction<GdipDrawRectangleI_delegate>("GdipDrawRectangleI");
                GdipDrawRectangles_ptr = LoadFunction<GdipDrawRectangles_delegate>("GdipDrawRectangles");
                GdipDrawRectanglesI_ptr = LoadFunction<GdipDrawRectanglesI_delegate>("GdipDrawRectanglesI");
                GdipFillEllipseI_ptr = LoadFunction<GdipFillEllipseI_delegate>("GdipFillEllipseI");
                GdipFillEllipse_ptr = LoadFunction<GdipFillEllipse_delegate>("GdipFillEllipse");
                GdipFillPolygon_ptr = LoadFunction<GdipFillPolygon_delegate>("GdipFillPolygon");
                GdipFillPolygonI_ptr = LoadFunction<GdipFillPolygonI_delegate>("GdipFillPolygonI");
                GdipFillPolygon2_ptr = LoadFunction<GdipFillPolygon2_delegate>("GdipFillPolygon2");
                GdipFillPolygon2I_ptr = LoadFunction<GdipFillPolygon2I_delegate>("GdipFillPolygon2I");
                GdipFillRectangle_ptr = LoadFunction<GdipFillRectangle_delegate>("GdipFillRectangle");
                GdipFillRectangleI_ptr = LoadFunction<GdipFillRectangleI_delegate>("GdipFillRectangleI");
                GdipFillRectangles_ptr = LoadFunction<GdipFillRectangles_delegate>("GdipFillRectangles");
                GdipFillRectanglesI_ptr = LoadFunction<GdipFillRectanglesI_delegate>("GdipFillRectanglesI");
                GdipDrawString_ptr = LoadFunction<GdipDrawString_delegate>("GdipDrawString");
                GdipGetDC_ptr = LoadFunction<GdipGetDC_delegate>("GdipGetDC");
                GdipReleaseDC_ptr = LoadFunction<GdipReleaseDC_delegate>("GdipReleaseDC");
                GdipDrawImageRectI_ptr = LoadFunction<GdipDrawImageRectI_delegate>("GdipDrawImageRectI");
                GdipGetRenderingOrigin_ptr = LoadFunction<GdipGetRenderingOrigin_delegate>("GdipGetRenderingOrigin");
                GdipSetRenderingOrigin_ptr = LoadFunction<GdipSetRenderingOrigin_delegate>("GdipSetRenderingOrigin");
                GdipCloneBitmapArea_ptr = LoadFunction<GdipCloneBitmapArea_delegate>("GdipCloneBitmapArea");
                GdipCloneBitmapAreaI_ptr = LoadFunction<GdipCloneBitmapAreaI_delegate>("GdipCloneBitmapAreaI");
                GdipGraphicsClear_ptr = LoadFunction<GdipGraphicsClear_delegate>("GdipGraphicsClear");
                GdipDrawClosedCurve_ptr = LoadFunction<GdipDrawClosedCurve_delegate>("GdipDrawClosedCurve");
                GdipDrawClosedCurveI_ptr = LoadFunction<GdipDrawClosedCurveI_delegate>("GdipDrawClosedCurveI");
                GdipDrawClosedCurve2_ptr = LoadFunction<GdipDrawClosedCurve2_delegate>("GdipDrawClosedCurve2");
                GdipDrawClosedCurve2I_ptr = LoadFunction<GdipDrawClosedCurve2I_delegate>("GdipDrawClosedCurve2I");
                GdipDrawCurve_ptr = LoadFunction<GdipDrawCurve_delegate>("GdipDrawCurve");
                GdipDrawCurveI_ptr = LoadFunction<GdipDrawCurveI_delegate>("GdipDrawCurveI");
                GdipDrawCurve2_ptr = LoadFunction<GdipDrawCurve2_delegate>("GdipDrawCurve2");
                GdipDrawCurve2I_ptr = LoadFunction<GdipDrawCurve2I_delegate>("GdipDrawCurve2I");
                GdipDrawCurve3_ptr = LoadFunction<GdipDrawCurve3_delegate>("GdipDrawCurve3");
                GdipDrawCurve3I_ptr = LoadFunction<GdipDrawCurve3I_delegate>("GdipDrawCurve3I");
                GdipFillClosedCurve_ptr = LoadFunction<GdipFillClosedCurve_delegate>("GdipFillClosedCurve");
                GdipFillClosedCurveI_ptr = LoadFunction<GdipFillClosedCurveI_delegate>("GdipFillClosedCurveI");
                GdipFillClosedCurve2_ptr = LoadFunction<GdipFillClosedCurve2_delegate>("GdipFillClosedCurve2");
                GdipFillClosedCurve2I_ptr = LoadFunction<GdipFillClosedCurve2I_delegate>("GdipFillClosedCurve2I");
                GdipFillPie_ptr = LoadFunction<GdipFillPie_delegate>("GdipFillPie");
                GdipFillPieI_ptr = LoadFunction<GdipFillPieI_delegate>("GdipFillPieI");
                GdipFillPath_ptr = LoadFunction<GdipFillPath_delegate>("GdipFillPath");
                GdipGetNearestColor_ptr = LoadFunction<GdipGetNearestColor_delegate>("GdipGetNearestColor");
                GdipTransformPoints_ptr = LoadFunction<GdipTransformPoints_delegate>("GdipTransformPoints");
                GdipTransformPointsI_ptr = LoadFunction<GdipTransformPointsI_delegate>("GdipTransformPointsI");
                GdipSetCompositingMode_ptr = LoadFunction<GdipSetCompositingMode_delegate>("GdipSetCompositingMode");
                GdipGetCompositingMode_ptr = LoadFunction<GdipGetCompositingMode_delegate>("GdipGetCompositingMode");
                GdipSetCompositingQuality_ptr = LoadFunction<GdipSetCompositingQuality_delegate>("GdipSetCompositingQuality");
                GdipGetCompositingQuality_ptr = LoadFunction<GdipGetCompositingQuality_delegate>("GdipGetCompositingQuality");
                GdipSetInterpolationMode_ptr = LoadFunction<GdipSetInterpolationMode_delegate>("GdipSetInterpolationMode");
                GdipGetInterpolationMode_ptr = LoadFunction<GdipGetInterpolationMode_delegate>("GdipGetInterpolationMode");
                GdipGetDpiX_ptr = LoadFunction<GdipGetDpiX_delegate>("GdipGetDpiX");
                GdipGetDpiY_ptr = LoadFunction<GdipGetDpiY_delegate>("GdipGetDpiY");
                GdipGetPageUnit_ptr = LoadFunction<GdipGetPageUnit_delegate>("GdipGetPageUnit");
                GdipGetPageScale_ptr = LoadFunction<GdipGetPageScale_delegate>("GdipGetPageScale");
                GdipSetPageUnit_ptr = LoadFunction<GdipSetPageUnit_delegate>("GdipSetPageUnit");
                GdipSetPageScale_ptr = LoadFunction<GdipSetPageScale_delegate>("GdipSetPageScale");
                GdipSetPixelOffsetMode_ptr = LoadFunction<GdipSetPixelOffsetMode_delegate>("GdipSetPixelOffsetMode");
                GdipGetPixelOffsetMode_ptr = LoadFunction<GdipGetPixelOffsetMode_delegate>("GdipGetPixelOffsetMode");
                GdipSetSmoothingMode_ptr = LoadFunction<GdipSetSmoothingMode_delegate>("GdipSetSmoothingMode");
                GdipGetSmoothingMode_ptr = LoadFunction<GdipGetSmoothingMode_delegate>("GdipGetSmoothingMode");
                GdipSetTextContrast_ptr = LoadFunction<GdipSetTextContrast_delegate>("GdipSetTextContrast");
                GdipGetTextContrast_ptr = LoadFunction<GdipGetTextContrast_delegate>("GdipGetTextContrast");
                GdipSetTextRenderingHint_ptr = LoadFunction<GdipSetTextRenderingHint_delegate>("GdipSetTextRenderingHint");
                GdipGetTextRenderingHint_ptr = LoadFunction<GdipGetTextRenderingHint_delegate>("GdipGetTextRenderingHint");
                GdipFlush_ptr = LoadFunction<GdipFlush_delegate>("GdipFlush");
                GdipAddPathString_ptr = LoadFunction<GdipAddPathString_delegate>("GdipAddPathString");
                GdipAddPathStringI_ptr = LoadFunction<GdipAddPathStringI_delegate>("GdipAddPathStringI");
                GdipCreateFromHWND_ptr = LoadFunction<GdipCreateFromHWND_delegate>("GdipCreateFromHWND");
                GdipMeasureString_ptr = LoadFunction<GdipMeasureString_delegate>("GdipMeasureString");
                GdipMeasureCharacterRanges_ptr = LoadFunction<GdipMeasureCharacterRanges_delegate>("GdipMeasureCharacterRanges");
                GdipCreateBitmapFromScan0_ptr = LoadFunction<GdipCreateBitmapFromScan0_delegate>("GdipCreateBitmapFromScan0");
                GdipCreateBitmapFromGraphics_ptr = LoadFunction<GdipCreateBitmapFromGraphics_delegate>("GdipCreateBitmapFromGraphics");
                GdipBitmapLockBits_ptr = LoadFunction<GdipBitmapLockBits_delegate>("GdipBitmapLockBits");
                GdipBitmapSetResolution_ptr = LoadFunction<GdipBitmapSetResolution_delegate>("GdipBitmapSetResolution");
                GdipBitmapUnlockBits_ptr = LoadFunction<GdipBitmapUnlockBits_delegate>("GdipBitmapUnlockBits");
                GdipBitmapGetPixel_ptr = LoadFunction<GdipBitmapGetPixel_delegate>("GdipBitmapGetPixel");
                GdipBitmapSetPixel_ptr = LoadFunction<GdipBitmapSetPixel_delegate>("GdipBitmapSetPixel");
                GdipLoadImageFromFile_ptr = LoadFunction<GdipLoadImageFromFile_delegate>("GdipLoadImageFromFile");
                GdipLoadImageFromStream_ptr = LoadFunction<GdipLoadImageFromStream_delegate>("GdipLoadImageFromStream");
                GdipSaveImageToStream_ptr = LoadFunction<GdipSaveImageToStream_delegate>("GdipSaveImageToStream");
                GdipCloneImage_ptr = LoadFunction<GdipCloneImage_delegate>("GdipCloneImage");
                GdipLoadImageFromFileICM_ptr = LoadFunction<GdipLoadImageFromFileICM_delegate>("GdipLoadImageFromFileICM");
                GdipCreateBitmapFromHBITMAP_ptr = LoadFunction<GdipCreateBitmapFromHBITMAP_delegate>("GdipCreateBitmapFromHBITMAP");
                GdipDisposeImage_ptr = LoadFunction<GdipDisposeImage_delegate>("GdipDisposeImage");
                GdipGetImageFlags_ptr = LoadFunction<GdipGetImageFlags_delegate>("GdipGetImageFlags");
                GdipGetImageType_ptr = LoadFunction<GdipGetImageType_delegate>("GdipGetImageType");
                GdipImageGetFrameDimensionsCount_ptr = LoadFunction<GdipImageGetFrameDimensionsCount_delegate>("GdipImageGetFrameDimensionsCount");
                GdipImageGetFrameDimensionsList_ptr = LoadFunction<GdipImageGetFrameDimensionsList_delegate>("GdipImageGetFrameDimensionsList");
                GdipGetImageHeight_ptr = LoadFunction<GdipGetImageHeight_delegate>("GdipGetImageHeight");
                GdipGetImageHorizontalResolution_ptr = LoadFunction<GdipGetImageHorizontalResolution_delegate>("GdipGetImageHorizontalResolution");
                GdipGetImagePaletteSize_ptr = LoadFunction<GdipGetImagePaletteSize_delegate>("GdipGetImagePaletteSize");
                GdipGetImagePalette_ptr = LoadFunction<GdipGetImagePalette_delegate>("GdipGetImagePalette");
                GdipSetImagePalette_ptr = LoadFunction<GdipSetImagePalette_delegate>("GdipSetImagePalette");
                GdipGetImageDimension_ptr = LoadFunction<GdipGetImageDimension_delegate>("GdipGetImageDimension");
                GdipGetImagePixelFormat_ptr = LoadFunction<GdipGetImagePixelFormat_delegate>("GdipGetImagePixelFormat");
                GdipGetPropertyCount_ptr = LoadFunction<GdipGetPropertyCount_delegate>("GdipGetPropertyCount");
                GdipGetPropertyIdList_ptr = LoadFunction<GdipGetPropertyIdList_delegate>("GdipGetPropertyIdList");
                GdipGetPropertySize_ptr = LoadFunction<GdipGetPropertySize_delegate>("GdipGetPropertySize");
                GdipGetAllPropertyItems_ptr = LoadFunction<GdipGetAllPropertyItems_delegate>("GdipGetAllPropertyItems");
                GdipGetImageRawFormat_ptr = LoadFunction<GdipGetImageRawFormat_delegate>("GdipGetImageRawFormat");
                GdipGetImageVerticalResolution_ptr = LoadFunction<GdipGetImageVerticalResolution_delegate>("GdipGetImageVerticalResolution");
                GdipGetImageWidth_ptr = LoadFunction<GdipGetImageWidth_delegate>("GdipGetImageWidth");
                GdipGetImageBounds_ptr = LoadFunction<GdipGetImageBounds_delegate>("GdipGetImageBounds");
                GdipGetEncoderParameterListSize_ptr = LoadFunction<GdipGetEncoderParameterListSize_delegate>("GdipGetEncoderParameterListSize");
                GdipGetEncoderParameterList_ptr = LoadFunction<GdipGetEncoderParameterList_delegate>("GdipGetEncoderParameterList");
                GdipImageGetFrameCount_ptr = LoadFunction<GdipImageGetFrameCount_delegate>("GdipImageGetFrameCount");
                GdipImageSelectActiveFrame_ptr = LoadFunction<GdipImageSelectActiveFrame_delegate>("GdipImageSelectActiveFrame");
                GdipGetPropertyItemSize_ptr = LoadFunction<GdipGetPropertyItemSize_delegate>("GdipGetPropertyItemSize");
                GdipGetPropertyItem_ptr = LoadFunction<GdipGetPropertyItem_delegate>("GdipGetPropertyItem");
                GdipRemovePropertyItem_ptr = LoadFunction<GdipRemovePropertyItem_delegate>("GdipRemovePropertyItem");
                GdipSetPropertyItem_ptr = LoadFunction<GdipSetPropertyItem_delegate>("GdipSetPropertyItem");
                GdipGetImageThumbnail_ptr = LoadFunction<GdipGetImageThumbnail_delegate>("GdipGetImageThumbnail");
                GdipImageRotateFlip_ptr = LoadFunction<GdipImageRotateFlip_delegate>("GdipImageRotateFlip");
                GdipSaveImageToFile_ptr = LoadFunction<GdipSaveImageToFile_delegate>("GdipSaveImageToFile");
                GdipSaveAdd_ptr = LoadFunction<GdipSaveAdd_delegate>("GdipSaveAdd");
                GdipSaveAddImage_ptr = LoadFunction<GdipSaveAddImage_delegate>("GdipSaveAddImage");
                GdipDrawImageI_ptr = LoadFunction<GdipDrawImageI_delegate>("GdipDrawImageI");
                GdipGetImageGraphicsContext_ptr = LoadFunction<GdipGetImageGraphicsContext_delegate>("GdipGetImageGraphicsContext");
                GdipDrawImage_ptr = LoadFunction<GdipDrawImage_delegate>("GdipDrawImage");
                GdipDrawImagePoints_ptr = LoadFunction<GdipDrawImagePoints_delegate>("GdipDrawImagePoints");
                GdipDrawImagePointsI_ptr = LoadFunction<GdipDrawImagePointsI_delegate>("GdipDrawImagePointsI");
                GdipDrawImageRectRectI_ptr = LoadFunction<GdipDrawImageRectRectI_delegate>("GdipDrawImageRectRectI");
                GdipDrawImageRectRect_ptr = LoadFunction<GdipDrawImageRectRect_delegate>("GdipDrawImageRectRect");
                GdipDrawImagePointsRectI_ptr = LoadFunction<GdipDrawImagePointsRectI_delegate>("GdipDrawImagePointsRectI");
                GdipDrawImagePointsRect_ptr = LoadFunction<GdipDrawImagePointsRect_delegate>("GdipDrawImagePointsRect");
                GdipDrawImageRect_ptr = LoadFunction<GdipDrawImageRect_delegate>("GdipDrawImageRect");
                GdipDrawImagePointRect_ptr = LoadFunction<GdipDrawImagePointRect_delegate>("GdipDrawImagePointRect");
                GdipDrawImagePointRectI_ptr = LoadFunction<GdipDrawImagePointRectI_delegate>("GdipDrawImagePointRectI");
                GdipCreateHBITMAPFromBitmap_ptr = LoadFunction<GdipCreateHBITMAPFromBitmap_delegate>("GdipCreateHBITMAPFromBitmap");
                GdipCreateBitmapFromFile_ptr = LoadFunction<GdipCreateBitmapFromFile_delegate>("GdipCreateBitmapFromFile");
                GdipCreateBitmapFromFileICM_ptr = LoadFunction<GdipCreateBitmapFromFileICM_delegate>("GdipCreateBitmapFromFileICM");
                GdipCreateHICONFromBitmap_ptr = LoadFunction<GdipCreateHICONFromBitmap_delegate>("GdipCreateHICONFromBitmap");
                GdipCreateBitmapFromHICON_ptr = LoadFunction<GdipCreateBitmapFromHICON_delegate>("GdipCreateBitmapFromHICON");
                GdipCreateBitmapFromResource_ptr = LoadFunction<GdipCreateBitmapFromResource_delegate>("GdipCreateBitmapFromResource");
                GdipCreatePath_ptr = LoadFunction<GdipCreatePath_delegate>("GdipCreatePath");
                GdipCreatePath2_ptr = LoadFunction<GdipCreatePath2_delegate>("GdipCreatePath2");
                GdipCreatePath2I_ptr = LoadFunction<GdipCreatePath2I_delegate>("GdipCreatePath2I");
                GdipClonePath_ptr = LoadFunction<GdipClonePath_delegate>("GdipClonePath");
                GdipDeletePath_ptr = LoadFunction<GdipDeletePath_delegate>("GdipDeletePath");
                GdipResetPath_ptr = LoadFunction<GdipResetPath_delegate>("GdipResetPath");
                GdipGetPointCount_ptr = LoadFunction<GdipGetPointCount_delegate>("GdipGetPointCount");
                GdipGetPathTypes_ptr = LoadFunction<GdipGetPathTypes_delegate>("GdipGetPathTypes");
                GdipGetPathPoints_ptr = LoadFunction<GdipGetPathPoints_delegate>("GdipGetPathPoints");
                GdipGetPathPointsI_ptr = LoadFunction<GdipGetPathPointsI_delegate>("GdipGetPathPointsI");
                GdipGetPathFillMode_ptr = LoadFunction<GdipGetPathFillMode_delegate>("GdipGetPathFillMode");
                GdipSetPathFillMode_ptr = LoadFunction<GdipSetPathFillMode_delegate>("GdipSetPathFillMode");
                GdipStartPathFigure_ptr = LoadFunction<GdipStartPathFigure_delegate>("GdipStartPathFigure");
                GdipClosePathFigure_ptr = LoadFunction<GdipClosePathFigure_delegate>("GdipClosePathFigure");
                GdipClosePathFigures_ptr = LoadFunction<GdipClosePathFigures_delegate>("GdipClosePathFigures");
                GdipSetPathMarker_ptr = LoadFunction<GdipSetPathMarker_delegate>("GdipSetPathMarker");
                GdipClearPathMarkers_ptr = LoadFunction<GdipClearPathMarkers_delegate>("GdipClearPathMarkers");
                GdipReversePath_ptr = LoadFunction<GdipReversePath_delegate>("GdipReversePath");
                GdipGetPathLastPoint_ptr = LoadFunction<GdipGetPathLastPoint_delegate>("GdipGetPathLastPoint");
                GdipAddPathLine_ptr = LoadFunction<GdipAddPathLine_delegate>("GdipAddPathLine");
                GdipAddPathLine2_ptr = LoadFunction<GdipAddPathLine2_delegate>("GdipAddPathLine2");
                GdipAddPathLine2I_ptr = LoadFunction<GdipAddPathLine2I_delegate>("GdipAddPathLine2I");
                GdipAddPathArc_ptr = LoadFunction<GdipAddPathArc_delegate>("GdipAddPathArc");
                GdipAddPathBezier_ptr = LoadFunction<GdipAddPathBezier_delegate>("GdipAddPathBezier");
                GdipAddPathBeziers_ptr = LoadFunction<GdipAddPathBeziers_delegate>("GdipAddPathBeziers");
                GdipAddPathCurve_ptr = LoadFunction<GdipAddPathCurve_delegate>("GdipAddPathCurve");
                GdipAddPathCurveI_ptr = LoadFunction<GdipAddPathCurveI_delegate>("GdipAddPathCurveI");
                GdipAddPathCurve2_ptr = LoadFunction<GdipAddPathCurve2_delegate>("GdipAddPathCurve2");
                GdipAddPathCurve2I_ptr = LoadFunction<GdipAddPathCurve2I_delegate>("GdipAddPathCurve2I");
                GdipAddPathCurve3_ptr = LoadFunction<GdipAddPathCurve3_delegate>("GdipAddPathCurve3");
                GdipAddPathCurve3I_ptr = LoadFunction<GdipAddPathCurve3I_delegate>("GdipAddPathCurve3I");
                GdipAddPathClosedCurve_ptr = LoadFunction<GdipAddPathClosedCurve_delegate>("GdipAddPathClosedCurve");
                GdipAddPathClosedCurveI_ptr = LoadFunction<GdipAddPathClosedCurveI_delegate>("GdipAddPathClosedCurveI");
                GdipAddPathClosedCurve2_ptr = LoadFunction<GdipAddPathClosedCurve2_delegate>("GdipAddPathClosedCurve2");
                GdipAddPathClosedCurve2I_ptr = LoadFunction<GdipAddPathClosedCurve2I_delegate>("GdipAddPathClosedCurve2I");
                GdipAddPathRectangle_ptr = LoadFunction<GdipAddPathRectangle_delegate>("GdipAddPathRectangle");
                GdipAddPathRectangles_ptr = LoadFunction<GdipAddPathRectangles_delegate>("GdipAddPathRectangles");
                GdipAddPathEllipse_ptr = LoadFunction<GdipAddPathEllipse_delegate>("GdipAddPathEllipse");
                GdipAddPathEllipseI_ptr = LoadFunction<GdipAddPathEllipseI_delegate>("GdipAddPathEllipseI");
                GdipAddPathPie_ptr = LoadFunction<GdipAddPathPie_delegate>("GdipAddPathPie");
                GdipAddPathPieI_ptr = LoadFunction<GdipAddPathPieI_delegate>("GdipAddPathPieI");
                GdipAddPathPolygon_ptr = LoadFunction<GdipAddPathPolygon_delegate>("GdipAddPathPolygon");
                GdipAddPathPath_ptr = LoadFunction<GdipAddPathPath_delegate>("GdipAddPathPath");
                GdipAddPathLineI_ptr = LoadFunction<GdipAddPathLineI_delegate>("GdipAddPathLineI");
                GdipAddPathArcI_ptr = LoadFunction<GdipAddPathArcI_delegate>("GdipAddPathArcI");
                GdipAddPathBezierI_ptr = LoadFunction<GdipAddPathBezierI_delegate>("GdipAddPathBezierI");
                GdipAddPathBeziersI_ptr = LoadFunction<GdipAddPathBeziersI_delegate>("GdipAddPathBeziersI");
                GdipAddPathPolygonI_ptr = LoadFunction<GdipAddPathPolygonI_delegate>("GdipAddPathPolygonI");
                GdipAddPathRectangleI_ptr = LoadFunction<GdipAddPathRectangleI_delegate>("GdipAddPathRectangleI");
                GdipAddPathRectanglesI_ptr = LoadFunction<GdipAddPathRectanglesI_delegate>("GdipAddPathRectanglesI");
                GdipFlattenPath_ptr = LoadFunction<GdipFlattenPath_delegate>("GdipFlattenPath");
                GdipTransformPath_ptr = LoadFunction<GdipTransformPath_delegate>("GdipTransformPath");
                GdipWarpPath_ptr = LoadFunction<GdipWarpPath_delegate>("GdipWarpPath");
                GdipWidenPath_ptr = LoadFunction<GdipWidenPath_delegate>("GdipWidenPath");
                GdipGetPathWorldBounds_ptr = LoadFunction<GdipGetPathWorldBounds_delegate>("GdipGetPathWorldBounds");
                GdipGetPathWorldBoundsI_ptr = LoadFunction<GdipGetPathWorldBoundsI_delegate>("GdipGetPathWorldBoundsI");
                GdipIsVisiblePathPoint_ptr = LoadFunction<GdipIsVisiblePathPoint_delegate>("GdipIsVisiblePathPoint");
                GdipIsVisiblePathPointI_ptr = LoadFunction<GdipIsVisiblePathPointI_delegate>("GdipIsVisiblePathPointI");
                GdipIsOutlineVisiblePathPoint_ptr = LoadFunction<GdipIsOutlineVisiblePathPoint_delegate>("GdipIsOutlineVisiblePathPoint");
                GdipIsOutlineVisiblePathPointI_ptr = LoadFunction<GdipIsOutlineVisiblePathPointI_delegate>("GdipIsOutlineVisiblePathPointI");
                GdipCreateFont_ptr = LoadFunction<GdipCreateFont_delegate>("GdipCreateFont");
                GdipDeleteFont_ptr = LoadFunction<GdipDeleteFont_delegate>("GdipDeleteFont");
                GdipGetLogFont_ptr = LoadFunction<GdipGetLogFont_delegate>("GdipGetLogFontW");
                GdipCreateFontFromDC_ptr = LoadFunction<GdipCreateFontFromDC_delegate>("GdipCreateFontFromDC");
                GdipCreateFontFromLogfont_ptr = LoadFunction<GdipCreateFontFromLogfont_delegate>("GdipCreateFontFromLogfontW");
                GdipCreateFontFromHfont_ptr = LoadFunction<GdipCreateFontFromHfont_delegate>("GdipCreateFontFromHfontA");
                GdipCreateFontFamilyFromName_ptr = LoadFunction<GdipCreateFontFamilyFromName_delegate>("GdipCreateFontFamilyFromName");
                GdipGetFamilyName_ptr = LoadFunction<GdipGetFamilyName_delegate>("GdipGetFamilyName");
                GdipGetGenericFontFamilySansSerif_ptr = LoadFunction<GdipGetGenericFontFamilySansSerif_delegate>("GdipGetGenericFontFamilySansSerif");
                GdipGetGenericFontFamilySerif_ptr = LoadFunction<GdipGetGenericFontFamilySerif_delegate>("GdipGetGenericFontFamilySerif");
                GdipGetGenericFontFamilyMonospace_ptr = LoadFunction<GdipGetGenericFontFamilyMonospace_delegate>("GdipGetGenericFontFamilyMonospace");
                GdipGetCellAscent_ptr = LoadFunction<GdipGetCellAscent_delegate>("GdipGetCellAscent");
                GdipGetCellDescent_ptr = LoadFunction<GdipGetCellDescent_delegate>("GdipGetCellDescent");
                GdipGetLineSpacing_ptr = LoadFunction<GdipGetLineSpacing_delegate>("GdipGetLineSpacing");
                GdipGetEmHeight_ptr = LoadFunction<GdipGetEmHeight_delegate>("GdipGetEmHeight");
                GdipIsStyleAvailable_ptr = LoadFunction<GdipIsStyleAvailable_delegate>("GdipIsStyleAvailable");
                GdipDeleteFontFamily_ptr = LoadFunction<GdipDeleteFontFamily_delegate>("GdipDeleteFontFamily");
                GdipGetFontSize_ptr = LoadFunction<GdipGetFontSize_delegate>("GdipGetFontSize");
                GdipGetFontHeight_ptr = LoadFunction<GdipGetFontHeight_delegate>("GdipGetFontHeight");
                GdipGetFontHeightGivenDPI_ptr = LoadFunction<GdipGetFontHeightGivenDPI_delegate>("GdipGetFontHeightGivenDPI");
                GdipCreateMetafileFromFile_ptr = LoadFunction<GdipCreateMetafileFromFile_delegate>("GdipCreateMetafileFromFile");
                GdipCreateMetafileFromEmf_ptr = LoadFunction<GdipCreateMetafileFromEmf_delegate>("GdipCreateMetafileFromEmf");
                GdipCreateMetafileFromWmf_ptr = LoadFunction<GdipCreateMetafileFromWmf_delegate>("GdipCreateMetafileFromWmf");
                GdipGetMetafileHeaderFromFile_ptr = LoadFunction<GdipGetMetafileHeaderFromFile_delegate>("GdipGetMetafileHeaderFromFile");
                GdipGetMetafileHeaderFromMetafile_ptr = LoadFunction<GdipGetMetafileHeaderFromMetafile_delegate>("GdipGetMetafileHeaderFromMetafile");
                GdipGetMetafileHeaderFromEmf_ptr = LoadFunction<GdipGetMetafileHeaderFromEmf_delegate>("GdipGetMetafileHeaderFromEmf");
                GdipGetMetafileHeaderFromWmf_ptr = LoadFunction<GdipGetMetafileHeaderFromWmf_delegate>("GdipGetMetafileHeaderFromWmf");
                GdipGetHemfFromMetafile_ptr = LoadFunction<GdipGetHemfFromMetafile_delegate>("GdipGetHemfFromMetafile");
                GdipGetMetafileDownLevelRasterizationLimit_ptr = LoadFunction<GdipGetMetafileDownLevelRasterizationLimit_delegate>("GdipGetMetafileDownLevelRasterizationLimit");
                GdipSetMetafileDownLevelRasterizationLimit_ptr = LoadFunction<GdipSetMetafileDownLevelRasterizationLimit_delegate>("GdipSetMetafileDownLevelRasterizationLimit");
                GdipPlayMetafileRecord_ptr = LoadFunction<GdipPlayMetafileRecord_delegate>("GdipPlayMetafileRecord");
                GdipRecordMetafile_ptr = LoadFunction<GdipRecordMetafile_delegate>("GdipRecordMetafile");
                GdipRecordMetafileI_ptr = LoadFunction<GdipRecordMetafileI_delegate>("GdipRecordMetafileI");
                GdipRecordMetafileFileName_ptr = LoadFunction<GdipRecordMetafileFileName_delegate>("GdipRecordMetafileFileName");
                GdipRecordMetafileFileNameI_ptr = LoadFunction<GdipRecordMetafileFileNameI_delegate>("GdipRecordMetafileFileNameI");
                GdipCreateMetafileFromStream_ptr = LoadFunction<GdipCreateMetafileFromStream_delegate>("GdipCreateMetafileFromStream");
                GdipGetMetafileHeaderFromStream_ptr = LoadFunction<GdipGetMetafileHeaderFromStream_delegate>("GdipGetMetafileHeaderFromStream");
                GdipRecordMetafileStream_ptr = LoadFunction<GdipRecordMetafileStream_delegate>("GdipRecordMetafileStream");
                GdipRecordMetafileStreamI_ptr = LoadFunction<GdipRecordMetafileStreamI_delegate>("GdipRecordMetafileStreamI");
                GdipCreateFromContext_macosx_ptr = LoadFunction<GdipCreateFromContext_macosx_delegate>("GdipCreateFromContext_macosx");
                GdipSetVisibleClip_linux_ptr = LoadFunction<GdipSetVisibleClip_linux_delegate>("GdipSetVisibleClip_linux");
                GdipCreateFromXDrawable_linux_ptr = LoadFunction<GdipCreateFromXDrawable_linux_delegate>("GdipCreateFromXDrawable_linux");
                GdipLoadImageFromDelegate_linux_ptr = LoadFunction<GdipLoadImageFromDelegate_linux_delegate>("GdipLoadImageFromDelegate_linux");
                GdipSaveImageToDelegate_linux_ptr = LoadFunction<GdipSaveImageToDelegate_linux_delegate>("GdipSaveImageToDelegate_linux");
                GdipCreateMetafileFromDelegate_linux_ptr = LoadFunction<GdipCreateMetafileFromDelegate_linux_delegate>("GdipCreateMetafileFromDelegate_linux");
                GdipGetMetafileHeaderFromDelegate_linux_ptr = LoadFunction<GdipGetMetafileHeaderFromDelegate_linux_delegate>("GdipGetMetafileHeaderFromDelegate_linux");
                GdipRecordMetafileFromDelegate_linux_ptr = LoadFunction<GdipRecordMetafileFromDelegate_linux_delegate>("GdipRecordMetafileFromDelegate_linux");
                GdipRecordMetafileFromDelegateI_linux_ptr = LoadFunction<GdipRecordMetafileFromDelegateI_linux_delegate>("GdipRecordMetafileFromDelegateI_linux");
            }

            // Imported functions

            private delegate Status GdiplusStartup_delegate(out IntPtr token, ref StartupInput input, out StartupOutput output);
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

            private delegate Status GdipDeleteBrush_delegate(IntPtr brush);
            private static FunctionWrapper<GdipDeleteBrush_delegate> GdipDeleteBrush_ptr;
            internal static Status GdipDeleteBrush(IntPtr brush) => GdipDeleteBrush_ptr.Delegate(brush);
            internal static int IntGdipDeleteBrush(HandleRef brush) => (int)GdipDeleteBrush_ptr.Delegate(brush.Handle);

            private delegate Status GdipGetBrushType_delegate(IntPtr brush, out BrushType type);
            private static FunctionWrapper<GdipGetBrushType_delegate> GdipGetBrushType_ptr;
            internal static Status GdipGetBrushType(IntPtr brush, out BrushType type) => GdipGetBrushType_ptr.Delegate(brush, out type);

            private delegate Status GdipCreateFromHDC_delegate(IntPtr hDC, out IntPtr graphics);
            private static FunctionWrapper<GdipCreateFromHDC_delegate> GdipCreateFromHDC_ptr;
            internal static Status GdipCreateFromHDC(IntPtr hDC, out IntPtr graphics) => GdipCreateFromHDC_ptr.Delegate(hDC, out graphics);

            private delegate Status GdipDeleteGraphics_delegate(IntPtr graphics);
            private static FunctionWrapper<GdipDeleteGraphics_delegate> GdipDeleteGraphics_ptr;
            internal static Status GdipDeleteGraphics(IntPtr graphics) => GdipDeleteGraphics_ptr.Delegate(graphics);
            internal static int IntGdipDeleteGraphics(HandleRef graphics) => (int)GdipDeleteGraphics_ptr.Delegate(graphics.Handle);

            private delegate Status GdipRestoreGraphics_delegate(IntPtr graphics, uint graphicsState);
            private static FunctionWrapper<GdipRestoreGraphics_delegate> GdipRestoreGraphics_ptr;
            internal static Status GdipRestoreGraphics(IntPtr graphics, uint graphicsState) => GdipRestoreGraphics_ptr.Delegate(graphics, graphicsState);

            private delegate Status GdipSaveGraphics_delegate(IntPtr graphics, out uint state);
            private static FunctionWrapper<GdipSaveGraphics_delegate> GdipSaveGraphics_ptr;
            internal static Status GdipSaveGraphics(IntPtr graphics, out uint state) => GdipSaveGraphics_ptr.Delegate(graphics, out state);

            private delegate Status GdipDrawArc_delegate(IntPtr graphics, IntPtr pen, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawArc_delegate> GdipDrawArc_ptr;
            internal static Status GdipDrawArc(IntPtr graphics, IntPtr pen, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipDrawArc_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipDrawArcI_delegate(IntPtr graphics, IntPtr pen, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawArcI_delegate> GdipDrawArcI_ptr;
            internal static Status GdipDrawArcI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipDrawArcI_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipDrawBezier_delegate(IntPtr graphics, IntPtr pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);
            private static FunctionWrapper<GdipDrawBezier_delegate> GdipDrawBezier_ptr;
            internal static Status GdipDrawBezier(IntPtr graphics, IntPtr pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) => GdipDrawBezier_ptr.Delegate(graphics, pen, x1, y1, x2, y2, x3, y3, x4, y4);

            private delegate Status GdipDrawBezierI_delegate(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);
            private static FunctionWrapper<GdipDrawBezierI_delegate> GdipDrawBezierI_ptr;
            internal static Status GdipDrawBezierI(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4) => GdipDrawBezierI_ptr.Delegate(graphics, pen, x1, y1, x2, y2, x3, y3, x4, y4);

            private delegate Status GdipDrawEllipseI_delegate(IntPtr graphics, IntPtr pen, int x, int y, int width, int height);
            private static FunctionWrapper<GdipDrawEllipseI_delegate> GdipDrawEllipseI_ptr;
            internal static Status GdipDrawEllipseI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height) => GdipDrawEllipseI_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate Status GdipDrawEllipse_delegate(IntPtr graphics, IntPtr pen, float x, float y, float width, float height);
            private static FunctionWrapper<GdipDrawEllipse_delegate> GdipDrawEllipse_ptr;
            internal static Status GdipDrawEllipse(IntPtr graphics, IntPtr pen, float x, float y, float width, float height) => GdipDrawEllipse_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate Status GdipDrawLine_delegate(IntPtr graphics, IntPtr pen, float x1, float y1, float x2, float y2);
            private static FunctionWrapper<GdipDrawLine_delegate> GdipDrawLine_ptr;
            internal static Status GdipDrawLine(IntPtr graphics, IntPtr pen, float x1, float y1, float x2, float y2) => GdipDrawLine_ptr.Delegate(graphics, pen, x1, y1, x2, y2);

            private delegate Status GdipDrawLineI_delegate(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2);
            private static FunctionWrapper<GdipDrawLineI_delegate> GdipDrawLineI_ptr;
            internal static Status GdipDrawLineI(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2) => GdipDrawLineI_ptr.Delegate(graphics, pen, x1, y1, x2, y2);

            private delegate Status GdipDrawLines_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count);
            private static FunctionWrapper<GdipDrawLines_delegate> GdipDrawLines_ptr;
            internal static Status GdipDrawLines(IntPtr graphics, IntPtr pen, PointF[] points, int count) => GdipDrawLines_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawLinesI_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count);
            private static FunctionWrapper<GdipDrawLinesI_delegate> GdipDrawLinesI_ptr;
            internal static Status GdipDrawLinesI(IntPtr graphics, IntPtr pen, Point[] points, int count) => GdipDrawLinesI_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawPath_delegate(IntPtr graphics, IntPtr pen, IntPtr path);
            private static FunctionWrapper<GdipDrawPath_delegate> GdipDrawPath_ptr;
            internal static Status GdipDrawPath(IntPtr graphics, IntPtr pen, IntPtr path) => GdipDrawPath_ptr.Delegate(graphics, pen, path);

            private delegate Status GdipDrawPie_delegate(IntPtr graphics, IntPtr pen, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawPie_delegate> GdipDrawPie_ptr;
            internal static Status GdipDrawPie(IntPtr graphics, IntPtr pen, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipDrawPie_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipDrawPieI_delegate(IntPtr graphics, IntPtr pen, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawPieI_delegate> GdipDrawPieI_ptr;
            internal static Status GdipDrawPieI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipDrawPieI_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipDrawPolygon_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count);
            private static FunctionWrapper<GdipDrawPolygon_delegate> GdipDrawPolygon_ptr;
            internal static Status GdipDrawPolygon(IntPtr graphics, IntPtr pen, PointF[] points, int count) => GdipDrawPolygon_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawPolygonI_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count);
            private static FunctionWrapper<GdipDrawPolygonI_delegate> GdipDrawPolygonI_ptr;
            internal static Status GdipDrawPolygonI(IntPtr graphics, IntPtr pen, Point[] points, int count) => GdipDrawPolygonI_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawRectangle_delegate(IntPtr graphics, IntPtr pen, float x, float y, float width, float height);
            private static FunctionWrapper<GdipDrawRectangle_delegate> GdipDrawRectangle_ptr;
            internal static Status GdipDrawRectangle(IntPtr graphics, IntPtr pen, float x, float y, float width, float height) => GdipDrawRectangle_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate Status GdipDrawRectangleI_delegate(IntPtr graphics, IntPtr pen, int x, int y, int width, int height);
            private static FunctionWrapper<GdipDrawRectangleI_delegate> GdipDrawRectangleI_ptr;
            internal static Status GdipDrawRectangleI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height) => GdipDrawRectangleI_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate Status GdipDrawRectangles_delegate(IntPtr graphics, IntPtr pen, RectangleF[] rects, int count);
            private static FunctionWrapper<GdipDrawRectangles_delegate> GdipDrawRectangles_ptr;
            internal static Status GdipDrawRectangles(IntPtr graphics, IntPtr pen, RectangleF[] rects, int count) => GdipDrawRectangles_ptr.Delegate(graphics, pen, rects, count);

            private delegate Status GdipDrawRectanglesI_delegate(IntPtr graphics, IntPtr pen, Rectangle[] rects, int count);
            private static FunctionWrapper<GdipDrawRectanglesI_delegate> GdipDrawRectanglesI_ptr;
            internal static Status GdipDrawRectanglesI(IntPtr graphics, IntPtr pen, Rectangle[] rects, int count) => GdipDrawRectanglesI_ptr.Delegate(graphics, pen, rects, count);

            private delegate Status GdipFillEllipseI_delegate(IntPtr graphics, IntPtr pen, int x, int y, int width, int height);
            private static FunctionWrapper<GdipFillEllipseI_delegate> GdipFillEllipseI_ptr;
            internal static Status GdipFillEllipseI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height) => GdipFillEllipseI_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate Status GdipFillEllipse_delegate(IntPtr graphics, IntPtr pen, float x, float y, float width, float height);
            private static FunctionWrapper<GdipFillEllipse_delegate> GdipFillEllipse_ptr;
            internal static Status GdipFillEllipse(IntPtr graphics, IntPtr pen, float x, float y, float width, float height) => GdipFillEllipse_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate Status GdipFillPolygon_delegate(IntPtr graphics, IntPtr brush, PointF[] points, int count, FillMode fillMode);
            private static FunctionWrapper<GdipFillPolygon_delegate> GdipFillPolygon_ptr;
            internal static Status GdipFillPolygon(IntPtr graphics, IntPtr brush, PointF[] points, int count, FillMode fillMode) => GdipFillPolygon_ptr.Delegate(graphics, brush, points, count, fillMode);

            private delegate Status GdipFillPolygonI_delegate(IntPtr graphics, IntPtr brush, Point[] points, int count, FillMode fillMode);
            private static FunctionWrapper<GdipFillPolygonI_delegate> GdipFillPolygonI_ptr;
            internal static Status GdipFillPolygonI(IntPtr graphics, IntPtr brush, Point[] points, int count, FillMode fillMode) => GdipFillPolygonI_ptr.Delegate(graphics, brush, points, count, fillMode);

            private delegate Status GdipFillPolygon2_delegate(IntPtr graphics, IntPtr brush, PointF[] points, int count);
            private static FunctionWrapper<GdipFillPolygon2_delegate> GdipFillPolygon2_ptr;
            internal static Status GdipFillPolygon2(IntPtr graphics, IntPtr brush, PointF[] points, int count) => GdipFillPolygon2_ptr.Delegate(graphics, brush, points, count);

            private delegate Status GdipFillPolygon2I_delegate(IntPtr graphics, IntPtr brush, Point[] points, int count);
            private static FunctionWrapper<GdipFillPolygon2I_delegate> GdipFillPolygon2I_ptr;
            internal static Status GdipFillPolygon2I(IntPtr graphics, IntPtr brush, Point[] points, int count) => GdipFillPolygon2I_ptr.Delegate(graphics, brush, points, count);

            private delegate Status GdipFillRectangle_delegate(IntPtr graphics, IntPtr brush, float x1, float y1, float x2, float y2);
            private static FunctionWrapper<GdipFillRectangle_delegate> GdipFillRectangle_ptr;
            internal static Status GdipFillRectangle(IntPtr graphics, IntPtr brush, float x1, float y1, float x2, float y2) => GdipFillRectangle_ptr.Delegate(graphics, brush, x1, y1, x2, y2);

            private delegate Status GdipFillRectangleI_delegate(IntPtr graphics, IntPtr brush, int x1, int y1, int x2, int y2);
            private static FunctionWrapper<GdipFillRectangleI_delegate> GdipFillRectangleI_ptr;
            internal static Status GdipFillRectangleI(IntPtr graphics, IntPtr brush, int x1, int y1, int x2, int y2) => GdipFillRectangleI_ptr.Delegate(graphics, brush, x1, y1, x2, y2);

            private delegate Status GdipFillRectangles_delegate(IntPtr graphics, IntPtr brush, RectangleF[] rects, int count);
            private static FunctionWrapper<GdipFillRectangles_delegate> GdipFillRectangles_ptr;
            internal static Status GdipFillRectangles(IntPtr graphics, IntPtr brush, RectangleF[] rects, int count) => GdipFillRectangles_ptr.Delegate(graphics, brush, rects, count);

            private delegate Status GdipFillRectanglesI_delegate(IntPtr graphics, IntPtr brush, Rectangle[] rects, int count);
            private static FunctionWrapper<GdipFillRectanglesI_delegate> GdipFillRectanglesI_ptr;
            internal static Status GdipFillRectanglesI(IntPtr graphics, IntPtr brush, Rectangle[] rects, int count) => GdipFillRectanglesI_ptr.Delegate(graphics, brush, rects, count);

            private delegate Status GdipDrawString_delegate(IntPtr graphics, [MarshalAs(UnmanagedType.LPWStr)]string text, int len, IntPtr font, ref RectangleF rc, IntPtr format, IntPtr brush);
            private static FunctionWrapper<GdipDrawString_delegate> GdipDrawString_ptr;
            internal static Status GdipDrawString(IntPtr graphics, string text, int len, IntPtr font, ref RectangleF rc, IntPtr format, IntPtr brush) => GdipDrawString_ptr.Delegate(graphics, text, len, font, ref rc, format, brush);

            private delegate Status GdipGetDC_delegate(IntPtr graphics, out IntPtr hdc);
            private static FunctionWrapper<GdipGetDC_delegate> GdipGetDC_ptr;
            internal static Status GdipGetDC(IntPtr graphics, out IntPtr hdc) => GdipGetDC_ptr.Delegate(graphics, out hdc);

            private delegate Status GdipReleaseDC_delegate(IntPtr graphics, IntPtr hdc);
            private static FunctionWrapper<GdipReleaseDC_delegate> GdipReleaseDC_ptr;
            internal static Status GdipReleaseDC(IntPtr graphics, IntPtr hdc) => GdipReleaseDC_ptr.Delegate(graphics, hdc);
            internal static int IntGdipReleaseDC(HandleRef graphics, HandleRef hdc) => (int)GdipReleaseDC_ptr.Delegate(graphics.Handle, hdc.Handle);

            private delegate Status GdipDrawImageRectI_delegate(IntPtr graphics, IntPtr image, int x, int y, int width, int height);
            private static FunctionWrapper<GdipDrawImageRectI_delegate> GdipDrawImageRectI_ptr;
            internal static Status GdipDrawImageRectI(IntPtr graphics, IntPtr image, int x, int y, int width, int height) => GdipDrawImageRectI_ptr.Delegate(graphics, image, x, y, width, height);

            private delegate Status GdipGetRenderingOrigin_delegate(IntPtr graphics, out int x, out int y);
            private static FunctionWrapper<GdipGetRenderingOrigin_delegate> GdipGetRenderingOrigin_ptr;
            internal static Status GdipGetRenderingOrigin(IntPtr graphics, out int x, out int y) => GdipGetRenderingOrigin_ptr.Delegate(graphics, out x, out y);

            private delegate Status GdipSetRenderingOrigin_delegate(IntPtr graphics, int x, int y);
            private static FunctionWrapper<GdipSetRenderingOrigin_delegate> GdipSetRenderingOrigin_ptr;
            internal static Status GdipSetRenderingOrigin(IntPtr graphics, int x, int y) => GdipSetRenderingOrigin_ptr.Delegate(graphics, x, y);

            private delegate Status GdipCloneBitmapArea_delegate(float x, float y, float width, float height, PixelFormat format, IntPtr original, out IntPtr bitmap);
            private static FunctionWrapper<GdipCloneBitmapArea_delegate> GdipCloneBitmapArea_ptr;
            internal static Status GdipCloneBitmapArea(float x, float y, float width, float height, PixelFormat format, IntPtr original, out IntPtr bitmap) => GdipCloneBitmapArea_ptr.Delegate(x, y, width, height, format, original, out bitmap);

            private delegate Status GdipCloneBitmapAreaI_delegate(int x, int y, int width, int height, PixelFormat format, IntPtr original, out IntPtr bitmap);
            private static FunctionWrapper<GdipCloneBitmapAreaI_delegate> GdipCloneBitmapAreaI_ptr;
            internal static Status GdipCloneBitmapAreaI(int x, int y, int width, int height, PixelFormat format, IntPtr original, out IntPtr bitmap) => GdipCloneBitmapAreaI_ptr.Delegate(x, y, width, height, format, original, out bitmap);

            private delegate Status GdipGraphicsClear_delegate(IntPtr graphics, int argb);
            private static FunctionWrapper<GdipGraphicsClear_delegate> GdipGraphicsClear_ptr;
            internal static Status GdipGraphicsClear(IntPtr graphics, int argb) => GdipGraphicsClear_ptr.Delegate(graphics, argb);

            private delegate Status GdipDrawClosedCurve_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count);
            private static FunctionWrapper<GdipDrawClosedCurve_delegate> GdipDrawClosedCurve_ptr;
            internal static Status GdipDrawClosedCurve(IntPtr graphics, IntPtr pen, PointF[] points, int count) => GdipDrawClosedCurve_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawClosedCurveI_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count);
            private static FunctionWrapper<GdipDrawClosedCurveI_delegate> GdipDrawClosedCurveI_ptr;
            internal static Status GdipDrawClosedCurveI(IntPtr graphics, IntPtr pen, Point[] points, int count) => GdipDrawClosedCurveI_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawClosedCurve2_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count, float tension);
            private static FunctionWrapper<GdipDrawClosedCurve2_delegate> GdipDrawClosedCurve2_ptr;
            internal static Status GdipDrawClosedCurve2(IntPtr graphics, IntPtr pen, PointF[] points, int count, float tension) => GdipDrawClosedCurve2_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate Status GdipDrawClosedCurve2I_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count, float tension);
            private static FunctionWrapper<GdipDrawClosedCurve2I_delegate> GdipDrawClosedCurve2I_ptr;
            internal static Status GdipDrawClosedCurve2I(IntPtr graphics, IntPtr pen, Point[] points, int count, float tension) => GdipDrawClosedCurve2I_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate Status GdipDrawCurve_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count);
            private static FunctionWrapper<GdipDrawCurve_delegate> GdipDrawCurve_ptr;
            internal static Status GdipDrawCurve(IntPtr graphics, IntPtr pen, PointF[] points, int count) => GdipDrawCurve_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawCurveI_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count);
            private static FunctionWrapper<GdipDrawCurveI_delegate> GdipDrawCurveI_ptr;
            internal static Status GdipDrawCurveI(IntPtr graphics, IntPtr pen, Point[] points, int count) => GdipDrawCurveI_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawCurve2_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count, float tension);
            private static FunctionWrapper<GdipDrawCurve2_delegate> GdipDrawCurve2_ptr;
            internal static Status GdipDrawCurve2(IntPtr graphics, IntPtr pen, PointF[] points, int count, float tension) => GdipDrawCurve2_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate Status GdipDrawCurve2I_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count, float tension);
            private static FunctionWrapper<GdipDrawCurve2I_delegate> GdipDrawCurve2I_ptr;
            internal static Status GdipDrawCurve2I(IntPtr graphics, IntPtr pen, Point[] points, int count, float tension) => GdipDrawCurve2I_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate Status GdipDrawCurve3_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipDrawCurve3_delegate> GdipDrawCurve3_ptr;
            internal static Status GdipDrawCurve3(IntPtr graphics, IntPtr pen, PointF[] points, int count, int offset, int numberOfSegments, float tension) => GdipDrawCurve3_ptr.Delegate(graphics, pen, points, count, offset, numberOfSegments, tension);

            private delegate Status GdipDrawCurve3I_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipDrawCurve3I_delegate> GdipDrawCurve3I_ptr;
            internal static Status GdipDrawCurve3I(IntPtr graphics, IntPtr pen, Point[] points, int count, int offset, int numberOfSegments, float tension) => GdipDrawCurve3I_ptr.Delegate(graphics, pen, points, count, offset, numberOfSegments, tension);

            private delegate Status GdipFillClosedCurve_delegate(IntPtr graphics, IntPtr brush, PointF[] points, int count);
            private static FunctionWrapper<GdipFillClosedCurve_delegate> GdipFillClosedCurve_ptr;
            internal static Status GdipFillClosedCurve(IntPtr graphics, IntPtr brush, PointF[] points, int count) => GdipFillClosedCurve_ptr.Delegate(graphics, brush, points, count);

            private delegate Status GdipFillClosedCurveI_delegate(IntPtr graphics, IntPtr brush, Point[] points, int count);
            private static FunctionWrapper<GdipFillClosedCurveI_delegate> GdipFillClosedCurveI_ptr;
            internal static Status GdipFillClosedCurveI(IntPtr graphics, IntPtr brush, Point[] points, int count) => GdipFillClosedCurveI_ptr.Delegate(graphics, brush, points, count);

            private delegate Status GdipFillClosedCurve2_delegate(IntPtr graphics, IntPtr brush, PointF[] points, int count, float tension, FillMode fillMode);
            private static FunctionWrapper<GdipFillClosedCurve2_delegate> GdipFillClosedCurve2_ptr;
            internal static Status GdipFillClosedCurve2(IntPtr graphics, IntPtr brush, PointF[] points, int count, float tension, FillMode fillMode) => GdipFillClosedCurve2_ptr.Delegate(graphics, brush, points, count, tension, fillMode);

            private delegate Status GdipFillClosedCurve2I_delegate(IntPtr graphics, IntPtr brush, Point[] points, int count, float tension, FillMode fillMode);
            private static FunctionWrapper<GdipFillClosedCurve2I_delegate> GdipFillClosedCurve2I_ptr;
            internal static Status GdipFillClosedCurve2I(IntPtr graphics, IntPtr brush, Point[] points, int count, float tension, FillMode fillMode) => GdipFillClosedCurve2I_ptr.Delegate(graphics, brush, points, count, tension, fillMode);

            private delegate Status GdipFillPie_delegate(IntPtr graphics, IntPtr brush, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipFillPie_delegate> GdipFillPie_ptr;
            internal static Status GdipFillPie(IntPtr graphics, IntPtr brush, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipFillPie_ptr.Delegate(graphics, brush, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipFillPieI_delegate(IntPtr graphics, IntPtr brush, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipFillPieI_delegate> GdipFillPieI_ptr;
            internal static Status GdipFillPieI(IntPtr graphics, IntPtr brush, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipFillPieI_ptr.Delegate(graphics, brush, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipFillPath_delegate(IntPtr graphics, IntPtr brush, IntPtr path);
            private static FunctionWrapper<GdipFillPath_delegate> GdipFillPath_ptr;
            internal static Status GdipFillPath(IntPtr graphics, IntPtr brush, IntPtr path) => GdipFillPath_ptr.Delegate(graphics, brush, path);

            private delegate Status GdipGetNearestColor_delegate(IntPtr graphics, out int argb);
            private static FunctionWrapper<GdipGetNearestColor_delegate> GdipGetNearestColor_ptr;
            internal static Status GdipGetNearestColor(IntPtr graphics, out int argb) => GdipGetNearestColor_ptr.Delegate(graphics, out argb);

            private delegate Status GdipTransformPoints_delegate(IntPtr graphics, CoordinateSpace destSpace, CoordinateSpace srcSpace, IntPtr points, int count);
            private static FunctionWrapper<GdipTransformPoints_delegate> GdipTransformPoints_ptr;
            internal static Status GdipTransformPoints(IntPtr graphics, CoordinateSpace destSpace, CoordinateSpace srcSpace, IntPtr points, int count) => GdipTransformPoints_ptr.Delegate(graphics, destSpace, srcSpace, points, count);

            private delegate Status GdipTransformPointsI_delegate(IntPtr graphics, CoordinateSpace destSpace, CoordinateSpace srcSpace, IntPtr points, int count);
            private static FunctionWrapper<GdipTransformPointsI_delegate> GdipTransformPointsI_ptr;
            internal static Status GdipTransformPointsI(IntPtr graphics, CoordinateSpace destSpace, CoordinateSpace srcSpace, IntPtr points, int count) => GdipTransformPointsI_ptr.Delegate(graphics, destSpace, srcSpace, points, count);

            private delegate Status GdipSetCompositingMode_delegate(IntPtr graphics, CompositingMode compositingMode);
            private static FunctionWrapper<GdipSetCompositingMode_delegate> GdipSetCompositingMode_ptr;
            internal static Status GdipSetCompositingMode(IntPtr graphics, CompositingMode compositingMode) => GdipSetCompositingMode_ptr.Delegate(graphics, compositingMode);

            private delegate Status GdipGetCompositingMode_delegate(IntPtr graphics, out CompositingMode compositingMode);
            private static FunctionWrapper<GdipGetCompositingMode_delegate> GdipGetCompositingMode_ptr;
            internal static Status GdipGetCompositingMode(IntPtr graphics, out CompositingMode compositingMode) => GdipGetCompositingMode_ptr.Delegate(graphics, out compositingMode);

            private delegate Status GdipSetCompositingQuality_delegate(IntPtr graphics, CompositingQuality compositingQuality);
            private static FunctionWrapper<GdipSetCompositingQuality_delegate> GdipSetCompositingQuality_ptr;
            internal static Status GdipSetCompositingQuality(IntPtr graphics, CompositingQuality compositingQuality) => GdipSetCompositingQuality_ptr.Delegate(graphics, compositingQuality);

            private delegate Status GdipGetCompositingQuality_delegate(IntPtr graphics, out CompositingQuality compositingQuality);
            private static FunctionWrapper<GdipGetCompositingQuality_delegate> GdipGetCompositingQuality_ptr;
            internal static Status GdipGetCompositingQuality(IntPtr graphics, out CompositingQuality compositingQuality) => GdipGetCompositingQuality_ptr.Delegate(graphics, out compositingQuality);

            private delegate Status GdipSetInterpolationMode_delegate(IntPtr graphics, InterpolationMode interpolationMode);
            private static FunctionWrapper<GdipSetInterpolationMode_delegate> GdipSetInterpolationMode_ptr;
            internal static Status GdipSetInterpolationMode(IntPtr graphics, InterpolationMode interpolationMode) => GdipSetInterpolationMode_ptr.Delegate(graphics, interpolationMode);

            private delegate Status GdipGetInterpolationMode_delegate(IntPtr graphics, out InterpolationMode interpolationMode);
            private static FunctionWrapper<GdipGetInterpolationMode_delegate> GdipGetInterpolationMode_ptr;
            internal static Status GdipGetInterpolationMode(IntPtr graphics, out InterpolationMode interpolationMode) => GdipGetInterpolationMode_ptr.Delegate(graphics, out interpolationMode);

            private delegate Status GdipGetDpiX_delegate(IntPtr graphics, out float dpi);
            private static FunctionWrapper<GdipGetDpiX_delegate> GdipGetDpiX_ptr;
            internal static Status GdipGetDpiX(IntPtr graphics, out float dpi) => GdipGetDpiX_ptr.Delegate(graphics, out dpi);

            private delegate Status GdipGetDpiY_delegate(IntPtr graphics, out float dpi);
            private static FunctionWrapper<GdipGetDpiY_delegate> GdipGetDpiY_ptr;
            internal static Status GdipGetDpiY(IntPtr graphics, out float dpi) => GdipGetDpiY_ptr.Delegate(graphics, out dpi);

            private delegate Status GdipGetPageUnit_delegate(IntPtr graphics, out GraphicsUnit unit);
            private static FunctionWrapper<GdipGetPageUnit_delegate> GdipGetPageUnit_ptr;
            internal static Status GdipGetPageUnit(IntPtr graphics, out GraphicsUnit unit) => GdipGetPageUnit_ptr.Delegate(graphics, out unit);

            private delegate Status GdipGetPageScale_delegate(IntPtr graphics, out float scale);
            private static FunctionWrapper<GdipGetPageScale_delegate> GdipGetPageScale_ptr;
            internal static Status GdipGetPageScale(IntPtr graphics, out float scale) => GdipGetPageScale_ptr.Delegate(graphics, out scale);

            private delegate Status GdipSetPageUnit_delegate(IntPtr graphics, GraphicsUnit unit);
            private static FunctionWrapper<GdipSetPageUnit_delegate> GdipSetPageUnit_ptr;
            internal static Status GdipSetPageUnit(IntPtr graphics, GraphicsUnit unit) => GdipSetPageUnit_ptr.Delegate(graphics, unit);

            private delegate Status GdipSetPageScale_delegate(IntPtr graphics, float scale);
            private static FunctionWrapper<GdipSetPageScale_delegate> GdipSetPageScale_ptr;
            internal static Status GdipSetPageScale(IntPtr graphics, float scale) => GdipSetPageScale_ptr.Delegate(graphics, scale);

            private delegate Status GdipSetPixelOffsetMode_delegate(IntPtr graphics, PixelOffsetMode pixelOffsetMode);
            private static FunctionWrapper<GdipSetPixelOffsetMode_delegate> GdipSetPixelOffsetMode_ptr;
            internal static Status GdipSetPixelOffsetMode(IntPtr graphics, PixelOffsetMode pixelOffsetMode) => GdipSetPixelOffsetMode_ptr.Delegate(graphics, pixelOffsetMode);

            private delegate Status GdipGetPixelOffsetMode_delegate(IntPtr graphics, out PixelOffsetMode pixelOffsetMode);
            private static FunctionWrapper<GdipGetPixelOffsetMode_delegate> GdipGetPixelOffsetMode_ptr;
            internal static Status GdipGetPixelOffsetMode(IntPtr graphics, out PixelOffsetMode pixelOffsetMode) => GdipGetPixelOffsetMode_ptr.Delegate(graphics, out pixelOffsetMode);

            private delegate Status GdipSetSmoothingMode_delegate(IntPtr graphics, SmoothingMode smoothingMode);
            private static FunctionWrapper<GdipSetSmoothingMode_delegate> GdipSetSmoothingMode_ptr;
            internal static Status GdipSetSmoothingMode(IntPtr graphics, SmoothingMode smoothingMode) => GdipSetSmoothingMode_ptr.Delegate(graphics, smoothingMode);

            private delegate Status GdipGetSmoothingMode_delegate(IntPtr graphics, out SmoothingMode smoothingMode);
            private static FunctionWrapper<GdipGetSmoothingMode_delegate> GdipGetSmoothingMode_ptr;
            internal static Status GdipGetSmoothingMode(IntPtr graphics, out SmoothingMode smoothingMode) => GdipGetSmoothingMode_ptr.Delegate(graphics, out smoothingMode);

            private delegate Status GdipSetTextContrast_delegate(IntPtr graphics, int contrast);
            private static FunctionWrapper<GdipSetTextContrast_delegate> GdipSetTextContrast_ptr;
            internal static Status GdipSetTextContrast(IntPtr graphics, int contrast) => GdipSetTextContrast_ptr.Delegate(graphics, contrast);

            private delegate Status GdipGetTextContrast_delegate(IntPtr graphics, out int contrast);
            private static FunctionWrapper<GdipGetTextContrast_delegate> GdipGetTextContrast_ptr;
            internal static Status GdipGetTextContrast(IntPtr graphics, out int contrast) => GdipGetTextContrast_ptr.Delegate(graphics, out contrast);

            private delegate Status GdipSetTextRenderingHint_delegate(IntPtr graphics, TextRenderingHint mode);
            private static FunctionWrapper<GdipSetTextRenderingHint_delegate> GdipSetTextRenderingHint_ptr;
            internal static Status GdipSetTextRenderingHint(IntPtr graphics, TextRenderingHint mode) => GdipSetTextRenderingHint_ptr.Delegate(graphics, mode);

            private delegate Status GdipGetTextRenderingHint_delegate(IntPtr graphics, out TextRenderingHint mode);
            private static FunctionWrapper<GdipGetTextRenderingHint_delegate> GdipGetTextRenderingHint_ptr;
            internal static Status GdipGetTextRenderingHint(IntPtr graphics, out TextRenderingHint mode) => GdipGetTextRenderingHint_ptr.Delegate(graphics, out mode);

            private delegate Status GdipFlush_delegate(IntPtr graphics, FlushIntention intention);
            private static FunctionWrapper<GdipFlush_delegate> GdipFlush_ptr;
            internal static Status GdipFlush(IntPtr graphics, FlushIntention intention) => GdipFlush_ptr.Delegate(graphics, intention);

            private delegate Status GdipAddPathString_delegate(IntPtr path, [MarshalAs(UnmanagedType.LPWStr)]string s, int lenght, IntPtr family, int style, float emSize, ref RectangleF layoutRect, IntPtr format);
            private static FunctionWrapper<GdipAddPathString_delegate> GdipAddPathString_ptr;
            internal static Status GdipAddPathString(IntPtr path, string s, int lenght, IntPtr family, int style, float emSize, ref RectangleF layoutRect, IntPtr format) => GdipAddPathString_ptr.Delegate(path, s, lenght, family, style, emSize, ref layoutRect, format);

            private delegate Status GdipAddPathStringI_delegate(IntPtr path, [MarshalAs(UnmanagedType.LPWStr)]string s, int lenght, IntPtr family, int style, float emSize, ref Rectangle layoutRect, IntPtr format);
            private static FunctionWrapper<GdipAddPathStringI_delegate> GdipAddPathStringI_ptr;
            internal static Status GdipAddPathStringI(IntPtr path, string s, int lenght, IntPtr family, int style, float emSize, ref Rectangle layoutRect, IntPtr format) => GdipAddPathStringI_ptr.Delegate(path, s, lenght, family, style, emSize, ref layoutRect, format);

            private delegate Status GdipCreateFromHWND_delegate(IntPtr hwnd, out IntPtr graphics);
            private static FunctionWrapper<GdipCreateFromHWND_delegate> GdipCreateFromHWND_ptr;
            internal static Status GdipCreateFromHWND(IntPtr hwnd, out IntPtr graphics) => GdipCreateFromHWND_ptr.Delegate(hwnd, out graphics);

            private delegate Status GdipMeasureString_delegate(IntPtr graphics, [MarshalAs(UnmanagedType.LPWStr)]string str, int length, IntPtr font, ref RectangleF layoutRect, IntPtr stringFormat, out RectangleF boundingBox, int* codepointsFitted, int* linesFilled);
            private static FunctionWrapper<GdipMeasureString_delegate> GdipMeasureString_ptr;
            internal static Status GdipMeasureString(IntPtr graphics, string str, int length, IntPtr font, ref RectangleF layoutRect, IntPtr stringFormat, out RectangleF boundingBox, int* codepointsFitted, int* linesFilled) => GdipMeasureString_ptr.Delegate(graphics, str, length, font, ref layoutRect, stringFormat, out boundingBox, codepointsFitted, linesFilled);

            private delegate Status GdipMeasureCharacterRanges_delegate(IntPtr graphics, [MarshalAs(UnmanagedType.LPWStr)]string str, int length, IntPtr font, ref RectangleF layoutRect, IntPtr stringFormat, int regcount, out IntPtr regions);
            private static FunctionWrapper<GdipMeasureCharacterRanges_delegate> GdipMeasureCharacterRanges_ptr;
            internal static Status GdipMeasureCharacterRanges(IntPtr graphics, string str, int length, IntPtr font, ref RectangleF layoutRect, IntPtr stringFormat, int regcount, out IntPtr regions) => GdipMeasureCharacterRanges_ptr.Delegate(graphics, str, length, font, ref layoutRect, stringFormat, regcount, out regions);

            private delegate Status GdipCreateBitmapFromScan0_delegate(int width, int height, int stride, PixelFormat format, IntPtr scan0, out IntPtr bmp);
            private static FunctionWrapper<GdipCreateBitmapFromScan0_delegate> GdipCreateBitmapFromScan0_ptr;
            internal static Status GdipCreateBitmapFromScan0(int width, int height, int stride, PixelFormat format, IntPtr scan0, out IntPtr bmp) => GdipCreateBitmapFromScan0_ptr.Delegate(width, height, stride, format, scan0, out bmp);

            private delegate Status GdipCreateBitmapFromGraphics_delegate(int width, int height, IntPtr target, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromGraphics_delegate> GdipCreateBitmapFromGraphics_ptr;
            internal static Status GdipCreateBitmapFromGraphics(int width, int height, IntPtr target, out IntPtr bitmap) => GdipCreateBitmapFromGraphics_ptr.Delegate(width, height, target, out bitmap);

            private delegate Status GdipBitmapLockBits_delegate(IntPtr bmp, ref Rectangle rc, ImageLockMode flags, PixelFormat format, [In] [Out] BitmapData bmpData);
            private static FunctionWrapper<GdipBitmapLockBits_delegate> GdipBitmapLockBits_ptr;
            internal static Status GdipBitmapLockBits(IntPtr bmp, ref Rectangle rc, ImageLockMode flags, PixelFormat format, [In] [Out] BitmapData bmpData) => GdipBitmapLockBits_ptr.Delegate(bmp, ref rc, flags, format, bmpData);

            private delegate Status GdipBitmapSetResolution_delegate(IntPtr bmp, float xdpi, float ydpi);
            private static FunctionWrapper<GdipBitmapSetResolution_delegate> GdipBitmapSetResolution_ptr;
            internal static Status GdipBitmapSetResolution(IntPtr bmp, float xdpi, float ydpi) => GdipBitmapSetResolution_ptr.Delegate(bmp, xdpi, ydpi);

            private delegate Status GdipBitmapUnlockBits_delegate(IntPtr bmp, [In] [Out] BitmapData bmpData);
            private static FunctionWrapper<GdipBitmapUnlockBits_delegate> GdipBitmapUnlockBits_ptr;
            internal static Status GdipBitmapUnlockBits(IntPtr bmp, [In] [Out] BitmapData bmpData) => GdipBitmapUnlockBits_ptr.Delegate(bmp, bmpData);

            private delegate Status GdipBitmapGetPixel_delegate(IntPtr bmp, int x, int y, out int argb);
            private static FunctionWrapper<GdipBitmapGetPixel_delegate> GdipBitmapGetPixel_ptr;
            internal static Status GdipBitmapGetPixel(IntPtr bmp, int x, int y, out int argb) => GdipBitmapGetPixel_ptr.Delegate(bmp, x, y, out argb);

            private delegate Status GdipBitmapSetPixel_delegate(IntPtr bmp, int x, int y, int argb);
            private static FunctionWrapper<GdipBitmapSetPixel_delegate> GdipBitmapSetPixel_ptr;
            internal static Status GdipBitmapSetPixel(IntPtr bmp, int x, int y, int argb) => GdipBitmapSetPixel_ptr.Delegate(bmp, x, y, argb);

            private delegate Status GdipLoadImageFromFile_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr image);
            private static FunctionWrapper<GdipLoadImageFromFile_delegate> GdipLoadImageFromFile_ptr;
            internal static Status GdipLoadImageFromFile(string filename, out IntPtr image) => GdipLoadImageFromFile_ptr.Delegate(filename, out image);

            private delegate Status GdipLoadImageFromStream_delegate([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))]IStream stream, out IntPtr image);
            private static FunctionWrapper<GdipLoadImageFromStream_delegate> GdipLoadImageFromStream_ptr;
            internal static Status GdipLoadImageFromStream(IStream stream, out IntPtr image) => GdipLoadImageFromStream_ptr.Delegate(stream, out image);

            private delegate Status GdipSaveImageToStream_delegate(HandleRef image, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))]IStream stream, ref Guid clsidEncoder, HandleRef encoderParams);
            private static FunctionWrapper<GdipSaveImageToStream_delegate> GdipSaveImageToStream_ptr;
            internal static Status GdipSaveImageToStream(HandleRef image, IStream stream, ref Guid clsidEncoder, HandleRef encoderParams) => GdipSaveImageToStream_ptr.Delegate(image, stream, ref clsidEncoder, encoderParams);

            private delegate Status GdipCloneImage_delegate(IntPtr image, out IntPtr imageclone);
            private static FunctionWrapper<GdipCloneImage_delegate> GdipCloneImage_ptr;
            internal static Status GdipCloneImage(IntPtr image, out IntPtr imageclone) => GdipCloneImage_ptr.Delegate(image, out imageclone);

            private delegate Status GdipLoadImageFromFileICM_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr image);
            private static FunctionWrapper<GdipLoadImageFromFileICM_delegate> GdipLoadImageFromFileICM_ptr;
            internal static Status GdipLoadImageFromFileICM(string filename, out IntPtr image) => GdipLoadImageFromFileICM_ptr.Delegate(filename, out image);

            private delegate Status GdipCreateBitmapFromHBITMAP_delegate(IntPtr hBitMap, IntPtr gdiPalette, out IntPtr image);
            private static FunctionWrapper<GdipCreateBitmapFromHBITMAP_delegate> GdipCreateBitmapFromHBITMAP_ptr;
            internal static Status GdipCreateBitmapFromHBITMAP(IntPtr hBitMap, IntPtr gdiPalette, out IntPtr image) => GdipCreateBitmapFromHBITMAP_ptr.Delegate(hBitMap, gdiPalette, out image);

            private delegate Status GdipDisposeImage_delegate(IntPtr image);
            private static FunctionWrapper<GdipDisposeImage_delegate> GdipDisposeImage_ptr;
            internal static Status GdipDisposeImage(IntPtr image) => GdipDisposeImage_ptr.Delegate(image);
            internal static int IntGdipDisposeImage(HandleRef image) => (int)GdipDisposeImage_ptr.Delegate(image.Handle);

            private delegate Status GdipGetImageFlags_delegate(IntPtr image, out int flag);
            private static FunctionWrapper<GdipGetImageFlags_delegate> GdipGetImageFlags_ptr;
            internal static Status GdipGetImageFlags(IntPtr image, out int flag) => GdipGetImageFlags_ptr.Delegate(image, out flag);

            private delegate Status GdipGetImageType_delegate(IntPtr image, out ImageType type);
            private static FunctionWrapper<GdipGetImageType_delegate> GdipGetImageType_ptr;
            internal static Status GdipGetImageType(IntPtr image, out ImageType type) => GdipGetImageType_ptr.Delegate(image, out type);

            private delegate Status GdipImageGetFrameDimensionsCount_delegate(IntPtr image, out uint count);
            private static FunctionWrapper<GdipImageGetFrameDimensionsCount_delegate> GdipImageGetFrameDimensionsCount_ptr;
            internal static Status GdipImageGetFrameDimensionsCount(IntPtr image, out uint count) => GdipImageGetFrameDimensionsCount_ptr.Delegate(image, out count);

            private delegate Status GdipImageGetFrameDimensionsList_delegate(IntPtr image, [Out] Guid[] dimensionIDs, uint count);
            private static FunctionWrapper<GdipImageGetFrameDimensionsList_delegate> GdipImageGetFrameDimensionsList_ptr;
            internal static Status GdipImageGetFrameDimensionsList(IntPtr image, [Out] Guid[] dimensionIDs, uint count) => GdipImageGetFrameDimensionsList_ptr.Delegate(image, dimensionIDs, count);

            private delegate Status GdipGetImageHeight_delegate(IntPtr image, out uint height);
            private static FunctionWrapper<GdipGetImageHeight_delegate> GdipGetImageHeight_ptr;
            internal static Status GdipGetImageHeight(IntPtr image, out uint height) => GdipGetImageHeight_ptr.Delegate(image, out height);

            private delegate Status GdipGetImageHorizontalResolution_delegate(IntPtr image, out float resolution);
            private static FunctionWrapper<GdipGetImageHorizontalResolution_delegate> GdipGetImageHorizontalResolution_ptr;
            internal static Status GdipGetImageHorizontalResolution(IntPtr image, out float resolution) => GdipGetImageHorizontalResolution_ptr.Delegate(image, out resolution);

            private delegate Status GdipGetImagePaletteSize_delegate(IntPtr image, out int size);
            private static FunctionWrapper<GdipGetImagePaletteSize_delegate> GdipGetImagePaletteSize_ptr;
            internal static Status GdipGetImagePaletteSize(IntPtr image, out int size) => GdipGetImagePaletteSize_ptr.Delegate(image, out size);

            private delegate Status GdipGetImagePalette_delegate(IntPtr image, IntPtr palette, int size);
            private static FunctionWrapper<GdipGetImagePalette_delegate> GdipGetImagePalette_ptr;
            internal static Status GdipGetImagePalette(IntPtr image, IntPtr palette, int size) => GdipGetImagePalette_ptr.Delegate(image, palette, size);

            private delegate Status GdipSetImagePalette_delegate(IntPtr image, IntPtr palette);
            private static FunctionWrapper<GdipSetImagePalette_delegate> GdipSetImagePalette_ptr;
            internal static Status GdipSetImagePalette(IntPtr image, IntPtr palette) => GdipSetImagePalette_ptr.Delegate(image, palette);

            private delegate Status GdipGetImageDimension_delegate(IntPtr image, out float width, out float height);
            private static FunctionWrapper<GdipGetImageDimension_delegate> GdipGetImageDimension_ptr;
            internal static Status GdipGetImageDimension(IntPtr image, out float width, out float height) => GdipGetImageDimension_ptr.Delegate(image, out width, out height);

            private delegate Status GdipGetImagePixelFormat_delegate(IntPtr image, out PixelFormat format);
            private static FunctionWrapper<GdipGetImagePixelFormat_delegate> GdipGetImagePixelFormat_ptr;
            internal static Status GdipGetImagePixelFormat(IntPtr image, out PixelFormat format) => GdipGetImagePixelFormat_ptr.Delegate(image, out format);

            private delegate Status GdipGetPropertyCount_delegate(IntPtr image, out uint propNumbers);
            private static FunctionWrapper<GdipGetPropertyCount_delegate> GdipGetPropertyCount_ptr;
            internal static Status GdipGetPropertyCount(IntPtr image, out uint propNumbers) => GdipGetPropertyCount_ptr.Delegate(image, out propNumbers);

            private delegate Status GdipGetPropertyIdList_delegate(IntPtr image, uint propNumbers, [Out] int[] list);
            private static FunctionWrapper<GdipGetPropertyIdList_delegate> GdipGetPropertyIdList_ptr;
            internal static Status GdipGetPropertyIdList(IntPtr image, uint propNumbers, [Out] int[] list) => GdipGetPropertyIdList_ptr.Delegate(image, propNumbers, list);

            private delegate Status GdipGetPropertySize_delegate(IntPtr image, out int bufferSize, out int propNumbers);
            private static FunctionWrapper<GdipGetPropertySize_delegate> GdipGetPropertySize_ptr;
            internal static Status GdipGetPropertySize(IntPtr image, out int bufferSize, out int propNumbers) => GdipGetPropertySize_ptr.Delegate(image, out bufferSize, out propNumbers);

            private delegate Status GdipGetAllPropertyItems_delegate(IntPtr image, int bufferSize, int propNumbers, IntPtr items);
            private static FunctionWrapper<GdipGetAllPropertyItems_delegate> GdipGetAllPropertyItems_ptr;
            internal static Status GdipGetAllPropertyItems(IntPtr image, int bufferSize, int propNumbers, IntPtr items) => GdipGetAllPropertyItems_ptr.Delegate(image, bufferSize, propNumbers, items);

            private delegate Status GdipGetImageRawFormat_delegate(IntPtr image, out Guid format);
            private static FunctionWrapper<GdipGetImageRawFormat_delegate> GdipGetImageRawFormat_ptr;
            internal static Status GdipGetImageRawFormat(IntPtr image, out Guid format) => GdipGetImageRawFormat_ptr.Delegate(image, out format);

            private delegate Status GdipGetImageVerticalResolution_delegate(IntPtr image, out float resolution);
            private static FunctionWrapper<GdipGetImageVerticalResolution_delegate> GdipGetImageVerticalResolution_ptr;
            internal static Status GdipGetImageVerticalResolution(IntPtr image, out float resolution) => GdipGetImageVerticalResolution_ptr.Delegate(image, out resolution);

            private delegate Status GdipGetImageWidth_delegate(IntPtr image, out uint width);
            private static FunctionWrapper<GdipGetImageWidth_delegate> GdipGetImageWidth_ptr;
            internal static Status GdipGetImageWidth(IntPtr image, out uint width) => GdipGetImageWidth_ptr.Delegate(image, out width);

            private delegate Status GdipGetImageBounds_delegate(IntPtr image, out RectangleF source, ref GraphicsUnit unit);
            private static FunctionWrapper<GdipGetImageBounds_delegate> GdipGetImageBounds_ptr;
            internal static Status GdipGetImageBounds(IntPtr image, out RectangleF source, ref GraphicsUnit unit) => GdipGetImageBounds_ptr.Delegate(image, out source, ref unit);

            private delegate Status GdipGetEncoderParameterListSize_delegate(IntPtr image, ref Guid encoder, out uint size);
            private static FunctionWrapper<GdipGetEncoderParameterListSize_delegate> GdipGetEncoderParameterListSize_ptr;
            internal static Status GdipGetEncoderParameterListSize(IntPtr image, ref Guid encoder, out uint size) => GdipGetEncoderParameterListSize_ptr.Delegate(image, ref encoder, out size);

            private delegate Status GdipGetEncoderParameterList_delegate(IntPtr image, ref Guid encoder, uint size, IntPtr buffer);
            private static FunctionWrapper<GdipGetEncoderParameterList_delegate> GdipGetEncoderParameterList_ptr;
            internal static Status GdipGetEncoderParameterList(IntPtr image, ref Guid encoder, uint size, IntPtr buffer) => GdipGetEncoderParameterList_ptr.Delegate(image, ref encoder, size, buffer);

            private delegate Status GdipImageGetFrameCount_delegate(IntPtr image, ref Guid guidDimension, out uint count);
            private static FunctionWrapper<GdipImageGetFrameCount_delegate> GdipImageGetFrameCount_ptr;
            internal static Status GdipImageGetFrameCount(IntPtr image, ref Guid guidDimension, out uint count) => GdipImageGetFrameCount_ptr.Delegate(image, ref guidDimension, out count);

            private delegate Status GdipImageSelectActiveFrame_delegate(IntPtr image, ref Guid guidDimension, int frameIndex);
            private static FunctionWrapper<GdipImageSelectActiveFrame_delegate> GdipImageSelectActiveFrame_ptr;
            internal static Status GdipImageSelectActiveFrame(IntPtr image, ref Guid guidDimension, int frameIndex) => GdipImageSelectActiveFrame_ptr.Delegate(image, ref guidDimension, frameIndex);

            private delegate Status GdipGetPropertyItemSize_delegate(IntPtr image, int propertyID, out int propertySize);
            private static FunctionWrapper<GdipGetPropertyItemSize_delegate> GdipGetPropertyItemSize_ptr;
            internal static Status GdipGetPropertyItemSize(IntPtr image, int propertyID, out int propertySize) => GdipGetPropertyItemSize_ptr.Delegate(image, propertyID, out propertySize);

            private delegate Status GdipGetPropertyItem_delegate(IntPtr image, int propertyID, int propertySize, IntPtr buffer);
            private static FunctionWrapper<GdipGetPropertyItem_delegate> GdipGetPropertyItem_ptr;
            internal static Status GdipGetPropertyItem(IntPtr image, int propertyID, int propertySize, IntPtr buffer) => GdipGetPropertyItem_ptr.Delegate(image, propertyID, propertySize, buffer);

            private delegate Status GdipRemovePropertyItem_delegate(IntPtr image, int propertyId);
            private static FunctionWrapper<GdipRemovePropertyItem_delegate> GdipRemovePropertyItem_ptr;
            internal static Status GdipRemovePropertyItem(IntPtr image, int propertyId) => GdipRemovePropertyItem_ptr.Delegate(image, propertyId);

            private delegate Status GdipSetPropertyItem_delegate(IntPtr image, GdipPropertyItem* propertyItem);
            private static FunctionWrapper<GdipSetPropertyItem_delegate> GdipSetPropertyItem_ptr;
            internal static Status GdipSetPropertyItem(IntPtr image, GdipPropertyItem* propertyItem) => GdipSetPropertyItem_ptr.Delegate(image, propertyItem);

            private delegate Status GdipGetImageThumbnail_delegate(IntPtr image, uint width, uint height, out IntPtr thumbImage, IntPtr callback, IntPtr callBackData);
            private static FunctionWrapper<GdipGetImageThumbnail_delegate> GdipGetImageThumbnail_ptr;
            internal static Status GdipGetImageThumbnail(IntPtr image, uint width, uint height, out IntPtr thumbImage, IntPtr callback, IntPtr callBackData) => GdipGetImageThumbnail_ptr.Delegate(image, width, height, out thumbImage, callback, callBackData);

            private delegate Status GdipImageRotateFlip_delegate(IntPtr image, RotateFlipType rotateFlipType);
            private static FunctionWrapper<GdipImageRotateFlip_delegate> GdipImageRotateFlip_ptr;
            internal static Status GdipImageRotateFlip(IntPtr image, RotateFlipType rotateFlipType) => GdipImageRotateFlip_ptr.Delegate(image, rotateFlipType);

            private delegate Status GdipSaveImageToFile_delegate(IntPtr image, [MarshalAs(UnmanagedType.LPWStr)]string filename, ref Guid encoderClsID, IntPtr encoderParameters);
            private static FunctionWrapper<GdipSaveImageToFile_delegate> GdipSaveImageToFile_ptr;
            internal static Status GdipSaveImageToFile(IntPtr image, string filename, ref Guid encoderClsID, IntPtr encoderParameters) => GdipSaveImageToFile_ptr.Delegate(image, filename, ref encoderClsID, encoderParameters);

            private delegate Status GdipSaveAdd_delegate(IntPtr image, IntPtr encoderParameters);
            private static FunctionWrapper<GdipSaveAdd_delegate> GdipSaveAdd_ptr;
            internal static Status GdipSaveAdd(IntPtr image, IntPtr encoderParameters) => GdipSaveAdd_ptr.Delegate(image, encoderParameters);

            private delegate Status GdipSaveAddImage_delegate(IntPtr image, IntPtr imagenew, IntPtr encoderParameters);
            private static FunctionWrapper<GdipSaveAddImage_delegate> GdipSaveAddImage_ptr;
            internal static Status GdipSaveAddImage(IntPtr image, IntPtr imagenew, IntPtr encoderParameters) => GdipSaveAddImage_ptr.Delegate(image, imagenew, encoderParameters);

            private delegate Status GdipDrawImageI_delegate(IntPtr graphics, IntPtr image, int x, int y);
            private static FunctionWrapper<GdipDrawImageI_delegate> GdipDrawImageI_ptr;
            internal static Status GdipDrawImageI(IntPtr graphics, IntPtr image, int x, int y) => GdipDrawImageI_ptr.Delegate(graphics, image, x, y);

            private delegate Status GdipGetImageGraphicsContext_delegate(IntPtr image, out IntPtr graphics);
            private static FunctionWrapper<GdipGetImageGraphicsContext_delegate> GdipGetImageGraphicsContext_ptr;
            internal static Status GdipGetImageGraphicsContext(IntPtr image, out IntPtr graphics) => GdipGetImageGraphicsContext_ptr.Delegate(image, out graphics);

            private delegate Status GdipDrawImage_delegate(IntPtr graphics, IntPtr image, float x, float y);
            private static FunctionWrapper<GdipDrawImage_delegate> GdipDrawImage_ptr;
            internal static Status GdipDrawImage(IntPtr graphics, IntPtr image, float x, float y) => GdipDrawImage_ptr.Delegate(graphics, image, x, y);

            private delegate Status GdipDrawImagePoints_delegate(IntPtr graphics, IntPtr image, PointF[] destPoints, int count);
            private static FunctionWrapper<GdipDrawImagePoints_delegate> GdipDrawImagePoints_ptr;
            internal static Status GdipDrawImagePoints(IntPtr graphics, IntPtr image, PointF[] destPoints, int count) => GdipDrawImagePoints_ptr.Delegate(graphics, image, destPoints, count);

            private delegate Status GdipDrawImagePointsI_delegate(IntPtr graphics, IntPtr image, Point[] destPoints, int count);
            private static FunctionWrapper<GdipDrawImagePointsI_delegate> GdipDrawImagePointsI_ptr;
            internal static Status GdipDrawImagePointsI(IntPtr graphics, IntPtr image, Point[] destPoints, int count) => GdipDrawImagePointsI_ptr.Delegate(graphics, image, destPoints, count);

            private delegate Status GdipDrawImageRectRectI_delegate(IntPtr graphics, IntPtr image, int dstx, int dsty, int dstwidth, int dstheight, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);
            private static FunctionWrapper<GdipDrawImageRectRectI_delegate> GdipDrawImageRectRectI_ptr;
            internal static Status GdipDrawImageRectRectI(IntPtr graphics, IntPtr image, int dstx, int dsty, int dstwidth, int dstheight, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData) => GdipDrawImageRectRectI_ptr.Delegate(graphics, image, dstx, dsty, dstwidth, dstheight, srcx, srcy, srcwidth, srcheight, srcUnit, imageattr, callback, callbackData);

            private delegate Status GdipDrawImageRectRect_delegate(IntPtr graphics, IntPtr image, float dstx, float dsty, float dstwidth, float dstheight, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);
            private static FunctionWrapper<GdipDrawImageRectRect_delegate> GdipDrawImageRectRect_ptr;
            internal static Status GdipDrawImageRectRect(IntPtr graphics, IntPtr image, float dstx, float dsty, float dstwidth, float dstheight, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData) => GdipDrawImageRectRect_ptr.Delegate(graphics, image, dstx, dsty, dstwidth, dstheight, srcx, srcy, srcwidth, srcheight, srcUnit, imageattr, callback, callbackData);

            private delegate Status GdipDrawImagePointsRectI_delegate(IntPtr graphics, IntPtr image, Point[] destPoints, int count, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);
            private static FunctionWrapper<GdipDrawImagePointsRectI_delegate> GdipDrawImagePointsRectI_ptr;
            internal static Status GdipDrawImagePointsRectI(IntPtr graphics, IntPtr image, Point[] destPoints, int count, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData) => GdipDrawImagePointsRectI_ptr.Delegate(graphics, image, destPoints, count, srcx, srcy, srcwidth, srcheight, srcUnit, imageattr, callback, callbackData);

            private delegate Status GdipDrawImagePointsRect_delegate(IntPtr graphics, IntPtr image, PointF[] destPoints, int count, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);
            private static FunctionWrapper<GdipDrawImagePointsRect_delegate> GdipDrawImagePointsRect_ptr;
            internal static Status GdipDrawImagePointsRect(IntPtr graphics, IntPtr image, PointF[] destPoints, int count, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData) => GdipDrawImagePointsRect_ptr.Delegate(graphics, image, destPoints, count, srcx, srcy, srcwidth, srcheight, srcUnit, imageattr, callback, callbackData);

            private delegate Status GdipDrawImageRect_delegate(IntPtr graphics, IntPtr image, float x, float y, float width, float height);
            private static FunctionWrapper<GdipDrawImageRect_delegate> GdipDrawImageRect_ptr;
            internal static Status GdipDrawImageRect(IntPtr graphics, IntPtr image, float x, float y, float width, float height) => GdipDrawImageRect_ptr.Delegate(graphics, image, x, y, width, height);

            private delegate Status GdipDrawImagePointRect_delegate(IntPtr graphics, IntPtr image, float x, float y, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit);
            private static FunctionWrapper<GdipDrawImagePointRect_delegate> GdipDrawImagePointRect_ptr;
            internal static Status GdipDrawImagePointRect(IntPtr graphics, IntPtr image, float x, float y, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit) => GdipDrawImagePointRect_ptr.Delegate(graphics, image, x, y, srcx, srcy, srcwidth, srcheight, srcUnit);

            private delegate Status GdipDrawImagePointRectI_delegate(IntPtr graphics, IntPtr image, int x, int y, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit);
            private static FunctionWrapper<GdipDrawImagePointRectI_delegate> GdipDrawImagePointRectI_ptr;
            internal static Status GdipDrawImagePointRectI(IntPtr graphics, IntPtr image, int x, int y, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit) => GdipDrawImagePointRectI_ptr.Delegate(graphics, image, x, y, srcx, srcy, srcwidth, srcheight, srcUnit);

            private delegate Status GdipCreateHBITMAPFromBitmap_delegate(IntPtr bmp, out IntPtr HandleBmp, int clrbackground);
            private static FunctionWrapper<GdipCreateHBITMAPFromBitmap_delegate> GdipCreateHBITMAPFromBitmap_ptr;
            internal static Status GdipCreateHBITMAPFromBitmap(IntPtr bmp, out IntPtr HandleBmp, int clrbackground) => GdipCreateHBITMAPFromBitmap_ptr.Delegate(bmp, out HandleBmp, clrbackground);

            private delegate Status GdipCreateBitmapFromFile_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromFile_delegate> GdipCreateBitmapFromFile_ptr;
            internal static Status GdipCreateBitmapFromFile(string filename, out IntPtr bitmap) => GdipCreateBitmapFromFile_ptr.Delegate(filename, out bitmap);

            private delegate Status GdipCreateBitmapFromFileICM_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromFileICM_delegate> GdipCreateBitmapFromFileICM_ptr;
            internal static Status GdipCreateBitmapFromFileICM(string filename, out IntPtr bitmap) => GdipCreateBitmapFromFileICM_ptr.Delegate(filename, out bitmap);

            private delegate Status GdipCreateHICONFromBitmap_delegate(IntPtr bmp, out IntPtr HandleIcon);
            private static FunctionWrapper<GdipCreateHICONFromBitmap_delegate> GdipCreateHICONFromBitmap_ptr;
            internal static Status GdipCreateHICONFromBitmap(IntPtr bmp, out IntPtr HandleIcon) => GdipCreateHICONFromBitmap_ptr.Delegate(bmp, out HandleIcon);

            private delegate Status GdipCreateBitmapFromHICON_delegate(IntPtr hicon, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromHICON_delegate> GdipCreateBitmapFromHICON_ptr;
            internal static Status GdipCreateBitmapFromHICON(IntPtr hicon, out IntPtr bitmap) => GdipCreateBitmapFromHICON_ptr.Delegate(hicon, out bitmap);

            private delegate Status GdipCreateBitmapFromResource_delegate(IntPtr hInstance, [MarshalAs(UnmanagedType.LPWStr)]string lpBitmapName, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromResource_delegate> GdipCreateBitmapFromResource_ptr;
            internal static Status GdipCreateBitmapFromResource(IntPtr hInstance, string lpBitmapName, out IntPtr bitmap) => GdipCreateBitmapFromResource_ptr.Delegate(hInstance, lpBitmapName, out bitmap);

            private delegate Status GdipCreatePath_delegate(FillMode brushMode, out IntPtr path);
            private static FunctionWrapper<GdipCreatePath_delegate> GdipCreatePath_ptr;
            internal static Status GdipCreatePath(FillMode brushMode, out IntPtr path) => GdipCreatePath_ptr.Delegate(brushMode, out path);

            private delegate Status GdipCreatePath2_delegate(PointF[] points, byte[] types, int count, FillMode brushMode, out IntPtr path);
            private static FunctionWrapper<GdipCreatePath2_delegate> GdipCreatePath2_ptr;
            internal static Status GdipCreatePath2(PointF[] points, byte[] types, int count, FillMode brushMode, out IntPtr path) => GdipCreatePath2_ptr.Delegate(points, types, count, brushMode, out path);

            private delegate Status GdipCreatePath2I_delegate(Point[] points, byte[] types, int count, FillMode brushMode, out IntPtr path);
            private static FunctionWrapper<GdipCreatePath2I_delegate> GdipCreatePath2I_ptr;
            internal static Status GdipCreatePath2I(Point[] points, byte[] types, int count, FillMode brushMode, out IntPtr path) => GdipCreatePath2I_ptr.Delegate(points, types, count, brushMode, out path);

            private delegate Status GdipClonePath_delegate(IntPtr path, out IntPtr clonePath);
            private static FunctionWrapper<GdipClonePath_delegate> GdipClonePath_ptr;
            internal static Status GdipClonePath(IntPtr path, out IntPtr clonePath) => GdipClonePath_ptr.Delegate(path, out clonePath);

            private delegate Status GdipDeletePath_delegate(IntPtr path);
            private static FunctionWrapper<GdipDeletePath_delegate> GdipDeletePath_ptr;
            internal static Status GdipDeletePath(IntPtr path) => GdipDeletePath_ptr.Delegate(path);
            internal static int IntGdipDeletePath(HandleRef path) => (int)GdipDeletePath_ptr.Delegate(path.Handle);

            private delegate Status GdipResetPath_delegate(IntPtr path);
            private static FunctionWrapper<GdipResetPath_delegate> GdipResetPath_ptr;
            internal static Status GdipResetPath(IntPtr path) => GdipResetPath_ptr.Delegate(path);

            private delegate Status GdipGetPointCount_delegate(IntPtr path, out int count);
            private static FunctionWrapper<GdipGetPointCount_delegate> GdipGetPointCount_ptr;
            internal static Status GdipGetPointCount(IntPtr path, out int count) => GdipGetPointCount_ptr.Delegate(path, out count);

            private delegate Status GdipGetPathTypes_delegate(IntPtr path, [Out] byte[] types, int count);
            private static FunctionWrapper<GdipGetPathTypes_delegate> GdipGetPathTypes_ptr;
            internal static Status GdipGetPathTypes(IntPtr path, [Out] byte[] types, int count) => GdipGetPathTypes_ptr.Delegate(path, types, count);

            private delegate Status GdipGetPathPoints_delegate(IntPtr path, [Out] PointF[] points, int count);
            private static FunctionWrapper<GdipGetPathPoints_delegate> GdipGetPathPoints_ptr;
            internal static Status GdipGetPathPoints(IntPtr path, [Out] PointF[] points, int count) => GdipGetPathPoints_ptr.Delegate(path, points, count);

            private delegate Status GdipGetPathPointsI_delegate(IntPtr path, [Out] Point[] points, int count);
            private static FunctionWrapper<GdipGetPathPointsI_delegate> GdipGetPathPointsI_ptr;
            internal static Status GdipGetPathPointsI(IntPtr path, [Out] Point[] points, int count) => GdipGetPathPointsI_ptr.Delegate(path, points, count);

            private delegate Status GdipGetPathFillMode_delegate(IntPtr path, out FillMode fillMode);
            private static FunctionWrapper<GdipGetPathFillMode_delegate> GdipGetPathFillMode_ptr;
            internal static Status GdipGetPathFillMode(IntPtr path, out FillMode fillMode) => GdipGetPathFillMode_ptr.Delegate(path, out fillMode);

            private delegate Status GdipSetPathFillMode_delegate(IntPtr path, FillMode fillMode);
            private static FunctionWrapper<GdipSetPathFillMode_delegate> GdipSetPathFillMode_ptr;
            internal static Status GdipSetPathFillMode(IntPtr path, FillMode fillMode) => GdipSetPathFillMode_ptr.Delegate(path, fillMode);

            private delegate Status GdipStartPathFigure_delegate(IntPtr path);
            private static FunctionWrapper<GdipStartPathFigure_delegate> GdipStartPathFigure_ptr;
            internal static Status GdipStartPathFigure(IntPtr path) => GdipStartPathFigure_ptr.Delegate(path);

            private delegate Status GdipClosePathFigure_delegate(IntPtr path);
            private static FunctionWrapper<GdipClosePathFigure_delegate> GdipClosePathFigure_ptr;
            internal static Status GdipClosePathFigure(IntPtr path) => GdipClosePathFigure_ptr.Delegate(path);

            private delegate Status GdipClosePathFigures_delegate(IntPtr path);
            private static FunctionWrapper<GdipClosePathFigures_delegate> GdipClosePathFigures_ptr;
            internal static Status GdipClosePathFigures(IntPtr path) => GdipClosePathFigures_ptr.Delegate(path);

            private delegate Status GdipSetPathMarker_delegate(IntPtr path);
            private static FunctionWrapper<GdipSetPathMarker_delegate> GdipSetPathMarker_ptr;
            internal static Status GdipSetPathMarker(IntPtr path) => GdipSetPathMarker_ptr.Delegate(path);

            private delegate Status GdipClearPathMarkers_delegate(IntPtr path);
            private static FunctionWrapper<GdipClearPathMarkers_delegate> GdipClearPathMarkers_ptr;
            internal static Status GdipClearPathMarkers(IntPtr path) => GdipClearPathMarkers_ptr.Delegate(path);

            private delegate Status GdipReversePath_delegate(IntPtr path);
            private static FunctionWrapper<GdipReversePath_delegate> GdipReversePath_ptr;
            internal static Status GdipReversePath(IntPtr path) => GdipReversePath_ptr.Delegate(path);

            private delegate Status GdipGetPathLastPoint_delegate(IntPtr path, out PointF lastPoint);
            private static FunctionWrapper<GdipGetPathLastPoint_delegate> GdipGetPathLastPoint_ptr;
            internal static Status GdipGetPathLastPoint(IntPtr path, out PointF lastPoint) => GdipGetPathLastPoint_ptr.Delegate(path, out lastPoint);

            private delegate Status GdipAddPathLine_delegate(IntPtr path, float x1, float y1, float x2, float y2);
            private static FunctionWrapper<GdipAddPathLine_delegate> GdipAddPathLine_ptr;
            internal static Status GdipAddPathLine(IntPtr path, float x1, float y1, float x2, float y2) => GdipAddPathLine_ptr.Delegate(path, x1, y1, x2, y2);

            private delegate Status GdipAddPathLine2_delegate(IntPtr path, PointF[] points, int count);
            private static FunctionWrapper<GdipAddPathLine2_delegate> GdipAddPathLine2_ptr;
            internal static Status GdipAddPathLine2(IntPtr path, PointF[] points, int count) => GdipAddPathLine2_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathLine2I_delegate(IntPtr path, Point[] points, int count);
            private static FunctionWrapper<GdipAddPathLine2I_delegate> GdipAddPathLine2I_ptr;
            internal static Status GdipAddPathLine2I(IntPtr path, Point[] points, int count) => GdipAddPathLine2I_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathArc_delegate(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathArc_delegate> GdipAddPathArc_ptr;
            internal static Status GdipAddPathArc(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipAddPathArc_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipAddPathBezier_delegate(IntPtr path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);
            private static FunctionWrapper<GdipAddPathBezier_delegate> GdipAddPathBezier_ptr;
            internal static Status GdipAddPathBezier(IntPtr path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) => GdipAddPathBezier_ptr.Delegate(path, x1, y1, x2, y2, x3, y3, x4, y4);

            private delegate Status GdipAddPathBeziers_delegate(IntPtr path, PointF[] points, int count);
            private static FunctionWrapper<GdipAddPathBeziers_delegate> GdipAddPathBeziers_ptr;
            internal static Status GdipAddPathBeziers(IntPtr path, PointF[] points, int count) => GdipAddPathBeziers_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathCurve_delegate(IntPtr path, PointF[] points, int count);
            private static FunctionWrapper<GdipAddPathCurve_delegate> GdipAddPathCurve_ptr;
            internal static Status GdipAddPathCurve(IntPtr path, PointF[] points, int count) => GdipAddPathCurve_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathCurveI_delegate(IntPtr path, Point[] points, int count);
            private static FunctionWrapper<GdipAddPathCurveI_delegate> GdipAddPathCurveI_ptr;
            internal static Status GdipAddPathCurveI(IntPtr path, Point[] points, int count) => GdipAddPathCurveI_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathCurve2_delegate(IntPtr path, PointF[] points, int count, float tension);
            private static FunctionWrapper<GdipAddPathCurve2_delegate> GdipAddPathCurve2_ptr;
            internal static Status GdipAddPathCurve2(IntPtr path, PointF[] points, int count, float tension) => GdipAddPathCurve2_ptr.Delegate(path, points, count, tension);

            private delegate Status GdipAddPathCurve2I_delegate(IntPtr path, Point[] points, int count, float tension);
            private static FunctionWrapper<GdipAddPathCurve2I_delegate> GdipAddPathCurve2I_ptr;
            internal static Status GdipAddPathCurve2I(IntPtr path, Point[] points, int count, float tension) => GdipAddPathCurve2I_ptr.Delegate(path, points, count, tension);

            private delegate Status GdipAddPathCurve3_delegate(IntPtr path, PointF[] points, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipAddPathCurve3_delegate> GdipAddPathCurve3_ptr;
            internal static Status GdipAddPathCurve3(IntPtr path, PointF[] points, int count, int offset, int numberOfSegments, float tension) => GdipAddPathCurve3_ptr.Delegate(path, points, count, offset, numberOfSegments, tension);

            private delegate Status GdipAddPathCurve3I_delegate(IntPtr path, Point[] points, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipAddPathCurve3I_delegate> GdipAddPathCurve3I_ptr;
            internal static Status GdipAddPathCurve3I(IntPtr path, Point[] points, int count, int offset, int numberOfSegments, float tension) => GdipAddPathCurve3I_ptr.Delegate(path, points, count, offset, numberOfSegments, tension);

            private delegate Status GdipAddPathClosedCurve_delegate(IntPtr path, PointF[] points, int count);
            private static FunctionWrapper<GdipAddPathClosedCurve_delegate> GdipAddPathClosedCurve_ptr;
            internal static Status GdipAddPathClosedCurve(IntPtr path, PointF[] points, int count) => GdipAddPathClosedCurve_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathClosedCurveI_delegate(IntPtr path, Point[] points, int count);
            private static FunctionWrapper<GdipAddPathClosedCurveI_delegate> GdipAddPathClosedCurveI_ptr;
            internal static Status GdipAddPathClosedCurveI(IntPtr path, Point[] points, int count) => GdipAddPathClosedCurveI_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathClosedCurve2_delegate(IntPtr path, PointF[] points, int count, float tension);
            private static FunctionWrapper<GdipAddPathClosedCurve2_delegate> GdipAddPathClosedCurve2_ptr;
            internal static Status GdipAddPathClosedCurve2(IntPtr path, PointF[] points, int count, float tension) => GdipAddPathClosedCurve2_ptr.Delegate(path, points, count, tension);

            private delegate Status GdipAddPathClosedCurve2I_delegate(IntPtr path, Point[] points, int count, float tension);
            private static FunctionWrapper<GdipAddPathClosedCurve2I_delegate> GdipAddPathClosedCurve2I_ptr;
            internal static Status GdipAddPathClosedCurve2I(IntPtr path, Point[] points, int count, float tension) => GdipAddPathClosedCurve2I_ptr.Delegate(path, points, count, tension);

            private delegate Status GdipAddPathRectangle_delegate(IntPtr path, float x, float y, float width, float height);
            private static FunctionWrapper<GdipAddPathRectangle_delegate> GdipAddPathRectangle_ptr;
            internal static Status GdipAddPathRectangle(IntPtr path, float x, float y, float width, float height) => GdipAddPathRectangle_ptr.Delegate(path, x, y, width, height);

            private delegate Status GdipAddPathRectangles_delegate(IntPtr path, RectangleF[] rects, int count);
            private static FunctionWrapper<GdipAddPathRectangles_delegate> GdipAddPathRectangles_ptr;
            internal static Status GdipAddPathRectangles(IntPtr path, RectangleF[] rects, int count) => GdipAddPathRectangles_ptr.Delegate(path, rects, count);

            private delegate Status GdipAddPathEllipse_delegate(IntPtr path, float x, float y, float width, float height);
            private static FunctionWrapper<GdipAddPathEllipse_delegate> GdipAddPathEllipse_ptr;
            internal static Status GdipAddPathEllipse(IntPtr path, float x, float y, float width, float height) => GdipAddPathEllipse_ptr.Delegate(path, x, y, width, height);

            private delegate Status GdipAddPathEllipseI_delegate(IntPtr path, int x, int y, int width, int height);
            private static FunctionWrapper<GdipAddPathEllipseI_delegate> GdipAddPathEllipseI_ptr;
            internal static Status GdipAddPathEllipseI(IntPtr path, int x, int y, int width, int height) => GdipAddPathEllipseI_ptr.Delegate(path, x, y, width, height);

            private delegate Status GdipAddPathPie_delegate(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathPie_delegate> GdipAddPathPie_ptr;
            internal static Status GdipAddPathPie(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipAddPathPie_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipAddPathPieI_delegate(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathPieI_delegate> GdipAddPathPieI_ptr;
            internal static Status GdipAddPathPieI(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipAddPathPieI_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipAddPathPolygon_delegate(IntPtr path, PointF[] points, int count);
            private static FunctionWrapper<GdipAddPathPolygon_delegate> GdipAddPathPolygon_ptr;
            internal static Status GdipAddPathPolygon(IntPtr path, PointF[] points, int count) => GdipAddPathPolygon_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathPath_delegate(IntPtr path, IntPtr addingPath, bool connect);
            private static FunctionWrapper<GdipAddPathPath_delegate> GdipAddPathPath_ptr;
            internal static Status GdipAddPathPath(IntPtr path, IntPtr addingPath, bool connect) => GdipAddPathPath_ptr.Delegate(path, addingPath, connect);

            private delegate Status GdipAddPathLineI_delegate(IntPtr path, int x1, int y1, int x2, int y2);
            private static FunctionWrapper<GdipAddPathLineI_delegate> GdipAddPathLineI_ptr;
            internal static Status GdipAddPathLineI(IntPtr path, int x1, int y1, int x2, int y2) => GdipAddPathLineI_ptr.Delegate(path, x1, y1, x2, y2);

            private delegate Status GdipAddPathArcI_delegate(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathArcI_delegate> GdipAddPathArcI_ptr;
            internal static Status GdipAddPathArcI(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipAddPathArcI_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipAddPathBezierI_delegate(IntPtr path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);
            private static FunctionWrapper<GdipAddPathBezierI_delegate> GdipAddPathBezierI_ptr;
            internal static Status GdipAddPathBezierI(IntPtr path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4) => GdipAddPathBezierI_ptr.Delegate(path, x1, y1, x2, y2, x3, y3, x4, y4);

            private delegate Status GdipAddPathBeziersI_delegate(IntPtr path, Point[] points, int count);
            private static FunctionWrapper<GdipAddPathBeziersI_delegate> GdipAddPathBeziersI_ptr;
            internal static Status GdipAddPathBeziersI(IntPtr path, Point[] points, int count) => GdipAddPathBeziersI_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathPolygonI_delegate(IntPtr path, Point[] points, int count);
            private static FunctionWrapper<GdipAddPathPolygonI_delegate> GdipAddPathPolygonI_ptr;
            internal static Status GdipAddPathPolygonI(IntPtr path, Point[] points, int count) => GdipAddPathPolygonI_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathRectangleI_delegate(IntPtr path, int x, int y, int width, int height);
            private static FunctionWrapper<GdipAddPathRectangleI_delegate> GdipAddPathRectangleI_ptr;
            internal static Status GdipAddPathRectangleI(IntPtr path, int x, int y, int width, int height) => GdipAddPathRectangleI_ptr.Delegate(path, x, y, width, height);

            private delegate Status GdipAddPathRectanglesI_delegate(IntPtr path, Rectangle[] rects, int count);
            private static FunctionWrapper<GdipAddPathRectanglesI_delegate> GdipAddPathRectanglesI_ptr;
            internal static Status GdipAddPathRectanglesI(IntPtr path, Rectangle[] rects, int count) => GdipAddPathRectanglesI_ptr.Delegate(path, rects, count);

            private delegate Status GdipFlattenPath_delegate(IntPtr path, IntPtr matrix, float floatness);
            private static FunctionWrapper<GdipFlattenPath_delegate> GdipFlattenPath_ptr;
            internal static Status GdipFlattenPath(IntPtr path, IntPtr matrix, float floatness) => GdipFlattenPath_ptr.Delegate(path, matrix, floatness);

            private delegate Status GdipTransformPath_delegate(IntPtr path, IntPtr matrix);
            private static FunctionWrapper<GdipTransformPath_delegate> GdipTransformPath_ptr;
            internal static Status GdipTransformPath(IntPtr path, IntPtr matrix) => GdipTransformPath_ptr.Delegate(path, matrix);

            private delegate Status GdipWarpPath_delegate(IntPtr path, IntPtr matrix, PointF[] points, int count, float srcx, float srcy, float srcwidth, float srcheight, WarpMode mode, float flatness);
            private static FunctionWrapper<GdipWarpPath_delegate> GdipWarpPath_ptr;
            internal static Status GdipWarpPath(IntPtr path, IntPtr matrix, PointF[] points, int count, float srcx, float srcy, float srcwidth, float srcheight, WarpMode mode, float flatness) => GdipWarpPath_ptr.Delegate(path, matrix, points, count, srcx, srcy, srcwidth, srcheight, mode, flatness);

            private delegate Status GdipWidenPath_delegate(IntPtr path, IntPtr pen, IntPtr matrix, float flatness);
            private static FunctionWrapper<GdipWidenPath_delegate> GdipWidenPath_ptr;
            internal static Status GdipWidenPath(IntPtr path, IntPtr pen, IntPtr matrix, float flatness) => GdipWidenPath_ptr.Delegate(path, pen, matrix, flatness);

            private delegate Status GdipGetPathWorldBounds_delegate(IntPtr path, out RectangleF bounds, IntPtr matrix, IntPtr pen);
            private static FunctionWrapper<GdipGetPathWorldBounds_delegate> GdipGetPathWorldBounds_ptr;
            internal static Status GdipGetPathWorldBounds(IntPtr path, out RectangleF bounds, IntPtr matrix, IntPtr pen) => GdipGetPathWorldBounds_ptr.Delegate(path, out bounds, matrix, pen);

            private delegate Status GdipGetPathWorldBoundsI_delegate(IntPtr path, out Rectangle bounds, IntPtr matrix, IntPtr pen);
            private static FunctionWrapper<GdipGetPathWorldBoundsI_delegate> GdipGetPathWorldBoundsI_ptr;
            internal static Status GdipGetPathWorldBoundsI(IntPtr path, out Rectangle bounds, IntPtr matrix, IntPtr pen) => GdipGetPathWorldBoundsI_ptr.Delegate(path, out bounds, matrix, pen);

            private delegate Status GdipIsVisiblePathPoint_delegate(IntPtr path, float x, float y, IntPtr graphics, out bool result);
            private static FunctionWrapper<GdipIsVisiblePathPoint_delegate> GdipIsVisiblePathPoint_ptr;
            internal static Status GdipIsVisiblePathPoint(IntPtr path, float x, float y, IntPtr graphics, out bool result) => GdipIsVisiblePathPoint_ptr.Delegate(path, x, y, graphics, out result);

            private delegate Status GdipIsVisiblePathPointI_delegate(IntPtr path, int x, int y, IntPtr graphics, out bool result);
            private static FunctionWrapper<GdipIsVisiblePathPointI_delegate> GdipIsVisiblePathPointI_ptr;
            internal static Status GdipIsVisiblePathPointI(IntPtr path, int x, int y, IntPtr graphics, out bool result) => GdipIsVisiblePathPointI_ptr.Delegate(path, x, y, graphics, out result);

            private delegate Status GdipIsOutlineVisiblePathPoint_delegate(IntPtr path, float x, float y, IntPtr pen, IntPtr graphics, out bool result);
            private static FunctionWrapper<GdipIsOutlineVisiblePathPoint_delegate> GdipIsOutlineVisiblePathPoint_ptr;
            internal static Status GdipIsOutlineVisiblePathPoint(IntPtr path, float x, float y, IntPtr pen, IntPtr graphics, out bool result) => GdipIsOutlineVisiblePathPoint_ptr.Delegate(path, x, y, pen, graphics, out result);

            private delegate Status GdipIsOutlineVisiblePathPointI_delegate(IntPtr path, int x, int y, IntPtr pen, IntPtr graphics, out bool result);
            private static FunctionWrapper<GdipIsOutlineVisiblePathPointI_delegate> GdipIsOutlineVisiblePathPointI_ptr;
            internal static Status GdipIsOutlineVisiblePathPointI(IntPtr path, int x, int y, IntPtr pen, IntPtr graphics, out bool result) => GdipIsOutlineVisiblePathPointI_ptr.Delegate(path, x, y, pen, graphics, out result);

            private delegate Status GdipCreateFont_delegate(IntPtr fontFamily, float emSize, FontStyle style, GraphicsUnit unit, out IntPtr font);
            private static FunctionWrapper<GdipCreateFont_delegate> GdipCreateFont_ptr;
            internal static Status GdipCreateFont(IntPtr fontFamily, float emSize, FontStyle style, GraphicsUnit unit, out IntPtr font) => GdipCreateFont_ptr.Delegate(fontFamily, emSize, style, unit, out font);

            private delegate Status GdipDeleteFont_delegate(IntPtr font);
            private static FunctionWrapper<GdipDeleteFont_delegate> GdipDeleteFont_ptr;
            internal static Status GdipDeleteFont(IntPtr font) => GdipDeleteFont_ptr.Delegate(font);
            internal static int IntGdipDeleteFont(HandleRef font) => (int)GdipDeleteFont_ptr.Delegate(font.Handle);

#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
            private delegate Status GdipGetLogFont_delegate(IntPtr font, IntPtr graphics, [MarshalAs(UnmanagedType.AsAny), Out] object logfontA);
#pragma warning restore CS0618
            private static FunctionWrapper<GdipGetLogFont_delegate> GdipGetLogFont_ptr;
            internal static Status GdipGetLogFont(IntPtr font, IntPtr graphics, [Out] object logfontA) => GdipGetLogFont_ptr.Delegate(font, graphics, logfontA);

            private delegate Status GdipCreateFontFromDC_delegate(IntPtr hdc, out IntPtr font);
            private static FunctionWrapper<GdipCreateFontFromDC_delegate> GdipCreateFontFromDC_ptr;
            internal static Status GdipCreateFontFromDC(IntPtr hdc, out IntPtr font) => GdipCreateFontFromDC_ptr.Delegate(hdc, out font);

            private delegate Status GdipCreateFontFromLogfont_delegate(IntPtr hdc, ref LOGFONT lf, out IntPtr ptr);
            private static FunctionWrapper<GdipCreateFontFromLogfont_delegate> GdipCreateFontFromLogfont_ptr;
            internal static Status GdipCreateFontFromLogfont(IntPtr hdc, ref LOGFONT lf, out IntPtr ptr) => GdipCreateFontFromLogfont_ptr.Delegate(hdc, ref lf, out ptr);

            private delegate Status GdipCreateFontFromHfont_delegate(IntPtr hdc, out IntPtr font, ref LOGFONT lf);
            private static FunctionWrapper<GdipCreateFontFromHfont_delegate> GdipCreateFontFromHfont_ptr;
            internal static Status GdipCreateFontFromHfont(IntPtr hdc, out IntPtr font, ref LOGFONT lf) => GdipCreateFontFromHfont_ptr.Delegate(hdc, out font, ref lf);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
            private delegate Status GdipCreateFontFamilyFromName_delegate([MarshalAs(UnmanagedType.LPWStr)]string fName, IntPtr collection, out IntPtr fontFamily);
            private static FunctionWrapper<GdipCreateFontFamilyFromName_delegate> GdipCreateFontFamilyFromName_ptr;
            internal static Status GdipCreateFontFamilyFromName(string fName, IntPtr collection, out IntPtr fontFamily) => GdipCreateFontFamilyFromName_ptr.Delegate(fName, collection, out fontFamily);

            private delegate Status GdipGetFamilyName_delegate(IntPtr family, IntPtr name, int language);
            private static FunctionWrapper<GdipGetFamilyName_delegate> GdipGetFamilyName_ptr;
            internal static Status GdipGetFamilyName(IntPtr family, IntPtr name, int language) => GdipGetFamilyName_ptr.Delegate(family, name, language);
            internal static unsafe Status GdipGetFamilyName(IntPtr family, StringBuilder nameBuilder, int language)
            {
                const int LF_FACESIZE = 32;
                char* namePtr = stackalloc char[LF_FACESIZE];
                Status ret = GdipGetFamilyName(family, (IntPtr)namePtr, language);
                string name = Marshal.PtrToStringUni((IntPtr)namePtr);
                nameBuilder.Append(name);
                return ret;
            }


            private delegate Status GdipGetGenericFontFamilySansSerif_delegate(out IntPtr fontFamily);
            private static FunctionWrapper<GdipGetGenericFontFamilySansSerif_delegate> GdipGetGenericFontFamilySansSerif_ptr;
            internal static Status GdipGetGenericFontFamilySansSerif(out IntPtr fontFamily) => GdipGetGenericFontFamilySansSerif_ptr.Delegate(out fontFamily);

            private delegate Status GdipGetGenericFontFamilySerif_delegate(out IntPtr fontFamily);
            private static FunctionWrapper<GdipGetGenericFontFamilySerif_delegate> GdipGetGenericFontFamilySerif_ptr;
            internal static Status GdipGetGenericFontFamilySerif(out IntPtr fontFamily) => GdipGetGenericFontFamilySerif_ptr.Delegate(out fontFamily);

            private delegate Status GdipGetGenericFontFamilyMonospace_delegate(out IntPtr fontFamily);
            private static FunctionWrapper<GdipGetGenericFontFamilyMonospace_delegate> GdipGetGenericFontFamilyMonospace_ptr;
            internal static Status GdipGetGenericFontFamilyMonospace(out IntPtr fontFamily) => GdipGetGenericFontFamilyMonospace_ptr.Delegate(out fontFamily);

            private delegate Status GdipGetCellAscent_delegate(IntPtr fontFamily, int style, out short ascent);
            private static FunctionWrapper<GdipGetCellAscent_delegate> GdipGetCellAscent_ptr;
            internal static Status GdipGetCellAscent(IntPtr fontFamily, int style, out short ascent) => GdipGetCellAscent_ptr.Delegate(fontFamily, style, out ascent);

            private delegate Status GdipGetCellDescent_delegate(IntPtr fontFamily, int style, out short descent);
            private static FunctionWrapper<GdipGetCellDescent_delegate> GdipGetCellDescent_ptr;
            internal static Status GdipGetCellDescent(IntPtr fontFamily, int style, out short descent) => GdipGetCellDescent_ptr.Delegate(fontFamily, style, out descent);

            private delegate Status GdipGetLineSpacing_delegate(IntPtr fontFamily, int style, out short spacing);
            private static FunctionWrapper<GdipGetLineSpacing_delegate> GdipGetLineSpacing_ptr;
            internal static Status GdipGetLineSpacing(IntPtr fontFamily, int style, out short spacing) => GdipGetLineSpacing_ptr.Delegate(fontFamily, style, out spacing);

            private delegate Status GdipGetEmHeight_delegate(IntPtr fontFamily, int style, out short emHeight);
            private static FunctionWrapper<GdipGetEmHeight_delegate> GdipGetEmHeight_ptr;
            internal static Status GdipGetEmHeight(IntPtr fontFamily, int style, out short emHeight) => GdipGetEmHeight_ptr.Delegate(fontFamily, style, out emHeight);

            private delegate Status GdipIsStyleAvailable_delegate(IntPtr fontFamily, int style, out bool styleAvailable);
            private static FunctionWrapper<GdipIsStyleAvailable_delegate> GdipIsStyleAvailable_ptr;
            internal static Status GdipIsStyleAvailable(IntPtr fontFamily, int style, out bool styleAvailable) => GdipIsStyleAvailable_ptr.Delegate(fontFamily, style, out styleAvailable);

            private delegate Status GdipDeleteFontFamily_delegate(IntPtr fontFamily);
            private static FunctionWrapper<GdipDeleteFontFamily_delegate> GdipDeleteFontFamily_ptr;
            internal static Status GdipDeleteFontFamily(IntPtr fontFamily) => GdipDeleteFontFamily_ptr.Delegate(fontFamily);
            internal static int IntGdipDeleteFontFamily(HandleRef fontFamily) => (int)GdipDeleteFontFamily_ptr.Delegate(fontFamily.Handle);

            private delegate Status GdipGetFontSize_delegate(IntPtr font, out float size);
            private static FunctionWrapper<GdipGetFontSize_delegate> GdipGetFontSize_ptr;
            internal static Status GdipGetFontSize(IntPtr font, out float size) => GdipGetFontSize_ptr.Delegate(font, out size);

            private delegate Status GdipGetFontHeight_delegate(IntPtr font, IntPtr graphics, out float height);
            private static FunctionWrapper<GdipGetFontHeight_delegate> GdipGetFontHeight_ptr;
            internal static Status GdipGetFontHeight(IntPtr font, IntPtr graphics, out float height) => GdipGetFontHeight_ptr.Delegate(font, graphics, out height);

            private delegate Status GdipGetFontHeightGivenDPI_delegate(IntPtr font, float dpi, out float height);
            private static FunctionWrapper<GdipGetFontHeightGivenDPI_delegate> GdipGetFontHeightGivenDPI_ptr;
            internal static Status GdipGetFontHeightGivenDPI(IntPtr font, float dpi, out float height) => GdipGetFontHeightGivenDPI_ptr.Delegate(font, dpi, out height);

            private delegate Status GdipCreateMetafileFromFile_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromFile_delegate> GdipCreateMetafileFromFile_ptr;
            internal static Status GdipCreateMetafileFromFile(string filename, out IntPtr metafile) => GdipCreateMetafileFromFile_ptr.Delegate(filename, out metafile);

            private delegate Status GdipCreateMetafileFromEmf_delegate(IntPtr hEmf, bool deleteEmf, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromEmf_delegate> GdipCreateMetafileFromEmf_ptr;
            internal static Status GdipCreateMetafileFromEmf(IntPtr hEmf, bool deleteEmf, out IntPtr metafile) => GdipCreateMetafileFromEmf_ptr.Delegate(hEmf, deleteEmf, out metafile);

            private delegate Status GdipCreateMetafileFromWmf_delegate(IntPtr hWmf, bool deleteWmf, WmfPlaceableFileHeader wmfPlaceableFileHeader, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromWmf_delegate> GdipCreateMetafileFromWmf_ptr;
            internal static Status GdipCreateMetafileFromWmf(IntPtr hWmf, bool deleteWmf, WmfPlaceableFileHeader wmfPlaceableFileHeader, out IntPtr metafile) => GdipCreateMetafileFromWmf_ptr.Delegate(hWmf, deleteWmf, wmfPlaceableFileHeader, out metafile);

            private delegate Status GdipGetMetafileHeaderFromFile_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromFile_delegate> GdipGetMetafileHeaderFromFile_ptr;
            internal static Status GdipGetMetafileHeaderFromFile(string filename, IntPtr header) => GdipGetMetafileHeaderFromFile_ptr.Delegate(filename, header);

            private delegate Status GdipGetMetafileHeaderFromMetafile_delegate(IntPtr metafile, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromMetafile_delegate> GdipGetMetafileHeaderFromMetafile_ptr;
            internal static Status GdipGetMetafileHeaderFromMetafile(IntPtr metafile, IntPtr header) => GdipGetMetafileHeaderFromMetafile_ptr.Delegate(metafile, header);

            private delegate Status GdipGetMetafileHeaderFromEmf_delegate(IntPtr hEmf, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromEmf_delegate> GdipGetMetafileHeaderFromEmf_ptr;
            internal static Status GdipGetMetafileHeaderFromEmf(IntPtr hEmf, IntPtr header) => GdipGetMetafileHeaderFromEmf_ptr.Delegate(hEmf, header);

            private delegate Status GdipGetMetafileHeaderFromWmf_delegate(IntPtr hWmf, IntPtr wmfPlaceableFileHeader, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromWmf_delegate> GdipGetMetafileHeaderFromWmf_ptr;
            internal static Status GdipGetMetafileHeaderFromWmf(IntPtr hWmf, IntPtr wmfPlaceableFileHeader, IntPtr header) => GdipGetMetafileHeaderFromWmf_ptr.Delegate(hWmf, wmfPlaceableFileHeader, header);

            private delegate Status GdipGetHemfFromMetafile_delegate(IntPtr metafile, out IntPtr hEmf);
            private static FunctionWrapper<GdipGetHemfFromMetafile_delegate> GdipGetHemfFromMetafile_ptr;
            internal static Status GdipGetHemfFromMetafile(IntPtr metafile, out IntPtr hEmf) => GdipGetHemfFromMetafile_ptr.Delegate(metafile, out hEmf);

            private delegate Status GdipGetMetafileDownLevelRasterizationLimit_delegate(IntPtr metafile, ref uint metafileRasterizationLimitDpi);
            private static FunctionWrapper<GdipGetMetafileDownLevelRasterizationLimit_delegate> GdipGetMetafileDownLevelRasterizationLimit_ptr;
            internal static Status GdipGetMetafileDownLevelRasterizationLimit(IntPtr metafile, ref uint metafileRasterizationLimitDpi) => GdipGetMetafileDownLevelRasterizationLimit_ptr.Delegate(metafile, ref metafileRasterizationLimitDpi);

            private delegate Status GdipSetMetafileDownLevelRasterizationLimit_delegate(IntPtr metafile, uint metafileRasterizationLimitDpi);
            private static FunctionWrapper<GdipSetMetafileDownLevelRasterizationLimit_delegate> GdipSetMetafileDownLevelRasterizationLimit_ptr;
            internal static Status GdipSetMetafileDownLevelRasterizationLimit(IntPtr metafile, uint metafileRasterizationLimitDpi) => GdipSetMetafileDownLevelRasterizationLimit_ptr.Delegate(metafile, metafileRasterizationLimitDpi);

            private delegate Status GdipPlayMetafileRecord_delegate(IntPtr metafile, EmfPlusRecordType recordType, int flags, int dataSize, byte[] data);
            private static FunctionWrapper<GdipPlayMetafileRecord_delegate> GdipPlayMetafileRecord_ptr;
            internal static Status GdipPlayMetafileRecord(IntPtr metafile, EmfPlusRecordType recordType, int flags, int dataSize, byte[] data) => GdipPlayMetafileRecord_ptr.Delegate(metafile, recordType, flags, dataSize, data);

            private delegate Status GdipRecordMetafile_delegate(IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafile_delegate> GdipRecordMetafile_ptr;
            internal static Status GdipRecordMetafile(IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafile_ptr.Delegate(hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate Status GdipRecordMetafileI_delegate(IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileI_delegate> GdipRecordMetafileI_ptr;
            internal static Status GdipRecordMetafileI(IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileI_ptr.Delegate(hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate Status GdipRecordMetafileFileName_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileFileName_delegate> GdipRecordMetafileFileName_ptr;
            internal static Status GdipRecordMetafileFileName(string filename, IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileFileName_ptr.Delegate(filename, hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate Status GdipRecordMetafileFileNameI_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileFileNameI_delegate> GdipRecordMetafileFileNameI_ptr;
            internal static Status GdipRecordMetafileFileNameI(string filename, IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileFileNameI_ptr.Delegate(filename, hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate Status GdipCreateMetafileFromStream_delegate([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))]IStream stream, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromStream_delegate> GdipCreateMetafileFromStream_ptr;
            internal static Status GdipCreateMetafileFromStream(IStream stream, out IntPtr metafile) => GdipCreateMetafileFromStream_ptr.Delegate(stream, out metafile);

            private delegate Status GdipGetMetafileHeaderFromStream_delegate([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))]IStream stream, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromStream_delegate> GdipGetMetafileHeaderFromStream_ptr;
            internal static Status GdipGetMetafileHeaderFromStream(IStream stream, IntPtr header) => GdipGetMetafileHeaderFromStream_ptr.Delegate(stream, header);

            private delegate Status GdipRecordMetafileStream_delegate([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))]IStream stream, IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileStream_delegate> GdipRecordMetafileStream_ptr;
            internal static Status GdipRecordMetafileStream(IStream stream, IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileStream_ptr.Delegate(stream, hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate Status GdipRecordMetafileStreamI_delegate([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))]IStream stream, IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileStreamI_delegate> GdipRecordMetafileStreamI_ptr;
            internal static Status GdipRecordMetafileStreamI(IStream stream, IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileStreamI_ptr.Delegate(stream, hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate Status GdipCreateFromContext_macosx_delegate(IntPtr cgref, int width, int height, out IntPtr graphics);
            private static FunctionWrapper<GdipCreateFromContext_macosx_delegate> GdipCreateFromContext_macosx_ptr;
            internal static Status GdipCreateFromContext_macosx(IntPtr cgref, int width, int height, out IntPtr graphics) =>GdipCreateFromContext_macosx_ptr.Delegate(cgref, width, height, out graphics);

            private delegate Status GdipSetVisibleClip_linux_delegate(IntPtr graphics, ref Rectangle rect);
            private static FunctionWrapper<GdipSetVisibleClip_linux_delegate> GdipSetVisibleClip_linux_ptr;
            internal static Status GdipSetVisibleClip_linux(IntPtr graphics, ref Rectangle rect) => GdipSetVisibleClip_linux_ptr.Delegate(graphics, ref rect);

            private delegate Status GdipCreateFromXDrawable_linux_delegate(IntPtr drawable, IntPtr display, out IntPtr graphics);
            private static FunctionWrapper<GdipCreateFromXDrawable_linux_delegate> GdipCreateFromXDrawable_linux_ptr;
            internal static Status GdipCreateFromXDrawable_linux(IntPtr drawable, IntPtr display, out IntPtr graphics) => GdipCreateFromXDrawable_linux_ptr.Delegate(drawable, display, out graphics);

            // Stream functions for non-Win32 (libgdiplus specific)
            private delegate Status GdipLoadImageFromDelegate_linux_delegate(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr image);
            private static FunctionWrapper<GdipLoadImageFromDelegate_linux_delegate> GdipLoadImageFromDelegate_linux_ptr;
            internal static Status GdipLoadImageFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr image)
                => GdipLoadImageFromDelegate_linux_ptr.Delegate(getHeader, getBytes, putBytes, doSeek, close, size, out image);

            private delegate Status GdipSaveImageToDelegate_linux_delegate(IntPtr image, StreamGetBytesDelegate getBytes,
                StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek, StreamCloseDelegate close,
                StreamSizeDelegate size, ref Guid encoderClsID, IntPtr encoderParameters);
            private static FunctionWrapper<GdipSaveImageToDelegate_linux_delegate> GdipSaveImageToDelegate_linux_ptr;
            internal static Status GdipSaveImageToDelegate_linux(IntPtr image, StreamGetBytesDelegate getBytes,
                StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek, StreamCloseDelegate close,
                StreamSizeDelegate size, ref Guid encoderClsID, IntPtr encoderParameters)
                => GdipSaveImageToDelegate_linux_ptr.Delegate(image, getBytes, putBytes, doSeek, close, size, ref encoderClsID, encoderParameters);

            private delegate Status GdipCreateMetafileFromDelegate_linux_delegate(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromDelegate_linux_delegate> GdipCreateMetafileFromDelegate_linux_ptr;
            internal static Status GdipCreateMetafileFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr metafile)
                => GdipCreateMetafileFromDelegate_linux_ptr.Delegate(getHeader, getBytes, putBytes, doSeek, close, size, out metafile);

            private delegate Status GdipGetMetafileHeaderFromDelegate_linux_delegate(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromDelegate_linux_delegate> GdipGetMetafileHeaderFromDelegate_linux_ptr;
            internal static Status GdipGetMetafileHeaderFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr header)
                => GdipGetMetafileHeaderFromDelegate_linux_ptr.Delegate(getHeader, getBytes, putBytes, doSeek, close, size, header);

            private delegate Status GdipRecordMetafileFromDelegate_linux_delegate(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref RectangleF frameRect,
                MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)] string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileFromDelegate_linux_delegate> GdipRecordMetafileFromDelegate_linux_ptr;
            internal static Status GdipRecordMetafileFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref RectangleF frameRect,
                MetafileFrameUnit frameUnit, string description, out IntPtr metafile)
                => GdipRecordMetafileFromDelegate_linux_ptr.Delegate(getHeader, getBytes, putBytes, doSeek, close, size, hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate Status GdipRecordMetafileFromDelegateI_linux_delegate(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref Rectangle frameRect,
                MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)] string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileFromDelegateI_linux_delegate> GdipRecordMetafileFromDelegateI_linux_ptr;
            internal static Status GdipRecordMetafileFromDelegateI_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref Rectangle frameRect,
                MetafileFrameUnit frameUnit, string description, out IntPtr metafile)
                => GdipRecordMetafileFromDelegateI_linux_ptr.Delegate(getHeader, getBytes, putBytes, doSeek, close, size, hdc, type, ref frameRect, frameUnit, description, out metafile);
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
