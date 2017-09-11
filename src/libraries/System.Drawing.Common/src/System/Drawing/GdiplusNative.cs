// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Drawing
{
    // Raw function imports for gdiplus
    // Functions are loaded manually in order to accomodate different shared library names on Unix.
    internal partial class SafeNativeMethods
    {
        internal partial class Gdip
        {
            private static IntPtr s_gdipModule;

            private static void LoadSharedFunctionPointers()
            {
                GdipBeginContainer_ptr = FunctionWrapper.Load<GdipBeginContainer_delegate>(s_gdipModule, "GdipBeginContainer");
                GdipBeginContainer2_ptr = FunctionWrapper.Load<GdipBeginContainer2_delegate>(s_gdipModule, "GdipBeginContainer2");
                GdipBeginContainerI_ptr = FunctionWrapper.Load<GdipBeginContainerI_delegate>(s_gdipModule, "GdipBeginContainerI");
                GdipEndContainer_ptr = FunctionWrapper.Load<GdipEndContainer_delegate>(s_gdipModule, "GdipEndContainer");
                GdipCreateAdjustableArrowCap_ptr = FunctionWrapper.Load<GdipCreateAdjustableArrowCap_delegate>(s_gdipModule, "GdipCreateAdjustableArrowCap");
                GdipGetAdjustableArrowCapHeight_ptr = FunctionWrapper.Load<GdipGetAdjustableArrowCapHeight_delegate>(s_gdipModule, "GdipGetAdjustableArrowCapHeight");
                GdipSetAdjustableArrowCapHeight_ptr = FunctionWrapper.Load<GdipSetAdjustableArrowCapHeight_delegate>(s_gdipModule, "GdipSetAdjustableArrowCapHeight");
                GdipSetAdjustableArrowCapWidth_ptr = FunctionWrapper.Load<GdipSetAdjustableArrowCapWidth_delegate>(s_gdipModule, "GdipSetAdjustableArrowCapWidth");
                GdipGetAdjustableArrowCapWidth_ptr = FunctionWrapper.Load<GdipGetAdjustableArrowCapWidth_delegate>(s_gdipModule, "GdipGetAdjustableArrowCapWidth");
                GdipSetAdjustableArrowCapMiddleInset_ptr = FunctionWrapper.Load<GdipSetAdjustableArrowCapMiddleInset_delegate>(s_gdipModule, "GdipSetAdjustableArrowCapMiddleInset");
                GdipGetAdjustableArrowCapMiddleInset_ptr = FunctionWrapper.Load<GdipGetAdjustableArrowCapMiddleInset_delegate>(s_gdipModule, "GdipGetAdjustableArrowCapMiddleInset");
                GdipSetAdjustableArrowCapFillState_ptr = FunctionWrapper.Load<GdipSetAdjustableArrowCapFillState_delegate>(s_gdipModule, "GdipSetAdjustableArrowCapFillState");
                GdipGetAdjustableArrowCapFillState_ptr = FunctionWrapper.Load<GdipGetAdjustableArrowCapFillState_delegate>(s_gdipModule, "GdipGetAdjustableArrowCapFillState");
                GdipGetCustomLineCapType_ptr = FunctionWrapper.Load<GdipGetCustomLineCapType_delegate>(s_gdipModule, "GdipGetCustomLineCapType");
                GdipCreateCustomLineCap_ptr = FunctionWrapper.Load<GdipCreateCustomLineCap_delegate>(s_gdipModule, "GdipCreateCustomLineCap");
                GdipDeleteCustomLineCap_ptr = FunctionWrapper.Load<GdipDeleteCustomLineCap_delegate>(s_gdipModule, "GdipDeleteCustomLineCap");
                GdipCloneCustomLineCap_ptr = FunctionWrapper.Load<GdipCloneCustomLineCap_delegate>(s_gdipModule, "GdipCloneCustomLineCap");
                GdipSetCustomLineCapStrokeCaps_ptr = FunctionWrapper.Load<GdipSetCustomLineCapStrokeCaps_delegate>(s_gdipModule, "GdipSetCustomLineCapStrokeCaps");
                GdipGetCustomLineCapStrokeCaps_ptr = FunctionWrapper.Load<GdipGetCustomLineCapStrokeCaps_delegate>(s_gdipModule, "GdipGetCustomLineCapStrokeCaps");
                GdipSetCustomLineCapStrokeJoin_ptr = FunctionWrapper.Load<GdipSetCustomLineCapStrokeJoin_delegate>(s_gdipModule, "GdipSetCustomLineCapStrokeJoin");
                GdipGetCustomLineCapStrokeJoin_ptr = FunctionWrapper.Load<GdipGetCustomLineCapStrokeJoin_delegate>(s_gdipModule, "GdipGetCustomLineCapStrokeJoin");
                GdipSetCustomLineCapBaseCap_ptr = FunctionWrapper.Load<GdipSetCustomLineCapBaseCap_delegate>(s_gdipModule, "GdipSetCustomLineCapBaseCap");
                GdipGetCustomLineCapBaseCap_ptr = FunctionWrapper.Load<GdipGetCustomLineCapBaseCap_delegate>(s_gdipModule, "GdipGetCustomLineCapBaseCap");
                GdipSetCustomLineCapBaseInset_ptr = FunctionWrapper.Load<GdipSetCustomLineCapBaseInset_delegate>(s_gdipModule, "GdipSetCustomLineCapBaseInset");
                GdipGetCustomLineCapBaseInset_ptr = FunctionWrapper.Load<GdipGetCustomLineCapBaseInset_delegate>(s_gdipModule, "GdipGetCustomLineCapBaseInset");
                GdipSetCustomLineCapWidthScale_ptr = FunctionWrapper.Load<GdipSetCustomLineCapWidthScale_delegate>(s_gdipModule, "GdipSetCustomLineCapWidthScale");
                GdipGetCustomLineCapWidthScale_ptr = FunctionWrapper.Load<GdipGetCustomLineCapWidthScale_delegate>(s_gdipModule, "GdipGetCustomLineCapWidthScale");
                GdipCreatePathIter_ptr = FunctionWrapper.Load<GdipCreatePathIter_delegate>(s_gdipModule, "GdipCreatePathIter");
                GdipDeletePathIter_ptr = FunctionWrapper.Load<GdipDeletePathIter_delegate>(s_gdipModule, "GdipDeletePathIter");
                GdipPathIterNextSubpath_ptr = FunctionWrapper.Load<GdipPathIterNextSubpath_delegate>(s_gdipModule, "GdipPathIterNextSubpath");
                GdipPathIterNextSubpathPath_ptr = FunctionWrapper.Load<GdipPathIterNextSubpathPath_delegate>(s_gdipModule, "GdipPathIterNextSubpathPath");
                GdipPathIterNextPathType_ptr = FunctionWrapper.Load<GdipPathIterNextPathType_delegate>(s_gdipModule, "GdipPathIterNextPathType");
                GdipPathIterNextMarker_ptr = FunctionWrapper.Load<GdipPathIterNextMarker_delegate>(s_gdipModule, "GdipPathIterNextMarker");
                GdipPathIterNextMarkerPath_ptr = FunctionWrapper.Load<GdipPathIterNextMarkerPath_delegate>(s_gdipModule, "GdipPathIterNextMarkerPath");
                GdipPathIterGetCount_ptr = FunctionWrapper.Load<GdipPathIterGetCount_delegate>(s_gdipModule, "GdipPathIterGetCount");
                GdipPathIterGetSubpathCount_ptr = FunctionWrapper.Load<GdipPathIterGetSubpathCount_delegate>(s_gdipModule, "GdipPathIterGetSubpathCount");
                GdipPathIterHasCurve_ptr = FunctionWrapper.Load<GdipPathIterHasCurve_delegate>(s_gdipModule, "GdipPathIterHasCurve");
                GdipPathIterRewind_ptr = FunctionWrapper.Load<GdipPathIterRewind_delegate>(s_gdipModule, "GdipPathIterRewind");
                GdipPathIterEnumerate_ptr = FunctionWrapper.Load<GdipPathIterEnumerate_delegate>(s_gdipModule, "GdipPathIterEnumerate");
                GdipPathIterCopyData_ptr = FunctionWrapper.Load<GdipPathIterCopyData_delegate>(s_gdipModule, "GdipPathIterCopyData");
                GdipCreateHatchBrush_ptr = FunctionWrapper.Load<GdipCreateHatchBrush_delegate>(s_gdipModule, "GdipCreateHatchBrush");
                GdipGetHatchStyle_ptr = FunctionWrapper.Load<GdipGetHatchStyle_delegate>(s_gdipModule, "GdipGetHatchStyle");
                GdipGetHatchForegroundColor_ptr = FunctionWrapper.Load<GdipGetHatchForegroundColor_delegate>(s_gdipModule, "GdipGetHatchForegroundColor");
                GdipGetHatchBackgroundColor_ptr = FunctionWrapper.Load<GdipGetHatchBackgroundColor_delegate>(s_gdipModule, "GdipGetHatchBackgroundColor");
                GdipCreateLineBrush_ptr = FunctionWrapper.Load<GdipCreateLineBrush_delegate>(s_gdipModule, "GdipCreateLineBrush");
                GdipCreateLineBrushI_ptr = FunctionWrapper.Load<GdipCreateLineBrushI_delegate>(s_gdipModule, "GdipCreateLineBrushI");
                GdipCreateLineBrushFromRect_ptr = FunctionWrapper.Load<GdipCreateLineBrushFromRect_delegate>(s_gdipModule, "GdipCreateLineBrushFromRect");
                GdipCreateLineBrushFromRectI_ptr = FunctionWrapper.Load<GdipCreateLineBrushFromRectI_delegate>(s_gdipModule, "GdipCreateLineBrushFromRectI");
                GdipCreateLineBrushFromRectWithAngle_ptr = FunctionWrapper.Load<GdipCreateLineBrushFromRectWithAngle_delegate>(s_gdipModule, "GdipCreateLineBrushFromRectWithAngle");
                GdipCreateLineBrushFromRectWithAngleI_ptr = FunctionWrapper.Load<GdipCreateLineBrushFromRectWithAngleI_delegate>(s_gdipModule, "GdipCreateLineBrushFromRectWithAngleI");
                GdipSetLineColors_ptr = FunctionWrapper.Load<GdipSetLineColors_delegate>(s_gdipModule, "GdipSetLineColors");
                GdipGetLineColors_ptr = FunctionWrapper.Load<GdipGetLineColors_delegate>(s_gdipModule, "GdipGetLineColors");
                GdipGetLineRect_ptr = FunctionWrapper.Load<GdipGetLineRect_delegate>(s_gdipModule, "GdipGetLineRect");
                GdipGetLineGammaCorrection_ptr = FunctionWrapper.Load<GdipGetLineGammaCorrection_delegate>(s_gdipModule, "GdipGetLineGammaCorrection");
                GdipSetLineGammaCorrection_ptr = FunctionWrapper.Load<GdipSetLineGammaCorrection_delegate>(s_gdipModule, "GdipSetLineGammaCorrection");
                GdipSetLineSigmaBlend_ptr = FunctionWrapper.Load<GdipSetLineSigmaBlend_delegate>(s_gdipModule, "GdipSetLineSigmaBlend");
                GdipSetLineLinearBlend_ptr = FunctionWrapper.Load<GdipSetLineLinearBlend_delegate>(s_gdipModule, "GdipSetLineLinearBlend");
                GdipGetLineBlendCount_ptr = FunctionWrapper.Load<GdipGetLineBlendCount_delegate>(s_gdipModule, "GdipGetLineBlendCount");
                GdipGetLineBlend_ptr = FunctionWrapper.Load<GdipGetLineBlend_delegate>(s_gdipModule, "GdipGetLineBlend");
                GdipSetLineBlend_ptr = FunctionWrapper.Load<GdipSetLineBlend_delegate>(s_gdipModule, "GdipSetLineBlend");
                GdipGetLinePresetBlendCount_ptr = FunctionWrapper.Load<GdipGetLinePresetBlendCount_delegate>(s_gdipModule, "GdipGetLinePresetBlendCount");
                GdipGetLinePresetBlend_ptr = FunctionWrapper.Load<GdipGetLinePresetBlend_delegate>(s_gdipModule, "GdipGetLinePresetBlend");
                GdipSetLinePresetBlend_ptr = FunctionWrapper.Load<GdipSetLinePresetBlend_delegate>(s_gdipModule, "GdipSetLinePresetBlend");
                GdipSetLineWrapMode_ptr = FunctionWrapper.Load<GdipSetLineWrapMode_delegate>(s_gdipModule, "GdipSetLineWrapMode");
                GdipGetLineWrapMode_ptr = FunctionWrapper.Load<GdipGetLineWrapMode_delegate>(s_gdipModule, "GdipGetLineWrapMode");
                GdipResetLineTransform_ptr = FunctionWrapper.Load<GdipResetLineTransform_delegate>(s_gdipModule, "GdipResetLineTransform");
                GdipMultiplyLineTransform_ptr = FunctionWrapper.Load<GdipMultiplyLineTransform_delegate>(s_gdipModule, "GdipMultiplyLineTransform");
                GdipGetLineTransform_ptr = FunctionWrapper.Load<GdipGetLineTransform_delegate>(s_gdipModule, "GdipGetLineTransform");
                GdipSetLineTransform_ptr = FunctionWrapper.Load<GdipSetLineTransform_delegate>(s_gdipModule, "GdipSetLineTransform");
                GdipTranslateLineTransform_ptr = FunctionWrapper.Load<GdipTranslateLineTransform_delegate>(s_gdipModule, "GdipTranslateLineTransform");
                GdipScaleLineTransform_ptr = FunctionWrapper.Load<GdipScaleLineTransform_delegate>(s_gdipModule, "GdipScaleLineTransform");
                GdipRotateLineTransform_ptr = FunctionWrapper.Load<GdipRotateLineTransform_delegate>(s_gdipModule, "GdipRotateLineTransform");
                GdipCreatePathGradient_ptr = FunctionWrapper.Load<GdipCreatePathGradient_delegate>(s_gdipModule, "GdipCreatePathGradient");
                GdipCreatePathGradientI_ptr = FunctionWrapper.Load<GdipCreatePathGradientI_delegate>(s_gdipModule, "GdipCreatePathGradientI");
                GdipCreatePathGradientFromPath_ptr = FunctionWrapper.Load<GdipCreatePathGradientFromPath_delegate>(s_gdipModule, "GdipCreatePathGradientFromPath");
                GdipGetPathGradientCenterColor_ptr = FunctionWrapper.Load<GdipGetPathGradientCenterColor_delegate>(s_gdipModule, "GdipGetPathGradientCenterColor");
                GdipSetPathGradientCenterColor_ptr = FunctionWrapper.Load<GdipSetPathGradientCenterColor_delegate>(s_gdipModule, "GdipSetPathGradientCenterColor");
                GdipGetPathGradientSurroundColorsWithCount_ptr = FunctionWrapper.Load<GdipGetPathGradientSurroundColorsWithCount_delegate>(s_gdipModule, "GdipGetPathGradientSurroundColorsWithCount");
                GdipSetPathGradientSurroundColorsWithCount_ptr = FunctionWrapper.Load<GdipSetPathGradientSurroundColorsWithCount_delegate>(s_gdipModule, "GdipSetPathGradientSurroundColorsWithCount");
                GdipGetPathGradientCenterPoint_ptr = FunctionWrapper.Load<GdipGetPathGradientCenterPoint_delegate>(s_gdipModule, "GdipGetPathGradientCenterPoint");
                GdipSetPathGradientCenterPoint_ptr = FunctionWrapper.Load<GdipSetPathGradientCenterPoint_delegate>(s_gdipModule, "GdipSetPathGradientCenterPoint");
                GdipGetPathGradientRect_ptr = FunctionWrapper.Load<GdipGetPathGradientRect_delegate>(s_gdipModule, "GdipGetPathGradientRect");
                GdipGetPathGradientPointCount_ptr = FunctionWrapper.Load<GdipGetPathGradientPointCount_delegate>(s_gdipModule, "GdipGetPathGradientPointCount");
                GdipGetPathGradientSurroundColorCount_ptr = FunctionWrapper.Load<GdipGetPathGradientSurroundColorCount_delegate>(s_gdipModule, "GdipGetPathGradientSurroundColorCount");
                GdipGetPathGradientBlendCount_ptr = FunctionWrapper.Load<GdipGetPathGradientBlendCount_delegate>(s_gdipModule, "GdipGetPathGradientBlendCount");
                GdipGetPathGradientBlend_ptr = FunctionWrapper.Load<GdipGetPathGradientBlend_delegate>(s_gdipModule, "GdipGetPathGradientBlend");
                GdipSetPathGradientBlend_ptr = FunctionWrapper.Load<GdipSetPathGradientBlend_delegate>(s_gdipModule, "GdipSetPathGradientBlend");
                GdipGetPathGradientPresetBlendCount_ptr = FunctionWrapper.Load<GdipGetPathGradientPresetBlendCount_delegate>(s_gdipModule, "GdipGetPathGradientPresetBlendCount");
                GdipGetPathGradientPresetBlend_ptr = FunctionWrapper.Load<GdipGetPathGradientPresetBlend_delegate>(s_gdipModule, "GdipGetPathGradientPresetBlend");
                GdipSetPathGradientPresetBlend_ptr = FunctionWrapper.Load<GdipSetPathGradientPresetBlend_delegate>(s_gdipModule, "GdipSetPathGradientPresetBlend");
                GdipSetPathGradientSigmaBlend_ptr = FunctionWrapper.Load<GdipSetPathGradientSigmaBlend_delegate>(s_gdipModule, "GdipSetPathGradientSigmaBlend");
                GdipSetPathGradientLinearBlend_ptr = FunctionWrapper.Load<GdipSetPathGradientLinearBlend_delegate>(s_gdipModule, "GdipSetPathGradientLinearBlend");
                GdipSetPathGradientWrapMode_ptr = FunctionWrapper.Load<GdipSetPathGradientWrapMode_delegate>(s_gdipModule, "GdipSetPathGradientWrapMode");
                GdipGetPathGradientWrapMode_ptr = FunctionWrapper.Load<GdipGetPathGradientWrapMode_delegate>(s_gdipModule, "GdipGetPathGradientWrapMode");
                GdipSetPathGradientTransform_ptr = FunctionWrapper.Load<GdipSetPathGradientTransform_delegate>(s_gdipModule, "GdipSetPathGradientTransform");
                GdipGetPathGradientTransform_ptr = FunctionWrapper.Load<GdipGetPathGradientTransform_delegate>(s_gdipModule, "GdipGetPathGradientTransform");
                GdipResetPathGradientTransform_ptr = FunctionWrapper.Load<GdipResetPathGradientTransform_delegate>(s_gdipModule, "GdipResetPathGradientTransform");
                GdipMultiplyPathGradientTransform_ptr = FunctionWrapper.Load<GdipMultiplyPathGradientTransform_delegate>(s_gdipModule, "GdipMultiplyPathGradientTransform");
                GdipTranslatePathGradientTransform_ptr = FunctionWrapper.Load<GdipTranslatePathGradientTransform_delegate>(s_gdipModule, "GdipTranslatePathGradientTransform");
                GdipScalePathGradientTransform_ptr = FunctionWrapper.Load<GdipScalePathGradientTransform_delegate>(s_gdipModule, "GdipScalePathGradientTransform");
                GdipRotatePathGradientTransform_ptr = FunctionWrapper.Load<GdipRotatePathGradientTransform_delegate>(s_gdipModule, "GdipRotatePathGradientTransform");
                GdipGetPathGradientFocusScales_ptr = FunctionWrapper.Load<GdipGetPathGradientFocusScales_delegate>(s_gdipModule, "GdipGetPathGradientFocusScales");
                GdipSetPathGradientFocusScales_ptr = FunctionWrapper.Load<GdipSetPathGradientFocusScales_delegate>(s_gdipModule, "GdipSetPathGradientFocusScales");
                GdipCloneBrush_ptr = FunctionWrapper.Load<GdipCloneBrush_delegate>(s_gdipModule, "GdipCloneBrush");
                GdipCreateImageAttributes_ptr = FunctionWrapper.Load<GdipCreateImageAttributes_delegate>(s_gdipModule, "GdipCreateImageAttributes");
                GdipCloneImageAttributes_ptr = FunctionWrapper.Load<GdipCloneImageAttributes_delegate>(s_gdipModule, "GdipCloneImageAttributes");
                GdipDisposeImageAttributes_ptr = FunctionWrapper.Load<GdipDisposeImageAttributes_delegate>(s_gdipModule, "GdipDisposeImageAttributes");
                GdipSetImageAttributesColorMatrix_ptr = FunctionWrapper.Load<GdipSetImageAttributesColorMatrix_delegate>(s_gdipModule, "GdipSetImageAttributesColorMatrix");
                GdipSetImageAttributesThreshold_ptr = FunctionWrapper.Load<GdipSetImageAttributesThreshold_delegate>(s_gdipModule, "GdipSetImageAttributesThreshold");
                GdipSetImageAttributesGamma_ptr = FunctionWrapper.Load<GdipSetImageAttributesGamma_delegate>(s_gdipModule, "GdipSetImageAttributesGamma");
                GdipSetImageAttributesNoOp_ptr = FunctionWrapper.Load<GdipSetImageAttributesNoOp_delegate>(s_gdipModule, "GdipSetImageAttributesNoOp");
                GdipSetImageAttributesColorKeys_ptr = FunctionWrapper.Load<GdipSetImageAttributesColorKeys_delegate>(s_gdipModule, "GdipSetImageAttributesColorKeys");
                GdipSetImageAttributesOutputChannel_ptr = FunctionWrapper.Load<GdipSetImageAttributesOutputChannel_delegate>(s_gdipModule, "GdipSetImageAttributesOutputChannel");
                GdipSetImageAttributesOutputChannelColorProfile_ptr = FunctionWrapper.Load<GdipSetImageAttributesOutputChannelColorProfile_delegate>(s_gdipModule, "GdipSetImageAttributesOutputChannelColorProfile");
                GdipSetImageAttributesRemapTable_ptr = FunctionWrapper.Load<GdipSetImageAttributesRemapTable_delegate>(s_gdipModule, "GdipSetImageAttributesRemapTable");
                GdipSetImageAttributesWrapMode_ptr = FunctionWrapper.Load<GdipSetImageAttributesWrapMode_delegate>(s_gdipModule, "GdipSetImageAttributesWrapMode");
                GdipGetImageAttributesAdjustedPalette_ptr = FunctionWrapper.Load<GdipGetImageAttributesAdjustedPalette_delegate>(s_gdipModule, "GdipGetImageAttributesAdjustedPalette");
                GdipGetImageDecodersSize_ptr = FunctionWrapper.Load<GdipGetImageDecodersSize_delegate>(s_gdipModule, "GdipGetImageDecodersSize");
                GdipGetImageDecoders_ptr = FunctionWrapper.Load<GdipGetImageDecoders_delegate>(s_gdipModule, "GdipGetImageDecoders");
                GdipGetImageEncodersSize_ptr = FunctionWrapper.Load<GdipGetImageEncodersSize_delegate>(s_gdipModule, "GdipGetImageEncodersSize");
                GdipGetImageEncoders_ptr = FunctionWrapper.Load<GdipGetImageEncoders_delegate>(s_gdipModule, "GdipGetImageEncoders");
                GdipCreateSolidFill_ptr = FunctionWrapper.Load<GdipCreateSolidFill_delegate>(s_gdipModule, "GdipCreateSolidFill");
                GdipSetSolidFillColor_ptr = FunctionWrapper.Load<GdipSetSolidFillColor_delegate>(s_gdipModule, "GdipSetSolidFillColor");
                GdipGetSolidFillColor_ptr = FunctionWrapper.Load<GdipGetSolidFillColor_delegate>(s_gdipModule, "GdipGetSolidFillColor");
                GdipCreateTexture_ptr = FunctionWrapper.Load<GdipCreateTexture_delegate>(s_gdipModule, "GdipCreateTexture");
                GdipCreateTexture2_ptr = FunctionWrapper.Load<GdipCreateTexture2_delegate>(s_gdipModule, "GdipCreateTexture2");
                GdipCreateTextureIA_ptr = FunctionWrapper.Load<GdipCreateTextureIA_delegate>(s_gdipModule, "GdipCreateTextureIA");
                GdipCreateTexture2I_ptr = FunctionWrapper.Load<GdipCreateTexture2I_delegate>(s_gdipModule, "GdipCreateTexture2I");
                GdipCreateTextureIAI_ptr = FunctionWrapper.Load<GdipCreateTextureIAI_delegate>(s_gdipModule, "GdipCreateTextureIAI");
                GdipSetTextureTransform_ptr = FunctionWrapper.Load<GdipSetTextureTransform_delegate>(s_gdipModule, "GdipSetTextureTransform");
                GdipGetTextureTransform_ptr = FunctionWrapper.Load<GdipGetTextureTransform_delegate>(s_gdipModule, "GdipGetTextureTransform");
                GdipResetTextureTransform_ptr = FunctionWrapper.Load<GdipResetTextureTransform_delegate>(s_gdipModule, "GdipResetTextureTransform");
                GdipMultiplyTextureTransform_ptr = FunctionWrapper.Load<GdipMultiplyTextureTransform_delegate>(s_gdipModule, "GdipMultiplyTextureTransform");
                GdipTranslateTextureTransform_ptr = FunctionWrapper.Load<GdipTranslateTextureTransform_delegate>(s_gdipModule, "GdipTranslateTextureTransform");
                GdipScaleTextureTransform_ptr = FunctionWrapper.Load<GdipScaleTextureTransform_delegate>(s_gdipModule, "GdipScaleTextureTransform");
                GdipRotateTextureTransform_ptr = FunctionWrapper.Load<GdipRotateTextureTransform_delegate>(s_gdipModule, "GdipRotateTextureTransform");
                GdipSetTextureWrapMode_ptr = FunctionWrapper.Load<GdipSetTextureWrapMode_delegate>(s_gdipModule, "GdipSetTextureWrapMode");
                GdipGetTextureWrapMode_ptr = FunctionWrapper.Load<GdipGetTextureWrapMode_delegate>(s_gdipModule, "GdipGetTextureWrapMode");
                GdipGetTextureImage_ptr = FunctionWrapper.Load<GdipGetTextureImage_delegate>(s_gdipModule, "GdipGetTextureImage");
                GdipGetFontCollectionFamilyCount_ptr = FunctionWrapper.Load<GdipGetFontCollectionFamilyCount_delegate>(s_gdipModule, "GdipGetFontCollectionFamilyCount");
                GdipGetFontCollectionFamilyList_ptr = FunctionWrapper.Load<GdipGetFontCollectionFamilyList_delegate>(s_gdipModule, "GdipGetFontCollectionFamilyList");
                GdipCloneFontFamily_ptr = FunctionWrapper.Load<GdipCloneFontFamily_delegate>(s_gdipModule, "GdipCloneFontFamily");
                GdipCreateFontFamilyFromName_ptr = FunctionWrapper.Load<GdipCreateFontFamilyFromName_delegate>(s_gdipModule, "GdipCreateFontFamilyFromName");
                GdipGetGenericFontFamilySansSerif_ptr = FunctionWrapper.Load<GdipGetGenericFontFamilySansSerif_delegate>(s_gdipModule, "GdipGetGenericFontFamilySansSerif");
                GdipGetGenericFontFamilySerif_ptr = FunctionWrapper.Load<GdipGetGenericFontFamilySerif_delegate>(s_gdipModule, "GdipGetGenericFontFamilySerif");
                GdipGetGenericFontFamilyMonospace_ptr = FunctionWrapper.Load<GdipGetGenericFontFamilyMonospace_delegate>(s_gdipModule, "GdipGetGenericFontFamilyMonospace");
                GdipDeleteFontFamily_ptr = FunctionWrapper.Load<GdipDeleteFontFamily_delegate>(s_gdipModule, "GdipDeleteFontFamily");
                GdipGetFamilyName_ptr = FunctionWrapper.Load<GdipGetFamilyName_delegate>(s_gdipModule, "GdipGetFamilyName");
                GdipIsStyleAvailable_ptr = FunctionWrapper.Load<GdipIsStyleAvailable_delegate>(s_gdipModule, "GdipIsStyleAvailable");
                GdipGetEmHeight_ptr = FunctionWrapper.Load<GdipGetEmHeight_delegate>(s_gdipModule, "GdipGetEmHeight");
                GdipGetCellAscent_ptr = FunctionWrapper.Load<GdipGetCellAscent_delegate>(s_gdipModule, "GdipGetCellAscent");
                GdipGetCellDescent_ptr = FunctionWrapper.Load<GdipGetCellDescent_delegate>(s_gdipModule, "GdipGetCellDescent");
                GdipGetLineSpacing_ptr = FunctionWrapper.Load<GdipGetLineSpacing_delegate>(s_gdipModule, "GdipGetLineSpacing");
                GdipNewInstalledFontCollection_ptr = FunctionWrapper.Load<GdipNewInstalledFontCollection_delegate>(s_gdipModule, "GdipNewInstalledFontCollection");
                GdipNewPrivateFontCollection_ptr = FunctionWrapper.Load<GdipNewPrivateFontCollection_delegate>(s_gdipModule, "GdipNewPrivateFontCollection");
                GdipDeletePrivateFontCollection_ptr = FunctionWrapper.Load<GdipDeletePrivateFontCollection_delegate>(s_gdipModule, "GdipDeletePrivateFontCollection");
                GdipPrivateAddFontFile_ptr = FunctionWrapper.Load<GdipPrivateAddFontFile_delegate>(s_gdipModule, "GdipPrivateAddFontFile");
                GdipPrivateAddMemoryFont_ptr = FunctionWrapper.Load<GdipPrivateAddMemoryFont_delegate>(s_gdipModule, "GdipPrivateAddMemoryFont");
                GdipCreatePen1_ptr = FunctionWrapper.Load<GdipCreatePen1_delegate>(s_gdipModule, "GdipCreatePen1");
                GdipCreatePen2_ptr = FunctionWrapper.Load<GdipCreatePen2_delegate>(s_gdipModule, "GdipCreatePen2");
                GdipClonePen_ptr = FunctionWrapper.Load<GdipClonePen_delegate>(s_gdipModule, "GdipClonePen");
                GdipDeletePen_ptr = FunctionWrapper.Load<GdipDeletePen_delegate>(s_gdipModule, "GdipDeletePen");
                GdipSetPenMode_ptr = FunctionWrapper.Load<GdipSetPenMode_delegate>(s_gdipModule, "GdipSetPenMode");
                GdipGetPenMode_ptr = FunctionWrapper.Load<GdipGetPenMode_delegate>(s_gdipModule, "GdipGetPenMode");
                GdipSetPenWidth_ptr = FunctionWrapper.Load<GdipSetPenWidth_delegate>(s_gdipModule, "GdipSetPenWidth");
                GdipGetPenWidth_ptr = FunctionWrapper.Load<GdipGetPenWidth_delegate>(s_gdipModule, "GdipGetPenWidth");
                GdipSetPenLineCap197819_ptr = FunctionWrapper.Load<GdipSetPenLineCap197819_delegate>(s_gdipModule, "GdipSetPenLineCap197819");
                GdipSetPenStartCap_ptr = FunctionWrapper.Load<GdipSetPenStartCap_delegate>(s_gdipModule, "GdipSetPenStartCap");
                GdipSetPenEndCap_ptr = FunctionWrapper.Load<GdipSetPenEndCap_delegate>(s_gdipModule, "GdipSetPenEndCap");
                GdipGetPenStartCap_ptr = FunctionWrapper.Load<GdipGetPenStartCap_delegate>(s_gdipModule, "GdipGetPenStartCap");
                GdipGetPenEndCap_ptr = FunctionWrapper.Load<GdipGetPenEndCap_delegate>(s_gdipModule, "GdipGetPenEndCap");
                GdipGetPenDashCap197819_ptr = FunctionWrapper.Load<GdipGetPenDashCap197819_delegate>(s_gdipModule, "GdipGetPenDashCap197819");
                GdipSetPenDashCap197819_ptr = FunctionWrapper.Load<GdipSetPenDashCap197819_delegate>(s_gdipModule, "GdipSetPenDashCap197819");
                GdipSetPenLineJoin_ptr = FunctionWrapper.Load<GdipSetPenLineJoin_delegate>(s_gdipModule, "GdipSetPenLineJoin");
                GdipGetPenLineJoin_ptr = FunctionWrapper.Load<GdipGetPenLineJoin_delegate>(s_gdipModule, "GdipGetPenLineJoin");
                GdipSetPenCustomStartCap_ptr = FunctionWrapper.Load<GdipSetPenCustomStartCap_delegate>(s_gdipModule, "GdipSetPenCustomStartCap");
                GdipGetPenCustomStartCap_ptr = FunctionWrapper.Load<GdipGetPenCustomStartCap_delegate>(s_gdipModule, "GdipGetPenCustomStartCap");
                GdipSetPenCustomEndCap_ptr = FunctionWrapper.Load<GdipSetPenCustomEndCap_delegate>(s_gdipModule, "GdipSetPenCustomEndCap");
                GdipGetPenCustomEndCap_ptr = FunctionWrapper.Load<GdipGetPenCustomEndCap_delegate>(s_gdipModule, "GdipGetPenCustomEndCap");
                GdipSetPenMiterLimit_ptr = FunctionWrapper.Load<GdipSetPenMiterLimit_delegate>(s_gdipModule, "GdipSetPenMiterLimit");
                GdipGetPenMiterLimit_ptr = FunctionWrapper.Load<GdipGetPenMiterLimit_delegate>(s_gdipModule, "GdipGetPenMiterLimit");
                GdipSetPenTransform_ptr = FunctionWrapper.Load<GdipSetPenTransform_delegate>(s_gdipModule, "GdipSetPenTransform");
                GdipGetPenTransform_ptr = FunctionWrapper.Load<GdipGetPenTransform_delegate>(s_gdipModule, "GdipGetPenTransform");
                GdipResetPenTransform_ptr = FunctionWrapper.Load<GdipResetPenTransform_delegate>(s_gdipModule, "GdipResetPenTransform");
                GdipMultiplyPenTransform_ptr = FunctionWrapper.Load<GdipMultiplyPenTransform_delegate>(s_gdipModule, "GdipMultiplyPenTransform");
                GdipTranslatePenTransform_ptr = FunctionWrapper.Load<GdipTranslatePenTransform_delegate>(s_gdipModule, "GdipTranslatePenTransform");
                GdipScalePenTransform_ptr = FunctionWrapper.Load<GdipScalePenTransform_delegate>(s_gdipModule, "GdipScalePenTransform");
                GdipRotatePenTransform_ptr = FunctionWrapper.Load<GdipRotatePenTransform_delegate>(s_gdipModule, "GdipRotatePenTransform");
                GdipSetPenColor_ptr = FunctionWrapper.Load<GdipSetPenColor_delegate>(s_gdipModule, "GdipSetPenColor");
                GdipGetPenColor_ptr = FunctionWrapper.Load<GdipGetPenColor_delegate>(s_gdipModule, "GdipGetPenColor");
                GdipSetPenBrushFill_ptr = FunctionWrapper.Load<GdipSetPenBrushFill_delegate>(s_gdipModule, "GdipSetPenBrushFill");
                GdipGetPenBrushFill_ptr = FunctionWrapper.Load<GdipGetPenBrushFill_delegate>(s_gdipModule, "GdipGetPenBrushFill");
                GdipGetPenFillType_ptr = FunctionWrapper.Load<GdipGetPenFillType_delegate>(s_gdipModule, "GdipGetPenFillType");
                GdipGetPenDashStyle_ptr = FunctionWrapper.Load<GdipGetPenDashStyle_delegate>(s_gdipModule, "GdipGetPenDashStyle");
                GdipSetPenDashStyle_ptr = FunctionWrapper.Load<GdipSetPenDashStyle_delegate>(s_gdipModule, "GdipSetPenDashStyle");
                GdipSetPenDashArray_ptr = FunctionWrapper.Load<GdipSetPenDashArray_delegate>(s_gdipModule, "GdipSetPenDashArray");
                GdipGetPenDashOffset_ptr = FunctionWrapper.Load<GdipGetPenDashOffset_delegate>(s_gdipModule, "GdipGetPenDashOffset");
                GdipSetPenDashOffset_ptr = FunctionWrapper.Load<GdipSetPenDashOffset_delegate>(s_gdipModule, "GdipSetPenDashOffset");
                GdipGetPenDashCount_ptr = FunctionWrapper.Load<GdipGetPenDashCount_delegate>(s_gdipModule, "GdipGetPenDashCount");
                GdipGetPenDashArray_ptr = FunctionWrapper.Load<GdipGetPenDashArray_delegate>(s_gdipModule, "GdipGetPenDashArray");
                GdipGetPenCompoundCount_ptr = FunctionWrapper.Load<GdipGetPenCompoundCount_delegate>(s_gdipModule, "GdipGetPenCompoundCount");
                GdipSetPenCompoundArray_ptr = FunctionWrapper.Load<GdipSetPenCompoundArray_delegate>(s_gdipModule, "GdipSetPenCompoundArray");
                GdipGetPenCompoundArray_ptr = FunctionWrapper.Load<GdipGetPenCompoundArray_delegate>(s_gdipModule, "GdipGetPenCompoundArray");
                GdipSetWorldTransform_ptr = FunctionWrapper.Load<GdipSetWorldTransform_delegate>(s_gdipModule, "GdipSetWorldTransform");
                GdipResetWorldTransform_ptr = FunctionWrapper.Load<GdipResetWorldTransform_delegate>(s_gdipModule, "GdipResetWorldTransform");
                GdipMultiplyWorldTransform_ptr = FunctionWrapper.Load<GdipMultiplyWorldTransform_delegate>(s_gdipModule, "GdipMultiplyWorldTransform");
                GdipTranslateWorldTransform_ptr = FunctionWrapper.Load<GdipTranslateWorldTransform_delegate>(s_gdipModule, "GdipTranslateWorldTransform");
                GdipScaleWorldTransform_ptr = FunctionWrapper.Load<GdipScaleWorldTransform_delegate>(s_gdipModule, "GdipScaleWorldTransform");
                GdipRotateWorldTransform_ptr = FunctionWrapper.Load<GdipRotateWorldTransform_delegate>(s_gdipModule, "GdipRotateWorldTransform");
                GdipGetWorldTransform_ptr = FunctionWrapper.Load<GdipGetWorldTransform_delegate>(s_gdipModule, "GdipGetWorldTransform");
                GdipCreateMatrix_ptr = FunctionWrapper.Load<GdipCreateMatrix_delegate>(s_gdipModule, "GdipCreateMatrix");
                GdipCreateMatrix2_ptr = FunctionWrapper.Load<GdipCreateMatrix2_delegate>(s_gdipModule, "GdipCreateMatrix2");
                GdipCreateMatrix3_ptr = FunctionWrapper.Load<GdipCreateMatrix3_delegate>(s_gdipModule, "GdipCreateMatrix3");
                GdipCreateMatrix3I_ptr = FunctionWrapper.Load<GdipCreateMatrix3I_delegate>(s_gdipModule, "GdipCreateMatrix3I");
                GdipCloneMatrix_ptr = FunctionWrapper.Load<GdipCloneMatrix_delegate>(s_gdipModule, "GdipCloneMatrix");
                GdipDeleteMatrix_ptr = FunctionWrapper.Load<GdipDeleteMatrix_delegate>(s_gdipModule, "GdipDeleteMatrix");
                GdipSetMatrixElements_ptr = FunctionWrapper.Load<GdipSetMatrixElements_delegate>(s_gdipModule, "GdipSetMatrixElements");
                GdipMultiplyMatrix_ptr = FunctionWrapper.Load<GdipMultiplyMatrix_delegate>(s_gdipModule, "GdipMultiplyMatrix");
                GdipTranslateMatrix_ptr = FunctionWrapper.Load<GdipTranslateMatrix_delegate>(s_gdipModule, "GdipTranslateMatrix");
                GdipScaleMatrix_ptr = FunctionWrapper.Load<GdipScaleMatrix_delegate>(s_gdipModule, "GdipScaleMatrix");
                GdipRotateMatrix_ptr = FunctionWrapper.Load<GdipRotateMatrix_delegate>(s_gdipModule, "GdipRotateMatrix");
                GdipShearMatrix_ptr = FunctionWrapper.Load<GdipShearMatrix_delegate>(s_gdipModule, "GdipShearMatrix");
                GdipInvertMatrix_ptr = FunctionWrapper.Load<GdipInvertMatrix_delegate>(s_gdipModule, "GdipInvertMatrix");
                GdipTransformMatrixPoints_ptr = FunctionWrapper.Load<GdipTransformMatrixPoints_delegate>(s_gdipModule, "GdipTransformMatrixPoints");
                GdipTransformMatrixPointsI_ptr = FunctionWrapper.Load<GdipTransformMatrixPointsI_delegate>(s_gdipModule, "GdipTransformMatrixPointsI");
                GdipVectorTransformMatrixPoints_ptr = FunctionWrapper.Load<GdipVectorTransformMatrixPoints_delegate>(s_gdipModule, "GdipVectorTransformMatrixPoints");
                GdipVectorTransformMatrixPointsI_ptr = FunctionWrapper.Load<GdipVectorTransformMatrixPointsI_delegate>(s_gdipModule, "GdipVectorTransformMatrixPointsI");
                GdipGetMatrixElements_ptr = FunctionWrapper.Load<GdipGetMatrixElements_delegate>(s_gdipModule, "GdipGetMatrixElements");
                GdipIsMatrixInvertible_ptr = FunctionWrapper.Load<GdipIsMatrixInvertible_delegate>(s_gdipModule, "GdipIsMatrixInvertible");
                GdipIsMatrixIdentity_ptr = FunctionWrapper.Load<GdipIsMatrixIdentity_delegate>(s_gdipModule, "GdipIsMatrixIdentity");
                GdipIsMatrixEqual_ptr = FunctionWrapper.Load<GdipIsMatrixEqual_delegate>(s_gdipModule, "GdipIsMatrixEqual");
                GdipCreateRegion_ptr = FunctionWrapper.Load<GdipCreateRegion_delegate>(s_gdipModule, "GdipCreateRegion");
                GdipCreateRegionRect_ptr = FunctionWrapper.Load<GdipCreateRegionRect_delegate>(s_gdipModule, "GdipCreateRegionRect");
                GdipCreateRegionRectI_ptr = FunctionWrapper.Load<GdipCreateRegionRectI_delegate>(s_gdipModule, "GdipCreateRegionRectI");
                GdipCreateRegionPath_ptr = FunctionWrapper.Load<GdipCreateRegionPath_delegate>(s_gdipModule, "GdipCreateRegionPath");
                GdipCreateRegionRgnData_ptr = FunctionWrapper.Load<GdipCreateRegionRgnData_delegate>(s_gdipModule, "GdipCreateRegionRgnData");
                GdipCreateRegionHrgn_ptr = FunctionWrapper.Load<GdipCreateRegionHrgn_delegate>(s_gdipModule, "GdipCreateRegionHrgn");
                GdipCloneRegion_ptr = FunctionWrapper.Load<GdipCloneRegion_delegate>(s_gdipModule, "GdipCloneRegion");
                GdipDeleteRegion_ptr = FunctionWrapper.Load<GdipDeleteRegion_delegate>(s_gdipModule, "GdipDeleteRegion");
                GdipFillRegion_ptr = FunctionWrapper.Load<GdipFillRegion_delegate>(s_gdipModule, "GdipFillRegion");
                GdipSetInfinite_ptr = FunctionWrapper.Load<GdipSetInfinite_delegate>(s_gdipModule, "GdipSetInfinite");
                GdipSetEmpty_ptr = FunctionWrapper.Load<GdipSetEmpty_delegate>(s_gdipModule, "GdipSetEmpty");
                GdipCombineRegionRect_ptr = FunctionWrapper.Load<GdipCombineRegionRect_delegate>(s_gdipModule, "GdipCombineRegionRect");
                GdipCombineRegionRectI_ptr = FunctionWrapper.Load<GdipCombineRegionRectI_delegate>(s_gdipModule, "GdipCombineRegionRectI");
                GdipCombineRegionPath_ptr = FunctionWrapper.Load<GdipCombineRegionPath_delegate>(s_gdipModule, "GdipCombineRegionPath");
                GdipCombineRegionRegion_ptr = FunctionWrapper.Load<GdipCombineRegionRegion_delegate>(s_gdipModule, "GdipCombineRegionRegion");
                GdipTranslateRegion_ptr = FunctionWrapper.Load<GdipTranslateRegion_delegate>(s_gdipModule, "GdipTranslateRegion");
                GdipTranslateRegionI_ptr = FunctionWrapper.Load<GdipTranslateRegionI_delegate>(s_gdipModule, "GdipTranslateRegionI");
                GdipTransformRegion_ptr = FunctionWrapper.Load<GdipTransformRegion_delegate>(s_gdipModule, "GdipTransformRegion");
                GdipGetRegionBounds_ptr = FunctionWrapper.Load<GdipGetRegionBounds_delegate>(s_gdipModule, "GdipGetRegionBounds");
                GdipGetRegionHRgn_ptr = FunctionWrapper.Load<GdipGetRegionHRgn_delegate>(s_gdipModule, "GdipGetRegionHRgn");
                GdipIsEmptyRegion_ptr = FunctionWrapper.Load<GdipIsEmptyRegion_delegate>(s_gdipModule, "GdipIsEmptyRegion");
                GdipIsInfiniteRegion_ptr = FunctionWrapper.Load<GdipIsInfiniteRegion_delegate>(s_gdipModule, "GdipIsInfiniteRegion");
                GdipIsEqualRegion_ptr = FunctionWrapper.Load<GdipIsEqualRegion_delegate>(s_gdipModule, "GdipIsEqualRegion");
                GdipGetRegionDataSize_ptr = FunctionWrapper.Load<GdipGetRegionDataSize_delegate>(s_gdipModule, "GdipGetRegionDataSize");
                GdipGetRegionData_ptr = FunctionWrapper.Load<GdipGetRegionData_delegate>(s_gdipModule, "GdipGetRegionData");
                GdipIsVisibleRegionPoint_ptr = FunctionWrapper.Load<GdipIsVisibleRegionPoint_delegate>(s_gdipModule, "GdipIsVisibleRegionPoint");
                GdipIsVisibleRegionPointI_ptr = FunctionWrapper.Load<GdipIsVisibleRegionPointI_delegate>(s_gdipModule, "GdipIsVisibleRegionPointI");
                GdipIsVisibleRegionRect_ptr = FunctionWrapper.Load<GdipIsVisibleRegionRect_delegate>(s_gdipModule, "GdipIsVisibleRegionRect");
                GdipIsVisibleRegionRectI_ptr = FunctionWrapper.Load<GdipIsVisibleRegionRectI_delegate>(s_gdipModule, "GdipIsVisibleRegionRectI");
                GdipGetRegionScansCount_ptr = FunctionWrapper.Load<GdipGetRegionScansCount_delegate>(s_gdipModule, "GdipGetRegionScansCount");
                GdipGetRegionScans_ptr = FunctionWrapper.Load<GdipGetRegionScans_delegate>(s_gdipModule, "GdipGetRegionScans");
                GdipSetClipGraphics_ptr = FunctionWrapper.Load<GdipSetClipGraphics_delegate>(s_gdipModule, "GdipSetClipGraphics");
                GdipSetClipRect_ptr = FunctionWrapper.Load<GdipSetClipRect_delegate>(s_gdipModule, "GdipSetClipRect");
                GdipSetClipRectI_ptr = FunctionWrapper.Load<GdipSetClipRectI_delegate>(s_gdipModule, "GdipSetClipRectI");
                GdipSetClipPath_ptr = FunctionWrapper.Load<GdipSetClipPath_delegate>(s_gdipModule, "GdipSetClipPath");
                GdipSetClipRegion_ptr = FunctionWrapper.Load<GdipSetClipRegion_delegate>(s_gdipModule, "GdipSetClipRegion");
                GdipResetClip_ptr = FunctionWrapper.Load<GdipResetClip_delegate>(s_gdipModule, "GdipResetClip");
                GdipTranslateClip_ptr = FunctionWrapper.Load<GdipTranslateClip_delegate>(s_gdipModule, "GdipTranslateClip");
                GdipGetClip_ptr = FunctionWrapper.Load<GdipGetClip_delegate>(s_gdipModule, "GdipGetClip");
                GdipGetClipBounds_ptr = FunctionWrapper.Load<GdipGetClipBounds_delegate>(s_gdipModule, "GdipGetClipBounds");
                GdipIsClipEmpty_ptr = FunctionWrapper.Load<GdipIsClipEmpty_delegate>(s_gdipModule, "GdipIsClipEmpty");
                GdipGetVisibleClipBounds_ptr = FunctionWrapper.Load<GdipGetVisibleClipBounds_delegate>(s_gdipModule, "GdipGetVisibleClipBounds");
                GdipIsVisibleClipEmpty_ptr = FunctionWrapper.Load<GdipIsVisibleClipEmpty_delegate>(s_gdipModule, "GdipIsVisibleClipEmpty");
                GdipIsVisiblePoint_ptr = FunctionWrapper.Load<GdipIsVisiblePoint_delegate>(s_gdipModule, "GdipIsVisiblePoint");
                GdipIsVisiblePointI_ptr = FunctionWrapper.Load<GdipIsVisiblePointI_delegate>(s_gdipModule, "GdipIsVisiblePointI");
                GdipIsVisibleRect_ptr = FunctionWrapper.Load<GdipIsVisibleRect_delegate>(s_gdipModule, "GdipIsVisibleRect");
                GdipIsVisibleRectI_ptr = FunctionWrapper.Load<GdipIsVisibleRectI_delegate>(s_gdipModule, "GdipIsVisibleRectI");
                GdipSetStringFormatMeasurableCharacterRanges_ptr = FunctionWrapper.Load<GdipSetStringFormatMeasurableCharacterRanges_delegate>(s_gdipModule, "GdipSetStringFormatMeasurableCharacterRanges");
                GdipCreateStringFormat_ptr = FunctionWrapper.Load<GdipCreateStringFormat_delegate>(s_gdipModule, "GdipCreateStringFormat");
                GdipStringFormatGetGenericDefault_ptr = FunctionWrapper.Load<GdipStringFormatGetGenericDefault_delegate>(s_gdipModule, "GdipStringFormatGetGenericDefault");
                GdipStringFormatGetGenericTypographic_ptr = FunctionWrapper.Load<GdipStringFormatGetGenericTypographic_delegate>(s_gdipModule, "GdipStringFormatGetGenericTypographic");
                GdipDeleteStringFormat_ptr = FunctionWrapper.Load<GdipDeleteStringFormat_delegate>(s_gdipModule, "GdipDeleteStringFormat");
                GdipCloneStringFormat_ptr = FunctionWrapper.Load<GdipCloneStringFormat_delegate>(s_gdipModule, "GdipCloneStringFormat");
                GdipSetStringFormatFlags_ptr = FunctionWrapper.Load<GdipSetStringFormatFlags_delegate>(s_gdipModule, "GdipSetStringFormatFlags");
                GdipGetStringFormatFlags_ptr = FunctionWrapper.Load<GdipGetStringFormatFlags_delegate>(s_gdipModule, "GdipGetStringFormatFlags");
                GdipSetStringFormatAlign_ptr = FunctionWrapper.Load<GdipSetStringFormatAlign_delegate>(s_gdipModule, "GdipSetStringFormatAlign");
                GdipGetStringFormatAlign_ptr = FunctionWrapper.Load<GdipGetStringFormatAlign_delegate>(s_gdipModule, "GdipGetStringFormatAlign");
                GdipSetStringFormatLineAlign_ptr = FunctionWrapper.Load<GdipSetStringFormatLineAlign_delegate>(s_gdipModule, "GdipSetStringFormatLineAlign");
                GdipGetStringFormatLineAlign_ptr = FunctionWrapper.Load<GdipGetStringFormatLineAlign_delegate>(s_gdipModule, "GdipGetStringFormatLineAlign");
                GdipSetStringFormatHotkeyPrefix_ptr = FunctionWrapper.Load<GdipSetStringFormatHotkeyPrefix_delegate>(s_gdipModule, "GdipSetStringFormatHotkeyPrefix");
                GdipGetStringFormatHotkeyPrefix_ptr = FunctionWrapper.Load<GdipGetStringFormatHotkeyPrefix_delegate>(s_gdipModule, "GdipGetStringFormatHotkeyPrefix");
                GdipSetStringFormatTabStops_ptr = FunctionWrapper.Load<GdipSetStringFormatTabStops_delegate>(s_gdipModule, "GdipSetStringFormatTabStops");
                GdipGetStringFormatTabStops_ptr = FunctionWrapper.Load<GdipGetStringFormatTabStops_delegate>(s_gdipModule, "GdipGetStringFormatTabStops");
                GdipGetStringFormatTabStopCount_ptr = FunctionWrapper.Load<GdipGetStringFormatTabStopCount_delegate>(s_gdipModule, "GdipGetStringFormatTabStopCount");
                GdipGetStringFormatMeasurableCharacterRangeCount_ptr = FunctionWrapper.Load<GdipGetStringFormatMeasurableCharacterRangeCount_delegate>(s_gdipModule, "GdipGetStringFormatMeasurableCharacterRangeCount");
                GdipSetStringFormatTrimming_ptr = FunctionWrapper.Load<GdipSetStringFormatTrimming_delegate>(s_gdipModule, "GdipSetStringFormatTrimming");
                GdipGetStringFormatTrimming_ptr = FunctionWrapper.Load<GdipGetStringFormatTrimming_delegate>(s_gdipModule, "GdipGetStringFormatTrimming");
                GdipSetStringFormatDigitSubstitution_ptr = FunctionWrapper.Load<GdipSetStringFormatDigitSubstitution_delegate>(s_gdipModule, "GdipSetStringFormatDigitSubstitution");
                GdipGetStringFormatDigitSubstitution_ptr = FunctionWrapper.Load<GdipGetStringFormatDigitSubstitution_delegate>(s_gdipModule, "GdipGetStringFormatDigitSubstitution");
            }

            // Shared function imports (all platforms)
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

            private delegate int GdipCreateAdjustableArrowCap_delegate(float height, float width, bool isFilled, out IntPtr adjustableArrowCap);
            private static FunctionWrapper<GdipCreateAdjustableArrowCap_delegate> GdipCreateAdjustableArrowCap_ptr;
            internal static int GdipCreateAdjustableArrowCap(float height, float width, bool isFilled, out IntPtr adjustableArrowCap) => GdipCreateAdjustableArrowCap_ptr.Delegate(height, width, isFilled, out adjustableArrowCap);

            private delegate int GdipGetAdjustableArrowCapHeight_delegate(HandleRef adjustableArrowCap, out float height);
            private static FunctionWrapper<GdipGetAdjustableArrowCapHeight_delegate> GdipGetAdjustableArrowCapHeight_ptr;
            internal static int GdipGetAdjustableArrowCapHeight(HandleRef adjustableArrowCap, out float height) => GdipGetAdjustableArrowCapHeight_ptr.Delegate(adjustableArrowCap, out height);

            private delegate int GdipSetAdjustableArrowCapHeight_delegate(HandleRef adjustableArrowCap, float height);
            private static FunctionWrapper<GdipSetAdjustableArrowCapHeight_delegate> GdipSetAdjustableArrowCapHeight_ptr;
            internal static int GdipSetAdjustableArrowCapHeight(HandleRef adjustableArrowCap, float height) => GdipSetAdjustableArrowCapHeight_ptr.Delegate(adjustableArrowCap, height);

            private delegate int GdipSetAdjustableArrowCapWidth_delegate(HandleRef adjustableArrowCap, float width);
            private static FunctionWrapper<GdipSetAdjustableArrowCapWidth_delegate> GdipSetAdjustableArrowCapWidth_ptr;
            internal static int GdipSetAdjustableArrowCapWidth(HandleRef adjustableArrowCap, float width) => GdipSetAdjustableArrowCapWidth_ptr.Delegate(adjustableArrowCap, width);

            private delegate int GdipGetAdjustableArrowCapWidth_delegate(HandleRef adjustableArrowCap, out float width);
            private static FunctionWrapper<GdipGetAdjustableArrowCapWidth_delegate> GdipGetAdjustableArrowCapWidth_ptr;
            internal static int GdipGetAdjustableArrowCapWidth(HandleRef adjustableArrowCap, out float width) => GdipGetAdjustableArrowCapWidth_ptr.Delegate(adjustableArrowCap, out width);

            private delegate int GdipSetAdjustableArrowCapMiddleInset_delegate(HandleRef adjustableArrowCap, float middleInset);
            private static FunctionWrapper<GdipSetAdjustableArrowCapMiddleInset_delegate> GdipSetAdjustableArrowCapMiddleInset_ptr;
            internal static int GdipSetAdjustableArrowCapMiddleInset(HandleRef adjustableArrowCap, float middleInset) => GdipSetAdjustableArrowCapMiddleInset_ptr.Delegate(adjustableArrowCap, middleInset);

            private delegate int GdipGetAdjustableArrowCapMiddleInset_delegate(HandleRef adjustableArrowCap, out float middleInset);
            private static FunctionWrapper<GdipGetAdjustableArrowCapMiddleInset_delegate> GdipGetAdjustableArrowCapMiddleInset_ptr;
            internal static int GdipGetAdjustableArrowCapMiddleInset(HandleRef adjustableArrowCap, out float middleInset) => GdipGetAdjustableArrowCapMiddleInset_ptr.Delegate(adjustableArrowCap, out middleInset);

            private delegate int GdipSetAdjustableArrowCapFillState_delegate(HandleRef adjustableArrowCap, bool fillState);
            private static FunctionWrapper<GdipSetAdjustableArrowCapFillState_delegate> GdipSetAdjustableArrowCapFillState_ptr;
            internal static int GdipSetAdjustableArrowCapFillState(HandleRef adjustableArrowCap, bool fillState) => GdipSetAdjustableArrowCapFillState_ptr.Delegate(adjustableArrowCap, fillState);

            private delegate int GdipGetAdjustableArrowCapFillState_delegate(HandleRef adjustableArrowCap, out bool fillState);
            private static FunctionWrapper<GdipGetAdjustableArrowCapFillState_delegate> GdipGetAdjustableArrowCapFillState_ptr;
            internal static int GdipGetAdjustableArrowCapFillState(HandleRef adjustableArrowCap, out bool fillState) => GdipGetAdjustableArrowCapFillState_ptr.Delegate(adjustableArrowCap, out fillState);

            private delegate int GdipGetCustomLineCapType_delegate(HandleRef customCap, out CustomLineCapType capType);
            private static FunctionWrapper<GdipGetCustomLineCapType_delegate> GdipGetCustomLineCapType_ptr;
            internal static int GdipGetCustomLineCapType(HandleRef customCap, out CustomLineCapType capType) => GdipGetCustomLineCapType_ptr.Delegate(customCap, out capType);

            private delegate int GdipCreateCustomLineCap_delegate(HandleRef fillpath, HandleRef strokepath, LineCap baseCap, float baseInset, out IntPtr customCap);
            private static FunctionWrapper<GdipCreateCustomLineCap_delegate> GdipCreateCustomLineCap_ptr;
            internal static int GdipCreateCustomLineCap(HandleRef fillpath, HandleRef strokepath, LineCap baseCap, float baseInset, out IntPtr customCap) => GdipCreateCustomLineCap_ptr.Delegate(fillpath, strokepath, baseCap, baseInset, out customCap);

            private delegate int GdipDeleteCustomLineCap_delegate(HandleRef customCap);
            private static FunctionWrapper<GdipDeleteCustomLineCap_delegate> GdipDeleteCustomLineCap_ptr;
            internal static int IntGdipDeleteCustomLineCap(HandleRef customCap) => GdipDeleteCustomLineCap_ptr.Delegate(customCap);

            private delegate int GdipCloneCustomLineCap_delegate(HandleRef customCap, out IntPtr clonedCap);
            private static FunctionWrapper<GdipCloneCustomLineCap_delegate> GdipCloneCustomLineCap_ptr;
            internal static int GdipCloneCustomLineCap(HandleRef customCap, out IntPtr clonedCap) => GdipCloneCustomLineCap_ptr.Delegate(customCap, out clonedCap);

            private delegate int GdipSetCustomLineCapStrokeCaps_delegate(HandleRef customCap, LineCap startCap, LineCap endCap);
            private static FunctionWrapper<GdipSetCustomLineCapStrokeCaps_delegate> GdipSetCustomLineCapStrokeCaps_ptr;
            internal static int GdipSetCustomLineCapStrokeCaps(HandleRef customCap, LineCap startCap, LineCap endCap) => GdipSetCustomLineCapStrokeCaps_ptr.Delegate(customCap, startCap, endCap);

            private delegate int GdipGetCustomLineCapStrokeCaps_delegate(HandleRef customCap, out LineCap startCap, out LineCap endCap);
            private static FunctionWrapper<GdipGetCustomLineCapStrokeCaps_delegate> GdipGetCustomLineCapStrokeCaps_ptr;
            internal static int GdipGetCustomLineCapStrokeCaps(HandleRef customCap, out LineCap startCap, out LineCap endCap) => GdipGetCustomLineCapStrokeCaps_ptr.Delegate(customCap, out startCap, out endCap);

            private delegate int GdipSetCustomLineCapStrokeJoin_delegate(HandleRef customCap, LineJoin lineJoin);
            private static FunctionWrapper<GdipSetCustomLineCapStrokeJoin_delegate> GdipSetCustomLineCapStrokeJoin_ptr;
            internal static int GdipSetCustomLineCapStrokeJoin(HandleRef customCap, LineJoin lineJoin) => GdipSetCustomLineCapStrokeJoin_ptr.Delegate(customCap, lineJoin);

            private delegate int GdipGetCustomLineCapStrokeJoin_delegate(HandleRef customCap, out LineJoin lineJoin);
            private static FunctionWrapper<GdipGetCustomLineCapStrokeJoin_delegate> GdipGetCustomLineCapStrokeJoin_ptr;
            internal static int GdipGetCustomLineCapStrokeJoin(HandleRef customCap, out LineJoin lineJoin) => GdipGetCustomLineCapStrokeJoin_ptr.Delegate(customCap, out lineJoin);

            private delegate int GdipSetCustomLineCapBaseCap_delegate(HandleRef customCap, LineCap baseCap);
            private static FunctionWrapper<GdipSetCustomLineCapBaseCap_delegate> GdipSetCustomLineCapBaseCap_ptr;
            internal static int GdipSetCustomLineCapBaseCap(HandleRef customCap, LineCap baseCap) => GdipSetCustomLineCapBaseCap_ptr.Delegate(customCap, baseCap);

            private delegate int GdipGetCustomLineCapBaseCap_delegate(HandleRef customCap, out LineCap baseCap);
            private static FunctionWrapper<GdipGetCustomLineCapBaseCap_delegate> GdipGetCustomLineCapBaseCap_ptr;
            internal static int GdipGetCustomLineCapBaseCap(HandleRef customCap, out LineCap baseCap) => GdipGetCustomLineCapBaseCap_ptr.Delegate(customCap, out baseCap);

            private delegate int GdipSetCustomLineCapBaseInset_delegate(HandleRef customCap, float inset);
            private static FunctionWrapper<GdipSetCustomLineCapBaseInset_delegate> GdipSetCustomLineCapBaseInset_ptr;
            internal static int GdipSetCustomLineCapBaseInset(HandleRef customCap, float inset) => GdipSetCustomLineCapBaseInset_ptr.Delegate(customCap, inset);

            private delegate int GdipGetCustomLineCapBaseInset_delegate(HandleRef customCap, out float inset);
            private static FunctionWrapper<GdipGetCustomLineCapBaseInset_delegate> GdipGetCustomLineCapBaseInset_ptr;
            internal static int GdipGetCustomLineCapBaseInset(HandleRef customCap, out float inset) => GdipGetCustomLineCapBaseInset_ptr.Delegate(customCap, out inset);

            private delegate int GdipSetCustomLineCapWidthScale_delegate(HandleRef customCap, float widthScale);
            private static FunctionWrapper<GdipSetCustomLineCapWidthScale_delegate> GdipSetCustomLineCapWidthScale_ptr;
            internal static int GdipSetCustomLineCapWidthScale(HandleRef customCap, float widthScale) => GdipSetCustomLineCapWidthScale_ptr.Delegate(customCap, widthScale);

            private delegate int GdipGetCustomLineCapWidthScale_delegate(HandleRef customCap, out float widthScale);
            private static FunctionWrapper<GdipGetCustomLineCapWidthScale_delegate> GdipGetCustomLineCapWidthScale_ptr;
            internal static int GdipGetCustomLineCapWidthScale(HandleRef customCap, out float widthScale) => GdipGetCustomLineCapWidthScale_ptr.Delegate(customCap, out widthScale);

            private delegate int GdipCreatePathIter_delegate(out IntPtr pathIter, HandleRef path);
            private static FunctionWrapper<GdipCreatePathIter_delegate> GdipCreatePathIter_ptr;
            internal static int GdipCreatePathIter(out IntPtr pathIter, HandleRef path) => GdipCreatePathIter_ptr.Delegate(out pathIter, path);

            private delegate int GdipDeletePathIter_delegate(HandleRef pathIter);
            private static FunctionWrapper<GdipDeletePathIter_delegate> GdipDeletePathIter_ptr;
            internal static int IntGdipDeletePathIter(HandleRef pathIter) => GdipDeletePathIter_ptr.Delegate(pathIter);

            private delegate int GdipPathIterNextSubpath_delegate(HandleRef pathIter, out int resultCount, out int startIndex, out int endIndex, out bool isClosed);
            private static FunctionWrapper<GdipPathIterNextSubpath_delegate> GdipPathIterNextSubpath_ptr;
            internal static int GdipPathIterNextSubpath(HandleRef pathIter, out int resultCount, out int startIndex, out int endIndex, out bool isClosed) => GdipPathIterNextSubpath_ptr.Delegate(pathIter, out resultCount, out startIndex, out endIndex, out isClosed);

            private delegate int GdipPathIterNextSubpathPath_delegate(HandleRef pathIter, out int resultCount, HandleRef path, out bool isClosed);
            private static FunctionWrapper<GdipPathIterNextSubpathPath_delegate> GdipPathIterNextSubpathPath_ptr;
            internal static int GdipPathIterNextSubpathPath(HandleRef pathIter, out int resultCount, HandleRef path, out bool isClosed) => GdipPathIterNextSubpathPath_ptr.Delegate(pathIter, out resultCount, path, out isClosed);

            private delegate int GdipPathIterNextPathType_delegate(HandleRef pathIter, out int resultCount, out byte pathType, out int startIndex, out int endIndex);
            private static FunctionWrapper<GdipPathIterNextPathType_delegate> GdipPathIterNextPathType_ptr;
            internal static int GdipPathIterNextPathType(HandleRef pathIter, out int resultCount, out byte pathType, out int startIndex, out int endIndex) => GdipPathIterNextPathType_ptr.Delegate(pathIter, out resultCount, out pathType, out startIndex, out endIndex);

            private delegate int GdipPathIterNextMarker_delegate(HandleRef pathIter, out int resultCount, out int startIndex, out int endIndex);
            private static FunctionWrapper<GdipPathIterNextMarker_delegate> GdipPathIterNextMarker_ptr;
            internal static int GdipPathIterNextMarker(HandleRef pathIter, out int resultCount, out int startIndex, out int endIndex) => GdipPathIterNextMarker_ptr.Delegate(pathIter, out resultCount, out startIndex, out endIndex);

            private delegate int GdipPathIterNextMarkerPath_delegate(HandleRef pathIter, out int resultCount, HandleRef path);
            private static FunctionWrapper<GdipPathIterNextMarkerPath_delegate> GdipPathIterNextMarkerPath_ptr;
            internal static int GdipPathIterNextMarkerPath(HandleRef pathIter, out int resultCount, HandleRef path) => GdipPathIterNextMarkerPath_ptr.Delegate(pathIter, out resultCount, path);

            private delegate int GdipPathIterGetCount_delegate(HandleRef pathIter, out int count);
            private static FunctionWrapper<GdipPathIterGetCount_delegate> GdipPathIterGetCount_ptr;
            internal static int GdipPathIterGetCount(HandleRef pathIter, out int count) => GdipPathIterGetCount_ptr.Delegate(pathIter, out count);

            private delegate int GdipPathIterGetSubpathCount_delegate(HandleRef pathIter, out int count);
            private static FunctionWrapper<GdipPathIterGetSubpathCount_delegate> GdipPathIterGetSubpathCount_ptr;
            internal static int GdipPathIterGetSubpathCount(HandleRef pathIter, out int count) => GdipPathIterGetSubpathCount_ptr.Delegate(pathIter, out count);

            private delegate int GdipPathIterHasCurve_delegate(HandleRef pathIter, out bool hasCurve);
            private static FunctionWrapper<GdipPathIterHasCurve_delegate> GdipPathIterHasCurve_ptr;
            internal static int GdipPathIterHasCurve(HandleRef pathIter, out bool hasCurve) => GdipPathIterHasCurve_ptr.Delegate(pathIter, out hasCurve);

            private delegate int GdipPathIterRewind_delegate(HandleRef pathIter);
            private static FunctionWrapper<GdipPathIterRewind_delegate> GdipPathIterRewind_ptr;
            internal static int GdipPathIterRewind(HandleRef pathIter) => GdipPathIterRewind_ptr.Delegate(pathIter);

            private delegate int GdipPathIterEnumerate_delegate(HandleRef pathIter, out int resultCount, IntPtr memoryPts, [In] [Out] byte[] types, int count);
            private static FunctionWrapper<GdipPathIterEnumerate_delegate> GdipPathIterEnumerate_ptr;
            internal static int GdipPathIterEnumerate(HandleRef pathIter, out int resultCount, IntPtr memoryPts, [In] [Out] byte[] types, int count) => GdipPathIterEnumerate_ptr.Delegate(pathIter, out resultCount, memoryPts, types, count);

            private delegate int GdipPathIterCopyData_delegate(HandleRef pathIter, out int resultCount, IntPtr memoryPts, [In] [Out] byte[] types, int startIndex, int endIndex);
            private static FunctionWrapper<GdipPathIterCopyData_delegate> GdipPathIterCopyData_ptr;
            internal static int GdipPathIterCopyData(HandleRef pathIter, out int resultCount, IntPtr memoryPts, [In] [Out] byte[] types, int startIndex, int endIndex) => GdipPathIterCopyData_ptr.Delegate(pathIter, out resultCount, memoryPts, types, startIndex, endIndex);

            private delegate int GdipCreateHatchBrush_delegate(int hatchstyle, int forecol, int backcol, out IntPtr brush);
            private static FunctionWrapper<GdipCreateHatchBrush_delegate> GdipCreateHatchBrush_ptr;
            internal static int GdipCreateHatchBrush(int hatchstyle, int forecol, int backcol, out IntPtr brush) => GdipCreateHatchBrush_ptr.Delegate(hatchstyle, forecol, backcol, out brush);

            private delegate int GdipGetHatchStyle_delegate(HandleRef brush, out int hatchstyle);
            private static FunctionWrapper<GdipGetHatchStyle_delegate> GdipGetHatchStyle_ptr;
            internal static int GdipGetHatchStyle(HandleRef brush, out int hatchstyle) => GdipGetHatchStyle_ptr.Delegate(brush, out hatchstyle);

            private delegate int GdipGetHatchForegroundColor_delegate(HandleRef brush, out int forecol);
            private static FunctionWrapper<GdipGetHatchForegroundColor_delegate> GdipGetHatchForegroundColor_ptr;
            internal static int GdipGetHatchForegroundColor(HandleRef brush, out int forecol) => GdipGetHatchForegroundColor_ptr.Delegate(brush, out forecol);

            private delegate int GdipGetHatchBackgroundColor_delegate(HandleRef brush, out int backcol);
            private static FunctionWrapper<GdipGetHatchBackgroundColor_delegate> GdipGetHatchBackgroundColor_ptr;
            internal static int GdipGetHatchBackgroundColor(HandleRef brush, out int backcol) => GdipGetHatchBackgroundColor_ptr.Delegate(brush, out backcol);

            private delegate int GdipCloneBrush_delegate(HandleRef brush, out IntPtr clonebrush);
            private static FunctionWrapper<GdipCloneBrush_delegate> GdipCloneBrush_ptr;
            internal static int GdipCloneBrush(HandleRef brush, out IntPtr clonebrush) => GdipCloneBrush_ptr.Delegate(brush, out clonebrush);

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

            private delegate int GdipCreateImageAttributes_delegate(out IntPtr imageattr);
            private static FunctionWrapper<GdipCreateImageAttributes_delegate> GdipCreateImageAttributes_ptr;
            internal static int GdipCreateImageAttributes(out IntPtr imageattr) => GdipCreateImageAttributes_ptr.Delegate(out imageattr);

            private delegate int GdipCloneImageAttributes_delegate(HandleRef imageattr, out IntPtr cloneImageattr);
            private static FunctionWrapper<GdipCloneImageAttributes_delegate> GdipCloneImageAttributes_ptr;
            internal static int GdipCloneImageAttributes(HandleRef imageattr, out IntPtr cloneImageattr) => GdipCloneImageAttributes_ptr.Delegate(imageattr, out cloneImageattr);

            private delegate int GdipDisposeImageAttributes_delegate(HandleRef imageattr);
            private static FunctionWrapper<GdipDisposeImageAttributes_delegate> GdipDisposeImageAttributes_ptr;
            internal static int IntGdipDisposeImageAttributes(HandleRef imageattr) => GdipDisposeImageAttributes_ptr.Delegate(imageattr);

            private delegate int GdipSetImageAttributesColorMatrix_delegate(HandleRef imageattr, ColorAdjustType type, bool enableFlag, ColorMatrix colorMatrix, ColorMatrix grayMatrix, ColorMatrixFlag flags);
            private static FunctionWrapper<GdipSetImageAttributesColorMatrix_delegate> GdipSetImageAttributesColorMatrix_ptr;
            internal static int GdipSetImageAttributesColorMatrix(HandleRef imageattr, ColorAdjustType type, bool enableFlag, ColorMatrix colorMatrix, ColorMatrix grayMatrix, ColorMatrixFlag flags) => GdipSetImageAttributesColorMatrix_ptr.Delegate(imageattr, type, enableFlag, colorMatrix, grayMatrix, flags);

            private delegate int GdipSetImageAttributesThreshold_delegate(HandleRef imageattr, ColorAdjustType type, bool enableFlag, float threshold);
            private static FunctionWrapper<GdipSetImageAttributesThreshold_delegate> GdipSetImageAttributesThreshold_ptr;
            internal static int GdipSetImageAttributesThreshold(HandleRef imageattr, ColorAdjustType type, bool enableFlag, float threshold) => GdipSetImageAttributesThreshold_ptr.Delegate(imageattr, type, enableFlag, threshold);

            private delegate int GdipSetImageAttributesGamma_delegate(HandleRef imageattr, ColorAdjustType type, bool enableFlag, float gamma);
            private static FunctionWrapper<GdipSetImageAttributesGamma_delegate> GdipSetImageAttributesGamma_ptr;
            internal static int GdipSetImageAttributesGamma(HandleRef imageattr, ColorAdjustType type, bool enableFlag, float gamma) => GdipSetImageAttributesGamma_ptr.Delegate(imageattr, type, enableFlag, gamma);

            private delegate int GdipSetImageAttributesNoOp_delegate(HandleRef imageattr, ColorAdjustType type, bool enableFlag);
            private static FunctionWrapper<GdipSetImageAttributesNoOp_delegate> GdipSetImageAttributesNoOp_ptr;
            internal static int GdipSetImageAttributesNoOp(HandleRef imageattr, ColorAdjustType type, bool enableFlag) => GdipSetImageAttributesNoOp_ptr.Delegate(imageattr, type, enableFlag);

            private delegate int GdipSetImageAttributesColorKeys_delegate(HandleRef imageattr, ColorAdjustType type, bool enableFlag, int colorLow, int colorHigh);
            private static FunctionWrapper<GdipSetImageAttributesColorKeys_delegate> GdipSetImageAttributesColorKeys_ptr;
            internal static int GdipSetImageAttributesColorKeys(HandleRef imageattr, ColorAdjustType type, bool enableFlag, int colorLow, int colorHigh) => GdipSetImageAttributesColorKeys_ptr.Delegate(imageattr, type, enableFlag, colorLow, colorHigh);

            private delegate int GdipSetImageAttributesOutputChannel_delegate(HandleRef imageattr, ColorAdjustType type, bool enableFlag, ColorChannelFlag flags);
            private static FunctionWrapper<GdipSetImageAttributesOutputChannel_delegate> GdipSetImageAttributesOutputChannel_ptr;
            internal static int GdipSetImageAttributesOutputChannel(HandleRef imageattr, ColorAdjustType type, bool enableFlag, ColorChannelFlag flags) => GdipSetImageAttributesOutputChannel_ptr.Delegate(imageattr, type, enableFlag, flags);

            private delegate int GdipSetImageAttributesOutputChannelColorProfile_delegate(HandleRef imageattr, ColorAdjustType type, bool enableFlag, [MarshalAs(UnmanagedType.LPWStr)]string colorProfileFilename);
            private static FunctionWrapper<GdipSetImageAttributesOutputChannelColorProfile_delegate> GdipSetImageAttributesOutputChannelColorProfile_ptr;
            internal static int GdipSetImageAttributesOutputChannelColorProfile(HandleRef imageattr, ColorAdjustType type, bool enableFlag, string colorProfileFilename) => GdipSetImageAttributesOutputChannelColorProfile_ptr.Delegate(imageattr, type, enableFlag, colorProfileFilename);

            private delegate int GdipSetImageAttributesRemapTable_delegate(HandleRef imageattr, ColorAdjustType type, bool enableFlag, int mapSize, HandleRef map);
            private static FunctionWrapper<GdipSetImageAttributesRemapTable_delegate> GdipSetImageAttributesRemapTable_ptr;
            internal static int GdipSetImageAttributesRemapTable(HandleRef imageattr, ColorAdjustType type, bool enableFlag, int mapSize, HandleRef map) => GdipSetImageAttributesRemapTable_ptr.Delegate(imageattr, type, enableFlag, mapSize, map);

            private delegate int GdipSetImageAttributesWrapMode_delegate(HandleRef imageattr, int wrapmode, int argb, bool clamp);
            private static FunctionWrapper<GdipSetImageAttributesWrapMode_delegate> GdipSetImageAttributesWrapMode_ptr;
            internal static int GdipSetImageAttributesWrapMode(HandleRef imageattr, int wrapmode, int argb, bool clamp) => GdipSetImageAttributesWrapMode_ptr.Delegate(imageattr, wrapmode, argb, clamp);

            private delegate int GdipGetImageAttributesAdjustedPalette_delegate(HandleRef imageattr, HandleRef palette, ColorAdjustType type);
            private static FunctionWrapper<GdipGetImageAttributesAdjustedPalette_delegate> GdipGetImageAttributesAdjustedPalette_ptr;
            internal static int GdipGetImageAttributesAdjustedPalette(HandleRef imageattr, HandleRef palette, ColorAdjustType type) => GdipGetImageAttributesAdjustedPalette_ptr.Delegate(imageattr, palette, type);

            private delegate int GdipGetImageDecodersSize_delegate(out int numDecoders, out int size);
            private static FunctionWrapper<GdipGetImageDecodersSize_delegate> GdipGetImageDecodersSize_ptr;
            internal static int GdipGetImageDecodersSize(out int numDecoders, out int size) => GdipGetImageDecodersSize_ptr.Delegate(out numDecoders, out size);

            private delegate int GdipGetImageDecoders_delegate(int numDecoders, int size, IntPtr decoders);
            private static FunctionWrapper<GdipGetImageDecoders_delegate> GdipGetImageDecoders_ptr;
            internal static int GdipGetImageDecoders(int numDecoders, int size, IntPtr decoders) => GdipGetImageDecoders_ptr.Delegate(numDecoders, size, decoders);

            private delegate int GdipGetImageEncodersSize_delegate(out int numEncoders, out int size);
            private static FunctionWrapper<GdipGetImageEncodersSize_delegate> GdipGetImageEncodersSize_ptr;
            internal static int GdipGetImageEncodersSize(out int numEncoders, out int size) => GdipGetImageEncodersSize_ptr.Delegate(out numEncoders, out size);

            private delegate int GdipGetImageEncoders_delegate(int numEncoders, int size, IntPtr encoders);
            private static FunctionWrapper<GdipGetImageEncoders_delegate> GdipGetImageEncoders_ptr;
            internal static int GdipGetImageEncoders(int numEncoders, int size, IntPtr encoders) => GdipGetImageEncoders_ptr.Delegate(numEncoders, size, encoders);

            private delegate int GdipCreateSolidFill_delegate(int color, out IntPtr brush);
            private static FunctionWrapper<GdipCreateSolidFill_delegate> GdipCreateSolidFill_ptr;
            internal static int GdipCreateSolidFill(int color, out IntPtr brush) => GdipCreateSolidFill_ptr.Delegate(color, out brush);

            private delegate int GdipSetSolidFillColor_delegate(HandleRef brush, int color);
            private static FunctionWrapper<GdipSetSolidFillColor_delegate> GdipSetSolidFillColor_ptr;
            internal static int GdipSetSolidFillColor(HandleRef brush, int color) => GdipSetSolidFillColor_ptr.Delegate(brush, color);

            private delegate int GdipGetSolidFillColor_delegate(HandleRef brush, out int color);
            private static FunctionWrapper<GdipGetSolidFillColor_delegate> GdipGetSolidFillColor_ptr;
            internal static int GdipGetSolidFillColor(HandleRef brush, out int color) => GdipGetSolidFillColor_ptr.Delegate(brush, out color);


            private delegate int GdipCreateTexture_delegate(HandleRef bitmap, int wrapmode, out IntPtr texture);
            private static FunctionWrapper<GdipCreateTexture_delegate> GdipCreateTexture_ptr;
            internal static int GdipCreateTexture(HandleRef bitmap, int wrapmode, out IntPtr texture) => GdipCreateTexture_ptr.Delegate(bitmap, wrapmode, out texture);

            private delegate int GdipCreateTexture2_delegate(HandleRef bitmap, int wrapmode, float x, float y, float width, float height, out IntPtr texture);
            private static FunctionWrapper<GdipCreateTexture2_delegate> GdipCreateTexture2_ptr;
            internal static int GdipCreateTexture2(HandleRef bitmap, int wrapmode, float x, float y, float width, float height, out IntPtr texture) => GdipCreateTexture2_ptr.Delegate(bitmap, wrapmode, x, y, width, height, out texture);

            private delegate int GdipCreateTextureIA_delegate(HandleRef bitmap, HandleRef imageAttrib, float x, float y, float width, float height, out IntPtr texture);
            private static FunctionWrapper<GdipCreateTextureIA_delegate> GdipCreateTextureIA_ptr;
            internal static int GdipCreateTextureIA(HandleRef bitmap, HandleRef imageAttrib, float x, float y, float width, float height, out IntPtr texture) => GdipCreateTextureIA_ptr.Delegate(bitmap, imageAttrib, x, y, width, height, out texture);

            private delegate int GdipCreateTexture2I_delegate(HandleRef bitmap, int wrapmode, int x, int y, int width, int height, out IntPtr texture);
            private static FunctionWrapper<GdipCreateTexture2I_delegate> GdipCreateTexture2I_ptr;
            internal static int GdipCreateTexture2I(HandleRef bitmap, int wrapmode, int x, int y, int width, int height, out IntPtr texture) => GdipCreateTexture2I_ptr.Delegate(bitmap, wrapmode, x, y, width, height, out texture);

            private delegate int GdipCreateTextureIAI_delegate(HandleRef bitmap, HandleRef imageAttrib, int x, int y, int width, int height, out IntPtr texture);
            private static FunctionWrapper<GdipCreateTextureIAI_delegate> GdipCreateTextureIAI_ptr;
            internal static int GdipCreateTextureIAI(HandleRef bitmap, HandleRef imageAttrib, int x, int y, int width, int height, out IntPtr texture) => GdipCreateTextureIAI_ptr.Delegate(bitmap, imageAttrib, x, y, width, height, out texture);

            private delegate int GdipSetTextureTransform_delegate(HandleRef brush, HandleRef matrix);
            private static FunctionWrapper<GdipSetTextureTransform_delegate> GdipSetTextureTransform_ptr;
            internal static int GdipSetTextureTransform(HandleRef brush, HandleRef matrix) => GdipSetTextureTransform_ptr.Delegate(brush, matrix);

            private delegate int GdipGetTextureTransform_delegate(HandleRef brush, HandleRef matrix);
            private static FunctionWrapper<GdipGetTextureTransform_delegate> GdipGetTextureTransform_ptr;
            internal static int GdipGetTextureTransform(HandleRef brush, HandleRef matrix) => GdipGetTextureTransform_ptr.Delegate(brush, matrix);

            private delegate int GdipResetTextureTransform_delegate(HandleRef brush);
            private static FunctionWrapper<GdipResetTextureTransform_delegate> GdipResetTextureTransform_ptr;
            internal static int GdipResetTextureTransform(HandleRef brush) => GdipResetTextureTransform_ptr.Delegate(brush);

            private delegate int GdipMultiplyTextureTransform_delegate(HandleRef brush, HandleRef matrix, MatrixOrder order);
            private static FunctionWrapper<GdipMultiplyTextureTransform_delegate> GdipMultiplyTextureTransform_ptr;
            internal static int GdipMultiplyTextureTransform(HandleRef brush, HandleRef matrix, MatrixOrder order) => GdipMultiplyTextureTransform_ptr.Delegate(brush, matrix, order);

            private delegate int GdipTranslateTextureTransform_delegate(HandleRef brush, float dx, float dy, MatrixOrder order);
            private static FunctionWrapper<GdipTranslateTextureTransform_delegate> GdipTranslateTextureTransform_ptr;
            internal static int GdipTranslateTextureTransform(HandleRef brush, float dx, float dy, MatrixOrder order) => GdipTranslateTextureTransform_ptr.Delegate(brush, dx, dy, order);

            private delegate int GdipScaleTextureTransform_delegate(HandleRef brush, float sx, float sy, MatrixOrder order);
            private static FunctionWrapper<GdipScaleTextureTransform_delegate> GdipScaleTextureTransform_ptr;
            internal static int GdipScaleTextureTransform(HandleRef brush, float sx, float sy, MatrixOrder order) => GdipScaleTextureTransform_ptr.Delegate(brush, sx, sy, order);

            private delegate int GdipRotateTextureTransform_delegate(HandleRef brush, float angle, MatrixOrder order);
            private static FunctionWrapper<GdipRotateTextureTransform_delegate> GdipRotateTextureTransform_ptr;
            internal static int GdipRotateTextureTransform(HandleRef brush, float angle, MatrixOrder order) => GdipRotateTextureTransform_ptr.Delegate(brush, angle, order);

            private delegate int GdipSetTextureWrapMode_delegate(HandleRef brush, int wrapMode);
            private static FunctionWrapper<GdipSetTextureWrapMode_delegate> GdipSetTextureWrapMode_ptr;
            internal static int GdipSetTextureWrapMode(HandleRef brush, int wrapMode) => GdipSetTextureWrapMode_ptr.Delegate(brush, wrapMode);

            private delegate int GdipGetTextureWrapMode_delegate(HandleRef brush, out int wrapMode);
            private static FunctionWrapper<GdipGetTextureWrapMode_delegate> GdipGetTextureWrapMode_ptr;
            internal static int GdipGetTextureWrapMode(HandleRef brush, out int wrapMode) => GdipGetTextureWrapMode_ptr.Delegate(brush, out wrapMode);

            private delegate int GdipGetTextureImage_delegate(HandleRef brush, out IntPtr image);
            private static FunctionWrapper<GdipGetTextureImage_delegate> GdipGetTextureImage_ptr;
            internal static int GdipGetTextureImage(HandleRef brush, out IntPtr image) => GdipGetTextureImage_ptr.Delegate(brush, out image);

            private delegate int GdipGetFontCollectionFamilyCount_delegate(HandleRef fontCollection, out int numFound);
            private static FunctionWrapper<GdipGetFontCollectionFamilyCount_delegate> GdipGetFontCollectionFamilyCount_ptr;
            internal static int GdipGetFontCollectionFamilyCount(HandleRef fontCollection, out int numFound) => GdipGetFontCollectionFamilyCount_ptr.Delegate(fontCollection, out numFound);

            private delegate int GdipGetFontCollectionFamilyList_delegate(HandleRef fontCollection, int numSought, IntPtr[] gpfamilies, out int numFound);
            private static FunctionWrapper<GdipGetFontCollectionFamilyList_delegate> GdipGetFontCollectionFamilyList_ptr;
            internal static int GdipGetFontCollectionFamilyList(HandleRef fontCollection, int numSought, IntPtr[] gpfamilies, out int numFound) => GdipGetFontCollectionFamilyList_ptr.Delegate(fontCollection, numSought, gpfamilies, out numFound);

            private delegate int GdipCloneFontFamily_delegate(HandleRef fontfamily, out IntPtr clonefontfamily);
            private static FunctionWrapper<GdipCloneFontFamily_delegate> GdipCloneFontFamily_ptr;
            internal static int GdipCloneFontFamily(HandleRef fontfamily, out IntPtr clonefontfamily) => GdipCloneFontFamily_ptr.Delegate(fontfamily, out clonefontfamily);

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

            private delegate int GdipNewInstalledFontCollection_delegate(out IntPtr fontCollection);
            private static FunctionWrapper<GdipNewInstalledFontCollection_delegate> GdipNewInstalledFontCollection_ptr;
            internal static int GdipNewInstalledFontCollection(out IntPtr fontCollection) => GdipNewInstalledFontCollection_ptr.Delegate(out fontCollection);

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

            private delegate int GdipFillRegion_delegate(HandleRef graphics, HandleRef brush, HandleRef region);
            private static FunctionWrapper<GdipFillRegion_delegate> GdipFillRegion_ptr;
            internal static int GdipFillRegion(HandleRef graphics, HandleRef brush, HandleRef region) => GdipFillRegion_ptr.Delegate(graphics, brush, region);

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

        [StructLayout(LayoutKind.Sequential)]
        internal struct StartupInput
        {
            public int GdiplusVersion;             // Must be 1

            // public DebugEventProc DebugEventCallback; // Ignored on free builds
            public IntPtr DebugEventCallback;

            public bool SuppressBackgroundThread;     // FALSE unless you're prepared to call 
                                                      // the hook/unhook functions properly

            public bool SuppressExternalCodecs;       // FALSE unless you want GDI+ only to use
                                                      // its internal image codecs.

            public static StartupInput GetDefault()
            {
                StartupInput result = new StartupInput();
                result.GdiplusVersion = 1;
                // result.DebugEventCallback = null;
                result.SuppressBackgroundThread = false;
                result.SuppressExternalCodecs = false;
                return result;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StartupOutput
        {
            // The following 2 fields won't be used.  They were originally intended 
            // for getting GDI+ to run on our thread - however there are marshalling
            // dealing with function *'s and what not - so we make explicit calls
            // to gdi+ after the fact, via the GdiplusNotificationHook and 
            // GdiplusNotificationUnhook methods.
            public IntPtr hook;//not used
            public IntPtr unhook;//not used.
        }
    }
}
