// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
            }

            // Shared function imports (all platforms)

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