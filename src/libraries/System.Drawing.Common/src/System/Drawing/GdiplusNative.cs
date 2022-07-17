// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.Drawing.Text;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

namespace System.Drawing
{
    // Raw function imports for gdiplus
    internal static partial class SafeNativeMethods
    {
        internal static unsafe partial class Gdip
        {
            private const string LibraryName = "gdiplus.dll";

            // Imported functions
            [LibraryImport(LibraryName)]
            private static partial int GdiplusStartup(out IntPtr token, in StartupInputEx input, out StartupOutput output);

            [LibraryImport(LibraryName)]
            internal static partial int GdipBeginContainer(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, ref RectangleF dstRect, ref RectangleF srcRect, GraphicsUnit unit, out int state);

            [LibraryImport(LibraryName)]
            internal static partial int GdipBeginContainer2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int state);

            [LibraryImport(LibraryName)]
            internal static partial int GdipBeginContainerI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, ref Rectangle dstRect, ref Rectangle srcRect, GraphicsUnit unit, out int state);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEndContainer(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int state);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateAdjustableArrowCap(float height, float width, [MarshalAs(UnmanagedType.Bool)] bool isFilled, out IntPtr adjustableArrowCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetAdjustableArrowCapHeight(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, out float height);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetAdjustableArrowCapHeight(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, float height);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetAdjustableArrowCapWidth(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, float width);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetAdjustableArrowCapWidth(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, out float width);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetAdjustableArrowCapMiddleInset(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, float middleInset);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetAdjustableArrowCapMiddleInset(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, out float middleInset);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetAdjustableArrowCapFillState(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, [MarshalAs(UnmanagedType.Bool)] bool fillState);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetAdjustableArrowCapFillState(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef adjustableArrowCap, [MarshalAs(UnmanagedType.Bool)] out bool fillState);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetCustomLineCapType(IntPtr customCap, out CustomLineCapType capType);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateCustomLineCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fillpath,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef strokepath, LineCap baseCap, float baseInset, out IntPtr customCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeleteCustomLineCap(IntPtr customCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeleteCustomLineCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCloneCustomLineCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, out IntPtr clonedCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetCustomLineCapStrokeCaps(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, LineCap startCap, LineCap endCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetCustomLineCapStrokeCaps(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, out LineCap startCap, out LineCap endCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetCustomLineCapStrokeJoin(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, LineJoin lineJoin);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetCustomLineCapStrokeJoin(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, out LineJoin lineJoin);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetCustomLineCapBaseCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, LineCap baseCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetCustomLineCapBaseCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, out LineCap baseCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetCustomLineCapBaseInset(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, float inset);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetCustomLineCapBaseInset(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, out float inset);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetCustomLineCapWidthScale(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, float widthScale);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetCustomLineCapWidthScale(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap, out float widthScale);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreatePathIter(out IntPtr pathIter,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeletePathIter(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter);

            [LibraryImport(LibraryName)]
            internal static partial int GdipPathIterNextSubpath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int resultCount, out int startIndex, out int endIndex, [MarshalAs(UnmanagedType.Bool)] out bool isClosed);

            [LibraryImport(LibraryName)]
            internal static partial int GdipPathIterNextSubpathPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int resultCount,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, [MarshalAs(UnmanagedType.Bool)] out bool isClosed);

            [LibraryImport(LibraryName)]
            internal static partial int GdipPathIterNextPathType(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int resultCount, out byte pathType, out int startIndex, out int endIndex);

            [LibraryImport(LibraryName)]
            internal static partial int GdipPathIterNextMarker(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int resultCount, out int startIndex, out int endIndex);

            [LibraryImport(LibraryName)]
            internal static partial int GdipPathIterNextMarkerPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int resultCount,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipPathIterGetCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipPathIterGetSubpathCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipPathIterHasCurve(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, [MarshalAs(UnmanagedType.Bool)] out bool hasCurve);

            [LibraryImport(LibraryName)]
            internal static partial int GdipPathIterRewind(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter);

            [LibraryImport(LibraryName)]
            internal static partial int GdipPathIterEnumerate(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int resultCount, PointF* points, byte* types, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipPathIterCopyData(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pathIter, out int resultCount, PointF* points, byte* types, int startIndex, int endIndex);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateHatchBrush(int hatchstyle, int forecol, int backcol, out IntPtr brush);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetHatchStyle(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int hatchstyle);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetHatchForegroundColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int forecol);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetHatchBackgroundColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int backcol);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCloneBrush(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out IntPtr clonebrush);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateLineBrush(ref PointF point1, ref PointF point2, int color1, int color2, WrapMode wrapMode, out IntPtr lineGradient);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateLineBrushI(ref Point point1, ref Point point2, int color1, int color2, WrapMode wrapMode, out IntPtr lineGradient);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateLineBrushFromRect(ref RectangleF rect, int color1, int color2, LinearGradientMode lineGradientMode, WrapMode wrapMode, out IntPtr lineGradient);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateLineBrushFromRectI(ref Rectangle rect, int color1, int color2, LinearGradientMode lineGradientMode, WrapMode wrapMode, out IntPtr lineGradient);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateLineBrushFromRectWithAngle(ref RectangleF rect, int color1, int color2, float angle, [MarshalAs(UnmanagedType.Bool)] bool isAngleScaleable, WrapMode wrapMode, out IntPtr lineGradient);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateLineBrushFromRectWithAngleI(ref Rectangle rect, int color1, int color2, float angle, [MarshalAs(UnmanagedType.Bool)] bool isAngleScaleable, WrapMode wrapMode, out IntPtr lineGradient);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetLineColors(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int color1, int color2);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetLineColors(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int[] colors);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetLineRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out RectangleF gprectf);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetLineGammaCorrection(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, [MarshalAs(UnmanagedType.Bool)] out bool useGammaCorrection);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetLineGammaCorrection(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, [MarshalAs(UnmanagedType.Bool)] bool useGammaCorrection);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetLineSigmaBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float focus, float scale);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetLineLinearBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float focus, float scale);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetLineBlendCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetLineBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetLineBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetLinePresetBlendCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetLinePresetBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetLinePresetBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetLineWrapMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int wrapMode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetLineWrapMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int wrapMode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipResetLineTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush);

            [LibraryImport(LibraryName)]
            internal static partial int GdipMultiplyLineTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetLineTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetLineTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTranslateLineTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float dx, float dy, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipScaleLineTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float sx, float sy, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipRotateLineTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float angle, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreatePathGradient(PointF* points, int count, WrapMode wrapMode, out IntPtr brush);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreatePathGradientI(Point* points, int count, WrapMode wrapMode, out IntPtr brush);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreatePathGradientFromPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, out IntPtr brush);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathGradientCenterColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int color);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPathGradientCenterColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int color);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathGradientSurroundColorsWithCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int[] color, ref int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPathGradientSurroundColorsWithCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int[] argb, ref int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathGradientCenterPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out PointF point);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPathGradientCenterPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, ref PointF point);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathGradientRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out RectangleF gprectf);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathGradientPointCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathGradientSurroundColorCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathGradientBlendCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathGradientBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float[] blend, float[] positions, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPathGradientBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathGradientPresetBlendCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathGradientPresetBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int[] blend, float[] positions, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPathGradientPresetBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int[] blend, float[] positions, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPathGradientSigmaBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float focus, float scale);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPathGradientLinearBlend(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float focus, float scale);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPathGradientWrapMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int wrapmode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathGradientWrapMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int wrapmode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPathGradientTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathGradientTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipResetPathGradientTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush);

            [LibraryImport(LibraryName)]
            internal static partial int GdipMultiplyPathGradientTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTranslatePathGradientTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float dx, float dy, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipScalePathGradientTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float sx, float sy, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipRotatePathGradientTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float angle, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathGradientFocusScales(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float[] xScale, float[] yScale);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPathGradientFocusScales(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float xScale, float yScale);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateImageAttributes(out IntPtr imageattr);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCloneImageAttributes(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, out IntPtr cloneImageattr);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDisposeImageAttributes(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetImageAttributesColorMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, [MarshalAs(UnmanagedType.Bool)] bool enableFlag,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(ColorMatrix.PinningMarshaller))]
#endif
            ColorMatrix? colorMatrix,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(ColorMatrix.PinningMarshaller))]
#endif
            ColorMatrix? grayMatrix, ColorMatrixFlag flags);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetImageAttributesThreshold(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, [MarshalAs(UnmanagedType.Bool)] bool enableFlag, float threshold);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetImageAttributesGamma(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, [MarshalAs(UnmanagedType.Bool)] bool enableFlag, float gamma);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetImageAttributesNoOp(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, [MarshalAs(UnmanagedType.Bool)] bool enableFlag);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetImageAttributesColorKeys(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, [MarshalAs(UnmanagedType.Bool)] bool enableFlag, int colorLow, int colorHigh);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetImageAttributesOutputChannel(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, [MarshalAs(UnmanagedType.Bool)] bool enableFlag, ColorChannelFlag flags);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipSetImageAttributesOutputChannelColorProfile(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, [MarshalAs(UnmanagedType.Bool)] bool enableFlag, string colorProfileFilename);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetImageAttributesRemapTable(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, ColorAdjustType type, [MarshalAs(UnmanagedType.Bool)] bool enableFlag, int mapSize, IntPtr map);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetImageAttributesWrapMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, int wrapmode, int argb, [MarshalAs(UnmanagedType.Bool)] bool clamp);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageAttributesAdjustedPalette(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattr, IntPtr palette, ColorAdjustType type);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageDecodersSize(out int numDecoders, out int size);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageDecoders(int numDecoders, int size, IntPtr decoders);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageEncodersSize(out int numEncoders, out int size);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageEncoders(int numEncoders, int size, IntPtr encoders);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateSolidFill(int color, out IntPtr brush);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetSolidFillColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int color);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetSolidFillColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int color);


            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateTexture(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap, int wrapmode, out IntPtr texture);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateTexture2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap, int wrapmode, float x, float y, float width, float height, out IntPtr texture);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateTextureIA(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageAttrib, float x, float y, float width, float height, out IntPtr texture);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateTexture2I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap, int wrapmode, int x, int y, int width, int height, out IntPtr texture);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateTextureIAI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageAttrib, int x, int y, int width, int height, out IntPtr texture);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetTextureTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetTextureTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipResetTextureTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush);

            [LibraryImport(LibraryName)]
            internal static partial int GdipMultiplyTextureTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTranslateTextureTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float dx, float dy, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipScaleTextureTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float sx, float sy, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipRotateTextureTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float angle, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetTextureWrapMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int wrapMode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetTextureWrapMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out int wrapMode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetTextureImage(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, out IntPtr image);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetFontCollectionFamilyCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontCollection, out int numFound);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetFontCollectionFamilyList(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontCollection, int numSought, IntPtr[] gpfamilies, out int numFound);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCloneFontFamily(IntPtr fontfamily, out IntPtr clonefontfamily);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipCreateFontFamilyFromName(string name,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontCollection, out IntPtr FontFamily);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetGenericFontFamilySansSerif(out IntPtr fontfamily);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetGenericFontFamilySerif(out IntPtr fontfamily);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetGenericFontFamilyMonospace(out IntPtr fontfamily);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeleteFontFamily(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontFamily);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipGetFamilyName(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef family, char* name, int language);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsStyleAvailable(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef family, FontStyle style, out int isStyleAvailable);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetEmHeight(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef family, FontStyle style, out int EmHeight);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetCellAscent(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef family, FontStyle style, out int CellAscent);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetCellDescent(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef family, FontStyle style, out int CellDescent);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetLineSpacing(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef family, FontStyle style, out int LineSpaceing);

            [LibraryImport(LibraryName)]
            internal static partial int GdipNewInstalledFontCollection(out IntPtr fontCollection);

            [LibraryImport(LibraryName)]
            internal static partial int GdipNewPrivateFontCollection(out IntPtr fontCollection);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeletePrivateFontCollection(ref IntPtr fontCollection);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipPrivateAddFontFile(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontCollection, string filename);

            [LibraryImport(LibraryName)]
            internal static partial int GdipPrivateAddMemoryFont(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontCollection, IntPtr memory, int length);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateFont(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontFamily, float emSize, FontStyle style, GraphicsUnit unit, out IntPtr font);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateFontFromDC(IntPtr hdc, ref IntPtr font);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCloneFont(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font, out IntPtr cloneFont);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeleteFont(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetFamily(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font, out IntPtr family);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetFontStyle(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font, out FontStyle style);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetFontSize(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font, out float size);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetFontHeight(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out float size);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetFontHeightGivenDPI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font, float dpi, out float size);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetFontUnit(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font, out GraphicsUnit unit);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetLogFontW(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef font,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, ref Interop.User32.LOGFONT lf);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreatePen1(int argb, float width, int unit, out IntPtr pen);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreatePen2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float width, int unit, out IntPtr pen);

            [LibraryImport(LibraryName)]
            internal static partial int GdipClonePen(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out IntPtr clonepen);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeletePen(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef Pen);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PenAlignment penAlign);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out PenAlignment penAlign);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenWidth(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float width);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenWidth(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float[] width);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenLineCap197819(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int startCap, int endCap, int dashCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenStartCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int startCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenEndCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int endCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenStartCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int startCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenEndCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int endCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenDashCap197819(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int dashCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenDashCap197819(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int dashCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenLineJoin(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int lineJoin);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenLineJoin(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int lineJoin);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenCustomStartCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenCustomStartCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out IntPtr customCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenCustomEndCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef customCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenCustomEndCap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out IntPtr customCap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenMiterLimit(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float miterLimit);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenMiterLimit(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float[] miterLimit);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipResetPenTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush);

            [LibraryImport(LibraryName)]
            internal static partial int GdipMultiplyPenTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTranslatePenTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float dx, float dy, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipScalePenTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float sx, float sy, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipRotatePenTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float angle, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int argb);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int argb);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenBrushFill(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenBrushFill(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out IntPtr brush);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenFillType(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int pentype);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenDashStyle(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int dashstyle);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenDashStyle(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int dashstyle);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenDashArray(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef memorydash, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenDashOffset(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float[] dashoffset);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenDashOffset(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float dashoffset);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenDashCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int dashcount);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenDashArray(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float[] memorydash, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenCompoundCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, out int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPenCompoundArray(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float[] array, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPenCompoundArray(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float[] array, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetWorldTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipResetWorldTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics);

            [LibraryImport(LibraryName)]
            internal static partial int GdipMultiplyWorldTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTranslateWorldTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float dx, float dy, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipScaleWorldTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float sx, float sy, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipRotateWorldTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float angle, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetWorldTransform(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetCompositingMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, CompositingMode compositingMode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetTextRenderingHint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, TextRenderingHint textRenderingHint);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetTextContrast(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int textContrast);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetInterpolationMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, InterpolationMode interpolationMode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetCompositingMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out CompositingMode compositingMode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetRenderingOrigin(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int x, int y);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetRenderingOrigin(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int x, out int y);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetCompositingQuality(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, CompositingQuality quality);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetCompositingQuality(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out CompositingQuality quality);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetSmoothingMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, SmoothingMode smoothingMode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetSmoothingMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out SmoothingMode smoothingMode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPixelOffsetMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, PixelOffsetMode pixelOffsetMode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPixelOffsetMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out PixelOffsetMode pixelOffsetMode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetTextRenderingHint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out TextRenderingHint textRenderingHint);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetTextContrast(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int textContrast);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetInterpolationMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out InterpolationMode interpolationMode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPageUnit(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out GraphicsUnit unit);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPageScale(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out float scale);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPageUnit(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, GraphicsUnit unit);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPageScale(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float scale);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetDpiX(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out float dpi);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetDpiY(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out float dpi);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateMatrix(out IntPtr matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateMatrix2(float m11, float m12, float m21, float m22, float dx, float dy, out IntPtr matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateMatrix3(ref RectangleF rect, PointF* dstplg, out IntPtr matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateMatrix3I(ref Rectangle rect, Point* dstplg, out IntPtr matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCloneMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, out IntPtr cloneMatrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeleteMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetMatrixElements(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, float m11, float m12, float m21, float m22, float dx, float dy);

            [LibraryImport(LibraryName)]
            internal static partial int GdipMultiplyMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix2, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTranslateMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, float offsetX, float offsetY, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipScaleMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, float scaleX, float scaleY, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipRotateMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, float angle, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipShearMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, float shearX, float shearY, MatrixOrder order);

            [LibraryImport(LibraryName)]
            internal static partial int GdipInvertMatrix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTransformMatrixPoints(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, PointF* pts, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTransformMatrixPointsI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, Point* pts, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipVectorTransformMatrixPoints(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, PointF* pts, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipVectorTransformMatrixPointsI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, Point* pts, int count);

            [LibraryImport(LibraryName)]
            internal static unsafe partial int GdipGetMatrixElements(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, float* m);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsMatrixInvertible(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, out int boolean);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsMatrixIdentity(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, out int boolean);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsMatrixEqual(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix2, out int boolean);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateRegion(out IntPtr region);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateRegionRect(ref RectangleF gprectf, out IntPtr region);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateRegionRectI(ref Rectangle gprect, out IntPtr region);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateRegionPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, out IntPtr region);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateRegionRgnData(byte[] rgndata, int size, out IntPtr region);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateRegionHrgn(IntPtr hRgn, out IntPtr region);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCloneRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, out IntPtr cloneregion);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeleteRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region);

            [LibraryImport(LibraryName, SetLastError = true)]
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

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetInfinite(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetEmpty(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCombineRegionRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, ref RectangleF gprectf, CombineMode mode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCombineRegionRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, ref Rectangle gprect, CombineMode mode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCombineRegionPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, CombineMode mode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCombineRegionRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region2, CombineMode mode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTranslateRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, float dx, float dy);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTranslateRegionI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, int dx, int dy);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTransformRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetRegionBounds(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out RectangleF gprectf);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetRegionHRgn(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out IntPtr hrgn);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsEmptyRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int boolean);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsInfiniteRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int boolean);

            [LibraryImport(LibraryName)]
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

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetRegionDataSize(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, out int bufferSize);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetRegionData(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, byte[] regionData, int bufferSize, out int sizeFilled);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsVisibleRegionPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, float X, float Y,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int boolean);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsVisibleRegionPointI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, int X, int Y,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int boolean);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsVisibleRegionRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, float X, float Y, float width, float height,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int boolean);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsVisibleRegionRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, int X, int Y, int width, int height,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int boolean);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetRegionScansCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, out int count,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetRegionScans(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, RectangleF* rects, out int count,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateFromHDC(IntPtr hdc, out IntPtr graphics);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetClipGraphics(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef srcgraphics, CombineMode mode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetClipRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float x, float y, float width, float height, CombineMode mode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetClipRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int x, int y, int width, int height, CombineMode mode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetClipPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, CombineMode mode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetClipRegion(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region, CombineMode mode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipResetClip(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTranslateClip(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float dx, float dy);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetClip(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef region);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetClipBounds(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out RectangleF rect);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsClipEmpty(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, [MarshalAs(UnmanagedType.Bool)] out bool result);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetVisibleClipBounds(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out RectangleF rect);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsVisibleClipEmpty(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, [MarshalAs(UnmanagedType.Bool)] out bool result);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsVisiblePoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float x, float y, [MarshalAs(UnmanagedType.Bool)] out bool result);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsVisiblePointI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int x, int y, [MarshalAs(UnmanagedType.Bool)] out bool result);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsVisibleRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, float x, float y, float width, float height, [MarshalAs(UnmanagedType.Bool)] out bool result);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsVisibleRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int x, int y, int width, int height, [MarshalAs(UnmanagedType.Bool)] out bool result);

            [LibraryImport(LibraryName)]
            internal static partial int GdipFlush(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, FlushIntention intention);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetDC(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out IntPtr hdc);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetStringFormatMeasurableCharacterRanges(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, int rangeCount, CharacterRange[] range);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateStringFormat(StringFormatFlags options, int language, out IntPtr format);

            [LibraryImport(LibraryName)]
            internal static partial int GdipStringFormatGetGenericDefault(out IntPtr format);

            [LibraryImport(LibraryName)]
            internal static partial int GdipStringFormatGetGenericTypographic(out IntPtr format);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeleteStringFormat(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCloneStringFormat(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out IntPtr newFormat);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetStringFormatFlags(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, StringFormatFlags options);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetStringFormatFlags(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out StringFormatFlags result);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetStringFormatAlign(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, StringAlignment align);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetStringFormatAlign(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out StringAlignment align);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetStringFormatLineAlign(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, StringAlignment align);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetStringFormatLineAlign(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out StringAlignment align);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetStringFormatHotkeyPrefix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, HotkeyPrefix hotkeyPrefix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetStringFormatHotkeyPrefix(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out HotkeyPrefix hotkeyPrefix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetStringFormatTabStops(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, float firstTabOffset, int count, float[] tabStops);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetStringFormatTabStops(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, int count, out float firstTabOffset, float[] tabStops);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetStringFormatTabStopCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetStringFormatMeasurableCharacterRangeCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetStringFormatTrimming(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, StringTrimming trimming);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetStringFormatTrimming(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out StringTrimming trimming);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetStringFormatDigitSubstitution(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, int langID, StringDigitSubstitute sds);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetStringFormatDigitSubstitution(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format, out int langID, out StringDigitSubstitute sds);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageDimension(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out float width, out float height);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageWidth(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out int width);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageHeight(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out int height);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageHorizontalResolution(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out float horzRes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageVerticalResolution(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out float vertRes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageFlags(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out int flags);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageRawFormat(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, ref Guid format);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImagePixelFormat(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out PixelFormat format);

            [LibraryImport(LibraryName)]
            internal static partial int GdipImageGetFrameCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, ref Guid dimensionID, out int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipImageSelectActiveFrame(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, ref Guid dimensionID, int frameIndex);

            [LibraryImport(LibraryName)]
            internal static partial int GdipImageRotateFlip(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int rotateFlipType);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetAllPropertyItems(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, uint totalBufferSize, uint numProperties, PropertyItemInternal* allItems);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPropertyCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out uint numOfProperty);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPropertyIdList(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, uint numOfProperty, int* list);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPropertyItem(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int propid, uint propSize, PropertyItemInternal* buffer);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPropertyItemSize(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int propid, out uint size);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPropertySize(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out uint totalBufferSize, out uint numProperties);

            [LibraryImport(LibraryName)]
            internal static partial int GdipRemovePropertyItem(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int propid);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPropertyItem(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, PropertyItemInternal* item);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageType(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out int type);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageType(IntPtr image, out int type);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDisposeImage(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDisposeImage(IntPtr image);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipCreateBitmapFromFile(string filename, out IntPtr bitmap);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipCreateBitmapFromFileICM(string filename, out IntPtr bitmap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromScan0(int width, int height, int stride, int format, IntPtr scan0, out IntPtr bitmap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromGraphics(int width, int height,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out IntPtr bitmap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromHBITMAP(IntPtr hbitmap, IntPtr hpalette, out IntPtr bitmap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromHICON(IntPtr hicon, out IntPtr bitmap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromResource(IntPtr hresource, IntPtr name, out IntPtr bitmap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateHBITMAPFromBitmap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef nativeBitmap, out IntPtr hbitmap, int argbBackground);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateHICONFromBitmap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef nativeBitmap, out IntPtr hicon);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCloneBitmapArea(float x, float y, float width, float height, int format,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef srcbitmap, out IntPtr dstbitmap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCloneBitmapAreaI(int x, int y, int width, int height, int format,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef srcbitmap, out IntPtr dstbitmap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipBitmapLockBits(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap, ref Rectangle rect, ImageLockMode flags, PixelFormat format,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(BitmapData.PinningMarshaller))]
#endif
            BitmapData lockedBitmapData);

            [LibraryImport(LibraryName)]
            internal static partial int GdipBitmapUnlockBits(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(BitmapData.PinningMarshaller))]
#endif
            BitmapData lockedBitmapData);

            [LibraryImport(LibraryName)]
            internal static partial int GdipBitmapGetPixel(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap, int x, int y, out int argb);

            [LibraryImport(LibraryName)]
            internal static partial int GdipBitmapSetPixel(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap, int x, int y, int argb);

            [LibraryImport(LibraryName)]
            internal static partial int GdipBitmapSetResolution(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef bitmap, float dpix, float dpiy);

            [LibraryImport(LibraryName)]
            internal static partial int GdipImageGetFrameDimensionsCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipImageGetFrameDimensionsList(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, Guid* dimensionIDs, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateMetafileFromEmf(IntPtr hEnhMetafile, [MarshalAs(UnmanagedType.Bool)] bool deleteEmf, out IntPtr metafile);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateMetafileFromWmf(IntPtr hMetafile, [MarshalAs(UnmanagedType.Bool)] bool deleteWmf,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(WmfPlaceableFileHeader.PinningMarshaller))]
#endif
                WmfPlaceableFileHeader wmfplacealbeHeader, out IntPtr metafile);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipCreateMetafileFromFile(string file, out IntPtr metafile);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipRecordMetafile(IntPtr referenceHdc, EmfType emfType, IntPtr pframeRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipRecordMetafile(IntPtr referenceHdc, EmfType emfType, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipRecordMetafileI(IntPtr referenceHdc, EmfType emfType, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipRecordMetafileFileName(string fileName, IntPtr referenceHdc, EmfType emfType, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipRecordMetafileFileName(string fileName, IntPtr referenceHdc, EmfType emfType, IntPtr pframeRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipRecordMetafileFileNameI(string fileName, IntPtr referenceHdc, EmfType emfType, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile);

            [LibraryImport(LibraryName)]
            internal static partial int GdipPlayMetafileRecord(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, EmfPlusRecordType recordType, int flags, int dataSize, byte[] data);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSaveGraphics(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, out int state);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawArc(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawArcI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawLinesI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawBezier(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawEllipse(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float x, float y, float width, float height);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawEllipseI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int x, int y, int width, int height);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawLine(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float x1, float y1, float x2, float y2);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawLineI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int x1, int y1, int x2, int y2);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawLines(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
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

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawPie(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawPieI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawPolygon(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawPolygonI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillEllipse(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float x, float y, float width, float height);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillEllipseI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int x, int y, int width, int height);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillPolygon(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, PointF* points, int count, FillMode brushMode);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillPolygonI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, Point* points, int count, FillMode brushMode);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillRectangle(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float x, float y, float width, float height);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillRectangleI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int x, int y, int width, int height);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillRectangles(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, RectangleF* rects, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillRectanglesI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, Rectangle* rects, int count);

            [LibraryImport(LibraryName,  SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
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

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImageRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int x, int y, int width, int height);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGraphicsClear(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int argb);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawClosedCurve(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawClosedCurveI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawClosedCurve2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count, float tension);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawClosedCurve2I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count, float tension);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawCurve(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawCurveI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawCurve2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count, float tension);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawCurve2I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count, float tension);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawCurve3(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count, int offset, int numberOfSegments, float tension);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawCurve3I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count, int offset, int numberOfSegments, float tension);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillClosedCurve(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, PointF* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillClosedCurveI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, Point* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillClosedCurve2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, PointF* points, int count, float tension, FillMode mode);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillClosedCurve2I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, Point* points, int count, float tension, FillMode mode);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillPie(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillPieI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
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

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
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

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImageI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int x, int y);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImage(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, float x, float y);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImagePoints(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, PointF* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImagePointsI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, Point* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
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
            HandleRef imageAttributes,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.DrawImageAbortMarshaller))]
#endif
            Graphics.DrawImageAbort? callback,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef callbackdata);

            [LibraryImport(LibraryName, SetLastError = true)]
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
            HandleRef imageAttributes,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.DrawImageAbortMarshaller))]
#endif
            Graphics.DrawImageAbort? callback,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef callbackdata);

            [LibraryImport(LibraryName, SetLastError = true)]
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
            HandleRef imageAttributes,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.DrawImageAbortMarshaller))]
#endif
            Graphics.DrawImageAbort? callback,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef callbackdata);

            [LibraryImport(LibraryName, SetLastError = true)]
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
            HandleRef imageAttributes,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.DrawImageAbortMarshaller))]
#endif
            Graphics.DrawImageAbort? callback,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef callbackdata);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImageRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, float x, float y, float width, float height);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImagePointRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, float x, float y, float srcx, float srcy, float srcwidth, float srcheight, int srcunit);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawImagePointRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int x, int y, int srcx, int srcy, int srcwidth, int srcheight, int srcunit);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawRectangle(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, float x, float y, float width, float height);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawRectangleI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, int x, int y, int width, int height);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawRectangles(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, RectangleF* rects, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawRectanglesI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Rectangle* rects, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTransformPoints(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int destSpace, int srcSpace, PointF* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTransformPointsI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int destSpace, int srcSpace, Point* points, int count);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipLoadImageFromFileICM(string filename, out IntPtr image);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipLoadImageFromFile(string filename, out IntPtr image);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetEncoderParameterListSize(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, ref Guid encoder, out int size);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetEncoderParameterList(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, ref Guid encoder, int size, IntPtr buffer);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreatePath(int brushMode, out IntPtr path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreatePath2(PointF* points, byte* types, int count, int brushMode, out IntPtr path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreatePath2I(Point* points, byte* types, int count, int brushMode, out IntPtr path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipClonePath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, out IntPtr clonepath);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeletePath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipResetPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPointCount(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, out int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathTypes(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, byte[] types, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathPoints(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathFillMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, out FillMode fillmode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPathFillMode(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, FillMode fillmode);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathData(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, GpPathData* pathData);

            [LibraryImport(LibraryName)]
            internal static partial int GdipStartPathFigure(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipClosePathFigure(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipClosePathFigures(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetPathMarker(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipClearPathMarkers(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipReversePath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathLastPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, out PointF lastPoint);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathLine(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x1, float y1, float x2, float y2);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathLine2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathArc(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathBezier(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathBeziers(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathCurve(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathCurve2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count, float tension);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathCurve3(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count, int offset, int numberOfSegments, float tension);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathClosedCurve(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathClosedCurve2(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count, float tension);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathRectangle(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x, float y, float width, float height);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathRectangles(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, RectangleF* rects, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathEllipse(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x, float y, float width, float height);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathPie(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x, float y, float width, float height, float startAngle, float sweepAngle);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathPolygon(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, PointF* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef addingPath, [MarshalAs(UnmanagedType.Bool)] bool connect);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipAddPathString(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, string s, int length,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontFamily, int style, float emSize, ref RectangleF layoutRect,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipAddPathStringI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, string s, int length,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef fontFamily, int style, float emSize, ref Rectangle layoutRect,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef format);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathLineI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x1, int y1, int x2, int y2);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathLine2I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathArcI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathBezierI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathBeziersI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathCurveI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathCurve2I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count, float tension);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathCurve3I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count, int offset, int numberOfSegments, float tension);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathClosedCurveI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathClosedCurve2I(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count, float tension);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathRectangleI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x, int y, int width, int height);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathRectanglesI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Rectangle* rects, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathEllipseI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x, int y, int width, int height);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathPieI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x, int y, int width, int height, float startAngle, float sweepAngle);

            [LibraryImport(LibraryName)]
            internal static partial int GdipAddPathPolygonI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, Point* points, int count);

            [LibraryImport(LibraryName)]
            internal static partial int GdipFlattenPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrixfloat, float flatness);

            [LibraryImport(LibraryName)]
            internal static partial int GdipWidenPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, float flatness);

            [LibraryImport(LibraryName)]
            internal static partial int GdipWarpPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix, PointF* points, int count, float srcX, float srcY, float srcWidth, float srcHeight, WarpMode warpMode, float flatness);

            [LibraryImport(LibraryName)]
            internal static partial int GdipTransformPath(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetPathWorldBounds(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, out RectangleF gprectf,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef matrix,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsVisiblePathPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x, float y,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, [MarshalAs(UnmanagedType.Bool)] out bool result);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsVisiblePathPointI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x, int y,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, [MarshalAs(UnmanagedType.Bool)] out bool result);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsOutlineVisiblePathPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, float x, float y,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, [MarshalAs(UnmanagedType.Bool)] out bool result);

            [LibraryImport(LibraryName)]
            internal static partial int GdipIsOutlineVisiblePathPointI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef path, int x, int y,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, [MarshalAs(UnmanagedType.Bool)] out bool result);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeleteBrush(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef brush);

            [LibraryImport(LibraryName)]
            internal static partial int GdipLoadImageFromStream(IntPtr stream, IntPtr* image);

            [LibraryImport(LibraryName)]
            internal static partial int GdipLoadImageFromStreamICM(IntPtr stream, IntPtr* image);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCloneImage(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out IntPtr cloneimage);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipSaveImageToFile(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, string filename, ref Guid classId,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef encoderParams);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSaveImageToStream(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, IntPtr stream, Guid* classId,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef encoderParams);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSaveAdd(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef encoderParams);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSaveAddImage(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef newImage,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef encoderParams);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageGraphicsContext(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out IntPtr graphics);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageBounds(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out RectangleF gprectf, out GraphicsUnit unit);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImageThumbnail(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, int thumbWidth, int thumbHeight, out IntPtr thumbImage,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Image.GetThumbnailImageAbortMarshaller))]
#endif
            Image.GetThumbnailImageAbort? callback, IntPtr callbackdata);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImagePalette(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, IntPtr palette, int size);

            [LibraryImport(LibraryName)]
            internal static partial int GdipSetImagePalette(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, IntPtr palette);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetImagePaletteSize(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef image, out int size);

            [LibraryImport(LibraryName)]
            internal static partial int GdipImageForceValidation(IntPtr image);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateFromHDC2(IntPtr hdc, IntPtr hdevice, out IntPtr graphics);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateFromHWND(IntPtr hwnd, out IntPtr graphics);

            [LibraryImport(LibraryName)]
            internal static partial int GdipDeleteGraphics(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics);

            [LibraryImport(LibraryName)]
            internal static partial int GdipReleaseDC(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, IntPtr hdc);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetNearestColor(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, ref int color);

            [LibraryImport(LibraryName)]
            internal static partial IntPtr GdipCreateHalftonePalette();

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawBeziers(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, PointF* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipDrawBeziersI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef pen, Point* points, int count);

            [LibraryImport(LibraryName, SetLastError = true)]
            internal static partial int GdipFillPath(
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
            HandleRef path);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileDestPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref PointF destPoint,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.EnumerateMetafileProcMarshaller))]
#endif
            Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileDestPointI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref Point destPoint,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.EnumerateMetafileProcMarshaller))]
#endif
            Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileDestRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref RectangleF destRect,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.EnumerateMetafileProcMarshaller))]
#endif
            Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileDestRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref Rectangle destRect,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.EnumerateMetafileProcMarshaller))]
#endif
            Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileDestPoints(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, PointF* destPoints, int count,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.EnumerateMetafileProcMarshaller))]
#endif
            Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileDestPointsI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, Point* destPoints, int count,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.EnumerateMetafileProcMarshaller))]
#endif
            Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileSrcRectDestPoint(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref PointF destPoint, ref RectangleF srcRect, GraphicsUnit pageUnit,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.EnumerateMetafileProcMarshaller))]
#endif
            Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileSrcRectDestPointI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref Point destPoint, ref Rectangle srcRect, GraphicsUnit pageUnit,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.EnumerateMetafileProcMarshaller))]
#endif
            Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileSrcRectDestRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref RectangleF destRect, ref RectangleF srcRect, GraphicsUnit pageUnit,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.EnumerateMetafileProcMarshaller))]
#endif
            Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileSrcRectDestRectI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, ref Rectangle destRect, ref Rectangle srcRect, GraphicsUnit pageUnit,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.EnumerateMetafileProcMarshaller))]
#endif
            Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileSrcRectDestPoints(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, PointF* destPoints, int count, ref RectangleF srcRect, GraphicsUnit pageUnit,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.EnumerateMetafileProcMarshaller))]
#endif
            Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipEnumerateMetafileSrcRectDestPointsI(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, Point* destPoints, int count, ref Rectangle srcRect, GraphicsUnit pageUnit,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(Graphics.EnumerateMetafileProcMarshaller))]
#endif
            Graphics.EnumerateMetafileProc callback, IntPtr callbackdata,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef imageattributes);

            [LibraryImport(LibraryName)]
            internal static partial int GdipRestoreGraphics(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int state);

            [LibraryImport(LibraryName, EntryPoint = "GdipGetMetafileHeaderFromWmf")]
            private static partial int GdipGetMetafileHeaderFromWmf_Internal(IntPtr hMetafile,
#if NET7_0_OR_GREATER
                [MarshalUsing(typeof(WmfPlaceableFileHeader.PinningMarshaller))]
#endif
                WmfPlaceableFileHeader wmfplaceable,
#if NET7_0_OR_GREATER
                [MarshalUsing(typeof(MetafileHeaderWmf.Marshaller))]
                ref MetafileHeaderWmf metafileHeaderWmf
#else
                MetafileHeaderWmf metafileHeaderWmf
#endif
                );

            internal static int GdipGetMetafileHeaderFromWmf(IntPtr hMetafile,
                WmfPlaceableFileHeader wmfplaceable,
                MetafileHeaderWmf metafileHeaderWmf
                )
            {
                return GdipGetMetafileHeaderFromWmf_Internal(hMetafile,
                    wmfplaceable,
#if NET7_0_OR_GREATER
                    ref
#endif
                    metafileHeaderWmf
                    );
            }

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetMetafileHeaderFromEmf(IntPtr hEnhMetafile, MetafileHeaderEmf metafileHeaderEmf);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipGetMetafileHeaderFromFile(string filename, IntPtr header);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetMetafileHeaderFromStream(IntPtr stream, IntPtr header);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetMetafileHeaderFromMetafile(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, IntPtr header);

            [LibraryImport(LibraryName)]
            internal static partial int GdipGetHemfFromMetafile(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef metafile, out IntPtr hEnhMetafile);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateMetafileFromStream(IntPtr stream, IntPtr* metafile);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipRecordMetafileStream(IntPtr stream, IntPtr referenceHdc, EmfType emfType, RectangleF* frameRect, MetafileFrameUnit frameUnit, string? description, IntPtr* metafile);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipRecordMetafileStream(IntPtr stream, IntPtr referenceHdc, EmfType emfType, IntPtr pframeRect, MetafileFrameUnit frameUnit, string? description, IntPtr* metafile);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipRecordMetafileStreamI(IntPtr stream, IntPtr referenceHdc, EmfType emfType, Rectangle* frameRect, MetafileFrameUnit frameUnit, string? description, IntPtr* metafile);

            [LibraryImport(LibraryName)]
            internal static partial int GdipComment(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef graphics, int sizeData, byte[] data);

            [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf16)]
            internal static partial int GdipCreateFontFromLogfontW(IntPtr hdc, ref Interop.User32.LOGFONT lf, out IntPtr font);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromStream(IntPtr stream, IntPtr* bitmap);

            [LibraryImport(LibraryName)]
            internal static partial int GdipCreateBitmapFromStreamICM(IntPtr stream, IntPtr* bitmap);

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
