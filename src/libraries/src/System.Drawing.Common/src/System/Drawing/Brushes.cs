// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes"]/*' />
    /// <devdoc>
    ///     Brushes for all the standard colors.
    /// </devdoc>
    public sealed class Brushes
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

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Transparent"]/*' />
        /// <devdoc>
        ///    A transparent brush.
        /// </devdoc>
        public static Brush Transparent
        {
            get
            {
                Brush transparent = (Brush)SafeNativeMethods.Gdip.ThreadData[s_transparentKey];
                if (transparent == null)
                {
                    transparent = new SolidBrush(Color.Transparent);
                    SafeNativeMethods.Gdip.ThreadData[s_transparentKey] = transparent;
                }
                return transparent;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.AliceBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush AliceBlue
        {
            get
            {
                Brush aliceBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_aliceBlueKey];
                if (aliceBlue == null)
                {
                    aliceBlue = new SolidBrush(Color.AliceBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_aliceBlueKey] = aliceBlue;
                }
                return aliceBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.AntiqueWhite"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush AntiqueWhite
        {
            get
            {
                Brush antiqueWhite = (Brush)SafeNativeMethods.Gdip.ThreadData[s_antiqueWhiteKey];
                if (antiqueWhite == null)
                {
                    antiqueWhite = new SolidBrush(Color.AntiqueWhite);
                    SafeNativeMethods.Gdip.ThreadData[s_antiqueWhiteKey] = antiqueWhite;
                }
                return antiqueWhite;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Aqua"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Aqua
        {
            get
            {
                Brush aqua = (Brush)SafeNativeMethods.Gdip.ThreadData[s_aquaKey];
                if (aqua == null)
                {
                    aqua = new SolidBrush(Color.Aqua);
                    SafeNativeMethods.Gdip.ThreadData[s_aquaKey] = aqua;
                }
                return aqua;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Aquamarine"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Aquamarine
        {
            get
            {
                Brush aquamarine = (Brush)SafeNativeMethods.Gdip.ThreadData[s_aquamarineKey];
                if (aquamarine == null)
                {
                    aquamarine = new SolidBrush(Color.Aquamarine);
                    SafeNativeMethods.Gdip.ThreadData[s_aquamarineKey] = aquamarine;
                }
                return aquamarine;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Azure"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Azure
        {
            get
            {
                Brush azure = (Brush)SafeNativeMethods.Gdip.ThreadData[s_azureKey];
                if (azure == null)
                {
                    azure = new SolidBrush(Color.Azure);
                    SafeNativeMethods.Gdip.ThreadData[s_azureKey] = azure;
                }
                return azure;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Beige"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Beige
        {
            get
            {
                Brush beige = (Brush)SafeNativeMethods.Gdip.ThreadData[s_beigeKey];
                if (beige == null)
                {
                    beige = new SolidBrush(Color.Beige);
                    SafeNativeMethods.Gdip.ThreadData[s_beigeKey] = beige;
                }
                return beige;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Bisque"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Bisque
        {
            get
            {
                Brush bisque = (Brush)SafeNativeMethods.Gdip.ThreadData[s_bisqueKey];
                if (bisque == null)
                {
                    bisque = new SolidBrush(Color.Bisque);
                    SafeNativeMethods.Gdip.ThreadData[s_bisqueKey] = bisque;
                }
                return bisque;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Black"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Black
        {
            get
            {
                Brush black = (Brush)SafeNativeMethods.Gdip.ThreadData[s_blackKey];
                if (black == null)
                {
                    black = new SolidBrush(Color.Black);
                    SafeNativeMethods.Gdip.ThreadData[s_blackKey] = black;
                }
                return black;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.BlanchedAlmond"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush BlanchedAlmond
        {
            get
            {
                Brush blanchedAlmond = (Brush)SafeNativeMethods.Gdip.ThreadData[s_blanchedAlmondKey];
                if (blanchedAlmond == null)
                {
                    blanchedAlmond = new SolidBrush(Color.BlanchedAlmond);
                    SafeNativeMethods.Gdip.ThreadData[s_blanchedAlmondKey] = blanchedAlmond;
                }
                return blanchedAlmond;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Blue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Blue
        {
            get
            {
                Brush blue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_blueKey];
                if (blue == null)
                {
                    blue = new SolidBrush(Color.Blue);
                    SafeNativeMethods.Gdip.ThreadData[s_blueKey] = blue;
                }
                return blue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.BlueViolet"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush BlueViolet
        {
            get
            {
                Brush blueViolet = (Brush)SafeNativeMethods.Gdip.ThreadData[s_blueVioletKey];
                if (blueViolet == null)
                {
                    blueViolet = new SolidBrush(Color.BlueViolet);
                    SafeNativeMethods.Gdip.ThreadData[s_blueVioletKey] = blueViolet;
                }
                return blueViolet;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Brown"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Brown
        {
            get
            {
                Brush brown = (Brush)SafeNativeMethods.Gdip.ThreadData[s_brownKey];
                if (brown == null)
                {
                    brown = new SolidBrush(Color.Brown);
                    SafeNativeMethods.Gdip.ThreadData[s_brownKey] = brown;
                }
                return brown;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.BurlyWood"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush BurlyWood
        {
            get
            {
                Brush burlyWood = (Brush)SafeNativeMethods.Gdip.ThreadData[s_burlyWoodKey];
                if (burlyWood == null)
                {
                    burlyWood = new SolidBrush(Color.BurlyWood);
                    SafeNativeMethods.Gdip.ThreadData[s_burlyWoodKey] = burlyWood;
                }
                return burlyWood;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.CadetBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush CadetBlue
        {
            get
            {
                Brush cadetBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_cadetBlueKey];
                if (cadetBlue == null)
                {
                    cadetBlue = new SolidBrush(Color.CadetBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_cadetBlueKey] = cadetBlue;
                }
                return cadetBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Chartreuse"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Chartreuse
        {
            get
            {
                Brush chartreuse = (Brush)SafeNativeMethods.Gdip.ThreadData[s_chartreuseKey];
                if (chartreuse == null)
                {
                    chartreuse = new SolidBrush(Color.Chartreuse);
                    SafeNativeMethods.Gdip.ThreadData[s_chartreuseKey] = chartreuse;
                }
                return chartreuse;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Chocolate"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Chocolate
        {
            get
            {
                Brush chocolate = (Brush)SafeNativeMethods.Gdip.ThreadData[s_chocolateKey];
                if (chocolate == null)
                {
                    chocolate = new SolidBrush(Color.Chocolate);
                    SafeNativeMethods.Gdip.ThreadData[s_chocolateKey] = chocolate;
                }
                return chocolate;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Coral"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Coral
        {
            get
            {
                Brush choral = (Brush)SafeNativeMethods.Gdip.ThreadData[s_choralKey];
                if (choral == null)
                {
                    choral = new SolidBrush(Color.Coral);
                    SafeNativeMethods.Gdip.ThreadData[s_choralKey] = choral;
                }
                return choral;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.CornflowerBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush CornflowerBlue
        {
            get
            {
                Brush cornflowerBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_cornflowerBlueKey];
                if (cornflowerBlue == null)
                {
                    cornflowerBlue = new SolidBrush(Color.CornflowerBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_cornflowerBlueKey] = cornflowerBlue;
                }
                return cornflowerBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Cornsilk"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Cornsilk
        {
            get
            {
                Brush cornsilk = (Brush)SafeNativeMethods.Gdip.ThreadData[s_cornsilkKey];
                if (cornsilk == null)
                {
                    cornsilk = new SolidBrush(Color.Cornsilk);
                    SafeNativeMethods.Gdip.ThreadData[s_cornsilkKey] = cornsilk;
                }
                return cornsilk;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Crimson"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Crimson
        {
            get
            {
                Brush crimson = (Brush)SafeNativeMethods.Gdip.ThreadData[s_crimsonKey];
                if (crimson == null)
                {
                    crimson = new SolidBrush(Color.Crimson);
                    SafeNativeMethods.Gdip.ThreadData[s_crimsonKey] = crimson;
                }
                return crimson;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Cyan"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Cyan
        {
            get
            {
                Brush cyan = (Brush)SafeNativeMethods.Gdip.ThreadData[s_cyanKey];
                if (cyan == null)
                {
                    cyan = new SolidBrush(Color.Cyan);
                    SafeNativeMethods.Gdip.ThreadData[s_cyanKey] = cyan;
                }
                return cyan;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkBlue
        {
            get
            {
                Brush darkBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkBlueKey];
                if (darkBlue == null)
                {
                    darkBlue = new SolidBrush(Color.DarkBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_darkBlueKey] = darkBlue;
                }
                return darkBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkCyan"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkCyan
        {
            get
            {
                Brush darkCyan = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkCyanKey];
                if (darkCyan == null)
                {
                    darkCyan = new SolidBrush(Color.DarkCyan);
                    SafeNativeMethods.Gdip.ThreadData[s_darkCyanKey] = darkCyan;
                }
                return darkCyan;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkGoldenrod"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkGoldenrod
        {
            get
            {
                Brush darkGoldenrod = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkGoldenrodKey];
                if (darkGoldenrod == null)
                {
                    darkGoldenrod = new SolidBrush(Color.DarkGoldenrod);
                    SafeNativeMethods.Gdip.ThreadData[s_darkGoldenrodKey] = darkGoldenrod;
                }
                return darkGoldenrod;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkGray"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkGray
        {
            get
            {
                Brush darkGray = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkGrayKey];
                if (darkGray == null)
                {
                    darkGray = new SolidBrush(Color.DarkGray);
                    SafeNativeMethods.Gdip.ThreadData[s_darkGrayKey] = darkGray;
                }
                return darkGray;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkGreen"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkGreen
        {
            get
            {
                Brush darkGreen = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkGreenKey];
                if (darkGreen == null)
                {
                    darkGreen = new SolidBrush(Color.DarkGreen);
                    SafeNativeMethods.Gdip.ThreadData[s_darkGreenKey] = darkGreen;
                }
                return darkGreen;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkKhaki"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkKhaki
        {
            get
            {
                Brush darkKhaki = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkKhakiKey];
                if (darkKhaki == null)
                {
                    darkKhaki = new SolidBrush(Color.DarkKhaki);
                    SafeNativeMethods.Gdip.ThreadData[s_darkKhakiKey] = darkKhaki;
                }
                return darkKhaki;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkMagenta"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkMagenta
        {
            get
            {
                Brush darkMagenta = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkMagentaKey];
                if (darkMagenta == null)
                {
                    darkMagenta = new SolidBrush(Color.DarkMagenta);
                    SafeNativeMethods.Gdip.ThreadData[s_darkMagentaKey] = darkMagenta;
                }
                return darkMagenta;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkOliveGreen"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkOliveGreen
        {
            get
            {
                Brush darkOliveGreen = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkOliveGreenKey];
                if (darkOliveGreen == null)
                {
                    darkOliveGreen = new SolidBrush(Color.DarkOliveGreen);
                    SafeNativeMethods.Gdip.ThreadData[s_darkOliveGreenKey] = darkOliveGreen;
                }
                return darkOliveGreen;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkOrange"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkOrange
        {
            get
            {
                Brush darkOrange = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkOrangeKey];
                if (darkOrange == null)
                {
                    darkOrange = new SolidBrush(Color.DarkOrange);
                    SafeNativeMethods.Gdip.ThreadData[s_darkOrangeKey] = darkOrange;
                }
                return darkOrange;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkOrchid"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkOrchid
        {
            get
            {
                Brush darkOrchid = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkOrchidKey];
                if (darkOrchid == null)
                {
                    darkOrchid = new SolidBrush(Color.DarkOrchid);
                    SafeNativeMethods.Gdip.ThreadData[s_darkOrchidKey] = darkOrchid;
                }
                return darkOrchid;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkRed"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkRed
        {
            get
            {
                Brush darkRed = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkRedKey];
                if (darkRed == null)
                {
                    darkRed = new SolidBrush(Color.DarkRed);
                    SafeNativeMethods.Gdip.ThreadData[s_darkRedKey] = darkRed;
                }
                return darkRed;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkSalmon"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkSalmon
        {
            get
            {
                Brush darkSalmon = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkSalmonKey];
                if (darkSalmon == null)
                {
                    darkSalmon = new SolidBrush(Color.DarkSalmon);
                    SafeNativeMethods.Gdip.ThreadData[s_darkSalmonKey] = darkSalmon;
                }
                return darkSalmon;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkSeaGreen"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkSeaGreen
        {
            get
            {
                Brush darkSeaGreen = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkSeaGreenKey];
                if (darkSeaGreen == null)
                {
                    darkSeaGreen = new SolidBrush(Color.DarkSeaGreen);
                    SafeNativeMethods.Gdip.ThreadData[s_darkSeaGreenKey] = darkSeaGreen;
                }
                return darkSeaGreen;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkSlateBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkSlateBlue
        {
            get
            {
                Brush darkSlateBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkSlateBlueKey];
                if (darkSlateBlue == null)
                {
                    darkSlateBlue = new SolidBrush(Color.DarkSlateBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_darkSlateBlueKey] = darkSlateBlue;
                }
                return darkSlateBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkSlateGray"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkSlateGray
        {
            get
            {
                Brush darkSlateGray = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkSlateGrayKey];
                if (darkSlateGray == null)
                {
                    darkSlateGray = new SolidBrush(Color.DarkSlateGray);
                    SafeNativeMethods.Gdip.ThreadData[s_darkSlateGrayKey] = darkSlateGray;
                }
                return darkSlateGray;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkTurquoise"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkTurquoise
        {
            get
            {
                Brush darkTurquoise = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkTurquoiseKey];
                if (darkTurquoise == null)
                {
                    darkTurquoise = new SolidBrush(Color.DarkTurquoise);
                    SafeNativeMethods.Gdip.ThreadData[s_darkTurquoiseKey] = darkTurquoise;
                }
                return darkTurquoise;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DarkViolet"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DarkViolet
        {
            get
            {
                Brush darkViolet = (Brush)SafeNativeMethods.Gdip.ThreadData[s_darkVioletKey];
                if (darkViolet == null)
                {
                    darkViolet = new SolidBrush(Color.DarkViolet);
                    SafeNativeMethods.Gdip.ThreadData[s_darkVioletKey] = darkViolet;
                }
                return darkViolet;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DeepPink"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DeepPink
        {
            get
            {
                Brush deepPink = (Brush)SafeNativeMethods.Gdip.ThreadData[s_deepPinkKey];
                if (deepPink == null)
                {
                    deepPink = new SolidBrush(Color.DeepPink);
                    SafeNativeMethods.Gdip.ThreadData[s_deepPinkKey] = deepPink;
                }
                return deepPink;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DeepSkyBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DeepSkyBlue
        {
            get
            {
                Brush deepSkyBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_deepSkyBlueKey];
                if (deepSkyBlue == null)
                {
                    deepSkyBlue = new SolidBrush(Color.DeepSkyBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_deepSkyBlueKey] = deepSkyBlue;
                }
                return deepSkyBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DimGray"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DimGray
        {
            get
            {
                Brush dimGray = (Brush)SafeNativeMethods.Gdip.ThreadData[s_dimGrayKey];
                if (dimGray == null)
                {
                    dimGray = new SolidBrush(Color.DimGray);
                    SafeNativeMethods.Gdip.ThreadData[s_dimGrayKey] = dimGray;
                }
                return dimGray;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.DodgerBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush DodgerBlue
        {
            get
            {
                Brush dodgerBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_dodgerBlueKey];
                if (dodgerBlue == null)
                {
                    dodgerBlue = new SolidBrush(Color.DodgerBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_dodgerBlueKey] = dodgerBlue;
                }
                return dodgerBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Firebrick"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Firebrick
        {
            get
            {
                Brush firebrick = (Brush)SafeNativeMethods.Gdip.ThreadData[s_firebrickKey];
                if (firebrick == null)
                {
                    firebrick = new SolidBrush(Color.Firebrick);
                    SafeNativeMethods.Gdip.ThreadData[s_firebrickKey] = firebrick;
                }
                return firebrick;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.FloralWhite"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush FloralWhite
        {
            get
            {
                Brush floralWhite = (Brush)SafeNativeMethods.Gdip.ThreadData[s_floralWhiteKey];
                if (floralWhite == null)
                {
                    floralWhite = new SolidBrush(Color.FloralWhite);
                    SafeNativeMethods.Gdip.ThreadData[s_floralWhiteKey] = floralWhite;
                }
                return floralWhite;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.ForestGreen"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush ForestGreen
        {
            get
            {
                Brush forestGreen = (Brush)SafeNativeMethods.Gdip.ThreadData[s_forestGreenKey];
                if (forestGreen == null)
                {
                    forestGreen = new SolidBrush(Color.ForestGreen);
                    SafeNativeMethods.Gdip.ThreadData[s_forestGreenKey] = forestGreen;
                }
                return forestGreen;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Fuchsia"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Fuchsia
        {
            get
            {
                Brush fuchia = (Brush)SafeNativeMethods.Gdip.ThreadData[s_fuchiaKey];
                if (fuchia == null)
                {
                    fuchia = new SolidBrush(Color.Fuchsia);
                    SafeNativeMethods.Gdip.ThreadData[s_fuchiaKey] = fuchia;
                }
                return fuchia;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Gainsboro"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Gainsboro
        {
            get
            {
                Brush gainsboro = (Brush)SafeNativeMethods.Gdip.ThreadData[s_gainsboroKey];
                if (gainsboro == null)
                {
                    gainsboro = new SolidBrush(Color.Gainsboro);
                    SafeNativeMethods.Gdip.ThreadData[s_gainsboroKey] = gainsboro;
                }
                return gainsboro;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.GhostWhite"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush GhostWhite
        {
            get
            {
                Brush ghostWhite = (Brush)SafeNativeMethods.Gdip.ThreadData[s_ghostWhiteKey];
                if (ghostWhite == null)
                {
                    ghostWhite = new SolidBrush(Color.GhostWhite);
                    SafeNativeMethods.Gdip.ThreadData[s_ghostWhiteKey] = ghostWhite;
                }
                return ghostWhite;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Gold"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Gold
        {
            get
            {
                Brush gold = (Brush)SafeNativeMethods.Gdip.ThreadData[s_goldKey];
                if (gold == null)
                {
                    gold = new SolidBrush(Color.Gold);
                    SafeNativeMethods.Gdip.ThreadData[s_goldKey] = gold;
                }
                return gold;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Goldenrod"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Goldenrod
        {
            get
            {
                Brush goldenrod = (Brush)SafeNativeMethods.Gdip.ThreadData[s_goldenrodKey];
                if (goldenrod == null)
                {
                    goldenrod = new SolidBrush(Color.Goldenrod);
                    SafeNativeMethods.Gdip.ThreadData[s_goldenrodKey] = goldenrod;
                }
                return goldenrod;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Gray"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Gray
        {
            get
            {
                Brush gray = (Brush)SafeNativeMethods.Gdip.ThreadData[s_grayKey];
                if (gray == null)
                {
                    gray = new SolidBrush(Color.Gray);
                    SafeNativeMethods.Gdip.ThreadData[s_grayKey] = gray;
                }
                return gray;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Green"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Green
        {
            get
            {
                Brush green = (Brush)SafeNativeMethods.Gdip.ThreadData[s_greenKey];
                if (green == null)
                {
                    green = new SolidBrush(Color.Green);
                    SafeNativeMethods.Gdip.ThreadData[s_greenKey] = green;
                }
                return green;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.GreenYellow"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush GreenYellow
        {
            get
            {
                Brush greenYellow = (Brush)SafeNativeMethods.Gdip.ThreadData[s_greenYellowKey];
                if (greenYellow == null)
                {
                    greenYellow = new SolidBrush(Color.GreenYellow);
                    SafeNativeMethods.Gdip.ThreadData[s_greenYellowKey] = greenYellow;
                }
                return greenYellow;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Honeydew"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Honeydew
        {
            get
            {
                Brush honeydew = (Brush)SafeNativeMethods.Gdip.ThreadData[s_honeydewKey];
                if (honeydew == null)
                {
                    honeydew = new SolidBrush(Color.Honeydew);
                    SafeNativeMethods.Gdip.ThreadData[s_honeydewKey] = honeydew;
                }
                return honeydew;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.HotPink"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush HotPink
        {
            get
            {
                Brush hotPink = (Brush)SafeNativeMethods.Gdip.ThreadData[s_hotPinkKey];
                if (hotPink == null)
                {
                    hotPink = new SolidBrush(Color.HotPink);
                    SafeNativeMethods.Gdip.ThreadData[s_hotPinkKey] = hotPink;
                }
                return hotPink;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.IndianRed"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush IndianRed
        {
            get
            {
                Brush indianRed = (Brush)SafeNativeMethods.Gdip.ThreadData[s_indianRedKey];
                if (indianRed == null)
                {
                    indianRed = new SolidBrush(Color.IndianRed);
                    SafeNativeMethods.Gdip.ThreadData[s_indianRedKey] = indianRed;
                }
                return indianRed;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Indigo"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Indigo
        {
            get
            {
                Brush indigo = (Brush)SafeNativeMethods.Gdip.ThreadData[s_indigoKey];
                if (indigo == null)
                {
                    indigo = new SolidBrush(Color.Indigo);
                    SafeNativeMethods.Gdip.ThreadData[s_indigoKey] = indigo;
                }
                return indigo;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Ivory"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Ivory
        {
            get
            {
                Brush ivory = (Brush)SafeNativeMethods.Gdip.ThreadData[s_ivoryKey];
                if (ivory == null)
                {
                    ivory = new SolidBrush(Color.Ivory);
                    SafeNativeMethods.Gdip.ThreadData[s_ivoryKey] = ivory;
                }
                return ivory;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Khaki"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Khaki
        {
            get
            {
                Brush khaki = (Brush)SafeNativeMethods.Gdip.ThreadData[s_khakiKey];
                if (khaki == null)
                {
                    khaki = new SolidBrush(Color.Khaki);
                    SafeNativeMethods.Gdip.ThreadData[s_khakiKey] = khaki;
                }
                return khaki;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Lavender"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Lavender
        {
            get
            {
                Brush lavender = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lavenderKey];
                if (lavender == null)
                {
                    lavender = new SolidBrush(Color.Lavender);
                    SafeNativeMethods.Gdip.ThreadData[s_lavenderKey] = lavender;
                }
                return lavender;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LavenderBlush"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LavenderBlush
        {
            get
            {
                Brush lavenderBlush = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lavenderBlushKey];
                if (lavenderBlush == null)
                {
                    lavenderBlush = new SolidBrush(Color.LavenderBlush);
                    SafeNativeMethods.Gdip.ThreadData[s_lavenderBlushKey] = lavenderBlush;
                }
                return lavenderBlush;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LawnGreen"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LawnGreen
        {
            get
            {
                Brush lawnGreen = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lawnGreenKey];
                if (lawnGreen == null)
                {
                    lawnGreen = new SolidBrush(Color.LawnGreen);
                    SafeNativeMethods.Gdip.ThreadData[s_lawnGreenKey] = lawnGreen;
                }
                return lawnGreen;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LemonChiffon"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LemonChiffon
        {
            get
            {
                Brush lemonChiffon = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lemonChiffonKey];
                if (lemonChiffon == null)
                {
                    lemonChiffon = new SolidBrush(Color.LemonChiffon);
                    SafeNativeMethods.Gdip.ThreadData[s_lemonChiffonKey] = lemonChiffon;
                }
                return lemonChiffon;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LightBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LightBlue
        {
            get
            {
                Brush lightBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lightBlueKey];
                if (lightBlue == null)
                {
                    lightBlue = new SolidBrush(Color.LightBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_lightBlueKey] = lightBlue;
                }
                return lightBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LightCoral"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LightCoral
        {
            get
            {
                Brush lightCoral = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lightCoralKey];
                if (lightCoral == null)
                {
                    lightCoral = new SolidBrush(Color.LightCoral);
                    SafeNativeMethods.Gdip.ThreadData[s_lightCoralKey] = lightCoral;
                }
                return lightCoral;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LightCyan"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LightCyan
        {
            get
            {
                Brush lightCyan = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lightCyanKey];
                if (lightCyan == null)
                {
                    lightCyan = new SolidBrush(Color.LightCyan);
                    SafeNativeMethods.Gdip.ThreadData[s_lightCyanKey] = lightCyan;
                }
                return lightCyan;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LightGoldenrodYellow"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LightGoldenrodYellow
        {
            get
            {
                Brush lightGoldenrodYellow = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lightGoldenrodYellowKey];
                if (lightGoldenrodYellow == null)
                {
                    lightGoldenrodYellow = new SolidBrush(Color.LightGoldenrodYellow);
                    SafeNativeMethods.Gdip.ThreadData[s_lightGoldenrodYellowKey] = lightGoldenrodYellow;
                }
                return lightGoldenrodYellow;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LightGreen"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LightGreen
        {
            get
            {
                Brush lightGreen = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lightGreenKey];
                if (lightGreen == null)
                {
                    lightGreen = new SolidBrush(Color.LightGreen);
                    SafeNativeMethods.Gdip.ThreadData[s_lightGreenKey] = lightGreen;
                }
                return lightGreen;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LightGray"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LightGray
        {
            get
            {
                Brush lightGray = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lightGrayKey];
                if (lightGray == null)
                {
                    lightGray = new SolidBrush(Color.LightGray);
                    SafeNativeMethods.Gdip.ThreadData[s_lightGrayKey] = lightGray;
                }
                return lightGray;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LightPink"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LightPink
        {
            get
            {
                Brush lightPink = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lightPinkKey];
                if (lightPink == null)
                {
                    lightPink = new SolidBrush(Color.LightPink);
                    SafeNativeMethods.Gdip.ThreadData[s_lightPinkKey] = lightPink;
                }
                return lightPink;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LightSalmon"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LightSalmon
        {
            get
            {
                Brush lightSalmon = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lightSalmonKey];
                if (lightSalmon == null)
                {
                    lightSalmon = new SolidBrush(Color.LightSalmon);
                    SafeNativeMethods.Gdip.ThreadData[s_lightSalmonKey] = lightSalmon;
                }
                return lightSalmon;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LightSeaGreen"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LightSeaGreen
        {
            get
            {
                Brush lightSeaGreen = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lightSeaGreenKey];
                if (lightSeaGreen == null)
                {
                    lightSeaGreen = new SolidBrush(Color.LightSeaGreen);
                    SafeNativeMethods.Gdip.ThreadData[s_lightSeaGreenKey] = lightSeaGreen;
                }
                return lightSeaGreen;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LightSkyBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LightSkyBlue
        {
            get
            {
                Brush lightSkyBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lightSkyBlueKey];
                if (lightSkyBlue == null)
                {
                    lightSkyBlue = new SolidBrush(Color.LightSkyBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_lightSkyBlueKey] = lightSkyBlue;
                }
                return lightSkyBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LightSlateGray"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LightSlateGray
        {
            get
            {
                Brush lightSlateGray = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lightSlateGrayKey];
                if (lightSlateGray == null)
                {
                    lightSlateGray = new SolidBrush(Color.LightSlateGray);
                    SafeNativeMethods.Gdip.ThreadData[s_lightSlateGrayKey] = lightSlateGray;
                }
                return lightSlateGray;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LightSteelBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LightSteelBlue
        {
            get
            {
                Brush lightSteelBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lightSteelBlueKey];
                if (lightSteelBlue == null)
                {
                    lightSteelBlue = new SolidBrush(Color.LightSteelBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_lightSteelBlueKey] = lightSteelBlue;
                }
                return lightSteelBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LightYellow"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LightYellow
        {
            get
            {
                Brush lightYellow = (Brush)SafeNativeMethods.Gdip.ThreadData[s_lightYellowKey];
                if (lightYellow == null)
                {
                    lightYellow = new SolidBrush(Color.LightYellow);
                    SafeNativeMethods.Gdip.ThreadData[s_lightYellowKey] = lightYellow;
                }
                return lightYellow;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Lime"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Lime
        {
            get
            {
                Brush lime = (Brush)SafeNativeMethods.Gdip.ThreadData[s_limeKey];
                if (lime == null)
                {
                    lime = new SolidBrush(Color.Lime);
                    SafeNativeMethods.Gdip.ThreadData[s_limeKey] = lime;
                }
                return lime;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.LimeGreen"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush LimeGreen
        {
            get
            {
                Brush limeGreen = (Brush)SafeNativeMethods.Gdip.ThreadData[s_limeGreenKey];
                if (limeGreen == null)
                {
                    limeGreen = new SolidBrush(Color.LimeGreen);
                    SafeNativeMethods.Gdip.ThreadData[s_limeGreenKey] = limeGreen;
                }
                return limeGreen;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Linen"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Linen
        {
            get
            {
                Brush linen = (Brush)SafeNativeMethods.Gdip.ThreadData[s_linenKey];
                if (linen == null)
                {
                    linen = new SolidBrush(Color.Linen);
                    SafeNativeMethods.Gdip.ThreadData[s_linenKey] = linen;
                }
                return linen;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Magenta"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Magenta
        {
            get
            {
                Brush magenta = (Brush)SafeNativeMethods.Gdip.ThreadData[s_magentaKey];
                if (magenta == null)
                {
                    magenta = new SolidBrush(Color.Magenta);
                    SafeNativeMethods.Gdip.ThreadData[s_magentaKey] = magenta;
                }
                return magenta;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Maroon"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Maroon
        {
            get
            {
                Brush maroon = (Brush)SafeNativeMethods.Gdip.ThreadData[s_maroonKey];
                if (maroon == null)
                {
                    maroon = new SolidBrush(Color.Maroon);
                    SafeNativeMethods.Gdip.ThreadData[s_maroonKey] = maroon;
                }
                return maroon;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.MediumAquamarine"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush MediumAquamarine
        {
            get
            {
                Brush mediumAquamarine = (Brush)SafeNativeMethods.Gdip.ThreadData[s_mediumAquamarineKey];
                if (mediumAquamarine == null)
                {
                    mediumAquamarine = new SolidBrush(Color.MediumAquamarine);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumAquamarineKey] = mediumAquamarine;
                }
                return mediumAquamarine;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.MediumBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush MediumBlue
        {
            get
            {
                Brush mediumBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_mediumBlueKey];
                if (mediumBlue == null)
                {
                    mediumBlue = new SolidBrush(Color.MediumBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumBlueKey] = mediumBlue;
                }
                return mediumBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.MediumOrchid"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush MediumOrchid
        {
            get
            {
                Brush mediumOrchid = (Brush)SafeNativeMethods.Gdip.ThreadData[s_mediumOrchidKey];
                if (mediumOrchid == null)
                {
                    mediumOrchid = new SolidBrush(Color.MediumOrchid);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumOrchidKey] = mediumOrchid;
                }
                return mediumOrchid;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.MediumPurple"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush MediumPurple
        {
            get
            {
                Brush mediumPurple = (Brush)SafeNativeMethods.Gdip.ThreadData[s_mediumPurpleKey];
                if (mediumPurple == null)
                {
                    mediumPurple = new SolidBrush(Color.MediumPurple);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumPurpleKey] = mediumPurple;
                }
                return mediumPurple;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.MediumSeaGreen"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush MediumSeaGreen
        {
            get
            {
                Brush mediumSeaGreen = (Brush)SafeNativeMethods.Gdip.ThreadData[s_mediumSeaGreenKey];
                if (mediumSeaGreen == null)
                {
                    mediumSeaGreen = new SolidBrush(Color.MediumSeaGreen);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumSeaGreenKey] = mediumSeaGreen;
                }
                return mediumSeaGreen;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.MediumSlateBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush MediumSlateBlue
        {
            get
            {
                Brush mediumSlateBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_mediumSlateBlueKey];
                if (mediumSlateBlue == null)
                {
                    mediumSlateBlue = new SolidBrush(Color.MediumSlateBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumSlateBlueKey] = mediumSlateBlue;
                }
                return mediumSlateBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.MediumSpringGreen"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush MediumSpringGreen
        {
            get
            {
                Brush mediumSpringGreen = (Brush)SafeNativeMethods.Gdip.ThreadData[s_mediumSpringGreenKey];
                if (mediumSpringGreen == null)
                {
                    mediumSpringGreen = new SolidBrush(Color.MediumSpringGreen);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumSpringGreenKey] = mediumSpringGreen;
                }
                return mediumSpringGreen;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.MediumTurquoise"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush MediumTurquoise
        {
            get
            {
                Brush mediumTurquoise = (Brush)SafeNativeMethods.Gdip.ThreadData[s_mediumTurquoiseKey];
                if (mediumTurquoise == null)
                {
                    mediumTurquoise = new SolidBrush(Color.MediumTurquoise);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumTurquoiseKey] = mediumTurquoise;
                }
                return mediumTurquoise;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.MediumVioletRed"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush MediumVioletRed
        {
            get
            {
                Brush mediumVioletRed = (Brush)SafeNativeMethods.Gdip.ThreadData[s_mediumVioletRedKey];
                if (mediumVioletRed == null)
                {
                    mediumVioletRed = new SolidBrush(Color.MediumVioletRed);
                    SafeNativeMethods.Gdip.ThreadData[s_mediumVioletRedKey] = mediumVioletRed;
                }
                return mediumVioletRed;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.MidnightBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush MidnightBlue
        {
            get
            {
                Brush midnightBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_midnightBlueKey];
                if (midnightBlue == null)
                {
                    midnightBlue = new SolidBrush(Color.MidnightBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_midnightBlueKey] = midnightBlue;
                }
                return midnightBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.MintCream"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush MintCream
        {
            get
            {
                Brush mintCream = (Brush)SafeNativeMethods.Gdip.ThreadData[s_mintCreamKey];
                if (mintCream == null)
                {
                    mintCream = new SolidBrush(Color.MintCream);
                    SafeNativeMethods.Gdip.ThreadData[s_mintCreamKey] = mintCream;
                }
                return mintCream;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.MistyRose"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush MistyRose
        {
            get
            {
                Brush mistyRose = (Brush)SafeNativeMethods.Gdip.ThreadData[s_mistyRoseKey];
                if (mistyRose == null)
                {
                    mistyRose = new SolidBrush(Color.MistyRose);
                    SafeNativeMethods.Gdip.ThreadData[s_mistyRoseKey] = mistyRose;
                }
                return mistyRose;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Moccasin"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Moccasin
        {
            get
            {
                Brush moccasin = (Brush)SafeNativeMethods.Gdip.ThreadData[s_moccasinKey];
                if (moccasin == null)
                {
                    moccasin = new SolidBrush(Color.Moccasin);
                    SafeNativeMethods.Gdip.ThreadData[s_moccasinKey] = moccasin;
                }
                return moccasin;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.NavajoWhite"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush NavajoWhite
        {
            get
            {
                Brush navajoWhite = (Brush)SafeNativeMethods.Gdip.ThreadData[s_navajoWhiteKey];
                if (navajoWhite == null)
                {
                    navajoWhite = new SolidBrush(Color.NavajoWhite);
                    SafeNativeMethods.Gdip.ThreadData[s_navajoWhiteKey] = navajoWhite;
                }
                return navajoWhite;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Navy"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Navy
        {
            get
            {
                Brush navy = (Brush)SafeNativeMethods.Gdip.ThreadData[s_navyKey];
                if (navy == null)
                {
                    navy = new SolidBrush(Color.Navy);
                    SafeNativeMethods.Gdip.ThreadData[s_navyKey] = navy;
                }
                return navy;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.OldLace"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush OldLace
        {
            get
            {
                Brush oldLace = (Brush)SafeNativeMethods.Gdip.ThreadData[s_oldLaceKey];
                if (oldLace == null)
                {
                    oldLace = new SolidBrush(Color.OldLace);
                    SafeNativeMethods.Gdip.ThreadData[s_oldLaceKey] = oldLace;
                }
                return oldLace;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Olive"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Olive
        {
            get
            {
                Brush olive = (Brush)SafeNativeMethods.Gdip.ThreadData[s_oliveKey];
                if (olive == null)
                {
                    olive = new SolidBrush(Color.Olive);
                    SafeNativeMethods.Gdip.ThreadData[s_oliveKey] = olive;
                }
                return olive;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.OliveDrab"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush OliveDrab
        {
            get
            {
                Brush oliveDrab = (Brush)SafeNativeMethods.Gdip.ThreadData[s_oliveDrabKey];
                if (oliveDrab == null)
                {
                    oliveDrab = new SolidBrush(Color.OliveDrab);
                    SafeNativeMethods.Gdip.ThreadData[s_oliveDrabKey] = oliveDrab;
                }
                return oliveDrab;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Orange"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Orange
        {
            get
            {
                Brush orange = (Brush)SafeNativeMethods.Gdip.ThreadData[s_orangeKey];
                if (orange == null)
                {
                    orange = new SolidBrush(Color.Orange);
                    SafeNativeMethods.Gdip.ThreadData[s_orangeKey] = orange;
                }
                return orange;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.OrangeRed"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush OrangeRed
        {
            get
            {
                Brush orangeRed = (Brush)SafeNativeMethods.Gdip.ThreadData[s_orangeRedKey];
                if (orangeRed == null)
                {
                    orangeRed = new SolidBrush(Color.OrangeRed);
                    SafeNativeMethods.Gdip.ThreadData[s_orangeRedKey] = orangeRed;
                }
                return orangeRed;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Orchid"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Orchid
        {
            get
            {
                Brush orchid = (Brush)SafeNativeMethods.Gdip.ThreadData[s_orchidKey];
                if (orchid == null)
                {
                    orchid = new SolidBrush(Color.Orchid);
                    SafeNativeMethods.Gdip.ThreadData[s_orchidKey] = orchid;
                }
                return orchid;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.PaleGoldenrod"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush PaleGoldenrod
        {
            get
            {
                Brush paleGoldenrod = (Brush)SafeNativeMethods.Gdip.ThreadData[s_paleGoldenrodKey];
                if (paleGoldenrod == null)
                {
                    paleGoldenrod = new SolidBrush(Color.PaleGoldenrod);
                    SafeNativeMethods.Gdip.ThreadData[s_paleGoldenrodKey] = paleGoldenrod;
                }
                return paleGoldenrod;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.PaleGreen"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush PaleGreen
        {
            get
            {
                Brush paleGreen = (Brush)SafeNativeMethods.Gdip.ThreadData[s_paleGreenKey];
                if (paleGreen == null)
                {
                    paleGreen = new SolidBrush(Color.PaleGreen);
                    SafeNativeMethods.Gdip.ThreadData[s_paleGreenKey] = paleGreen;
                }
                return paleGreen;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.PaleTurquoise"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush PaleTurquoise
        {
            get
            {
                Brush paleTurquoise = (Brush)SafeNativeMethods.Gdip.ThreadData[s_paleTurquoiseKey];
                if (paleTurquoise == null)
                {
                    paleTurquoise = new SolidBrush(Color.PaleTurquoise);
                    SafeNativeMethods.Gdip.ThreadData[s_paleTurquoiseKey] = paleTurquoise;
                }
                return paleTurquoise;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.PaleVioletRed"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush PaleVioletRed
        {
            get
            {
                Brush paleVioletRed = (Brush)SafeNativeMethods.Gdip.ThreadData[s_paleVioletRedKey];
                if (paleVioletRed == null)
                {
                    paleVioletRed = new SolidBrush(Color.PaleVioletRed);
                    SafeNativeMethods.Gdip.ThreadData[s_paleVioletRedKey] = paleVioletRed;
                }
                return paleVioletRed;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.PapayaWhip"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush PapayaWhip
        {
            get
            {
                Brush papayaWhip = (Brush)SafeNativeMethods.Gdip.ThreadData[s_papayaWhipKey];
                if (papayaWhip == null)
                {
                    papayaWhip = new SolidBrush(Color.PapayaWhip);
                    SafeNativeMethods.Gdip.ThreadData[s_papayaWhipKey] = papayaWhip;
                }
                return papayaWhip;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.PeachPuff"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush PeachPuff
        {
            get
            {
                Brush peachPuff = (Brush)SafeNativeMethods.Gdip.ThreadData[s_peachPuffKey];
                if (peachPuff == null)
                {
                    peachPuff = new SolidBrush(Color.PeachPuff);
                    SafeNativeMethods.Gdip.ThreadData[s_peachPuffKey] = peachPuff;
                }
                return peachPuff;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Peru"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Peru
        {
            get
            {
                Brush peru = (Brush)SafeNativeMethods.Gdip.ThreadData[s_peruKey];
                if (peru == null)
                {
                    peru = new SolidBrush(Color.Peru);
                    SafeNativeMethods.Gdip.ThreadData[s_peruKey] = peru;
                }
                return peru;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Pink"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Pink
        {
            get
            {
                Brush pink = (Brush)SafeNativeMethods.Gdip.ThreadData[s_pinkKey];
                if (pink == null)
                {
                    pink = new SolidBrush(Color.Pink);
                    SafeNativeMethods.Gdip.ThreadData[s_pinkKey] = pink;
                }
                return pink;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Plum"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Plum
        {
            get
            {
                Brush plum = (Brush)SafeNativeMethods.Gdip.ThreadData[s_plumKey];
                if (plum == null)
                {
                    plum = new SolidBrush(Color.Plum);
                    SafeNativeMethods.Gdip.ThreadData[s_plumKey] = plum;
                }
                return plum;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.PowderBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush PowderBlue
        {
            get
            {
                Brush powderBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_powderBlueKey];
                if (powderBlue == null)
                {
                    powderBlue = new SolidBrush(Color.PowderBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_powderBlueKey] = powderBlue;
                }
                return powderBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Purple"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Purple
        {
            get
            {
                Brush purple = (Brush)SafeNativeMethods.Gdip.ThreadData[s_purpleKey];
                if (purple == null)
                {
                    purple = new SolidBrush(Color.Purple);
                    SafeNativeMethods.Gdip.ThreadData[s_purpleKey] = purple;
                }
                return purple;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Red"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Red
        {
            get
            {
                Brush red = (Brush)SafeNativeMethods.Gdip.ThreadData[s_redKey];
                if (red == null)
                {
                    red = new SolidBrush(Color.Red);
                    SafeNativeMethods.Gdip.ThreadData[s_redKey] = red;
                }
                return red;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.RosyBrown"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush RosyBrown
        {
            get
            {
                Brush rosyBrown = (Brush)SafeNativeMethods.Gdip.ThreadData[s_rosyBrownKey];
                if (rosyBrown == null)
                {
                    rosyBrown = new SolidBrush(Color.RosyBrown);
                    SafeNativeMethods.Gdip.ThreadData[s_rosyBrownKey] = rosyBrown;
                }
                return rosyBrown;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.RoyalBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush RoyalBlue
        {
            get
            {
                Brush royalBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_royalBlueKey];
                if (royalBlue == null)
                {
                    royalBlue = new SolidBrush(Color.RoyalBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_royalBlueKey] = royalBlue;
                }
                return royalBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.SaddleBrown"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush SaddleBrown
        {
            get
            {
                Brush saddleBrown = (Brush)SafeNativeMethods.Gdip.ThreadData[s_saddleBrownKey];
                if (saddleBrown == null)
                {
                    saddleBrown = new SolidBrush(Color.SaddleBrown);
                    SafeNativeMethods.Gdip.ThreadData[s_saddleBrownKey] = saddleBrown;
                }
                return saddleBrown;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Salmon"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Salmon
        {
            get
            {
                Brush salmon = (Brush)SafeNativeMethods.Gdip.ThreadData[s_salmonKey];
                if (salmon == null)
                {
                    salmon = new SolidBrush(Color.Salmon);
                    SafeNativeMethods.Gdip.ThreadData[s_salmonKey] = salmon;
                }
                return salmon;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.SandyBrown"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush SandyBrown
        {
            get
            {
                Brush sandyBrown = (Brush)SafeNativeMethods.Gdip.ThreadData[s_sandyBrownKey];
                if (sandyBrown == null)
                {
                    sandyBrown = new SolidBrush(Color.SandyBrown);
                    SafeNativeMethods.Gdip.ThreadData[s_sandyBrownKey] = sandyBrown;
                }
                return sandyBrown;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.SeaGreen"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush SeaGreen
        {
            get
            {
                Brush seaGreen = (Brush)SafeNativeMethods.Gdip.ThreadData[s_seaGreenKey];
                if (seaGreen == null)
                {
                    seaGreen = new SolidBrush(Color.SeaGreen);
                    SafeNativeMethods.Gdip.ThreadData[s_seaGreenKey] = seaGreen;
                }
                return seaGreen;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.SeaShell"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush SeaShell
        {
            get
            {
                Brush seaShell = (Brush)SafeNativeMethods.Gdip.ThreadData[s_seaShellKey];
                if (seaShell == null)
                {
                    seaShell = new SolidBrush(Color.SeaShell);
                    SafeNativeMethods.Gdip.ThreadData[s_seaShellKey] = seaShell;
                }
                return seaShell;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Sienna"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Sienna
        {
            get
            {
                Brush sienna = (Brush)SafeNativeMethods.Gdip.ThreadData[s_siennaKey];
                if (sienna == null)
                {
                    sienna = new SolidBrush(Color.Sienna);
                    SafeNativeMethods.Gdip.ThreadData[s_siennaKey] = sienna;
                }
                return sienna;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Silver"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Silver
        {
            get
            {
                Brush silver = (Brush)SafeNativeMethods.Gdip.ThreadData[s_silverKey];
                if (silver == null)
                {
                    silver = new SolidBrush(Color.Silver);
                    SafeNativeMethods.Gdip.ThreadData[s_silverKey] = silver;
                }
                return silver;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.SkyBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush SkyBlue
        {
            get
            {
                Brush skyBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_skyBlueKey];
                if (skyBlue == null)
                {
                    skyBlue = new SolidBrush(Color.SkyBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_skyBlueKey] = skyBlue;
                }
                return skyBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.SlateBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush SlateBlue
        {
            get
            {
                Brush slateBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_slateBlueKey];
                if (slateBlue == null)
                {
                    slateBlue = new SolidBrush(Color.SlateBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_slateBlueKey] = slateBlue;
                }
                return slateBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.SlateGray"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush SlateGray
        {
            get
            {
                Brush slateGray = (Brush)SafeNativeMethods.Gdip.ThreadData[s_slateGrayKey];
                if (slateGray == null)
                {
                    slateGray = new SolidBrush(Color.SlateGray);
                    SafeNativeMethods.Gdip.ThreadData[s_slateGrayKey] = slateGray;
                }
                return slateGray;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Snow"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Snow
        {
            get
            {
                Brush snow = (Brush)SafeNativeMethods.Gdip.ThreadData[s_snowKey];
                if (snow == null)
                {
                    snow = new SolidBrush(Color.Snow);
                    SafeNativeMethods.Gdip.ThreadData[s_snowKey] = snow;
                }
                return snow;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.SpringGreen"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush SpringGreen
        {
            get
            {
                Brush springGreen = (Brush)SafeNativeMethods.Gdip.ThreadData[s_springGreenKey];
                if (springGreen == null)
                {
                    springGreen = new SolidBrush(Color.SpringGreen);
                    SafeNativeMethods.Gdip.ThreadData[s_springGreenKey] = springGreen;
                }
                return springGreen;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.SteelBlue"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush SteelBlue
        {
            get
            {
                Brush steelBlue = (Brush)SafeNativeMethods.Gdip.ThreadData[s_steelBlueKey];
                if (steelBlue == null)
                {
                    steelBlue = new SolidBrush(Color.SteelBlue);
                    SafeNativeMethods.Gdip.ThreadData[s_steelBlueKey] = steelBlue;
                }
                return steelBlue;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Tan"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Tan
        {
            get
            {
                Brush tan = (Brush)SafeNativeMethods.Gdip.ThreadData[s_tanKey];
                if (tan == null)
                {
                    tan = new SolidBrush(Color.Tan);
                    SafeNativeMethods.Gdip.ThreadData[s_tanKey] = tan;
                }
                return tan;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Teal"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Teal
        {
            get
            {
                Brush teal = (Brush)SafeNativeMethods.Gdip.ThreadData[s_tealKey];
                if (teal == null)
                {
                    teal = new SolidBrush(Color.Teal);
                    SafeNativeMethods.Gdip.ThreadData[s_tealKey] = teal;
                }
                return teal;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Thistle"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Thistle
        {
            get
            {
                Brush thistle = (Brush)SafeNativeMethods.Gdip.ThreadData[s_thistleKey];
                if (thistle == null)
                {
                    thistle = new SolidBrush(Color.Thistle);
                    SafeNativeMethods.Gdip.ThreadData[s_thistleKey] = thistle;
                }
                return thistle;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Tomato"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Tomato
        {
            get
            {
                Brush tomato = (Brush)SafeNativeMethods.Gdip.ThreadData[s_tomatoKey];
                if (tomato == null)
                {
                    tomato = new SolidBrush(Color.Tomato);
                    SafeNativeMethods.Gdip.ThreadData[s_tomatoKey] = tomato;
                }
                return tomato;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Turquoise"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Turquoise
        {
            get
            {
                Brush turquoise = (Brush)SafeNativeMethods.Gdip.ThreadData[s_turquoiseKey];
                if (turquoise == null)
                {
                    turquoise = new SolidBrush(Color.Turquoise);
                    SafeNativeMethods.Gdip.ThreadData[s_turquoiseKey] = turquoise;
                }
                return turquoise;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Violet"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Violet
        {
            get
            {
                Brush violet = (Brush)SafeNativeMethods.Gdip.ThreadData[s_violetKey];
                if (violet == null)
                {
                    violet = new SolidBrush(Color.Violet);
                    SafeNativeMethods.Gdip.ThreadData[s_violetKey] = violet;
                }
                return violet;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Wheat"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Wheat
        {
            get
            {
                Brush wheat = (Brush)SafeNativeMethods.Gdip.ThreadData[s_wheatKey];
                if (wheat == null)
                {
                    wheat = new SolidBrush(Color.Wheat);
                    SafeNativeMethods.Gdip.ThreadData[s_wheatKey] = wheat;
                }
                return wheat;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.White"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush White
        {
            get
            {
                Brush white = (Brush)SafeNativeMethods.Gdip.ThreadData[s_whiteKey];
                if (white == null)
                {
                    white = new SolidBrush(Color.White);
                    SafeNativeMethods.Gdip.ThreadData[s_whiteKey] = white;
                }
                return white;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.WhiteSmoke"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush WhiteSmoke
        {
            get
            {
                Brush whiteSmoke = (Brush)SafeNativeMethods.Gdip.ThreadData[s_whiteSmokeKey];
                if (whiteSmoke == null)
                {
                    whiteSmoke = new SolidBrush(Color.WhiteSmoke);
                    SafeNativeMethods.Gdip.ThreadData[s_whiteSmokeKey] = whiteSmoke;
                }
                return whiteSmoke;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.Yellow"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush Yellow
        {
            get
            {
                Brush yellow = (Brush)SafeNativeMethods.Gdip.ThreadData[s_yellowKey];
                if (yellow == null)
                {
                    yellow = new SolidBrush(Color.Yellow);
                    SafeNativeMethods.Gdip.ThreadData[s_yellowKey] = yellow;
                }
                return yellow;
            }
        }

        /// <include file='doc\Brushes.uex' path='docs/doc[@for="Brushes.YellowGreen"]/*' />
        /// <devdoc>
        ///    A brush of the given color.
        /// </devdoc>
        public static Brush YellowGreen
        {
            get
            {
                Brush yellowGreen = (Brush)SafeNativeMethods.Gdip.ThreadData[s_yellowGreenKey];
                if (yellowGreen == null)
                {
                    yellowGreen = new SolidBrush(Color.YellowGreen);
                    SafeNativeMethods.Gdip.ThreadData[s_yellowGreenKey] = yellowGreen;
                }
                return yellowGreen;
            }
        }
    }
}

