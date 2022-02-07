// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.GeneratedMarshalling;
#endif

namespace System.Drawing
{
    // Raw function imports for gdiplus
    // Functions are loaded manually in order to accomodate different shared library names on Unix.
    internal static partial class SafeNativeMethods
    {
        internal static unsafe partial class Gdip
        {
            // Shared function imports (all platforms)
            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipBeginContainer(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, ref RectangleF dstRect, ref RectangleF srcRect, GraphicsUnit unit, out int state);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipBeginContainer2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int state);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipBeginContainerI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, ref Rectangle dstRect, ref Rectangle srcRect, GraphicsUnit unit, out int state);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipEndContainer(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int state);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateAdjustableArrowCap(float height, float width, bool isFilled, out IntPtr adjustableArrowCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetAdjustableArrowCapHeight(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, out float height);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetAdjustableArrowCapHeight(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, float height);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetAdjustableArrowCapWidth(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, float width);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetAdjustableArrowCapWidth(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, out float width);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetAdjustableArrowCapMiddleInset(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, float middleInset);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetAdjustableArrowCapMiddleInset(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, out float middleInset);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetAdjustableArrowCapFillState(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, bool fillState);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetAdjustableArrowCapFillState(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, out bool fillState);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetCustomLineCapType(IntPtr customCap, out CustomLineCapType capType);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateCustomLineCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fillpath,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef strokepath, LineCap baseCap, float baseInset, out IntPtr customCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDeleteCustomLineCap(IntPtr customCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDeleteCustomLineCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCloneCustomLineCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, out IntPtr clonedCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetCustomLineCapStrokeCaps(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, LineCap startCap, LineCap endCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetCustomLineCapStrokeCaps(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, out LineCap startCap, out LineCap endCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetCustomLineCapStrokeJoin(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, LineJoin lineJoin);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetCustomLineCapStrokeJoin(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, out LineJoin lineJoin);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetCustomLineCapBaseCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, LineCap baseCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetCustomLineCapBaseCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, out LineCap baseCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetCustomLineCapBaseInset(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, float inset);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetCustomLineCapBaseInset(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, out float inset);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetCustomLineCapWidthScale(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, float widthScale);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetCustomLineCapWidthScale(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, out float widthScale);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreatePathIter(out IntPtr pathIter,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDeletePathIter(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipPathIterNextSubpath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int resultCount, out int startIndex, out int endIndex, out bool isClosed);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipPathIterNextSubpathPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int resultCount,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, out bool isClosed);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipPathIterNextPathType(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int resultCount, out byte pathType, out int startIndex, out int endIndex);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipPathIterNextMarker(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int resultCount, out int startIndex, out int endIndex);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipPathIterNextMarkerPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int resultCount,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipPathIterGetCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipPathIterGetSubpathCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipPathIterHasCurve(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out bool hasCurve);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipPathIterRewind(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipPathIterEnumerate(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int resultCount, PointF* points, byte* types, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipPathIterCopyData(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int resultCount, PointF* points, byte* types, int startIndex, int endIndex);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateHatchBrush(int hatchstyle, int forecol, int backcol, out IntPtr brush);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetHatchStyle(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int hatchstyle);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetHatchForegroundColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int forecol);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetHatchBackgroundColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int backcol);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCloneBrush(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out IntPtr clonebrush);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateLineBrush(ref PointF point1, ref PointF point2, int color1, int color2, WrapMode wrapMode, out IntPtr lineGradient);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateLineBrushI(ref Point point1, ref Point point2, int color1, int color2, WrapMode wrapMode, out IntPtr lineGradient);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateLineBrushFromRect(ref RectangleF rect, int color1, int color2, LinearGradientMode lineGradientMode, WrapMode wrapMode, out IntPtr lineGradient);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateLineBrushFromRectI(ref Rectangle rect, int color1, int color2, LinearGradientMode lineGradientMode, WrapMode wrapMode, out IntPtr lineGradient);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateLineBrushFromRectWithAngle(ref RectangleF rect, int color1, int color2, float angle, bool isAngleScaleable, WrapMode wrapMode, out IntPtr lineGradient);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateLineBrushFromRectWithAngleI(ref Rectangle rect, int color1, int color2, float angle, bool isAngleScaleable, WrapMode wrapMode, out IntPtr lineGradient);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetLineColors(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int color1, int color2);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetLineColors(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int[] colors);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetLineRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out RectangleF gprectf);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetLineGammaCorrection(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out bool useGammaCorrection);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetLineGammaCorrection(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, bool useGammaCorrection);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetLineSigmaBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float focus, float scale);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetLineLinearBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float focus, float scale);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetLineBlendCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetLineBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetLineBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetLinePresetBlendCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetLinePresetBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetLinePresetBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetLineWrapMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int wrapMode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetLineWrapMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int wrapMode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipResetLineTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipMultiplyLineTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetLineTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetLineTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipTranslateLineTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float dx, float dy, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipScaleLineTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float sx, float sy, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipRotateLineTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float angle, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreatePathGradient(PointF* points, int count, WrapMode wrapMode, out IntPtr brush);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreatePathGradientI(Point* points, int count, WrapMode wrapMode, out IntPtr brush);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreatePathGradientFromPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, out IntPtr brush);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathGradientCenterColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int color);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPathGradientCenterColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int color);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathGradientSurroundColorsWithCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int[] color, ref int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPathGradientSurroundColorsWithCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int[] argb, ref int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathGradientCenterPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out PointF point);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPathGradientCenterPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, ref PointF point);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathGradientRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out RectangleF gprectf);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathGradientPointCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathGradientSurroundColorCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathGradientBlendCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathGradientBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float[] blend, float[] positions, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPathGradientBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathGradientPresetBlendCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathGradientPresetBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int[] blend, float[] positions, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPathGradientPresetBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int[] blend, float[] positions, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPathGradientSigmaBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float focus, float scale);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPathGradientLinearBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float focus, float scale);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPathGradientWrapMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int wrapmode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathGradientWrapMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int wrapmode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPathGradientTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathGradientTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipResetPathGradientTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipMultiplyPathGradientTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipTranslatePathGradientTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float dx, float dy, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipScalePathGradientTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float sx, float sy, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipRotatePathGradientTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float angle, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPathGradientFocusScales(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float[] xScale, float[] yScale);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPathGradientFocusScales(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float xScale, float yScale);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateImageAttributes(out IntPtr imageattr);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCloneImageAttributes(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, out IntPtr cloneImageattr);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDisposeImageAttributes(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetImageAttributesColorMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, bool enableFlag,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(ColorMatrix.PinningMarshaller))]
#endif
            ColorMatrix? colorMatrix,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(ColorMatrix.PinningMarshaller))]
#endif
            ColorMatrix? grayMatrix, ColorMatrixFlag flags);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetImageAttributesThreshold(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, bool enableFlag, float threshold);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetImageAttributesGamma(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, bool enableFlag, float gamma);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetImageAttributesNoOp(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, bool enableFlag);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetImageAttributesColorKeys(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, bool enableFlag, int colorLow, int colorHigh);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetImageAttributesOutputChannel(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, bool enableFlag, ColorChannelFlag flags);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode)]
            internal static partial int GdipSetImageAttributesOutputChannelColorProfile(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, bool enableFlag, string colorProfileFilename);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetImageAttributesRemapTable(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, bool enableFlag, int mapSize, IntPtr map);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetImageAttributesWrapMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, int wrapmode, int argb, bool clamp);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageAttributesAdjustedPalette(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, IntPtr palette, ColorAdjustType type);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageDecodersSize(out int numDecoders, out int size);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageDecoders(int numDecoders, int size, IntPtr decoders);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageEncodersSize(out int numEncoders, out int size);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageEncoders(int numEncoders, int size, IntPtr encoders);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateSolidFill(int color, out IntPtr brush);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetSolidFillColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int color);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetSolidFillColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int color);


            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateTexture(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap, int wrapmode, out IntPtr texture);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateTexture2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap, int wrapmode, float x, float y, float width, float height, out IntPtr texture);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateTextureIA(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageAttrib, float x, float y, float width, float height, out IntPtr texture);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateTexture2I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap, int wrapmode, int x, int y, int width, int height, out IntPtr texture);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateTextureIAI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageAttrib, int x, int y, int width, int height, out IntPtr texture);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetTextureTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetTextureTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipResetTextureTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipMultiplyTextureTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipTranslateTextureTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float dx, float dy, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipScaleTextureTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float sx, float sy, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipRotateTextureTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float angle, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetTextureWrapMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int wrapMode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetTextureWrapMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int wrapMode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetTextureImage(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out IntPtr image);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetFontCollectionFamilyCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontCollection, out int numFound);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetFontCollectionFamilyList(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontCollection, int numSought, IntPtr[] gpfamilies, out int numFound);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCloneFontFamily(IntPtr fontfamily, out IntPtr clonefontfamily);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode)]
            internal static partial int GdipCreateFontFamilyFromName(string name,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontCollection, out IntPtr FontFamily);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetGenericFontFamilySansSerif(out IntPtr fontfamily);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetGenericFontFamilySerif(out IntPtr fontfamily);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetGenericFontFamilyMonospace(out IntPtr fontfamily);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDeleteFontFamily(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontFamily);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode)]
            internal static partial int GdipGetFamilyName(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef family, char* name, int language);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsStyleAvailable(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef family, FontStyle style, out int isStyleAvailable);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetEmHeight(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef family, FontStyle style, out int EmHeight);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetCellAscent(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef family, FontStyle style, out int CellAscent);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetCellDescent(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef family, FontStyle style, out int CellDescent);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetLineSpacing(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef family, FontStyle style, out int LineSpaceing);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipNewInstalledFontCollection(out IntPtr fontCollection);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipNewPrivateFontCollection(out IntPtr fontCollection);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDeletePrivateFontCollection(ref IntPtr fontCollection);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode)]
            internal static partial int GdipPrivateAddFontFile(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontCollection, string filename);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipPrivateAddMemoryFont(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontCollection, IntPtr memory, int length);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateFont(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontFamily, float emSize, FontStyle style, GraphicsUnit unit, out IntPtr font);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateFontFromDC(IntPtr hdc, ref IntPtr font);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCloneFont(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font, out IntPtr cloneFont);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDeleteFont(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetFamily(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font, out IntPtr family);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetFontStyle(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font, out FontStyle style);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetFontSize(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font, out float size);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetFontHeight(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out float size);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetFontHeightGivenDPI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font, float dpi, out float size);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetFontUnit(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font, out GraphicsUnit unit);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetLogFontW(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, ref Interop.User32.LOGFONT lf);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreatePen1(int argb, float width, int unit, out IntPtr pen);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreatePen2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float width, int unit, out IntPtr pen);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipClonePen(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out IntPtr clonepen);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDeletePen(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef Pen);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PenAlignment penAlign);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out PenAlignment penAlign);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenWidth(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float width);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenWidth(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float[] width);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenLineCap197819(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int startCap, int endCap, int dashCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenStartCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int startCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenEndCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int endCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenStartCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int startCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenEndCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int endCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenDashCap197819(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int dashCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenDashCap197819(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int dashCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenLineJoin(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int lineJoin);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenLineJoin(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int lineJoin);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenCustomStartCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenCustomStartCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out IntPtr customCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenCustomEndCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenCustomEndCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out IntPtr customCap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenMiterLimit(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float miterLimit);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenMiterLimit(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float[] miterLimit);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipResetPenTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipMultiplyPenTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipTranslatePenTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float dx, float dy, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipScalePenTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float sx, float sy, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipRotatePenTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float angle, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int argb);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int argb);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenBrushFill(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenBrushFill(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out IntPtr brush);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenFillType(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int pentype);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenDashStyle(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int dashstyle);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenDashStyle(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int dashstyle);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenDashArray(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef memorydash, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenDashOffset(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float[] dashoffset);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenDashOffset(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float dashoffset);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenDashCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int dashcount);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenDashArray(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float[] memorydash, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenCompoundCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPenCompoundArray(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float[] array, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPenCompoundArray(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float[] array, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetWorldTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipResetWorldTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipMultiplyWorldTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipTranslateWorldTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float dx, float dy, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipScaleWorldTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float sx, float sy, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipRotateWorldTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float angle, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetWorldTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetCompositingMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, CompositingMode compositingMode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetTextRenderingHint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, TextRenderingHint textRenderingHint);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetTextContrast(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int textContrast);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetInterpolationMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, InterpolationMode interpolationMode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetCompositingMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out CompositingMode compositingMode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetRenderingOrigin(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int x, int y);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetRenderingOrigin(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int x, out int y);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetCompositingQuality(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, CompositingQuality quality);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetCompositingQuality(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out CompositingQuality quality);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetSmoothingMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, SmoothingMode smoothingMode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetSmoothingMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out SmoothingMode smoothingMode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPixelOffsetMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, PixelOffsetMode pixelOffsetMode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPixelOffsetMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out PixelOffsetMode pixelOffsetMode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetTextRenderingHint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out TextRenderingHint textRenderingHint);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetTextContrast(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int textContrast);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetInterpolationMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out InterpolationMode interpolationMode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPageUnit(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out GraphicsUnit unit);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPageScale(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out float scale);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPageUnit(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, GraphicsUnit unit);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPageScale(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float scale);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetDpiX(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out float dpi);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetDpiY(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out float dpi);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateMatrix(out IntPtr matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateMatrix2(float m11, float m12, float m21, float m22, float dx, float dy, out IntPtr matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateMatrix3(ref RectangleF rect, PointF* dstplg, out IntPtr matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateMatrix3I(ref Rectangle rect, Point* dstplg, out IntPtr matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCloneMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, out IntPtr cloneMatrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDeleteMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetMatrixElements(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, float m11, float m12, float m21, float m22, float dx, float dy);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipMultiplyMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix2, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipTranslateMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, float offsetX, float offsetY, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipScaleMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, float scaleX, float scaleY, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipRotateMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, float angle, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipShearMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, float shearX, float shearY, MatrixOrder order);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipInvertMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipTransformMatrixPoints(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, PointF* pts, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipTransformMatrixPointsI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, Point* pts, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipVectorTransformMatrixPoints(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, PointF* pts, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipVectorTransformMatrixPointsI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, Point* pts, int count);

            [GeneratedDllImport(LibraryName)]
            internal static unsafe partial int GdipGetMatrixElements(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, float* m);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsMatrixInvertible(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, out int boolean);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsMatrixIdentity(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, out int boolean);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsMatrixEqual(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix2, out int boolean);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateRegion(out IntPtr region);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateRegionRect(ref RectangleF gprectf, out IntPtr region);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateRegionRectI(ref Rectangle gprect, out IntPtr region);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateRegionPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, out IntPtr region);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateRegionRgnData(byte[] rgndata, int size, out IntPtr region);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateRegionHrgn(IntPtr hRgn, out IntPtr region);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCloneRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, out IntPtr cloneregion);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDeleteRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetInfinite(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetEmpty(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCombineRegionRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, ref RectangleF gprectf, CombineMode mode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCombineRegionRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, ref Rectangle gprect, CombineMode mode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCombineRegionPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, CombineMode mode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCombineRegionRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region2, CombineMode mode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipTranslateRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, float dx, float dy);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipTranslateRegionI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, int dx, int dy);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipTransformRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetRegionBounds(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out RectangleF gprectf);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetRegionHRgn(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out IntPtr hrgn);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsEmptyRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int boolean);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsInfiniteRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int boolean);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsEqualRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region2,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int boolean);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetRegionDataSize(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, out int bufferSize);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetRegionData(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, byte[] regionData, int bufferSize, out int sizeFilled);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsVisibleRegionPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, float X, float Y,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int boolean);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsVisibleRegionPointI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, int X, int Y,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int boolean);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsVisibleRegionRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, float X, float Y, float width, float height,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int boolean);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsVisibleRegionRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, int X, int Y, int width, int height,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int boolean);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetRegionScansCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, out int count,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetRegionScans(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, RectangleF* rects, out int count,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateFromHDC(IntPtr hdc, out IntPtr graphics);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetClipGraphics(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef srcgraphics, CombineMode mode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetClipRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float x, float y, float width, float height, CombineMode mode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetClipRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int x, int y, int width, int height, CombineMode mode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetClipPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, CombineMode mode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetClipRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, CombineMode mode);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipResetClip(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipTranslateClip(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float dx, float dy);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetClip(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetClipBounds(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out RectangleF rect);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsClipEmpty(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out bool result);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetVisibleClipBounds(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out RectangleF rect);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsVisibleClipEmpty(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out bool result);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsVisiblePoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float x, float y, out bool result);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsVisiblePointI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int x, int y, out bool result);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsVisibleRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float x, float y, float width, float height, out bool result);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipIsVisibleRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int x, int y, int width, int height, out bool result);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipFlush(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, FlushIntention intention);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetDC(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out IntPtr hdc);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetStringFormatMeasurableCharacterRanges(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, int rangeCount, CharacterRange[] range);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateStringFormat(StringFormatFlags options, int language, out IntPtr format);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipStringFormatGetGenericDefault(out IntPtr format);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipStringFormatGetGenericTypographic(out IntPtr format);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDeleteStringFormat(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCloneStringFormat(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out IntPtr newFormat);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetStringFormatFlags(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, StringFormatFlags options);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetStringFormatFlags(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out StringFormatFlags result);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetStringFormatAlign(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, StringAlignment align);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetStringFormatAlign(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out StringAlignment align);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetStringFormatLineAlign(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, StringAlignment align);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetStringFormatLineAlign(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out StringAlignment align);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetStringFormatHotkeyPrefix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, HotkeyPrefix hotkeyPrefix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetStringFormatHotkeyPrefix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out HotkeyPrefix hotkeyPrefix);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetStringFormatTabStops(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, float firstTabOffset, int count, float[] tabStops);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetStringFormatTabStops(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, int count, out float firstTabOffset, float[] tabStops);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetStringFormatTabStopCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetStringFormatMeasurableCharacterRangeCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetStringFormatTrimming(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, StringTrimming trimming);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetStringFormatTrimming(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out StringTrimming trimming);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetStringFormatDigitSubstitution(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, int langID, StringDigitSubstitute sds);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetStringFormatDigitSubstitution(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out int langID, out StringDigitSubstitute sds);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageDimension(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out float width, out float height);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageWidth(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out int width);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageHeight(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out int height);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageHorizontalResolution(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out float horzRes);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageVerticalResolution(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out float vertRes);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageFlags(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out int flags);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageRawFormat(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, ref Guid format);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImagePixelFormat(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out PixelFormat format);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipImageGetFrameCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, ref Guid dimensionID, out int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipImageSelectActiveFrame(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, ref Guid dimensionID, int frameIndex);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipImageRotateFlip(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int rotateFlipType);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetAllPropertyItems(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, uint totalBufferSize, uint numProperties, PropertyItemInternal* allItems);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPropertyCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out uint numOfProperty);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPropertyIdList(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, uint numOfProperty, int* list);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPropertyItem(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int propid, uint propSize, PropertyItemInternal* buffer);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPropertyItemSize(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int propid, out uint size);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetPropertySize(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out uint totalBufferSize, out uint numProperties);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipRemovePropertyItem(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int propid);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSetPropertyItem(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, PropertyItemInternal* item);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageType(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out int type);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetImageType(IntPtr image, out int type);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDisposeImage(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipDisposeImage(IntPtr image);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipCreateBitmapFromFile(string filename, out IntPtr bitmap);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipCreateBitmapFromFileICM(string filename, out IntPtr bitmap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromScan0(int width, int height, int stride, int format, IntPtr scan0, out IntPtr bitmap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromGraphics(int width, int height,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out IntPtr bitmap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromHBITMAP(IntPtr hbitmap, IntPtr hpalette, out IntPtr bitmap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromHICON(IntPtr hicon, out IntPtr bitmap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromResource(IntPtr hresource, IntPtr name, out IntPtr bitmap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateHBITMAPFromBitmap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef nativeBitmap, out IntPtr hbitmap, int argbBackground);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateHICONFromBitmap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef nativeBitmap, out IntPtr hicon);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCloneBitmapArea(float x, float y, float width, float height, int format,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef srcbitmap, out IntPtr dstbitmap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCloneBitmapAreaI(int x, int y, int width, int height, int format,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef srcbitmap, out IntPtr dstbitmap);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipBitmapLockBits(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap, ref Rectangle rect, ImageLockMode flags, PixelFormat format,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(BitmapData.PinningMarshaller))]
#endif
            BitmapData lockedBitmapData);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipBitmapUnlockBits(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(BitmapData.PinningMarshaller))]
#endif
            BitmapData lockedBitmapData);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipBitmapGetPixel(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap, int x, int y, out int argb);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipBitmapSetPixel(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap, int x, int y, int argb);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipBitmapSetResolution(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap, float dpix, float dpiy);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipImageGetFrameDimensionsCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipImageGetFrameDimensionsList(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, Guid* dimensionIDs, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateMetafileFromEmf(IntPtr hEnhMetafile, bool deleteEmf, out IntPtr metafile);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipCreateMetafileFromWmf(IntPtr hMetafile, bool deleteWmf,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(WmfPlaceableFileHeader.PinningMarshaller))]
#endif
                WmfPlaceableFileHeader wmfplacealbeHeader, out IntPtr metafile);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipCreateMetafileFromFile(string file, out IntPtr metafile);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipRecordMetafile(IntPtr referenceHdc, EmfType emfType, IntPtr pframeRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipRecordMetafile(IntPtr referenceHdc, EmfType emfType, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipRecordMetafileI(IntPtr referenceHdc, EmfType emfType, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipRecordMetafileFileName(string fileName, IntPtr referenceHdc, EmfType emfType, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipRecordMetafileFileName(string fileName, IntPtr referenceHdc, EmfType emfType, IntPtr pframeRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipRecordMetafileFileNameI(string fileName, IntPtr referenceHdc, EmfType emfType, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipPlayMetafileRecord(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, EmfPlusRecordType recordType, int flags, int dataSize, byte[] data);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipSaveGraphics(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int state);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawArc(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawArcI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawLinesI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawBezier(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawEllipse(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float x, float y, float width, float height);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawEllipseI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int x, int y, int width, int height);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawLine(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float x1, float y1, float x2, float y2);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawLineI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int x1, int y1, int x2, int y2);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawLines(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawPie(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawPieI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawPolygon(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawPolygonI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillEllipse(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float x, float y, float width, float height);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillEllipseI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int x, int y, int width, int height);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillPolygon(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, PointF* points, int count, FillMode brushMode);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillPolygonI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, Point* points, int count, FillMode brushMode);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillRectangle(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float x, float y, float width, float height);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillRectangleI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int x, int y, int width, int height);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillRectangles(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, RectangleF* rects, int count);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillRectanglesI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, Rectangle* rects, int count);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, SetLastError = true)]
            internal static partial int GdipDrawString(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, string textString, int length,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font, ref RectangleF layoutRect,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef stringFormat,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImageRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int x, int y, int width, int height);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGraphicsClear(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int argb);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawClosedCurve(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawClosedCurveI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawClosedCurve2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count, float tension);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawClosedCurve2I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count, float tension);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawCurve(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawCurveI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawCurve2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count, float tension);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawCurve2I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count, float tension);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawCurve3(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count, int offset, int numberOfSegments, float tension);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawCurve3I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count, int offset, int numberOfSegments, float tension);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillClosedCurve(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, PointF* points, int count);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillClosedCurveI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, Point* points, int count);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillClosedCurve2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, PointF* points, int count, float tension, FillMode mode);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillClosedCurve2I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, Point* points, int count, float tension, FillMode mode);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillPie(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillPieI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode)]
            internal static partial int GdipMeasureString(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, string textString, int length,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font, ref RectangleF layoutRect,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef stringFormat, ref RectangleF boundingBox, out int codepointsFitted, out int linesFilled);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode)]
            internal static partial int GdipMeasureCharacterRanges(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, string textString, int length,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font, ref RectangleF layoutRect,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef stringFormat, int characterCount, IntPtr[] region);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImageI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int x, int y);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImage(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, float x, float y);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImagePoints(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, PointF* points, int count);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImagePointsI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, Point* points, int count);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImageRectRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int dstx, int dsty, int dstwidth, int dstheight, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcunit,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageAttributes, Graphics.DrawImageAbort? callback,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef callbackdata);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImagePointsRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, PointF* points, int count, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcunit,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageAttributes, Graphics.DrawImageAbort? callback,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef callbackdata);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImageRectRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, float dstx, float dsty, float dstwidth, float dstheight, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcunit,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageAttributes, Graphics.DrawImageAbort? callback,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef callbackdata);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImagePointsRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, Point* points, int count, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcunit,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageAttributes, Graphics.DrawImageAbort? callback,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef callbackdata);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImageRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, float x, float y, float width, float height);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImagePointRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, float x, float y, float srcx, float srcy, float srcwidth, float srcheight, int srcunit);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImagePointRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int x, int y, int srcx, int srcy, int srcwidth, int srcheight, int srcunit);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawRectangle(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float x, float y, float width, float height);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawRectangleI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int x, int y, int width, int height);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawRectangles(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, RectangleF* rects, int count);

            [GeneratedDllImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawRectanglesI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Rectangle* rects, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipTransformPoints(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int destSpace, int srcSpace, PointF* points, int count);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipTransformPointsI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int destSpace, int srcSpace, Point* points, int count);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipLoadImageFromFileICM(string filename, out IntPtr image);

            [GeneratedDllImport(LibraryName, CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static partial int GdipLoadImageFromFile(string filename, out IntPtr image);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetEncoderParameterListSize(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, ref Guid encoder, out int size);

            [GeneratedDllImport(LibraryName)]
            internal static partial int GdipGetEncoderParameterList(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, ref Guid encoder, int size, IntPtr buffer);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StartupInputEx
        {
            public int GdiplusVersion;             // Must be 1 or 2

            public IntPtr DebugEventCallback;

            public Interop.BOOL SuppressBackgroundThread;     // FALSE unless you're prepared to call
                                                      // the hook/unhook functions properly

            public Interop.BOOL SuppressExternalCodecs;       // FALSE unless you want GDI+ only to use
                                                      // its internal image codecs.
            public int StartupParameters;

            public static StartupInputEx GetDefault()
            {
                OperatingSystem os = Environment.OSVersion;
                StartupInputEx result = default;

                // In Windows 7 GDI+1.1 story is different as there are different binaries per GDI+ version.
                bool isWindows7 = os.Platform == PlatformID.Win32NT && os.Version.Major == 6 && os.Version.Minor == 1;
                result.GdiplusVersion = isWindows7 ? 1 : 2;
                result.SuppressBackgroundThread = Interop.BOOL.FALSE;
                result.SuppressExternalCodecs = Interop.BOOL.FALSE;
                result.StartupParameters = 0;
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
