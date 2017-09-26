// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Drawing
{
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public  enum KnownColor
    {
        // This enum is order dependant!!!
        //
        // The value of these known colors are indexes into a color array.
        // Do not modify this enum without updating KnownColorTable.
        //

        // 0 - reserved for "not a known color"
        // "System" colors
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.ActiveBorder"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ActiveBorder = 1,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.ActiveCaption"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ActiveCaption,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.ActiveCaptionText"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ActiveCaptionText,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.AppWorkspace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        AppWorkspace,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Control"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Control,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.ControlDark"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ControlDark,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.ControlDarkDark"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ControlDarkDark,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.ControlLight"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ControlLight,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.ControlLightLight"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ControlLightLight,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.ControlText"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ControlText,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Desktop"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Desktop,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.GrayText"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        GrayText,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Highlight"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Highlight,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.HighlightText"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        HighlightText,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.HotTrack"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        HotTrack,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.InactiveBorder"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        InactiveBorder,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.InactiveCaption"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        InactiveCaption,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.InactiveCaptionText"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        InactiveCaptionText,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Info"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Info,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.InfoText"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        InfoText,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Menu"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Menu,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.MenuText"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MenuText,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.ScrollBar"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ScrollBar,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Window"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Window,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.WindowFrame"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WindowFrame,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.WindowText"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WindowText,

        // "Web" Colors
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Transparent"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Transparent,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.AliceBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        AliceBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.AntiqueWhite"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        AntiqueWhite,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Aqua"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Aqua,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Aquamarine"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Aquamarine,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Azure"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Azure,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Beige"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Beige,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Bisque"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Bisque,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Black"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Black,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.BlanchedAlmond"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        BlanchedAlmond,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Blue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Blue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.BlueViolet"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        BlueViolet,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Brown"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Brown,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.BurlyWood"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        BurlyWood,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.CadetBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        CadetBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Chartreuse"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Chartreuse,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Chocolate"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Chocolate,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Coral"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Coral,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.CornflowerBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        CornflowerBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Cornsilk"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Cornsilk,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Crimson"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Crimson,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Cyan"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Cyan,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkCyan"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkCyan,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkGoldenrod"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkGoldenrod,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkGray"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkGray,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkGreen,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkKhaki"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkKhaki,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkMagenta"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkMagenta,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkOliveGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkOliveGreen,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkOrange"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkOrange,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkOrchid"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkOrchid,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkRed"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkRed,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkSalmon"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkSalmon,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkSeaGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkSeaGreen,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkSlateBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkSlateBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkSlateGray"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkSlateGray,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkTurquoise"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkTurquoise,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DarkViolet"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DarkViolet,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DeepPink"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DeepPink,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DeepSkyBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DeepSkyBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DimGray"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DimGray,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.DodgerBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DodgerBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Firebrick"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Firebrick,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.FloralWhite"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        FloralWhite,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.ForestGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ForestGreen,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Fuchsia"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Fuchsia,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Gainsboro"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Gainsboro,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.GhostWhite"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        GhostWhite,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Gold"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Gold,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Goldenrod"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Goldenrod,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Gray"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Gray,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Green"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Green,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.GreenYellow"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        GreenYellow,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Honeydew"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Honeydew,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.HotPink"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        HotPink,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.IndianRed"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        IndianRed,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Indigo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Indigo,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Ivory"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Ivory,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Khaki"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Khaki,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Lavender"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Lavender,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LavenderBlush"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LavenderBlush,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LawnGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LawnGreen,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LemonChiffon"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LemonChiffon,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LightBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LightBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LightCoral"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LightCoral,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LightCyan"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LightCyan,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LightGoldenrodYellow"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LightGoldenrodYellow,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LightGray"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LightGray,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LightGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LightGreen,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LightPink"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LightPink,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LightSalmon"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LightSalmon,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LightSeaGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LightSeaGreen,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LightSkyBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LightSkyBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LightSlateGray"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LightSlateGray,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LightSteelBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LightSteelBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LightYellow"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LightYellow,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Lime"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Lime,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.LimeGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LimeGreen,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Linen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Linen,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Magenta"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Magenta,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Maroon"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Maroon,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.MediumAquamarine"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MediumAquamarine,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.MediumBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MediumBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.MediumOrchid"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MediumOrchid,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.MediumPurple"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MediumPurple,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.MediumSeaGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MediumSeaGreen,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.MediumSlateBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MediumSlateBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.MediumSpringGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MediumSpringGreen,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.MediumTurquoise"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MediumTurquoise,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.MediumVioletRed"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MediumVioletRed,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.MidnightBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MidnightBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.MintCream"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MintCream,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.MistyRose"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MistyRose,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Moccasin"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Moccasin,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.NavajoWhite"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        NavajoWhite,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Navy"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Navy,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.OldLace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        OldLace,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Olive"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Olive,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.OliveDrab"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        OliveDrab,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Orange"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Orange,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.OrangeRed"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        OrangeRed,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Orchid"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Orchid,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.PaleGoldenrod"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        PaleGoldenrod,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.PaleGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        PaleGreen,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.PaleTurquoise"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        PaleTurquoise,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.PaleVioletRed"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        PaleVioletRed,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.PapayaWhip"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        PapayaWhip,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.PeachPuff"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        PeachPuff,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Peru"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Peru,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Pink"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Pink,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Plum"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Plum,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.PowderBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        PowderBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Purple"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Purple,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Red"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Red,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.RosyBrown"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        RosyBrown,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.RoyalBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        RoyalBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.SaddleBrown"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SaddleBrown,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Salmon"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Salmon,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.SandyBrown"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SandyBrown,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.SeaGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SeaGreen,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.SeaShell"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SeaShell,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Sienna"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Sienna,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Silver"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Silver,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.SkyBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SkyBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.SlateBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SlateBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.SlateGray"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SlateGray,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Snow"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Snow,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.SpringGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SpringGreen,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.SteelBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SteelBlue,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Tan"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Tan,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Teal"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Teal,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Thistle"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Thistle,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Tomato"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Tomato,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Turquoise"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Turquoise,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Violet"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Violet,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Wheat"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Wheat,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.White"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        White,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.WhiteSmoke"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        WhiteSmoke,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.Yellow"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Yellow,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.YellowGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        YellowGreen,

        // NEW ADDITIONS IN WHIDBEY - DO NOT MOVE THESE UP OR IT WILL BE A BREAKING CHANGE

        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.ButtonFace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ButtonFace,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.ButtonHighlight"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ButtonHighlight,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.ButtonShadow"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ButtonShadow,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.GradientActiveCaption"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        GradientActiveCaption,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.GradientInactiveCaption"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        GradientInactiveCaption,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.MenuBar"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MenuBar,
        /// <include file='doc\KnownColor.uex' path='docs/doc[@for="KnownColor.MenuHighlight"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MenuHighlight
    }
}