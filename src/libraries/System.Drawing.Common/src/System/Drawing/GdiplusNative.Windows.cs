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
            private const string LibraryName = "gdiplus.dll";

            private static SafeLibraryHandle s_gdipHandle;

            private static IntPtr LoadNativeLibrary()
            {
                s_gdipHandle = Interop.Kernel32.LoadLibraryExW(LibraryName, IntPtr.Zero, 0);
                return s_gdipHandle.DangerousGetHandle();
            }

            private static void PlatformInitialize()
            {
                LoadFunctionPointers();
            }

            private static void LoadFunctionPointers()
            {
                GdiplusStartup_ptr = FunctionWrapper.Load<GdiplusStartup_delegate>(s_gdipModule, "GdiplusStartup", LibraryName);
                GdipCreatePath_ptr = FunctionWrapper.Load<GdipCreatePath_delegate>(s_gdipModule, "GdipCreatePath", LibraryName);
                GdipCreatePath2_ptr = FunctionWrapper.Load<GdipCreatePath2_delegate>(s_gdipModule, "GdipCreatePath2", LibraryName);
                GdipCreatePath2I_ptr = FunctionWrapper.Load<GdipCreatePath2I_delegate>(s_gdipModule, "GdipCreatePath2I", LibraryName);
                GdipClonePath_ptr = FunctionWrapper.Load<GdipClonePath_delegate>(s_gdipModule, "GdipClonePath", LibraryName);
                GdipDeletePath_ptr = FunctionWrapper.Load<GdipDeletePath_delegate>(s_gdipModule, "GdipDeletePath", LibraryName);
                GdipResetPath_ptr = FunctionWrapper.Load<GdipResetPath_delegate>(s_gdipModule, "GdipResetPath", LibraryName);
                GdipGetPointCount_ptr = FunctionWrapper.Load<GdipGetPointCount_delegate>(s_gdipModule, "GdipGetPointCount", LibraryName);
                GdipGetPathTypes_ptr = FunctionWrapper.Load<GdipGetPathTypes_delegate>(s_gdipModule, "GdipGetPathTypes", LibraryName);
                GdipGetPathPoints_ptr = FunctionWrapper.Load<GdipGetPathPoints_delegate>(s_gdipModule, "GdipGetPathPoints", LibraryName);
                GdipGetPathFillMode_ptr = FunctionWrapper.Load<GdipGetPathFillMode_delegate>(s_gdipModule, "GdipGetPathFillMode", LibraryName);
                GdipSetPathFillMode_ptr = FunctionWrapper.Load<GdipSetPathFillMode_delegate>(s_gdipModule, "GdipSetPathFillMode", LibraryName);
                GdipGetPathData_ptr = FunctionWrapper.Load<GdipGetPathData_delegate>(s_gdipModule, "GdipGetPathData", LibraryName);
                GdipStartPathFigure_ptr = FunctionWrapper.Load<GdipStartPathFigure_delegate>(s_gdipModule, "GdipStartPathFigure", LibraryName);
                GdipClosePathFigure_ptr = FunctionWrapper.Load<GdipClosePathFigure_delegate>(s_gdipModule, "GdipClosePathFigure", LibraryName);
                GdipClosePathFigures_ptr = FunctionWrapper.Load<GdipClosePathFigures_delegate>(s_gdipModule, "GdipClosePathFigures", LibraryName);
                GdipSetPathMarker_ptr = FunctionWrapper.Load<GdipSetPathMarker_delegate>(s_gdipModule, "GdipSetPathMarker", LibraryName);
                GdipClearPathMarkers_ptr = FunctionWrapper.Load<GdipClearPathMarkers_delegate>(s_gdipModule, "GdipClearPathMarkers", LibraryName);
                GdipReversePath_ptr = FunctionWrapper.Load<GdipReversePath_delegate>(s_gdipModule, "GdipReversePath", LibraryName);
                GdipGetPathLastPoint_ptr = FunctionWrapper.Load<GdipGetPathLastPoint_delegate>(s_gdipModule, "GdipGetPathLastPoint", LibraryName);
                GdipAddPathLine_ptr = FunctionWrapper.Load<GdipAddPathLine_delegate>(s_gdipModule, "GdipAddPathLine", LibraryName);
                GdipAddPathLine2_ptr = FunctionWrapper.Load<GdipAddPathLine2_delegate>(s_gdipModule, "GdipAddPathLine2", LibraryName);
                GdipAddPathArc_ptr = FunctionWrapper.Load<GdipAddPathArc_delegate>(s_gdipModule, "GdipAddPathArc", LibraryName);
                GdipAddPathBezier_ptr = FunctionWrapper.Load<GdipAddPathBezier_delegate>(s_gdipModule, "GdipAddPathBezier", LibraryName);
                GdipAddPathBeziers_ptr = FunctionWrapper.Load<GdipAddPathBeziers_delegate>(s_gdipModule, "GdipAddPathBeziers", LibraryName);
                GdipAddPathCurve_ptr = FunctionWrapper.Load<GdipAddPathCurve_delegate>(s_gdipModule, "GdipAddPathCurve", LibraryName);
                GdipAddPathCurve2_ptr = FunctionWrapper.Load<GdipAddPathCurve2_delegate>(s_gdipModule, "GdipAddPathCurve2", LibraryName);
                GdipAddPathCurve3_ptr = FunctionWrapper.Load<GdipAddPathCurve3_delegate>(s_gdipModule, "GdipAddPathCurve3", LibraryName);
                GdipAddPathClosedCurve_ptr = FunctionWrapper.Load<GdipAddPathClosedCurve_delegate>(s_gdipModule, "GdipAddPathClosedCurve", LibraryName);
                GdipAddPathClosedCurve2_ptr = FunctionWrapper.Load<GdipAddPathClosedCurve2_delegate>(s_gdipModule, "GdipAddPathClosedCurve2", LibraryName);
                GdipAddPathRectangle_ptr = FunctionWrapper.Load<GdipAddPathRectangle_delegate>(s_gdipModule, "GdipAddPathRectangle", LibraryName);
                GdipAddPathRectangles_ptr = FunctionWrapper.Load<GdipAddPathRectangles_delegate>(s_gdipModule, "GdipAddPathRectangles", LibraryName);
                GdipAddPathEllipse_ptr = FunctionWrapper.Load<GdipAddPathEllipse_delegate>(s_gdipModule, "GdipAddPathEllipse", LibraryName);
                GdipAddPathPie_ptr = FunctionWrapper.Load<GdipAddPathPie_delegate>(s_gdipModule, "GdipAddPathPie", LibraryName);
                GdipAddPathPolygon_ptr = FunctionWrapper.Load<GdipAddPathPolygon_delegate>(s_gdipModule, "GdipAddPathPolygon", LibraryName);
                GdipAddPathPath_ptr = FunctionWrapper.Load<GdipAddPathPath_delegate>(s_gdipModule, "GdipAddPathPath", LibraryName);
                GdipAddPathString_ptr = FunctionWrapper.Load<GdipAddPathString_delegate>(s_gdipModule, "GdipAddPathString", LibraryName);
                GdipAddPathStringI_ptr = FunctionWrapper.Load<GdipAddPathStringI_delegate>(s_gdipModule, "GdipAddPathStringI", LibraryName);
                GdipAddPathLineI_ptr = FunctionWrapper.Load<GdipAddPathLineI_delegate>(s_gdipModule, "GdipAddPathLineI", LibraryName);
                GdipAddPathLine2I_ptr = FunctionWrapper.Load<GdipAddPathLine2I_delegate>(s_gdipModule, "GdipAddPathLine2I", LibraryName);
                GdipAddPathArcI_ptr = FunctionWrapper.Load<GdipAddPathArcI_delegate>(s_gdipModule, "GdipAddPathArcI", LibraryName);
                GdipAddPathBezierI_ptr = FunctionWrapper.Load<GdipAddPathBezierI_delegate>(s_gdipModule, "GdipAddPathBezierI", LibraryName);
                GdipAddPathBeziersI_ptr = FunctionWrapper.Load<GdipAddPathBeziersI_delegate>(s_gdipModule, "GdipAddPathBeziersI", LibraryName);
                GdipAddPathCurveI_ptr = FunctionWrapper.Load<GdipAddPathCurveI_delegate>(s_gdipModule, "GdipAddPathCurveI", LibraryName);
                GdipAddPathCurve2I_ptr = FunctionWrapper.Load<GdipAddPathCurve2I_delegate>(s_gdipModule, "GdipAddPathCurve2I", LibraryName);
                GdipAddPathCurve3I_ptr = FunctionWrapper.Load<GdipAddPathCurve3I_delegate>(s_gdipModule, "GdipAddPathCurve3I", LibraryName);
                GdipAddPathClosedCurveI_ptr = FunctionWrapper.Load<GdipAddPathClosedCurveI_delegate>(s_gdipModule, "GdipAddPathClosedCurveI", LibraryName);
                GdipAddPathClosedCurve2I_ptr = FunctionWrapper.Load<GdipAddPathClosedCurve2I_delegate>(s_gdipModule, "GdipAddPathClosedCurve2I", LibraryName);
                GdipAddPathRectangleI_ptr = FunctionWrapper.Load<GdipAddPathRectangleI_delegate>(s_gdipModule, "GdipAddPathRectangleI", LibraryName);
                GdipAddPathRectanglesI_ptr = FunctionWrapper.Load<GdipAddPathRectanglesI_delegate>(s_gdipModule, "GdipAddPathRectanglesI", LibraryName);
                GdipAddPathEllipseI_ptr = FunctionWrapper.Load<GdipAddPathEllipseI_delegate>(s_gdipModule, "GdipAddPathEllipseI", LibraryName);
                GdipAddPathPieI_ptr = FunctionWrapper.Load<GdipAddPathPieI_delegate>(s_gdipModule, "GdipAddPathPieI", LibraryName);
                GdipAddPathPolygonI_ptr = FunctionWrapper.Load<GdipAddPathPolygonI_delegate>(s_gdipModule, "GdipAddPathPolygonI", LibraryName);
                GdipFlattenPath_ptr = FunctionWrapper.Load<GdipFlattenPath_delegate>(s_gdipModule, "GdipFlattenPath", LibraryName);
                GdipWidenPath_ptr = FunctionWrapper.Load<GdipWidenPath_delegate>(s_gdipModule, "GdipWidenPath", LibraryName);
                GdipWarpPath_ptr = FunctionWrapper.Load<GdipWarpPath_delegate>(s_gdipModule, "GdipWarpPath", LibraryName);
                GdipTransformPath_ptr = FunctionWrapper.Load<GdipTransformPath_delegate>(s_gdipModule, "GdipTransformPath", LibraryName);
                GdipGetPathWorldBounds_ptr = FunctionWrapper.Load<GdipGetPathWorldBounds_delegate>(s_gdipModule, "GdipGetPathWorldBounds", LibraryName);
                GdipIsVisiblePathPoint_ptr = FunctionWrapper.Load<GdipIsVisiblePathPoint_delegate>(s_gdipModule, "GdipIsVisiblePathPoint", LibraryName);
                GdipIsVisiblePathPointI_ptr = FunctionWrapper.Load<GdipIsVisiblePathPointI_delegate>(s_gdipModule, "GdipIsVisiblePathPointI", LibraryName);
                GdipIsOutlineVisiblePathPoint_ptr = FunctionWrapper.Load<GdipIsOutlineVisiblePathPoint_delegate>(s_gdipModule, "GdipIsOutlineVisiblePathPoint", LibraryName);
                GdipIsOutlineVisiblePathPointI_ptr = FunctionWrapper.Load<GdipIsOutlineVisiblePathPointI_delegate>(s_gdipModule, "GdipIsOutlineVisiblePathPointI", LibraryName);
                GdipDeleteBrush_ptr = FunctionWrapper.Load<GdipDeleteBrush_delegate>(s_gdipModule, "GdipDeleteBrush", LibraryName);
                GdipLoadImageFromStream_ptr = FunctionWrapper.Load<GdipLoadImageFromStream_delegate>(s_gdipModule, "GdipLoadImageFromStream", LibraryName);
                GdipLoadImageFromFile_ptr = FunctionWrapper.Load<GdipLoadImageFromFile_delegate>(s_gdipModule, "GdipLoadImageFromFile", LibraryName);
                GdipLoadImageFromStreamICM_ptr = FunctionWrapper.Load<GdipLoadImageFromStreamICM_delegate>(s_gdipModule, "GdipLoadImageFromStreamICM", LibraryName);
                GdipLoadImageFromFileICM_ptr = FunctionWrapper.Load<GdipLoadImageFromFileICM_delegate>(s_gdipModule, "GdipLoadImageFromFileICM", LibraryName);
                GdipCloneImage_ptr = FunctionWrapper.Load<GdipCloneImage_delegate>(s_gdipModule, "GdipCloneImage", LibraryName);
                GdipDisposeImage_ptr = FunctionWrapper.Load<GdipDisposeImage_delegate>(s_gdipModule, "GdipDisposeImage", LibraryName);
                GdipSaveImageToFile_ptr = FunctionWrapper.Load<GdipSaveImageToFile_delegate>(s_gdipModule, "GdipSaveImageToFile", LibraryName);
                GdipSaveImageToStream_ptr = FunctionWrapper.Load<GdipSaveImageToStream_delegate>(s_gdipModule, "GdipSaveImageToStream", LibraryName);
                GdipSaveAdd_ptr = FunctionWrapper.Load<GdipSaveAdd_delegate>(s_gdipModule, "GdipSaveAdd", LibraryName);
                GdipSaveAddImage_ptr = FunctionWrapper.Load<GdipSaveAddImage_delegate>(s_gdipModule, "GdipSaveAddImage", LibraryName);
                GdipGetImageGraphicsContext_ptr = FunctionWrapper.Load<GdipGetImageGraphicsContext_delegate>(s_gdipModule, "GdipGetImageGraphicsContext", LibraryName);
                GdipGetImageBounds_ptr = FunctionWrapper.Load<GdipGetImageBounds_delegate>(s_gdipModule, "GdipGetImageBounds", LibraryName);
                GdipGetImageDimension_ptr = FunctionWrapper.Load<GdipGetImageDimension_delegate>(s_gdipModule, "GdipGetImageDimension", LibraryName);
                GdipGetImageType_ptr = FunctionWrapper.Load<GdipGetImageType_delegate>(s_gdipModule, "GdipGetImageType", LibraryName);
                GdipGetImageWidth_ptr = FunctionWrapper.Load<GdipGetImageWidth_delegate>(s_gdipModule, "GdipGetImageWidth", LibraryName);
                GdipGetImageHeight_ptr = FunctionWrapper.Load<GdipGetImageHeight_delegate>(s_gdipModule, "GdipGetImageHeight", LibraryName);
                GdipGetImageHorizontalResolution_ptr = FunctionWrapper.Load<GdipGetImageHorizontalResolution_delegate>(s_gdipModule, "GdipGetImageHorizontalResolution", LibraryName);
                GdipGetImageVerticalResolution_ptr = FunctionWrapper.Load<GdipGetImageVerticalResolution_delegate>(s_gdipModule, "GdipGetImageVerticalResolution", LibraryName);
                GdipGetImageFlags_ptr = FunctionWrapper.Load<GdipGetImageFlags_delegate>(s_gdipModule, "GdipGetImageFlags", LibraryName);
                GdipGetImageRawFormat_ptr = FunctionWrapper.Load<GdipGetImageRawFormat_delegate>(s_gdipModule, "GdipGetImageRawFormat", LibraryName);
                GdipGetImagePixelFormat_ptr = FunctionWrapper.Load<GdipGetImagePixelFormat_delegate>(s_gdipModule, "GdipGetImagePixelFormat", LibraryName);
                GdipGetImageThumbnail_ptr = FunctionWrapper.Load<GdipGetImageThumbnail_delegate>(s_gdipModule, "GdipGetImageThumbnail", LibraryName);
                GdipGetEncoderParameterListSize_ptr = FunctionWrapper.Load<GdipGetEncoderParameterListSize_delegate>(s_gdipModule, "GdipGetEncoderParameterListSize", LibraryName);
                GdipGetEncoderParameterList_ptr = FunctionWrapper.Load<GdipGetEncoderParameterList_delegate>(s_gdipModule, "GdipGetEncoderParameterList", LibraryName);
                GdipImageGetFrameDimensionsCount_ptr = FunctionWrapper.Load<GdipImageGetFrameDimensionsCount_delegate>(s_gdipModule, "GdipImageGetFrameDimensionsCount", LibraryName);
                GdipImageGetFrameDimensionsList_ptr = FunctionWrapper.Load<GdipImageGetFrameDimensionsList_delegate>(s_gdipModule, "GdipImageGetFrameDimensionsList", LibraryName);
                GdipImageGetFrameCount_ptr = FunctionWrapper.Load<GdipImageGetFrameCount_delegate>(s_gdipModule, "GdipImageGetFrameCount", LibraryName);
                GdipImageSelectActiveFrame_ptr = FunctionWrapper.Load<GdipImageSelectActiveFrame_delegate>(s_gdipModule, "GdipImageSelectActiveFrame", LibraryName);
                GdipImageRotateFlip_ptr = FunctionWrapper.Load<GdipImageRotateFlip_delegate>(s_gdipModule, "GdipImageRotateFlip", LibraryName);
                GdipGetImagePalette_ptr = FunctionWrapper.Load<GdipGetImagePalette_delegate>(s_gdipModule, "GdipGetImagePalette", LibraryName);
                GdipSetImagePalette_ptr = FunctionWrapper.Load<GdipSetImagePalette_delegate>(s_gdipModule, "GdipSetImagePalette", LibraryName);
                GdipGetImagePaletteSize_ptr = FunctionWrapper.Load<GdipGetImagePaletteSize_delegate>(s_gdipModule, "GdipGetImagePaletteSize", LibraryName);
                GdipGetPropertyCount_ptr = FunctionWrapper.Load<GdipGetPropertyCount_delegate>(s_gdipModule, "GdipGetPropertyCount", LibraryName);
                GdipGetPropertyIdList_ptr = FunctionWrapper.Load<GdipGetPropertyIdList_delegate>(s_gdipModule, "GdipGetPropertyIdList", LibraryName);
                GdipGetPropertyItemSize_ptr = FunctionWrapper.Load<GdipGetPropertyItemSize_delegate>(s_gdipModule, "GdipGetPropertyItemSize", LibraryName);
                GdipGetPropertyItem_ptr = FunctionWrapper.Load<GdipGetPropertyItem_delegate>(s_gdipModule, "GdipGetPropertyItem", LibraryName);
                GdipGetPropertySize_ptr = FunctionWrapper.Load<GdipGetPropertySize_delegate>(s_gdipModule, "GdipGetPropertySize", LibraryName);
                GdipGetAllPropertyItems_ptr = FunctionWrapper.Load<GdipGetAllPropertyItems_delegate>(s_gdipModule, "GdipGetAllPropertyItems", LibraryName);
                GdipRemovePropertyItem_ptr = FunctionWrapper.Load<GdipRemovePropertyItem_delegate>(s_gdipModule, "GdipRemovePropertyItem", LibraryName);
                GdipSetPropertyItem_ptr = FunctionWrapper.Load<GdipSetPropertyItem_delegate>(s_gdipModule, "GdipSetPropertyItem", LibraryName);
                GdipImageForceValidation_ptr = FunctionWrapper.Load<GdipImageForceValidation_delegate>(s_gdipModule, "GdipImageForceValidation", LibraryName);
                GdipFlush_ptr = FunctionWrapper.Load<GdipFlush_delegate>(s_gdipModule, "GdipFlush", LibraryName);
                GdipCreateFromHDC_ptr = FunctionWrapper.Load<GdipCreateFromHDC_delegate>(s_gdipModule, "GdipCreateFromHDC", LibraryName);
                GdipCreateFromHDC2_ptr = FunctionWrapper.Load<GdipCreateFromHDC2_delegate>(s_gdipModule, "GdipCreateFromHDC2", LibraryName);
                GdipCreateFromHWND_ptr = FunctionWrapper.Load<GdipCreateFromHWND_delegate>(s_gdipModule, "GdipCreateFromHWND", LibraryName);
                GdipDeleteGraphics_ptr = FunctionWrapper.Load<GdipDeleteGraphics_delegate>(s_gdipModule, "GdipDeleteGraphics", LibraryName);
                GdipGetDC_ptr = FunctionWrapper.Load<GdipGetDC_delegate>(s_gdipModule, "GdipGetDC", LibraryName);
                GdipReleaseDC_ptr = FunctionWrapper.Load<GdipReleaseDC_delegate>(s_gdipModule, "GdipReleaseDC", LibraryName);
                GdipSetCompositingMode_ptr = FunctionWrapper.Load<GdipSetCompositingMode_delegate>(s_gdipModule, "GdipSetCompositingMode", LibraryName);
                GdipSetTextRenderingHint_ptr = FunctionWrapper.Load<GdipSetTextRenderingHint_delegate>(s_gdipModule, "GdipSetTextRenderingHint", LibraryName);
                GdipSetTextContrast_ptr = FunctionWrapper.Load<GdipSetTextContrast_delegate>(s_gdipModule, "GdipSetTextContrast", LibraryName);
                GdipSetInterpolationMode_ptr = FunctionWrapper.Load<GdipSetInterpolationMode_delegate>(s_gdipModule, "GdipSetInterpolationMode", LibraryName);
                GdipGetCompositingMode_ptr = FunctionWrapper.Load<GdipGetCompositingMode_delegate>(s_gdipModule, "GdipGetCompositingMode", LibraryName);
                GdipSetRenderingOrigin_ptr = FunctionWrapper.Load<GdipSetRenderingOrigin_delegate>(s_gdipModule, "GdipSetRenderingOrigin", LibraryName);
                GdipGetRenderingOrigin_ptr = FunctionWrapper.Load<GdipGetRenderingOrigin_delegate>(s_gdipModule, "GdipGetRenderingOrigin", LibraryName);
                GdipSetCompositingQuality_ptr = FunctionWrapper.Load<GdipSetCompositingQuality_delegate>(s_gdipModule, "GdipSetCompositingQuality", LibraryName);
                GdipGetCompositingQuality_ptr = FunctionWrapper.Load<GdipGetCompositingQuality_delegate>(s_gdipModule, "GdipGetCompositingQuality", LibraryName);
                GdipSetSmoothingMode_ptr = FunctionWrapper.Load<GdipSetSmoothingMode_delegate>(s_gdipModule, "GdipSetSmoothingMode", LibraryName);
                GdipGetSmoothingMode_ptr = FunctionWrapper.Load<GdipGetSmoothingMode_delegate>(s_gdipModule, "GdipGetSmoothingMode", LibraryName);
                GdipSetPixelOffsetMode_ptr = FunctionWrapper.Load<GdipSetPixelOffsetMode_delegate>(s_gdipModule, "GdipSetPixelOffsetMode", LibraryName);
                GdipGetPixelOffsetMode_ptr = FunctionWrapper.Load<GdipGetPixelOffsetMode_delegate>(s_gdipModule, "GdipGetPixelOffsetMode", LibraryName);
                GdipGetTextRenderingHint_ptr = FunctionWrapper.Load<GdipGetTextRenderingHint_delegate>(s_gdipModule, "GdipGetTextRenderingHint", LibraryName);
                GdipGetTextContrast_ptr = FunctionWrapper.Load<GdipGetTextContrast_delegate>(s_gdipModule, "GdipGetTextContrast", LibraryName);
                GdipGetInterpolationMode_ptr = FunctionWrapper.Load<GdipGetInterpolationMode_delegate>(s_gdipModule, "GdipGetInterpolationMode", LibraryName);
                GdipGetPageUnit_ptr = FunctionWrapper.Load<GdipGetPageUnit_delegate>(s_gdipModule, "GdipGetPageUnit", LibraryName);
                GdipGetPageScale_ptr = FunctionWrapper.Load<GdipGetPageScale_delegate>(s_gdipModule, "GdipGetPageScale", LibraryName);
                GdipSetPageUnit_ptr = FunctionWrapper.Load<GdipSetPageUnit_delegate>(s_gdipModule, "GdipSetPageUnit", LibraryName);
                GdipSetPageScale_ptr = FunctionWrapper.Load<GdipSetPageScale_delegate>(s_gdipModule, "GdipSetPageScale", LibraryName);
                GdipGetDpiX_ptr = FunctionWrapper.Load<GdipGetDpiX_delegate>(s_gdipModule, "GdipGetDpiX", LibraryName);
                GdipGetDpiY_ptr = FunctionWrapper.Load<GdipGetDpiY_delegate>(s_gdipModule, "GdipGetDpiY", LibraryName);
                GdipTransformPoints_ptr = FunctionWrapper.Load<GdipTransformPoints_delegate>(s_gdipModule, "GdipTransformPoints", LibraryName);
                GdipTransformPointsI_ptr = FunctionWrapper.Load<GdipTransformPointsI_delegate>(s_gdipModule, "GdipTransformPointsI", LibraryName);
                GdipGetNearestColor_ptr = FunctionWrapper.Load<GdipGetNearestColor_delegate>(s_gdipModule, "GdipGetNearestColor", LibraryName);
                GdipCreateHalftonePalette_ptr = FunctionWrapper.Load<GdipCreateHalftonePalette_delegate>(s_gdipModule, "GdipCreateHalftonePalette", LibraryName);
                GdipDrawLine_ptr = FunctionWrapper.Load<GdipDrawLine_delegate>(s_gdipModule, "GdipDrawLine", LibraryName);
                GdipDrawLineI_ptr = FunctionWrapper.Load<GdipDrawLineI_delegate>(s_gdipModule, "GdipDrawLineI", LibraryName);
                GdipDrawLines_ptr = FunctionWrapper.Load<GdipDrawLines_delegate>(s_gdipModule, "GdipDrawLines", LibraryName);
                GdipDrawLinesI_ptr = FunctionWrapper.Load<GdipDrawLinesI_delegate>(s_gdipModule, "GdipDrawLinesI", LibraryName);
                GdipDrawArc_ptr = FunctionWrapper.Load<GdipDrawArc_delegate>(s_gdipModule, "GdipDrawArc", LibraryName);
                GdipDrawArcI_ptr = FunctionWrapper.Load<GdipDrawArcI_delegate>(s_gdipModule, "GdipDrawArcI", LibraryName);
                GdipDrawBezier_ptr = FunctionWrapper.Load<GdipDrawBezier_delegate>(s_gdipModule, "GdipDrawBezier", LibraryName);
                GdipDrawBeziers_ptr = FunctionWrapper.Load<GdipDrawBeziers_delegate>(s_gdipModule, "GdipDrawBeziers", LibraryName);
                GdipDrawBeziersI_ptr = FunctionWrapper.Load<GdipDrawBeziersI_delegate>(s_gdipModule, "GdipDrawBeziersI", LibraryName);
                GdipDrawRectangle_ptr = FunctionWrapper.Load<GdipDrawRectangle_delegate>(s_gdipModule, "GdipDrawRectangle", LibraryName);
                GdipDrawRectangleI_ptr = FunctionWrapper.Load<GdipDrawRectangleI_delegate>(s_gdipModule, "GdipDrawRectangleI", LibraryName);
                GdipDrawRectangles_ptr = FunctionWrapper.Load<GdipDrawRectangles_delegate>(s_gdipModule, "GdipDrawRectangles", LibraryName);
                GdipDrawRectanglesI_ptr = FunctionWrapper.Load<GdipDrawRectanglesI_delegate>(s_gdipModule, "GdipDrawRectanglesI", LibraryName);
                GdipDrawEllipse_ptr = FunctionWrapper.Load<GdipDrawEllipse_delegate>(s_gdipModule, "GdipDrawEllipse", LibraryName);
                GdipDrawEllipseI_ptr = FunctionWrapper.Load<GdipDrawEllipseI_delegate>(s_gdipModule, "GdipDrawEllipseI", LibraryName);
                GdipDrawPie_ptr = FunctionWrapper.Load<GdipDrawPie_delegate>(s_gdipModule, "GdipDrawPie", LibraryName);
                GdipDrawPieI_ptr = FunctionWrapper.Load<GdipDrawPieI_delegate>(s_gdipModule, "GdipDrawPieI", LibraryName);
                GdipDrawPolygon_ptr = FunctionWrapper.Load<GdipDrawPolygon_delegate>(s_gdipModule, "GdipDrawPolygon", LibraryName);
                GdipDrawPolygonI_ptr = FunctionWrapper.Load<GdipDrawPolygonI_delegate>(s_gdipModule, "GdipDrawPolygonI", LibraryName);
                GdipDrawPath_ptr = FunctionWrapper.Load<GdipDrawPath_delegate>(s_gdipModule, "GdipDrawPath", LibraryName);
                GdipDrawCurve_ptr = FunctionWrapper.Load<GdipDrawCurve_delegate>(s_gdipModule, "GdipDrawCurve", LibraryName);
                GdipDrawCurveI_ptr = FunctionWrapper.Load<GdipDrawCurveI_delegate>(s_gdipModule, "GdipDrawCurveI", LibraryName);
                GdipDrawCurve2_ptr = FunctionWrapper.Load<GdipDrawCurve2_delegate>(s_gdipModule, "GdipDrawCurve2", LibraryName);
                GdipDrawCurve2I_ptr = FunctionWrapper.Load<GdipDrawCurve2I_delegate>(s_gdipModule, "GdipDrawCurve2I", LibraryName);
                GdipDrawCurve3_ptr = FunctionWrapper.Load<GdipDrawCurve3_delegate>(s_gdipModule, "GdipDrawCurve3", LibraryName);
                GdipDrawCurve3I_ptr = FunctionWrapper.Load<GdipDrawCurve3I_delegate>(s_gdipModule, "GdipDrawCurve3I", LibraryName);
                GdipDrawClosedCurve_ptr = FunctionWrapper.Load<GdipDrawClosedCurve_delegate>(s_gdipModule, "GdipDrawClosedCurve", LibraryName);
                GdipDrawClosedCurveI_ptr = FunctionWrapper.Load<GdipDrawClosedCurveI_delegate>(s_gdipModule, "GdipDrawClosedCurveI", LibraryName);
                GdipDrawClosedCurve2_ptr = FunctionWrapper.Load<GdipDrawClosedCurve2_delegate>(s_gdipModule, "GdipDrawClosedCurve2", LibraryName);
                GdipDrawClosedCurve2I_ptr = FunctionWrapper.Load<GdipDrawClosedCurve2I_delegate>(s_gdipModule, "GdipDrawClosedCurve2I", LibraryName);
                GdipGraphicsClear_ptr = FunctionWrapper.Load<GdipGraphicsClear_delegate>(s_gdipModule, "GdipGraphicsClear", LibraryName);
                GdipFillRectangle_ptr = FunctionWrapper.Load<GdipFillRectangle_delegate>(s_gdipModule, "GdipFillRectangle", LibraryName);
                GdipFillRectangleI_ptr = FunctionWrapper.Load<GdipFillRectangleI_delegate>(s_gdipModule, "GdipFillRectangleI", LibraryName);
                GdipFillRectangles_ptr = FunctionWrapper.Load<GdipFillRectangles_delegate>(s_gdipModule, "GdipFillRectangles", LibraryName);
                GdipFillRectanglesI_ptr = FunctionWrapper.Load<GdipFillRectanglesI_delegate>(s_gdipModule, "GdipFillRectanglesI", LibraryName);
                GdipFillPolygon_ptr = FunctionWrapper.Load<GdipFillPolygon_delegate>(s_gdipModule, "GdipFillPolygon", LibraryName);
                GdipFillPolygonI_ptr = FunctionWrapper.Load<GdipFillPolygonI_delegate>(s_gdipModule, "GdipFillPolygonI", LibraryName);
                GdipFillEllipse_ptr = FunctionWrapper.Load<GdipFillEllipse_delegate>(s_gdipModule, "GdipFillEllipse", LibraryName);
                GdipFillEllipseI_ptr = FunctionWrapper.Load<GdipFillEllipseI_delegate>(s_gdipModule, "GdipFillEllipseI", LibraryName);
                GdipFillPie_ptr = FunctionWrapper.Load<GdipFillPie_delegate>(s_gdipModule, "GdipFillPie", LibraryName);
                GdipFillPieI_ptr = FunctionWrapper.Load<GdipFillPieI_delegate>(s_gdipModule, "GdipFillPieI", LibraryName);
                GdipFillPath_ptr = FunctionWrapper.Load<GdipFillPath_delegate>(s_gdipModule, "GdipFillPath", LibraryName);
                GdipFillClosedCurve_ptr = FunctionWrapper.Load<GdipFillClosedCurve_delegate>(s_gdipModule, "GdipFillClosedCurve", LibraryName);
                GdipFillClosedCurveI_ptr = FunctionWrapper.Load<GdipFillClosedCurveI_delegate>(s_gdipModule, "GdipFillClosedCurveI", LibraryName);
                GdipFillClosedCurve2_ptr = FunctionWrapper.Load<GdipFillClosedCurve2_delegate>(s_gdipModule, "GdipFillClosedCurve2", LibraryName);
                GdipFillClosedCurve2I_ptr = FunctionWrapper.Load<GdipFillClosedCurve2I_delegate>(s_gdipModule, "GdipFillClosedCurve2I", LibraryName);
                GdipDrawImage_ptr = FunctionWrapper.Load<GdipDrawImage_delegate>(s_gdipModule, "GdipDrawImage", LibraryName);
                GdipDrawImageI_ptr = FunctionWrapper.Load<GdipDrawImageI_delegate>(s_gdipModule, "GdipDrawImageI", LibraryName);
                GdipDrawImageRect_ptr = FunctionWrapper.Load<GdipDrawImageRect_delegate>(s_gdipModule, "GdipDrawImageRect", LibraryName);
                GdipDrawImageRectI_ptr = FunctionWrapper.Load<GdipDrawImageRectI_delegate>(s_gdipModule, "GdipDrawImageRectI", LibraryName);
                GdipDrawImagePoints_ptr = FunctionWrapper.Load<GdipDrawImagePoints_delegate>(s_gdipModule, "GdipDrawImagePoints", LibraryName);
                GdipDrawImagePointsI_ptr = FunctionWrapper.Load<GdipDrawImagePointsI_delegate>(s_gdipModule, "GdipDrawImagePointsI", LibraryName);
                GdipDrawImagePointRect_ptr = FunctionWrapper.Load<GdipDrawImagePointRect_delegate>(s_gdipModule, "GdipDrawImagePointRect", LibraryName);
                GdipDrawImagePointRectI_ptr = FunctionWrapper.Load<GdipDrawImagePointRectI_delegate>(s_gdipModule, "GdipDrawImagePointRectI", LibraryName);
                GdipDrawImageRectRect_ptr = FunctionWrapper.Load<GdipDrawImageRectRect_delegate>(s_gdipModule, "GdipDrawImageRectRect", LibraryName);
                GdipDrawImageRectRectI_ptr = FunctionWrapper.Load<GdipDrawImageRectRectI_delegate>(s_gdipModule, "GdipDrawImageRectRectI", LibraryName);
                GdipDrawImagePointsRect_ptr = FunctionWrapper.Load<GdipDrawImagePointsRect_delegate>(s_gdipModule, "GdipDrawImagePointsRect", LibraryName);
                GdipDrawImagePointsRectI_ptr = FunctionWrapper.Load<GdipDrawImagePointsRectI_delegate>(s_gdipModule, "GdipDrawImagePointsRectI", LibraryName);
                GdipEnumerateMetafileDestPoint_ptr = FunctionWrapper.Load<GdipEnumerateMetafileDestPoint_delegate>(s_gdipModule, "GdipEnumerateMetafileDestPoint", LibraryName);
                GdipEnumerateMetafileDestPointI_ptr = FunctionWrapper.Load<GdipEnumerateMetafileDestPointI_delegate>(s_gdipModule, "GdipEnumerateMetafileDestPointI", LibraryName);
                GdipEnumerateMetafileDestRect_ptr = FunctionWrapper.Load<GdipEnumerateMetafileDestRect_delegate>(s_gdipModule, "GdipEnumerateMetafileDestRect", LibraryName);
                GdipEnumerateMetafileDestRectI_ptr = FunctionWrapper.Load<GdipEnumerateMetafileDestRectI_delegate>(s_gdipModule, "GdipEnumerateMetafileDestRectI", LibraryName);
                GdipEnumerateMetafileDestPoints_ptr = FunctionWrapper.Load<GdipEnumerateMetafileDestPoints_delegate>(s_gdipModule, "GdipEnumerateMetafileDestPoints", LibraryName);
                GdipEnumerateMetafileDestPointsI_ptr = FunctionWrapper.Load<GdipEnumerateMetafileDestPointsI_delegate>(s_gdipModule, "GdipEnumerateMetafileDestPointsI", LibraryName);
                GdipEnumerateMetafileSrcRectDestPoint_ptr = FunctionWrapper.Load<GdipEnumerateMetafileSrcRectDestPoint_delegate>(s_gdipModule, "GdipEnumerateMetafileSrcRectDestPoint", LibraryName);
                GdipEnumerateMetafileSrcRectDestPointI_ptr = FunctionWrapper.Load<GdipEnumerateMetafileSrcRectDestPointI_delegate>(s_gdipModule, "GdipEnumerateMetafileSrcRectDestPointI", LibraryName);
                GdipEnumerateMetafileSrcRectDestRect_ptr = FunctionWrapper.Load<GdipEnumerateMetafileSrcRectDestRect_delegate>(s_gdipModule, "GdipEnumerateMetafileSrcRectDestRect", LibraryName);
                GdipEnumerateMetafileSrcRectDestRectI_ptr = FunctionWrapper.Load<GdipEnumerateMetafileSrcRectDestRectI_delegate>(s_gdipModule, "GdipEnumerateMetafileSrcRectDestRectI", LibraryName);
                GdipEnumerateMetafileSrcRectDestPoints_ptr = FunctionWrapper.Load<GdipEnumerateMetafileSrcRectDestPoints_delegate>(s_gdipModule, "GdipEnumerateMetafileSrcRectDestPoints", LibraryName);
                GdipEnumerateMetafileSrcRectDestPointsI_ptr = FunctionWrapper.Load<GdipEnumerateMetafileSrcRectDestPointsI_delegate>(s_gdipModule, "GdipEnumerateMetafileSrcRectDestPointsI", LibraryName);
                GdipPlayMetafileRecord_ptr = FunctionWrapper.Load<GdipPlayMetafileRecord_delegate>(s_gdipModule, "GdipPlayMetafileRecord", LibraryName);
                GdipSaveGraphics_ptr = FunctionWrapper.Load<GdipSaveGraphics_delegate>(s_gdipModule, "GdipSaveGraphics", LibraryName);
                GdipRestoreGraphics_ptr = FunctionWrapper.Load<GdipRestoreGraphics_delegate>(s_gdipModule, "GdipRestoreGraphics", LibraryName);
                GdipEndContainer_ptr = FunctionWrapper.Load<GdipEndContainer_delegate>(s_gdipModule, "GdipEndContainer", LibraryName);
                GdipGetMetafileHeaderFromWmf_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromWmf_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromWmf", LibraryName);
                GdipGetMetafileHeaderFromEmf_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromEmf_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromEmf", LibraryName);
                GdipGetMetafileHeaderFromFile_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromFile_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromFile", LibraryName);
                GdipGetMetafileHeaderFromStream_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromStream_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromStream", LibraryName);
                GdipGetMetafileHeaderFromMetafile_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromMetafile_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromMetafile", LibraryName);
                GdipGetHemfFromMetafile_ptr = FunctionWrapper.Load<GdipGetHemfFromMetafile_delegate>(s_gdipModule, "GdipGetHemfFromMetafile", LibraryName);
                GdipCreateMetafileFromWmf_ptr = FunctionWrapper.Load<GdipCreateMetafileFromWmf_delegate>(s_gdipModule, "GdipCreateMetafileFromWmf", LibraryName);
                GdipCreateMetafileFromEmf_ptr = FunctionWrapper.Load<GdipCreateMetafileFromEmf_delegate>(s_gdipModule, "GdipCreateMetafileFromEmf", LibraryName);
                GdipCreateMetafileFromFile_ptr = FunctionWrapper.Load<GdipCreateMetafileFromFile_delegate>(s_gdipModule, "GdipCreateMetafileFromFile", LibraryName);
                GdipCreateMetafileFromStream_ptr = FunctionWrapper.Load<GdipCreateMetafileFromStream_delegate>(s_gdipModule, "GdipCreateMetafileFromStream", LibraryName);
                GdipRecordMetafile_ptr = FunctionWrapper.Load<GdipRecordMetafile_delegate>(s_gdipModule, "GdipRecordMetafile", LibraryName);
                GdipRecordMetafile2_ptr = FunctionWrapper.Load<GdipRecordMetafile2_delegate>(s_gdipModule, "GdipRecordMetafile", LibraryName);
                GdipRecordMetafileI_ptr = FunctionWrapper.Load<GdipRecordMetafileI_delegate>(s_gdipModule, "GdipRecordMetafileI", LibraryName);
                GdipRecordMetafileFileName_ptr = FunctionWrapper.Load<GdipRecordMetafileFileName_delegate>(s_gdipModule, "GdipRecordMetafileFileName", LibraryName);
                GdipRecordMetafileFileName2_ptr = FunctionWrapper.Load<GdipRecordMetafileFileName2_delegate>(s_gdipModule, "GdipRecordMetafileFileName", LibraryName);
                GdipRecordMetafileFileNameI_ptr = FunctionWrapper.Load<GdipRecordMetafileFileNameI_delegate>(s_gdipModule, "GdipRecordMetafileFileNameI", LibraryName);
                GdipRecordMetafileStream_ptr = FunctionWrapper.Load<GdipRecordMetafileStream_delegate>(s_gdipModule, "GdipRecordMetafileStream", LibraryName);
                GdipRecordMetafileStream2_ptr = FunctionWrapper.Load<GdipRecordMetafileStream2_delegate>(s_gdipModule, "GdipRecordMetafileStream", LibraryName);
                GdipRecordMetafileStreamI_ptr = FunctionWrapper.Load<GdipRecordMetafileStreamI_delegate>(s_gdipModule, "GdipRecordMetafileStreamI", LibraryName);
                GdipComment_ptr = FunctionWrapper.Load<GdipComment_delegate>(s_gdipModule, "GdipComment", LibraryName);
                GdipCreateFontFromDC_ptr = FunctionWrapper.Load<GdipCreateFontFromDC_delegate>(s_gdipModule, "GdipCreateFontFromDC", LibraryName);
                GdipCreateFontFromLogfontW_ptr = FunctionWrapper.Load<GdipCreateFontFromLogfontW_delegate>(s_gdipModule, "GdipCreateFontFromLogfontW", LibraryName);
                GdipCreateFont_ptr = FunctionWrapper.Load<GdipCreateFont_delegate>(s_gdipModule, "GdipCreateFont", LibraryName);
                GdipGetLogFontW_ptr = FunctionWrapper.Load<GdipGetLogFontW_delegate>(s_gdipModule, "GdipGetLogFontW", LibraryName);
                GdipCloneFont_ptr = FunctionWrapper.Load<GdipCloneFont_delegate>(s_gdipModule, "GdipCloneFont", LibraryName);
                GdipDeleteFont_ptr = FunctionWrapper.Load<GdipDeleteFont_delegate>(s_gdipModule, "GdipDeleteFont", LibraryName);
                GdipGetFamily_ptr = FunctionWrapper.Load<GdipGetFamily_delegate>(s_gdipModule, "GdipGetFamily", LibraryName);
                GdipGetFontStyle_ptr = FunctionWrapper.Load<GdipGetFontStyle_delegate>(s_gdipModule, "GdipGetFontStyle", LibraryName);
                GdipGetFontSize_ptr = FunctionWrapper.Load<GdipGetFontSize_delegate>(s_gdipModule, "GdipGetFontSize", LibraryName);
                GdipGetFontHeight_ptr = FunctionWrapper.Load<GdipGetFontHeight_delegate>(s_gdipModule, "GdipGetFontHeight", LibraryName);
                GdipGetFontHeightGivenDPI_ptr = FunctionWrapper.Load<GdipGetFontHeightGivenDPI_delegate>(s_gdipModule, "GdipGetFontHeightGivenDPI", LibraryName);
                GdipGetFontUnit_ptr = FunctionWrapper.Load<GdipGetFontUnit_delegate>(s_gdipModule, "GdipGetFontUnit", LibraryName);
                GdipDrawString_ptr = FunctionWrapper.Load<GdipDrawString_delegate>(s_gdipModule, "GdipDrawString", LibraryName);
                GdipMeasureString_ptr = FunctionWrapper.Load<GdipMeasureString_delegate>(s_gdipModule, "GdipMeasureString", LibraryName);
                GdipMeasureCharacterRanges_ptr = FunctionWrapper.Load<GdipMeasureCharacterRanges_delegate>(s_gdipModule, "GdipMeasureCharacterRanges", LibraryName);
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
