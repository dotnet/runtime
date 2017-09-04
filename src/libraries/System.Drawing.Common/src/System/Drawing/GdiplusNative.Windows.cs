// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Drawing
{
    internal partial class SafeNativeMethods
    {
        internal partial class Gdip
        {
            private static SafeLibraryHandle s_gdipHandle;

            private static IntPtr LoadNativeLibrary()
            {
                s_gdipHandle = Interop.Kernel32.LoadLibraryExW("gdiplus.dll", IntPtr.Zero, 0);
                return s_gdipHandle.DangerousGetHandle();
            }

            private static IntPtr LoadFunctionPointer(IntPtr nativeLibraryHandle, string functionName) => Interop.Kernel32.GetProcAddress(nativeLibraryHandle, functionName);

            private static void LoadPlatformFunctionPointers()
            {
                GdiplusStartup_ptr = LoadFunction<GdiplusStartup_delegate>("GdiplusStartup");
                GdipCreatePath_ptr = LoadFunction<GdipCreatePath_delegate>("GdipCreatePath");
                GdipCreatePath2_ptr = LoadFunction<GdipCreatePath2_delegate>("GdipCreatePath2");
                GdipCreatePath2I_ptr = LoadFunction<GdipCreatePath2I_delegate>("GdipCreatePath2I");
                GdipClonePath_ptr = LoadFunction<GdipClonePath_delegate>("GdipClonePath");
                GdipDeletePath_ptr = LoadFunction<GdipDeletePath_delegate>("GdipDeletePath");
                GdipResetPath_ptr = LoadFunction<GdipResetPath_delegate>("GdipResetPath");
                GdipGetPointCount_ptr = LoadFunction<GdipGetPointCount_delegate>("GdipGetPointCount");
                GdipGetPathTypes_ptr = LoadFunction<GdipGetPathTypes_delegate>("GdipGetPathTypes");
                GdipGetPathPoints_ptr = LoadFunction<GdipGetPathPoints_delegate>("GdipGetPathPoints");
                GdipGetPathFillMode_ptr = LoadFunction<GdipGetPathFillMode_delegate>("GdipGetPathFillMode");
                GdipSetPathFillMode_ptr = LoadFunction<GdipSetPathFillMode_delegate>("GdipSetPathFillMode");
                GdipGetPathData_ptr = LoadFunction<GdipGetPathData_delegate>("GdipGetPathData");
                GdipStartPathFigure_ptr = LoadFunction<GdipStartPathFigure_delegate>("GdipStartPathFigure");
                GdipClosePathFigure_ptr = LoadFunction<GdipClosePathFigure_delegate>("GdipClosePathFigure");
                GdipClosePathFigures_ptr = LoadFunction<GdipClosePathFigures_delegate>("GdipClosePathFigures");
                GdipSetPathMarker_ptr = LoadFunction<GdipSetPathMarker_delegate>("GdipSetPathMarker");
                GdipClearPathMarkers_ptr = LoadFunction<GdipClearPathMarkers_delegate>("GdipClearPathMarkers");
                GdipReversePath_ptr = LoadFunction<GdipReversePath_delegate>("GdipReversePath");
                GdipGetPathLastPoint_ptr = LoadFunction<GdipGetPathLastPoint_delegate>("GdipGetPathLastPoint");
                GdipAddPathLine_ptr = LoadFunction<GdipAddPathLine_delegate>("GdipAddPathLine");
                GdipAddPathLine2_ptr = LoadFunction<GdipAddPathLine2_delegate>("GdipAddPathLine2");
                GdipAddPathArc_ptr = LoadFunction<GdipAddPathArc_delegate>("GdipAddPathArc");
                GdipAddPathBezier_ptr = LoadFunction<GdipAddPathBezier_delegate>("GdipAddPathBezier");
                GdipAddPathBeziers_ptr = LoadFunction<GdipAddPathBeziers_delegate>("GdipAddPathBeziers");
                GdipAddPathCurve_ptr = LoadFunction<GdipAddPathCurve_delegate>("GdipAddPathCurve");
                GdipAddPathCurve2_ptr = LoadFunction<GdipAddPathCurve2_delegate>("GdipAddPathCurve2");
                GdipAddPathCurve3_ptr = LoadFunction<GdipAddPathCurve3_delegate>("GdipAddPathCurve3");
                GdipAddPathClosedCurve_ptr = LoadFunction<GdipAddPathClosedCurve_delegate>("GdipAddPathClosedCurve");
                GdipAddPathClosedCurve2_ptr = LoadFunction<GdipAddPathClosedCurve2_delegate>("GdipAddPathClosedCurve2");
                GdipAddPathRectangle_ptr = LoadFunction<GdipAddPathRectangle_delegate>("GdipAddPathRectangle");
                GdipAddPathRectangles_ptr = LoadFunction<GdipAddPathRectangles_delegate>("GdipAddPathRectangles");
                GdipAddPathEllipse_ptr = LoadFunction<GdipAddPathEllipse_delegate>("GdipAddPathEllipse");
                GdipAddPathPie_ptr = LoadFunction<GdipAddPathPie_delegate>("GdipAddPathPie");
                GdipAddPathPolygon_ptr = LoadFunction<GdipAddPathPolygon_delegate>("GdipAddPathPolygon");
                GdipAddPathPath_ptr = LoadFunction<GdipAddPathPath_delegate>("GdipAddPathPath");
                GdipAddPathString_ptr = LoadFunction<GdipAddPathString_delegate>("GdipAddPathString");
                GdipAddPathStringI_ptr = LoadFunction<GdipAddPathStringI_delegate>("GdipAddPathStringI");
                GdipAddPathLineI_ptr = LoadFunction<GdipAddPathLineI_delegate>("GdipAddPathLineI");
                GdipAddPathLine2I_ptr = LoadFunction<GdipAddPathLine2I_delegate>("GdipAddPathLine2I");
                GdipAddPathArcI_ptr = LoadFunction<GdipAddPathArcI_delegate>("GdipAddPathArcI");
                GdipAddPathBezierI_ptr = LoadFunction<GdipAddPathBezierI_delegate>("GdipAddPathBezierI");
                GdipAddPathBeziersI_ptr = LoadFunction<GdipAddPathBeziersI_delegate>("GdipAddPathBeziersI");
                GdipAddPathCurveI_ptr = LoadFunction<GdipAddPathCurveI_delegate>("GdipAddPathCurveI");
                GdipAddPathCurve2I_ptr = LoadFunction<GdipAddPathCurve2I_delegate>("GdipAddPathCurve2I");
                GdipAddPathCurve3I_ptr = LoadFunction<GdipAddPathCurve3I_delegate>("GdipAddPathCurve3I");
                GdipAddPathClosedCurveI_ptr = LoadFunction<GdipAddPathClosedCurveI_delegate>("GdipAddPathClosedCurveI");
                GdipAddPathClosedCurve2I_ptr = LoadFunction<GdipAddPathClosedCurve2I_delegate>("GdipAddPathClosedCurve2I");
                GdipAddPathRectangleI_ptr = LoadFunction<GdipAddPathRectangleI_delegate>("GdipAddPathRectangleI");
                GdipAddPathRectanglesI_ptr = LoadFunction<GdipAddPathRectanglesI_delegate>("GdipAddPathRectanglesI");
                GdipAddPathEllipseI_ptr = LoadFunction<GdipAddPathEllipseI_delegate>("GdipAddPathEllipseI");
                GdipAddPathPieI_ptr = LoadFunction<GdipAddPathPieI_delegate>("GdipAddPathPieI");
                GdipAddPathPolygonI_ptr = LoadFunction<GdipAddPathPolygonI_delegate>("GdipAddPathPolygonI");
                GdipFlattenPath_ptr = LoadFunction<GdipFlattenPath_delegate>("GdipFlattenPath");
                GdipWidenPath_ptr = LoadFunction<GdipWidenPath_delegate>("GdipWidenPath");
                GdipWarpPath_ptr = LoadFunction<GdipWarpPath_delegate>("GdipWarpPath");
                GdipTransformPath_ptr = LoadFunction<GdipTransformPath_delegate>("GdipTransformPath");
                GdipGetPathWorldBounds_ptr = LoadFunction<GdipGetPathWorldBounds_delegate>("GdipGetPathWorldBounds");
                GdipIsVisiblePathPoint_ptr = LoadFunction<GdipIsVisiblePathPoint_delegate>("GdipIsVisiblePathPoint");
                GdipIsVisiblePathPointI_ptr = LoadFunction<GdipIsVisiblePathPointI_delegate>("GdipIsVisiblePathPointI");
                GdipIsOutlineVisiblePathPoint_ptr = LoadFunction<GdipIsOutlineVisiblePathPoint_delegate>("GdipIsOutlineVisiblePathPoint");
                GdipIsOutlineVisiblePathPointI_ptr = LoadFunction<GdipIsOutlineVisiblePathPointI_delegate>("GdipIsOutlineVisiblePathPointI");
                GdipDeleteBrush_ptr = LoadFunction<GdipDeleteBrush_delegate>("GdipDeleteBrush");
                GdipLoadImageFromStream_ptr = LoadFunction<GdipLoadImageFromStream_delegate>("GdipLoadImageFromStream");
                GdipLoadImageFromFile_ptr = LoadFunction<GdipLoadImageFromFile_delegate>("GdipLoadImageFromFile");
                GdipLoadImageFromStreamICM_ptr = LoadFunction<GdipLoadImageFromStreamICM_delegate>("GdipLoadImageFromStreamICM");
                GdipLoadImageFromFileICM_ptr = LoadFunction<GdipLoadImageFromFileICM_delegate>("GdipLoadImageFromFileICM");
                GdipCloneImage_ptr = LoadFunction<GdipCloneImage_delegate>("GdipCloneImage");
                GdipDisposeImage_ptr = LoadFunction<GdipDisposeImage_delegate>("GdipDisposeImage");
                GdipSaveImageToFile_ptr = LoadFunction<GdipSaveImageToFile_delegate>("GdipSaveImageToFile");
                GdipSaveImageToStream_ptr = LoadFunction<GdipSaveImageToStream_delegate>("GdipSaveImageToStream");
                GdipSaveAdd_ptr = LoadFunction<GdipSaveAdd_delegate>("GdipSaveAdd");
                GdipSaveAddImage_ptr = LoadFunction<GdipSaveAddImage_delegate>("GdipSaveAddImage");
                GdipGetImageGraphicsContext_ptr = LoadFunction<GdipGetImageGraphicsContext_delegate>("GdipGetImageGraphicsContext");
                GdipGetImageBounds_ptr = LoadFunction<GdipGetImageBounds_delegate>("GdipGetImageBounds");
                GdipGetImageDimension_ptr = LoadFunction<GdipGetImageDimension_delegate>("GdipGetImageDimension");
                GdipGetImageType_ptr = LoadFunction<GdipGetImageType_delegate>("GdipGetImageType");
                GdipGetImageWidth_ptr = LoadFunction<GdipGetImageWidth_delegate>("GdipGetImageWidth");
                GdipGetImageHeight_ptr = LoadFunction<GdipGetImageHeight_delegate>("GdipGetImageHeight");
                GdipGetImageHorizontalResolution_ptr = LoadFunction<GdipGetImageHorizontalResolution_delegate>("GdipGetImageHorizontalResolution");
                GdipGetImageVerticalResolution_ptr = LoadFunction<GdipGetImageVerticalResolution_delegate>("GdipGetImageVerticalResolution");
                GdipGetImageFlags_ptr = LoadFunction<GdipGetImageFlags_delegate>("GdipGetImageFlags");
                GdipGetImageRawFormat_ptr = LoadFunction<GdipGetImageRawFormat_delegate>("GdipGetImageRawFormat");
                GdipGetImagePixelFormat_ptr = LoadFunction<GdipGetImagePixelFormat_delegate>("GdipGetImagePixelFormat");
                GdipGetImageThumbnail_ptr = LoadFunction<GdipGetImageThumbnail_delegate>("GdipGetImageThumbnail");
                GdipGetEncoderParameterListSize_ptr = LoadFunction<GdipGetEncoderParameterListSize_delegate>("GdipGetEncoderParameterListSize");
                GdipGetEncoderParameterList_ptr = LoadFunction<GdipGetEncoderParameterList_delegate>("GdipGetEncoderParameterList");
                GdipImageGetFrameDimensionsCount_ptr = LoadFunction<GdipImageGetFrameDimensionsCount_delegate>("GdipImageGetFrameDimensionsCount");
                GdipImageGetFrameDimensionsList_ptr = LoadFunction<GdipImageGetFrameDimensionsList_delegate>("GdipImageGetFrameDimensionsList");
                GdipImageGetFrameCount_ptr = LoadFunction<GdipImageGetFrameCount_delegate>("GdipImageGetFrameCount");
                GdipImageSelectActiveFrame_ptr = LoadFunction<GdipImageSelectActiveFrame_delegate>("GdipImageSelectActiveFrame");
                GdipImageRotateFlip_ptr = LoadFunction<GdipImageRotateFlip_delegate>("GdipImageRotateFlip");
                GdipGetImagePalette_ptr = LoadFunction<GdipGetImagePalette_delegate>("GdipGetImagePalette");
                GdipSetImagePalette_ptr = LoadFunction<GdipSetImagePalette_delegate>("GdipSetImagePalette");
                GdipGetImagePaletteSize_ptr = LoadFunction<GdipGetImagePaletteSize_delegate>("GdipGetImagePaletteSize");
                GdipGetPropertyCount_ptr = LoadFunction<GdipGetPropertyCount_delegate>("GdipGetPropertyCount");
                GdipGetPropertyIdList_ptr = LoadFunction<GdipGetPropertyIdList_delegate>("GdipGetPropertyIdList");
                GdipGetPropertyItemSize_ptr = LoadFunction<GdipGetPropertyItemSize_delegate>("GdipGetPropertyItemSize");
                GdipGetPropertyItem_ptr = LoadFunction<GdipGetPropertyItem_delegate>("GdipGetPropertyItem");
                GdipGetPropertySize_ptr = LoadFunction<GdipGetPropertySize_delegate>("GdipGetPropertySize");
                GdipGetAllPropertyItems_ptr = LoadFunction<GdipGetAllPropertyItems_delegate>("GdipGetAllPropertyItems");
                GdipRemovePropertyItem_ptr = LoadFunction<GdipRemovePropertyItem_delegate>("GdipRemovePropertyItem");
                GdipSetPropertyItem_ptr = LoadFunction<GdipSetPropertyItem_delegate>("GdipSetPropertyItem");
                GdipImageForceValidation_ptr = LoadFunction<GdipImageForceValidation_delegate>("GdipImageForceValidation");
                GdipCreateBitmapFromStream_ptr = LoadFunction<GdipCreateBitmapFromStream_delegate>("GdipCreateBitmapFromStream");
                GdipCreateBitmapFromFile_ptr = LoadFunction<GdipCreateBitmapFromFile_delegate>("GdipCreateBitmapFromFile");
                GdipCreateBitmapFromStreamICM_ptr = LoadFunction<GdipCreateBitmapFromStreamICM_delegate>("GdipCreateBitmapFromStreamICM");
                GdipCreateBitmapFromFileICM_ptr = LoadFunction<GdipCreateBitmapFromFileICM_delegate>("GdipCreateBitmapFromFileICM");
                GdipCreateBitmapFromScan0_ptr = LoadFunction<GdipCreateBitmapFromScan0_delegate>("GdipCreateBitmapFromScan0");
                GdipCreateBitmapFromGraphics_ptr = LoadFunction<GdipCreateBitmapFromGraphics_delegate>("GdipCreateBitmapFromGraphics");
                GdipCreateBitmapFromHBITMAP_ptr = LoadFunction<GdipCreateBitmapFromHBITMAP_delegate>("GdipCreateBitmapFromHBITMAP");
                GdipCreateBitmapFromHICON_ptr = LoadFunction<GdipCreateBitmapFromHICON_delegate>("GdipCreateBitmapFromHICON");
                GdipCreateBitmapFromResource_ptr = LoadFunction<GdipCreateBitmapFromResource_delegate>("GdipCreateBitmapFromResource");
                GdipCreateHBITMAPFromBitmap_ptr = LoadFunction<GdipCreateHBITMAPFromBitmap_delegate>("GdipCreateHBITMAPFromBitmap");
                GdipCreateHICONFromBitmap_ptr = LoadFunction<GdipCreateHICONFromBitmap_delegate>("GdipCreateHICONFromBitmap");
                GdipCloneBitmapArea_ptr = LoadFunction<GdipCloneBitmapArea_delegate>("GdipCloneBitmapArea");
                GdipCloneBitmapAreaI_ptr = LoadFunction<GdipCloneBitmapAreaI_delegate>("GdipCloneBitmapAreaI");
                GdipBitmapLockBits_ptr = LoadFunction<GdipBitmapLockBits_delegate>("GdipBitmapLockBits");
                GdipBitmapUnlockBits_ptr = LoadFunction<GdipBitmapUnlockBits_delegate>("GdipBitmapUnlockBits");
                GdipBitmapGetPixel_ptr = LoadFunction<GdipBitmapGetPixel_delegate>("GdipBitmapGetPixel");
                GdipBitmapSetPixel_ptr = LoadFunction<GdipBitmapSetPixel_delegate>("GdipBitmapSetPixel");
                GdipBitmapSetResolution_ptr = LoadFunction<GdipBitmapSetResolution_delegate>("GdipBitmapSetResolution");
                GdipFlush_ptr = LoadFunction<GdipFlush_delegate>("GdipFlush");
                GdipCreateFromHDC_ptr = LoadFunction<GdipCreateFromHDC_delegate>("GdipCreateFromHDC");
                GdipCreateFromHDC2_ptr = LoadFunction<GdipCreateFromHDC2_delegate>("GdipCreateFromHDC2");
                GdipCreateFromHWND_ptr = LoadFunction<GdipCreateFromHWND_delegate>("GdipCreateFromHWND");
                GdipDeleteGraphics_ptr = LoadFunction<GdipDeleteGraphics_delegate>("GdipDeleteGraphics");
                GdipGetDC_ptr = LoadFunction<GdipGetDC_delegate>("GdipGetDC");
                GdipReleaseDC_ptr = LoadFunction<GdipReleaseDC_delegate>("GdipReleaseDC");
                GdipSetCompositingMode_ptr = LoadFunction<GdipSetCompositingMode_delegate>("GdipSetCompositingMode");
                GdipSetTextRenderingHint_ptr = LoadFunction<GdipSetTextRenderingHint_delegate>("GdipSetTextRenderingHint");
                GdipSetTextContrast_ptr = LoadFunction<GdipSetTextContrast_delegate>("GdipSetTextContrast");
                GdipSetInterpolationMode_ptr = LoadFunction<GdipSetInterpolationMode_delegate>("GdipSetInterpolationMode");
                GdipGetCompositingMode_ptr = LoadFunction<GdipGetCompositingMode_delegate>("GdipGetCompositingMode");
                GdipSetRenderingOrigin_ptr = LoadFunction<GdipSetRenderingOrigin_delegate>("GdipSetRenderingOrigin");
                GdipGetRenderingOrigin_ptr = LoadFunction<GdipGetRenderingOrigin_delegate>("GdipGetRenderingOrigin");
                GdipSetCompositingQuality_ptr = LoadFunction<GdipSetCompositingQuality_delegate>("GdipSetCompositingQuality");
                GdipGetCompositingQuality_ptr = LoadFunction<GdipGetCompositingQuality_delegate>("GdipGetCompositingQuality");
                GdipSetSmoothingMode_ptr = LoadFunction<GdipSetSmoothingMode_delegate>("GdipSetSmoothingMode");
                GdipGetSmoothingMode_ptr = LoadFunction<GdipGetSmoothingMode_delegate>("GdipGetSmoothingMode");
                GdipSetPixelOffsetMode_ptr = LoadFunction<GdipSetPixelOffsetMode_delegate>("GdipSetPixelOffsetMode");
                GdipGetPixelOffsetMode_ptr = LoadFunction<GdipGetPixelOffsetMode_delegate>("GdipGetPixelOffsetMode");
                GdipGetTextRenderingHint_ptr = LoadFunction<GdipGetTextRenderingHint_delegate>("GdipGetTextRenderingHint");
                GdipGetTextContrast_ptr = LoadFunction<GdipGetTextContrast_delegate>("GdipGetTextContrast");
                GdipGetInterpolationMode_ptr = LoadFunction<GdipGetInterpolationMode_delegate>("GdipGetInterpolationMode");
                GdipGetPageUnit_ptr = LoadFunction<GdipGetPageUnit_delegate>("GdipGetPageUnit");
                GdipGetPageScale_ptr = LoadFunction<GdipGetPageScale_delegate>("GdipGetPageScale");
                GdipSetPageUnit_ptr = LoadFunction<GdipSetPageUnit_delegate>("GdipSetPageUnit");
                GdipSetPageScale_ptr = LoadFunction<GdipSetPageScale_delegate>("GdipSetPageScale");
                GdipGetDpiX_ptr = LoadFunction<GdipGetDpiX_delegate>("GdipGetDpiX");
                GdipGetDpiY_ptr = LoadFunction<GdipGetDpiY_delegate>("GdipGetDpiY");
                GdipTransformPoints_ptr = LoadFunction<GdipTransformPoints_delegate>("GdipTransformPoints");
                GdipTransformPointsI_ptr = LoadFunction<GdipTransformPointsI_delegate>("GdipTransformPointsI");
                GdipGetNearestColor_ptr = LoadFunction<GdipGetNearestColor_delegate>("GdipGetNearestColor");
                GdipCreateHalftonePalette_ptr = LoadFunction<GdipCreateHalftonePalette_delegate>("GdipCreateHalftonePalette");
                GdipDrawLine_ptr = LoadFunction<GdipDrawLine_delegate>("GdipDrawLine");
                GdipDrawLineI_ptr = LoadFunction<GdipDrawLineI_delegate>("GdipDrawLineI");
                GdipDrawLines_ptr = LoadFunction<GdipDrawLines_delegate>("GdipDrawLines");
                GdipDrawLinesI_ptr = LoadFunction<GdipDrawLinesI_delegate>("GdipDrawLinesI");
                GdipDrawArc_ptr = LoadFunction<GdipDrawArc_delegate>("GdipDrawArc");
                GdipDrawArcI_ptr = LoadFunction<GdipDrawArcI_delegate>("GdipDrawArcI");
                GdipDrawBezier_ptr = LoadFunction<GdipDrawBezier_delegate>("GdipDrawBezier");
                GdipDrawBeziers_ptr = LoadFunction<GdipDrawBeziers_delegate>("GdipDrawBeziers");
                GdipDrawBeziersI_ptr = LoadFunction<GdipDrawBeziersI_delegate>("GdipDrawBeziersI");
                GdipDrawRectangle_ptr = LoadFunction<GdipDrawRectangle_delegate>("GdipDrawRectangle");
                GdipDrawRectangleI_ptr = LoadFunction<GdipDrawRectangleI_delegate>("GdipDrawRectangleI");
                GdipDrawRectangles_ptr = LoadFunction<GdipDrawRectangles_delegate>("GdipDrawRectangles");
                GdipDrawRectanglesI_ptr = LoadFunction<GdipDrawRectanglesI_delegate>("GdipDrawRectanglesI");
                GdipDrawEllipse_ptr = LoadFunction<GdipDrawEllipse_delegate>("GdipDrawEllipse");
                GdipDrawEllipseI_ptr = LoadFunction<GdipDrawEllipseI_delegate>("GdipDrawEllipseI");
                GdipDrawPie_ptr = LoadFunction<GdipDrawPie_delegate>("GdipDrawPie");
                GdipDrawPieI_ptr = LoadFunction<GdipDrawPieI_delegate>("GdipDrawPieI");
                GdipDrawPolygon_ptr = LoadFunction<GdipDrawPolygon_delegate>("GdipDrawPolygon");
                GdipDrawPolygonI_ptr = LoadFunction<GdipDrawPolygonI_delegate>("GdipDrawPolygonI");
                GdipDrawPath_ptr = LoadFunction<GdipDrawPath_delegate>("GdipDrawPath");
                GdipDrawCurve_ptr = LoadFunction<GdipDrawCurve_delegate>("GdipDrawCurve");
                GdipDrawCurveI_ptr = LoadFunction<GdipDrawCurveI_delegate>("GdipDrawCurveI");
                GdipDrawCurve2_ptr = LoadFunction<GdipDrawCurve2_delegate>("GdipDrawCurve2");
                GdipDrawCurve2I_ptr = LoadFunction<GdipDrawCurve2I_delegate>("GdipDrawCurve2I");
                GdipDrawCurve3_ptr = LoadFunction<GdipDrawCurve3_delegate>("GdipDrawCurve3");
                GdipDrawCurve3I_ptr = LoadFunction<GdipDrawCurve3I_delegate>("GdipDrawCurve3I");
                GdipDrawClosedCurve_ptr = LoadFunction<GdipDrawClosedCurve_delegate>("GdipDrawClosedCurve");
                GdipDrawClosedCurveI_ptr = LoadFunction<GdipDrawClosedCurveI_delegate>("GdipDrawClosedCurveI");
                GdipDrawClosedCurve2_ptr = LoadFunction<GdipDrawClosedCurve2_delegate>("GdipDrawClosedCurve2");
                GdipDrawClosedCurve2I_ptr = LoadFunction<GdipDrawClosedCurve2I_delegate>("GdipDrawClosedCurve2I");
                GdipGraphicsClear_ptr = LoadFunction<GdipGraphicsClear_delegate>("GdipGraphicsClear");
                GdipFillRectangle_ptr = LoadFunction<GdipFillRectangle_delegate>("GdipFillRectangle");
                GdipFillRectangleI_ptr = LoadFunction<GdipFillRectangleI_delegate>("GdipFillRectangleI");
                GdipFillRectangles_ptr = LoadFunction<GdipFillRectangles_delegate>("GdipFillRectangles");
                GdipFillRectanglesI_ptr = LoadFunction<GdipFillRectanglesI_delegate>("GdipFillRectanglesI");
                GdipFillPolygon_ptr = LoadFunction<GdipFillPolygon_delegate>("GdipFillPolygon");
                GdipFillPolygonI_ptr = LoadFunction<GdipFillPolygonI_delegate>("GdipFillPolygonI");
                GdipFillEllipse_ptr = LoadFunction<GdipFillEllipse_delegate>("GdipFillEllipse");
                GdipFillEllipseI_ptr = LoadFunction<GdipFillEllipseI_delegate>("GdipFillEllipseI");
                GdipFillPie_ptr = LoadFunction<GdipFillPie_delegate>("GdipFillPie");
                GdipFillPieI_ptr = LoadFunction<GdipFillPieI_delegate>("GdipFillPieI");
                GdipFillPath_ptr = LoadFunction<GdipFillPath_delegate>("GdipFillPath");
                GdipFillClosedCurve_ptr = LoadFunction<GdipFillClosedCurve_delegate>("GdipFillClosedCurve");
                GdipFillClosedCurveI_ptr = LoadFunction<GdipFillClosedCurveI_delegate>("GdipFillClosedCurveI");
                GdipFillClosedCurve2_ptr = LoadFunction<GdipFillClosedCurve2_delegate>("GdipFillClosedCurve2");
                GdipFillClosedCurve2I_ptr = LoadFunction<GdipFillClosedCurve2I_delegate>("GdipFillClosedCurve2I");
                GdipDrawImage_ptr = LoadFunction<GdipDrawImage_delegate>("GdipDrawImage");
                GdipDrawImageI_ptr = LoadFunction<GdipDrawImageI_delegate>("GdipDrawImageI");
                GdipDrawImageRect_ptr = LoadFunction<GdipDrawImageRect_delegate>("GdipDrawImageRect");
                GdipDrawImageRectI_ptr = LoadFunction<GdipDrawImageRectI_delegate>("GdipDrawImageRectI");
                GdipDrawImagePoints_ptr = LoadFunction<GdipDrawImagePoints_delegate>("GdipDrawImagePoints");
                GdipDrawImagePointsI_ptr = LoadFunction<GdipDrawImagePointsI_delegate>("GdipDrawImagePointsI");
                GdipDrawImagePointRect_ptr = LoadFunction<GdipDrawImagePointRect_delegate>("GdipDrawImagePointRect");
                GdipDrawImagePointRectI_ptr = LoadFunction<GdipDrawImagePointRectI_delegate>("GdipDrawImagePointRectI");
                GdipDrawImageRectRect_ptr = LoadFunction<GdipDrawImageRectRect_delegate>("GdipDrawImageRectRect");
                GdipDrawImageRectRectI_ptr = LoadFunction<GdipDrawImageRectRectI_delegate>("GdipDrawImageRectRectI");
                GdipDrawImagePointsRect_ptr = LoadFunction<GdipDrawImagePointsRect_delegate>("GdipDrawImagePointsRect");
                GdipDrawImagePointsRectI_ptr = LoadFunction<GdipDrawImagePointsRectI_delegate>("GdipDrawImagePointsRectI");
                GdipEnumerateMetafileDestPoint_ptr = LoadFunction<GdipEnumerateMetafileDestPoint_delegate>("GdipEnumerateMetafileDestPoint");
                GdipEnumerateMetafileDestPointI_ptr = LoadFunction<GdipEnumerateMetafileDestPointI_delegate>("GdipEnumerateMetafileDestPointI");
                GdipEnumerateMetafileDestRect_ptr = LoadFunction<GdipEnumerateMetafileDestRect_delegate>("GdipEnumerateMetafileDestRect");
                GdipEnumerateMetafileDestRectI_ptr = LoadFunction<GdipEnumerateMetafileDestRectI_delegate>("GdipEnumerateMetafileDestRectI");
                GdipEnumerateMetafileDestPoints_ptr = LoadFunction<GdipEnumerateMetafileDestPoints_delegate>("GdipEnumerateMetafileDestPoints");
                GdipEnumerateMetafileDestPointsI_ptr = LoadFunction<GdipEnumerateMetafileDestPointsI_delegate>("GdipEnumerateMetafileDestPointsI");
                GdipEnumerateMetafileSrcRectDestPoint_ptr = LoadFunction<GdipEnumerateMetafileSrcRectDestPoint_delegate>("GdipEnumerateMetafileSrcRectDestPoint");
                GdipEnumerateMetafileSrcRectDestPointI_ptr = LoadFunction<GdipEnumerateMetafileSrcRectDestPointI_delegate>("GdipEnumerateMetafileSrcRectDestPointI");
                GdipEnumerateMetafileSrcRectDestRect_ptr = LoadFunction<GdipEnumerateMetafileSrcRectDestRect_delegate>("GdipEnumerateMetafileSrcRectDestRect");
                GdipEnumerateMetafileSrcRectDestRectI_ptr = LoadFunction<GdipEnumerateMetafileSrcRectDestRectI_delegate>("GdipEnumerateMetafileSrcRectDestRectI");
                GdipEnumerateMetafileSrcRectDestPoints_ptr = LoadFunction<GdipEnumerateMetafileSrcRectDestPoints_delegate>("GdipEnumerateMetafileSrcRectDestPoints");
                GdipEnumerateMetafileSrcRectDestPointsI_ptr = LoadFunction<GdipEnumerateMetafileSrcRectDestPointsI_delegate>("GdipEnumerateMetafileSrcRectDestPointsI");
                GdipPlayMetafileRecord_ptr = LoadFunction<GdipPlayMetafileRecord_delegate>("GdipPlayMetafileRecord");
                GdipSaveGraphics_ptr = LoadFunction<GdipSaveGraphics_delegate>("GdipSaveGraphics");
                GdipRestoreGraphics_ptr = LoadFunction<GdipRestoreGraphics_delegate>("GdipRestoreGraphics");
                GdipEndContainer_ptr = LoadFunction<GdipEndContainer_delegate>("GdipEndContainer");
                GdipGetMetafileHeaderFromWmf_ptr = LoadFunction<GdipGetMetafileHeaderFromWmf_delegate>("GdipGetMetafileHeaderFromWmf");
                GdipGetMetafileHeaderFromEmf_ptr = LoadFunction<GdipGetMetafileHeaderFromEmf_delegate>("GdipGetMetafileHeaderFromEmf");
                GdipGetMetafileHeaderFromFile_ptr = LoadFunction<GdipGetMetafileHeaderFromFile_delegate>("GdipGetMetafileHeaderFromFile");
                GdipGetMetafileHeaderFromStream_ptr = LoadFunction<GdipGetMetafileHeaderFromStream_delegate>("GdipGetMetafileHeaderFromStream");
                GdipGetMetafileHeaderFromMetafile_ptr = LoadFunction<GdipGetMetafileHeaderFromMetafile_delegate>("GdipGetMetafileHeaderFromMetafile");
                GdipGetHemfFromMetafile_ptr = LoadFunction<GdipGetHemfFromMetafile_delegate>("GdipGetHemfFromMetafile");
                GdipCreateMetafileFromWmf_ptr = LoadFunction<GdipCreateMetafileFromWmf_delegate>("GdipCreateMetafileFromWmf");
                GdipCreateMetafileFromEmf_ptr = LoadFunction<GdipCreateMetafileFromEmf_delegate>("GdipCreateMetafileFromEmf");
                GdipCreateMetafileFromFile_ptr = LoadFunction<GdipCreateMetafileFromFile_delegate>("GdipCreateMetafileFromFile");
                GdipCreateMetafileFromStream_ptr = LoadFunction<GdipCreateMetafileFromStream_delegate>("GdipCreateMetafileFromStream");
                GdipRecordMetafile_ptr = LoadFunction<GdipRecordMetafile_delegate>("GdipRecordMetafile");
                GdipRecordMetafile2_ptr = LoadFunction<GdipRecordMetafile2_delegate>("GdipRecordMetafile");
                GdipRecordMetafileI_ptr = LoadFunction<GdipRecordMetafileI_delegate>("GdipRecordMetafileI");
                GdipRecordMetafileFileName_ptr = LoadFunction<GdipRecordMetafileFileName_delegate>("GdipRecordMetafileFileName");
                GdipRecordMetafileFileName2_ptr = LoadFunction<GdipRecordMetafileFileName2_delegate>("GdipRecordMetafileFileName");
                GdipRecordMetafileFileNameI_ptr = LoadFunction<GdipRecordMetafileFileNameI_delegate>("GdipRecordMetafileFileNameI");
                GdipRecordMetafileStream_ptr = LoadFunction<GdipRecordMetafileStream_delegate>("GdipRecordMetafileStream");
                GdipRecordMetafileStream2_ptr = LoadFunction<GdipRecordMetafileStream2_delegate>("GdipRecordMetafileStream");
                GdipRecordMetafileStreamI_ptr = LoadFunction<GdipRecordMetafileStreamI_delegate>("GdipRecordMetafileStreamI");
                GdipComment_ptr = LoadFunction<GdipComment_delegate>("GdipComment");
                GdipCreateFontFromDC_ptr = LoadFunction<GdipCreateFontFromDC_delegate>("GdipCreateFontFromDC");
                GdipCreateFontFromLogfontW_ptr = LoadFunction<GdipCreateFontFromLogfontW_delegate>("GdipCreateFontFromLogfontW");
                GdipCreateFont_ptr = LoadFunction<GdipCreateFont_delegate>("GdipCreateFont");
                GdipGetLogFontW_ptr = LoadFunction<GdipGetLogFontW_delegate>("GdipGetLogFontW");
                GdipCloneFont_ptr = LoadFunction<GdipCloneFont_delegate>("GdipCloneFont");
                GdipDeleteFont_ptr = LoadFunction<GdipDeleteFont_delegate>("GdipDeleteFont");
                GdipGetFamily_ptr = LoadFunction<GdipGetFamily_delegate>("GdipGetFamily");
                GdipGetFontStyle_ptr = LoadFunction<GdipGetFontStyle_delegate>("GdipGetFontStyle");
                GdipGetFontSize_ptr = LoadFunction<GdipGetFontSize_delegate>("GdipGetFontSize");
                GdipGetFontHeight_ptr = LoadFunction<GdipGetFontHeight_delegate>("GdipGetFontHeight");
                GdipGetFontHeightGivenDPI_ptr = LoadFunction<GdipGetFontHeightGivenDPI_delegate>("GdipGetFontHeightGivenDPI");
                GdipGetFontUnit_ptr = LoadFunction<GdipGetFontUnit_delegate>("GdipGetFontUnit");
                GdipDrawString_ptr = LoadFunction<GdipDrawString_delegate>("GdipDrawString");
                GdipMeasureString_ptr = LoadFunction<GdipMeasureString_delegate>("GdipMeasureString");
                GdipMeasureCharacterRanges_ptr = LoadFunction<GdipMeasureCharacterRanges_delegate>("GdipMeasureCharacterRanges");
            }

            // Imported functions

            private delegate int GdiplusStartup_delegate(out IntPtr token, ref StartupInput input, out StartupOutput output);
            private static FunctionWrapper<GdiplusStartup_delegate> GdiplusStartup_ptr;
            private static int GdiplusStartup(out IntPtr token, ref StartupInput input, out StartupOutput output) => GdiplusStartup_ptr.Delegate(out token, ref input, out output);

            private delegate int GdipCreatePath_delegate(int brushMode, out IntPtr path);
            private static FunctionWrapper<GdipCreatePath_delegate> GdipCreatePath_ptr;
            internal static int GdipCreatePath(int brushMode, out IntPtr path) => GdipCreatePath_ptr.Delegate(brushMode, out path);

            private delegate int GdipCreatePath2_delegate(HandleRef points, HandleRef types, int count, int brushMode, out IntPtr path);
            private static FunctionWrapper<GdipCreatePath2_delegate> GdipCreatePath2_ptr;
            internal static int GdipCreatePath2(HandleRef points, HandleRef types, int count, int brushMode, out IntPtr path) => GdipCreatePath2_ptr.Delegate(points, types, count, brushMode, out path);

            private delegate int GdipCreatePath2I_delegate(HandleRef points, HandleRef types, int count, int brushMode, out IntPtr path);
            private static FunctionWrapper<GdipCreatePath2I_delegate> GdipCreatePath2I_ptr;
            internal static int GdipCreatePath2I(HandleRef points, HandleRef types, int count, int brushMode, out IntPtr path) => GdipCreatePath2I_ptr.Delegate(points, types, count, brushMode, out path);

            private delegate int GdipClonePath_delegate(HandleRef path, out IntPtr clonepath);
            private static FunctionWrapper<GdipClonePath_delegate> GdipClonePath_ptr;
            internal static int GdipClonePath(HandleRef path, out IntPtr clonepath) => GdipClonePath_ptr.Delegate(path, out clonepath);

            private delegate int GdipDeletePath_delegate(HandleRef path);
            private static FunctionWrapper<GdipDeletePath_delegate> GdipDeletePath_ptr;
            internal static int IntGdipDeletePath(HandleRef path) => GdipDeletePath_ptr.Delegate(path);

            private delegate int GdipResetPath_delegate(HandleRef path);
            private static FunctionWrapper<GdipResetPath_delegate> GdipResetPath_ptr;
            internal static int GdipResetPath(HandleRef path) => GdipResetPath_ptr.Delegate(path);

            private delegate int GdipGetPointCount_delegate(HandleRef path, out int count);
            private static FunctionWrapper<GdipGetPointCount_delegate> GdipGetPointCount_ptr;
            internal static int GdipGetPointCount(HandleRef path, out int count) => GdipGetPointCount_ptr.Delegate(path, out count);

            private delegate int GdipGetPathTypes_delegate(HandleRef path, byte[] types, int count);
            private static FunctionWrapper<GdipGetPathTypes_delegate> GdipGetPathTypes_ptr;
            internal static int GdipGetPathTypes(HandleRef path, byte[] types, int count) => GdipGetPathTypes_ptr.Delegate(path, types, count);

            private delegate int GdipGetPathPoints_delegate(HandleRef path, HandleRef points, int count);
            private static FunctionWrapper<GdipGetPathPoints_delegate> GdipGetPathPoints_ptr;
            internal static int GdipGetPathPoints(HandleRef path, HandleRef points, int count) => GdipGetPathPoints_ptr.Delegate(path, points, count);

            private delegate int GdipGetPathFillMode_delegate(HandleRef path, out int fillmode);
            private static FunctionWrapper<GdipGetPathFillMode_delegate> GdipGetPathFillMode_ptr;
            internal static int GdipGetPathFillMode(HandleRef path, out int fillmode) => GdipGetPathFillMode_ptr.Delegate(path, out fillmode);

            private delegate int GdipSetPathFillMode_delegate(HandleRef path, int fillmode);
            private static FunctionWrapper<GdipSetPathFillMode_delegate> GdipSetPathFillMode_ptr;
            internal static int GdipSetPathFillMode(HandleRef path, int fillmode) => GdipSetPathFillMode_ptr.Delegate(path, fillmode);

            private delegate int GdipGetPathData_delegate(HandleRef path, IntPtr pathData);
            private static FunctionWrapper<GdipGetPathData_delegate> GdipGetPathData_ptr;
            internal static int GdipGetPathData(HandleRef path, IntPtr pathData) => GdipGetPathData_ptr.Delegate(path, pathData);

            private delegate int GdipStartPathFigure_delegate(HandleRef path);
            private static FunctionWrapper<GdipStartPathFigure_delegate> GdipStartPathFigure_ptr;
            internal static int GdipStartPathFigure(HandleRef path) => GdipStartPathFigure_ptr.Delegate(path);

            private delegate int GdipClosePathFigure_delegate(HandleRef path);
            private static FunctionWrapper<GdipClosePathFigure_delegate> GdipClosePathFigure_ptr;
            internal static int GdipClosePathFigure(HandleRef path) => GdipClosePathFigure_ptr.Delegate(path);

            private delegate int GdipClosePathFigures_delegate(HandleRef path);
            private static FunctionWrapper<GdipClosePathFigures_delegate> GdipClosePathFigures_ptr;
            internal static int GdipClosePathFigures(HandleRef path) => GdipClosePathFigures_ptr.Delegate(path);

            private delegate int GdipSetPathMarker_delegate(HandleRef path);
            private static FunctionWrapper<GdipSetPathMarker_delegate> GdipSetPathMarker_ptr;
            internal static int GdipSetPathMarker(HandleRef path) => GdipSetPathMarker_ptr.Delegate(path);

            private delegate int GdipClearPathMarkers_delegate(HandleRef path);
            private static FunctionWrapper<GdipClearPathMarkers_delegate> GdipClearPathMarkers_ptr;
            internal static int GdipClearPathMarkers(HandleRef path) => GdipClearPathMarkers_ptr.Delegate(path);

            private delegate int GdipReversePath_delegate(HandleRef path);
            private static FunctionWrapper<GdipReversePath_delegate> GdipReversePath_ptr;
            internal static int GdipReversePath(HandleRef path) => GdipReversePath_ptr.Delegate(path);

            private delegate int GdipGetPathLastPoint_delegate(HandleRef path, GPPOINTF lastPoint);
            private static FunctionWrapper<GdipGetPathLastPoint_delegate> GdipGetPathLastPoint_ptr;
            internal static int GdipGetPathLastPoint(HandleRef path, GPPOINTF lastPoint) => GdipGetPathLastPoint_ptr.Delegate(path, lastPoint);

            private delegate int GdipAddPathLine_delegate(HandleRef path, float x1, float y1, float x2, float y2);
            private static FunctionWrapper<GdipAddPathLine_delegate> GdipAddPathLine_ptr;
            internal static int GdipAddPathLine(HandleRef path, float x1, float y1, float x2, float y2) => GdipAddPathLine_ptr.Delegate(path, x1, y1, x2, y2);

            private delegate int GdipAddPathLine2_delegate(HandleRef path, HandleRef memorypts, int count);
            private static FunctionWrapper<GdipAddPathLine2_delegate> GdipAddPathLine2_ptr;
            internal static int GdipAddPathLine2(HandleRef path, HandleRef memorypts, int count) => GdipAddPathLine2_ptr.Delegate(path, memorypts, count);

            private delegate int GdipAddPathArc_delegate(HandleRef path, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathArc_delegate> GdipAddPathArc_ptr;
            internal static int GdipAddPathArc(HandleRef path, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipAddPathArc_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipAddPathBezier_delegate(HandleRef path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);
            private static FunctionWrapper<GdipAddPathBezier_delegate> GdipAddPathBezier_ptr;
            internal static int GdipAddPathBezier(HandleRef path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) => GdipAddPathBezier_ptr.Delegate(path, x1, y1, x2, y2, x3, y3, x4, y4);

            private delegate int GdipAddPathBeziers_delegate(HandleRef path, HandleRef memorypts, int count);
            private static FunctionWrapper<GdipAddPathBeziers_delegate> GdipAddPathBeziers_ptr;
            internal static int GdipAddPathBeziers(HandleRef path, HandleRef memorypts, int count) => GdipAddPathBeziers_ptr.Delegate(path, memorypts, count);

            private delegate int GdipAddPathCurve_delegate(HandleRef path, HandleRef memorypts, int count);
            private static FunctionWrapper<GdipAddPathCurve_delegate> GdipAddPathCurve_ptr;
            internal static int GdipAddPathCurve(HandleRef path, HandleRef memorypts, int count) => GdipAddPathCurve_ptr.Delegate(path, memorypts, count);

            private delegate int GdipAddPathCurve2_delegate(HandleRef path, HandleRef memorypts, int count, float tension);
            private static FunctionWrapper<GdipAddPathCurve2_delegate> GdipAddPathCurve2_ptr;
            internal static int GdipAddPathCurve2(HandleRef path, HandleRef memorypts, int count, float tension) => GdipAddPathCurve2_ptr.Delegate(path, memorypts, count, tension);

            private delegate int GdipAddPathCurve3_delegate(HandleRef path, HandleRef memorypts, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipAddPathCurve3_delegate> GdipAddPathCurve3_ptr;
            internal static int GdipAddPathCurve3(HandleRef path, HandleRef memorypts, int count, int offset, int numberOfSegments, float tension) => GdipAddPathCurve3_ptr.Delegate(path, memorypts, count, offset, numberOfSegments, tension);

            private delegate int GdipAddPathClosedCurve_delegate(HandleRef path, HandleRef memorypts, int count);
            private static FunctionWrapper<GdipAddPathClosedCurve_delegate> GdipAddPathClosedCurve_ptr;
            internal static int GdipAddPathClosedCurve(HandleRef path, HandleRef memorypts, int count) => GdipAddPathClosedCurve_ptr.Delegate(path, memorypts, count);

            private delegate int GdipAddPathClosedCurve2_delegate(HandleRef path, HandleRef memorypts, int count, float tension);
            private static FunctionWrapper<GdipAddPathClosedCurve2_delegate> GdipAddPathClosedCurve2_ptr;
            internal static int GdipAddPathClosedCurve2(HandleRef path, HandleRef memorypts, int count, float tension) => GdipAddPathClosedCurve2_ptr.Delegate(path, memorypts, count, tension);

            private delegate int GdipAddPathRectangle_delegate(HandleRef path, float x, float y, float width, float height);
            private static FunctionWrapper<GdipAddPathRectangle_delegate> GdipAddPathRectangle_ptr;
            internal static int GdipAddPathRectangle(HandleRef path, float x, float y, float width, float height) => GdipAddPathRectangle_ptr.Delegate(path, x, y, width, height);

            private delegate int GdipAddPathRectangles_delegate(HandleRef path, HandleRef rects, int count);
            private static FunctionWrapper<GdipAddPathRectangles_delegate> GdipAddPathRectangles_ptr;
            internal static int GdipAddPathRectangles(HandleRef path, HandleRef rects, int count) => GdipAddPathRectangles_ptr.Delegate(path, rects, count);

            private delegate int GdipAddPathEllipse_delegate(HandleRef path, float x, float y, float width, float height);
            private static FunctionWrapper<GdipAddPathEllipse_delegate> GdipAddPathEllipse_ptr;
            internal static int GdipAddPathEllipse(HandleRef path, float x, float y, float width, float height) => GdipAddPathEllipse_ptr.Delegate(path, x, y, width, height);

            private delegate int GdipAddPathPie_delegate(HandleRef path, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathPie_delegate> GdipAddPathPie_ptr;
            internal static int GdipAddPathPie(HandleRef path, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipAddPathPie_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipAddPathPolygon_delegate(HandleRef path, HandleRef memorypts, int count);
            private static FunctionWrapper<GdipAddPathPolygon_delegate> GdipAddPathPolygon_ptr;
            internal static int GdipAddPathPolygon(HandleRef path, HandleRef memorypts, int count) => GdipAddPathPolygon_ptr.Delegate(path, memorypts, count);

            private delegate int GdipAddPathPath_delegate(HandleRef path, HandleRef addingPath, bool connect);
            private static FunctionWrapper<GdipAddPathPath_delegate> GdipAddPathPath_ptr;
            internal static int GdipAddPathPath(HandleRef path, HandleRef addingPath, bool connect) => GdipAddPathPath_ptr.Delegate(path, addingPath, connect);

            private delegate int GdipAddPathString_delegate(HandleRef path, [MarshalAs(UnmanagedType.LPWStr)]string s, int length, HandleRef fontFamily, int style, float emSize, ref GPRECTF layoutRect, HandleRef format);
            private static FunctionWrapper<GdipAddPathString_delegate> GdipAddPathString_ptr;
            internal static int GdipAddPathString(HandleRef path, string s, int length, HandleRef fontFamily, int style, float emSize, ref GPRECTF layoutRect, HandleRef format) => GdipAddPathString_ptr.Delegate(path, s, length, fontFamily, style, emSize, ref layoutRect, format);

            private delegate int GdipAddPathStringI_delegate(HandleRef path, [MarshalAs(UnmanagedType.LPWStr)]string s, int length, HandleRef fontFamily, int style, float emSize, ref GPRECT layoutRect, HandleRef format);
            private static FunctionWrapper<GdipAddPathStringI_delegate> GdipAddPathStringI_ptr;
            internal static int GdipAddPathStringI(HandleRef path, string s, int length, HandleRef fontFamily, int style, float emSize, ref GPRECT layoutRect, HandleRef format) => GdipAddPathStringI_ptr.Delegate(path, s, length, fontFamily, style, emSize, ref layoutRect, format);

            private delegate int GdipAddPathLineI_delegate(HandleRef path, int x1, int y1, int x2, int y2);
            private static FunctionWrapper<GdipAddPathLineI_delegate> GdipAddPathLineI_ptr;
            internal static int GdipAddPathLineI(HandleRef path, int x1, int y1, int x2, int y2) => GdipAddPathLineI_ptr.Delegate(path, x1, y1, x2, y2);

            private delegate int GdipAddPathLine2I_delegate(HandleRef path, HandleRef memorypts, int count);
            private static FunctionWrapper<GdipAddPathLine2I_delegate> GdipAddPathLine2I_ptr;
            internal static int GdipAddPathLine2I(HandleRef path, HandleRef memorypts, int count) => GdipAddPathLine2I_ptr.Delegate(path, memorypts, count);

            private delegate int GdipAddPathArcI_delegate(HandleRef path, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathArcI_delegate> GdipAddPathArcI_ptr;
            internal static int GdipAddPathArcI(HandleRef path, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipAddPathArcI_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipAddPathBezierI_delegate(HandleRef path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);
            private static FunctionWrapper<GdipAddPathBezierI_delegate> GdipAddPathBezierI_ptr;
            internal static int GdipAddPathBezierI(HandleRef path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4) => GdipAddPathBezierI_ptr.Delegate(path, x1, y1, x2, y2, x3, y3, x4, y4);

            private delegate int GdipAddPathBeziersI_delegate(HandleRef path, HandleRef memorypts, int count);
            private static FunctionWrapper<GdipAddPathBeziersI_delegate> GdipAddPathBeziersI_ptr;
            internal static int GdipAddPathBeziersI(HandleRef path, HandleRef memorypts, int count) => GdipAddPathBeziersI_ptr.Delegate(path, memorypts, count);

            private delegate int GdipAddPathCurveI_delegate(HandleRef path, HandleRef memorypts, int count);
            private static FunctionWrapper<GdipAddPathCurveI_delegate> GdipAddPathCurveI_ptr;
            internal static int GdipAddPathCurveI(HandleRef path, HandleRef memorypts, int count) => GdipAddPathCurveI_ptr.Delegate(path, memorypts, count);

            private delegate int GdipAddPathCurve2I_delegate(HandleRef path, HandleRef memorypts, int count, float tension);
            private static FunctionWrapper<GdipAddPathCurve2I_delegate> GdipAddPathCurve2I_ptr;
            internal static int GdipAddPathCurve2I(HandleRef path, HandleRef memorypts, int count, float tension) => GdipAddPathCurve2I_ptr.Delegate(path, memorypts, count, tension);

            private delegate int GdipAddPathCurve3I_delegate(HandleRef path, HandleRef memorypts, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipAddPathCurve3I_delegate> GdipAddPathCurve3I_ptr;
            internal static int GdipAddPathCurve3I(HandleRef path, HandleRef memorypts, int count, int offset, int numberOfSegments, float tension) => GdipAddPathCurve3I_ptr.Delegate(path, memorypts, count, offset, numberOfSegments, tension);

            private delegate int GdipAddPathClosedCurveI_delegate(HandleRef path, HandleRef memorypts, int count);
            private static FunctionWrapper<GdipAddPathClosedCurveI_delegate> GdipAddPathClosedCurveI_ptr;
            internal static int GdipAddPathClosedCurveI(HandleRef path, HandleRef memorypts, int count) => GdipAddPathClosedCurveI_ptr.Delegate(path, memorypts, count);

            private delegate int GdipAddPathClosedCurve2I_delegate(HandleRef path, HandleRef memorypts, int count, float tension);
            private static FunctionWrapper<GdipAddPathClosedCurve2I_delegate> GdipAddPathClosedCurve2I_ptr;
            internal static int GdipAddPathClosedCurve2I(HandleRef path, HandleRef memorypts, int count, float tension) => GdipAddPathClosedCurve2I_ptr.Delegate(path, memorypts, count, tension);

            private delegate int GdipAddPathRectangleI_delegate(HandleRef path, int x, int y, int width, int height);
            private static FunctionWrapper<GdipAddPathRectangleI_delegate> GdipAddPathRectangleI_ptr;
            internal static int GdipAddPathRectangleI(HandleRef path, int x, int y, int width, int height) => GdipAddPathRectangleI_ptr.Delegate(path, x, y, width, height);

            private delegate int GdipAddPathRectanglesI_delegate(HandleRef path, HandleRef rects, int count);
            private static FunctionWrapper<GdipAddPathRectanglesI_delegate> GdipAddPathRectanglesI_ptr;
            internal static int GdipAddPathRectanglesI(HandleRef path, HandleRef rects, int count) => GdipAddPathRectanglesI_ptr.Delegate(path, rects, count);

            private delegate int GdipAddPathEllipseI_delegate(HandleRef path, int x, int y, int width, int height);
            private static FunctionWrapper<GdipAddPathEllipseI_delegate> GdipAddPathEllipseI_ptr;
            internal static int GdipAddPathEllipseI(HandleRef path, int x, int y, int width, int height) => GdipAddPathEllipseI_ptr.Delegate(path, x, y, width, height);

            private delegate int GdipAddPathPieI_delegate(HandleRef path, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathPieI_delegate> GdipAddPathPieI_ptr;
            internal static int GdipAddPathPieI(HandleRef path, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipAddPathPieI_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipAddPathPolygonI_delegate(HandleRef path, HandleRef memorypts, int count);
            private static FunctionWrapper<GdipAddPathPolygonI_delegate> GdipAddPathPolygonI_ptr;
            internal static int GdipAddPathPolygonI(HandleRef path, HandleRef memorypts, int count) => GdipAddPathPolygonI_ptr.Delegate(path, memorypts, count);

            private delegate int GdipFlattenPath_delegate(HandleRef path, HandleRef matrixfloat, float flatness);
            private static FunctionWrapper<GdipFlattenPath_delegate> GdipFlattenPath_ptr;
            internal static int GdipFlattenPath(HandleRef path, HandleRef matrixfloat, float flatness) => GdipFlattenPath_ptr.Delegate(path, matrixfloat, flatness);

            private delegate int GdipWidenPath_delegate(HandleRef path, HandleRef pen, HandleRef matrix, float flatness);
            private static FunctionWrapper<GdipWidenPath_delegate> GdipWidenPath_ptr;
            internal static int GdipWidenPath(HandleRef path, HandleRef pen, HandleRef matrix, float flatness) => GdipWidenPath_ptr.Delegate(path, pen, matrix, flatness);

            private delegate int GdipWarpPath_delegate(HandleRef path, HandleRef matrix, HandleRef points, int count, float srcX, float srcY, float srcWidth, float srcHeight, WarpMode warpMode, float flatness);
            private static FunctionWrapper<GdipWarpPath_delegate> GdipWarpPath_ptr;
            internal static int GdipWarpPath(HandleRef path, HandleRef matrix, HandleRef points, int count, float srcX, float srcY, float srcWidth, float srcHeight, WarpMode warpMode, float flatness) => GdipWarpPath_ptr.Delegate(path, matrix, points, count, srcX, srcY, srcWidth, srcHeight, warpMode, flatness);

            private delegate int GdipTransformPath_delegate(HandleRef path, HandleRef matrix);
            private static FunctionWrapper<GdipTransformPath_delegate> GdipTransformPath_ptr;
            internal static int GdipTransformPath(HandleRef path, HandleRef matrix) => GdipTransformPath_ptr.Delegate(path, matrix);

            private delegate int GdipGetPathWorldBounds_delegate(HandleRef path, ref GPRECTF gprectf, HandleRef matrix, HandleRef pen);
            private static FunctionWrapper<GdipGetPathWorldBounds_delegate> GdipGetPathWorldBounds_ptr;
            internal static int GdipGetPathWorldBounds(HandleRef path, ref GPRECTF gprectf, HandleRef matrix, HandleRef pen) => GdipGetPathWorldBounds_ptr.Delegate(path, ref gprectf, matrix, pen);

            private delegate int GdipIsVisiblePathPoint_delegate(HandleRef path, float x, float y, HandleRef graphics, out int boolean);
            private static FunctionWrapper<GdipIsVisiblePathPoint_delegate> GdipIsVisiblePathPoint_ptr;
            internal static int GdipIsVisiblePathPoint(HandleRef path, float x, float y, HandleRef graphics, out int boolean) => GdipIsVisiblePathPoint_ptr.Delegate(path, x, y, graphics, out boolean);

            private delegate int GdipIsVisiblePathPointI_delegate(HandleRef path, int x, int y, HandleRef graphics, out int boolean);
            private static FunctionWrapper<GdipIsVisiblePathPointI_delegate> GdipIsVisiblePathPointI_ptr;
            internal static int GdipIsVisiblePathPointI(HandleRef path, int x, int y, HandleRef graphics, out int boolean) => GdipIsVisiblePathPointI_ptr.Delegate(path, x, y, graphics, out boolean);

            private delegate int GdipIsOutlineVisiblePathPoint_delegate(HandleRef path, float x, float y, HandleRef pen, HandleRef graphics, out int boolean);
            private static FunctionWrapper<GdipIsOutlineVisiblePathPoint_delegate> GdipIsOutlineVisiblePathPoint_ptr;
            internal static int GdipIsOutlineVisiblePathPoint(HandleRef path, float x, float y, HandleRef pen, HandleRef graphics, out int boolean) => GdipIsOutlineVisiblePathPoint_ptr.Delegate(path, x, y, pen, graphics, out boolean);

            private delegate int GdipIsOutlineVisiblePathPointI_delegate(HandleRef path, int x, int y, HandleRef pen, HandleRef graphics, out int boolean);
            private static FunctionWrapper<GdipIsOutlineVisiblePathPointI_delegate> GdipIsOutlineVisiblePathPointI_ptr;
            internal static int GdipIsOutlineVisiblePathPointI(HandleRef path, int x, int y, HandleRef pen, HandleRef graphics, out int boolean) => GdipIsOutlineVisiblePathPointI_ptr.Delegate(path, x, y, pen, graphics, out boolean);

            private delegate int GdipDeleteBrush_delegate(HandleRef brush);
            private static FunctionWrapper<GdipDeleteBrush_delegate> GdipDeleteBrush_ptr;
            internal static int IntGdipDeleteBrush(HandleRef brush) => GdipDeleteBrush_ptr.Delegate(brush);

            private delegate int GdipLoadImageFromStream_delegate(UnsafeNativeMethods.IStream stream, out IntPtr image);
            private static FunctionWrapper<GdipLoadImageFromStream_delegate> GdipLoadImageFromStream_ptr;
            internal static int GdipLoadImageFromStream(UnsafeNativeMethods.IStream stream, out IntPtr image) => GdipLoadImageFromStream_ptr.Delegate(stream, out image);

            private delegate int GdipLoadImageFromFile_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr image);
            private static FunctionWrapper<GdipLoadImageFromFile_delegate> GdipLoadImageFromFile_ptr;
            internal static int GdipLoadImageFromFile(string filename, out IntPtr image) => GdipLoadImageFromFile_ptr.Delegate(filename, out image);

            private delegate int GdipLoadImageFromStreamICM_delegate(UnsafeNativeMethods.IStream stream, out IntPtr image);
            private static FunctionWrapper<GdipLoadImageFromStreamICM_delegate> GdipLoadImageFromStreamICM_ptr;
            internal static int GdipLoadImageFromStreamICM(UnsafeNativeMethods.IStream stream, out IntPtr image) => GdipLoadImageFromStreamICM_ptr.Delegate(stream, out image);

            private delegate int GdipLoadImageFromFileICM_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr image);
            private static FunctionWrapper<GdipLoadImageFromFileICM_delegate> GdipLoadImageFromFileICM_ptr;
            internal static int GdipLoadImageFromFileICM(string filename, out IntPtr image) => GdipLoadImageFromFileICM_ptr.Delegate(filename, out image);

            private delegate int GdipCloneImage_delegate(HandleRef image, out IntPtr cloneimage);
            private static FunctionWrapper<GdipCloneImage_delegate> GdipCloneImage_ptr;
            internal static int GdipCloneImage(HandleRef image, out IntPtr cloneimage) => GdipCloneImage_ptr.Delegate(image, out cloneimage);

            private delegate int GdipDisposeImage_delegate(HandleRef image);
            private static FunctionWrapper<GdipDisposeImage_delegate> GdipDisposeImage_ptr;
            internal static int IntGdipDisposeImage(HandleRef image) => GdipDisposeImage_ptr.Delegate(image);

            private delegate int GdipSaveImageToFile_delegate(HandleRef image, [MarshalAs(UnmanagedType.LPWStr)] string filename, ref Guid classId, HandleRef encoderParams);
            private static FunctionWrapper<GdipSaveImageToFile_delegate> GdipSaveImageToFile_ptr;
            internal static int GdipSaveImageToFile(HandleRef image, string filename, ref Guid classId, HandleRef encoderParams) => GdipSaveImageToFile_ptr.Delegate(image, filename, ref classId, encoderParams);

            private delegate int GdipSaveImageToStream_delegate(HandleRef image, UnsafeNativeMethods.IStream stream, ref Guid classId, HandleRef encoderParams);
            private static FunctionWrapper<GdipSaveImageToStream_delegate> GdipSaveImageToStream_ptr;
            internal static int GdipSaveImageToStream(HandleRef image, UnsafeNativeMethods.IStream stream, ref Guid classId, HandleRef encoderParams) => GdipSaveImageToStream_ptr.Delegate(image, stream, ref classId, encoderParams);

            private delegate int GdipSaveAdd_delegate(HandleRef image, HandleRef encoderParams);
            private static FunctionWrapper<GdipSaveAdd_delegate> GdipSaveAdd_ptr;
            internal static int GdipSaveAdd(HandleRef image, HandleRef encoderParams) => GdipSaveAdd_ptr.Delegate(image, encoderParams);

            private delegate int GdipSaveAddImage_delegate(HandleRef image, HandleRef newImage, HandleRef encoderParams);
            private static FunctionWrapper<GdipSaveAddImage_delegate> GdipSaveAddImage_ptr;
            internal static int GdipSaveAddImage(HandleRef image, HandleRef newImage, HandleRef encoderParams) => GdipSaveAddImage_ptr.Delegate(image, newImage, encoderParams);

            private delegate int GdipGetImageGraphicsContext_delegate(HandleRef image, out IntPtr graphics);
            private static FunctionWrapper<GdipGetImageGraphicsContext_delegate> GdipGetImageGraphicsContext_ptr;
            internal static int GdipGetImageGraphicsContext(HandleRef image, out IntPtr graphics) => GdipGetImageGraphicsContext_ptr.Delegate(image, out graphics);

            private delegate int GdipGetImageBounds_delegate(HandleRef image, ref GPRECTF gprectf, out GraphicsUnit unit);
            private static FunctionWrapper<GdipGetImageBounds_delegate> GdipGetImageBounds_ptr;
            internal static int GdipGetImageBounds(HandleRef image, ref GPRECTF gprectf, out GraphicsUnit unit) => GdipGetImageBounds_ptr.Delegate(image, ref gprectf, out unit);

            private delegate int GdipGetImageDimension_delegate(HandleRef image, out float width, out float height);
            private static FunctionWrapper<GdipGetImageDimension_delegate> GdipGetImageDimension_ptr;
            internal static int GdipGetImageDimension(HandleRef image, out float width, out float height) => GdipGetImageDimension_ptr.Delegate(image, out width, out height);

            private delegate int GdipGetImageType_delegate(HandleRef image, out int type);
            private static FunctionWrapper<GdipGetImageType_delegate> GdipGetImageType_ptr;
            internal static int GdipGetImageType(HandleRef image, out int type) => GdipGetImageType_ptr.Delegate(image, out type);

            private delegate int GdipGetImageWidth_delegate(HandleRef image, out int width);
            private static FunctionWrapper<GdipGetImageWidth_delegate> GdipGetImageWidth_ptr;
            internal static int GdipGetImageWidth(HandleRef image, out int width) => GdipGetImageWidth_ptr.Delegate(image, out width);

            private delegate int GdipGetImageHeight_delegate(HandleRef image, out int height);
            private static FunctionWrapper<GdipGetImageHeight_delegate> GdipGetImageHeight_ptr;
            internal static int GdipGetImageHeight(HandleRef image, out int height) => GdipGetImageHeight_ptr.Delegate(image, out height);

            private delegate int GdipGetImageHorizontalResolution_delegate(HandleRef image, out float horzRes);
            private static FunctionWrapper<GdipGetImageHorizontalResolution_delegate> GdipGetImageHorizontalResolution_ptr;
            internal static int GdipGetImageHorizontalResolution(HandleRef image, out float horzRes) => GdipGetImageHorizontalResolution_ptr.Delegate(image, out horzRes);

            private delegate int GdipGetImageVerticalResolution_delegate(HandleRef image, out float vertRes);
            private static FunctionWrapper<GdipGetImageVerticalResolution_delegate> GdipGetImageVerticalResolution_ptr;
            internal static int GdipGetImageVerticalResolution(HandleRef image, out float vertRes) => GdipGetImageVerticalResolution_ptr.Delegate(image, out vertRes);

            private delegate int GdipGetImageFlags_delegate(HandleRef image, out int flags);
            private static FunctionWrapper<GdipGetImageFlags_delegate> GdipGetImageFlags_ptr;
            internal static int GdipGetImageFlags(HandleRef image, out int flags) => GdipGetImageFlags_ptr.Delegate(image, out flags);

            private delegate int GdipGetImageRawFormat_delegate(HandleRef image, ref Guid format);
            private static FunctionWrapper<GdipGetImageRawFormat_delegate> GdipGetImageRawFormat_ptr;
            internal static int GdipGetImageRawFormat(HandleRef image, ref Guid format) => GdipGetImageRawFormat_ptr.Delegate(image, ref format);

            private delegate int GdipGetImagePixelFormat_delegate(HandleRef image, out int format);
            private static FunctionWrapper<GdipGetImagePixelFormat_delegate> GdipGetImagePixelFormat_ptr;
            internal static int GdipGetImagePixelFormat(HandleRef image, out int format) => GdipGetImagePixelFormat_ptr.Delegate(image, out format);

            private delegate int GdipGetImageThumbnail_delegate(HandleRef image, int thumbWidth, int thumbHeight, out IntPtr thumbImage, Image.GetThumbnailImageAbort callback, IntPtr callbackdata);
            private static FunctionWrapper<GdipGetImageThumbnail_delegate> GdipGetImageThumbnail_ptr;
            internal static int GdipGetImageThumbnail(HandleRef image, int thumbWidth, int thumbHeight, out IntPtr thumbImage, Image.GetThumbnailImageAbort callback, IntPtr callbackdata) => GdipGetImageThumbnail_ptr.Delegate(image, thumbWidth, thumbHeight, out thumbImage, callback, callbackdata);

            private delegate int GdipGetEncoderParameterListSize_delegate(HandleRef image, ref Guid clsid, out int size);
            private static FunctionWrapper<GdipGetEncoderParameterListSize_delegate> GdipGetEncoderParameterListSize_ptr;
            internal static int GdipGetEncoderParameterListSize(HandleRef image, ref Guid clsid, out int size) => GdipGetEncoderParameterListSize_ptr.Delegate(image, ref clsid, out size);

            private delegate int GdipGetEncoderParameterList_delegate(HandleRef image, ref Guid clsid, int size, IntPtr buffer);
            private static FunctionWrapper<GdipGetEncoderParameterList_delegate> GdipGetEncoderParameterList_ptr;
            internal static int GdipGetEncoderParameterList(HandleRef image, ref Guid clsid, int size, IntPtr buffer) => GdipGetEncoderParameterList_ptr.Delegate(image, ref clsid, size, buffer);

            private delegate int GdipImageGetFrameDimensionsCount_delegate(HandleRef image, out int count);
            private static FunctionWrapper<GdipImageGetFrameDimensionsCount_delegate> GdipImageGetFrameDimensionsCount_ptr;
            internal static int GdipImageGetFrameDimensionsCount(HandleRef image, out int count) => GdipImageGetFrameDimensionsCount_ptr.Delegate(image, out count);

            private delegate int GdipImageGetFrameDimensionsList_delegate(HandleRef image, IntPtr buffer, int count);
            private static FunctionWrapper<GdipImageGetFrameDimensionsList_delegate> GdipImageGetFrameDimensionsList_ptr;
            internal static int GdipImageGetFrameDimensionsList(HandleRef image, IntPtr buffer, int count) => GdipImageGetFrameDimensionsList_ptr.Delegate(image, buffer, count);

            private delegate int GdipImageGetFrameCount_delegate(HandleRef image, ref Guid dimensionID, int[] count);
            private static FunctionWrapper<GdipImageGetFrameCount_delegate> GdipImageGetFrameCount_ptr;
            internal static int GdipImageGetFrameCount(HandleRef image, ref Guid dimensionID, int[] count) => GdipImageGetFrameCount_ptr.Delegate(image, ref dimensionID, count);

            private delegate int GdipImageSelectActiveFrame_delegate(HandleRef image, ref Guid dimensionID, int frameIndex);
            private static FunctionWrapper<GdipImageSelectActiveFrame_delegate> GdipImageSelectActiveFrame_ptr;
            internal static int GdipImageSelectActiveFrame(HandleRef image, ref Guid dimensionID, int frameIndex) => GdipImageSelectActiveFrame_ptr.Delegate(image, ref dimensionID, frameIndex);

            private delegate int GdipImageRotateFlip_delegate(HandleRef image, int rotateFlipType);
            private static FunctionWrapper<GdipImageRotateFlip_delegate> GdipImageRotateFlip_ptr;
            internal static int GdipImageRotateFlip(HandleRef image, int rotateFlipType) => GdipImageRotateFlip_ptr.Delegate(image, rotateFlipType);

            private delegate int GdipGetImagePalette_delegate(HandleRef image, IntPtr palette, int size);
            private static FunctionWrapper<GdipGetImagePalette_delegate> GdipGetImagePalette_ptr;
            internal static int GdipGetImagePalette(HandleRef image, IntPtr palette, int size) => GdipGetImagePalette_ptr.Delegate(image, palette, size);

            private delegate int GdipSetImagePalette_delegate(HandleRef image, IntPtr palette);
            private static FunctionWrapper<GdipSetImagePalette_delegate> GdipSetImagePalette_ptr;
            internal static int GdipSetImagePalette(HandleRef image, IntPtr palette) => GdipSetImagePalette_ptr.Delegate(image, palette);

            private delegate int GdipGetImagePaletteSize_delegate(HandleRef image, out int size);
            private static FunctionWrapper<GdipGetImagePaletteSize_delegate> GdipGetImagePaletteSize_ptr;
            internal static int GdipGetImagePaletteSize(HandleRef image, out int size) => GdipGetImagePaletteSize_ptr.Delegate(image, out size);

            private delegate int GdipGetPropertyCount_delegate(HandleRef image, out int count);
            private static FunctionWrapper<GdipGetPropertyCount_delegate> GdipGetPropertyCount_ptr;
            internal static int GdipGetPropertyCount(HandleRef image, out int count) => GdipGetPropertyCount_ptr.Delegate(image, out count);

            private delegate int GdipGetPropertyIdList_delegate(HandleRef image, int count, int[] list);
            private static FunctionWrapper<GdipGetPropertyIdList_delegate> GdipGetPropertyIdList_ptr;
            internal static int GdipGetPropertyIdList(HandleRef image, int count, int[] list) => GdipGetPropertyIdList_ptr.Delegate(image, count, list);

            private delegate int GdipGetPropertyItemSize_delegate(HandleRef image, int propid, out int size);
            private static FunctionWrapper<GdipGetPropertyItemSize_delegate> GdipGetPropertyItemSize_ptr;
            internal static int GdipGetPropertyItemSize(HandleRef image, int propid, out int size) => GdipGetPropertyItemSize_ptr.Delegate(image, propid, out size);

            private delegate int GdipGetPropertyItem_delegate(HandleRef image, int propid, int size, IntPtr buffer);
            private static FunctionWrapper<GdipGetPropertyItem_delegate> GdipGetPropertyItem_ptr;
            internal static int GdipGetPropertyItem(HandleRef image, int propid, int size, IntPtr buffer) => GdipGetPropertyItem_ptr.Delegate(image, propid, size, buffer);

            private delegate int GdipGetPropertySize_delegate(HandleRef image, out int totalSize, ref int count);
            private static FunctionWrapper<GdipGetPropertySize_delegate> GdipGetPropertySize_ptr;
            internal static int GdipGetPropertySize(HandleRef image, out int totalSize, ref int count) => GdipGetPropertySize_ptr.Delegate(image, out totalSize, ref count);

            private delegate int GdipGetAllPropertyItems_delegate(HandleRef image, int totalSize, int count, IntPtr buffer);
            private static FunctionWrapper<GdipGetAllPropertyItems_delegate> GdipGetAllPropertyItems_ptr;
            internal static int GdipGetAllPropertyItems(HandleRef image, int totalSize, int count, IntPtr buffer) => GdipGetAllPropertyItems_ptr.Delegate(image, totalSize, count, buffer);

            private delegate int GdipRemovePropertyItem_delegate(HandleRef image, int propid);
            private static FunctionWrapper<GdipRemovePropertyItem_delegate> GdipRemovePropertyItem_ptr;
            internal static int GdipRemovePropertyItem(HandleRef image, int propid) => GdipRemovePropertyItem_ptr.Delegate(image, propid);

            private delegate int GdipSetPropertyItem_delegate(HandleRef image, PropertyItemInternal propitem);
            private static FunctionWrapper<GdipSetPropertyItem_delegate> GdipSetPropertyItem_ptr;
            internal static int GdipSetPropertyItem(HandleRef image, PropertyItemInternal propitem) => GdipSetPropertyItem_ptr.Delegate(image, propitem);

            private delegate int GdipImageForceValidation_delegate(HandleRef image);
            private static FunctionWrapper<GdipImageForceValidation_delegate> GdipImageForceValidation_ptr;
            internal static int GdipImageForceValidation(HandleRef image) => GdipImageForceValidation_ptr.Delegate(image);

            private delegate int GdipCreateBitmapFromStream_delegate(UnsafeNativeMethods.IStream stream, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromStream_delegate> GdipCreateBitmapFromStream_ptr;
            internal static int GdipCreateBitmapFromStream(UnsafeNativeMethods.IStream stream, out IntPtr bitmap) => GdipCreateBitmapFromStream_ptr.Delegate(stream, out bitmap);

            private delegate int GdipCreateBitmapFromFile_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromFile_delegate> GdipCreateBitmapFromFile_ptr;
            internal static int GdipCreateBitmapFromFile(string filename, out IntPtr bitmap) => GdipCreateBitmapFromFile_ptr.Delegate(filename, out bitmap);

            private delegate int GdipCreateBitmapFromStreamICM_delegate(UnsafeNativeMethods.IStream stream, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromStreamICM_delegate> GdipCreateBitmapFromStreamICM_ptr;
            internal static int GdipCreateBitmapFromStreamICM(UnsafeNativeMethods.IStream stream, out IntPtr bitmap) => GdipCreateBitmapFromStreamICM_ptr.Delegate(stream, out bitmap);

            private delegate int GdipCreateBitmapFromFileICM_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromFileICM_delegate> GdipCreateBitmapFromFileICM_ptr;
            internal static int GdipCreateBitmapFromFileICM(string filename, out IntPtr bitmap) => GdipCreateBitmapFromFileICM_ptr.Delegate(filename, out bitmap);

            private delegate int GdipCreateBitmapFromScan0_delegate(int width, int height, int stride, int format, HandleRef scan0, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromScan0_delegate> GdipCreateBitmapFromScan0_ptr;
            internal static int GdipCreateBitmapFromScan0(int width, int height, int stride, int format, HandleRef scan0, out IntPtr bitmap) => GdipCreateBitmapFromScan0_ptr.Delegate(width, height, stride, format, scan0, out bitmap);

            private delegate int GdipCreateBitmapFromGraphics_delegate(int width, int height, HandleRef graphics, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromGraphics_delegate> GdipCreateBitmapFromGraphics_ptr;
            internal static int GdipCreateBitmapFromGraphics(int width, int height, HandleRef graphics, out IntPtr bitmap) => GdipCreateBitmapFromGraphics_ptr.Delegate(width, height, graphics, out bitmap);

            private delegate int GdipCreateBitmapFromHBITMAP_delegate(HandleRef hbitmap, HandleRef hpalette, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromHBITMAP_delegate> GdipCreateBitmapFromHBITMAP_ptr;
            internal static int GdipCreateBitmapFromHBITMAP(HandleRef hbitmap, HandleRef hpalette, out IntPtr bitmap) => GdipCreateBitmapFromHBITMAP_ptr.Delegate(hbitmap, hpalette, out bitmap);

            private delegate int GdipCreateBitmapFromHICON_delegate(HandleRef hicon, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromHICON_delegate> GdipCreateBitmapFromHICON_ptr;
            internal static int GdipCreateBitmapFromHICON(HandleRef hicon, out IntPtr bitmap) => GdipCreateBitmapFromHICON_ptr.Delegate(hicon, out bitmap);

            private delegate int GdipCreateBitmapFromResource_delegate(HandleRef hresource, HandleRef name, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromResource_delegate> GdipCreateBitmapFromResource_ptr;
            internal static int GdipCreateBitmapFromResource(HandleRef hresource, HandleRef name, out IntPtr bitmap) => GdipCreateBitmapFromResource_ptr.Delegate(hresource, name, out bitmap);

            private delegate int GdipCreateHBITMAPFromBitmap_delegate(HandleRef nativeBitmap, out IntPtr hbitmap, int argbBackground);
            private static FunctionWrapper<GdipCreateHBITMAPFromBitmap_delegate> GdipCreateHBITMAPFromBitmap_ptr;
            internal static int GdipCreateHBITMAPFromBitmap(HandleRef nativeBitmap, out IntPtr hbitmap, int argbBackground) => GdipCreateHBITMAPFromBitmap_ptr.Delegate(nativeBitmap, out hbitmap, argbBackground);

            private delegate int GdipCreateHICONFromBitmap_delegate(HandleRef nativeBitmap, out IntPtr hicon);
            private static FunctionWrapper<GdipCreateHICONFromBitmap_delegate> GdipCreateHICONFromBitmap_ptr;
            internal static int GdipCreateHICONFromBitmap(HandleRef nativeBitmap, out IntPtr hicon) => GdipCreateHICONFromBitmap_ptr.Delegate(nativeBitmap, out hicon);

            private delegate int GdipCloneBitmapArea_delegate(float x, float y, float width, float height, int format, HandleRef srcbitmap, out IntPtr dstbitmap);
            private static FunctionWrapper<GdipCloneBitmapArea_delegate> GdipCloneBitmapArea_ptr;
            internal static int GdipCloneBitmapArea(float x, float y, float width, float height, int format, HandleRef srcbitmap, out IntPtr dstbitmap) => GdipCloneBitmapArea_ptr.Delegate(x, y, width, height, format, srcbitmap, out dstbitmap);

            private delegate int GdipCloneBitmapAreaI_delegate(int x, int y, int width, int height, int format, HandleRef srcbitmap, out IntPtr dstbitmap);
            private static FunctionWrapper<GdipCloneBitmapAreaI_delegate> GdipCloneBitmapAreaI_ptr;
            internal static int GdipCloneBitmapAreaI(int x, int y, int width, int height, int format, HandleRef srcbitmap, out IntPtr dstbitmap) => GdipCloneBitmapAreaI_ptr.Delegate(x, y, width, height, format, srcbitmap, out dstbitmap);

            private delegate int GdipBitmapLockBits_delegate(HandleRef bitmap, ref GPRECT rect, ImageLockMode flags, PixelFormat format, [In] [Out] BitmapData lockedBitmapData);
            private static FunctionWrapper<GdipBitmapLockBits_delegate> GdipBitmapLockBits_ptr;
            internal static int GdipBitmapLockBits(HandleRef bitmap, ref GPRECT rect, ImageLockMode flags, PixelFormat format, [In] [Out] BitmapData lockedBitmapData) => GdipBitmapLockBits_ptr.Delegate(bitmap, ref rect, flags, format, lockedBitmapData);

            private delegate int GdipBitmapUnlockBits_delegate(HandleRef bitmap, BitmapData lockedBitmapData);
            private static FunctionWrapper<GdipBitmapUnlockBits_delegate> GdipBitmapUnlockBits_ptr;
            internal static int GdipBitmapUnlockBits(HandleRef bitmap, BitmapData lockedBitmapData) => GdipBitmapUnlockBits_ptr.Delegate(bitmap, lockedBitmapData);

            private delegate int GdipBitmapGetPixel_delegate(HandleRef bitmap, int x, int y, out int argb);
            private static FunctionWrapper<GdipBitmapGetPixel_delegate> GdipBitmapGetPixel_ptr;
            internal static int GdipBitmapGetPixel(HandleRef bitmap, int x, int y, out int argb) => GdipBitmapGetPixel_ptr.Delegate(bitmap, x, y, out argb);

            private delegate int GdipBitmapSetPixel_delegate(HandleRef bitmap, int x, int y, int argb);
            private static FunctionWrapper<GdipBitmapSetPixel_delegate> GdipBitmapSetPixel_ptr;
            internal static int GdipBitmapSetPixel(HandleRef bitmap, int x, int y, int argb) => GdipBitmapSetPixel_ptr.Delegate(bitmap, x, y, argb);

            private delegate int GdipBitmapSetResolution_delegate(HandleRef bitmap, float dpix, float dpiy);
            private static FunctionWrapper<GdipBitmapSetResolution_delegate> GdipBitmapSetResolution_ptr;
            internal static int GdipBitmapSetResolution(HandleRef bitmap, float dpix, float dpiy) => GdipBitmapSetResolution_ptr.Delegate(bitmap, dpix, dpiy);



            private delegate int GdipFlush_delegate(HandleRef graphics, FlushIntention intention);
            private static FunctionWrapper<GdipFlush_delegate> GdipFlush_ptr;
            internal static int GdipFlush(HandleRef graphics, FlushIntention intention) => GdipFlush_ptr.Delegate(graphics, intention);

            private delegate int GdipCreateFromHDC_delegate(HandleRef hdc, out IntPtr graphics);
            private static FunctionWrapper<GdipCreateFromHDC_delegate> GdipCreateFromHDC_ptr;
            internal static int GdipCreateFromHDC(HandleRef hdc, out IntPtr graphics) => GdipCreateFromHDC_ptr.Delegate(hdc, out graphics);

            private delegate int GdipCreateFromHDC2_delegate(HandleRef hdc, HandleRef hdevice, out IntPtr graphics);
            private static FunctionWrapper<GdipCreateFromHDC2_delegate> GdipCreateFromHDC2_ptr;
            internal static int GdipCreateFromHDC2(HandleRef hdc, HandleRef hdevice, out IntPtr graphics) => GdipCreateFromHDC2_ptr.Delegate(hdc, hdevice, out graphics);

            private delegate int GdipCreateFromHWND_delegate(HandleRef hwnd, out IntPtr graphics);
            private static FunctionWrapper<GdipCreateFromHWND_delegate> GdipCreateFromHWND_ptr;
            internal static int GdipCreateFromHWND(HandleRef hwnd, out IntPtr graphics) => GdipCreateFromHWND_ptr.Delegate(hwnd, out graphics);

            private delegate int GdipDeleteGraphics_delegate(HandleRef graphics);
            private static FunctionWrapper<GdipDeleteGraphics_delegate> GdipDeleteGraphics_ptr;
            internal static int IntGdipDeleteGraphics(HandleRef graphics) => GdipDeleteGraphics_ptr.Delegate(graphics);

            private delegate int GdipGetDC_delegate(HandleRef graphics, out IntPtr hdc);
            private static FunctionWrapper<GdipGetDC_delegate> GdipGetDC_ptr;
            internal static int GdipGetDC(HandleRef graphics, out IntPtr hdc) => GdipGetDC_ptr.Delegate(graphics, out hdc);

            private delegate int GdipReleaseDC_delegate(HandleRef graphics, HandleRef hdc);
            private static FunctionWrapper<GdipReleaseDC_delegate> GdipReleaseDC_ptr;
            internal static int IntGdipReleaseDC(HandleRef graphics, HandleRef hdc) => GdipReleaseDC_ptr.Delegate(graphics, hdc);

            private delegate int GdipSetCompositingMode_delegate(HandleRef graphics, int compositeMode);
            private static FunctionWrapper<GdipSetCompositingMode_delegate> GdipSetCompositingMode_ptr;
            internal static int GdipSetCompositingMode(HandleRef graphics, int compositeMode) => GdipSetCompositingMode_ptr.Delegate(graphics, compositeMode);

            private delegate int GdipSetTextRenderingHint_delegate(HandleRef graphics, TextRenderingHint textRenderingHint);
            private static FunctionWrapper<GdipSetTextRenderingHint_delegate> GdipSetTextRenderingHint_ptr;
            internal static int GdipSetTextRenderingHint(HandleRef graphics, TextRenderingHint textRenderingHint) => GdipSetTextRenderingHint_ptr.Delegate(graphics, textRenderingHint);

            private delegate int GdipSetTextContrast_delegate(HandleRef graphics, int textContrast);
            private static FunctionWrapper<GdipSetTextContrast_delegate> GdipSetTextContrast_ptr;
            internal static int GdipSetTextContrast(HandleRef graphics, int textContrast) => GdipSetTextContrast_ptr.Delegate(graphics, textContrast);

            private delegate int GdipSetInterpolationMode_delegate(HandleRef graphics, int mode);
            private static FunctionWrapper<GdipSetInterpolationMode_delegate> GdipSetInterpolationMode_ptr;
            internal static int GdipSetInterpolationMode(HandleRef graphics, int mode) => GdipSetInterpolationMode_ptr.Delegate(graphics, mode);

            private delegate int GdipGetCompositingMode_delegate(HandleRef graphics, out int compositeMode);
            private static FunctionWrapper<GdipGetCompositingMode_delegate> GdipGetCompositingMode_ptr;
            internal static int GdipGetCompositingMode(HandleRef graphics, out int compositeMode) => GdipGetCompositingMode_ptr.Delegate(graphics, out compositeMode);

            private delegate int GdipSetRenderingOrigin_delegate(HandleRef graphics, int x, int y);
            private static FunctionWrapper<GdipSetRenderingOrigin_delegate> GdipSetRenderingOrigin_ptr;
            internal static int GdipSetRenderingOrigin(HandleRef graphics, int x, int y) => GdipSetRenderingOrigin_ptr.Delegate(graphics, x, y);

            private delegate int GdipGetRenderingOrigin_delegate(HandleRef graphics, out int x, out int y);
            private static FunctionWrapper<GdipGetRenderingOrigin_delegate> GdipGetRenderingOrigin_ptr;
            internal static int GdipGetRenderingOrigin(HandleRef graphics, out int x, out int y) => GdipGetRenderingOrigin_ptr.Delegate(graphics, out x, out y);

            private delegate int GdipSetCompositingQuality_delegate(HandleRef graphics, CompositingQuality quality);
            private static FunctionWrapper<GdipSetCompositingQuality_delegate> GdipSetCompositingQuality_ptr;
            internal static int GdipSetCompositingQuality(HandleRef graphics, CompositingQuality quality) => GdipSetCompositingQuality_ptr.Delegate(graphics, quality);

            private delegate int GdipGetCompositingQuality_delegate(HandleRef graphics, out CompositingQuality quality);
            private static FunctionWrapper<GdipGetCompositingQuality_delegate> GdipGetCompositingQuality_ptr;
            internal static int GdipGetCompositingQuality(HandleRef graphics, out CompositingQuality quality) => GdipGetCompositingQuality_ptr.Delegate(graphics, out quality);

            private delegate int GdipSetSmoothingMode_delegate(HandleRef graphics, SmoothingMode smoothingMode);
            private static FunctionWrapper<GdipSetSmoothingMode_delegate> GdipSetSmoothingMode_ptr;
            internal static int GdipSetSmoothingMode(HandleRef graphics, SmoothingMode smoothingMode) => GdipSetSmoothingMode_ptr.Delegate(graphics, smoothingMode);

            private delegate int GdipGetSmoothingMode_delegate(HandleRef graphics, out SmoothingMode smoothingMode);
            private static FunctionWrapper<GdipGetSmoothingMode_delegate> GdipGetSmoothingMode_ptr;
            internal static int GdipGetSmoothingMode(HandleRef graphics, out SmoothingMode smoothingMode) => GdipGetSmoothingMode_ptr.Delegate(graphics, out smoothingMode);

            private delegate int GdipSetPixelOffsetMode_delegate(HandleRef graphics, PixelOffsetMode pixelOffsetMode);
            private static FunctionWrapper<GdipSetPixelOffsetMode_delegate> GdipSetPixelOffsetMode_ptr;
            internal static int GdipSetPixelOffsetMode(HandleRef graphics, PixelOffsetMode pixelOffsetMode) => GdipSetPixelOffsetMode_ptr.Delegate(graphics, pixelOffsetMode);

            private delegate int GdipGetPixelOffsetMode_delegate(HandleRef graphics, out PixelOffsetMode pixelOffsetMode);
            private static FunctionWrapper<GdipGetPixelOffsetMode_delegate> GdipGetPixelOffsetMode_ptr;
            internal static int GdipGetPixelOffsetMode(HandleRef graphics, out PixelOffsetMode pixelOffsetMode) => GdipGetPixelOffsetMode_ptr.Delegate(graphics, out pixelOffsetMode);

            private delegate int GdipGetTextRenderingHint_delegate(HandleRef graphics, out TextRenderingHint textRenderingHint);
            private static FunctionWrapper<GdipGetTextRenderingHint_delegate> GdipGetTextRenderingHint_ptr;
            internal static int GdipGetTextRenderingHint(HandleRef graphics, out TextRenderingHint textRenderingHint) => GdipGetTextRenderingHint_ptr.Delegate(graphics, out textRenderingHint);

            private delegate int GdipGetTextContrast_delegate(HandleRef graphics, out int textContrast);
            private static FunctionWrapper<GdipGetTextContrast_delegate> GdipGetTextContrast_ptr;
            internal static int GdipGetTextContrast(HandleRef graphics, out int textContrast) => GdipGetTextContrast_ptr.Delegate(graphics, out textContrast);

            private delegate int GdipGetInterpolationMode_delegate(HandleRef graphics, out int mode);
            private static FunctionWrapper<GdipGetInterpolationMode_delegate> GdipGetInterpolationMode_ptr;
            internal static int GdipGetInterpolationMode(HandleRef graphics, out int mode) => GdipGetInterpolationMode_ptr.Delegate(graphics, out mode);

            private delegate int GdipGetPageUnit_delegate(HandleRef graphics, out int unit);
            private static FunctionWrapper<GdipGetPageUnit_delegate> GdipGetPageUnit_ptr;
            internal static int GdipGetPageUnit(HandleRef graphics, out int unit) => GdipGetPageUnit_ptr.Delegate(graphics, out unit);

            private delegate int GdipGetPageScale_delegate(HandleRef graphics, float[] scale);
            private static FunctionWrapper<GdipGetPageScale_delegate> GdipGetPageScale_ptr;
            internal static int GdipGetPageScale(HandleRef graphics, float[] scale) => GdipGetPageScale_ptr.Delegate(graphics, scale);

            private delegate int GdipSetPageUnit_delegate(HandleRef graphics, int unit);
            private static FunctionWrapper<GdipSetPageUnit_delegate> GdipSetPageUnit_ptr;
            internal static int GdipSetPageUnit(HandleRef graphics, int unit) => GdipSetPageUnit_ptr.Delegate(graphics, unit);

            private delegate int GdipSetPageScale_delegate(HandleRef graphics, float scale);
            private static FunctionWrapper<GdipSetPageScale_delegate> GdipSetPageScale_ptr;
            internal static int GdipSetPageScale(HandleRef graphics, float scale) => GdipSetPageScale_ptr.Delegate(graphics, scale);

            private delegate int GdipGetDpiX_delegate(HandleRef graphics, float[] dpi);
            private static FunctionWrapper<GdipGetDpiX_delegate> GdipGetDpiX_ptr;
            internal static int GdipGetDpiX(HandleRef graphics, float[] dpi) => GdipGetDpiX_ptr.Delegate(graphics, dpi);

            private delegate int GdipGetDpiY_delegate(HandleRef graphics, float[] dpi);
            private static FunctionWrapper<GdipGetDpiY_delegate> GdipGetDpiY_ptr;
            internal static int GdipGetDpiY(HandleRef graphics, float[] dpi) => GdipGetDpiY_ptr.Delegate(graphics, dpi);

            private delegate int GdipTransformPoints_delegate(HandleRef graphics, int destSpace, int srcSpace, IntPtr points, int count);
            private static FunctionWrapper<GdipTransformPoints_delegate> GdipTransformPoints_ptr;
            internal static int GdipTransformPoints(HandleRef graphics, int destSpace, int srcSpace, IntPtr points, int count) => GdipTransformPoints_ptr.Delegate(graphics, destSpace, srcSpace, points, count);

            private delegate int GdipTransformPointsI_delegate(HandleRef graphics, int destSpace, int srcSpace, IntPtr points, int count);
            private static FunctionWrapper<GdipTransformPointsI_delegate> GdipTransformPointsI_ptr;
            internal static int GdipTransformPointsI(HandleRef graphics, int destSpace, int srcSpace, IntPtr points, int count) => GdipTransformPointsI_ptr.Delegate(graphics, destSpace, srcSpace, points, count);

            private delegate int GdipGetNearestColor_delegate(HandleRef graphics, ref int color);
            private static FunctionWrapper<GdipGetNearestColor_delegate> GdipGetNearestColor_ptr;
            internal static int GdipGetNearestColor(HandleRef graphics, ref int color) => GdipGetNearestColor_ptr.Delegate(graphics, ref color);

            private delegate IntPtr GdipCreateHalftonePalette_delegate();
            private static FunctionWrapper<GdipCreateHalftonePalette_delegate> GdipCreateHalftonePalette_ptr;
            internal static IntPtr GdipCreateHalftonePalette() => GdipCreateHalftonePalette_ptr.Delegate();

            private delegate int GdipDrawLine_delegate(HandleRef graphics, HandleRef pen, float x1, float y1, float x2, float y2);
            private static FunctionWrapper<GdipDrawLine_delegate> GdipDrawLine_ptr;
            internal static int GdipDrawLine(HandleRef graphics, HandleRef pen, float x1, float y1, float x2, float y2) => GdipDrawLine_ptr.Delegate(graphics, pen, x1, y1, x2, y2);

            private delegate int GdipDrawLineI_delegate(HandleRef graphics, HandleRef pen, int x1, int y1, int x2, int y2);
            private static FunctionWrapper<GdipDrawLineI_delegate> GdipDrawLineI_ptr;
            internal static int GdipDrawLineI(HandleRef graphics, HandleRef pen, int x1, int y1, int x2, int y2) => GdipDrawLineI_ptr.Delegate(graphics, pen, x1, y1, x2, y2);

            private delegate int GdipDrawLines_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count);
            private static FunctionWrapper<GdipDrawLines_delegate> GdipDrawLines_ptr;
            internal static int GdipDrawLines(HandleRef graphics, HandleRef pen, HandleRef points, int count) => GdipDrawLines_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawLinesI_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count);
            private static FunctionWrapper<GdipDrawLinesI_delegate> GdipDrawLinesI_ptr;
            internal static int GdipDrawLinesI(HandleRef graphics, HandleRef pen, HandleRef points, int count) => GdipDrawLinesI_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawArc_delegate(HandleRef graphics, HandleRef pen, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawArc_delegate> GdipDrawArc_ptr;
            internal static int GdipDrawArc(HandleRef graphics, HandleRef pen, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipDrawArc_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipDrawArcI_delegate(HandleRef graphics, HandleRef pen, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawArcI_delegate> GdipDrawArcI_ptr;
            internal static int GdipDrawArcI(HandleRef graphics, HandleRef pen, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipDrawArcI_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipDrawBezier_delegate(HandleRef graphics, HandleRef pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);
            private static FunctionWrapper<GdipDrawBezier_delegate> GdipDrawBezier_ptr;
            internal static int GdipDrawBezier(HandleRef graphics, HandleRef pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) => GdipDrawBezier_ptr.Delegate(graphics, pen, x1, y1, x2, y2, x3, y3, x4, y4);

            private delegate int GdipDrawBeziers_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count);
            private static FunctionWrapper<GdipDrawBeziers_delegate> GdipDrawBeziers_ptr;
            internal static int GdipDrawBeziers(HandleRef graphics, HandleRef pen, HandleRef points, int count) => GdipDrawBeziers_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawBeziersI_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count);
            private static FunctionWrapper<GdipDrawBeziersI_delegate> GdipDrawBeziersI_ptr;
            internal static int GdipDrawBeziersI(HandleRef graphics, HandleRef pen, HandleRef points, int count) => GdipDrawBeziersI_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawRectangle_delegate(HandleRef graphics, HandleRef pen, float x, float y, float width, float height);
            private static FunctionWrapper<GdipDrawRectangle_delegate> GdipDrawRectangle_ptr;
            internal static int GdipDrawRectangle(HandleRef graphics, HandleRef pen, float x, float y, float width, float height) => GdipDrawRectangle_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate int GdipDrawRectangleI_delegate(HandleRef graphics, HandleRef pen, int x, int y, int width, int height);
            private static FunctionWrapper<GdipDrawRectangleI_delegate> GdipDrawRectangleI_ptr;
            internal static int GdipDrawRectangleI(HandleRef graphics, HandleRef pen, int x, int y, int width, int height) => GdipDrawRectangleI_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate int GdipDrawRectangles_delegate(HandleRef graphics, HandleRef pen, HandleRef rects, int count);
            private static FunctionWrapper<GdipDrawRectangles_delegate> GdipDrawRectangles_ptr;
            internal static int GdipDrawRectangles(HandleRef graphics, HandleRef pen, HandleRef rects, int count) => GdipDrawRectangles_ptr.Delegate(graphics, pen, rects, count);

            private delegate int GdipDrawRectanglesI_delegate(HandleRef graphics, HandleRef pen, HandleRef rects, int count);
            private static FunctionWrapper<GdipDrawRectanglesI_delegate> GdipDrawRectanglesI_ptr;
            internal static int GdipDrawRectanglesI(HandleRef graphics, HandleRef pen, HandleRef rects, int count) => GdipDrawRectanglesI_ptr.Delegate(graphics, pen, rects, count);

            private delegate int GdipDrawEllipse_delegate(HandleRef graphics, HandleRef pen, float x, float y, float width, float height);
            private static FunctionWrapper<GdipDrawEllipse_delegate> GdipDrawEllipse_ptr;
            internal static int GdipDrawEllipse(HandleRef graphics, HandleRef pen, float x, float y, float width, float height) => GdipDrawEllipse_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate int GdipDrawEllipseI_delegate(HandleRef graphics, HandleRef pen, int x, int y, int width, int height);
            private static FunctionWrapper<GdipDrawEllipseI_delegate> GdipDrawEllipseI_ptr;
            internal static int GdipDrawEllipseI(HandleRef graphics, HandleRef pen, int x, int y, int width, int height) => GdipDrawEllipseI_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate int GdipDrawPie_delegate(HandleRef graphics, HandleRef pen, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawPie_delegate> GdipDrawPie_ptr;
            internal static int GdipDrawPie(HandleRef graphics, HandleRef pen, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipDrawPie_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipDrawPieI_delegate(HandleRef graphics, HandleRef pen, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawPieI_delegate> GdipDrawPieI_ptr;
            internal static int GdipDrawPieI(HandleRef graphics, HandleRef pen, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipDrawPieI_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipDrawPolygon_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count);
            private static FunctionWrapper<GdipDrawPolygon_delegate> GdipDrawPolygon_ptr;
            internal static int GdipDrawPolygon(HandleRef graphics, HandleRef pen, HandleRef points, int count) => GdipDrawPolygon_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawPolygonI_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count);
            private static FunctionWrapper<GdipDrawPolygonI_delegate> GdipDrawPolygonI_ptr;
            internal static int GdipDrawPolygonI(HandleRef graphics, HandleRef pen, HandleRef points, int count) => GdipDrawPolygonI_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawPath_delegate(HandleRef graphics, HandleRef pen, HandleRef path);
            private static FunctionWrapper<GdipDrawPath_delegate> GdipDrawPath_ptr;
            internal static int GdipDrawPath(HandleRef graphics, HandleRef pen, HandleRef path) => GdipDrawPath_ptr.Delegate(graphics, pen, path);

            private delegate int GdipDrawCurve_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count);
            private static FunctionWrapper<GdipDrawCurve_delegate> GdipDrawCurve_ptr;
            internal static int GdipDrawCurve(HandleRef graphics, HandleRef pen, HandleRef points, int count) => GdipDrawCurve_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawCurveI_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count);
            private static FunctionWrapper<GdipDrawCurveI_delegate> GdipDrawCurveI_ptr;
            internal static int GdipDrawCurveI(HandleRef graphics, HandleRef pen, HandleRef points, int count) => GdipDrawCurveI_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawCurve2_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count, float tension);
            private static FunctionWrapper<GdipDrawCurve2_delegate> GdipDrawCurve2_ptr;
            internal static int GdipDrawCurve2(HandleRef graphics, HandleRef pen, HandleRef points, int count, float tension) => GdipDrawCurve2_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate int GdipDrawCurve2I_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count, float tension);
            private static FunctionWrapper<GdipDrawCurve2I_delegate> GdipDrawCurve2I_ptr;
            internal static int GdipDrawCurve2I(HandleRef graphics, HandleRef pen, HandleRef points, int count, float tension) => GdipDrawCurve2I_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate int GdipDrawCurve3_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipDrawCurve3_delegate> GdipDrawCurve3_ptr;
            internal static int GdipDrawCurve3(HandleRef graphics, HandleRef pen, HandleRef points, int count, int offset, int numberOfSegments, float tension) => GdipDrawCurve3_ptr.Delegate(graphics, pen, points, count, offset, numberOfSegments, tension);

            private delegate int GdipDrawCurve3I_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipDrawCurve3I_delegate> GdipDrawCurve3I_ptr;
            internal static int GdipDrawCurve3I(HandleRef graphics, HandleRef pen, HandleRef points, int count, int offset, int numberOfSegments, float tension) => GdipDrawCurve3I_ptr.Delegate(graphics, pen, points, count, offset, numberOfSegments, tension);

            private delegate int GdipDrawClosedCurve_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count);
            private static FunctionWrapper<GdipDrawClosedCurve_delegate> GdipDrawClosedCurve_ptr;
            internal static int GdipDrawClosedCurve(HandleRef graphics, HandleRef pen, HandleRef points, int count) => GdipDrawClosedCurve_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawClosedCurveI_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count);
            private static FunctionWrapper<GdipDrawClosedCurveI_delegate> GdipDrawClosedCurveI_ptr;
            internal static int GdipDrawClosedCurveI(HandleRef graphics, HandleRef pen, HandleRef points, int count) => GdipDrawClosedCurveI_ptr.Delegate(graphics, pen, points, count);

            private delegate int GdipDrawClosedCurve2_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count, float tension);
            private static FunctionWrapper<GdipDrawClosedCurve2_delegate> GdipDrawClosedCurve2_ptr;
            internal static int GdipDrawClosedCurve2(HandleRef graphics, HandleRef pen, HandleRef points, int count, float tension) => GdipDrawClosedCurve2_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate int GdipDrawClosedCurve2I_delegate(HandleRef graphics, HandleRef pen, HandleRef points, int count, float tension);
            private static FunctionWrapper<GdipDrawClosedCurve2I_delegate> GdipDrawClosedCurve2I_ptr;
            internal static int GdipDrawClosedCurve2I(HandleRef graphics, HandleRef pen, HandleRef points, int count, float tension) => GdipDrawClosedCurve2I_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate int GdipGraphicsClear_delegate(HandleRef graphics, int argb);
            private static FunctionWrapper<GdipGraphicsClear_delegate> GdipGraphicsClear_ptr;
            internal static int GdipGraphicsClear(HandleRef graphics, int argb) => GdipGraphicsClear_ptr.Delegate(graphics, argb);

            private delegate int GdipFillRectangle_delegate(HandleRef graphics, HandleRef brush, float x, float y, float width, float height);
            private static FunctionWrapper<GdipFillRectangle_delegate> GdipFillRectangle_ptr;
            internal static int GdipFillRectangle(HandleRef graphics, HandleRef brush, float x, float y, float width, float height) => GdipFillRectangle_ptr.Delegate(graphics, brush, x, y, width, height);

            private delegate int GdipFillRectangleI_delegate(HandleRef graphics, HandleRef brush, int x, int y, int width, int height);
            private static FunctionWrapper<GdipFillRectangleI_delegate> GdipFillRectangleI_ptr;
            internal static int GdipFillRectangleI(HandleRef graphics, HandleRef brush, int x, int y, int width, int height) => GdipFillRectangleI_ptr.Delegate(graphics, brush, x, y, width, height);

            private delegate int GdipFillRectangles_delegate(HandleRef graphics, HandleRef brush, HandleRef rects, int count);
            private static FunctionWrapper<GdipFillRectangles_delegate> GdipFillRectangles_ptr;
            internal static int GdipFillRectangles(HandleRef graphics, HandleRef brush, HandleRef rects, int count) => GdipFillRectangles_ptr.Delegate(graphics, brush, rects, count);

            private delegate int GdipFillRectanglesI_delegate(HandleRef graphics, HandleRef brush, HandleRef rects, int count);
            private static FunctionWrapper<GdipFillRectanglesI_delegate> GdipFillRectanglesI_ptr;
            internal static int GdipFillRectanglesI(HandleRef graphics, HandleRef brush, HandleRef rects, int count) => GdipFillRectanglesI_ptr.Delegate(graphics, brush, rects, count);

            private delegate int GdipFillPolygon_delegate(HandleRef graphics, HandleRef brush, HandleRef points, int count, int brushMode);
            private static FunctionWrapper<GdipFillPolygon_delegate> GdipFillPolygon_ptr;
            internal static int GdipFillPolygon(HandleRef graphics, HandleRef brush, HandleRef points, int count, int brushMode) => GdipFillPolygon_ptr.Delegate(graphics, brush, points, count, brushMode);

            private delegate int GdipFillPolygonI_delegate(HandleRef graphics, HandleRef brush, HandleRef points, int count, int brushMode);
            private static FunctionWrapper<GdipFillPolygonI_delegate> GdipFillPolygonI_ptr;
            internal static int GdipFillPolygonI(HandleRef graphics, HandleRef brush, HandleRef points, int count, int brushMode) => GdipFillPolygonI_ptr.Delegate(graphics, brush, points, count, brushMode);

            private delegate int GdipFillEllipse_delegate(HandleRef graphics, HandleRef brush, float x, float y, float width, float height);
            private static FunctionWrapper<GdipFillEllipse_delegate> GdipFillEllipse_ptr;
            internal static int GdipFillEllipse(HandleRef graphics, HandleRef brush, float x, float y, float width, float height) => GdipFillEllipse_ptr.Delegate(graphics, brush, x, y, width, height);

            private delegate int GdipFillEllipseI_delegate(HandleRef graphics, HandleRef brush, int x, int y, int width, int height);
            private static FunctionWrapper<GdipFillEllipseI_delegate> GdipFillEllipseI_ptr;
            internal static int GdipFillEllipseI(HandleRef graphics, HandleRef brush, int x, int y, int width, int height) => GdipFillEllipseI_ptr.Delegate(graphics, brush, x, y, width, height);

            private delegate int GdipFillPie_delegate(HandleRef graphics, HandleRef brush, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipFillPie_delegate> GdipFillPie_ptr;
            internal static int GdipFillPie(HandleRef graphics, HandleRef brush, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipFillPie_ptr.Delegate(graphics, brush, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipFillPieI_delegate(HandleRef graphics, HandleRef brush, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipFillPieI_delegate> GdipFillPieI_ptr;
            internal static int GdipFillPieI(HandleRef graphics, HandleRef brush, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipFillPieI_ptr.Delegate(graphics, brush, x, y, width, height, startAngle, sweepAngle);

            private delegate int GdipFillPath_delegate(HandleRef graphics, HandleRef brush, HandleRef path);
            private static FunctionWrapper<GdipFillPath_delegate> GdipFillPath_ptr;
            internal static int GdipFillPath(HandleRef graphics, HandleRef brush, HandleRef path) => GdipFillPath_ptr.Delegate(graphics, brush, path);

            private delegate int GdipFillClosedCurve_delegate(HandleRef graphics, HandleRef brush, HandleRef points, int count);
            private static FunctionWrapper<GdipFillClosedCurve_delegate> GdipFillClosedCurve_ptr;
            internal static int GdipFillClosedCurve(HandleRef graphics, HandleRef brush, HandleRef points, int count) => GdipFillClosedCurve_ptr.Delegate(graphics, brush, points, count);

            private delegate int GdipFillClosedCurveI_delegate(HandleRef graphics, HandleRef brush, HandleRef points, int count);
            private static FunctionWrapper<GdipFillClosedCurveI_delegate> GdipFillClosedCurveI_ptr;
            internal static int GdipFillClosedCurveI(HandleRef graphics, HandleRef brush, HandleRef points, int count) => GdipFillClosedCurveI_ptr.Delegate(graphics, brush, points, count);

            private delegate int GdipFillClosedCurve2_delegate(HandleRef graphics, HandleRef brush, HandleRef points, int count, float tension, int mode);
            private static FunctionWrapper<GdipFillClosedCurve2_delegate> GdipFillClosedCurve2_ptr;
            internal static int GdipFillClosedCurve2(HandleRef graphics, HandleRef brush, HandleRef points, int count, float tension, int mode) => GdipFillClosedCurve2_ptr.Delegate(graphics, brush, points, count, tension, mode);

            private delegate int GdipFillClosedCurve2I_delegate(HandleRef graphics, HandleRef brush, HandleRef points, int count, float tension, int mode);
            private static FunctionWrapper<GdipFillClosedCurve2I_delegate> GdipFillClosedCurve2I_ptr;
            internal static int GdipFillClosedCurve2I(HandleRef graphics, HandleRef brush, HandleRef points, int count, float tension, int mode) => GdipFillClosedCurve2I_ptr.Delegate(graphics, brush, points, count, tension, mode);

            private delegate int GdipDrawImage_delegate(HandleRef graphics, HandleRef image, float x, float y);
            private static FunctionWrapper<GdipDrawImage_delegate> GdipDrawImage_ptr;
            internal static int GdipDrawImage(HandleRef graphics, HandleRef image, float x, float y) => GdipDrawImage_ptr.Delegate(graphics, image, x, y);

            private delegate int GdipDrawImageI_delegate(HandleRef graphics, HandleRef image, int x, int y);
            private static FunctionWrapper<GdipDrawImageI_delegate> GdipDrawImageI_ptr;
            internal static int GdipDrawImageI(HandleRef graphics, HandleRef image, int x, int y) => GdipDrawImageI_ptr.Delegate(graphics, image, x, y);

            private delegate int GdipDrawImageRect_delegate(HandleRef graphics, HandleRef image, float x, float y, float width, float height);
            private static FunctionWrapper<GdipDrawImageRect_delegate> GdipDrawImageRect_ptr;
            internal static int GdipDrawImageRect(HandleRef graphics, HandleRef image, float x, float y, float width, float height) => GdipDrawImageRect_ptr.Delegate(graphics, image, x, y, width, height);

            private delegate int GdipDrawImageRectI_delegate(HandleRef graphics, HandleRef image, int x, int y, int width, int height);
            private static FunctionWrapper<GdipDrawImageRectI_delegate> GdipDrawImageRectI_ptr;
            internal static int GdipDrawImageRectI(HandleRef graphics, HandleRef image, int x, int y, int width, int height) => GdipDrawImageRectI_ptr.Delegate(graphics, image, x, y, width, height);

            private delegate int GdipDrawImagePoints_delegate(HandleRef graphics, HandleRef image, HandleRef points, int count);
            private static FunctionWrapper<GdipDrawImagePoints_delegate> GdipDrawImagePoints_ptr;
            internal static int GdipDrawImagePoints(HandleRef graphics, HandleRef image, HandleRef points, int count) => GdipDrawImagePoints_ptr.Delegate(graphics, image, points, count);

            private delegate int GdipDrawImagePointsI_delegate(HandleRef graphics, HandleRef image, HandleRef points, int count);
            private static FunctionWrapper<GdipDrawImagePointsI_delegate> GdipDrawImagePointsI_ptr;
            internal static int GdipDrawImagePointsI(HandleRef graphics, HandleRef image, HandleRef points, int count) => GdipDrawImagePointsI_ptr.Delegate(graphics, image, points, count);

            private delegate int GdipDrawImagePointRect_delegate(HandleRef graphics, HandleRef image, float x, float y, float srcx, float srcy, float srcwidth, float srcheight, int srcunit);
            private static FunctionWrapper<GdipDrawImagePointRect_delegate> GdipDrawImagePointRect_ptr;
            internal static int GdipDrawImagePointRect(HandleRef graphics, HandleRef image, float x, float y, float srcx, float srcy, float srcwidth, float srcheight, int srcunit) => GdipDrawImagePointRect_ptr.Delegate(graphics, image, x, y, srcx, srcy, srcwidth, srcheight, srcunit);

            private delegate int GdipDrawImagePointRectI_delegate(HandleRef graphics, HandleRef image, int x, int y, int srcx, int srcy, int srcwidth, int srcheight, int srcunit);
            private static FunctionWrapper<GdipDrawImagePointRectI_delegate> GdipDrawImagePointRectI_ptr;
            internal static int GdipDrawImagePointRectI(HandleRef graphics, HandleRef image, int x, int y, int srcx, int srcy, int srcwidth, int srcheight, int srcunit) => GdipDrawImagePointRectI_ptr.Delegate(graphics, image, x, y, srcx, srcy, srcwidth, srcheight, srcunit);

            private delegate int GdipDrawImageRectRect_delegate(HandleRef graphics, HandleRef image, float dstx, float dsty, float dstwidth, float dstheight, float srcx, float srcy, float srcwidth, float srcheight, int srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort callback, HandleRef callbackdata);
            private static FunctionWrapper<GdipDrawImageRectRect_delegate> GdipDrawImageRectRect_ptr;
            internal static int GdipDrawImageRectRect(HandleRef graphics, HandleRef image, float dstx, float dsty, float dstwidth, float dstheight, float srcx, float srcy, float srcwidth, float srcheight, int srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort callback, HandleRef callbackdata) => GdipDrawImageRectRect_ptr.Delegate(graphics, image, dstx, dsty, dstwidth, dstheight, srcx, srcy, srcwidth, srcheight, srcunit, imageAttributes, callback, callbackdata);

            private delegate int GdipDrawImageRectRectI_delegate(HandleRef graphics, HandleRef image, int dstx, int dsty, int dstwidth, int dstheight, int srcx, int srcy, int srcwidth, int srcheight, int srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort callback, HandleRef callbackdata);
            private static FunctionWrapper<GdipDrawImageRectRectI_delegate> GdipDrawImageRectRectI_ptr;
            internal static int GdipDrawImageRectRectI(HandleRef graphics, HandleRef image, int dstx, int dsty, int dstwidth, int dstheight, int srcx, int srcy, int srcwidth, int srcheight, int srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort callback, HandleRef callbackdata) => GdipDrawImageRectRectI_ptr.Delegate(graphics, image, dstx, dsty, dstwidth, dstheight, srcx, srcy, srcwidth, srcheight, srcunit, imageAttributes, callback, callbackdata);

            private delegate int GdipDrawImagePointsRect_delegate(HandleRef graphics, HandleRef image, HandleRef points, int count, float srcx, float srcy, float srcwidth, float srcheight, int srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort callback, HandleRef callbackdata);
            private static FunctionWrapper<GdipDrawImagePointsRect_delegate> GdipDrawImagePointsRect_ptr;
            internal static int GdipDrawImagePointsRect(HandleRef graphics, HandleRef image, HandleRef points, int count, float srcx, float srcy, float srcwidth, float srcheight, int srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort callback, HandleRef callbackdata) => GdipDrawImagePointsRect_ptr.Delegate(graphics, image, points, count, srcx, srcy, srcwidth, srcheight, srcunit, imageAttributes, callback, callbackdata);

            private delegate int GdipDrawImagePointsRectI_delegate(HandleRef graphics, HandleRef image, HandleRef points, int count, int srcx, int srcy, int srcwidth, int srcheight, int srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort callback, HandleRef callbackdata);
            private static FunctionWrapper<GdipDrawImagePointsRectI_delegate> GdipDrawImagePointsRectI_ptr;
            internal static int GdipDrawImagePointsRectI(HandleRef graphics, HandleRef image, HandleRef points, int count, int srcx, int srcy, int srcwidth, int srcheight, int srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort callback, HandleRef callbackdata) => GdipDrawImagePointsRectI_ptr.Delegate(graphics, image, points, count, srcx, srcy, srcwidth, srcheight, srcunit, imageAttributes, callback, callbackdata);

            private delegate int GdipEnumerateMetafileDestPoint_delegate(HandleRef graphics, HandleRef metafile, GPPOINTF destPoint, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);
            private static FunctionWrapper<GdipEnumerateMetafileDestPoint_delegate> GdipEnumerateMetafileDestPoint_ptr;
            internal static int GdipEnumerateMetafileDestPoint(HandleRef graphics, HandleRef metafile, GPPOINTF destPoint, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes) => GdipEnumerateMetafileDestPoint_ptr.Delegate(graphics, metafile, destPoint, callback, callbackdata, imageattributes);

            private delegate int GdipEnumerateMetafileDestPointI_delegate(HandleRef graphics, HandleRef metafile, GPPOINT destPoint, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);
            private static FunctionWrapper<GdipEnumerateMetafileDestPointI_delegate> GdipEnumerateMetafileDestPointI_ptr;
            internal static int GdipEnumerateMetafileDestPointI(HandleRef graphics, HandleRef metafile, GPPOINT destPoint, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes) => GdipEnumerateMetafileDestPointI_ptr.Delegate(graphics, metafile, destPoint, callback, callbackdata, imageattributes);

            private delegate int GdipEnumerateMetafileDestRect_delegate(HandleRef graphics, HandleRef metafile, ref GPRECTF destRect, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);
            private static FunctionWrapper<GdipEnumerateMetafileDestRect_delegate> GdipEnumerateMetafileDestRect_ptr;
            internal static int GdipEnumerateMetafileDestRect(HandleRef graphics, HandleRef metafile, ref GPRECTF destRect, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes) => GdipEnumerateMetafileDestRect_ptr.Delegate(graphics, metafile, ref destRect, callback, callbackdata, imageattributes);

            private delegate int GdipEnumerateMetafileDestRectI_delegate(HandleRef graphics, HandleRef metafile, ref GPRECT destRect, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);
            private static FunctionWrapper<GdipEnumerateMetafileDestRectI_delegate> GdipEnumerateMetafileDestRectI_ptr;
            internal static int GdipEnumerateMetafileDestRectI(HandleRef graphics, HandleRef metafile, ref GPRECT destRect, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes) => GdipEnumerateMetafileDestRectI_ptr.Delegate(graphics, metafile, ref destRect, callback, callbackdata, imageattributes);

            private delegate int GdipEnumerateMetafileDestPoints_delegate(HandleRef graphics, HandleRef metafile, IntPtr destPoints, int count, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);
            private static FunctionWrapper<GdipEnumerateMetafileDestPoints_delegate> GdipEnumerateMetafileDestPoints_ptr;
            internal static int GdipEnumerateMetafileDestPoints(HandleRef graphics, HandleRef metafile, IntPtr destPoints, int count, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes) => GdipEnumerateMetafileDestPoints_ptr.Delegate(graphics, metafile, destPoints, count, callback, callbackdata, imageattributes);

            private delegate int GdipEnumerateMetafileDestPointsI_delegate(HandleRef graphics, HandleRef metafile, IntPtr destPoints, int count, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);
            private static FunctionWrapper<GdipEnumerateMetafileDestPointsI_delegate> GdipEnumerateMetafileDestPointsI_ptr;
            internal static int GdipEnumerateMetafileDestPointsI(HandleRef graphics, HandleRef metafile, IntPtr destPoints, int count, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes) => GdipEnumerateMetafileDestPointsI_ptr.Delegate(graphics, metafile, destPoints, count, callback, callbackdata, imageattributes);

            private delegate int GdipEnumerateMetafileSrcRectDestPoint_delegate(HandleRef graphics, HandleRef metafile, GPPOINTF destPoint, ref GPRECTF srcRect, int pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);
            private static FunctionWrapper<GdipEnumerateMetafileSrcRectDestPoint_delegate> GdipEnumerateMetafileSrcRectDestPoint_ptr;
            internal static int GdipEnumerateMetafileSrcRectDestPoint(HandleRef graphics, HandleRef metafile, GPPOINTF destPoint, ref GPRECTF srcRect, int pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes) => GdipEnumerateMetafileSrcRectDestPoint_ptr.Delegate(graphics, metafile, destPoint, ref srcRect, pageUnit, callback, callbackdata, imageattributes);

            private delegate int GdipEnumerateMetafileSrcRectDestPointI_delegate(HandleRef graphics, HandleRef metafile, GPPOINT destPoint, ref GPRECT srcRect, int pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);
            private static FunctionWrapper<GdipEnumerateMetafileSrcRectDestPointI_delegate> GdipEnumerateMetafileSrcRectDestPointI_ptr;
            internal static int GdipEnumerateMetafileSrcRectDestPointI(HandleRef graphics, HandleRef metafile, GPPOINT destPoint, ref GPRECT srcRect, int pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes) => GdipEnumerateMetafileSrcRectDestPointI_ptr.Delegate(graphics, metafile, destPoint, ref srcRect, pageUnit, callback, callbackdata, imageattributes);

            private delegate int GdipEnumerateMetafileSrcRectDestRect_delegate(HandleRef graphics, HandleRef metafile, ref GPRECTF destRect, ref GPRECTF srcRect, int pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);
            private static FunctionWrapper<GdipEnumerateMetafileSrcRectDestRect_delegate> GdipEnumerateMetafileSrcRectDestRect_ptr;
            internal static int GdipEnumerateMetafileSrcRectDestRect(HandleRef graphics, HandleRef metafile, ref GPRECTF destRect, ref GPRECTF srcRect, int pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes) => GdipEnumerateMetafileSrcRectDestRect_ptr.Delegate(graphics, metafile, ref destRect, ref srcRect, pageUnit, callback, callbackdata, imageattributes);

            private delegate int GdipEnumerateMetafileSrcRectDestRectI_delegate(HandleRef graphics, HandleRef metafile, ref GPRECT destRect, ref GPRECT srcRect, int pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);
            private static FunctionWrapper<GdipEnumerateMetafileSrcRectDestRectI_delegate> GdipEnumerateMetafileSrcRectDestRectI_ptr;
            internal static int GdipEnumerateMetafileSrcRectDestRectI(HandleRef graphics, HandleRef metafile, ref GPRECT destRect, ref GPRECT srcRect, int pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes) => GdipEnumerateMetafileSrcRectDestRectI_ptr.Delegate(graphics, metafile, ref destRect, ref srcRect, pageUnit, callback, callbackdata, imageattributes);

            private delegate int GdipEnumerateMetafileSrcRectDestPoints_delegate(HandleRef graphics, HandleRef metafile, IntPtr destPoints, int count, ref GPRECTF srcRect, int pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);
            private static FunctionWrapper<GdipEnumerateMetafileSrcRectDestPoints_delegate> GdipEnumerateMetafileSrcRectDestPoints_ptr;
            internal static int GdipEnumerateMetafileSrcRectDestPoints(HandleRef graphics, HandleRef metafile, IntPtr destPoints, int count, ref GPRECTF srcRect, int pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes) => GdipEnumerateMetafileSrcRectDestPoints_ptr.Delegate(graphics, metafile, destPoints, count, ref srcRect, pageUnit, callback, callbackdata, imageattributes);

            private delegate int GdipEnumerateMetafileSrcRectDestPointsI_delegate(HandleRef graphics, HandleRef metafile, IntPtr destPoints, int count, ref GPRECT srcRect, int pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes);
            private static FunctionWrapper<GdipEnumerateMetafileSrcRectDestPointsI_delegate> GdipEnumerateMetafileSrcRectDestPointsI_ptr;
            internal static int GdipEnumerateMetafileSrcRectDestPointsI(HandleRef graphics, HandleRef metafile, IntPtr destPoints, int count, ref GPRECT srcRect, int pageUnit, Graphics.EnumerateMetafileProc callback, HandleRef callbackdata, HandleRef imageattributes) => GdipEnumerateMetafileSrcRectDestPointsI_ptr.Delegate(graphics, metafile, destPoints, count, ref srcRect, pageUnit, callback, callbackdata, imageattributes);

            private delegate int GdipPlayMetafileRecord_delegate(HandleRef graphics, EmfPlusRecordType recordType, int flags, int dataSize, byte[] data);
            private static FunctionWrapper<GdipPlayMetafileRecord_delegate> GdipPlayMetafileRecord_ptr;
            internal static int GdipPlayMetafileRecord(HandleRef graphics, EmfPlusRecordType recordType, int flags, int dataSize, byte[] data) => GdipPlayMetafileRecord_ptr.Delegate(graphics, recordType, flags, dataSize, data);

            private delegate int GdipSaveGraphics_delegate(HandleRef graphics, out int state);
            private static FunctionWrapper<GdipSaveGraphics_delegate> GdipSaveGraphics_ptr;
            internal static int GdipSaveGraphics(HandleRef graphics, out int state) => GdipSaveGraphics_ptr.Delegate(graphics, out state);

            private delegate int GdipRestoreGraphics_delegate(HandleRef graphics, int state);
            private static FunctionWrapper<GdipRestoreGraphics_delegate> GdipRestoreGraphics_ptr;
            internal static int GdipRestoreGraphics(HandleRef graphics, int state) => GdipRestoreGraphics_ptr.Delegate(graphics, state);

            private delegate int GdipGetMetafileHeaderFromWmf_delegate(HandleRef hMetafile, WmfPlaceableFileHeader wmfplaceable, [In] [Out] MetafileHeaderWmf metafileHeaderWmf);
            private static FunctionWrapper<GdipGetMetafileHeaderFromWmf_delegate> GdipGetMetafileHeaderFromWmf_ptr;
            internal static int GdipGetMetafileHeaderFromWmf(HandleRef hMetafile, WmfPlaceableFileHeader wmfplaceable, [In] [Out] MetafileHeaderWmf metafileHeaderWmf) => GdipGetMetafileHeaderFromWmf_ptr.Delegate(hMetafile, wmfplaceable, metafileHeaderWmf);

            private delegate int GdipGetMetafileHeaderFromEmf_delegate(HandleRef hEnhMetafile, [In] [Out] MetafileHeaderEmf metafileHeaderEmf);
            private static FunctionWrapper<GdipGetMetafileHeaderFromEmf_delegate> GdipGetMetafileHeaderFromEmf_ptr;
            internal static int GdipGetMetafileHeaderFromEmf(HandleRef hEnhMetafile, [In] [Out] MetafileHeaderEmf metafileHeaderEmf) => GdipGetMetafileHeaderFromEmf_ptr.Delegate(hEnhMetafile, metafileHeaderEmf);

            private delegate int GdipGetMetafileHeaderFromFile_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromFile_delegate> GdipGetMetafileHeaderFromFile_ptr;
            internal static int GdipGetMetafileHeaderFromFile(string filename, IntPtr header) => GdipGetMetafileHeaderFromFile_ptr.Delegate(filename, header);

            private delegate int GdipGetMetafileHeaderFromStream_delegate(UnsafeNativeMethods.IStream stream, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromStream_delegate> GdipGetMetafileHeaderFromStream_ptr;
            internal static int GdipGetMetafileHeaderFromStream(UnsafeNativeMethods.IStream stream, IntPtr header) => GdipGetMetafileHeaderFromStream_ptr.Delegate(stream, header);

            private delegate int GdipGetMetafileHeaderFromMetafile_delegate(HandleRef metafile, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromMetafile_delegate> GdipGetMetafileHeaderFromMetafile_ptr;
            internal static int GdipGetMetafileHeaderFromMetafile(HandleRef metafile, IntPtr header) => GdipGetMetafileHeaderFromMetafile_ptr.Delegate(metafile, header);

            private delegate int GdipGetHemfFromMetafile_delegate(HandleRef metafile, out IntPtr hEnhMetafile);
            private static FunctionWrapper<GdipGetHemfFromMetafile_delegate> GdipGetHemfFromMetafile_ptr;
            internal static int GdipGetHemfFromMetafile(HandleRef metafile, out IntPtr hEnhMetafile) => GdipGetHemfFromMetafile_ptr.Delegate(metafile, out hEnhMetafile);

            private delegate int GdipCreateMetafileFromWmf_delegate(HandleRef hMetafile, [MarshalAs(UnmanagedType.Bool)]bool deleteWmf, WmfPlaceableFileHeader wmfplacealbeHeader, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromWmf_delegate> GdipCreateMetafileFromWmf_ptr;
            internal static int GdipCreateMetafileFromWmf(HandleRef hMetafile, bool deleteWmf, WmfPlaceableFileHeader wmfplacealbeHeader, out IntPtr metafile) => GdipCreateMetafileFromWmf_ptr.Delegate(hMetafile, deleteWmf, wmfplacealbeHeader, out metafile);

            private delegate int GdipCreateMetafileFromEmf_delegate(HandleRef hEnhMetafile, bool deleteEmf, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromEmf_delegate> GdipCreateMetafileFromEmf_ptr;
            internal static int GdipCreateMetafileFromEmf(HandleRef hEnhMetafile, bool deleteEmf, out IntPtr metafile) => GdipCreateMetafileFromEmf_ptr.Delegate(hEnhMetafile, deleteEmf, out metafile);

            private delegate int GdipCreateMetafileFromFile_delegate([MarshalAs(UnmanagedType.LPWStr)]string file, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromFile_delegate> GdipCreateMetafileFromFile_ptr;
            internal static int GdipCreateMetafileFromFile(string file, out IntPtr metafile) => GdipCreateMetafileFromFile_ptr.Delegate(file, out metafile);

            private delegate int GdipCreateMetafileFromStream_delegate(UnsafeNativeMethods.IStream stream, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromStream_delegate> GdipCreateMetafileFromStream_ptr;
            internal static int GdipCreateMetafileFromStream(UnsafeNativeMethods.IStream stream, out IntPtr metafile) => GdipCreateMetafileFromStream_ptr.Delegate(stream, out metafile);

            private delegate int GdipRecordMetafile_delegate(HandleRef referenceHdc, int emfType, ref GPRECTF frameRect, int frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafile_delegate> GdipRecordMetafile_ptr;
            internal static int GdipRecordMetafile(HandleRef referenceHdc, int emfType, ref GPRECTF frameRect, int frameUnit, string description, out IntPtr metafile) => GdipRecordMetafile_ptr.Delegate(referenceHdc, emfType, ref frameRect, frameUnit, description, out metafile);

            private delegate int GdipRecordMetafile2_delegate(HandleRef referenceHdc, int emfType, HandleRef pframeRect, int frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafile2_delegate> GdipRecordMetafile2_ptr;
            internal static int GdipRecordMetafile(HandleRef referenceHdc, int emfType, HandleRef pframeRect, int frameUnit, string description, out IntPtr metafile) => GdipRecordMetafile2_ptr.Delegate(referenceHdc, emfType, pframeRect, frameUnit, description, out metafile);

            private delegate int GdipRecordMetafileI_delegate(HandleRef referenceHdc, int emfType, ref GPRECT frameRect, int frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileI_delegate> GdipRecordMetafileI_ptr;
            internal static int GdipRecordMetafileI(HandleRef referenceHdc, int emfType, ref GPRECT frameRect, int frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileI_ptr.Delegate(referenceHdc, emfType, ref frameRect, frameUnit, description, out metafile);

            private delegate int GdipRecordMetafileFileName_delegate([MarshalAs(UnmanagedType.LPWStr)]string fileName, HandleRef referenceHdc, int emfType, ref GPRECTF frameRect, int frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileFileName_delegate> GdipRecordMetafileFileName_ptr;
            internal static int GdipRecordMetafileFileName(string fileName, HandleRef referenceHdc, int emfType, ref GPRECTF frameRect, int frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileFileName_ptr.Delegate(fileName, referenceHdc, emfType, ref frameRect, frameUnit, description, out metafile);

            private delegate int GdipRecordMetafileFileName2_delegate([MarshalAs(UnmanagedType.LPWStr)]string fileName, HandleRef referenceHdc, int emfType, HandleRef pframeRect, int frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileFileName2_delegate> GdipRecordMetafileFileName2_ptr;
            internal static int GdipRecordMetafileFileName(string fileName, HandleRef referenceHdc, int emfType, HandleRef pframeRect, int frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileFileName2_ptr.Delegate(fileName, referenceHdc, emfType, pframeRect, frameUnit, description, out metafile);

            private delegate int GdipRecordMetafileFileNameI_delegate([MarshalAs(UnmanagedType.LPWStr)]string fileName, HandleRef referenceHdc, int emfType, ref GPRECT frameRect, int frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileFileNameI_delegate> GdipRecordMetafileFileNameI_ptr;
            internal static int GdipRecordMetafileFileNameI(string fileName, HandleRef referenceHdc, int emfType, ref GPRECT frameRect, int frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileFileNameI_ptr.Delegate(fileName, referenceHdc, emfType, ref frameRect, frameUnit, description, out metafile);

            private delegate int GdipRecordMetafileStream_delegate(UnsafeNativeMethods.IStream stream, HandleRef referenceHdc, int emfType, ref GPRECTF frameRect, int frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileStream_delegate> GdipRecordMetafileStream_ptr;
            internal static int GdipRecordMetafileStream(UnsafeNativeMethods.IStream stream, HandleRef referenceHdc, int emfType, ref GPRECTF frameRect, int frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile) => GdipRecordMetafileStream_ptr.Delegate(stream, referenceHdc, emfType, ref frameRect, frameUnit, description, out metafile);

            private delegate int GdipRecordMetafileStream2_delegate(UnsafeNativeMethods.IStream stream, HandleRef referenceHdc, int emfType, HandleRef pframeRect, int frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileStream2_delegate> GdipRecordMetafileStream2_ptr;
            internal static int GdipRecordMetafileStream(UnsafeNativeMethods.IStream stream, HandleRef referenceHdc, int emfType, HandleRef pframeRect, int frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileStream2_ptr.Delegate(stream, referenceHdc, emfType, pframeRect, frameUnit, description, out metafile);

            private delegate int GdipRecordMetafileStreamI_delegate(UnsafeNativeMethods.IStream stream, HandleRef referenceHdc, int emfType, ref GPRECT frameRect, int frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileStreamI_delegate> GdipRecordMetafileStreamI_ptr;
            internal static int GdipRecordMetafileStreamI(UnsafeNativeMethods.IStream stream, HandleRef referenceHdc, int emfType, ref GPRECT frameRect, int frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileStreamI_ptr.Delegate(stream, referenceHdc, emfType, ref frameRect, frameUnit, description, out metafile);

            private delegate int GdipComment_delegate(HandleRef graphics, int sizeData, byte[] data);
            private static FunctionWrapper<GdipComment_delegate> GdipComment_ptr;
            internal static int GdipComment(HandleRef graphics, int sizeData, byte[] data) => GdipComment_ptr.Delegate(graphics, sizeData, data);

            private delegate int GdipCreateFontFromDC_delegate(HandleRef hdc, ref IntPtr font);
            private static FunctionWrapper<GdipCreateFontFromDC_delegate> GdipCreateFontFromDC_ptr;
            internal static int GdipCreateFontFromDC(HandleRef hdc, ref IntPtr font) => GdipCreateFontFromDC_ptr.Delegate(hdc, ref font);

#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
            private delegate int GdipCreateFontFromLogfontW_delegate(HandleRef hdc, [In] [Out] [MarshalAs(UnmanagedType.AsAny)]object lf, out IntPtr font);
#pragma warning restore CS0618
            private static FunctionWrapper<GdipCreateFontFromLogfontW_delegate> GdipCreateFontFromLogfontW_ptr;
            internal static int GdipCreateFontFromLogfontW(HandleRef hdc, [In] [Out] object lf, out IntPtr font) => GdipCreateFontFromLogfontW_ptr.Delegate(hdc, lf, out font);

            private delegate int GdipCreateFont_delegate(HandleRef fontFamily, float emSize, FontStyle style, GraphicsUnit unit, out IntPtr font);
            private static FunctionWrapper<GdipCreateFont_delegate> GdipCreateFont_ptr;
            internal static int GdipCreateFont(HandleRef fontFamily, float emSize, FontStyle style, GraphicsUnit unit, out IntPtr font) => GdipCreateFont_ptr.Delegate(fontFamily, emSize, style, unit, out font);

#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
            private delegate int GdipGetLogFontW_delegate(HandleRef font, HandleRef graphics, [In] [Out] [MarshalAs(UnmanagedType.AsAny)]object lf);
#pragma warning restore CS0618
            private static FunctionWrapper<GdipGetLogFontW_delegate> GdipGetLogFontW_ptr;
            internal static int GdipGetLogFontW(HandleRef font, HandleRef graphics, [In] [Out] object lf) => GdipGetLogFontW_ptr.Delegate(font, graphics, lf);

            private delegate int GdipCloneFont_delegate(HandleRef font, out IntPtr cloneFont);
            private static FunctionWrapper<GdipCloneFont_delegate> GdipCloneFont_ptr;
            internal static int GdipCloneFont(HandleRef font, out IntPtr cloneFont) => GdipCloneFont_ptr.Delegate(font, out cloneFont);

            private delegate int GdipDeleteFont_delegate(HandleRef font);
            private static FunctionWrapper<GdipDeleteFont_delegate> GdipDeleteFont_ptr;
            internal static int IntGdipDeleteFont(HandleRef font) => GdipDeleteFont_ptr.Delegate(font);

            private delegate int GdipGetFamily_delegate(HandleRef font, out IntPtr family);
            private static FunctionWrapper<GdipGetFamily_delegate> GdipGetFamily_ptr;
            internal static int GdipGetFamily(HandleRef font, out IntPtr family) => GdipGetFamily_ptr.Delegate(font, out family);

            private delegate int GdipGetFontStyle_delegate(HandleRef font, out FontStyle style);
            private static FunctionWrapper<GdipGetFontStyle_delegate> GdipGetFontStyle_ptr;
            internal static int GdipGetFontStyle(HandleRef font, out FontStyle style) => GdipGetFontStyle_ptr.Delegate(font, out style);

            private delegate int GdipGetFontSize_delegate(HandleRef font, out float size);
            private static FunctionWrapper<GdipGetFontSize_delegate> GdipGetFontSize_ptr;
            internal static int GdipGetFontSize(HandleRef font, out float size) => GdipGetFontSize_ptr.Delegate(font, out size);

            private delegate int GdipGetFontHeight_delegate(HandleRef font, HandleRef graphics, out float size);
            private static FunctionWrapper<GdipGetFontHeight_delegate> GdipGetFontHeight_ptr;
            internal static int GdipGetFontHeight(HandleRef font, HandleRef graphics, out float size) => GdipGetFontHeight_ptr.Delegate(font, graphics, out size);

            private delegate int GdipGetFontHeightGivenDPI_delegate(HandleRef font, float dpi, out float size);
            private static FunctionWrapper<GdipGetFontHeightGivenDPI_delegate> GdipGetFontHeightGivenDPI_ptr;
            internal static int GdipGetFontHeightGivenDPI(HandleRef font, float dpi, out float size) => GdipGetFontHeightGivenDPI_ptr.Delegate(font, dpi, out size);

            private delegate int GdipGetFontUnit_delegate(HandleRef font, out GraphicsUnit unit);
            private static FunctionWrapper<GdipGetFontUnit_delegate> GdipGetFontUnit_ptr;
            internal static int GdipGetFontUnit(HandleRef font, out GraphicsUnit unit) => GdipGetFontUnit_ptr.Delegate(font, out unit);

            private delegate int GdipDrawString_delegate(HandleRef graphics, [MarshalAs(UnmanagedType.LPWStr)]string textString, int length, HandleRef font, ref GPRECTF layoutRect, HandleRef stringFormat, HandleRef brush);
            private static FunctionWrapper<GdipDrawString_delegate> GdipDrawString_ptr;
            internal static int GdipDrawString(HandleRef graphics, string textString, int length, HandleRef font, ref GPRECTF layoutRect, HandleRef stringFormat, HandleRef brush) => GdipDrawString_ptr.Delegate(graphics, textString, length, font, ref layoutRect, stringFormat, brush);

            private delegate int GdipMeasureString_delegate(HandleRef graphics, [MarshalAs(UnmanagedType.LPWStr)]string textString, int length, HandleRef font, ref GPRECTF layoutRect, HandleRef stringFormat, ref GPRECTF boundingBox, out int codepointsFitted, out int linesFilled);
            private static FunctionWrapper<GdipMeasureString_delegate> GdipMeasureString_ptr;
            internal static int GdipMeasureString(HandleRef graphics, string textString, int length, HandleRef font, ref GPRECTF layoutRect, HandleRef stringFormat, ref GPRECTF boundingBox, out int codepointsFitted, out int linesFilled) => GdipMeasureString_ptr.Delegate(graphics, textString, length, font, ref layoutRect, stringFormat, ref boundingBox, out codepointsFitted, out linesFilled);

            private delegate int GdipMeasureCharacterRanges_delegate(HandleRef graphics, [MarshalAs(UnmanagedType.LPWStr)]string textString, int length, HandleRef font, ref GPRECTF layoutRect, HandleRef stringFormat, int characterCount, [In] [Out] IntPtr[] region);
            private static FunctionWrapper<GdipMeasureCharacterRanges_delegate> GdipMeasureCharacterRanges_ptr;
            internal static int GdipMeasureCharacterRanges(HandleRef graphics, string textString, int length, HandleRef font, ref GPRECTF layoutRect, HandleRef stringFormat, int characterCount, [In] [Out] IntPtr[] region) => GdipMeasureCharacterRanges_ptr.Delegate(graphics, textString, length, font, ref layoutRect, stringFormat, characterCount, region);
        }
    }
}
