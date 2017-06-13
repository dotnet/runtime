// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /*
     * EmfPlusRecordType constants
     */

    /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the methods available in a metafile to read and write graphic
    ///       commands.
    ///    </para>
    /// </devdoc>    
    public enum EmfPlusRecordType
    {
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfRecordBase"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfRecordBase = 0x00010000,

        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetBkColor"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetBkColor = WmfRecordBase | 0x201,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetBkMode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetBkMode = WmfRecordBase | 0x102,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetMapMode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetMapMode = WmfRecordBase | 0x103,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetROP2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetROP2 = WmfRecordBase | 0x104,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetRelAbs"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetRelAbs = WmfRecordBase | 0x105,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetPolyFillMode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetPolyFillMode = WmfRecordBase | 0x106,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetStretchBltMode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetStretchBltMode = WmfRecordBase | 0x107,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetTextCharExtra"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetTextCharExtra = WmfRecordBase | 0x108,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetTextColor"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetTextColor = WmfRecordBase | 0x209,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetTextJustification"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetTextJustification = WmfRecordBase | 0x20A,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetWindowOrg"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetWindowOrg = WmfRecordBase | 0x20B,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetWindowExt"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetWindowExt = WmfRecordBase | 0x20C,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetViewportOrg"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetViewportOrg = WmfRecordBase | 0x20D,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetViewportExt"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetViewportExt = WmfRecordBase | 0x20E,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfOffsetWindowOrg"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfOffsetWindowOrg = WmfRecordBase | 0x20F,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfScaleWindowExt"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfScaleWindowExt = WmfRecordBase | 0x410,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfOffsetViewportOrg"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfOffsetViewportOrg = WmfRecordBase | 0x211,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfScaleViewportExt"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfScaleViewportExt = WmfRecordBase | 0x412,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfLineTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfLineTo = WmfRecordBase | 0x213,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfMoveTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfMoveTo = WmfRecordBase | 0x214,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfExcludeClipRect"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfExcludeClipRect = WmfRecordBase | 0x415,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfIntersectClipRect"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfIntersectClipRect = WmfRecordBase | 0x416,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfArc"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfArc = WmfRecordBase | 0x817,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfEllipse"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfEllipse = WmfRecordBase | 0x418,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfFloodFill"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfFloodFill = WmfRecordBase | 0x419,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfPie"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfPie = WmfRecordBase | 0x81A,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfRectangle"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfRectangle = WmfRecordBase | 0x41B,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfRoundRect"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfRoundRect = WmfRecordBase | 0x61C,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfPatBlt"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfPatBlt = WmfRecordBase | 0x61D,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSaveDC"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSaveDC = WmfRecordBase | 0x01E,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetPixel"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetPixel = WmfRecordBase | 0x41F,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfOffsetCilpRgn"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfOffsetCilpRgn = WmfRecordBase | 0x220,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfTextOut"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfTextOut = WmfRecordBase | 0x521,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfBitBlt"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfBitBlt = WmfRecordBase | 0x922,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfStretchBlt"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfStretchBlt = WmfRecordBase | 0xB23,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfPolygon"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfPolygon = WmfRecordBase | 0x324,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfPolyline"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfPolyline = WmfRecordBase | 0x325,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfEscape"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfEscape = WmfRecordBase | 0x626,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfRestoreDC"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfRestoreDC = WmfRecordBase | 0x127,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfFillRegion"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfFillRegion = WmfRecordBase | 0x228,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfFrameRegion"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfFrameRegion = WmfRecordBase | 0x429,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfInvertRegion"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfInvertRegion = WmfRecordBase | 0x12A,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfPaintRegion"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfPaintRegion = WmfRecordBase | 0x12B,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSelectClipRegion"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSelectClipRegion = WmfRecordBase | 0x12C,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSelectObject"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSelectObject = WmfRecordBase | 0x12D,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetTextAlign"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetTextAlign = WmfRecordBase | 0x12E,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfChord"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfChord = WmfRecordBase | 0x830,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetMapperFlags"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetMapperFlags = WmfRecordBase | 0x231,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfExtTextOut"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfExtTextOut = WmfRecordBase | 0xA32,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetDibToDev"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetDibToDev = WmfRecordBase | 0xD33,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSelectPalette"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSelectPalette = WmfRecordBase | 0x234,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfRealizePalette"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfRealizePalette = WmfRecordBase | 0x035,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfAnimatePalette"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfAnimatePalette = WmfRecordBase | 0x436,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetPalEntries"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetPalEntries = WmfRecordBase | 0x037,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfPolyPolygon"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfPolyPolygon = WmfRecordBase | 0x538,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfResizePalette"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfResizePalette = WmfRecordBase | 0x139,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfDibBitBlt"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfDibBitBlt = WmfRecordBase | 0x940,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfDibStretchBlt"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfDibStretchBlt = WmfRecordBase | 0xb41,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfDibCreatePatternBrush"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfDibCreatePatternBrush = WmfRecordBase | 0x142,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfStretchDib"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfStretchDib = WmfRecordBase | 0xf43,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfExtFloodFill"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfExtFloodFill = WmfRecordBase | 0x548,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfSetLayout"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfSetLayout = WmfRecordBase | 0x149, // META_SETLAYOUT
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfDeleteObject"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfDeleteObject = WmfRecordBase | 0x1f0,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfCreatePalette"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfCreatePalette = WmfRecordBase | 0x0f7,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfCreatePatternBrush"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfCreatePatternBrush = WmfRecordBase | 0x1f9,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfCreatePenIndirect"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfCreatePenIndirect = WmfRecordBase | 0x2fa,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfCreateFontIndirect"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfCreateFontIndirect = WmfRecordBase | 0x2fb,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfCreateBrushIndirect"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfCreateBrushIndirect = WmfRecordBase | 0x2fc,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.WmfCreateRegion"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WmfCreateRegion = WmfRecordBase | 0x6ff,

        // Since we have to enumerate GDI records right along with GDI+ records,
        // we list all the GDI records here so that they can be part of the
        // same enumeration type which is used in the enumeration callback.

        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfHeader"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfHeader = 1,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolyBezier"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolyBezier = 2,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolygon"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolygon = 3,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolyline"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolyline = 4,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolyBezierTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolyBezierTo = 5,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolyLineTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolyLineTo = 6,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolyPolyline"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolyPolyline = 7,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolyPolygon"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolyPolygon = 8,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetWindowExtEx"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetWindowExtEx = 9,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetWindowOrgEx"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetWindowOrgEx = 10,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetViewportExtEx"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetViewportExtEx = 11,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetViewportOrgEx"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetViewportOrgEx = 12,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetBrushOrgEx"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetBrushOrgEx = 13,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfEof"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfEof = 14,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetPixelV"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetPixelV = 15,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetMapperFlags"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetMapperFlags = 16,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetMapMode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetMapMode = 17,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetBkMode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetBkMode = 18,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetPolyFillMode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetPolyFillMode = 19,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetROP2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetROP2 = 20,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetStretchBltMode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetStretchBltMode = 21,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetTextAlign"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetTextAlign = 22,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetColorAdjustment"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetColorAdjustment = 23,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetTextColor"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetTextColor = 24,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetBkColor"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetBkColor = 25,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfOffsetClipRgn"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfOffsetClipRgn = 26,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfMoveToEx"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfMoveToEx = 27,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetMetaRgn"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetMetaRgn = 28,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfExcludeClipRect"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfExcludeClipRect = 29,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfIntersectClipRect"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfIntersectClipRect = 30,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfScaleViewportExtEx"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfScaleViewportExtEx = 31,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfScaleWindowExtEx"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfScaleWindowExtEx = 32,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSaveDC"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSaveDC = 33,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfRestoreDC"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfRestoreDC = 34,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetWorldTransform"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetWorldTransform = 35,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfModifyWorldTransform"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfModifyWorldTransform = 36,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSelectObject"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSelectObject = 37,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfCreatePen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfCreatePen = 38,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfCreateBrushIndirect"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfCreateBrushIndirect = 39,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfDeleteObject"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfDeleteObject = 40,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfAngleArc"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfAngleArc = 41,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfEllipse"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfEllipse = 42,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfRectangle"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfRectangle = 43,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfRoundRect"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfRoundRect = 44,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfRoundArc"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfRoundArc = 45,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfChord"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfChord = 46,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPie"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPie = 47,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSelectPalette"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSelectPalette = 48,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfCreatePalette"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfCreatePalette = 49,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetPaletteEntries"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetPaletteEntries = 50,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfResizePalette"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfResizePalette = 51,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfRealizePalette"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfRealizePalette = 52,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfExtFloodFill"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfExtFloodFill = 53,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfLineTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfLineTo = 54,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfArcTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfArcTo = 55,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolyDraw"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolyDraw = 56,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetArcDirection"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetArcDirection = 57,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetMiterLimit"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetMiterLimit = 58,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfBeginPath"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfBeginPath = 59,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfEndPath"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfEndPath = 60,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfCloseFigure"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfCloseFigure = 61,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfFillPath"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfFillPath = 62,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfStrokeAndFillPath"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfStrokeAndFillPath = 63,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfStrokePath"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfStrokePath = 64,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfFlattenPath"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfFlattenPath = 65,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfWidenPath"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfWidenPath = 66,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSelectClipPath"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSelectClipPath = 67,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfAbortPath"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfAbortPath = 68,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfReserved069"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfReserved069 = 69,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfGdiComment"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfGdiComment = 70,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfFillRgn"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfFillRgn = 71,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfFrameRgn"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfFrameRgn = 72,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfInvertRgn"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfInvertRgn = 73,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPaintRgn"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPaintRgn = 74,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfExtSelectClipRgn"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfExtSelectClipRgn = 75,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfBitBlt"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfBitBlt = 76,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfStretchBlt"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfStretchBlt = 77,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfMaskBlt"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfMaskBlt = 78,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPlgBlt"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPlgBlt = 79,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetDIBitsToDevice"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetDIBitsToDevice = 80,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfStretchDIBits"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfStretchDIBits = 81,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfExtCreateFontIndirect"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfExtCreateFontIndirect = 82,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfExtTextOutA"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfExtTextOutA = 83,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfExtTextOutW"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfExtTextOutW = 84,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolyBezier16"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolyBezier16 = 85,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolygon16"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolygon16 = 86,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolyline16"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolyline16 = 87,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolyBezierTo16"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolyBezierTo16 = 88,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolylineTo16"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolylineTo16 = 89,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolyPolyline16"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolyPolyline16 = 90,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolyPolygon16"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolyPolygon16 = 91,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolyDraw16"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolyDraw16 = 92,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfCreateMonoBrush"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfCreateMonoBrush = 93,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfCreateDibPatternBrushPt"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfCreateDibPatternBrushPt = 94,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfExtCreatePen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfExtCreatePen = 95,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolyTextOutA"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolyTextOutA = 96,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPolyTextOutW"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPolyTextOutW = 97,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetIcmMode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetIcmMode = 98,  // EMR_SETICMMODE,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfCreateColorSpace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfCreateColorSpace = 99,  // EMR_CREATECOLORSPACE,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetColorSpace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetColorSpace = 100, // EMR_SETCOLORSPACE,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfDeleteColorSpace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfDeleteColorSpace = 101, // EMR_DELETECOLORSPACE,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfGlsRecord"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfGlsRecord = 102, // EMR_GLSRECORD,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfGlsBoundedRecord"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfGlsBoundedRecord = 103, // EMR_GLSBOUNDEDRECORD,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPixelFormat"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPixelFormat = 104, // EMR_PIXELFORMAT,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfDrawEscape"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfDrawEscape = 105, // EMR_RESERVED_105,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfExtEscape"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfExtEscape = 106, // EMR_RESERVED_106,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfStartDoc"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfStartDoc = 107, // EMR_RESERVED_107,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSmallTextOut"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSmallTextOut = 108, // EMR_RESERVED_108,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfForceUfiMapping"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfForceUfiMapping = 109, // EMR_RESERVED_109,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfNamedEscpae"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfNamedEscpae = 110, // EMR_RESERVED_110,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfColorCorrectPalette"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfColorCorrectPalette = 111, // EMR_COLORCORRECTPALETTE,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetIcmProfileA"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetIcmProfileA = 112, // EMR_SETICMPROFILEA,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetIcmProfileW"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetIcmProfileW = 113, // EMR_SETICMPROFILEW,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfAlphaBlend"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfAlphaBlend = 114, // EMR_ALPHABLEND,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetLayout"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetLayout = 115, // EMR_SETLAYOUT,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfTransparentBlt"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfTransparentBlt = 116, // EMR_TRANSPARENTBLT,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfReserved117"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfReserved117 = 117,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfGradientFill"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfGradientFill = 118, // EMR_GRADIENTFILL,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetLinkedUfis"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetLinkedUfis = 119, // EMR_RESERVED_119,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfSetTextJustification"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfSetTextJustification = 120, // EMR_RESERVED_120,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfColorMatchToTargetW"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfColorMatchToTargetW = 121, // EMR_COLORMATCHTOTARGETW,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfCreateColorSpaceW"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfCreateColorSpaceW = 122, // EMR_CREATECOLORSPACEW,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfMax"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfMax = 122,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfMin"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfMin = 1,

        // That is the END of the GDI EMF records.

        // Now we start the list of EMF+ records.  We leave quite
        // a bit of room here for the addition of any new GDI
        // records that may be added later.

        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EmfPlusRecordBase"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EmfPlusRecordBase = 0x00004000,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.Invalid"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Invalid = EmfPlusRecordBase,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.Header"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Header,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EndOfFile"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EndOfFile,

        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.Comment"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Comment,

        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.GetDC"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        GetDC,    // the application grabbed the metafile dc

        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.MultiFormatStart"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MultiFormatStart,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.MultiFormatSection"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MultiFormatSection,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.MultiFormatEnd"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MultiFormatEnd,

        // For all Persistent Objects
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.Object"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Object,
        // Drawing Records
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.Clear"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Clear,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.FillRects"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        FillRects,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.DrawRects"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DrawRects,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.FillPolygon"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        FillPolygon,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.DrawLines"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DrawLines,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.FillEllipse"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        FillEllipse,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.DrawEllipse"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DrawEllipse,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.FillPie"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        FillPie,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.DrawPie"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DrawPie,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.DrawArc"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DrawArc,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.FillRegion"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        FillRegion,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.FillPath"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        FillPath,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.DrawPath"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DrawPath,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.FillClosedCurve"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        FillClosedCurve,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.DrawClosedCurve"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DrawClosedCurve,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.DrawCurve"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DrawCurve,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.DrawBeziers"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DrawBeziers,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.DrawImage"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DrawImage,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.DrawImagePoints"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DrawImagePoints,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.DrawString"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DrawString,

        // Graphics State Records
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.SetRenderingOrigin"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SetRenderingOrigin,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.SetAntiAliasMode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SetAntiAliasMode,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.SetTextRenderingHint"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SetTextRenderingHint,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.SetTextContrast"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SetTextContrast,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.SetInterpolationMode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SetInterpolationMode,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.SetPixelOffsetMode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SetPixelOffsetMode,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.SetCompositingMode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SetCompositingMode,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.SetCompositingQuality"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SetCompositingQuality,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.Save"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Save,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.Restore"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Restore,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.BeginContainer"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        BeginContainer,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.BeginContainerNoParams"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        BeginContainerNoParams,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.EndContainer"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EndContainer,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.SetWorldTransform"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SetWorldTransform,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.ResetWorldTransform"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ResetWorldTransform,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.MultiplyWorldTransform"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MultiplyWorldTransform,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.TranslateWorldTransform"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        TranslateWorldTransform,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.ScaleWorldTransform"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ScaleWorldTransform,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.RotateWorldTransform"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        RotateWorldTransform,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.SetPageTransform"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SetPageTransform,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.ResetClip"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ResetClip,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.SetClipRect"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SetClipRect,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.SetClipPath"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SetClipPath,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.SetClipRegion"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SetClipRegion,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.OffsetClip"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        OffsetClip,

        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.DrawDriverString"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DrawDriverString,

        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.Total"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Total,

        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.Max"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Max = Total - 1,
        /// <include file='doc\EmfPlusRecordType.uex' path='docs/doc[@for="EmfPlusRecordType.Min"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Min = Header
    }
}
