// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens"]/*' />
    /// <devdoc>
    ///     Pens for all the standard colors.
    /// </devdoc>
    public sealed class Pens
    {
        private static readonly object s_transparentKey = new object();
        private static readonly object s_aliceBlueKey = new object();
        private static readonly object s_antiqueWhiteKey = new object();
        private static readonly object s_aquaKey = new object();
        private static readonly object s_aquamarineKey = new object();
        private static readonly object s_azureKey = new object();
        private static readonly object s_beigeKey = new object();
        private static readonly object s_bisqueKey = new object();
        private static readonly object s_blackKey = new object();
        private static readonly object s_blanchedAlmondKey = new object();
        private static readonly object s_blueKey = new object();
        private static readonly object s_blueVioletKey = new object();
        private static readonly object s_brownKey = new object();
        private static readonly object s_burlyWoodKey = new object();
        private static readonly object s_cadetBlueKey = new object();
        private static readonly object s_chartreuseKey = new object();
        private static readonly object s_chocolateKey = new object();
        private static readonly object s_choralKey = new object();
        private static readonly object s_cornflowerBlueKey = new object();
        private static readonly object s_cornsilkKey = new object();
        private static readonly object s_crimsonKey = new object();
        private static readonly object s_cyanKey = new object();
        private static readonly object s_darkBlueKey = new object();
        private static readonly object s_darkCyanKey = new object();
        private static readonly object s_darkGoldenrodKey = new object();
        private static readonly object s_darkGrayKey = new object();
        private static readonly object s_darkGreenKey = new object();
        private static readonly object s_darkKhakiKey = new object();
        private static readonly object s_darkMagentaKey = new object();
        private static readonly object s_darkOliveGreenKey = new object();
        private static readonly object s_darkOrangeKey = new object();
        private static readonly object s_darkOrchidKey = new object();
        private static readonly object s_darkRedKey = new object();
        private static readonly object s_darkSalmonKey = new object();
        private static readonly object s_darkSeaGreenKey = new object();
        private static readonly object s_darkSlateBlueKey = new object();
        private static readonly object s_darkSlateGrayKey = new object();
        private static readonly object s_darkTurquoiseKey = new object();
        private static readonly object s_darkVioletKey = new object();
        private static readonly object s_deepPinkKey = new object();
        private static readonly object s_deepSkyBlueKey = new object();
        private static readonly object s_dimGrayKey = new object();
        private static readonly object s_dodgerBlueKey = new object();
        private static readonly object s_firebrickKey = new object();
        private static readonly object s_floralWhiteKey = new object();
        private static readonly object s_forestGreenKey = new object();
        private static readonly object s_fuchiaKey = new object();
        private static readonly object s_gainsboroKey = new object();
        private static readonly object s_ghostWhiteKey = new object();
        private static readonly object s_goldKey = new object();
        private static readonly object s_goldenrodKey = new object();
        private static readonly object s_grayKey = new object();
        private static readonly object s_greenKey = new object();
        private static readonly object s_greenYellowKey = new object();
        private static readonly object s_honeydewKey = new object();
        private static readonly object s_hotPinkKey = new object();
        private static readonly object s_indianRedKey = new object();
        private static readonly object s_indigoKey = new object();
        private static readonly object s_ivoryKey = new object();
        private static readonly object s_khakiKey = new object();
        private static readonly object s_lavenderKey = new object();
        private static readonly object s_lavenderBlushKey = new object();
        private static readonly object s_lawnGreenKey = new object();
        private static readonly object s_lemonChiffonKey = new object();
        private static readonly object s_lightBlueKey = new object();
        private static readonly object s_lightCoralKey = new object();
        private static readonly object s_lightCyanKey = new object();
        private static readonly object s_lightGoldenrodYellowKey = new object();
        private static readonly object s_lightGreenKey = new object();
        private static readonly object s_lightGrayKey = new object();
        private static readonly object s_lightPinkKey = new object();
        private static readonly object s_lightSalmonKey = new object();
        private static readonly object s_lightSeaGreenKey = new object();
        private static readonly object s_lightSkyBlueKey = new object();
        private static readonly object s_lightSlateGrayKey = new object();
        private static readonly object s_lightSteelBlueKey = new object();
        private static readonly object s_lightYellowKey = new object();
        private static readonly object s_limeKey = new object();
        private static readonly object s_limeGreenKey = new object();
        private static readonly object s_linenKey = new object();
        private static readonly object s_magentaKey = new object();
        private static readonly object s_maroonKey = new object();
        private static readonly object s_mediumAquamarineKey = new object();
        private static readonly object s_mediumBlueKey = new object();
        private static readonly object s_mediumOrchidKey = new object();
        private static readonly object s_mediumPurpleKey = new object();
        private static readonly object s_mediumSeaGreenKey = new object();
        private static readonly object s_mediumSlateBlueKey = new object();
        private static readonly object s_mediumSpringGreenKey = new object();
        private static readonly object s_mediumTurquoiseKey = new object();
        private static readonly object s_mediumVioletRedKey = new object();
        private static readonly object s_midnightBlueKey = new object();
        private static readonly object s_mintCreamKey = new object();
        private static readonly object s_mistyRoseKey = new object();
        private static readonly object s_moccasinKey = new object();
        private static readonly object s_navajoWhiteKey = new object();
        private static readonly object s_navyKey = new object();
        private static readonly object s_oldLaceKey = new object();
        private static readonly object s_oliveKey = new object();
        private static readonly object s_oliveDrabKey = new object();
        private static readonly object s_orangeKey = new object();
        private static readonly object s_orangeRedKey = new object();
        private static readonly object s_orchidKey = new object();
        private static readonly object s_paleGoldenrodKey = new object();
        private static readonly object s_paleGreenKey = new object();
        private static readonly object s_paleTurquoiseKey = new object();
        private static readonly object s_paleVioletRedKey = new object();
        private static readonly object s_papayaWhipKey = new object();
        private static readonly object s_peachPuffKey = new object();
        private static readonly object s_peruKey = new object();
        private static readonly object s_pinkKey = new object();
        private static readonly object s_plumKey = new object();
        private static readonly object s_powderBlueKey = new object();
        private static readonly object s_purpleKey = new object();
        private static readonly object s_redKey = new object();
        private static readonly object s_rosyBrownKey = new object();
        private static readonly object s_royalBlueKey = new object();
        private static readonly object s_saddleBrownKey = new object();
        private static readonly object s_salmonKey = new object();
        private static readonly object s_sandyBrownKey = new object();
        private static readonly object s_seaGreenKey = new object();
        private static readonly object s_seaShellKey = new object();
        private static readonly object s_siennaKey = new object();
        private static readonly object s_silverKey = new object();
        private static readonly object s_skyBlueKey = new object();
        private static readonly object s_slateBlueKey = new object();
        private static readonly object s_slateGrayKey = new object();
        private static readonly object s_snowKey = new object();
        private static readonly object s_springGreenKey = new object();
        private static readonly object s_steelBlueKey = new object();
        private static readonly object s_tanKey = new object();
        private static readonly object s_tealKey = new object();
        private static readonly object s_thistleKey = new object();
        private static readonly object s_tomatoKey = new object();
        private static readonly object s_turquoiseKey = new object();
        private static readonly object s_violetKey = new object();
        private static readonly object s_wheatKey = new object();
        private static readonly object s_whiteKey = new object();
        private static readonly object s_whiteSmokeKey = new object();
        private static readonly object s_yellowKey = new object();
        private static readonly object s_yellowGreenKey = new object();

        private Pens()
        {
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Transparent"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Transparent
        {
            get
            {
                Pen transparent = (Pen)SafeNativeMethods.Gdip.ThreadData[s_transparentKey];
                if (transparent == null)
                {
                    transparent = new Pen(Color.Transparent, true);
                    SafeNativeMethods.Gdip.ThreadData[s_transparentKey] = transparent;
                }
                return transparent;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.AliceBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen AliceBlue
        {
            get
            {
                Pen aliceBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_aliceBlueKey];
                if (aliceBlue == null)
                {
                    aliceBlue = new Pen(Color.AliceBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_aliceBlueKey] = aliceBlue;
                }
                return aliceBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.AntiqueWhite"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen AntiqueWhite
        {
            get
            {
                Pen antiqueWhite = (Pen)SafeNativeMethods.Gdip.ThreadData[s_antiqueWhiteKey];
                if (antiqueWhite == null)
                {
                    antiqueWhite = new Pen(Color.AntiqueWhite, true);
                    SafeNativeMethods.Gdip.ThreadData[s_antiqueWhiteKey] = antiqueWhite;
                }
                return antiqueWhite;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Aqua"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Aqua
        {
            get
            {
                Pen aqua = (Pen)SafeNativeMethods.Gdip.ThreadData[s_aquaKey];
                if (aqua == null)
                {
                    aqua = new Pen(Color.Aqua, true);
                    SafeNativeMethods.Gdip.ThreadData[s_aquaKey] = aqua;
                }
                return aqua;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Aquamarine"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Aquamarine
        {
            get
            {
                Pen aquamarine = (Pen)SafeNativeMethods.Gdip.ThreadData[s_aquamarineKey];
                if (aquamarine == null)
                {
                    aquamarine = new Pen(Color.Aquamarine, true);
                    SafeNativeMethods.Gdip.ThreadData[s_aquamarineKey] = aquamarine;
                }
                return aquamarine;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Azure"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Azure
        {
            get
            {
                Pen azure = (Pen)SafeNativeMethods.Gdip.ThreadData[s_azureKey];
                if (azure == null)
                {
                    azure = new Pen(Color.Azure, true);
                    SafeNativeMethods.Gdip.ThreadData[s_azureKey] = azure;
                }
                return azure;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Beige"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Beige
        {
            get
            {
                Pen beige = (Pen)SafeNativeMethods.Gdip.ThreadData[s_beigeKey];
                if (beige == null)
                {
                    beige = new Pen(Color.Beige, true);
                    SafeNativeMethods.Gdip.ThreadData[s_beigeKey] = beige;
                }
                return beige;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Bisque"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Bisque
        {
            get
            {
                Pen bisque = (Pen)SafeNativeMethods.Gdip.ThreadData[s_bisqueKey];
                if (bisque == null)
                {
                    bisque = new Pen(Color.Bisque, true);
                    SafeNativeMethods.Gdip.ThreadData[s_bisqueKey] = bisque;
                }
                return bisque;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Black"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Black
        {
            get
            {
                Pen black = (Pen)SafeNativeMethods.Gdip.ThreadData[s_blackKey];
                if (black == null)
                {
                    black = new Pen(Color.Black, true);
                    SafeNativeMethods.Gdip.ThreadData[s_blackKey] = black;
                }
                return black;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.BlanchedAlmond"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen BlanchedAlmond
        {
            get
            {
                Pen blanchedAlmond = (Pen)SafeNativeMethods.Gdip.ThreadData[s_blanchedAlmondKey];
                if (blanchedAlmond == null)
                {
                    blanchedAlmond = new Pen(Color.BlanchedAlmond, true);
                    SafeNativeMethods.Gdip.ThreadData[s_blanchedAlmondKey] = blanchedAlmond;
                }
                return blanchedAlmond;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Blue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Blue
        {
            get
            {
                Pen blue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_blueKey];
                if (blue == null)
                {
                    blue = new Pen(Color.Blue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_blueKey] = blue;
                }
                return blue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.BlueViolet"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen BlueViolet
        {
            get
            {
                Pen blueViolet = (Pen)SafeNativeMethods.Gdip.ThreadData[s_blueVioletKey];
                if (blueViolet == null)
                {
                    blueViolet = new Pen(Color.BlueViolet, true);
                    SafeNativeMethods.Gdip.ThreadData[s_blueVioletKey] = blueViolet;
                }
                return blueViolet;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Brown"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Brown
        {
            get
            {
                Pen brown = (Pen)SafeNativeMethods.Gdip.ThreadData[s_brownKey];
                if (brown == null)
                {
                    brown = new Pen(Color.Brown, true);
                    SafeNativeMethods.Gdip.ThreadData[s_brownKey] = brown;
                }
                return brown;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.BurlyWood"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen BurlyWood
        {
            get
            {
                Pen burlyWood = (Pen)SafeNativeMethods.Gdip.ThreadData[s_burlyWoodKey];
                if (burlyWood == null)
                {
                    burlyWood = new Pen(Color.BurlyWood, true);
                    SafeNativeMethods.Gdip.ThreadData[s_burlyWoodKey] = burlyWood;
                }
                return burlyWood;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.CadetBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen CadetBlue
        {
            get
            {
                Pen cadetBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_cadetBlueKey];
                if (cadetBlue == null)
                {
                    cadetBlue = new Pen(Color.CadetBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_cadetBlueKey] = cadetBlue;
                }
                return cadetBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Chartreuse"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Chartreuse
        {
            get
            {
                Pen chartreuse = (Pen)SafeNativeMethods.Gdip.ThreadData[s_chartreuseKey];
                if (chartreuse == null)
                {
                    chartreuse = new Pen(Color.Chartreuse, true);
                    SafeNativeMethods.Gdip.ThreadData[s_chartreuseKey] = chartreuse;
                }
                return chartreuse;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Chocolate"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Chocolate
        {
            get
            {
                Pen chocolate = (Pen)SafeNativeMethods.Gdip.ThreadData[s_chocolateKey];
                if (chocolate == null)
                {
                    chocolate = new Pen(Color.Chocolate, true);
                    SafeNativeMethods.Gdip.ThreadData[s_chocolateKey] = chocolate;
                }
                return chocolate;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Coral"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Coral
        {
            get
            {
                Pen choral = (Pen)SafeNativeMethods.Gdip.ThreadData[s_choralKey];
                if (choral == null)
                {
                    choral = new Pen(Color.Coral, true);
                    SafeNativeMethods.Gdip.ThreadData[s_choralKey] = choral;
                }
                return choral;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.CornflowerBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen CornflowerBlue
        {
            get
            {
                Pen cornflowerBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_cornflowerBlueKey];
                if (cornflowerBlue == null)
                {
                    cornflowerBlue = new Pen(Color.CornflowerBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_cornflowerBlueKey] = cornflowerBlue;
                }
                return cornflowerBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Cornsilk"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Cornsilk
        {
            get
            {
                Pen cornsilk = (Pen)SafeNativeMethods.Gdip.ThreadData[s_cornsilkKey];
                if (cornsilk == null)
                {
                    cornsilk = new Pen(Color.Cornsilk, true);
                    SafeNativeMethods.Gdip.ThreadData[s_cornsilkKey] = cornsilk;
                }
                return cornsilk;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Crimson"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Crimson
        {
            get
            {
                Pen crimson = (Pen)SafeNativeMethods.Gdip.ThreadData[s_crimsonKey];
                if (crimson == null)
                {
                    crimson = new Pen(Color.Crimson, true);
                    SafeNativeMethods.Gdip.ThreadData[s_crimsonKey] = crimson;
                }
                return crimson;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Cyan"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Cyan
        {
            get
            {
                Pen cyan = (Pen)SafeNativeMethods.Gdip.ThreadData[s_cyanKey];
                if (cyan == null)
                {
                    cyan = new Pen(Color.Cyan, true);
                    SafeNativeMethods.Gdip.ThreadData[s_cyanKey] = cyan;
                }
                return cyan;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkBlue
        {
            get
            {
                Pen darkBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkBlueKey];
                if (darkBlue == null)
                {
                    darkBlue = new Pen(Color.DarkBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkBlueKey] = darkBlue;
                }
                return darkBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkCyan"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkCyan
        {
            get
            {
                Pen darkCyan = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkCyanKey];
                if (darkCyan == null)
                {
                    darkCyan = new Pen(Color.DarkCyan, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkCyanKey] = darkCyan;
                }
                return darkCyan;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkGoldenrod"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkGoldenrod
        {
            get
            {
                Pen darkGoldenrod = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkGoldenrodKey];
                if (darkGoldenrod == null)
                {
                    darkGoldenrod = new Pen(Color.DarkGoldenrod, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkGoldenrodKey] = darkGoldenrod;
                }
                return darkGoldenrod;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkGray"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkGray
        {
            get
            {
                Pen darkGray = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkGrayKey];
                if (darkGray == null)
                {
                    darkGray = new Pen(Color.DarkGray, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkGrayKey] = darkGray;
                }
                return darkGray;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkGreen
        {
            get
            {
                Pen darkGreen = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkGreenKey];
                if (darkGreen == null)
                {
                    darkGreen = new Pen(Color.DarkGreen, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkGreenKey] = darkGreen;
                }
                return darkGreen;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkKhaki"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkKhaki
        {
            get
            {
                Pen darkKhaki = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkKhakiKey];
                if (darkKhaki == null)
                {
                    darkKhaki = new Pen(Color.DarkKhaki, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkKhakiKey] = darkKhaki;
                }
                return darkKhaki;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkMagenta"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkMagenta
        {
            get
            {
                Pen darkMagenta = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkMagentaKey];
                if (darkMagenta == null)
                {
                    darkMagenta = new Pen(Color.DarkMagenta, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkMagentaKey] = darkMagenta;
                }
                return darkMagenta;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkOliveGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkOliveGreen
        {
            get
            {
                Pen darkOliveGreen = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkOliveGreenKey];
                if (darkOliveGreen == null)
                {
                    darkOliveGreen = new Pen(Color.DarkOliveGreen, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkOliveGreenKey] = darkOliveGreen;
                }
                return darkOliveGreen;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkOrange"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkOrange
        {
            get
            {
                Pen darkOrange = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkOrangeKey];
                if (darkOrange == null)
                {
                    darkOrange = new Pen(Color.DarkOrange, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkOrangeKey] = darkOrange;
                }
                return darkOrange;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkOrchid"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkOrchid
        {
            get
            {
                Pen darkOrchid = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkOrchidKey];
                if (darkOrchid == null)
                {
                    darkOrchid = new Pen(Color.DarkOrchid, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkOrchidKey] = darkOrchid;
                }
                return darkOrchid;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkRed"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkRed
        {
            get
            {
                Pen darkRed = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkRedKey];
                if (darkRed == null)
                {
                    darkRed = new Pen(Color.DarkRed, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkRedKey] = darkRed;
                }
                return darkRed;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkSalmon"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkSalmon
        {
            get
            {
                Pen darkSalmon = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkSalmonKey];
                if (darkSalmon == null)
                {
                    darkSalmon = new Pen(Color.DarkSalmon, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkSalmonKey] = darkSalmon;
                }
                return darkSalmon;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkSeaGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkSeaGreen
        {
            get
            {
                Pen darkSeaGreen = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkSeaGreenKey];
                if (darkSeaGreen == null)
                {
                    darkSeaGreen = new Pen(Color.DarkSeaGreen, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkSeaGreenKey] = darkSeaGreen;
                }
                return darkSeaGreen;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkSlateBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkSlateBlue
        {
            get
            {
                Pen darkSlateBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkSlateBlueKey];
                if (darkSlateBlue == null)
                {
                    darkSlateBlue = new Pen(Color.DarkSlateBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkSlateBlueKey] = darkSlateBlue;
                }
                return darkSlateBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkSlateGray"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkSlateGray
        {
            get
            {
                Pen darkSlateGray = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkSlateGrayKey];
                if (darkSlateGray == null)
                {
                    darkSlateGray = new Pen(Color.DarkSlateGray, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkSlateGrayKey] = darkSlateGray;
                }
                return darkSlateGray;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkTurquoise"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkTurquoise
        {
            get
            {
                Pen darkTurquoise = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkTurquoiseKey];
                if (darkTurquoise == null)
                {
                    darkTurquoise = new Pen(Color.DarkTurquoise, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkTurquoiseKey] = darkTurquoise;
                }
                return darkTurquoise;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DarkViolet"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DarkViolet
        {
            get
            {
                Pen darkViolet = (Pen)SafeNativeMethods.Gdip.ThreadData[s_darkVioletKey];
                if (darkViolet == null)
                {
                    darkViolet = new Pen(Color.DarkViolet, true);
                    SafeNativeMethods.Gdip.ThreadData[s_darkVioletKey] = darkViolet;
                }
                return darkViolet;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DeepPink"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DeepPink
        {
            get
            {
                Pen deepPink = (Pen)SafeNativeMethods.Gdip.ThreadData[s_deepPinkKey];
                if (deepPink == null)
                {
                    deepPink = new Pen(Color.DeepPink, true);
                    SafeNativeMethods.Gdip.ThreadData[s_deepPinkKey] = deepPink;
                }
                return deepPink;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DeepSkyBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DeepSkyBlue
        {
            get
            {
                Pen deepSkyBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_deepSkyBlueKey];
                if (deepSkyBlue == null)
                {
                    deepSkyBlue = new Pen(Color.DeepSkyBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_deepSkyBlueKey] = deepSkyBlue;
                }
                return deepSkyBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DimGray"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DimGray
        {
            get
            {
                Pen dimGray = (Pen)SafeNativeMethods.Gdip.ThreadData[s_dimGrayKey];
                if (dimGray == null)
                {
                    dimGray = new Pen(Color.DimGray, true);
                    SafeNativeMethods.Gdip.ThreadData[s_dimGrayKey] = dimGray;
                }
                return dimGray;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.DodgerBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen DodgerBlue
        {
            get
            {
                Pen dodgerBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_dodgerBlueKey];
                if (dodgerBlue == null)
                {
                    dodgerBlue = new Pen(Color.DodgerBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_dodgerBlueKey] = dodgerBlue;
                }
                return dodgerBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Firebrick"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Firebrick
        {
            get
            {
                Pen firebrick = (Pen)SafeNativeMethods.Gdip.ThreadData[s_firebrickKey];
                if (firebrick == null)
                {
                    firebrick = new Pen(Color.Firebrick, true);
                    SafeNativeMethods.Gdip.ThreadData[s_firebrickKey] = firebrick;
                }
                return firebrick;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.FloralWhite"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen FloralWhite
        {
            get
            {
                Pen floralWhite = (Pen)SafeNativeMethods.Gdip.ThreadData[s_floralWhiteKey];
                if (floralWhite == null)
                {
                    floralWhite = new Pen(Color.FloralWhite, true);
                    SafeNativeMethods.Gdip.ThreadData[s_floralWhiteKey] = floralWhite;
                }
                return floralWhite;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.ForestGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen ForestGreen
        {
            get
            {
                Pen forestGreen = (Pen)SafeNativeMethods.Gdip.ThreadData[s_forestGreenKey];
                if (forestGreen == null)
                {
                    forestGreen = new Pen(Color.ForestGreen, true);
                    SafeNativeMethods.Gdip.ThreadData[s_forestGreenKey] = forestGreen;
                }
                return forestGreen;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Fuchsia"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Fuchsia
        {
            get
            {
                Pen fuchia = (Pen)SafeNativeMethods.Gdip.ThreadData[s_fuchiaKey];
                if (fuchia == null)
                {
                    fuchia = new Pen(Color.Fuchsia, true);
                    SafeNativeMethods.Gdip.ThreadData[s_fuchiaKey] = fuchia;
                }
                return fuchia;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Gainsboro"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Gainsboro
        {
            get
            {
                Pen gainsboro = (Pen)SafeNativeMethods.Gdip.ThreadData[s_gainsboroKey];
                if (gainsboro == null)
                {
                    gainsboro = new Pen(Color.Gainsboro, true);
                    SafeNativeMethods.Gdip.ThreadData[s_gainsboroKey] = gainsboro;
                }
                return gainsboro;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.GhostWhite"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen GhostWhite
        {
            get
            {
                Pen ghostWhite = (Pen)SafeNativeMethods.Gdip.ThreadData[s_ghostWhiteKey];
                if (ghostWhite == null)
                {
                    ghostWhite = new Pen(Color.GhostWhite, true);
                    SafeNativeMethods.Gdip.ThreadData[s_ghostWhiteKey] = ghostWhite;
                }
                return ghostWhite;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Gold"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Gold
        {
            get
            {
                Pen gold = (Pen)SafeNativeMethods.Gdip.ThreadData[s_goldKey];
                if (gold == null)
                {
                    gold = new Pen(Color.Gold, true);
                    SafeNativeMethods.Gdip.ThreadData[s_goldKey] = gold;
                }
                return gold;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Goldenrod"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Goldenrod
        {
            get
            {
                Pen goldenrod = (Pen)SafeNativeMethods.Gdip.ThreadData[s_goldenrodKey];
                if (goldenrod == null)
                {
                    goldenrod = new Pen(Color.Goldenrod, true);
                    SafeNativeMethods.Gdip.ThreadData[s_goldenrodKey] = goldenrod;
                }
                return goldenrod;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Gray"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Gray
        {
            get
            {
                Pen gray = (Pen)SafeNativeMethods.Gdip.ThreadData[s_grayKey];
                if (gray == null)
                {
                    gray = new Pen(Color.Gray, true);
                    SafeNativeMethods.Gdip.ThreadData[s_grayKey] = gray;
                }
                return gray;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Green"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Green
        {
            get
            {
                Pen green = (Pen)SafeNativeMethods.Gdip.ThreadData[s_greenKey];
                if (green == null)
                {
                    green = new Pen(Color.Green, true);
                    SafeNativeMethods.Gdip.ThreadData[s_greenKey] = green;
                }
                return green;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.GreenYellow"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen GreenYellow
        {
            get
            {
                Pen greenYellow = (Pen)SafeNativeMethods.Gdip.ThreadData[s_greenYellowKey];
                if (greenYellow == null)
                {
                    greenYellow = new Pen(Color.GreenYellow, true);
                    SafeNativeMethods.Gdip.ThreadData[s_greenYellowKey] = greenYellow;
                }
                return greenYellow;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Honeydew"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Honeydew
        {
            get
            {
                Pen honeydew = (Pen)SafeNativeMethods.Gdip.ThreadData[s_honeydewKey];
                if (honeydew == null)
                {
                    honeydew = new Pen(Color.Honeydew, true);
                    SafeNativeMethods.Gdip.ThreadData[s_honeydewKey] = honeydew;
                }
                return honeydew;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.HotPink"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen HotPink
        {
            get
            {
                Pen hotPink = (Pen)SafeNativeMethods.Gdip.ThreadData[s_hotPinkKey];
                if (hotPink == null)
                {
                    hotPink = new Pen(Color.HotPink, true);
                    SafeNativeMethods.Gdip.ThreadData[s_hotPinkKey] = hotPink;
                }
                return hotPink;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.IndianRed"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen IndianRed
        {
            get
            {
                Pen indianRed = (Pen)SafeNativeMethods.Gdip.ThreadData[s_indianRedKey];
                if (indianRed == null)
                {
                    indianRed = new Pen(Color.IndianRed, true);
                    SafeNativeMethods.Gdip.ThreadData[s_indianRedKey] = indianRed;
                }
                return indianRed;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Indigo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Indigo
        {
            get
            {
                Pen indigo = (Pen)SafeNativeMethods.Gdip.ThreadData[s_indigoKey];
                if (indigo == null)
                {
                    indigo = new Pen(Color.Indigo, true);
                    SafeNativeMethods.Gdip.ThreadData[s_indigoKey] = indigo;
                }
                return indigo;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Ivory"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Ivory
        {
            get
            {
                Pen ivory = (Pen)SafeNativeMethods.Gdip.ThreadData[s_ivoryKey];
                if (ivory == null)
                {
                    ivory = new Pen(Color.Ivory, true);
                    SafeNativeMethods.Gdip.ThreadData[s_ivoryKey] = ivory;
                }
                return ivory;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Khaki"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Khaki
        {
            get
            {
                Pen khaki = (Pen)SafeNativeMethods.Gdip.ThreadData[s_khakiKey];
                if (khaki == null)
                {
                    khaki = new Pen(Color.Khaki, true);
                    SafeNativeMethods.Gdip.ThreadData[s_khakiKey] = khaki;
                }
                return khaki;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Lavender"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Lavender
        {
            get
            {
                Pen lavender = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lavenderKey];
                if (lavender == null)
                {
                    lavender = new Pen(Color.Lavender, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lavenderKey] = lavender;
                }
                return lavender;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LavenderBlush"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LavenderBlush
        {
            get
            {
                Pen lavenderBlush = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lavenderBlushKey];
                if (lavenderBlush == null)
                {
                    lavenderBlush = new Pen(Color.LavenderBlush, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lavenderBlushKey] = lavenderBlush;
                }
                return lavenderBlush;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LawnGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LawnGreen
        {
            get
            {
                Pen lawnGreen = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lawnGreenKey];
                if (lawnGreen == null)
                {
                    lawnGreen = new Pen(Color.LawnGreen, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lawnGreenKey] = lawnGreen;
                }
                return lawnGreen;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LemonChiffon"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LemonChiffon
        {
            get
            {
                Pen lemonChiffon = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lemonChiffonKey];
                if (lemonChiffon == null)
                {
                    lemonChiffon = new Pen(Color.LemonChiffon, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lemonChiffonKey] = lemonChiffon;
                }
                return lemonChiffon;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LightBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LightBlue
        {
            get
            {
                Pen lightBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lightBlueKey];
                if (lightBlue == null)
                {
                    lightBlue = new Pen(Color.LightBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lightBlueKey] = lightBlue;
                }
                return lightBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LightCoral"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LightCoral
        {
            get
            {
                Pen lightCoral = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lightCoralKey];
                if (lightCoral == null)
                {
                    lightCoral = new Pen(Color.LightCoral, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lightCoralKey] = lightCoral;
                }
                return lightCoral;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LightCyan"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LightCyan
        {
            get
            {
                Pen lightCyan = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lightCyanKey];
                if (lightCyan == null)
                {
                    lightCyan = new Pen(Color.LightCyan, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lightCyanKey] = lightCyan;
                }
                return lightCyan;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LightGoldenrodYellow"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LightGoldenrodYellow
        {
            get
            {
                Pen lightGoldenrodYellow = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lightGoldenrodYellowKey];
                if (lightGoldenrodYellow == null)
                {
                    lightGoldenrodYellow = new Pen(Color.LightGoldenrodYellow, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lightGoldenrodYellowKey] = lightGoldenrodYellow;
                }
                return lightGoldenrodYellow;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LightGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LightGreen
        {
            get
            {
                Pen lightGreen = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lightGreenKey];
                if (lightGreen == null)
                {
                    lightGreen = new Pen(Color.LightGreen, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lightGreenKey] = lightGreen;
                }
                return lightGreen;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LightGray"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LightGray
        {
            get
            {
                Pen lightGray = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lightGrayKey];
                if (lightGray == null)
                {
                    lightGray = new Pen(Color.LightGray, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lightGrayKey] = lightGray;
                }
                return lightGray;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LightPink"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LightPink
        {
            get
            {
                Pen lightPink = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lightPinkKey];
                if (lightPink == null)
                {
                    lightPink = new Pen(Color.LightPink, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lightPinkKey] = lightPink;
                }
                return lightPink;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LightSalmon"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LightSalmon
        {
            get
            {
                Pen lightSalmon = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lightSalmonKey];
                if (lightSalmon == null)
                {
                    lightSalmon = new Pen(Color.LightSalmon, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lightSalmonKey] = lightSalmon;
                }
                return lightSalmon;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LightSeaGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LightSeaGreen
        {
            get
            {
                Pen lightSeaGreen = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lightSeaGreenKey];
                if (lightSeaGreen == null)
                {
                    lightSeaGreen = new Pen(Color.LightSeaGreen, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lightSeaGreenKey] = lightSeaGreen;
                }
                return lightSeaGreen;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LightSkyBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LightSkyBlue
        {
            get
            {
                Pen lightSkyBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lightSkyBlueKey];
                if (lightSkyBlue == null)
                {
                    lightSkyBlue = new Pen(Color.LightSkyBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lightSkyBlueKey] = lightSkyBlue;
                }
                return lightSkyBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LightSlateGray"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LightSlateGray
        {
            get
            {
                Pen lightSlateGray = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lightSlateGrayKey];
                if (lightSlateGray == null)
                {
                    lightSlateGray = new Pen(Color.LightSlateGray, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lightSlateGrayKey] = lightSlateGray;
                }
                return lightSlateGray;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LightSteelBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LightSteelBlue
        {
            get
            {
                Pen lightSteelBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lightSteelBlueKey];
                if (lightSteelBlue == null)
                {
                    lightSteelBlue = new Pen(Color.LightSteelBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lightSteelBlueKey] = lightSteelBlue;
                }
                return lightSteelBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LightYellow"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LightYellow
        {
            get
            {
                Pen lightYellow = (Pen)SafeNativeMethods.Gdip.ThreadData[s_lightYellowKey];
                if (lightYellow == null)
                {
                    lightYellow = new Pen(Color.LightYellow, true);
                    SafeNativeMethods.Gdip.ThreadData[s_lightYellowKey] = lightYellow;
                }
                return lightYellow;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Lime"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Lime
        {
            get
            {
                Pen lime = (Pen)SafeNativeMethods.Gdip.ThreadData[s_limeKey];
                if (lime == null)
                {
                    lime = new Pen(Color.Lime, true);
                    SafeNativeMethods.Gdip.ThreadData[s_limeKey] = lime;
                }
                return lime;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.LimeGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen LimeGreen
        {
            get
            {
                Pen limeGreen = (Pen)SafeNativeMethods.Gdip.ThreadData[s_limeGreenKey];
                if (limeGreen == null)
                {
                    limeGreen = new Pen(Color.LimeGreen, true);
                    SafeNativeMethods.Gdip.ThreadData[s_limeGreenKey] = limeGreen;
                }
                return limeGreen;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Linen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Linen
        {
            get
            {
                Pen linen = (Pen)SafeNativeMethods.Gdip.ThreadData[s_linenKey];
                if (linen == null)
                {
                    linen = new Pen(Color.Linen, true);
                    SafeNativeMethods.Gdip.ThreadData[s_linenKey] = linen;
                }
                return linen;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Magenta"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Magenta
        {
            get
            {
                Pen magenta = (Pen)SafeNativeMethods.Gdip.ThreadData[s_magentaKey];
                if (magenta == null)
                {
                    magenta = new Pen(Color.Magenta, true);
                    SafeNativeMethods.Gdip.ThreadData[s_magentaKey] = magenta;
                }
                return magenta;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Maroon"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Maroon
        {
            get
            {
                Pen maroon = (Pen)SafeNativeMethods.Gdip.ThreadData[s_maroonKey];
                if (maroon == null)
                {
                    maroon = new Pen(Color.Maroon, true);
                    SafeNativeMethods.Gdip.ThreadData[s_maroonKey] = maroon;
                }
                return maroon;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.MediumAquamarine"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen MediumAquamarine
        {
            get
            {
                Pen mediumAquamarine = (Pen)SafeNativeMethods.Gdip.ThreadData[s_mediumAquamarineKey];
                if (mediumAquamarine == null)
                {
                    mediumAquamarine = new Pen(Color.MediumAquamarine, true);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumAquamarineKey] = mediumAquamarine;
                }
                return mediumAquamarine;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.MediumBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen MediumBlue
        {
            get
            {
                Pen mediumBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_mediumBlueKey];
                if (mediumBlue == null)
                {
                    mediumBlue = new Pen(Color.MediumBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumBlueKey] = mediumBlue;
                }
                return mediumBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.MediumOrchid"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen MediumOrchid
        {
            get
            {
                Pen mediumOrchid = (Pen)SafeNativeMethods.Gdip.ThreadData[s_mediumOrchidKey];
                if (mediumOrchid == null)
                {
                    mediumOrchid = new Pen(Color.MediumOrchid, true);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumOrchidKey] = mediumOrchid;
                }
                return mediumOrchid;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.MediumPurple"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen MediumPurple
        {
            get
            {
                Pen mediumPurple = (Pen)SafeNativeMethods.Gdip.ThreadData[s_mediumPurpleKey];
                if (mediumPurple == null)
                {
                    mediumPurple = new Pen(Color.MediumPurple, true);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumPurpleKey] = mediumPurple;
                }
                return mediumPurple;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.MediumSeaGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen MediumSeaGreen
        {
            get
            {
                Pen mediumSeaGreen = (Pen)SafeNativeMethods.Gdip.ThreadData[s_mediumSeaGreenKey];
                if (mediumSeaGreen == null)
                {
                    mediumSeaGreen = new Pen(Color.MediumSeaGreen, true);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumSeaGreenKey] = mediumSeaGreen;
                }
                return mediumSeaGreen;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.MediumSlateBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen MediumSlateBlue
        {
            get
            {
                Pen mediumSlateBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_mediumSlateBlueKey];
                if (mediumSlateBlue == null)
                {
                    mediumSlateBlue = new Pen(Color.MediumSlateBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumSlateBlueKey] = mediumSlateBlue;
                }
                return mediumSlateBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.MediumSpringGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen MediumSpringGreen
        {
            get
            {
                Pen mediumSpringGreen = (Pen)SafeNativeMethods.Gdip.ThreadData[s_mediumSpringGreenKey];
                if (mediumSpringGreen == null)
                {
                    mediumSpringGreen = new Pen(Color.MediumSpringGreen, true);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumSpringGreenKey] = mediumSpringGreen;
                }
                return mediumSpringGreen;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.MediumTurquoise"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen MediumTurquoise
        {
            get
            {
                Pen mediumTurquoise = (Pen)SafeNativeMethods.Gdip.ThreadData[s_mediumTurquoiseKey];
                if (mediumTurquoise == null)
                {
                    mediumTurquoise = new Pen(Color.MediumTurquoise, true);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumTurquoiseKey] = mediumTurquoise;
                }
                return mediumTurquoise;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.MediumVioletRed"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen MediumVioletRed
        {
            get
            {
                Pen mediumVioletRed = (Pen)SafeNativeMethods.Gdip.ThreadData[s_mediumVioletRedKey];
                if (mediumVioletRed == null)
                {
                    mediumVioletRed = new Pen(Color.MediumVioletRed, true);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumVioletRedKey] = mediumVioletRed;
                }
                return mediumVioletRed;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.MidnightBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen MidnightBlue
        {
            get
            {
                Pen midnightBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_midnightBlueKey];
                if (midnightBlue == null)
                {
                    midnightBlue = new Pen(Color.MidnightBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_midnightBlueKey] = midnightBlue;
                }
                return midnightBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.MintCream"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen MintCream
        {
            get
            {
                Pen mintCream = (Pen)SafeNativeMethods.Gdip.ThreadData[s_mintCreamKey];
                if (mintCream == null)
                {
                    mintCream = new Pen(Color.MintCream, true);
                    SafeNativeMethods.Gdip.ThreadData[s_mintCreamKey] = mintCream;
                }
                return mintCream;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.MistyRose"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen MistyRose
        {
            get
            {
                Pen mistyRose = (Pen)SafeNativeMethods.Gdip.ThreadData[s_mistyRoseKey];
                if (mistyRose == null)
                {
                    mistyRose = new Pen(Color.MistyRose, true);
                    SafeNativeMethods.Gdip.ThreadData[s_mistyRoseKey] = mistyRose;
                }
                return mistyRose;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Moccasin"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Moccasin
        {
            get
            {
                Pen moccasin = (Pen)SafeNativeMethods.Gdip.ThreadData[s_moccasinKey];
                if (moccasin == null)
                {
                    moccasin = new Pen(Color.Moccasin, true);
                    SafeNativeMethods.Gdip.ThreadData[s_moccasinKey] = moccasin;
                }
                return moccasin;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.NavajoWhite"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen NavajoWhite
        {
            get
            {
                Pen navajoWhite = (Pen)SafeNativeMethods.Gdip.ThreadData[s_navajoWhiteKey];
                if (navajoWhite == null)
                {
                    navajoWhite = new Pen(Color.NavajoWhite, true);
                    SafeNativeMethods.Gdip.ThreadData[s_navajoWhiteKey] = navajoWhite;
                }
                return navajoWhite;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Navy"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Navy
        {
            get
            {
                Pen navy = (Pen)SafeNativeMethods.Gdip.ThreadData[s_navyKey];
                if (navy == null)
                {
                    navy = new Pen(Color.Navy, true);
                    SafeNativeMethods.Gdip.ThreadData[s_navyKey] = navy;
                }
                return navy;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.OldLace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen OldLace
        {
            get
            {
                Pen oldLace = (Pen)SafeNativeMethods.Gdip.ThreadData[s_oldLaceKey];
                if (oldLace == null)
                {
                    oldLace = new Pen(Color.OldLace, true);
                    SafeNativeMethods.Gdip.ThreadData[s_oldLaceKey] = oldLace;
                }
                return oldLace;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Olive"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Olive
        {
            get
            {
                Pen olive = (Pen)SafeNativeMethods.Gdip.ThreadData[s_oliveKey];
                if (olive == null)
                {
                    olive = new Pen(Color.Olive, true);
                    SafeNativeMethods.Gdip.ThreadData[s_oliveKey] = olive;
                }
                return olive;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.OliveDrab"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen OliveDrab
        {
            get
            {
                Pen oliveDrab = (Pen)SafeNativeMethods.Gdip.ThreadData[s_oliveDrabKey];
                if (oliveDrab == null)
                {
                    oliveDrab = new Pen(Color.OliveDrab, true);
                    SafeNativeMethods.Gdip.ThreadData[s_oliveDrabKey] = oliveDrab;
                }
                return oliveDrab;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Orange"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Orange
        {
            get
            {
                Pen orange = (Pen)SafeNativeMethods.Gdip.ThreadData[s_orangeKey];
                if (orange == null)
                {
                    orange = new Pen(Color.Orange, true);
                    SafeNativeMethods.Gdip.ThreadData[s_orangeKey] = orange;
                }
                return orange;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.OrangeRed"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen OrangeRed
        {
            get
            {
                Pen orangeRed = (Pen)SafeNativeMethods.Gdip.ThreadData[s_orangeRedKey];
                if (orangeRed == null)
                {
                    orangeRed = new Pen(Color.OrangeRed, true);
                    SafeNativeMethods.Gdip.ThreadData[s_orangeRedKey] = orangeRed;
                }
                return orangeRed;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Orchid"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Orchid
        {
            get
            {
                Pen orchid = (Pen)SafeNativeMethods.Gdip.ThreadData[s_orchidKey];
                if (orchid == null)
                {
                    orchid = new Pen(Color.Orchid, true);
                    SafeNativeMethods.Gdip.ThreadData[s_orchidKey] = orchid;
                }
                return orchid;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.PaleGoldenrod"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen PaleGoldenrod
        {
            get
            {
                Pen paleGoldenrod = (Pen)SafeNativeMethods.Gdip.ThreadData[s_paleGoldenrodKey];
                if (paleGoldenrod == null)
                {
                    paleGoldenrod = new Pen(Color.PaleGoldenrod, true);
                    SafeNativeMethods.Gdip.ThreadData[s_paleGoldenrodKey] = paleGoldenrod;
                }
                return paleGoldenrod;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.PaleGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen PaleGreen
        {
            get
            {
                Pen paleGreen = (Pen)SafeNativeMethods.Gdip.ThreadData[s_paleGreenKey];
                if (paleGreen == null)
                {
                    paleGreen = new Pen(Color.PaleGreen, true);
                    SafeNativeMethods.Gdip.ThreadData[s_paleGreenKey] = paleGreen;
                }
                return paleGreen;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.PaleTurquoise"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen PaleTurquoise
        {
            get
            {
                Pen paleTurquoise = (Pen)SafeNativeMethods.Gdip.ThreadData[s_paleTurquoiseKey];
                if (paleTurquoise == null)
                {
                    paleTurquoise = new Pen(Color.PaleTurquoise, true);
                    SafeNativeMethods.Gdip.ThreadData[s_paleTurquoiseKey] = paleTurquoise;
                }
                return paleTurquoise;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.PaleVioletRed"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen PaleVioletRed
        {
            get
            {
                Pen paleVioletRed = (Pen)SafeNativeMethods.Gdip.ThreadData[s_paleVioletRedKey];
                if (paleVioletRed == null)
                {
                    paleVioletRed = new Pen(Color.PaleVioletRed, true);
                    SafeNativeMethods.Gdip.ThreadData[s_paleVioletRedKey] = paleVioletRed;
                }
                return paleVioletRed;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.PapayaWhip"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen PapayaWhip
        {
            get
            {
                Pen papayaWhip = (Pen)SafeNativeMethods.Gdip.ThreadData[s_papayaWhipKey];
                if (papayaWhip == null)
                {
                    papayaWhip = new Pen(Color.PapayaWhip, true);
                    SafeNativeMethods.Gdip.ThreadData[s_papayaWhipKey] = papayaWhip;
                }
                return papayaWhip;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.PeachPuff"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen PeachPuff
        {
            get
            {
                Pen peachPuff = (Pen)SafeNativeMethods.Gdip.ThreadData[s_peachPuffKey];
                if (peachPuff == null)
                {
                    peachPuff = new Pen(Color.PeachPuff, true);
                    SafeNativeMethods.Gdip.ThreadData[s_peachPuffKey] = peachPuff;
                }
                return peachPuff;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Peru"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Peru
        {
            get
            {
                Pen peru = (Pen)SafeNativeMethods.Gdip.ThreadData[s_peruKey];
                if (peru == null)
                {
                    peru = new Pen(Color.Peru, true);
                    SafeNativeMethods.Gdip.ThreadData[s_peruKey] = peru;
                }
                return peru;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Pink"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Pink
        {
            get
            {
                Pen pink = (Pen)SafeNativeMethods.Gdip.ThreadData[s_pinkKey];
                if (pink == null)
                {
                    pink = new Pen(Color.Pink, true);
                    SafeNativeMethods.Gdip.ThreadData[s_pinkKey] = pink;
                }
                return pink;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Plum"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Plum
        {
            get
            {
                Pen plum = (Pen)SafeNativeMethods.Gdip.ThreadData[s_plumKey];
                if (plum == null)
                {
                    plum = new Pen(Color.Plum, true);
                    SafeNativeMethods.Gdip.ThreadData[s_plumKey] = plum;
                }
                return plum;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.PowderBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen PowderBlue
        {
            get
            {
                Pen powderBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_powderBlueKey];
                if (powderBlue == null)
                {
                    powderBlue = new Pen(Color.PowderBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_powderBlueKey] = powderBlue;
                }
                return powderBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Purple"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Purple
        {
            get
            {
                Pen purple = (Pen)SafeNativeMethods.Gdip.ThreadData[s_purpleKey];
                if (purple == null)
                {
                    purple = new Pen(Color.Purple, true);
                    SafeNativeMethods.Gdip.ThreadData[s_purpleKey] = purple;
                }
                return purple;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Red"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Red
        {
            get
            {
                Pen red = (Pen)SafeNativeMethods.Gdip.ThreadData[s_redKey];
                if (red == null)
                {
                    red = new Pen(Color.Red, true);
                    SafeNativeMethods.Gdip.ThreadData[s_redKey] = red;
                }
                return red;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.RosyBrown"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen RosyBrown
        {
            get
            {
                Pen rosyBrown = (Pen)SafeNativeMethods.Gdip.ThreadData[s_rosyBrownKey];
                if (rosyBrown == null)
                {
                    rosyBrown = new Pen(Color.RosyBrown, true);
                    SafeNativeMethods.Gdip.ThreadData[s_rosyBrownKey] = rosyBrown;
                }
                return rosyBrown;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.RoyalBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen RoyalBlue
        {
            get
            {
                Pen royalBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_royalBlueKey];
                if (royalBlue == null)
                {
                    royalBlue = new Pen(Color.RoyalBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_royalBlueKey] = royalBlue;
                }
                return royalBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.SaddleBrown"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen SaddleBrown
        {
            get
            {
                Pen saddleBrown = (Pen)SafeNativeMethods.Gdip.ThreadData[s_saddleBrownKey];
                if (saddleBrown == null)
                {
                    saddleBrown = new Pen(Color.SaddleBrown, true);
                    SafeNativeMethods.Gdip.ThreadData[s_saddleBrownKey] = saddleBrown;
                }
                return saddleBrown;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Salmon"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Salmon
        {
            get
            {
                Pen salmon = (Pen)SafeNativeMethods.Gdip.ThreadData[s_salmonKey];
                if (salmon == null)
                {
                    salmon = new Pen(Color.Salmon, true);
                    SafeNativeMethods.Gdip.ThreadData[s_salmonKey] = salmon;
                }
                return salmon;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.SandyBrown"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen SandyBrown
        {
            get
            {
                Pen sandyBrown = (Pen)SafeNativeMethods.Gdip.ThreadData[s_sandyBrownKey];
                if (sandyBrown == null)
                {
                    sandyBrown = new Pen(Color.SandyBrown, true);
                    SafeNativeMethods.Gdip.ThreadData[s_sandyBrownKey] = sandyBrown;
                }
                return sandyBrown;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.SeaGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen SeaGreen
        {
            get
            {
                Pen seaGreen = (Pen)SafeNativeMethods.Gdip.ThreadData[s_seaGreenKey];
                if (seaGreen == null)
                {
                    seaGreen = new Pen(Color.SeaGreen, true);
                    SafeNativeMethods.Gdip.ThreadData[s_seaGreenKey] = seaGreen;
                }
                return seaGreen;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.SeaShell"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen SeaShell
        {
            get
            {
                Pen seaShell = (Pen)SafeNativeMethods.Gdip.ThreadData[s_seaShellKey];
                if (seaShell == null)
                {
                    seaShell = new Pen(Color.SeaShell, true);
                    SafeNativeMethods.Gdip.ThreadData[s_seaShellKey] = seaShell;
                }
                return seaShell;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Sienna"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Sienna
        {
            get
            {
                Pen sienna = (Pen)SafeNativeMethods.Gdip.ThreadData[s_siennaKey];
                if (sienna == null)
                {
                    sienna = new Pen(Color.Sienna, true);
                    SafeNativeMethods.Gdip.ThreadData[s_siennaKey] = sienna;
                }
                return sienna;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Silver"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Silver
        {
            get
            {
                Pen silver = (Pen)SafeNativeMethods.Gdip.ThreadData[s_silverKey];
                if (silver == null)
                {
                    silver = new Pen(Color.Silver, true);
                    SafeNativeMethods.Gdip.ThreadData[s_silverKey] = silver;
                }
                return silver;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.SkyBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen SkyBlue
        {
            get
            {
                Pen skyBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_skyBlueKey];
                if (skyBlue == null)
                {
                    skyBlue = new Pen(Color.SkyBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_skyBlueKey] = skyBlue;
                }
                return skyBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.SlateBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen SlateBlue
        {
            get
            {
                Pen slateBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_slateBlueKey];
                if (slateBlue == null)
                {
                    slateBlue = new Pen(Color.SlateBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_slateBlueKey] = slateBlue;
                }
                return slateBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.SlateGray"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen SlateGray
        {
            get
            {
                Pen slateGray = (Pen)SafeNativeMethods.Gdip.ThreadData[s_slateGrayKey];
                if (slateGray == null)
                {
                    slateGray = new Pen(Color.SlateGray, true);
                    SafeNativeMethods.Gdip.ThreadData[s_slateGrayKey] = slateGray;
                }
                return slateGray;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Snow"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Snow
        {
            get
            {
                Pen snow = (Pen)SafeNativeMethods.Gdip.ThreadData[s_snowKey];
                if (snow == null)
                {
                    snow = new Pen(Color.Snow, true);
                    SafeNativeMethods.Gdip.ThreadData[s_snowKey] = snow;
                }
                return snow;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.SpringGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen SpringGreen
        {
            get
            {
                Pen springGreen = (Pen)SafeNativeMethods.Gdip.ThreadData[s_springGreenKey];
                if (springGreen == null)
                {
                    springGreen = new Pen(Color.SpringGreen, true);
                    SafeNativeMethods.Gdip.ThreadData[s_springGreenKey] = springGreen;
                }
                return springGreen;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.SteelBlue"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen SteelBlue
        {
            get
            {
                Pen steelBlue = (Pen)SafeNativeMethods.Gdip.ThreadData[s_steelBlueKey];
                if (steelBlue == null)
                {
                    steelBlue = new Pen(Color.SteelBlue, true);
                    SafeNativeMethods.Gdip.ThreadData[s_steelBlueKey] = steelBlue;
                }
                return steelBlue;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Tan"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Tan
        {
            get
            {
                Pen tan = (Pen)SafeNativeMethods.Gdip.ThreadData[s_tanKey];
                if (tan == null)
                {
                    tan = new Pen(Color.Tan, true);
                    SafeNativeMethods.Gdip.ThreadData[s_tanKey] = tan;
                }
                return tan;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Teal"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Teal
        {
            get
            {
                Pen teal = (Pen)SafeNativeMethods.Gdip.ThreadData[s_tealKey];
                if (teal == null)
                {
                    teal = new Pen(Color.Teal, true);
                    SafeNativeMethods.Gdip.ThreadData[s_tealKey] = teal;
                }
                return teal;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Thistle"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Thistle
        {
            get
            {
                Pen thistle = (Pen)SafeNativeMethods.Gdip.ThreadData[s_thistleKey];
                if (thistle == null)
                {
                    thistle = new Pen(Color.Thistle, true);
                    SafeNativeMethods.Gdip.ThreadData[s_thistleKey] = thistle;
                }
                return thistle;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Tomato"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Tomato
        {
            get
            {
                Pen tomato = (Pen)SafeNativeMethods.Gdip.ThreadData[s_tomatoKey];
                if (tomato == null)
                {
                    tomato = new Pen(Color.Tomato, true);
                    SafeNativeMethods.Gdip.ThreadData[s_tomatoKey] = tomato;
                }
                return tomato;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Turquoise"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Turquoise
        {
            get
            {
                Pen turquoise = (Pen)SafeNativeMethods.Gdip.ThreadData[s_turquoiseKey];
                if (turquoise == null)
                {
                    turquoise = new Pen(Color.Turquoise, true);
                    SafeNativeMethods.Gdip.ThreadData[s_turquoiseKey] = turquoise;
                }
                return turquoise;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Violet"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Violet
        {
            get
            {
                Pen violet = (Pen)SafeNativeMethods.Gdip.ThreadData[s_violetKey];
                if (violet == null)
                {
                    violet = new Pen(Color.Violet, true);
                    SafeNativeMethods.Gdip.ThreadData[s_violetKey] = violet;
                }
                return violet;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Wheat"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Wheat
        {
            get
            {
                Pen wheat = (Pen)SafeNativeMethods.Gdip.ThreadData[s_wheatKey];
                if (wheat == null)
                {
                    wheat = new Pen(Color.Wheat, true);
                    SafeNativeMethods.Gdip.ThreadData[s_wheatKey] = wheat;
                }
                return wheat;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.White"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen White
        {
            get
            {
                Pen white = (Pen)SafeNativeMethods.Gdip.ThreadData[s_whiteKey];
                if (white == null)
                {
                    white = new Pen(Color.White, true);
                    SafeNativeMethods.Gdip.ThreadData[s_whiteKey] = white;
                }
                return white;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.WhiteSmoke"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen WhiteSmoke
        {
            get
            {
                Pen whiteSmoke = (Pen)SafeNativeMethods.Gdip.ThreadData[s_whiteSmokeKey];
                if (whiteSmoke == null)
                {
                    whiteSmoke = new Pen(Color.WhiteSmoke, true);
                    SafeNativeMethods.Gdip.ThreadData[s_whiteSmokeKey] = whiteSmoke;
                }
                return whiteSmoke;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.Yellow"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen Yellow
        {
            get
            {
                Pen yellow = (Pen)SafeNativeMethods.Gdip.ThreadData[s_yellowKey];
                if (yellow == null)
                {
                    yellow = new Pen(Color.Yellow, true);
                    SafeNativeMethods.Gdip.ThreadData[s_yellowKey] = yellow;
                }
                return yellow;
            }
        }

        /// <include file='doc\Pens.uex' path='docs/doc[@for="Pens.YellowGreen"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen YellowGreen
        {
            get
            {
                Pen yellowGreen = (Pen)SafeNativeMethods.Gdip.ThreadData[s_yellowGreenKey];
                if (yellowGreen == null)
                {
                    yellowGreen = new Pen(Color.YellowGreen, true);
                    SafeNativeMethods.Gdip.ThreadData[s_yellowGreenKey] = yellowGreen;
                }
                return yellowGreen;
            }
        }
    }
}
