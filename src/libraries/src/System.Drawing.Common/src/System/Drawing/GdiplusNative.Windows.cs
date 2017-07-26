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
                GdipCreateMatrix_ptr = LoadFunction<GdipCreateMatrix_delegate>("GdipCreateMatrix");
                GdipCreateMatrix2_ptr = LoadFunction<GdipCreateMatrix2_delegate>("GdipCreateMatrix2");
                GdipCreateMatrix3_ptr = LoadFunction<GdipCreateMatrix3_delegate>("GdipCreateMatrix3");
                GdipCreateMatrix3I_ptr = LoadFunction<GdipCreateMatrix3I_delegate>("GdipCreateMatrix3I");
                GdipCloneMatrix_ptr = LoadFunction<GdipCloneMatrix_delegate>("GdipCloneMatrix");
                GdipDeleteMatrix_ptr = LoadFunction<GdipDeleteMatrix_delegate>("GdipDeleteMatrix");
                GdipSetMatrixElements_ptr = LoadFunction<GdipSetMatrixElements_delegate>("GdipSetMatrixElements");
                GdipMultiplyMatrix_ptr = LoadFunction<GdipMultiplyMatrix_delegate>("GdipMultiplyMatrix");
                GdipTranslateMatrix_ptr = LoadFunction<GdipTranslateMatrix_delegate>("GdipTranslateMatrix");
                GdipScaleMatrix_ptr = LoadFunction<GdipScaleMatrix_delegate>("GdipScaleMatrix");
                GdipRotateMatrix_ptr = LoadFunction<GdipRotateMatrix_delegate>("GdipRotateMatrix");
                GdipShearMatrix_ptr = LoadFunction<GdipShearMatrix_delegate>("GdipShearMatrix");
                GdipInvertMatrix_ptr = LoadFunction<GdipInvertMatrix_delegate>("GdipInvertMatrix");
                GdipTransformMatrixPoints_ptr = LoadFunction<GdipTransformMatrixPoints_delegate>("GdipTransformMatrixPoints");
                GdipTransformMatrixPointsI_ptr = LoadFunction<GdipTransformMatrixPointsI_delegate>("GdipTransformMatrixPointsI");
                GdipVectorTransformMatrixPoints_ptr = LoadFunction<GdipVectorTransformMatrixPoints_delegate>("GdipVectorTransformMatrixPoints");
                GdipVectorTransformMatrixPointsI_ptr = LoadFunction<GdipVectorTransformMatrixPointsI_delegate>("GdipVectorTransformMatrixPointsI");
                GdipGetMatrixElements_ptr = LoadFunction<GdipGetMatrixElements_delegate>("GdipGetMatrixElements");
                GdipIsMatrixInvertible_ptr = LoadFunction<GdipIsMatrixInvertible_delegate>("GdipIsMatrixInvertible");
                GdipIsMatrixIdentity_ptr = LoadFunction<GdipIsMatrixIdentity_delegate>("GdipIsMatrixIdentity");
                GdipIsMatrixEqual_ptr = LoadFunction<GdipIsMatrixEqual_delegate>("GdipIsMatrixEqual");
                GdipCreateRegion_ptr = LoadFunction<GdipCreateRegion_delegate>("GdipCreateRegion");
                GdipCreateRegionRect_ptr = LoadFunction<GdipCreateRegionRect_delegate>("GdipCreateRegionRect");
                GdipCreateRegionRectI_ptr = LoadFunction<GdipCreateRegionRectI_delegate>("GdipCreateRegionRectI");
                GdipCreateRegionPath_ptr = LoadFunction<GdipCreateRegionPath_delegate>("GdipCreateRegionPath");
                GdipCreateRegionRgnData_ptr = LoadFunction<GdipCreateRegionRgnData_delegate>("GdipCreateRegionRgnData");
                GdipCreateRegionHrgn_ptr = LoadFunction<GdipCreateRegionHrgn_delegate>("GdipCreateRegionHrgn");
                GdipCloneRegion_ptr = LoadFunction<GdipCloneRegion_delegate>("GdipCloneRegion");
                GdipDeleteRegion_ptr = LoadFunction<GdipDeleteRegion_delegate>("GdipDeleteRegion");
                GdipSetInfinite_ptr = LoadFunction<GdipSetInfinite_delegate>("GdipSetInfinite");
                GdipSetEmpty_ptr = LoadFunction<GdipSetEmpty_delegate>("GdipSetEmpty");
                GdipCombineRegionRect_ptr = LoadFunction<GdipCombineRegionRect_delegate>("GdipCombineRegionRect");
                GdipCombineRegionRectI_ptr = LoadFunction<GdipCombineRegionRectI_delegate>("GdipCombineRegionRectI");
                GdipCombineRegionPath_ptr = LoadFunction<GdipCombineRegionPath_delegate>("GdipCombineRegionPath");
                GdipCombineRegionRegion_ptr = LoadFunction<GdipCombineRegionRegion_delegate>("GdipCombineRegionRegion");
                GdipTranslateRegion_ptr = LoadFunction<GdipTranslateRegion_delegate>("GdipTranslateRegion");
                GdipTranslateRegionI_ptr = LoadFunction<GdipTranslateRegionI_delegate>("GdipTranslateRegionI");
                GdipTransformRegion_ptr = LoadFunction<GdipTransformRegion_delegate>("GdipTransformRegion");
                GdipGetRegionBounds_ptr = LoadFunction<GdipGetRegionBounds_delegate>("GdipGetRegionBounds");
                GdipGetRegionHRgn_ptr = LoadFunction<GdipGetRegionHRgn_delegate>("GdipGetRegionHRgn");
                GdipIsEmptyRegion_ptr = LoadFunction<GdipIsEmptyRegion_delegate>("GdipIsEmptyRegion");
                GdipIsInfiniteRegion_ptr = LoadFunction<GdipIsInfiniteRegion_delegate>("GdipIsInfiniteRegion");
                GdipIsEqualRegion_ptr = LoadFunction<GdipIsEqualRegion_delegate>("GdipIsEqualRegion");
                GdipGetRegionDataSize_ptr = LoadFunction<GdipGetRegionDataSize_delegate>("GdipGetRegionDataSize");
                GdipGetRegionData_ptr = LoadFunction<GdipGetRegionData_delegate>("GdipGetRegionData");
                GdipIsVisibleRegionPoint_ptr = LoadFunction<GdipIsVisibleRegionPoint_delegate>("GdipIsVisibleRegionPoint");
                GdipIsVisibleRegionPointI_ptr = LoadFunction<GdipIsVisibleRegionPointI_delegate>("GdipIsVisibleRegionPointI");
                GdipIsVisibleRegionRect_ptr = LoadFunction<GdipIsVisibleRegionRect_delegate>("GdipIsVisibleRegionRect");
                GdipIsVisibleRegionRectI_ptr = LoadFunction<GdipIsVisibleRegionRectI_delegate>("GdipIsVisibleRegionRectI");
                GdipGetRegionScansCount_ptr = LoadFunction<GdipGetRegionScansCount_delegate>("GdipGetRegionScansCount");
                GdipGetRegionScans_ptr = LoadFunction<GdipGetRegionScans_delegate>("GdipGetRegionScans");
                GdipDeleteBrush_ptr = LoadFunction<GdipDeleteBrush_delegate>("GdipDeleteBrush");
                GdipCreateLineBrush_ptr = LoadFunction<GdipCreateLineBrush_delegate>("GdipCreateLineBrush");
                GdipCreateLineBrushI_ptr = LoadFunction<GdipCreateLineBrushI_delegate>("GdipCreateLineBrushI");
                GdipCreateLineBrushFromRect_ptr = LoadFunction<GdipCreateLineBrushFromRect_delegate>("GdipCreateLineBrushFromRect");
                GdipCreateLineBrushFromRectI_ptr = LoadFunction<GdipCreateLineBrushFromRectI_delegate>("GdipCreateLineBrushFromRectI");
                GdipCreateLineBrushFromRectWithAngle_ptr = LoadFunction<GdipCreateLineBrushFromRectWithAngle_delegate>("GdipCreateLineBrushFromRectWithAngle");
                GdipCreateLineBrushFromRectWithAngleI_ptr = LoadFunction<GdipCreateLineBrushFromRectWithAngleI_delegate>("GdipCreateLineBrushFromRectWithAngleI");
                GdipSetLineColors_ptr = LoadFunction<GdipSetLineColors_delegate>("GdipSetLineColors");
                GdipGetLineColors_ptr = LoadFunction<GdipGetLineColors_delegate>("GdipGetLineColors");
                GdipGetLineRect_ptr = LoadFunction<GdipGetLineRect_delegate>("GdipGetLineRect");
                GdipGetLineGammaCorrection_ptr = LoadFunction<GdipGetLineGammaCorrection_delegate>("GdipGetLineGammaCorrection");
                GdipSetLineGammaCorrection_ptr = LoadFunction<GdipSetLineGammaCorrection_delegate>("GdipSetLineGammaCorrection");
                GdipSetLineSigmaBlend_ptr = LoadFunction<GdipSetLineSigmaBlend_delegate>("GdipSetLineSigmaBlend");
                GdipSetLineLinearBlend_ptr = LoadFunction<GdipSetLineLinearBlend_delegate>("GdipSetLineLinearBlend");
                GdipGetLineBlendCount_ptr = LoadFunction<GdipGetLineBlendCount_delegate>("GdipGetLineBlendCount");
                GdipGetLineBlend_ptr = LoadFunction<GdipGetLineBlend_delegate>("GdipGetLineBlend");
                GdipSetLineBlend_ptr = LoadFunction<GdipSetLineBlend_delegate>("GdipSetLineBlend");
                GdipGetLinePresetBlendCount_ptr = LoadFunction<GdipGetLinePresetBlendCount_delegate>("GdipGetLinePresetBlendCount");
                GdipGetLinePresetBlend_ptr = LoadFunction<GdipGetLinePresetBlend_delegate>("GdipGetLinePresetBlend");
                GdipSetLinePresetBlend_ptr = LoadFunction<GdipSetLinePresetBlend_delegate>("GdipSetLinePresetBlend");
                GdipSetLineWrapMode_ptr = LoadFunction<GdipSetLineWrapMode_delegate>("GdipSetLineWrapMode");
                GdipGetLineWrapMode_ptr = LoadFunction<GdipGetLineWrapMode_delegate>("GdipGetLineWrapMode");
                GdipResetLineTransform_ptr = LoadFunction<GdipResetLineTransform_delegate>("GdipResetLineTransform");
                GdipMultiplyLineTransform_ptr = LoadFunction<GdipMultiplyLineTransform_delegate>("GdipMultiplyLineTransform");
                GdipGetLineTransform_ptr = LoadFunction<GdipGetLineTransform_delegate>("GdipGetLineTransform");
                GdipSetLineTransform_ptr = LoadFunction<GdipSetLineTransform_delegate>("GdipSetLineTransform");
                GdipTranslateLineTransform_ptr = LoadFunction<GdipTranslateLineTransform_delegate>("GdipTranslateLineTransform");
                GdipScaleLineTransform_ptr = LoadFunction<GdipScaleLineTransform_delegate>("GdipScaleLineTransform");
                GdipRotateLineTransform_ptr = LoadFunction<GdipRotateLineTransform_delegate>("GdipRotateLineTransform");
                GdipCreatePathGradient_ptr = LoadFunction<GdipCreatePathGradient_delegate>("GdipCreatePathGradient");
                GdipCreatePathGradientI_ptr = LoadFunction<GdipCreatePathGradientI_delegate>("GdipCreatePathGradientI");
                GdipCreatePathGradientFromPath_ptr = LoadFunction<GdipCreatePathGradientFromPath_delegate>("GdipCreatePathGradientFromPath");
                GdipGetPathGradientCenterColor_ptr = LoadFunction<GdipGetPathGradientCenterColor_delegate>("GdipGetPathGradientCenterColor");
                GdipSetPathGradientCenterColor_ptr = LoadFunction<GdipSetPathGradientCenterColor_delegate>("GdipSetPathGradientCenterColor");
                GdipGetPathGradientSurroundColorsWithCount_ptr = LoadFunction<GdipGetPathGradientSurroundColorsWithCount_delegate>("GdipGetPathGradientSurroundColorsWithCount");
                GdipSetPathGradientSurroundColorsWithCount_ptr = LoadFunction<GdipSetPathGradientSurroundColorsWithCount_delegate>("GdipSetPathGradientSurroundColorsWithCount");
                GdipGetPathGradientCenterPoint_ptr = LoadFunction<GdipGetPathGradientCenterPoint_delegate>("GdipGetPathGradientCenterPoint");
                GdipSetPathGradientCenterPoint_ptr = LoadFunction<GdipSetPathGradientCenterPoint_delegate>("GdipSetPathGradientCenterPoint");
                GdipGetPathGradientRect_ptr = LoadFunction<GdipGetPathGradientRect_delegate>("GdipGetPathGradientRect");
                GdipGetPathGradientPointCount_ptr = LoadFunction<GdipGetPathGradientPointCount_delegate>("GdipGetPathGradientPointCount");
                GdipGetPathGradientSurroundColorCount_ptr = LoadFunction<GdipGetPathGradientSurroundColorCount_delegate>("GdipGetPathGradientSurroundColorCount");
                GdipGetPathGradientBlendCount_ptr = LoadFunction<GdipGetPathGradientBlendCount_delegate>("GdipGetPathGradientBlendCount");
                GdipGetPathGradientBlend_ptr = LoadFunction<GdipGetPathGradientBlend_delegate>("GdipGetPathGradientBlend");
                GdipSetPathGradientBlend_ptr = LoadFunction<GdipSetPathGradientBlend_delegate>("GdipSetPathGradientBlend");
                GdipGetPathGradientPresetBlendCount_ptr = LoadFunction<GdipGetPathGradientPresetBlendCount_delegate>("GdipGetPathGradientPresetBlendCount");
                GdipGetPathGradientPresetBlend_ptr = LoadFunction<GdipGetPathGradientPresetBlend_delegate>("GdipGetPathGradientPresetBlend");
                GdipSetPathGradientPresetBlend_ptr = LoadFunction<GdipSetPathGradientPresetBlend_delegate>("GdipSetPathGradientPresetBlend");
                GdipSetPathGradientSigmaBlend_ptr = LoadFunction<GdipSetPathGradientSigmaBlend_delegate>("GdipSetPathGradientSigmaBlend");
                GdipSetPathGradientLinearBlend_ptr = LoadFunction<GdipSetPathGradientLinearBlend_delegate>("GdipSetPathGradientLinearBlend");
                GdipSetPathGradientWrapMode_ptr = LoadFunction<GdipSetPathGradientWrapMode_delegate>("GdipSetPathGradientWrapMode");
                GdipGetPathGradientWrapMode_ptr = LoadFunction<GdipGetPathGradientWrapMode_delegate>("GdipGetPathGradientWrapMode");
                GdipSetPathGradientTransform_ptr = LoadFunction<GdipSetPathGradientTransform_delegate>("GdipSetPathGradientTransform");
                GdipGetPathGradientTransform_ptr = LoadFunction<GdipGetPathGradientTransform_delegate>("GdipGetPathGradientTransform");
                GdipResetPathGradientTransform_ptr = LoadFunction<GdipResetPathGradientTransform_delegate>("GdipResetPathGradientTransform");
                GdipMultiplyPathGradientTransform_ptr = LoadFunction<GdipMultiplyPathGradientTransform_delegate>("GdipMultiplyPathGradientTransform");
                GdipTranslatePathGradientTransform_ptr = LoadFunction<GdipTranslatePathGradientTransform_delegate>("GdipTranslatePathGradientTransform");
                GdipScalePathGradientTransform_ptr = LoadFunction<GdipScalePathGradientTransform_delegate>("GdipScalePathGradientTransform");
                GdipRotatePathGradientTransform_ptr = LoadFunction<GdipRotatePathGradientTransform_delegate>("GdipRotatePathGradientTransform");
                GdipGetPathGradientFocusScales_ptr = LoadFunction<GdipGetPathGradientFocusScales_delegate>("GdipGetPathGradientFocusScales");
                GdipSetPathGradientFocusScales_ptr = LoadFunction<GdipSetPathGradientFocusScales_delegate>("GdipSetPathGradientFocusScales");
                GdipCreatePen1_ptr = LoadFunction<GdipCreatePen1_delegate>("GdipCreatePen1");
                GdipCreatePen2_ptr = LoadFunction<GdipCreatePen2_delegate>("GdipCreatePen2");
                GdipClonePen_ptr = LoadFunction<GdipClonePen_delegate>("GdipClonePen");
                GdipDeletePen_ptr = LoadFunction<GdipDeletePen_delegate>("GdipDeletePen");
                GdipSetPenMode_ptr = LoadFunction<GdipSetPenMode_delegate>("GdipSetPenMode");
                GdipGetPenMode_ptr = LoadFunction<GdipGetPenMode_delegate>("GdipGetPenMode");
                GdipSetPenWidth_ptr = LoadFunction<GdipSetPenWidth_delegate>("GdipSetPenWidth");
                GdipGetPenWidth_ptr = LoadFunction<GdipGetPenWidth_delegate>("GdipGetPenWidth");
                GdipSetPenLineCap197819_ptr = LoadFunction<GdipSetPenLineCap197819_delegate>("GdipSetPenLineCap197819");
                GdipSetPenStartCap_ptr = LoadFunction<GdipSetPenStartCap_delegate>("GdipSetPenStartCap");
                GdipSetPenEndCap_ptr = LoadFunction<GdipSetPenEndCap_delegate>("GdipSetPenEndCap");
                GdipGetPenStartCap_ptr = LoadFunction<GdipGetPenStartCap_delegate>("GdipGetPenStartCap");
                GdipGetPenEndCap_ptr = LoadFunction<GdipGetPenEndCap_delegate>("GdipGetPenEndCap");
                GdipGetPenDashCap197819_ptr = LoadFunction<GdipGetPenDashCap197819_delegate>("GdipGetPenDashCap197819");
                GdipSetPenDashCap197819_ptr = LoadFunction<GdipSetPenDashCap197819_delegate>("GdipSetPenDashCap197819");
                GdipSetPenLineJoin_ptr = LoadFunction<GdipSetPenLineJoin_delegate>("GdipSetPenLineJoin");
                GdipGetPenLineJoin_ptr = LoadFunction<GdipGetPenLineJoin_delegate>("GdipGetPenLineJoin");
                GdipSetPenCustomStartCap_ptr = LoadFunction<GdipSetPenCustomStartCap_delegate>("GdipSetPenCustomStartCap");
                GdipGetPenCustomStartCap_ptr = LoadFunction<GdipGetPenCustomStartCap_delegate>("GdipGetPenCustomStartCap");
                GdipSetPenCustomEndCap_ptr = LoadFunction<GdipSetPenCustomEndCap_delegate>("GdipSetPenCustomEndCap");
                GdipGetPenCustomEndCap_ptr = LoadFunction<GdipGetPenCustomEndCap_delegate>("GdipGetPenCustomEndCap");
                GdipSetPenMiterLimit_ptr = LoadFunction<GdipSetPenMiterLimit_delegate>("GdipSetPenMiterLimit");
                GdipGetPenMiterLimit_ptr = LoadFunction<GdipGetPenMiterLimit_delegate>("GdipGetPenMiterLimit");
                GdipSetPenTransform_ptr = LoadFunction<GdipSetPenTransform_delegate>("GdipSetPenTransform");
                GdipGetPenTransform_ptr = LoadFunction<GdipGetPenTransform_delegate>("GdipGetPenTransform");
                GdipResetPenTransform_ptr = LoadFunction<GdipResetPenTransform_delegate>("GdipResetPenTransform");
                GdipMultiplyPenTransform_ptr = LoadFunction<GdipMultiplyPenTransform_delegate>("GdipMultiplyPenTransform");
                GdipTranslatePenTransform_ptr = LoadFunction<GdipTranslatePenTransform_delegate>("GdipTranslatePenTransform");
                GdipScalePenTransform_ptr = LoadFunction<GdipScalePenTransform_delegate>("GdipScalePenTransform");
                GdipRotatePenTransform_ptr = LoadFunction<GdipRotatePenTransform_delegate>("GdipRotatePenTransform");
                GdipSetPenColor_ptr = LoadFunction<GdipSetPenColor_delegate>("GdipSetPenColor");
                GdipGetPenColor_ptr = LoadFunction<GdipGetPenColor_delegate>("GdipGetPenColor");
                GdipSetPenBrushFill_ptr = LoadFunction<GdipSetPenBrushFill_delegate>("GdipSetPenBrushFill");
                GdipGetPenBrushFill_ptr = LoadFunction<GdipGetPenBrushFill_delegate>("GdipGetPenBrushFill");
                GdipGetPenFillType_ptr = LoadFunction<GdipGetPenFillType_delegate>("GdipGetPenFillType");
                GdipGetPenDashStyle_ptr = LoadFunction<GdipGetPenDashStyle_delegate>("GdipGetPenDashStyle");
                GdipSetPenDashStyle_ptr = LoadFunction<GdipSetPenDashStyle_delegate>("GdipSetPenDashStyle");
                GdipSetPenDashArray_ptr = LoadFunction<GdipSetPenDashArray_delegate>("GdipSetPenDashArray");
                GdipGetPenDashOffset_ptr = LoadFunction<GdipGetPenDashOffset_delegate>("GdipGetPenDashOffset");
                GdipSetPenDashOffset_ptr = LoadFunction<GdipSetPenDashOffset_delegate>("GdipSetPenDashOffset");
                GdipGetPenDashCount_ptr = LoadFunction<GdipGetPenDashCount_delegate>("GdipGetPenDashCount");
                GdipGetPenDashArray_ptr = LoadFunction<GdipGetPenDashArray_delegate>("GdipGetPenDashArray");
                GdipGetPenCompoundCount_ptr = LoadFunction<GdipGetPenCompoundCount_delegate>("GdipGetPenCompoundCount");
                GdipSetPenCompoundArray_ptr = LoadFunction<GdipSetPenCompoundArray_delegate>("GdipSetPenCompoundArray");
                GdipGetPenCompoundArray_ptr = LoadFunction<GdipGetPenCompoundArray_delegate>("GdipGetPenCompoundArray");
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
                GdipSetWorldTransform_ptr = LoadFunction<GdipSetWorldTransform_delegate>("GdipSetWorldTransform");
                GdipResetWorldTransform_ptr = LoadFunction<GdipResetWorldTransform_delegate>("GdipResetWorldTransform");
                GdipMultiplyWorldTransform_ptr = LoadFunction<GdipMultiplyWorldTransform_delegate>("GdipMultiplyWorldTransform");
                GdipTranslateWorldTransform_ptr = LoadFunction<GdipTranslateWorldTransform_delegate>("GdipTranslateWorldTransform");
                GdipScaleWorldTransform_ptr = LoadFunction<GdipScaleWorldTransform_delegate>("GdipScaleWorldTransform");
                GdipRotateWorldTransform_ptr = LoadFunction<GdipRotateWorldTransform_delegate>("GdipRotateWorldTransform");
                GdipGetWorldTransform_ptr = LoadFunction<GdipGetWorldTransform_delegate>("GdipGetWorldTransform");
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
                GdipFillRegion_ptr = LoadFunction<GdipFillRegion_delegate>("GdipFillRegion");
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
                GdipSetClipGraphics_ptr = LoadFunction<GdipSetClipGraphics_delegate>("GdipSetClipGraphics");
                GdipSetClipRect_ptr = LoadFunction<GdipSetClipRect_delegate>("GdipSetClipRect");
                GdipSetClipRectI_ptr = LoadFunction<GdipSetClipRectI_delegate>("GdipSetClipRectI");
                GdipSetClipPath_ptr = LoadFunction<GdipSetClipPath_delegate>("GdipSetClipPath");
                GdipSetClipRegion_ptr = LoadFunction<GdipSetClipRegion_delegate>("GdipSetClipRegion");
                GdipResetClip_ptr = LoadFunction<GdipResetClip_delegate>("GdipResetClip");
                GdipTranslateClip_ptr = LoadFunction<GdipTranslateClip_delegate>("GdipTranslateClip");
                GdipGetClip_ptr = LoadFunction<GdipGetClip_delegate>("GdipGetClip");
                GdipGetClipBounds_ptr = LoadFunction<GdipGetClipBounds_delegate>("GdipGetClipBounds");
                GdipIsClipEmpty_ptr = LoadFunction<GdipIsClipEmpty_delegate>("GdipIsClipEmpty");
                GdipGetVisibleClipBounds_ptr = LoadFunction<GdipGetVisibleClipBounds_delegate>("GdipGetVisibleClipBounds");
                GdipIsVisibleClipEmpty_ptr = LoadFunction<GdipIsVisibleClipEmpty_delegate>("GdipIsVisibleClipEmpty");
                GdipIsVisiblePoint_ptr = LoadFunction<GdipIsVisiblePoint_delegate>("GdipIsVisiblePoint");
                GdipIsVisiblePointI_ptr = LoadFunction<GdipIsVisiblePointI_delegate>("GdipIsVisiblePointI");
                GdipIsVisibleRect_ptr = LoadFunction<GdipIsVisibleRect_delegate>("GdipIsVisibleRect");
                GdipIsVisibleRectI_ptr = LoadFunction<GdipIsVisibleRectI_delegate>("GdipIsVisibleRectI");
                GdipSaveGraphics_ptr = LoadFunction<GdipSaveGraphics_delegate>("GdipSaveGraphics");
                GdipRestoreGraphics_ptr = LoadFunction<GdipRestoreGraphics_delegate>("GdipRestoreGraphics");
                GdipBeginContainer_ptr = LoadFunction<GdipBeginContainer_delegate>("GdipBeginContainer");
                GdipBeginContainer2_ptr = LoadFunction<GdipBeginContainer2_delegate>("GdipBeginContainer2");
                GdipBeginContainerI_ptr = LoadFunction<GdipBeginContainerI_delegate>("GdipBeginContainerI");
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
                GdipNewPrivateFontCollection_ptr = LoadFunction<GdipNewPrivateFontCollection_delegate>("GdipNewPrivateFontCollection");
                GdipDeletePrivateFontCollection_ptr = LoadFunction<GdipDeletePrivateFontCollection_delegate>("GdipDeletePrivateFontCollection");
                GdipPrivateAddFontFile_ptr = LoadFunction<GdipPrivateAddFontFile_delegate>("GdipPrivateAddFontFile");
                GdipPrivateAddMemoryFont_ptr = LoadFunction<GdipPrivateAddMemoryFont_delegate>("GdipPrivateAddMemoryFont");
                GdipCreateFontFamilyFromName_ptr = LoadFunction<GdipCreateFontFamilyFromName_delegate>("GdipCreateFontFamilyFromName");
                GdipGetGenericFontFamilySansSerif_ptr = LoadFunction<GdipGetGenericFontFamilySansSerif_delegate>("GdipGetGenericFontFamilySansSerif");
                GdipGetGenericFontFamilySerif_ptr = LoadFunction<GdipGetGenericFontFamilySerif_delegate>("GdipGetGenericFontFamilySerif");
                GdipGetGenericFontFamilyMonospace_ptr = LoadFunction<GdipGetGenericFontFamilyMonospace_delegate>("GdipGetGenericFontFamilyMonospace");
                GdipDeleteFontFamily_ptr = LoadFunction<GdipDeleteFontFamily_delegate>("GdipDeleteFontFamily");
                GdipGetFamilyName_ptr = LoadFunction<GdipGetFamilyName_delegate>("GdipGetFamilyName");
                GdipIsStyleAvailable_ptr = LoadFunction<GdipIsStyleAvailable_delegate>("GdipIsStyleAvailable");
                GdipGetEmHeight_ptr = LoadFunction<GdipGetEmHeight_delegate>("GdipGetEmHeight");
                GdipGetCellAscent_ptr = LoadFunction<GdipGetCellAscent_delegate>("GdipGetCellAscent");
                GdipGetCellDescent_ptr = LoadFunction<GdipGetCellDescent_delegate>("GdipGetCellDescent");
                GdipGetLineSpacing_ptr = LoadFunction<GdipGetLineSpacing_delegate>("GdipGetLineSpacing");
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
                GdipSetStringFormatMeasurableCharacterRanges_ptr = LoadFunction<GdipSetStringFormatMeasurableCharacterRanges_delegate>("GdipSetStringFormatMeasurableCharacterRanges");
                GdipCreateStringFormat_ptr = LoadFunction<GdipCreateStringFormat_delegate>("GdipCreateStringFormat");
                GdipStringFormatGetGenericDefault_ptr = LoadFunction<GdipStringFormatGetGenericDefault_delegate>("GdipStringFormatGetGenericDefault");
                GdipStringFormatGetGenericTypographic_ptr = LoadFunction<GdipStringFormatGetGenericTypographic_delegate>("GdipStringFormatGetGenericTypographic");
                GdipDeleteStringFormat_ptr = LoadFunction<GdipDeleteStringFormat_delegate>("GdipDeleteStringFormat");
                GdipCloneStringFormat_ptr = LoadFunction<GdipCloneStringFormat_delegate>("GdipCloneStringFormat");
                GdipSetStringFormatFlags_ptr = LoadFunction<GdipSetStringFormatFlags_delegate>("GdipSetStringFormatFlags");
                GdipGetStringFormatFlags_ptr = LoadFunction<GdipGetStringFormatFlags_delegate>("GdipGetStringFormatFlags");
                GdipSetStringFormatAlign_ptr = LoadFunction<GdipSetStringFormatAlign_delegate>("GdipSetStringFormatAlign");
                GdipGetStringFormatAlign_ptr = LoadFunction<GdipGetStringFormatAlign_delegate>("GdipGetStringFormatAlign");
                GdipSetStringFormatLineAlign_ptr = LoadFunction<GdipSetStringFormatLineAlign_delegate>("GdipSetStringFormatLineAlign");
                GdipGetStringFormatLineAlign_ptr = LoadFunction<GdipGetStringFormatLineAlign_delegate>("GdipGetStringFormatLineAlign");
                GdipSetStringFormatHotkeyPrefix_ptr = LoadFunction<GdipSetStringFormatHotkeyPrefix_delegate>("GdipSetStringFormatHotkeyPrefix");
                GdipGetStringFormatHotkeyPrefix_ptr = LoadFunction<GdipGetStringFormatHotkeyPrefix_delegate>("GdipGetStringFormatHotkeyPrefix");
                GdipSetStringFormatTabStops_ptr = LoadFunction<GdipSetStringFormatTabStops_delegate>("GdipSetStringFormatTabStops");
                GdipGetStringFormatTabStops_ptr = LoadFunction<GdipGetStringFormatTabStops_delegate>("GdipGetStringFormatTabStops");
                GdipGetStringFormatTabStopCount_ptr = LoadFunction<GdipGetStringFormatTabStopCount_delegate>("GdipGetStringFormatTabStopCount");
                GdipGetStringFormatMeasurableCharacterRangeCount_ptr = LoadFunction<GdipGetStringFormatMeasurableCharacterRangeCount_delegate>("GdipGetStringFormatMeasurableCharacterRangeCount");
                GdipSetStringFormatTrimming_ptr = LoadFunction<GdipSetStringFormatTrimming_delegate>("GdipSetStringFormatTrimming");
                GdipGetStringFormatTrimming_ptr = LoadFunction<GdipGetStringFormatTrimming_delegate>("GdipGetStringFormatTrimming");
                GdipSetStringFormatDigitSubstitution_ptr = LoadFunction<GdipSetStringFormatDigitSubstitution_delegate>("GdipSetStringFormatDigitSubstitution");
                GdipGetStringFormatDigitSubstitution_ptr = LoadFunction<GdipGetStringFormatDigitSubstitution_delegate>("GdipGetStringFormatDigitSubstitution");
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

            private delegate int GdipCreateMatrix_delegate(out IntPtr matrix);
            private static FunctionWrapper<GdipCreateMatrix_delegate> GdipCreateMatrix_ptr;
            internal static int GdipCreateMatrix(out IntPtr matrix) => GdipCreateMatrix_ptr.Delegate(out matrix);

            private delegate int GdipCreateMatrix2_delegate(float m11, float m12, float m21, float m22, float dx, float dy, out IntPtr matrix);
            private static FunctionWrapper<GdipCreateMatrix2_delegate> GdipCreateMatrix2_ptr;
            internal static int GdipCreateMatrix2(float m11, float m12, float m21, float m22, float dx, float dy, out IntPtr matrix) => GdipCreateMatrix2_ptr.Delegate(m11, m12, m21, m22, dx, dy, out matrix);

            private delegate int GdipCreateMatrix3_delegate(ref GPRECTF rect, HandleRef dstplg, out IntPtr matrix);
            private static FunctionWrapper<GdipCreateMatrix3_delegate> GdipCreateMatrix3_ptr;
            internal static int GdipCreateMatrix3(ref GPRECTF rect, HandleRef dstplg, out IntPtr matrix) => GdipCreateMatrix3_ptr.Delegate(ref rect, dstplg, out matrix);

            private delegate int GdipCreateMatrix3I_delegate(ref GPRECT rect, HandleRef dstplg, out IntPtr matrix);
            private static FunctionWrapper<GdipCreateMatrix3I_delegate> GdipCreateMatrix3I_ptr;
            internal static int GdipCreateMatrix3I(ref GPRECT rect, HandleRef dstplg, out IntPtr matrix) => GdipCreateMatrix3I_ptr.Delegate(ref rect, dstplg, out matrix);

            private delegate int GdipCloneMatrix_delegate(HandleRef matrix, out IntPtr cloneMatrix);
            private static FunctionWrapper<GdipCloneMatrix_delegate> GdipCloneMatrix_ptr;
            internal static int GdipCloneMatrix(HandleRef matrix, out IntPtr cloneMatrix) => GdipCloneMatrix_ptr.Delegate(matrix, out cloneMatrix);

            private delegate int GdipDeleteMatrix_delegate(HandleRef matrix);
            private static FunctionWrapper<GdipDeleteMatrix_delegate> GdipDeleteMatrix_ptr;
            internal static int IntGdipDeleteMatrix(HandleRef matrix) => GdipDeleteMatrix_ptr.Delegate(matrix);

            private delegate int GdipSetMatrixElements_delegate(HandleRef matrix, float m11, float m12, float m21, float m22, float dx, float dy);
            private static FunctionWrapper<GdipSetMatrixElements_delegate> GdipSetMatrixElements_ptr;
            internal static int GdipSetMatrixElements(HandleRef matrix, float m11, float m12, float m21, float m22, float dx, float dy) => GdipSetMatrixElements_ptr.Delegate(matrix, m11, m12, m21, m22, dx, dy);

            private delegate int GdipMultiplyMatrix_delegate(HandleRef matrix, HandleRef matrix2, MatrixOrder order);
            private static FunctionWrapper<GdipMultiplyMatrix_delegate> GdipMultiplyMatrix_ptr;
            internal static int GdipMultiplyMatrix(HandleRef matrix, HandleRef matrix2, MatrixOrder order) => GdipMultiplyMatrix_ptr.Delegate(matrix, matrix2, order);

            private delegate int GdipTranslateMatrix_delegate(HandleRef matrix, float offsetX, float offsetY, MatrixOrder order);
            private static FunctionWrapper<GdipTranslateMatrix_delegate> GdipTranslateMatrix_ptr;
            internal static int GdipTranslateMatrix(HandleRef matrix, float offsetX, float offsetY, MatrixOrder order) => GdipTranslateMatrix_ptr.Delegate(matrix, offsetX, offsetY, order);

            private delegate int GdipScaleMatrix_delegate(HandleRef matrix, float scaleX, float scaleY, MatrixOrder order);
            private static FunctionWrapper<GdipScaleMatrix_delegate> GdipScaleMatrix_ptr;
            internal static int GdipScaleMatrix(HandleRef matrix, float scaleX, float scaleY, MatrixOrder order) => GdipScaleMatrix_ptr.Delegate(matrix, scaleX, scaleY, order);

            private delegate int GdipRotateMatrix_delegate(HandleRef matrix, float angle, MatrixOrder order);
            private static FunctionWrapper<GdipRotateMatrix_delegate> GdipRotateMatrix_ptr;
            internal static int GdipRotateMatrix(HandleRef matrix, float angle, MatrixOrder order) => GdipRotateMatrix_ptr.Delegate(matrix, angle, order);

            private delegate int GdipShearMatrix_delegate(HandleRef matrix, float shearX, float shearY, MatrixOrder order);
            private static FunctionWrapper<GdipShearMatrix_delegate> GdipShearMatrix_ptr;
            internal static int GdipShearMatrix(HandleRef matrix, float shearX, float shearY, MatrixOrder order) => GdipShearMatrix_ptr.Delegate(matrix, shearX, shearY, order);

            private delegate int GdipInvertMatrix_delegate(HandleRef matrix);
            private static FunctionWrapper<GdipInvertMatrix_delegate> GdipInvertMatrix_ptr;
            internal static int GdipInvertMatrix(HandleRef matrix) => GdipInvertMatrix_ptr.Delegate(matrix);

            private delegate int GdipTransformMatrixPoints_delegate(HandleRef matrix, HandleRef pts, int count);
            private static FunctionWrapper<GdipTransformMatrixPoints_delegate> GdipTransformMatrixPoints_ptr;
            internal static int GdipTransformMatrixPoints(HandleRef matrix, HandleRef pts, int count) => GdipTransformMatrixPoints_ptr.Delegate(matrix, pts, count);

            private delegate int GdipTransformMatrixPointsI_delegate(HandleRef matrix, HandleRef pts, int count);
            private static FunctionWrapper<GdipTransformMatrixPointsI_delegate> GdipTransformMatrixPointsI_ptr;
            internal static int GdipTransformMatrixPointsI(HandleRef matrix, HandleRef pts, int count) => GdipTransformMatrixPointsI_ptr.Delegate(matrix, pts, count);

            private delegate int GdipVectorTransformMatrixPoints_delegate(HandleRef matrix, HandleRef pts, int count);
            private static FunctionWrapper<GdipVectorTransformMatrixPoints_delegate> GdipVectorTransformMatrixPoints_ptr;
            internal static int GdipVectorTransformMatrixPoints(HandleRef matrix, HandleRef pts, int count) => GdipVectorTransformMatrixPoints_ptr.Delegate(matrix, pts, count);

            private delegate int GdipVectorTransformMatrixPointsI_delegate(HandleRef matrix, HandleRef pts, int count);
            private static FunctionWrapper<GdipVectorTransformMatrixPointsI_delegate> GdipVectorTransformMatrixPointsI_ptr;
            internal static int GdipVectorTransformMatrixPointsI(HandleRef matrix, HandleRef pts, int count) => GdipVectorTransformMatrixPointsI_ptr.Delegate(matrix, pts, count);

            private delegate int GdipGetMatrixElements_delegate(HandleRef matrix, IntPtr m);
            private static FunctionWrapper<GdipGetMatrixElements_delegate> GdipGetMatrixElements_ptr;
            internal static int GdipGetMatrixElements(HandleRef matrix, IntPtr m) => GdipGetMatrixElements_ptr.Delegate(matrix, m);

            private delegate int GdipIsMatrixInvertible_delegate(HandleRef matrix, out int boolean);
            private static FunctionWrapper<GdipIsMatrixInvertible_delegate> GdipIsMatrixInvertible_ptr;
            internal static int GdipIsMatrixInvertible(HandleRef matrix, out int boolean) => GdipIsMatrixInvertible_ptr.Delegate(matrix, out boolean);

            private delegate int GdipIsMatrixIdentity_delegate(HandleRef matrix, out int boolean);
            private static FunctionWrapper<GdipIsMatrixIdentity_delegate> GdipIsMatrixIdentity_ptr;
            internal static int GdipIsMatrixIdentity(HandleRef matrix, out int boolean) => GdipIsMatrixIdentity_ptr.Delegate(matrix, out boolean);

            private delegate int GdipIsMatrixEqual_delegate(HandleRef matrix, HandleRef matrix2, out int boolean);
            private static FunctionWrapper<GdipIsMatrixEqual_delegate> GdipIsMatrixEqual_ptr;
            internal static int GdipIsMatrixEqual(HandleRef matrix, HandleRef matrix2, out int boolean) => GdipIsMatrixEqual_ptr.Delegate(matrix, matrix2, out boolean);

            private delegate int GdipCreateRegion_delegate(out IntPtr region);
            private static FunctionWrapper<GdipCreateRegion_delegate> GdipCreateRegion_ptr;
            internal static int GdipCreateRegion(out IntPtr region) => GdipCreateRegion_ptr.Delegate(out region);

            private delegate int GdipCreateRegionRect_delegate(ref GPRECTF gprectf, out IntPtr region);
            private static FunctionWrapper<GdipCreateRegionRect_delegate> GdipCreateRegionRect_ptr;
            internal static int GdipCreateRegionRect(ref GPRECTF gprectf, out IntPtr region) => GdipCreateRegionRect_ptr.Delegate(ref gprectf, out region);

            private delegate int GdipCreateRegionRectI_delegate(ref GPRECT gprect, out IntPtr region);
            private static FunctionWrapper<GdipCreateRegionRectI_delegate> GdipCreateRegionRectI_ptr;
            internal static int GdipCreateRegionRectI(ref GPRECT gprect, out IntPtr region) => GdipCreateRegionRectI_ptr.Delegate(ref gprect, out region);

            private delegate int GdipCreateRegionPath_delegate(HandleRef path, out IntPtr region);
            private static FunctionWrapper<GdipCreateRegionPath_delegate> GdipCreateRegionPath_ptr;
            internal static int GdipCreateRegionPath(HandleRef path, out IntPtr region) => GdipCreateRegionPath_ptr.Delegate(path, out region);

            private delegate int GdipCreateRegionRgnData_delegate(byte[] rgndata, int size, out IntPtr region);
            private static FunctionWrapper<GdipCreateRegionRgnData_delegate> GdipCreateRegionRgnData_ptr;
            internal static int GdipCreateRegionRgnData(byte[] rgndata, int size, out IntPtr region) => GdipCreateRegionRgnData_ptr.Delegate(rgndata, size, out region);

            private delegate int GdipCreateRegionHrgn_delegate(HandleRef hRgn, out IntPtr region);
            private static FunctionWrapper<GdipCreateRegionHrgn_delegate> GdipCreateRegionHrgn_ptr;
            internal static int GdipCreateRegionHrgn(HandleRef hRgn, out IntPtr region) => GdipCreateRegionHrgn_ptr.Delegate(hRgn, out region);

            private delegate int GdipCloneRegion_delegate(HandleRef region, out IntPtr cloneregion);
            private static FunctionWrapper<GdipCloneRegion_delegate> GdipCloneRegion_ptr;
            internal static int GdipCloneRegion(HandleRef region, out IntPtr cloneregion) => GdipCloneRegion_ptr.Delegate(region, out cloneregion);

            private delegate int GdipDeleteRegion_delegate(HandleRef region);
            private static FunctionWrapper<GdipDeleteRegion_delegate> GdipDeleteRegion_ptr;
            internal static int IntGdipDeleteRegion(HandleRef region) => GdipDeleteRegion_ptr.Delegate(region);

            private delegate int GdipSetInfinite_delegate(HandleRef region);
            private static FunctionWrapper<GdipSetInfinite_delegate> GdipSetInfinite_ptr;
            internal static int GdipSetInfinite(HandleRef region) => GdipSetInfinite_ptr.Delegate(region);

            private delegate int GdipSetEmpty_delegate(HandleRef region);
            private static FunctionWrapper<GdipSetEmpty_delegate> GdipSetEmpty_ptr;
            internal static int GdipSetEmpty(HandleRef region) => GdipSetEmpty_ptr.Delegate(region);

            private delegate int GdipCombineRegionRect_delegate(HandleRef region, ref GPRECTF gprectf, CombineMode mode);
            private static FunctionWrapper<GdipCombineRegionRect_delegate> GdipCombineRegionRect_ptr;
            internal static int GdipCombineRegionRect(HandleRef region, ref GPRECTF gprectf, CombineMode mode) => GdipCombineRegionRect_ptr.Delegate(region, ref gprectf, mode);

            private delegate int GdipCombineRegionRectI_delegate(HandleRef region, ref GPRECT gprect, CombineMode mode);
            private static FunctionWrapper<GdipCombineRegionRectI_delegate> GdipCombineRegionRectI_ptr;
            internal static int GdipCombineRegionRectI(HandleRef region, ref GPRECT gprect, CombineMode mode) => GdipCombineRegionRectI_ptr.Delegate(region, ref gprect, mode);

            private delegate int GdipCombineRegionPath_delegate(HandleRef region, HandleRef path, CombineMode mode);
            private static FunctionWrapper<GdipCombineRegionPath_delegate> GdipCombineRegionPath_ptr;
            internal static int GdipCombineRegionPath(HandleRef region, HandleRef path, CombineMode mode) => GdipCombineRegionPath_ptr.Delegate(region, path, mode);

            private delegate int GdipCombineRegionRegion_delegate(HandleRef region, HandleRef region2, CombineMode mode);
            private static FunctionWrapper<GdipCombineRegionRegion_delegate> GdipCombineRegionRegion_ptr;
            internal static int GdipCombineRegionRegion(HandleRef region, HandleRef region2, CombineMode mode) => GdipCombineRegionRegion_ptr.Delegate(region, region2, mode);

            private delegate int GdipTranslateRegion_delegate(HandleRef region, float dx, float dy);
            private static FunctionWrapper<GdipTranslateRegion_delegate> GdipTranslateRegion_ptr;
            internal static int GdipTranslateRegion(HandleRef region, float dx, float dy) => GdipTranslateRegion_ptr.Delegate(region, dx, dy);

            private delegate int GdipTranslateRegionI_delegate(HandleRef region, int dx, int dy);
            private static FunctionWrapper<GdipTranslateRegionI_delegate> GdipTranslateRegionI_ptr;
            internal static int GdipTranslateRegionI(HandleRef region, int dx, int dy) => GdipTranslateRegionI_ptr.Delegate(region, dx, dy);

            private delegate int GdipTransformRegion_delegate(HandleRef region, HandleRef matrix);
            private static FunctionWrapper<GdipTransformRegion_delegate> GdipTransformRegion_ptr;
            internal static int GdipTransformRegion(HandleRef region, HandleRef matrix) => GdipTransformRegion_ptr.Delegate(region, matrix);

            private delegate int GdipGetRegionBounds_delegate(HandleRef region, HandleRef graphics, ref GPRECTF gprectf);
            private static FunctionWrapper<GdipGetRegionBounds_delegate> GdipGetRegionBounds_ptr;
            internal static int GdipGetRegionBounds(HandleRef region, HandleRef graphics, ref GPRECTF gprectf) => GdipGetRegionBounds_ptr.Delegate(region, graphics, ref gprectf);

            private delegate int GdipGetRegionHRgn_delegate(HandleRef region, HandleRef graphics, out IntPtr hrgn);
            private static FunctionWrapper<GdipGetRegionHRgn_delegate> GdipGetRegionHRgn_ptr;
            internal static int GdipGetRegionHRgn(HandleRef region, HandleRef graphics, out IntPtr hrgn) => GdipGetRegionHRgn_ptr.Delegate(region, graphics, out hrgn);

            private delegate int GdipIsEmptyRegion_delegate(HandleRef region, HandleRef graphics, out int boolean);
            private static FunctionWrapper<GdipIsEmptyRegion_delegate> GdipIsEmptyRegion_ptr;
            internal static int GdipIsEmptyRegion(HandleRef region, HandleRef graphics, out int boolean) => GdipIsEmptyRegion_ptr.Delegate(region, graphics, out boolean);

            private delegate int GdipIsInfiniteRegion_delegate(HandleRef region, HandleRef graphics, out int boolean);
            private static FunctionWrapper<GdipIsInfiniteRegion_delegate> GdipIsInfiniteRegion_ptr;
            internal static int GdipIsInfiniteRegion(HandleRef region, HandleRef graphics, out int boolean) => GdipIsInfiniteRegion_ptr.Delegate(region, graphics, out boolean);

            private delegate int GdipIsEqualRegion_delegate(HandleRef region, HandleRef region2, HandleRef graphics, out int boolean);
            private static FunctionWrapper<GdipIsEqualRegion_delegate> GdipIsEqualRegion_ptr;
            internal static int GdipIsEqualRegion(HandleRef region, HandleRef region2, HandleRef graphics, out int boolean) => GdipIsEqualRegion_ptr.Delegate(region, region2, graphics, out boolean);

            private delegate int GdipGetRegionDataSize_delegate(HandleRef region, out int bufferSize);
            private static FunctionWrapper<GdipGetRegionDataSize_delegate> GdipGetRegionDataSize_ptr;
            internal static int GdipGetRegionDataSize(HandleRef region, out int bufferSize) => GdipGetRegionDataSize_ptr.Delegate(region, out bufferSize);

            private delegate int GdipGetRegionData_delegate(HandleRef region, byte[] regionData, int bufferSize, out int sizeFilled);
            private static FunctionWrapper<GdipGetRegionData_delegate> GdipGetRegionData_ptr;
            internal static int GdipGetRegionData(HandleRef region, byte[] regionData, int bufferSize, out int sizeFilled) => GdipGetRegionData_ptr.Delegate(region, regionData, bufferSize, out sizeFilled);

            private delegate int GdipIsVisibleRegionPoint_delegate(HandleRef region, float X, float Y, HandleRef graphics, out int boolean);
            private static FunctionWrapper<GdipIsVisibleRegionPoint_delegate> GdipIsVisibleRegionPoint_ptr;
            internal static int GdipIsVisibleRegionPoint(HandleRef region, float X, float Y, HandleRef graphics, out int boolean) => GdipIsVisibleRegionPoint_ptr.Delegate(region, X, Y, graphics, out boolean);

            private delegate int GdipIsVisibleRegionPointI_delegate(HandleRef region, int X, int Y, HandleRef graphics, out int boolean);
            private static FunctionWrapper<GdipIsVisibleRegionPointI_delegate> GdipIsVisibleRegionPointI_ptr;
            internal static int GdipIsVisibleRegionPointI(HandleRef region, int X, int Y, HandleRef graphics, out int boolean) => GdipIsVisibleRegionPointI_ptr.Delegate(region, X, Y, graphics, out boolean);

            private delegate int GdipIsVisibleRegionRect_delegate(HandleRef region, float X, float Y, float width, float height, HandleRef graphics, out int boolean);
            private static FunctionWrapper<GdipIsVisibleRegionRect_delegate> GdipIsVisibleRegionRect_ptr;
            internal static int GdipIsVisibleRegionRect(HandleRef region, float X, float Y, float width, float height, HandleRef graphics, out int boolean) => GdipIsVisibleRegionRect_ptr.Delegate(region, X, Y, width, height, graphics, out boolean);

            private delegate int GdipIsVisibleRegionRectI_delegate(HandleRef region, int X, int Y, int width, int height, HandleRef graphics, out int boolean);
            private static FunctionWrapper<GdipIsVisibleRegionRectI_delegate> GdipIsVisibleRegionRectI_ptr;
            internal static int GdipIsVisibleRegionRectI(HandleRef region, int X, int Y, int width, int height, HandleRef graphics, out int boolean) => GdipIsVisibleRegionRectI_ptr.Delegate(region, X, Y, width, height, graphics, out boolean);

            private delegate int GdipGetRegionScansCount_delegate(HandleRef region, out int count, HandleRef matrix);
            private static FunctionWrapper<GdipGetRegionScansCount_delegate> GdipGetRegionScansCount_ptr;
            internal static int GdipGetRegionScansCount(HandleRef region, out int count, HandleRef matrix) => GdipGetRegionScansCount_ptr.Delegate(region, out count, matrix);

            private delegate int GdipGetRegionScans_delegate(HandleRef region, IntPtr rects, out int count, HandleRef matrix);
            private static FunctionWrapper<GdipGetRegionScans_delegate> GdipGetRegionScans_ptr;
            internal static int GdipGetRegionScans(HandleRef region, IntPtr rects, out int count, HandleRef matrix) => GdipGetRegionScans_ptr.Delegate(region, rects, out count, matrix);

            private delegate int GdipDeleteBrush_delegate(HandleRef brush);
            private static FunctionWrapper<GdipDeleteBrush_delegate> GdipDeleteBrush_ptr;
            internal static int IntGdipDeleteBrush(HandleRef brush) => GdipDeleteBrush_ptr.Delegate(brush);

            private delegate int GdipCreateLineBrush_delegate(GPPOINTF point1, GPPOINTF point2, int color1, int color2, int wrapMode, out IntPtr lineGradient);
            private static FunctionWrapper<GdipCreateLineBrush_delegate> GdipCreateLineBrush_ptr;
            internal static int GdipCreateLineBrush(GPPOINTF point1, GPPOINTF point2, int color1, int color2, int wrapMode, out IntPtr lineGradient) => GdipCreateLineBrush_ptr.Delegate(point1, point2, color1, color2, wrapMode, out lineGradient);

            private delegate int GdipCreateLineBrushI_delegate(GPPOINT point1, GPPOINT point2, int color1, int color2, int wrapMode, out IntPtr lineGradient);
            private static FunctionWrapper<GdipCreateLineBrushI_delegate> GdipCreateLineBrushI_ptr;
            internal static int GdipCreateLineBrushI(GPPOINT point1, GPPOINT point2, int color1, int color2, int wrapMode, out IntPtr lineGradient) => GdipCreateLineBrushI_ptr.Delegate(point1, point2, color1, color2, wrapMode, out lineGradient);

            private delegate int GdipCreateLineBrushFromRect_delegate(ref GPRECTF rect, int color1, int color2, int lineGradientMode, int wrapMode, out IntPtr lineGradient);
            private static FunctionWrapper<GdipCreateLineBrushFromRect_delegate> GdipCreateLineBrushFromRect_ptr;
            internal static int GdipCreateLineBrushFromRect(ref GPRECTF rect, int color1, int color2, int lineGradientMode, int wrapMode, out IntPtr lineGradient) => GdipCreateLineBrushFromRect_ptr.Delegate(ref rect, color1, color2, lineGradientMode, wrapMode, out lineGradient);

            private delegate int GdipCreateLineBrushFromRectI_delegate(ref GPRECT rect, int color1, int color2, int lineGradientMode, int wrapMode, out IntPtr lineGradient);
            private static FunctionWrapper<GdipCreateLineBrushFromRectI_delegate> GdipCreateLineBrushFromRectI_ptr;
            internal static int GdipCreateLineBrushFromRectI(ref GPRECT rect, int color1, int color2, int lineGradientMode, int wrapMode, out IntPtr lineGradient) => GdipCreateLineBrushFromRectI_ptr.Delegate(ref rect, color1, color2, lineGradientMode, wrapMode, out lineGradient);

            private delegate int GdipCreateLineBrushFromRectWithAngle_delegate(ref GPRECTF rect, int color1, int color2, float angle, bool isAngleScaleable, int wrapMode, out IntPtr lineGradient);
            private static FunctionWrapper<GdipCreateLineBrushFromRectWithAngle_delegate> GdipCreateLineBrushFromRectWithAngle_ptr;
            internal static int GdipCreateLineBrushFromRectWithAngle(ref GPRECTF rect, int color1, int color2, float angle, bool isAngleScaleable, int wrapMode, out IntPtr lineGradient) => GdipCreateLineBrushFromRectWithAngle_ptr.Delegate(ref rect, color1, color2, angle, isAngleScaleable, wrapMode, out lineGradient);

            private delegate int GdipCreateLineBrushFromRectWithAngleI_delegate(ref GPRECT rect, int color1, int color2, float angle, bool isAngleScaleable, int wrapMode, out IntPtr lineGradient);
            private static FunctionWrapper<GdipCreateLineBrushFromRectWithAngleI_delegate> GdipCreateLineBrushFromRectWithAngleI_ptr;
            internal static int GdipCreateLineBrushFromRectWithAngleI(ref GPRECT rect, int color1, int color2, float angle, bool isAngleScaleable, int wrapMode, out IntPtr lineGradient) => GdipCreateLineBrushFromRectWithAngleI_ptr.Delegate(ref rect, color1, color2, angle, isAngleScaleable, wrapMode, out lineGradient);

            private delegate int GdipSetLineColors_delegate(HandleRef brush, int color1, int color2);
            private static FunctionWrapper<GdipSetLineColors_delegate> GdipSetLineColors_ptr;
            internal static int GdipSetLineColors(HandleRef brush, int color1, int color2) => GdipSetLineColors_ptr.Delegate(brush, color1, color2);

            private delegate int GdipGetLineColors_delegate(HandleRef brush, int[] colors);
            private static FunctionWrapper<GdipGetLineColors_delegate> GdipGetLineColors_ptr;
            internal static int GdipGetLineColors(HandleRef brush, int[] colors) => GdipGetLineColors_ptr.Delegate(brush, colors);

            private delegate int GdipGetLineRect_delegate(HandleRef brush, ref GPRECTF gprectf);
            private static FunctionWrapper<GdipGetLineRect_delegate> GdipGetLineRect_ptr;
            internal static int GdipGetLineRect(HandleRef brush, ref GPRECTF gprectf) => GdipGetLineRect_ptr.Delegate(brush, ref gprectf);

            private delegate int GdipGetLineGammaCorrection_delegate(HandleRef brush, out bool useGammaCorrection);
            private static FunctionWrapper<GdipGetLineGammaCorrection_delegate> GdipGetLineGammaCorrection_ptr;
            internal static int GdipGetLineGammaCorrection(HandleRef brush, out bool useGammaCorrection) => GdipGetLineGammaCorrection_ptr.Delegate(brush, out useGammaCorrection);

            private delegate int GdipSetLineGammaCorrection_delegate(HandleRef brush, bool useGammaCorrection);
            private static FunctionWrapper<GdipSetLineGammaCorrection_delegate> GdipSetLineGammaCorrection_ptr;
            internal static int GdipSetLineGammaCorrection(HandleRef brush, bool useGammaCorrection) => GdipSetLineGammaCorrection_ptr.Delegate(brush, useGammaCorrection);

            private delegate int GdipSetLineSigmaBlend_delegate(HandleRef brush, float focus, float scale);
            private static FunctionWrapper<GdipSetLineSigmaBlend_delegate> GdipSetLineSigmaBlend_ptr;
            internal static int GdipSetLineSigmaBlend(HandleRef brush, float focus, float scale) => GdipSetLineSigmaBlend_ptr.Delegate(brush, focus, scale);

            private delegate int GdipSetLineLinearBlend_delegate(HandleRef brush, float focus, float scale);
            private static FunctionWrapper<GdipSetLineLinearBlend_delegate> GdipSetLineLinearBlend_ptr;
            internal static int GdipSetLineLinearBlend(HandleRef brush, float focus, float scale) => GdipSetLineLinearBlend_ptr.Delegate(brush, focus, scale);

            private delegate int GdipGetLineBlendCount_delegate(HandleRef brush, out int count);
            private static FunctionWrapper<GdipGetLineBlendCount_delegate> GdipGetLineBlendCount_ptr;
            internal static int GdipGetLineBlendCount(HandleRef brush, out int count) => GdipGetLineBlendCount_ptr.Delegate(brush, out count);

            private delegate int GdipGetLineBlend_delegate(HandleRef brush, IntPtr blend, IntPtr positions, int count);
            private static FunctionWrapper<GdipGetLineBlend_delegate> GdipGetLineBlend_ptr;
            internal static int GdipGetLineBlend(HandleRef brush, IntPtr blend, IntPtr positions, int count) => GdipGetLineBlend_ptr.Delegate(brush, blend, positions, count);

            private delegate int GdipSetLineBlend_delegate(HandleRef brush, HandleRef blend, HandleRef positions, int count);
            private static FunctionWrapper<GdipSetLineBlend_delegate> GdipSetLineBlend_ptr;
            internal static int GdipSetLineBlend(HandleRef brush, HandleRef blend, HandleRef positions, int count) => GdipSetLineBlend_ptr.Delegate(brush, blend, positions, count);

            private delegate int GdipGetLinePresetBlendCount_delegate(HandleRef brush, out int count);
            private static FunctionWrapper<GdipGetLinePresetBlendCount_delegate> GdipGetLinePresetBlendCount_ptr;
            internal static int GdipGetLinePresetBlendCount(HandleRef brush, out int count) => GdipGetLinePresetBlendCount_ptr.Delegate(brush, out count);

            private delegate int GdipGetLinePresetBlend_delegate(HandleRef brush, IntPtr blend, IntPtr positions, int count);
            private static FunctionWrapper<GdipGetLinePresetBlend_delegate> GdipGetLinePresetBlend_ptr;
            internal static int GdipGetLinePresetBlend(HandleRef brush, IntPtr blend, IntPtr positions, int count) => GdipGetLinePresetBlend_ptr.Delegate(brush, blend, positions, count);

            private delegate int GdipSetLinePresetBlend_delegate(HandleRef brush, HandleRef blend, HandleRef positions, int count);
            private static FunctionWrapper<GdipSetLinePresetBlend_delegate> GdipSetLinePresetBlend_ptr;
            internal static int GdipSetLinePresetBlend(HandleRef brush, HandleRef blend, HandleRef positions, int count) => GdipSetLinePresetBlend_ptr.Delegate(brush, blend, positions, count);

            private delegate int GdipSetLineWrapMode_delegate(HandleRef brush, int wrapMode);
            private static FunctionWrapper<GdipSetLineWrapMode_delegate> GdipSetLineWrapMode_ptr;
            internal static int GdipSetLineWrapMode(HandleRef brush, int wrapMode) => GdipSetLineWrapMode_ptr.Delegate(brush, wrapMode);

            private delegate int GdipGetLineWrapMode_delegate(HandleRef brush, out int wrapMode);
            private static FunctionWrapper<GdipGetLineWrapMode_delegate> GdipGetLineWrapMode_ptr;
            internal static int GdipGetLineWrapMode(HandleRef brush, out int wrapMode) => GdipGetLineWrapMode_ptr.Delegate(brush, out wrapMode);

            private delegate int GdipResetLineTransform_delegate(HandleRef brush);
            private static FunctionWrapper<GdipResetLineTransform_delegate> GdipResetLineTransform_ptr;
            internal static int GdipResetLineTransform(HandleRef brush) => GdipResetLineTransform_ptr.Delegate(brush);

            private delegate int GdipMultiplyLineTransform_delegate(HandleRef brush, HandleRef matrix, MatrixOrder order);
            private static FunctionWrapper<GdipMultiplyLineTransform_delegate> GdipMultiplyLineTransform_ptr;
            internal static int GdipMultiplyLineTransform(HandleRef brush, HandleRef matrix, MatrixOrder order) => GdipMultiplyLineTransform_ptr.Delegate(brush, matrix, order);

            private delegate int GdipGetLineTransform_delegate(HandleRef brush, HandleRef matrix);
            private static FunctionWrapper<GdipGetLineTransform_delegate> GdipGetLineTransform_ptr;
            internal static int GdipGetLineTransform(HandleRef brush, HandleRef matrix) => GdipGetLineTransform_ptr.Delegate(brush, matrix);

            private delegate int GdipSetLineTransform_delegate(HandleRef brush, HandleRef matrix);
            private static FunctionWrapper<GdipSetLineTransform_delegate> GdipSetLineTransform_ptr;
            internal static int GdipSetLineTransform(HandleRef brush, HandleRef matrix) => GdipSetLineTransform_ptr.Delegate(brush, matrix);

            private delegate int GdipTranslateLineTransform_delegate(HandleRef brush, float dx, float dy, MatrixOrder order);
            private static FunctionWrapper<GdipTranslateLineTransform_delegate> GdipTranslateLineTransform_ptr;
            internal static int GdipTranslateLineTransform(HandleRef brush, float dx, float dy, MatrixOrder order) => GdipTranslateLineTransform_ptr.Delegate(brush, dx, dy, order);

            private delegate int GdipScaleLineTransform_delegate(HandleRef brush, float sx, float sy, MatrixOrder order);
            private static FunctionWrapper<GdipScaleLineTransform_delegate> GdipScaleLineTransform_ptr;
            internal static int GdipScaleLineTransform(HandleRef brush, float sx, float sy, MatrixOrder order) => GdipScaleLineTransform_ptr.Delegate(brush, sx, sy, order);

            private delegate int GdipRotateLineTransform_delegate(HandleRef brush, float angle, MatrixOrder order);
            private static FunctionWrapper<GdipRotateLineTransform_delegate> GdipRotateLineTransform_ptr;
            internal static int GdipRotateLineTransform(HandleRef brush, float angle, MatrixOrder order) => GdipRotateLineTransform_ptr.Delegate(brush, angle, order);

            private delegate int GdipCreatePathGradient_delegate(HandleRef points, int count, int wrapMode, out IntPtr brush);
            private static FunctionWrapper<GdipCreatePathGradient_delegate> GdipCreatePathGradient_ptr;
            internal static int GdipCreatePathGradient(HandleRef points, int count, int wrapMode, out IntPtr brush) => GdipCreatePathGradient_ptr.Delegate(points, count, wrapMode, out brush);

            private delegate int GdipCreatePathGradientI_delegate(HandleRef points, int count, int wrapMode, out IntPtr brush);
            private static FunctionWrapper<GdipCreatePathGradientI_delegate> GdipCreatePathGradientI_ptr;
            internal static int GdipCreatePathGradientI(HandleRef points, int count, int wrapMode, out IntPtr brush) => GdipCreatePathGradientI_ptr.Delegate(points, count, wrapMode, out brush);

            private delegate int GdipCreatePathGradientFromPath_delegate(HandleRef path, out IntPtr brush);
            private static FunctionWrapper<GdipCreatePathGradientFromPath_delegate> GdipCreatePathGradientFromPath_ptr;
            internal static int GdipCreatePathGradientFromPath(HandleRef path, out IntPtr brush) => GdipCreatePathGradientFromPath_ptr.Delegate(path, out brush);

            private delegate int GdipGetPathGradientCenterColor_delegate(HandleRef brush, out int color);
            private static FunctionWrapper<GdipGetPathGradientCenterColor_delegate> GdipGetPathGradientCenterColor_ptr;
            internal static int GdipGetPathGradientCenterColor(HandleRef brush, out int color) => GdipGetPathGradientCenterColor_ptr.Delegate(brush, out color);

            private delegate int GdipSetPathGradientCenterColor_delegate(HandleRef brush, int color);
            private static FunctionWrapper<GdipSetPathGradientCenterColor_delegate> GdipSetPathGradientCenterColor_ptr;
            internal static int GdipSetPathGradientCenterColor(HandleRef brush, int color) => GdipSetPathGradientCenterColor_ptr.Delegate(brush, color);

            private delegate int GdipGetPathGradientSurroundColorsWithCount_delegate(HandleRef brush, int[] color, ref int count);
            private static FunctionWrapper<GdipGetPathGradientSurroundColorsWithCount_delegate> GdipGetPathGradientSurroundColorsWithCount_ptr;
            internal static int GdipGetPathGradientSurroundColorsWithCount(HandleRef brush, int[] color, ref int count) => GdipGetPathGradientSurroundColorsWithCount_ptr.Delegate(brush, color, ref count);

            private delegate int GdipSetPathGradientSurroundColorsWithCount_delegate(HandleRef brush, int[] argb, ref int count);
            private static FunctionWrapper<GdipSetPathGradientSurroundColorsWithCount_delegate> GdipSetPathGradientSurroundColorsWithCount_ptr;
            internal static int GdipSetPathGradientSurroundColorsWithCount(HandleRef brush, int[] argb, ref int count) => GdipSetPathGradientSurroundColorsWithCount_ptr.Delegate(brush, argb, ref count);

            private delegate int GdipGetPathGradientCenterPoint_delegate(HandleRef brush, GPPOINTF point);
            private static FunctionWrapper<GdipGetPathGradientCenterPoint_delegate> GdipGetPathGradientCenterPoint_ptr;
            internal static int GdipGetPathGradientCenterPoint(HandleRef brush, GPPOINTF point) => GdipGetPathGradientCenterPoint_ptr.Delegate(brush, point);

            private delegate int GdipSetPathGradientCenterPoint_delegate(HandleRef brush, GPPOINTF point);
            private static FunctionWrapper<GdipSetPathGradientCenterPoint_delegate> GdipSetPathGradientCenterPoint_ptr;
            internal static int GdipSetPathGradientCenterPoint(HandleRef brush, GPPOINTF point) => GdipSetPathGradientCenterPoint_ptr.Delegate(brush, point);

            private delegate int GdipGetPathGradientRect_delegate(HandleRef brush, ref GPRECTF gprectf);
            private static FunctionWrapper<GdipGetPathGradientRect_delegate> GdipGetPathGradientRect_ptr;
            internal static int GdipGetPathGradientRect(HandleRef brush, ref GPRECTF gprectf) => GdipGetPathGradientRect_ptr.Delegate(brush, ref gprectf);

            private delegate int GdipGetPathGradientPointCount_delegate(HandleRef brush, out int count);
            private static FunctionWrapper<GdipGetPathGradientPointCount_delegate> GdipGetPathGradientPointCount_ptr;
            internal static int GdipGetPathGradientPointCount(HandleRef brush, out int count) => GdipGetPathGradientPointCount_ptr.Delegate(brush, out count);

            private delegate int GdipGetPathGradientSurroundColorCount_delegate(HandleRef brush, out int count);
            private static FunctionWrapper<GdipGetPathGradientSurroundColorCount_delegate> GdipGetPathGradientSurroundColorCount_ptr;
            internal static int GdipGetPathGradientSurroundColorCount(HandleRef brush, out int count) => GdipGetPathGradientSurroundColorCount_ptr.Delegate(brush, out count);

            private delegate int GdipGetPathGradientBlendCount_delegate(HandleRef brush, out int count);
            private static FunctionWrapper<GdipGetPathGradientBlendCount_delegate> GdipGetPathGradientBlendCount_ptr;
            internal static int GdipGetPathGradientBlendCount(HandleRef brush, out int count) => GdipGetPathGradientBlendCount_ptr.Delegate(brush, out count);

            private delegate int GdipGetPathGradientBlend_delegate(HandleRef brush, IntPtr blend, IntPtr positions, int count);
            private static FunctionWrapper<GdipGetPathGradientBlend_delegate> GdipGetPathGradientBlend_ptr;
            internal static int GdipGetPathGradientBlend(HandleRef brush, IntPtr blend, IntPtr positions, int count) => GdipGetPathGradientBlend_ptr.Delegate(brush, blend, positions, count);

            private delegate int GdipSetPathGradientBlend_delegate(HandleRef brush, HandleRef blend, HandleRef positions, int count);
            private static FunctionWrapper<GdipSetPathGradientBlend_delegate> GdipSetPathGradientBlend_ptr;
            internal static int GdipSetPathGradientBlend(HandleRef brush, HandleRef blend, HandleRef positions, int count) => GdipSetPathGradientBlend_ptr.Delegate(brush, blend, positions, count);

            private delegate int GdipGetPathGradientPresetBlendCount_delegate(HandleRef brush, out int count);
            private static FunctionWrapper<GdipGetPathGradientPresetBlendCount_delegate> GdipGetPathGradientPresetBlendCount_ptr;
            internal static int GdipGetPathGradientPresetBlendCount(HandleRef brush, out int count) => GdipGetPathGradientPresetBlendCount_ptr.Delegate(brush, out count);

            private delegate int GdipGetPathGradientPresetBlend_delegate(HandleRef brush, IntPtr blend, IntPtr positions, int count);
            private static FunctionWrapper<GdipGetPathGradientPresetBlend_delegate> GdipGetPathGradientPresetBlend_ptr;
            internal static int GdipGetPathGradientPresetBlend(HandleRef brush, IntPtr blend, IntPtr positions, int count) => GdipGetPathGradientPresetBlend_ptr.Delegate(brush, blend, positions, count);

            private delegate int GdipSetPathGradientPresetBlend_delegate(HandleRef brush, HandleRef blend, HandleRef positions, int count);
            private static FunctionWrapper<GdipSetPathGradientPresetBlend_delegate> GdipSetPathGradientPresetBlend_ptr;
            internal static int GdipSetPathGradientPresetBlend(HandleRef brush, HandleRef blend, HandleRef positions, int count) => GdipSetPathGradientPresetBlend_ptr.Delegate(brush, blend, positions, count);

            private delegate int GdipSetPathGradientSigmaBlend_delegate(HandleRef brush, float focus, float scale);
            private static FunctionWrapper<GdipSetPathGradientSigmaBlend_delegate> GdipSetPathGradientSigmaBlend_ptr;
            internal static int GdipSetPathGradientSigmaBlend(HandleRef brush, float focus, float scale) => GdipSetPathGradientSigmaBlend_ptr.Delegate(brush, focus, scale);

            private delegate int GdipSetPathGradientLinearBlend_delegate(HandleRef brush, float focus, float scale);
            private static FunctionWrapper<GdipSetPathGradientLinearBlend_delegate> GdipSetPathGradientLinearBlend_ptr;
            internal static int GdipSetPathGradientLinearBlend(HandleRef brush, float focus, float scale) => GdipSetPathGradientLinearBlend_ptr.Delegate(brush, focus, scale);

            private delegate int GdipSetPathGradientWrapMode_delegate(HandleRef brush, int wrapmode);
            private static FunctionWrapper<GdipSetPathGradientWrapMode_delegate> GdipSetPathGradientWrapMode_ptr;
            internal static int GdipSetPathGradientWrapMode(HandleRef brush, int wrapmode) => GdipSetPathGradientWrapMode_ptr.Delegate(brush, wrapmode);

            private delegate int GdipGetPathGradientWrapMode_delegate(HandleRef brush, out int wrapmode);
            private static FunctionWrapper<GdipGetPathGradientWrapMode_delegate> GdipGetPathGradientWrapMode_ptr;
            internal static int GdipGetPathGradientWrapMode(HandleRef brush, out int wrapmode) => GdipGetPathGradientWrapMode_ptr.Delegate(brush, out wrapmode);

            private delegate int GdipSetPathGradientTransform_delegate(HandleRef brush, HandleRef matrix);
            private static FunctionWrapper<GdipSetPathGradientTransform_delegate> GdipSetPathGradientTransform_ptr;
            internal static int GdipSetPathGradientTransform(HandleRef brush, HandleRef matrix) => GdipSetPathGradientTransform_ptr.Delegate(brush, matrix);

            private delegate int GdipGetPathGradientTransform_delegate(HandleRef brush, HandleRef matrix);
            private static FunctionWrapper<GdipGetPathGradientTransform_delegate> GdipGetPathGradientTransform_ptr;
            internal static int GdipGetPathGradientTransform(HandleRef brush, HandleRef matrix) => GdipGetPathGradientTransform_ptr.Delegate(brush, matrix);

            private delegate int GdipResetPathGradientTransform_delegate(HandleRef brush);
            private static FunctionWrapper<GdipResetPathGradientTransform_delegate> GdipResetPathGradientTransform_ptr;
            internal static int GdipResetPathGradientTransform(HandleRef brush) => GdipResetPathGradientTransform_ptr.Delegate(brush);

            private delegate int GdipMultiplyPathGradientTransform_delegate(HandleRef brush, HandleRef matrix, MatrixOrder order);
            private static FunctionWrapper<GdipMultiplyPathGradientTransform_delegate> GdipMultiplyPathGradientTransform_ptr;
            internal static int GdipMultiplyPathGradientTransform(HandleRef brush, HandleRef matrix, MatrixOrder order) => GdipMultiplyPathGradientTransform_ptr.Delegate(brush, matrix, order);

            private delegate int GdipTranslatePathGradientTransform_delegate(HandleRef brush, float dx, float dy, MatrixOrder order);
            private static FunctionWrapper<GdipTranslatePathGradientTransform_delegate> GdipTranslatePathGradientTransform_ptr;
            internal static int GdipTranslatePathGradientTransform(HandleRef brush, float dx, float dy, MatrixOrder order) => GdipTranslatePathGradientTransform_ptr.Delegate(brush, dx, dy, order);

            private delegate int GdipScalePathGradientTransform_delegate(HandleRef brush, float sx, float sy, MatrixOrder order);
            private static FunctionWrapper<GdipScalePathGradientTransform_delegate> GdipScalePathGradientTransform_ptr;
            internal static int GdipScalePathGradientTransform(HandleRef brush, float sx, float sy, MatrixOrder order) => GdipScalePathGradientTransform_ptr.Delegate(brush, sx, sy, order);

            private delegate int GdipRotatePathGradientTransform_delegate(HandleRef brush, float angle, MatrixOrder order);
            private static FunctionWrapper<GdipRotatePathGradientTransform_delegate> GdipRotatePathGradientTransform_ptr;
            internal static int GdipRotatePathGradientTransform(HandleRef brush, float angle, MatrixOrder order) => GdipRotatePathGradientTransform_ptr.Delegate(brush, angle, order);

            private delegate int GdipGetPathGradientFocusScales_delegate(HandleRef brush, float[] xScale, float[] yScale);
            private static FunctionWrapper<GdipGetPathGradientFocusScales_delegate> GdipGetPathGradientFocusScales_ptr;
            internal static int GdipGetPathGradientFocusScales(HandleRef brush, float[] xScale, float[] yScale) => GdipGetPathGradientFocusScales_ptr.Delegate(brush, xScale, yScale);

            private delegate int GdipSetPathGradientFocusScales_delegate(HandleRef brush, float xScale, float yScale);
            private static FunctionWrapper<GdipSetPathGradientFocusScales_delegate> GdipSetPathGradientFocusScales_ptr;
            internal static int GdipSetPathGradientFocusScales(HandleRef brush, float xScale, float yScale) => GdipSetPathGradientFocusScales_ptr.Delegate(brush, xScale, yScale);

            private delegate int GdipCreatePen1_delegate(int argb, float width, int unit, out IntPtr pen);
            private static FunctionWrapper<GdipCreatePen1_delegate> GdipCreatePen1_ptr;
            internal static int GdipCreatePen1(int argb, float width, int unit, out IntPtr pen) => GdipCreatePen1_ptr.Delegate(argb, width, unit, out pen);

            private delegate int GdipCreatePen2_delegate(HandleRef brush, float width, int unit, out IntPtr pen);
            private static FunctionWrapper<GdipCreatePen2_delegate> GdipCreatePen2_ptr;
            internal static int GdipCreatePen2(HandleRef brush, float width, int unit, out IntPtr pen) => GdipCreatePen2_ptr.Delegate(brush, width, unit, out pen);

            private delegate int GdipClonePen_delegate(HandleRef pen, out IntPtr clonepen);
            private static FunctionWrapper<GdipClonePen_delegate> GdipClonePen_ptr;
            internal static int GdipClonePen(HandleRef pen, out IntPtr clonepen) => GdipClonePen_ptr.Delegate(pen, out clonepen);

            private delegate int GdipDeletePen_delegate(HandleRef Pen);
            private static FunctionWrapper<GdipDeletePen_delegate> GdipDeletePen_ptr;
            internal static int IntGdipDeletePen(HandleRef Pen) => GdipDeletePen_ptr.Delegate(Pen);

            private delegate int GdipSetPenMode_delegate(HandleRef pen, PenAlignment penAlign);
            private static FunctionWrapper<GdipSetPenMode_delegate> GdipSetPenMode_ptr;
            internal static int GdipSetPenMode(HandleRef pen, PenAlignment penAlign) => GdipSetPenMode_ptr.Delegate(pen, penAlign);

            private delegate int GdipGetPenMode_delegate(HandleRef pen, out PenAlignment penAlign);
            private static FunctionWrapper<GdipGetPenMode_delegate> GdipGetPenMode_ptr;
            internal static int GdipGetPenMode(HandleRef pen, out PenAlignment penAlign) => GdipGetPenMode_ptr.Delegate(pen, out penAlign);

            private delegate int GdipSetPenWidth_delegate(HandleRef pen, float width);
            private static FunctionWrapper<GdipSetPenWidth_delegate> GdipSetPenWidth_ptr;
            internal static int GdipSetPenWidth(HandleRef pen, float width) => GdipSetPenWidth_ptr.Delegate(pen, width);

            private delegate int GdipGetPenWidth_delegate(HandleRef pen, float[] width);
            private static FunctionWrapper<GdipGetPenWidth_delegate> GdipGetPenWidth_ptr;
            internal static int GdipGetPenWidth(HandleRef pen, float[] width) => GdipGetPenWidth_ptr.Delegate(pen, width);

            private delegate int GdipSetPenLineCap197819_delegate(HandleRef pen, int startCap, int endCap, int dashCap);
            private static FunctionWrapper<GdipSetPenLineCap197819_delegate> GdipSetPenLineCap197819_ptr;
            internal static int GdipSetPenLineCap197819(HandleRef pen, int startCap, int endCap, int dashCap) => GdipSetPenLineCap197819_ptr.Delegate(pen, startCap, endCap, dashCap);

            private delegate int GdipSetPenStartCap_delegate(HandleRef pen, int startCap);
            private static FunctionWrapper<GdipSetPenStartCap_delegate> GdipSetPenStartCap_ptr;
            internal static int GdipSetPenStartCap(HandleRef pen, int startCap) => GdipSetPenStartCap_ptr.Delegate(pen, startCap);

            private delegate int GdipSetPenEndCap_delegate(HandleRef pen, int endCap);
            private static FunctionWrapper<GdipSetPenEndCap_delegate> GdipSetPenEndCap_ptr;
            internal static int GdipSetPenEndCap(HandleRef pen, int endCap) => GdipSetPenEndCap_ptr.Delegate(pen, endCap);

            private delegate int GdipGetPenStartCap_delegate(HandleRef pen, out int startCap);
            private static FunctionWrapper<GdipGetPenStartCap_delegate> GdipGetPenStartCap_ptr;
            internal static int GdipGetPenStartCap(HandleRef pen, out int startCap) => GdipGetPenStartCap_ptr.Delegate(pen, out startCap);

            private delegate int GdipGetPenEndCap_delegate(HandleRef pen, out int endCap);
            private static FunctionWrapper<GdipGetPenEndCap_delegate> GdipGetPenEndCap_ptr;
            internal static int GdipGetPenEndCap(HandleRef pen, out int endCap) => GdipGetPenEndCap_ptr.Delegate(pen, out endCap);

            private delegate int GdipGetPenDashCap197819_delegate(HandleRef pen, out int dashCap);
            private static FunctionWrapper<GdipGetPenDashCap197819_delegate> GdipGetPenDashCap197819_ptr;
            internal static int GdipGetPenDashCap197819(HandleRef pen, out int dashCap) => GdipGetPenDashCap197819_ptr.Delegate(pen, out dashCap);

            private delegate int GdipSetPenDashCap197819_delegate(HandleRef pen, int dashCap);
            private static FunctionWrapper<GdipSetPenDashCap197819_delegate> GdipSetPenDashCap197819_ptr;
            internal static int GdipSetPenDashCap197819(HandleRef pen, int dashCap) => GdipSetPenDashCap197819_ptr.Delegate(pen, dashCap);

            private delegate int GdipSetPenLineJoin_delegate(HandleRef pen, int lineJoin);
            private static FunctionWrapper<GdipSetPenLineJoin_delegate> GdipSetPenLineJoin_ptr;
            internal static int GdipSetPenLineJoin(HandleRef pen, int lineJoin) => GdipSetPenLineJoin_ptr.Delegate(pen, lineJoin);

            private delegate int GdipGetPenLineJoin_delegate(HandleRef pen, out int lineJoin);
            private static FunctionWrapper<GdipGetPenLineJoin_delegate> GdipGetPenLineJoin_ptr;
            internal static int GdipGetPenLineJoin(HandleRef pen, out int lineJoin) => GdipGetPenLineJoin_ptr.Delegate(pen, out lineJoin);

            private delegate int GdipSetPenCustomStartCap_delegate(HandleRef pen, HandleRef customCap);
            private static FunctionWrapper<GdipSetPenCustomStartCap_delegate> GdipSetPenCustomStartCap_ptr;
            internal static int GdipSetPenCustomStartCap(HandleRef pen, HandleRef customCap) => GdipSetPenCustomStartCap_ptr.Delegate(pen, customCap);

            private delegate int GdipGetPenCustomStartCap_delegate(HandleRef pen, out IntPtr customCap);
            private static FunctionWrapper<GdipGetPenCustomStartCap_delegate> GdipGetPenCustomStartCap_ptr;
            internal static int GdipGetPenCustomStartCap(HandleRef pen, out IntPtr customCap) => GdipGetPenCustomStartCap_ptr.Delegate(pen, out customCap);

            private delegate int GdipSetPenCustomEndCap_delegate(HandleRef pen, HandleRef customCap);
            private static FunctionWrapper<GdipSetPenCustomEndCap_delegate> GdipSetPenCustomEndCap_ptr;
            internal static int GdipSetPenCustomEndCap(HandleRef pen, HandleRef customCap) => GdipSetPenCustomEndCap_ptr.Delegate(pen, customCap);

            private delegate int GdipGetPenCustomEndCap_delegate(HandleRef pen, out IntPtr customCap);
            private static FunctionWrapper<GdipGetPenCustomEndCap_delegate> GdipGetPenCustomEndCap_ptr;
            internal static int GdipGetPenCustomEndCap(HandleRef pen, out IntPtr customCap) => GdipGetPenCustomEndCap_ptr.Delegate(pen, out customCap);

            private delegate int GdipSetPenMiterLimit_delegate(HandleRef pen, float miterLimit);
            private static FunctionWrapper<GdipSetPenMiterLimit_delegate> GdipSetPenMiterLimit_ptr;
            internal static int GdipSetPenMiterLimit(HandleRef pen, float miterLimit) => GdipSetPenMiterLimit_ptr.Delegate(pen, miterLimit);

            private delegate int GdipGetPenMiterLimit_delegate(HandleRef pen, float[] miterLimit);
            private static FunctionWrapper<GdipGetPenMiterLimit_delegate> GdipGetPenMiterLimit_ptr;
            internal static int GdipGetPenMiterLimit(HandleRef pen, float[] miterLimit) => GdipGetPenMiterLimit_ptr.Delegate(pen, miterLimit);

            private delegate int GdipSetPenTransform_delegate(HandleRef pen, HandleRef matrix);
            private static FunctionWrapper<GdipSetPenTransform_delegate> GdipSetPenTransform_ptr;
            internal static int GdipSetPenTransform(HandleRef pen, HandleRef matrix) => GdipSetPenTransform_ptr.Delegate(pen, matrix);

            private delegate int GdipGetPenTransform_delegate(HandleRef pen, HandleRef matrix);
            private static FunctionWrapper<GdipGetPenTransform_delegate> GdipGetPenTransform_ptr;
            internal static int GdipGetPenTransform(HandleRef pen, HandleRef matrix) => GdipGetPenTransform_ptr.Delegate(pen, matrix);

            private delegate int GdipResetPenTransform_delegate(HandleRef brush);
            private static FunctionWrapper<GdipResetPenTransform_delegate> GdipResetPenTransform_ptr;
            internal static int GdipResetPenTransform(HandleRef brush) => GdipResetPenTransform_ptr.Delegate(brush);

            private delegate int GdipMultiplyPenTransform_delegate(HandleRef brush, HandleRef matrix, MatrixOrder order);
            private static FunctionWrapper<GdipMultiplyPenTransform_delegate> GdipMultiplyPenTransform_ptr;
            internal static int GdipMultiplyPenTransform(HandleRef brush, HandleRef matrix, MatrixOrder order) => GdipMultiplyPenTransform_ptr.Delegate(brush, matrix, order);

            private delegate int GdipTranslatePenTransform_delegate(HandleRef brush, float dx, float dy, MatrixOrder order);
            private static FunctionWrapper<GdipTranslatePenTransform_delegate> GdipTranslatePenTransform_ptr;
            internal static int GdipTranslatePenTransform(HandleRef brush, float dx, float dy, MatrixOrder order) => GdipTranslatePenTransform_ptr.Delegate(brush, dx, dy, order);

            private delegate int GdipScalePenTransform_delegate(HandleRef brush, float sx, float sy, MatrixOrder order);
            private static FunctionWrapper<GdipScalePenTransform_delegate> GdipScalePenTransform_ptr;
            internal static int GdipScalePenTransform(HandleRef brush, float sx, float sy, MatrixOrder order) => GdipScalePenTransform_ptr.Delegate(brush, sx, sy, order);

            private delegate int GdipRotatePenTransform_delegate(HandleRef brush, float angle, MatrixOrder order);
            private static FunctionWrapper<GdipRotatePenTransform_delegate> GdipRotatePenTransform_ptr;
            internal static int GdipRotatePenTransform(HandleRef brush, float angle, MatrixOrder order) => GdipRotatePenTransform_ptr.Delegate(brush, angle, order);

            private delegate int GdipSetPenColor_delegate(HandleRef pen, int argb);
            private static FunctionWrapper<GdipSetPenColor_delegate> GdipSetPenColor_ptr;
            internal static int GdipSetPenColor(HandleRef pen, int argb) => GdipSetPenColor_ptr.Delegate(pen, argb);

            private delegate int GdipGetPenColor_delegate(HandleRef pen, out int argb);
            private static FunctionWrapper<GdipGetPenColor_delegate> GdipGetPenColor_ptr;
            internal static int GdipGetPenColor(HandleRef pen, out int argb) => GdipGetPenColor_ptr.Delegate(pen, out argb);

            private delegate int GdipSetPenBrushFill_delegate(HandleRef pen, HandleRef brush);
            private static FunctionWrapper<GdipSetPenBrushFill_delegate> GdipSetPenBrushFill_ptr;
            internal static int GdipSetPenBrushFill(HandleRef pen, HandleRef brush) => GdipSetPenBrushFill_ptr.Delegate(pen, brush);

            private delegate int GdipGetPenBrushFill_delegate(HandleRef pen, out IntPtr brush);
            private static FunctionWrapper<GdipGetPenBrushFill_delegate> GdipGetPenBrushFill_ptr;
            internal static int GdipGetPenBrushFill(HandleRef pen, out IntPtr brush) => GdipGetPenBrushFill_ptr.Delegate(pen, out brush);

            private delegate int GdipGetPenFillType_delegate(HandleRef pen, out int pentype);
            private static FunctionWrapper<GdipGetPenFillType_delegate> GdipGetPenFillType_ptr;
            internal static int GdipGetPenFillType(HandleRef pen, out int pentype) => GdipGetPenFillType_ptr.Delegate(pen, out pentype);

            private delegate int GdipGetPenDashStyle_delegate(HandleRef pen, out int dashstyle);
            private static FunctionWrapper<GdipGetPenDashStyle_delegate> GdipGetPenDashStyle_ptr;
            internal static int GdipGetPenDashStyle(HandleRef pen, out int dashstyle) => GdipGetPenDashStyle_ptr.Delegate(pen, out dashstyle);

            private delegate int GdipSetPenDashStyle_delegate(HandleRef pen, int dashstyle);
            private static FunctionWrapper<GdipSetPenDashStyle_delegate> GdipSetPenDashStyle_ptr;
            internal static int GdipSetPenDashStyle(HandleRef pen, int dashstyle) => GdipSetPenDashStyle_ptr.Delegate(pen, dashstyle);

            private delegate int GdipSetPenDashArray_delegate(HandleRef pen, HandleRef memorydash, int count);
            private static FunctionWrapper<GdipSetPenDashArray_delegate> GdipSetPenDashArray_ptr;
            internal static int GdipSetPenDashArray(HandleRef pen, HandleRef memorydash, int count) => GdipSetPenDashArray_ptr.Delegate(pen, memorydash, count);

            private delegate int GdipGetPenDashOffset_delegate(HandleRef pen, float[] dashoffset);
            private static FunctionWrapper<GdipGetPenDashOffset_delegate> GdipGetPenDashOffset_ptr;
            internal static int GdipGetPenDashOffset(HandleRef pen, float[] dashoffset) => GdipGetPenDashOffset_ptr.Delegate(pen, dashoffset);

            private delegate int GdipSetPenDashOffset_delegate(HandleRef pen, float dashoffset);
            private static FunctionWrapper<GdipSetPenDashOffset_delegate> GdipSetPenDashOffset_ptr;
            internal static int GdipSetPenDashOffset(HandleRef pen, float dashoffset) => GdipSetPenDashOffset_ptr.Delegate(pen, dashoffset);

            private delegate int GdipGetPenDashCount_delegate(HandleRef pen, out int dashcount);
            private static FunctionWrapper<GdipGetPenDashCount_delegate> GdipGetPenDashCount_ptr;
            internal static int GdipGetPenDashCount(HandleRef pen, out int dashcount) => GdipGetPenDashCount_ptr.Delegate(pen, out dashcount);

            private delegate int GdipGetPenDashArray_delegate(HandleRef pen, IntPtr memorydash, int count);
            private static FunctionWrapper<GdipGetPenDashArray_delegate> GdipGetPenDashArray_ptr;
            internal static int GdipGetPenDashArray(HandleRef pen, IntPtr memorydash, int count) => GdipGetPenDashArray_ptr.Delegate(pen, memorydash, count);

            private delegate int GdipGetPenCompoundCount_delegate(HandleRef pen, out int count);
            private static FunctionWrapper<GdipGetPenCompoundCount_delegate> GdipGetPenCompoundCount_ptr;
            internal static int GdipGetPenCompoundCount(HandleRef pen, out int count) => GdipGetPenCompoundCount_ptr.Delegate(pen, out count);

            private delegate int GdipSetPenCompoundArray_delegate(HandleRef pen, float[] array, int count);
            private static FunctionWrapper<GdipSetPenCompoundArray_delegate> GdipSetPenCompoundArray_ptr;
            internal static int GdipSetPenCompoundArray(HandleRef pen, float[] array, int count) => GdipSetPenCompoundArray_ptr.Delegate(pen, array, count);

            private delegate int GdipGetPenCompoundArray_delegate(HandleRef pen, float[] array, int count);
            private static FunctionWrapper<GdipGetPenCompoundArray_delegate> GdipGetPenCompoundArray_ptr;
            internal static int GdipGetPenCompoundArray(HandleRef pen, float[] array, int count) => GdipGetPenCompoundArray_ptr.Delegate(pen, array, count);

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

            private delegate int GdipSaveImageToFile_delegate(HandleRef image, string filename, ref Guid classId, HandleRef encoderParams);
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

            private delegate int GdipSetWorldTransform_delegate(HandleRef graphics, HandleRef matrix);
            private static FunctionWrapper<GdipSetWorldTransform_delegate> GdipSetWorldTransform_ptr;
            internal static int GdipSetWorldTransform(HandleRef graphics, HandleRef matrix) => GdipSetWorldTransform_ptr.Delegate(graphics, matrix);

            private delegate int GdipResetWorldTransform_delegate(HandleRef graphics);
            private static FunctionWrapper<GdipResetWorldTransform_delegate> GdipResetWorldTransform_ptr;
            internal static int GdipResetWorldTransform(HandleRef graphics) => GdipResetWorldTransform_ptr.Delegate(graphics);

            private delegate int GdipMultiplyWorldTransform_delegate(HandleRef graphics, HandleRef matrix, MatrixOrder order);
            private static FunctionWrapper<GdipMultiplyWorldTransform_delegate> GdipMultiplyWorldTransform_ptr;
            internal static int GdipMultiplyWorldTransform(HandleRef graphics, HandleRef matrix, MatrixOrder order) => GdipMultiplyWorldTransform_ptr.Delegate(graphics, matrix, order);

            private delegate int GdipTranslateWorldTransform_delegate(HandleRef graphics, float dx, float dy, MatrixOrder order);
            private static FunctionWrapper<GdipTranslateWorldTransform_delegate> GdipTranslateWorldTransform_ptr;
            internal static int GdipTranslateWorldTransform(HandleRef graphics, float dx, float dy, MatrixOrder order) => GdipTranslateWorldTransform_ptr.Delegate(graphics, dx, dy, order);

            private delegate int GdipScaleWorldTransform_delegate(HandleRef graphics, float sx, float sy, MatrixOrder order);
            private static FunctionWrapper<GdipScaleWorldTransform_delegate> GdipScaleWorldTransform_ptr;
            internal static int GdipScaleWorldTransform(HandleRef graphics, float sx, float sy, MatrixOrder order) => GdipScaleWorldTransform_ptr.Delegate(graphics, sx, sy, order);

            private delegate int GdipRotateWorldTransform_delegate(HandleRef graphics, float angle, MatrixOrder order);
            private static FunctionWrapper<GdipRotateWorldTransform_delegate> GdipRotateWorldTransform_ptr;
            internal static int GdipRotateWorldTransform(HandleRef graphics, float angle, MatrixOrder order) => GdipRotateWorldTransform_ptr.Delegate(graphics, angle, order);

            private delegate int GdipGetWorldTransform_delegate(HandleRef graphics, HandleRef matrix);
            private static FunctionWrapper<GdipGetWorldTransform_delegate> GdipGetWorldTransform_ptr;
            internal static int GdipGetWorldTransform(HandleRef graphics, HandleRef matrix) => GdipGetWorldTransform_ptr.Delegate(graphics, matrix);

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

            private delegate int GdipFillRegion_delegate(HandleRef graphics, HandleRef brush, HandleRef region);
            private static FunctionWrapper<GdipFillRegion_delegate> GdipFillRegion_ptr;
            internal static int GdipFillRegion(HandleRef graphics, HandleRef brush, HandleRef region) => GdipFillRegion_ptr.Delegate(graphics, brush, region);

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

            private delegate int GdipSetClipGraphics_delegate(HandleRef graphics, HandleRef srcgraphics, CombineMode mode);
            private static FunctionWrapper<GdipSetClipGraphics_delegate> GdipSetClipGraphics_ptr;
            internal static int GdipSetClipGraphics(HandleRef graphics, HandleRef srcgraphics, CombineMode mode) => GdipSetClipGraphics_ptr.Delegate(graphics, srcgraphics, mode);

            private delegate int GdipSetClipRect_delegate(HandleRef graphics, float x, float y, float width, float height, CombineMode mode);
            private static FunctionWrapper<GdipSetClipRect_delegate> GdipSetClipRect_ptr;
            internal static int GdipSetClipRect(HandleRef graphics, float x, float y, float width, float height, CombineMode mode) => GdipSetClipRect_ptr.Delegate(graphics, x, y, width, height, mode);

            private delegate int GdipSetClipRectI_delegate(HandleRef graphics, int x, int y, int width, int height, CombineMode mode);
            private static FunctionWrapper<GdipSetClipRectI_delegate> GdipSetClipRectI_ptr;
            internal static int GdipSetClipRectI(HandleRef graphics, int x, int y, int width, int height, CombineMode mode) => GdipSetClipRectI_ptr.Delegate(graphics, x, y, width, height, mode);

            private delegate int GdipSetClipPath_delegate(HandleRef graphics, HandleRef path, CombineMode mode);
            private static FunctionWrapper<GdipSetClipPath_delegate> GdipSetClipPath_ptr;
            internal static int GdipSetClipPath(HandleRef graphics, HandleRef path, CombineMode mode) => GdipSetClipPath_ptr.Delegate(graphics, path, mode);

            private delegate int GdipSetClipRegion_delegate(HandleRef graphics, HandleRef region, CombineMode mode);
            private static FunctionWrapper<GdipSetClipRegion_delegate> GdipSetClipRegion_ptr;
            internal static int GdipSetClipRegion(HandleRef graphics, HandleRef region, CombineMode mode) => GdipSetClipRegion_ptr.Delegate(graphics, region, mode);

            private delegate int GdipResetClip_delegate(HandleRef graphics);
            private static FunctionWrapper<GdipResetClip_delegate> GdipResetClip_ptr;
            internal static int GdipResetClip(HandleRef graphics) => GdipResetClip_ptr.Delegate(graphics);

            private delegate int GdipTranslateClip_delegate(HandleRef graphics, float dx, float dy);
            private static FunctionWrapper<GdipTranslateClip_delegate> GdipTranslateClip_ptr;
            internal static int GdipTranslateClip(HandleRef graphics, float dx, float dy) => GdipTranslateClip_ptr.Delegate(graphics, dx, dy);

            private delegate int GdipGetClip_delegate(HandleRef graphics, HandleRef region);
            private static FunctionWrapper<GdipGetClip_delegate> GdipGetClip_ptr;
            internal static int GdipGetClip(HandleRef graphics, HandleRef region) => GdipGetClip_ptr.Delegate(graphics, region);

            private delegate int GdipGetClipBounds_delegate(HandleRef graphics, ref GPRECTF rect);
            private static FunctionWrapper<GdipGetClipBounds_delegate> GdipGetClipBounds_ptr;
            internal static int GdipGetClipBounds(HandleRef graphics, ref GPRECTF rect) => GdipGetClipBounds_ptr.Delegate(graphics, ref rect);

            private delegate int GdipIsClipEmpty_delegate(HandleRef graphics, out int boolean);
            private static FunctionWrapper<GdipIsClipEmpty_delegate> GdipIsClipEmpty_ptr;
            internal static int GdipIsClipEmpty(HandleRef graphics, out int boolean) => GdipIsClipEmpty_ptr.Delegate(graphics, out boolean);

            private delegate int GdipGetVisibleClipBounds_delegate(HandleRef graphics, ref GPRECTF rect);
            private static FunctionWrapper<GdipGetVisibleClipBounds_delegate> GdipGetVisibleClipBounds_ptr;
            internal static int GdipGetVisibleClipBounds(HandleRef graphics, ref GPRECTF rect) => GdipGetVisibleClipBounds_ptr.Delegate(graphics, ref rect);

            private delegate int GdipIsVisibleClipEmpty_delegate(HandleRef graphics, out int boolean);
            private static FunctionWrapper<GdipIsVisibleClipEmpty_delegate> GdipIsVisibleClipEmpty_ptr;
            internal static int GdipIsVisibleClipEmpty(HandleRef graphics, out int boolean) => GdipIsVisibleClipEmpty_ptr.Delegate(graphics, out boolean);

            private delegate int GdipIsVisiblePoint_delegate(HandleRef graphics, float x, float y, out int boolean);
            private static FunctionWrapper<GdipIsVisiblePoint_delegate> GdipIsVisiblePoint_ptr;
            internal static int GdipIsVisiblePoint(HandleRef graphics, float x, float y, out int boolean) => GdipIsVisiblePoint_ptr.Delegate(graphics, x, y, out boolean);

            private delegate int GdipIsVisiblePointI_delegate(HandleRef graphics, int x, int y, out int boolean);
            private static FunctionWrapper<GdipIsVisiblePointI_delegate> GdipIsVisiblePointI_ptr;
            internal static int GdipIsVisiblePointI(HandleRef graphics, int x, int y, out int boolean) => GdipIsVisiblePointI_ptr.Delegate(graphics, x, y, out boolean);

            private delegate int GdipIsVisibleRect_delegate(HandleRef graphics, float x, float y, float width, float height, out int boolean);
            private static FunctionWrapper<GdipIsVisibleRect_delegate> GdipIsVisibleRect_ptr;
            internal static int GdipIsVisibleRect(HandleRef graphics, float x, float y, float width, float height, out int boolean) => GdipIsVisibleRect_ptr.Delegate(graphics, x, y, width, height, out boolean);

            private delegate int GdipIsVisibleRectI_delegate(HandleRef graphics, int x, int y, int width, int height, out int boolean);
            private static FunctionWrapper<GdipIsVisibleRectI_delegate> GdipIsVisibleRectI_ptr;
            internal static int GdipIsVisibleRectI(HandleRef graphics, int x, int y, int width, int height, out int boolean) => GdipIsVisibleRectI_ptr.Delegate(graphics, x, y, width, height, out boolean);

            private delegate int GdipSaveGraphics_delegate(HandleRef graphics, out int state);
            private static FunctionWrapper<GdipSaveGraphics_delegate> GdipSaveGraphics_ptr;
            internal static int GdipSaveGraphics(HandleRef graphics, out int state) => GdipSaveGraphics_ptr.Delegate(graphics, out state);

            private delegate int GdipRestoreGraphics_delegate(HandleRef graphics, int state);
            private static FunctionWrapper<GdipRestoreGraphics_delegate> GdipRestoreGraphics_ptr;
            internal static int GdipRestoreGraphics(HandleRef graphics, int state) => GdipRestoreGraphics_ptr.Delegate(graphics, state);

            private delegate int GdipBeginContainer_delegate(HandleRef graphics, ref GPRECTF dstRect, ref GPRECTF srcRect, int unit, out int state);
            private static FunctionWrapper<GdipBeginContainer_delegate> GdipBeginContainer_ptr;
            internal static int GdipBeginContainer(HandleRef graphics, ref GPRECTF dstRect, ref GPRECTF srcRect, int unit, out int state) => GdipBeginContainer_ptr.Delegate(graphics, ref dstRect, ref srcRect, unit, out state);

            private delegate int GdipBeginContainer2_delegate(HandleRef graphics, out int state);
            private static FunctionWrapper<GdipBeginContainer2_delegate> GdipBeginContainer2_ptr;
            internal static int GdipBeginContainer2(HandleRef graphics, out int state) => GdipBeginContainer2_ptr.Delegate(graphics, out state);

            private delegate int GdipBeginContainerI_delegate(HandleRef graphics, ref GPRECT dstRect, ref GPRECT srcRect, int unit, out int state);
            private static FunctionWrapper<GdipBeginContainerI_delegate> GdipBeginContainerI_ptr;
            internal static int GdipBeginContainerI(HandleRef graphics, ref GPRECT dstRect, ref GPRECT srcRect, int unit, out int state) => GdipBeginContainerI_ptr.Delegate(graphics, ref dstRect, ref srcRect, unit, out state);

            private delegate int GdipEndContainer_delegate(HandleRef graphics, int state);
            private static FunctionWrapper<GdipEndContainer_delegate> GdipEndContainer_ptr;
            internal static int GdipEndContainer(HandleRef graphics, int state) => GdipEndContainer_ptr.Delegate(graphics, state);

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

            private delegate int GdipNewPrivateFontCollection_delegate(out IntPtr fontCollection);
            private static FunctionWrapper<GdipNewPrivateFontCollection_delegate> GdipNewPrivateFontCollection_ptr;
            internal static int GdipNewPrivateFontCollection(out IntPtr fontCollection) => GdipNewPrivateFontCollection_ptr.Delegate(out fontCollection);

            private delegate int GdipDeletePrivateFontCollection_delegate(ref IntPtr fontCollection);
            private static FunctionWrapper<GdipDeletePrivateFontCollection_delegate> GdipDeletePrivateFontCollection_ptr;
            internal static int IntGdipDeletePrivateFontCollection(ref IntPtr fontCollection) => GdipDeletePrivateFontCollection_ptr.Delegate(ref fontCollection);

            private delegate int GdipPrivateAddFontFile_delegate(HandleRef fontCollection, [MarshalAs(UnmanagedType.LPWStr)]string filename);
            private static FunctionWrapper<GdipPrivateAddFontFile_delegate> GdipPrivateAddFontFile_ptr;
            internal static int GdipPrivateAddFontFile(HandleRef fontCollection, string filename) => GdipPrivateAddFontFile_ptr.Delegate(fontCollection, filename);

            private delegate int GdipPrivateAddMemoryFont_delegate(HandleRef fontCollection, HandleRef memory, int length);
            private static FunctionWrapper<GdipPrivateAddMemoryFont_delegate> GdipPrivateAddMemoryFont_ptr;
            internal static int GdipPrivateAddMemoryFont(HandleRef fontCollection, HandleRef memory, int length) => GdipPrivateAddMemoryFont_ptr.Delegate(fontCollection, memory, length);

            private delegate int GdipCreateFontFamilyFromName_delegate([MarshalAs(UnmanagedType.LPWStr)]string name, HandleRef fontCollection, out IntPtr FontFamily);
            private static FunctionWrapper<GdipCreateFontFamilyFromName_delegate> GdipCreateFontFamilyFromName_ptr;
            internal static int GdipCreateFontFamilyFromName(string name, HandleRef fontCollection, out IntPtr FontFamily) => GdipCreateFontFamilyFromName_ptr.Delegate(name, fontCollection, out FontFamily);

            private delegate int GdipGetGenericFontFamilySansSerif_delegate(out IntPtr fontfamily);
            private static FunctionWrapper<GdipGetGenericFontFamilySansSerif_delegate> GdipGetGenericFontFamilySansSerif_ptr;
            internal static int GdipGetGenericFontFamilySansSerif(out IntPtr fontfamily) => GdipGetGenericFontFamilySansSerif_ptr.Delegate(out fontfamily);

            private delegate int GdipGetGenericFontFamilySerif_delegate(out IntPtr fontfamily);
            private static FunctionWrapper<GdipGetGenericFontFamilySerif_delegate> GdipGetGenericFontFamilySerif_ptr;
            internal static int GdipGetGenericFontFamilySerif(out IntPtr fontfamily) => GdipGetGenericFontFamilySerif_ptr.Delegate(out fontfamily);

            private delegate int GdipGetGenericFontFamilyMonospace_delegate(out IntPtr fontfamily);
            private static FunctionWrapper<GdipGetGenericFontFamilyMonospace_delegate> GdipGetGenericFontFamilyMonospace_ptr;
            internal static int GdipGetGenericFontFamilyMonospace(out IntPtr fontfamily) => GdipGetGenericFontFamilyMonospace_ptr.Delegate(out fontfamily);

            private delegate int GdipDeleteFontFamily_delegate(HandleRef fontFamily);
            private static FunctionWrapper<GdipDeleteFontFamily_delegate> GdipDeleteFontFamily_ptr;
            internal static int IntGdipDeleteFontFamily(HandleRef fontFamily) => GdipDeleteFontFamily_ptr.Delegate(fontFamily);

            private delegate int GdipGetFamilyName_delegate(HandleRef family, IntPtr name, int language);
            private static FunctionWrapper<GdipGetFamilyName_delegate> GdipGetFamilyName_ptr;
            internal static int GdipGetFamilyName(HandleRef family, IntPtr name, int language) => GdipGetFamilyName_ptr.Delegate(family, name, language);
            internal static unsafe int GdipGetFamilyName(HandleRef family, StringBuilder nameBuilder, int language)
            {
                const int LF_FACESIZE = 32;
                char* namePtr = stackalloc char[LF_FACESIZE];
                int ret = GdipGetFamilyName(family, (IntPtr)namePtr, language);
                string name = Marshal.PtrToStringUni((IntPtr)namePtr);
                nameBuilder.Append(name);
                return ret;
            }

            private delegate int GdipIsStyleAvailable_delegate(HandleRef family, FontStyle style, out int isStyleAvailable);
            private static FunctionWrapper<GdipIsStyleAvailable_delegate> GdipIsStyleAvailable_ptr;
            internal static int GdipIsStyleAvailable(HandleRef family, FontStyle style, out int isStyleAvailable) => GdipIsStyleAvailable_ptr.Delegate(family, style, out isStyleAvailable);

            private delegate int GdipGetEmHeight_delegate(HandleRef family, FontStyle style, out int EmHeight);
            private static FunctionWrapper<GdipGetEmHeight_delegate> GdipGetEmHeight_ptr;
            internal static int GdipGetEmHeight(HandleRef family, FontStyle style, out int EmHeight) => GdipGetEmHeight_ptr.Delegate(family, style, out EmHeight);

            private delegate int GdipGetCellAscent_delegate(HandleRef family, FontStyle style, out int CellAscent);
            private static FunctionWrapper<GdipGetCellAscent_delegate> GdipGetCellAscent_ptr;
            internal static int GdipGetCellAscent(HandleRef family, FontStyle style, out int CellAscent) => GdipGetCellAscent_ptr.Delegate(family, style, out CellAscent);

            private delegate int GdipGetCellDescent_delegate(HandleRef family, FontStyle style, out int CellDescent);
            private static FunctionWrapper<GdipGetCellDescent_delegate> GdipGetCellDescent_ptr;
            internal static int GdipGetCellDescent(HandleRef family, FontStyle style, out int CellDescent) => GdipGetCellDescent_ptr.Delegate(family, style, out CellDescent);

            private delegate int GdipGetLineSpacing_delegate(HandleRef family, FontStyle style, out int LineSpaceing);
            private static FunctionWrapper<GdipGetLineSpacing_delegate> GdipGetLineSpacing_ptr;
            internal static int GdipGetLineSpacing(HandleRef family, FontStyle style, out int LineSpaceing) => GdipGetLineSpacing_ptr.Delegate(family, style, out LineSpaceing);

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

            private delegate int GdipSetStringFormatMeasurableCharacterRanges_delegate(HandleRef format, int rangeCount, [In] [Out] CharacterRange[] range);
            private static FunctionWrapper<GdipSetStringFormatMeasurableCharacterRanges_delegate> GdipSetStringFormatMeasurableCharacterRanges_ptr;
            internal static int GdipSetStringFormatMeasurableCharacterRanges(HandleRef format, int rangeCount, [In] [Out] CharacterRange[] range) => GdipSetStringFormatMeasurableCharacterRanges_ptr.Delegate(format, rangeCount, range);

            private delegate int GdipCreateStringFormat_delegate(StringFormatFlags options, int language, out IntPtr format);
            private static FunctionWrapper<GdipCreateStringFormat_delegate> GdipCreateStringFormat_ptr;
            internal static int GdipCreateStringFormat(StringFormatFlags options, int language, out IntPtr format) => GdipCreateStringFormat_ptr.Delegate(options, language, out format);

            private delegate int GdipStringFormatGetGenericDefault_delegate(out IntPtr format);
            private static FunctionWrapper<GdipStringFormatGetGenericDefault_delegate> GdipStringFormatGetGenericDefault_ptr;
            internal static int GdipStringFormatGetGenericDefault(out IntPtr format) => GdipStringFormatGetGenericDefault_ptr.Delegate(out format);

            private delegate int GdipStringFormatGetGenericTypographic_delegate(out IntPtr format);
            private static FunctionWrapper<GdipStringFormatGetGenericTypographic_delegate> GdipStringFormatGetGenericTypographic_ptr;
            internal static int GdipStringFormatGetGenericTypographic(out IntPtr format) => GdipStringFormatGetGenericTypographic_ptr.Delegate(out format);

            private delegate int GdipDeleteStringFormat_delegate(HandleRef format);
            private static FunctionWrapper<GdipDeleteStringFormat_delegate> GdipDeleteStringFormat_ptr;
            internal static int IntGdipDeleteStringFormat(HandleRef format) => GdipDeleteStringFormat_ptr.Delegate(format);

            private delegate int GdipCloneStringFormat_delegate(HandleRef format, out IntPtr newFormat);
            private static FunctionWrapper<GdipCloneStringFormat_delegate> GdipCloneStringFormat_ptr;
            internal static int GdipCloneStringFormat(HandleRef format, out IntPtr newFormat) => GdipCloneStringFormat_ptr.Delegate(format, out newFormat);

            private delegate int GdipSetStringFormatFlags_delegate(HandleRef format, StringFormatFlags options);
            private static FunctionWrapper<GdipSetStringFormatFlags_delegate> GdipSetStringFormatFlags_ptr;
            internal static int GdipSetStringFormatFlags(HandleRef format, StringFormatFlags options) => GdipSetStringFormatFlags_ptr.Delegate(format, options);

            private delegate int GdipGetStringFormatFlags_delegate(HandleRef format, out StringFormatFlags result);
            private static FunctionWrapper<GdipGetStringFormatFlags_delegate> GdipGetStringFormatFlags_ptr;
            internal static int GdipGetStringFormatFlags(HandleRef format, out StringFormatFlags result) => GdipGetStringFormatFlags_ptr.Delegate(format, out result);

            private delegate int GdipSetStringFormatAlign_delegate(HandleRef format, StringAlignment align);
            private static FunctionWrapper<GdipSetStringFormatAlign_delegate> GdipSetStringFormatAlign_ptr;
            internal static int GdipSetStringFormatAlign(HandleRef format, StringAlignment align) => GdipSetStringFormatAlign_ptr.Delegate(format, align);

            private delegate int GdipGetStringFormatAlign_delegate(HandleRef format, out StringAlignment align);
            private static FunctionWrapper<GdipGetStringFormatAlign_delegate> GdipGetStringFormatAlign_ptr;
            internal static int GdipGetStringFormatAlign(HandleRef format, out StringAlignment align) => GdipGetStringFormatAlign_ptr.Delegate(format, out align);

            private delegate int GdipSetStringFormatLineAlign_delegate(HandleRef format, StringAlignment align);
            private static FunctionWrapper<GdipSetStringFormatLineAlign_delegate> GdipSetStringFormatLineAlign_ptr;
            internal static int GdipSetStringFormatLineAlign(HandleRef format, StringAlignment align) => GdipSetStringFormatLineAlign_ptr.Delegate(format, align);

            private delegate int GdipGetStringFormatLineAlign_delegate(HandleRef format, out StringAlignment align);
            private static FunctionWrapper<GdipGetStringFormatLineAlign_delegate> GdipGetStringFormatLineAlign_ptr;
            internal static int GdipGetStringFormatLineAlign(HandleRef format, out StringAlignment align) => GdipGetStringFormatLineAlign_ptr.Delegate(format, out align);

            private delegate int GdipSetStringFormatHotkeyPrefix_delegate(HandleRef format, HotkeyPrefix hotkeyPrefix);
            private static FunctionWrapper<GdipSetStringFormatHotkeyPrefix_delegate> GdipSetStringFormatHotkeyPrefix_ptr;
            internal static int GdipSetStringFormatHotkeyPrefix(HandleRef format, HotkeyPrefix hotkeyPrefix) => GdipSetStringFormatHotkeyPrefix_ptr.Delegate(format, hotkeyPrefix);

            private delegate int GdipGetStringFormatHotkeyPrefix_delegate(HandleRef format, out HotkeyPrefix hotkeyPrefix);
            private static FunctionWrapper<GdipGetStringFormatHotkeyPrefix_delegate> GdipGetStringFormatHotkeyPrefix_ptr;
            internal static int GdipGetStringFormatHotkeyPrefix(HandleRef format, out HotkeyPrefix hotkeyPrefix) => GdipGetStringFormatHotkeyPrefix_ptr.Delegate(format, out hotkeyPrefix);

            private delegate int GdipSetStringFormatTabStops_delegate(HandleRef format, float firstTabOffset, int count, float[] tabStops);
            private static FunctionWrapper<GdipSetStringFormatTabStops_delegate> GdipSetStringFormatTabStops_ptr;
            internal static int GdipSetStringFormatTabStops(HandleRef format, float firstTabOffset, int count, float[] tabStops) => GdipSetStringFormatTabStops_ptr.Delegate(format, firstTabOffset, count, tabStops);

            private delegate int GdipGetStringFormatTabStops_delegate(HandleRef format, int count, out float firstTabOffset, [In] [Out] float[] tabStops);
            private static FunctionWrapper<GdipGetStringFormatTabStops_delegate> GdipGetStringFormatTabStops_ptr;
            internal static int GdipGetStringFormatTabStops(HandleRef format, int count, out float firstTabOffset, [In] [Out] float[] tabStops) => GdipGetStringFormatTabStops_ptr.Delegate(format, count, out firstTabOffset, tabStops);

            private delegate int GdipGetStringFormatTabStopCount_delegate(HandleRef format, out int count);
            private static FunctionWrapper<GdipGetStringFormatTabStopCount_delegate> GdipGetStringFormatTabStopCount_ptr;
            internal static int GdipGetStringFormatTabStopCount(HandleRef format, out int count) => GdipGetStringFormatTabStopCount_ptr.Delegate(format, out count);

            private delegate int GdipGetStringFormatMeasurableCharacterRangeCount_delegate(HandleRef format, out int count);
            private static FunctionWrapper<GdipGetStringFormatMeasurableCharacterRangeCount_delegate> GdipGetStringFormatMeasurableCharacterRangeCount_ptr;
            internal static int GdipGetStringFormatMeasurableCharacterRangeCount(HandleRef format, out int count) => GdipGetStringFormatMeasurableCharacterRangeCount_ptr.Delegate(format, out count);

            private delegate int GdipSetStringFormatTrimming_delegate(HandleRef format, StringTrimming trimming);
            private static FunctionWrapper<GdipSetStringFormatTrimming_delegate> GdipSetStringFormatTrimming_ptr;
            internal static int GdipSetStringFormatTrimming(HandleRef format, StringTrimming trimming) => GdipSetStringFormatTrimming_ptr.Delegate(format, trimming);

            private delegate int GdipGetStringFormatTrimming_delegate(HandleRef format, out StringTrimming trimming);
            private static FunctionWrapper<GdipGetStringFormatTrimming_delegate> GdipGetStringFormatTrimming_ptr;
            internal static int GdipGetStringFormatTrimming(HandleRef format, out StringTrimming trimming) => GdipGetStringFormatTrimming_ptr.Delegate(format, out trimming);

            private delegate int GdipSetStringFormatDigitSubstitution_delegate(HandleRef format, int langID, StringDigitSubstitute sds);
            private static FunctionWrapper<GdipSetStringFormatDigitSubstitution_delegate> GdipSetStringFormatDigitSubstitution_ptr;
            internal static int GdipSetStringFormatDigitSubstitution(HandleRef format, int langID, StringDigitSubstitute sds) => GdipSetStringFormatDigitSubstitution_ptr.Delegate(format, langID, sds);

            private delegate int GdipGetStringFormatDigitSubstitution_delegate(HandleRef format, out int langID, out StringDigitSubstitute sds);
            private static FunctionWrapper<GdipGetStringFormatDigitSubstitution_delegate> GdipGetStringFormatDigitSubstitution_ptr;
            internal static int GdipGetStringFormatDigitSubstitution(HandleRef format, out int langID, out StringDigitSubstitute sds) => GdipGetStringFormatDigitSubstitution_ptr.Delegate(format, out langID, out sds);
        }
    }
}