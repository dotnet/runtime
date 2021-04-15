// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    // Raw function imports for gdiplus
    // Functions are loaded manually in order to accomodate different shared library names on Unix.
    internal static partial class SafeNativeMethods
    {
        internal static unsafe partial class Gdip
        {
            // Shared function imports (all platforms)
            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipBeginContainer(HandleRef graphics, ref RectangleF dstRect, ref RectangleF srcRect, GraphicsUnit unit, out int state);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipBeginContainer2(HandleRef graphics, out int state);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipBeginContainerI(HandleRef graphics, ref Rectangle dstRect, ref Rectangle srcRect, GraphicsUnit unit, out int state);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipEndContainer(HandleRef graphics, int state);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateAdjustableArrowCap(float height, float width, bool isFilled, out IntPtr adjustableArrowCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetAdjustableArrowCapHeight(HandleRef adjustableArrowCap, out float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetAdjustableArrowCapHeight(HandleRef adjustableArrowCap, float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetAdjustableArrowCapWidth(HandleRef adjustableArrowCap, float width);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetAdjustableArrowCapWidth(HandleRef adjustableArrowCap, out float width);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetAdjustableArrowCapMiddleInset(HandleRef adjustableArrowCap, float middleInset);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetAdjustableArrowCapMiddleInset(HandleRef adjustableArrowCap, out float middleInset);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetAdjustableArrowCapFillState(HandleRef adjustableArrowCap, bool fillState);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetAdjustableArrowCapFillState(HandleRef adjustableArrowCap, out bool fillState);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetCustomLineCapType(IntPtr customCap, out CustomLineCapType capType);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateCustomLineCap(HandleRef fillpath, HandleRef strokepath, LineCap baseCap, float baseInset, out IntPtr customCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeleteCustomLineCap(IntPtr customCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeleteCustomLineCap(HandleRef customCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCloneCustomLineCap(HandleRef customCap, out IntPtr clonedCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetCustomLineCapStrokeCaps(HandleRef customCap, LineCap startCap, LineCap endCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetCustomLineCapStrokeCaps(HandleRef customCap, out LineCap startCap, out LineCap endCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetCustomLineCapStrokeJoin(HandleRef customCap, LineJoin lineJoin);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetCustomLineCapStrokeJoin(HandleRef customCap, out LineJoin lineJoin);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetCustomLineCapBaseCap(HandleRef customCap, LineCap baseCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetCustomLineCapBaseCap(HandleRef customCap, out LineCap baseCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetCustomLineCapBaseInset(HandleRef customCap, float inset);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetCustomLineCapBaseInset(HandleRef customCap, out float inset);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetCustomLineCapWidthScale(HandleRef customCap, float widthScale);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetCustomLineCapWidthScale(HandleRef customCap, out float widthScale);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreatePathIter(out IntPtr pathIter, HandleRef path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeletePathIter(HandleRef pathIter);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipPathIterNextSubpath(HandleRef pathIter, out int resultCount, out int startIndex, out int endIndex, out bool isClosed);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipPathIterNextSubpathPath(HandleRef pathIter, out int resultCount, HandleRef path, out bool isClosed);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipPathIterNextPathType(HandleRef pathIter, out int resultCount, out byte pathType, out int startIndex, out int endIndex);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipPathIterNextMarker(HandleRef pathIter, out int resultCount, out int startIndex, out int endIndex);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipPathIterNextMarkerPath(HandleRef pathIter, out int resultCount, HandleRef path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipPathIterGetCount(HandleRef pathIter, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipPathIterGetSubpathCount(HandleRef pathIter, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipPathIterHasCurve(HandleRef pathIter, out bool hasCurve);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipPathIterRewind(HandleRef pathIter);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipPathIterEnumerate(HandleRef pathIter, out int resultCount, PointF* points, byte* types, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipPathIterCopyData(HandleRef pathIter, out int resultCount, PointF* points, byte* types, int startIndex, int endIndex);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateHatchBrush(int hatchstyle, int forecol, int backcol, out IntPtr brush);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetHatchStyle(HandleRef brush, out int hatchstyle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetHatchForegroundColor(HandleRef brush, out int forecol);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetHatchBackgroundColor(HandleRef brush, out int backcol);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCloneBrush(HandleRef brush, out IntPtr clonebrush);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateLineBrush(ref PointF point1, ref PointF point2, int color1, int color2, WrapMode wrapMode, out IntPtr lineGradient);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateLineBrushI(ref Point point1, ref Point point2, int color1, int color2, WrapMode wrapMode, out IntPtr lineGradient);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateLineBrushFromRect(ref RectangleF rect, int color1, int color2, LinearGradientMode lineGradientMode, WrapMode wrapMode, out IntPtr lineGradient);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateLineBrushFromRectI(ref Rectangle rect, int color1, int color2, LinearGradientMode lineGradientMode, WrapMode wrapMode, out IntPtr lineGradient);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateLineBrushFromRectWithAngle(ref RectangleF rect, int color1, int color2, float angle, bool isAngleScaleable, WrapMode wrapMode, out IntPtr lineGradient);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateLineBrushFromRectWithAngleI(ref Rectangle rect, int color1, int color2, float angle, bool isAngleScaleable, WrapMode wrapMode, out IntPtr lineGradient);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetLineColors(HandleRef brush, int color1, int color2);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetLineColors(HandleRef brush, int[] colors);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetLineRect(HandleRef brush, out RectangleF gprectf);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetLineGammaCorrection(HandleRef brush, out bool useGammaCorrection);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetLineGammaCorrection(HandleRef brush, bool useGammaCorrection);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetLineSigmaBlend(HandleRef brush, float focus, float scale);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetLineLinearBlend(HandleRef brush, float focus, float scale);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetLineBlendCount(HandleRef brush, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetLineBlend(HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetLineBlend(HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetLinePresetBlendCount(HandleRef brush, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetLinePresetBlend(HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetLinePresetBlend(HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetLineWrapMode(HandleRef brush, int wrapMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetLineWrapMode(HandleRef brush, out int wrapMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipResetLineTransform(HandleRef brush);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipMultiplyLineTransform(HandleRef brush, HandleRef matrix, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetLineTransform(HandleRef brush, HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetLineTransform(HandleRef brush, HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTranslateLineTransform(HandleRef brush, float dx, float dy, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipScaleLineTransform(HandleRef brush, float sx, float sy, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipRotateLineTransform(HandleRef brush, float angle, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreatePathGradient(PointF* points, int count, WrapMode wrapMode, out IntPtr brush);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreatePathGradientI(Point* points, int count, WrapMode wrapMode, out IntPtr brush);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreatePathGradientFromPath(HandleRef path, out IntPtr brush);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathGradientCenterColor(HandleRef brush, out int color);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPathGradientCenterColor(HandleRef brush, int color);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathGradientSurroundColorsWithCount(HandleRef brush, int[] color, ref int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPathGradientSurroundColorsWithCount(HandleRef brush, int[] argb, ref int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathGradientCenterPoint(HandleRef brush, out PointF point);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPathGradientCenterPoint(HandleRef brush, ref PointF point);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathGradientRect(HandleRef brush, out RectangleF gprectf);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathGradientPointCount(HandleRef brush, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathGradientSurroundColorCount(HandleRef brush, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathGradientBlendCount(HandleRef brush, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathGradientBlend(HandleRef brush, float[] blend, float[] positions, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPathGradientBlend(HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathGradientPresetBlendCount(HandleRef brush, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathGradientPresetBlend(HandleRef brush, int[] blend, float[] positions, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPathGradientPresetBlend(HandleRef brush, int[] blend, float[] positions, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPathGradientSigmaBlend(HandleRef brush, float focus, float scale);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPathGradientLinearBlend(HandleRef brush, float focus, float scale);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPathGradientWrapMode(HandleRef brush, int wrapmode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathGradientWrapMode(HandleRef brush, out int wrapmode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPathGradientTransform(HandleRef brush, HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathGradientTransform(HandleRef brush, HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipResetPathGradientTransform(HandleRef brush);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipMultiplyPathGradientTransform(HandleRef brush, HandleRef matrix, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTranslatePathGradientTransform(HandleRef brush, float dx, float dy, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipScalePathGradientTransform(HandleRef brush, float sx, float sy, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipRotatePathGradientTransform(HandleRef brush, float angle, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPathGradientFocusScales(HandleRef brush, float[] xScale, float[] yScale);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPathGradientFocusScales(HandleRef brush, float xScale, float yScale);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateImageAttributes(out IntPtr imageattr);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCloneImageAttributes(HandleRef imageattr, out IntPtr cloneImageattr);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDisposeImageAttributes(HandleRef imageattr);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetImageAttributesColorMatrix(HandleRef imageattr, ColorAdjustType type, bool enableFlag, ColorMatrix? colorMatrix, ColorMatrix? grayMatrix, ColorMatrixFlag flags);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetImageAttributesThreshold(HandleRef imageattr, ColorAdjustType type, bool enableFlag, float threshold);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetImageAttributesGamma(HandleRef imageattr, ColorAdjustType type, bool enableFlag, float gamma);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetImageAttributesNoOp(HandleRef imageattr, ColorAdjustType type, bool enableFlag);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetImageAttributesColorKeys(HandleRef imageattr, ColorAdjustType type, bool enableFlag, int colorLow, int colorHigh);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetImageAttributesOutputChannel(HandleRef imageattr, ColorAdjustType type, bool enableFlag, ColorChannelFlag flags);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipSetImageAttributesOutputChannelColorProfile(HandleRef imageattr, ColorAdjustType type, bool enableFlag, string colorProfileFilename);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetImageAttributesRemapTable(HandleRef imageattr, ColorAdjustType type, bool enableFlag, int mapSize, IntPtr map);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetImageAttributesWrapMode(HandleRef imageattr, int wrapmode, int argb, bool clamp);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageAttributesAdjustedPalette(HandleRef imageattr, IntPtr palette, ColorAdjustType type);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageDecodersSize(out int numDecoders, out int size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageDecoders(int numDecoders, int size, IntPtr decoders);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageEncodersSize(out int numEncoders, out int size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageEncoders(int numEncoders, int size, IntPtr encoders);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateSolidFill(int color, out IntPtr brush);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetSolidFillColor(HandleRef brush, int color);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetSolidFillColor(HandleRef brush, out int color);


            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateTexture(HandleRef bitmap, int wrapmode, out IntPtr texture);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateTexture2(HandleRef bitmap, int wrapmode, float x, float y, float width, float height, out IntPtr texture);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateTextureIA(HandleRef bitmap, HandleRef imageAttrib, float x, float y, float width, float height, out IntPtr texture);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateTexture2I(HandleRef bitmap, int wrapmode, int x, int y, int width, int height, out IntPtr texture);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateTextureIAI(HandleRef bitmap, HandleRef imageAttrib, int x, int y, int width, int height, out IntPtr texture);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetTextureTransform(HandleRef brush, HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetTextureTransform(HandleRef brush, HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipResetTextureTransform(HandleRef brush);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipMultiplyTextureTransform(HandleRef brush, HandleRef matrix, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTranslateTextureTransform(HandleRef brush, float dx, float dy, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipScaleTextureTransform(HandleRef brush, float sx, float sy, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipRotateTextureTransform(HandleRef brush, float angle, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetTextureWrapMode(HandleRef brush, int wrapMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetTextureWrapMode(HandleRef brush, out int wrapMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetTextureImage(HandleRef brush, out IntPtr image);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetFontCollectionFamilyCount(HandleRef fontCollection, out int numFound);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetFontCollectionFamilyList(HandleRef fontCollection, int numSought, IntPtr[] gpfamilies, out int numFound);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCloneFontFamily(IntPtr fontfamily, out IntPtr clonefontfamily);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipCreateFontFamilyFromName(string name, HandleRef fontCollection, out IntPtr FontFamily);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetGenericFontFamilySansSerif(out IntPtr fontfamily);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetGenericFontFamilySerif(out IntPtr fontfamily);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetGenericFontFamilyMonospace(out IntPtr fontfamily);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeleteFontFamily(HandleRef fontFamily);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipGetFamilyName(HandleRef family, char* name, int language);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsStyleAvailable(HandleRef family, FontStyle style, out int isStyleAvailable);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetEmHeight(HandleRef family, FontStyle style, out int EmHeight);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetCellAscent(HandleRef family, FontStyle style, out int CellAscent);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetCellDescent(HandleRef family, FontStyle style, out int CellDescent);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetLineSpacing(HandleRef family, FontStyle style, out int LineSpaceing);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipNewInstalledFontCollection(out IntPtr fontCollection);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipNewPrivateFontCollection(out IntPtr fontCollection);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeletePrivateFontCollection(ref IntPtr fontCollection);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipPrivateAddFontFile(HandleRef fontCollection, string filename);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipPrivateAddMemoryFont(HandleRef fontCollection, IntPtr memory, int length);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateFont(HandleRef fontFamily, float emSize, FontStyle style, GraphicsUnit unit, out IntPtr font);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateFontFromDC(IntPtr hdc, ref IntPtr font);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCloneFont(HandleRef font, out IntPtr cloneFont);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeleteFont(HandleRef font);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetFamily(HandleRef font, out IntPtr family);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetFontStyle(HandleRef font, out FontStyle style);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetFontSize(HandleRef font, out float size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetFontHeight(HandleRef font, HandleRef graphics, out float size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetFontHeightGivenDPI(HandleRef font, float dpi, out float size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetFontUnit(HandleRef font, out GraphicsUnit unit);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetLogFontW(HandleRef font, HandleRef graphics, ref Interop.User32.LOGFONT lf);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreatePen1(int argb, float width, int unit, out IntPtr pen);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreatePen2(HandleRef brush, float width, int unit, out IntPtr pen);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipClonePen(HandleRef pen, out IntPtr clonepen);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeletePen(HandleRef Pen);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenMode(HandleRef pen, PenAlignment penAlign);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenMode(HandleRef pen, out PenAlignment penAlign);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenWidth(HandleRef pen, float width);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenWidth(HandleRef pen, float[] width);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenLineCap197819(HandleRef pen, int startCap, int endCap, int dashCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenStartCap(HandleRef pen, int startCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenEndCap(HandleRef pen, int endCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenStartCap(HandleRef pen, out int startCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenEndCap(HandleRef pen, out int endCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenDashCap197819(HandleRef pen, out int dashCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenDashCap197819(HandleRef pen, int dashCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenLineJoin(HandleRef pen, int lineJoin);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenLineJoin(HandleRef pen, out int lineJoin);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenCustomStartCap(HandleRef pen, HandleRef customCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenCustomStartCap(HandleRef pen, out IntPtr customCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenCustomEndCap(HandleRef pen, HandleRef customCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenCustomEndCap(HandleRef pen, out IntPtr customCap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenMiterLimit(HandleRef pen, float miterLimit);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenMiterLimit(HandleRef pen, float[] miterLimit);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenTransform(HandleRef pen, HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenTransform(HandleRef pen, HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipResetPenTransform(HandleRef brush);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipMultiplyPenTransform(HandleRef brush, HandleRef matrix, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTranslatePenTransform(HandleRef brush, float dx, float dy, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipScalePenTransform(HandleRef brush, float sx, float sy, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipRotatePenTransform(HandleRef brush, float angle, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenColor(HandleRef pen, int argb);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenColor(HandleRef pen, out int argb);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenBrushFill(HandleRef pen, HandleRef brush);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenBrushFill(HandleRef pen, out IntPtr brush);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenFillType(HandleRef pen, out int pentype);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenDashStyle(HandleRef pen, out int dashstyle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenDashStyle(HandleRef pen, int dashstyle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenDashArray(HandleRef pen, HandleRef memorydash, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenDashOffset(HandleRef pen, float[] dashoffset);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenDashOffset(HandleRef pen, float dashoffset);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenDashCount(HandleRef pen, out int dashcount);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenDashArray(HandleRef pen, float[] memorydash, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenCompoundCount(HandleRef pen, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPenCompoundArray(HandleRef pen, float[] array, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPenCompoundArray(HandleRef pen, float[] array, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetWorldTransform(HandleRef graphics, HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipResetWorldTransform(HandleRef graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipMultiplyWorldTransform(HandleRef graphics, HandleRef matrix, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTranslateWorldTransform(HandleRef graphics, float dx, float dy, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipScaleWorldTransform(HandleRef graphics, float sx, float sy, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipRotateWorldTransform(HandleRef graphics, float angle, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetWorldTransform(HandleRef graphics, HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetCompositingMode(HandleRef graphics, CompositingMode compositingMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetTextRenderingHint(HandleRef graphics, TextRenderingHint textRenderingHint);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetTextContrast(HandleRef graphics, int textContrast);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetInterpolationMode(HandleRef graphics, InterpolationMode interpolationMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetCompositingMode(HandleRef graphics, out CompositingMode compositingMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetRenderingOrigin(HandleRef graphics, int x, int y);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetRenderingOrigin(HandleRef graphics, out int x, out int y);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetCompositingQuality(HandleRef graphics, CompositingQuality quality);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetCompositingQuality(HandleRef graphics, out CompositingQuality quality);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetSmoothingMode(HandleRef graphics, SmoothingMode smoothingMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetSmoothingMode(HandleRef graphics, out SmoothingMode smoothingMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPixelOffsetMode(HandleRef graphics, PixelOffsetMode pixelOffsetMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPixelOffsetMode(HandleRef graphics, out PixelOffsetMode pixelOffsetMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetTextRenderingHint(HandleRef graphics, out TextRenderingHint textRenderingHint);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetTextContrast(HandleRef graphics, out int textContrast);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetInterpolationMode(HandleRef graphics, out InterpolationMode interpolationMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPageUnit(HandleRef graphics, out GraphicsUnit unit);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPageScale(HandleRef graphics, out float scale);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPageUnit(HandleRef graphics, GraphicsUnit unit);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPageScale(HandleRef graphics, float scale);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetDpiX(HandleRef graphics, out float dpi);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetDpiY(HandleRef graphics, out float dpi);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateMatrix(out IntPtr matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateMatrix2(float m11, float m12, float m21, float m22, float dx, float dy, out IntPtr matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateMatrix3(ref RectangleF rect, PointF* dstplg, out IntPtr matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateMatrix3I(ref Rectangle rect, Point* dstplg, out IntPtr matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCloneMatrix(HandleRef matrix, out IntPtr cloneMatrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeleteMatrix(HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetMatrixElements(HandleRef matrix, float m11, float m12, float m21, float m22, float dx, float dy);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipMultiplyMatrix(HandleRef matrix, HandleRef matrix2, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTranslateMatrix(HandleRef matrix, float offsetX, float offsetY, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipScaleMatrix(HandleRef matrix, float scaleX, float scaleY, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipRotateMatrix(HandleRef matrix, float angle, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipShearMatrix(HandleRef matrix, float shearX, float shearY, MatrixOrder order);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipInvertMatrix(HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTransformMatrixPoints(HandleRef matrix, PointF* pts, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTransformMatrixPointsI(HandleRef matrix, Point* pts, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipVectorTransformMatrixPoints(HandleRef matrix, PointF* pts, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipVectorTransformMatrixPointsI(HandleRef matrix, Point* pts, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern unsafe int GdipGetMatrixElements(HandleRef matrix, float* m);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsMatrixInvertible(HandleRef matrix, out int boolean);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsMatrixIdentity(HandleRef matrix, out int boolean);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsMatrixEqual(HandleRef matrix, HandleRef matrix2, out int boolean);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateRegion(out IntPtr region);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateRegionRect(ref RectangleF gprectf, out IntPtr region);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateRegionRectI(ref Rectangle gprect, out IntPtr region);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateRegionPath(HandleRef path, out IntPtr region);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateRegionRgnData(byte[] rgndata, int size, out IntPtr region);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateRegionHrgn(IntPtr hRgn, out IntPtr region);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCloneRegion(HandleRef region, out IntPtr cloneregion);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeleteRegion(HandleRef region);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillRegion(HandleRef graphics, HandleRef brush, HandleRef region);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetInfinite(HandleRef region);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetEmpty(HandleRef region);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCombineRegionRect(HandleRef region, ref RectangleF gprectf, CombineMode mode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCombineRegionRectI(HandleRef region, ref Rectangle gprect, CombineMode mode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCombineRegionPath(HandleRef region, HandleRef path, CombineMode mode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCombineRegionRegion(HandleRef region, HandleRef region2, CombineMode mode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTranslateRegion(HandleRef region, float dx, float dy);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTranslateRegionI(HandleRef region, int dx, int dy);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTransformRegion(HandleRef region, HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetRegionBounds(HandleRef region, HandleRef graphics, out RectangleF gprectf);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetRegionHRgn(HandleRef region, HandleRef graphics, out IntPtr hrgn);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsEmptyRegion(HandleRef region, HandleRef graphics, out int boolean);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsInfiniteRegion(HandleRef region, HandleRef graphics, out int boolean);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsEqualRegion(HandleRef region, HandleRef region2, HandleRef graphics, out int boolean);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetRegionDataSize(HandleRef region, out int bufferSize);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetRegionData(HandleRef region, byte[] regionData, int bufferSize, out int sizeFilled);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsVisibleRegionPoint(HandleRef region, float X, float Y, HandleRef graphics, out int boolean);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsVisibleRegionPointI(HandleRef region, int X, int Y, HandleRef graphics, out int boolean);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsVisibleRegionRect(HandleRef region, float X, float Y, float width, float height, HandleRef graphics, out int boolean);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsVisibleRegionRectI(HandleRef region, int X, int Y, int width, int height, HandleRef graphics, out int boolean);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetRegionScansCount(HandleRef region, out int count, HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetRegionScans(HandleRef region, RectangleF* rects, out int count, HandleRef matrix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateFromHDC(IntPtr hdc, out IntPtr graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetClipGraphics(HandleRef graphics, HandleRef srcgraphics, CombineMode mode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetClipRect(HandleRef graphics, float x, float y, float width, float height, CombineMode mode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetClipRectI(HandleRef graphics, int x, int y, int width, int height, CombineMode mode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetClipPath(HandleRef graphics, HandleRef path, CombineMode mode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetClipRegion(HandleRef graphics, HandleRef region, CombineMode mode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipResetClip(HandleRef graphics);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTranslateClip(HandleRef graphics, float dx, float dy);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetClip(HandleRef graphics, HandleRef region);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetClipBounds(HandleRef graphics, out RectangleF rect);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsClipEmpty(HandleRef graphics, out bool result);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetVisibleClipBounds(HandleRef graphics, out RectangleF rect);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsVisibleClipEmpty(HandleRef graphics, out bool result);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsVisiblePoint(HandleRef graphics, float x, float y, out bool result);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsVisiblePointI(HandleRef graphics, int x, int y, out bool result);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsVisibleRect(HandleRef graphics, float x, float y, float width, float height, out bool result);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipIsVisibleRectI(HandleRef graphics, int x, int y, int width, int height, out bool result);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFlush(HandleRef graphics, FlushIntention intention);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetDC(HandleRef graphics, out IntPtr hdc);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetStringFormatMeasurableCharacterRanges(HandleRef format, int rangeCount, [In] [Out] CharacterRange[] range);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateStringFormat(StringFormatFlags options, int language, out IntPtr format);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipStringFormatGetGenericDefault(out IntPtr format);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipStringFormatGetGenericTypographic(out IntPtr format);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDeleteStringFormat(HandleRef format);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCloneStringFormat(HandleRef format, out IntPtr newFormat);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetStringFormatFlags(HandleRef format, StringFormatFlags options);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetStringFormatFlags(HandleRef format, out StringFormatFlags result);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetStringFormatAlign(HandleRef format, StringAlignment align);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetStringFormatAlign(HandleRef format, out StringAlignment align);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetStringFormatLineAlign(HandleRef format, StringAlignment align);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetStringFormatLineAlign(HandleRef format, out StringAlignment align);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetStringFormatHotkeyPrefix(HandleRef format, HotkeyPrefix hotkeyPrefix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetStringFormatHotkeyPrefix(HandleRef format, out HotkeyPrefix hotkeyPrefix);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetStringFormatTabStops(HandleRef format, float firstTabOffset, int count, float[] tabStops);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetStringFormatTabStops(HandleRef format, int count, out float firstTabOffset, [In] [Out] float[] tabStops);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetStringFormatTabStopCount(HandleRef format, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetStringFormatMeasurableCharacterRangeCount(HandleRef format, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetStringFormatTrimming(HandleRef format, StringTrimming trimming);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetStringFormatTrimming(HandleRef format, out StringTrimming trimming);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetStringFormatDigitSubstitution(HandleRef format, int langID, StringDigitSubstitute sds);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetStringFormatDigitSubstitution(HandleRef format, out int langID, out StringDigitSubstitute sds);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageDimension(HandleRef image, out float width, out float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageWidth(HandleRef image, out int width);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageHeight(HandleRef image, out int height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageHorizontalResolution(HandleRef image, out float horzRes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageVerticalResolution(HandleRef image, out float vertRes);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageFlags(HandleRef image, out int flags);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageRawFormat(HandleRef image, ref Guid format);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImagePixelFormat(HandleRef image, out PixelFormat format);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipImageGetFrameCount(HandleRef image, ref Guid dimensionID, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipImageSelectActiveFrame(HandleRef image, ref Guid dimensionID, int frameIndex);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipImageRotateFlip(HandleRef image, int rotateFlipType);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetAllPropertyItems(HandleRef image, uint totalBufferSize, uint numProperties, PropertyItemInternal* allItems);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPropertyCount(HandleRef image, out uint numOfProperty);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPropertyIdList(HandleRef image, uint numOfProperty, int* list);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPropertyItem(HandleRef image, int propid, uint propSize, PropertyItemInternal* buffer);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPropertyItemSize(HandleRef image, int propid, out uint size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetPropertySize(HandleRef image, out uint totalBufferSize, out uint numProperties);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipRemovePropertyItem(HandleRef image, int propid);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSetPropertyItem(HandleRef image, PropertyItemInternal* item);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageType(HandleRef image, out int type);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetImageType(IntPtr image, out int type);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDisposeImage(HandleRef image);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDisposeImage(IntPtr image);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipCreateBitmapFromFile(string filename, out IntPtr bitmap);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipCreateBitmapFromFileICM(string filename, out IntPtr bitmap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateBitmapFromScan0(int width, int height, int stride, int format, IntPtr scan0, out IntPtr bitmap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateBitmapFromGraphics(int width, int height, HandleRef graphics, out IntPtr bitmap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateBitmapFromHBITMAP(IntPtr hbitmap, IntPtr hpalette, out IntPtr bitmap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateBitmapFromHICON(IntPtr hicon, out IntPtr bitmap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateBitmapFromResource(IntPtr hresource, IntPtr name, out IntPtr bitmap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateHBITMAPFromBitmap(HandleRef nativeBitmap, out IntPtr hbitmap, int argbBackground);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateHICONFromBitmap(HandleRef nativeBitmap, out IntPtr hicon);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCloneBitmapArea(float x, float y, float width, float height, int format, HandleRef srcbitmap, out IntPtr dstbitmap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCloneBitmapAreaI(int x, int y, int width, int height, int format, HandleRef srcbitmap, out IntPtr dstbitmap);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipBitmapLockBits(HandleRef bitmap, ref Rectangle rect, ImageLockMode flags, PixelFormat format, [In] [Out] BitmapData lockedBitmapData);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipBitmapUnlockBits(HandleRef bitmap, BitmapData lockedBitmapData);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipBitmapGetPixel(HandleRef bitmap, int x, int y, out int argb);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipBitmapSetPixel(HandleRef bitmap, int x, int y, int argb);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipBitmapSetResolution(HandleRef bitmap, float dpix, float dpiy);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipImageGetFrameDimensionsCount(HandleRef image, out int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipImageGetFrameDimensionsList(HandleRef image, Guid* dimensionIDs, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateMetafileFromEmf(IntPtr hEnhMetafile, bool deleteEmf, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipCreateMetafileFromWmf(IntPtr hMetafile, bool deleteWmf, WmfPlaceableFileHeader wmfplacealbeHeader, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipCreateMetafileFromFile(string file, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipRecordMetafile(IntPtr referenceHdc, EmfType emfType, IntPtr pframeRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafile(IntPtr referenceHdc, EmfType emfType, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileI(IntPtr referenceHdc, EmfType emfType, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileFileName(string fileName, IntPtr referenceHdc, EmfType emfType, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileFileName(string fileName, IntPtr referenceHdc, EmfType emfType, IntPtr pframeRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipRecordMetafileFileNameI(string fileName, IntPtr referenceHdc, EmfType emfType, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipPlayMetafileRecord(HandleRef metafile, EmfPlusRecordType recordType, int flags, int dataSize, byte[] data);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipSaveGraphics(HandleRef graphics, out int state);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawArc(HandleRef graphics, HandleRef pen, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawArcI(HandleRef graphics, HandleRef pen, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawLinesI(HandleRef graphics, HandleRef pen, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawBezier(HandleRef graphics, HandleRef pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawEllipse(HandleRef graphics, HandleRef pen, float x, float y, float width, float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawEllipseI(HandleRef graphics, HandleRef pen, int x, int y, int width, int height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawLine(HandleRef graphics, HandleRef pen, float x1, float y1, float x2, float y2);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawLineI(HandleRef graphics, HandleRef pen, int x1, int y1, int x2, int y2);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawLines(HandleRef graphics, HandleRef pen, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawPath(HandleRef graphics, HandleRef pen, HandleRef path);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawPie(HandleRef graphics, HandleRef pen, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawPieI(HandleRef graphics, HandleRef pen, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawPolygon(HandleRef graphics, HandleRef pen, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawPolygonI(HandleRef graphics, HandleRef pen, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillEllipse(HandleRef graphics, HandleRef brush, float x, float y, float width, float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillEllipseI(HandleRef graphics, HandleRef brush, int x, int y, int width, int height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillPolygon(HandleRef graphics, HandleRef brush, PointF* points, int count, FillMode brushMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillPolygonI(HandleRef graphics, HandleRef brush, Point* points, int count, FillMode brushMode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillRectangle(HandleRef graphics, HandleRef brush, float x, float y, float width, float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillRectangleI(HandleRef graphics, HandleRef brush, int x, int y, int width, int height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillRectangles(HandleRef graphics, HandleRef brush, RectangleF* rects, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillRectanglesI(HandleRef graphics, HandleRef brush, Rectangle* rects, int count);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipDrawString(HandleRef graphics, string textString, int length, HandleRef font, ref RectangleF layoutRect, HandleRef stringFormat, HandleRef brush);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImageRectI(HandleRef graphics, HandleRef image, int x, int y, int width, int height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGraphicsClear(HandleRef graphics, int argb);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawClosedCurve(HandleRef graphics, HandleRef pen, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawClosedCurveI(HandleRef graphics, HandleRef pen, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawClosedCurve2(HandleRef graphics, HandleRef pen, PointF* points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawClosedCurve2I(HandleRef graphics, HandleRef pen, Point* points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawCurve(HandleRef graphics, HandleRef pen, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawCurveI(HandleRef graphics, HandleRef pen, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawCurve2(HandleRef graphics, HandleRef pen, PointF* points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawCurve2I(HandleRef graphics, HandleRef pen, Point* points, int count, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawCurve3(HandleRef graphics, HandleRef pen, PointF* points, int count, int offset, int numberOfSegments, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawCurve3I(HandleRef graphics, HandleRef pen, Point* points, int count, int offset, int numberOfSegments, float tension);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillClosedCurve(HandleRef graphics, HandleRef brush, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillClosedCurveI(HandleRef graphics, HandleRef brush, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillClosedCurve2(HandleRef graphics, HandleRef brush, PointF* points, int count, float tension, FillMode mode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillClosedCurve2I(HandleRef graphics, HandleRef brush, Point* points, int count, float tension, FillMode mode);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillPie(HandleRef graphics, HandleRef brush, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipFillPieI(HandleRef graphics, HandleRef brush, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipMeasureString(HandleRef graphics, string textString, int length, HandleRef font, ref RectangleF layoutRect, HandleRef stringFormat, ref RectangleF boundingBox, out int codepointsFitted, out int linesFilled);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipMeasureCharacterRanges(HandleRef graphics, string textString, int length, HandleRef font, ref RectangleF layoutRect, HandleRef stringFormat, int characterCount, [In] [Out] IntPtr[] region);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImageI(HandleRef graphics, HandleRef image, int x, int y);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImage(HandleRef graphics, HandleRef image, float x, float y);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImagePoints(HandleRef graphics, HandleRef image, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImagePointsI(HandleRef graphics, HandleRef image, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImageRectRectI(HandleRef graphics, HandleRef image, int dstx, int dsty, int dstwidth, int dstheight, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort? callback, HandleRef callbackdata);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImagePointsRect(HandleRef graphics, HandleRef image, PointF* points, int count, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort? callback, HandleRef callbackdata);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImageRectRect(HandleRef graphics, HandleRef image, float dstx, float dsty, float dstwidth, float dstheight, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort? callback, HandleRef callbackdata);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImagePointsRectI(HandleRef graphics, HandleRef image, Point* points, int count, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcunit, HandleRef imageAttributes, Graphics.DrawImageAbort? callback, HandleRef callbackdata);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImageRect(HandleRef graphics, HandleRef image, float x, float y, float width, float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImagePointRect(HandleRef graphics, HandleRef image, float x, float y, float srcx, float srcy, float srcwidth, float srcheight, int srcunit);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawImagePointRectI(HandleRef graphics, HandleRef image, int x, int y, int srcx, int srcy, int srcwidth, int srcheight, int srcunit);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawRectangle(HandleRef graphics, HandleRef pen, float x, float y, float width, float height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawRectangleI(HandleRef graphics, HandleRef pen, int x, int y, int width, int height);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawRectangles(HandleRef graphics, HandleRef pen, RectangleF* rects, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipDrawRectanglesI(HandleRef graphics, HandleRef pen, Rectangle* rects, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTransformPoints(HandleRef graphics, int destSpace, int srcSpace, PointF* points, int count);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipTransformPointsI(HandleRef graphics, int destSpace, int srcSpace, Point* points, int count);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipLoadImageFromFileICM(string filename, out IntPtr image);

            [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
            internal static extern int GdipLoadImageFromFile(string filename, out IntPtr image);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetEncoderParameterListSize(HandleRef image, ref Guid encoder, out int size);

            [DllImport(LibraryName, ExactSpelling = true)]
            internal static extern int GdipGetEncoderParameterList(HandleRef image, ref Guid encoder, int size, IntPtr buffer);
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
                OperatingSystem os = Environment.OSVersion;
                StartupInput result = default;

                // In Windows 7 GDI+1.1 story is different as there are different binaries per GDI+ version.
                bool isWindows7 = os.Platform == PlatformID.Win32NT && os.Version.Major == 6 && os.Version.Minor == 1;
                result.GdiplusVersion = isWindows7 ? 1 : 2;
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
            public IntPtr hook; //not used
            public IntPtr unhook; //not used.
        }
    }
}
