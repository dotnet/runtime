// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    // Raw function imports for gdiplus
    // Functions are loaded manually in order to accomodate different shared library names on Unix.
    internal partial class SafeNativeMethods
    {
        internal partial class Gdip
        {
            private static IntPtr s_gdipModule;

            private static FunctionWrapper<T> LoadFunction<T>(string name) where T : class
            {
                if (s_gdipModule == IntPtr.Zero)
                {
                    throw new DllNotFoundException();
                }

                Lazy<T> lazyDelegate = new Lazy<T>(() =>
                {
                    IntPtr funcPtr = LoadFunctionPointer(s_gdipModule, name);
                    if (funcPtr == IntPtr.Zero)
                    {
                        return null;
                    }
                    else
                    {
                        return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
                    }
                });
                
                return new FunctionWrapper<T>(lazyDelegate);
            }

            /// <summary>
            /// Utility class which lazily loads function pointers and throws an EntryPointNotFoundException at
            /// usage-time if the function pointer can not be loaded.
            /// </summary>
            /// <typeparam name="T">The type of the function to wrap.</typeparam>
            private class FunctionWrapper<T> where T : class
            {
                private Lazy<T> _lazyDelegate;

                public FunctionWrapper(Lazy<T> lazyDelegate)
                {
                    _lazyDelegate = lazyDelegate;
                }

                public T Delegate => _lazyDelegate.Value ?? throw new EntryPointNotFoundException();
            }

            private static void LoadSharedFunctionPointers()
            {
                GdipBeginContainer_ptr = LoadFunction<GdipBeginContainer_delegate>("GdipBeginContainer");
                GdipBeginContainer2_ptr = LoadFunction<GdipBeginContainer2_delegate>("GdipBeginContainer2");
                GdipBeginContainerI_ptr = LoadFunction<GdipBeginContainerI_delegate>("GdipBeginContainerI");
                GdipEndContainer_ptr = LoadFunction<GdipEndContainer_delegate>("GdipEndContainer");
                GdipCreateAdjustableArrowCap_ptr = LoadFunction<GdipCreateAdjustableArrowCap_delegate>("GdipCreateAdjustableArrowCap");
                GdipGetAdjustableArrowCapHeight_ptr = LoadFunction<GdipGetAdjustableArrowCapHeight_delegate>("GdipGetAdjustableArrowCapHeight");
                GdipSetAdjustableArrowCapHeight_ptr = LoadFunction<GdipSetAdjustableArrowCapHeight_delegate>("GdipSetAdjustableArrowCapHeight");
                GdipSetAdjustableArrowCapWidth_ptr = LoadFunction<GdipSetAdjustableArrowCapWidth_delegate>("GdipSetAdjustableArrowCapWidth");
                GdipGetAdjustableArrowCapWidth_ptr = LoadFunction<GdipGetAdjustableArrowCapWidth_delegate>("GdipGetAdjustableArrowCapWidth");
                GdipSetAdjustableArrowCapMiddleInset_ptr = LoadFunction<GdipSetAdjustableArrowCapMiddleInset_delegate>("GdipSetAdjustableArrowCapMiddleInset");
                GdipGetAdjustableArrowCapMiddleInset_ptr = LoadFunction<GdipGetAdjustableArrowCapMiddleInset_delegate>("GdipGetAdjustableArrowCapMiddleInset");
                GdipSetAdjustableArrowCapFillState_ptr = LoadFunction<GdipSetAdjustableArrowCapFillState_delegate>("GdipSetAdjustableArrowCapFillState");
                GdipGetAdjustableArrowCapFillState_ptr = LoadFunction<GdipGetAdjustableArrowCapFillState_delegate>("GdipGetAdjustableArrowCapFillState");
                GdipGetCustomLineCapType_ptr = LoadFunction<GdipGetCustomLineCapType_delegate>("GdipGetCustomLineCapType");
                GdipCreateCustomLineCap_ptr = LoadFunction<GdipCreateCustomLineCap_delegate>("GdipCreateCustomLineCap");
                GdipDeleteCustomLineCap_ptr = LoadFunction<GdipDeleteCustomLineCap_delegate>("GdipDeleteCustomLineCap");
                GdipCloneCustomLineCap_ptr = LoadFunction<GdipCloneCustomLineCap_delegate>("GdipCloneCustomLineCap");
                GdipSetCustomLineCapStrokeCaps_ptr = LoadFunction<GdipSetCustomLineCapStrokeCaps_delegate>("GdipSetCustomLineCapStrokeCaps");
                GdipGetCustomLineCapStrokeCaps_ptr = LoadFunction<GdipGetCustomLineCapStrokeCaps_delegate>("GdipGetCustomLineCapStrokeCaps");
                GdipSetCustomLineCapStrokeJoin_ptr = LoadFunction<GdipSetCustomLineCapStrokeJoin_delegate>("GdipSetCustomLineCapStrokeJoin");
                GdipGetCustomLineCapStrokeJoin_ptr = LoadFunction<GdipGetCustomLineCapStrokeJoin_delegate>("GdipGetCustomLineCapStrokeJoin");
                GdipSetCustomLineCapBaseCap_ptr = LoadFunction<GdipSetCustomLineCapBaseCap_delegate>("GdipSetCustomLineCapBaseCap");
                GdipGetCustomLineCapBaseCap_ptr = LoadFunction<GdipGetCustomLineCapBaseCap_delegate>("GdipGetCustomLineCapBaseCap");
                GdipSetCustomLineCapBaseInset_ptr = LoadFunction<GdipSetCustomLineCapBaseInset_delegate>("GdipSetCustomLineCapBaseInset");
                GdipGetCustomLineCapBaseInset_ptr = LoadFunction<GdipGetCustomLineCapBaseInset_delegate>("GdipGetCustomLineCapBaseInset");
                GdipSetCustomLineCapWidthScale_ptr = LoadFunction<GdipSetCustomLineCapWidthScale_delegate>("GdipSetCustomLineCapWidthScale");
                GdipGetCustomLineCapWidthScale_ptr = LoadFunction<GdipGetCustomLineCapWidthScale_delegate>("GdipGetCustomLineCapWidthScale");
                GdipCreatePathIter_ptr = LoadFunction<GdipCreatePathIter_delegate>("GdipCreatePathIter");
                GdipDeletePathIter_ptr = LoadFunction<GdipDeletePathIter_delegate>("GdipDeletePathIter");
                GdipPathIterNextSubpath_ptr = LoadFunction<GdipPathIterNextSubpath_delegate>("GdipPathIterNextSubpath");
                GdipPathIterNextSubpathPath_ptr = LoadFunction<GdipPathIterNextSubpathPath_delegate>("GdipPathIterNextSubpathPath");
                GdipPathIterNextPathType_ptr = LoadFunction<GdipPathIterNextPathType_delegate>("GdipPathIterNextPathType");
                GdipPathIterNextMarker_ptr = LoadFunction<GdipPathIterNextMarker_delegate>("GdipPathIterNextMarker");
                GdipPathIterNextMarkerPath_ptr = LoadFunction<GdipPathIterNextMarkerPath_delegate>("GdipPathIterNextMarkerPath");
                GdipPathIterGetCount_ptr = LoadFunction<GdipPathIterGetCount_delegate>("GdipPathIterGetCount");
                GdipPathIterGetSubpathCount_ptr = LoadFunction<GdipPathIterGetSubpathCount_delegate>("GdipPathIterGetSubpathCount");
                GdipPathIterHasCurve_ptr = LoadFunction<GdipPathIterHasCurve_delegate>("GdipPathIterHasCurve");
                GdipPathIterRewind_ptr = LoadFunction<GdipPathIterRewind_delegate>("GdipPathIterRewind");
                GdipPathIterEnumerate_ptr = LoadFunction<GdipPathIterEnumerate_delegate>("GdipPathIterEnumerate");
                GdipPathIterCopyData_ptr = LoadFunction<GdipPathIterCopyData_delegate>("GdipPathIterCopyData");
                GdipCreateHatchBrush_ptr = LoadFunction<GdipCreateHatchBrush_delegate>("GdipCreateHatchBrush");
                GdipGetHatchStyle_ptr = LoadFunction<GdipGetHatchStyle_delegate>("GdipGetHatchStyle");
                GdipGetHatchForegroundColor_ptr = LoadFunction<GdipGetHatchForegroundColor_delegate>("GdipGetHatchForegroundColor");
                GdipGetHatchBackgroundColor_ptr = LoadFunction<GdipGetHatchBackgroundColor_delegate>("GdipGetHatchBackgroundColor");
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
                GdipCloneBrush_ptr = LoadFunction<GdipCloneBrush_delegate>("GdipCloneBrush");
                GdipCreateImageAttributes_ptr = LoadFunction<GdipCreateImageAttributes_delegate>("GdipCreateImageAttributes");
                GdipCloneImageAttributes_ptr = LoadFunction<GdipCloneImageAttributes_delegate>("GdipCloneImageAttributes");
                GdipDisposeImageAttributes_ptr = LoadFunction<GdipDisposeImageAttributes_delegate>("GdipDisposeImageAttributes");
                GdipSetImageAttributesColorMatrix_ptr = LoadFunction<GdipSetImageAttributesColorMatrix_delegate>("GdipSetImageAttributesColorMatrix");
                GdipSetImageAttributesThreshold_ptr = LoadFunction<GdipSetImageAttributesThreshold_delegate>("GdipSetImageAttributesThreshold");
                GdipSetImageAttributesGamma_ptr = LoadFunction<GdipSetImageAttributesGamma_delegate>("GdipSetImageAttributesGamma");
                GdipSetImageAttributesNoOp_ptr = LoadFunction<GdipSetImageAttributesNoOp_delegate>("GdipSetImageAttributesNoOp");
                GdipSetImageAttributesColorKeys_ptr = LoadFunction<GdipSetImageAttributesColorKeys_delegate>("GdipSetImageAttributesColorKeys");
                GdipSetImageAttributesOutputChannel_ptr = LoadFunction<GdipSetImageAttributesOutputChannel_delegate>("GdipSetImageAttributesOutputChannel");
                GdipSetImageAttributesOutputChannelColorProfile_ptr = LoadFunction<GdipSetImageAttributesOutputChannelColorProfile_delegate>("GdipSetImageAttributesOutputChannelColorProfile");
                GdipSetImageAttributesRemapTable_ptr = LoadFunction<GdipSetImageAttributesRemapTable_delegate>("GdipSetImageAttributesRemapTable");
                GdipSetImageAttributesWrapMode_ptr = LoadFunction<GdipSetImageAttributesWrapMode_delegate>("GdipSetImageAttributesWrapMode");
                GdipGetImageAttributesAdjustedPalette_ptr = LoadFunction<GdipGetImageAttributesAdjustedPalette_delegate>("GdipGetImageAttributesAdjustedPalette");
                GdipGetImageDecodersSize_ptr = LoadFunction<GdipGetImageDecodersSize_delegate>("GdipGetImageDecodersSize");
                GdipGetImageDecoders_ptr = LoadFunction<GdipGetImageDecoders_delegate>("GdipGetImageDecoders");
                GdipGetImageEncodersSize_ptr = LoadFunction<GdipGetImageEncodersSize_delegate>("GdipGetImageEncodersSize");
                GdipGetImageEncoders_ptr = LoadFunction<GdipGetImageEncoders_delegate>("GdipGetImageEncoders");
                GdipCreateSolidFill_ptr = LoadFunction<GdipCreateSolidFill_delegate>("GdipCreateSolidFill");
                GdipSetSolidFillColor_ptr = LoadFunction<GdipSetSolidFillColor_delegate>("GdipSetSolidFillColor");
                GdipGetSolidFillColor_ptr = LoadFunction<GdipGetSolidFillColor_delegate>("GdipGetSolidFillColor");
                GdipCreateTexture_ptr = LoadFunction<GdipCreateTexture_delegate>("GdipCreateTexture");
                GdipCreateTexture2_ptr = LoadFunction<GdipCreateTexture2_delegate>("GdipCreateTexture2");
                GdipCreateTextureIA_ptr = LoadFunction<GdipCreateTextureIA_delegate>("GdipCreateTextureIA");
                GdipCreateTexture2I_ptr = LoadFunction<GdipCreateTexture2I_delegate>("GdipCreateTexture2I");
                GdipCreateTextureIAI_ptr = LoadFunction<GdipCreateTextureIAI_delegate>("GdipCreateTextureIAI");
                GdipSetTextureTransform_ptr = LoadFunction<GdipSetTextureTransform_delegate>("GdipSetTextureTransform");
                GdipGetTextureTransform_ptr = LoadFunction<GdipGetTextureTransform_delegate>("GdipGetTextureTransform");
                GdipResetTextureTransform_ptr = LoadFunction<GdipResetTextureTransform_delegate>("GdipResetTextureTransform");
                GdipMultiplyTextureTransform_ptr = LoadFunction<GdipMultiplyTextureTransform_delegate>("GdipMultiplyTextureTransform");
                GdipTranslateTextureTransform_ptr = LoadFunction<GdipTranslateTextureTransform_delegate>("GdipTranslateTextureTransform");
                GdipScaleTextureTransform_ptr = LoadFunction<GdipScaleTextureTransform_delegate>("GdipScaleTextureTransform");
                GdipRotateTextureTransform_ptr = LoadFunction<GdipRotateTextureTransform_delegate>("GdipRotateTextureTransform");
                GdipSetTextureWrapMode_ptr = LoadFunction<GdipSetTextureWrapMode_delegate>("GdipSetTextureWrapMode");
                GdipGetTextureWrapMode_ptr = LoadFunction<GdipGetTextureWrapMode_delegate>("GdipGetTextureWrapMode");
                GdipGetTextureImage_ptr = LoadFunction<GdipGetTextureImage_delegate>("GdipGetTextureImage");
                GdipGetFontCollectionFamilyCount_ptr = LoadFunction<GdipGetFontCollectionFamilyCount_delegate>("GdipGetFontCollectionFamilyCount");
                GdipGetFontCollectionFamilyList_ptr = LoadFunction<GdipGetFontCollectionFamilyList_delegate>("GdipGetFontCollectionFamilyList");
                GdipCloneFontFamily_ptr = LoadFunction<GdipCloneFontFamily_delegate>("GdipCloneFontFamily");
                GdipNewInstalledFontCollection_ptr = LoadFunction<GdipNewInstalledFontCollection_delegate>("GdipNewInstalledFontCollection");
                GdipNewPrivateFontCollection_ptr = LoadFunction<GdipNewPrivateFontCollection_delegate>("GdipNewPrivateFontCollection");
                GdipDeletePrivateFontCollection_ptr = LoadFunction<GdipDeletePrivateFontCollection_delegate>("GdipDeletePrivateFontCollection");
                GdipPrivateAddFontFile_ptr = LoadFunction<GdipPrivateAddFontFile_delegate>("GdipPrivateAddFontFile");
                GdipPrivateAddMemoryFont_ptr = LoadFunction<GdipPrivateAddMemoryFont_delegate>("GdipPrivateAddMemoryFont");
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
                GdipSetWorldTransform_ptr = LoadFunction<GdipSetWorldTransform_delegate>("GdipSetWorldTransform");
                GdipResetWorldTransform_ptr = LoadFunction<GdipResetWorldTransform_delegate>("GdipResetWorldTransform");
                GdipMultiplyWorldTransform_ptr = LoadFunction<GdipMultiplyWorldTransform_delegate>("GdipMultiplyWorldTransform");
                GdipTranslateWorldTransform_ptr = LoadFunction<GdipTranslateWorldTransform_delegate>("GdipTranslateWorldTransform");
                GdipScaleWorldTransform_ptr = LoadFunction<GdipScaleWorldTransform_delegate>("GdipScaleWorldTransform");
                GdipRotateWorldTransform_ptr = LoadFunction<GdipRotateWorldTransform_delegate>("GdipRotateWorldTransform");
                GdipGetWorldTransform_ptr = LoadFunction<GdipGetWorldTransform_delegate>("GdipGetWorldTransform");
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
                GdipFillRegion_ptr = LoadFunction<GdipFillRegion_delegate>("GdipFillRegion");
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
