// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    public static class Pens
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
        private static readonly object s_coralKey = new object();
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
        private static readonly object s_fuchsiaKey = new object();
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

        public static Pen Transparent => GetPen(s_transparentKey, Color.Transparent);

        public static Pen AliceBlue => GetPen(s_aliceBlueKey, Color.AliceBlue);
        public static Pen AntiqueWhite => GetPen(s_antiqueWhiteKey, Color.AntiqueWhite);
        public static Pen Aqua => GetPen(s_aquaKey, Color.Aqua);
        public static Pen Aquamarine => GetPen(s_aquamarineKey, Color.Aquamarine);
        public static Pen Azure => GetPen(s_azureKey, Color.Azure);

        public static Pen Beige => GetPen(s_beigeKey, Color.Beige);
        public static Pen Bisque => GetPen(s_bisqueKey, Color.Bisque);
        public static Pen Black => GetPen(s_blackKey, Color.Black);
        public static Pen BlanchedAlmond => GetPen(s_blanchedAlmondKey, Color.BlanchedAlmond);
        public static Pen Blue => GetPen(s_blueKey, Color.Blue);
        public static Pen BlueViolet => GetPen(s_blueVioletKey, Color.BlueViolet);
        public static Pen Brown => GetPen(s_brownKey, Color.Brown);
        public static Pen BurlyWood => GetPen(s_burlyWoodKey, Color.BurlyWood);

        public static Pen CadetBlue => GetPen(s_cadetBlueKey, Color.CadetBlue);
        public static Pen Chartreuse => GetPen(s_chartreuseKey, Color.Chartreuse);
        public static Pen Chocolate => GetPen(s_chocolateKey, Color.Chocolate);
        public static Pen Coral => GetPen(s_coralKey, Color.Coral);
        public static Pen CornflowerBlue => GetPen(s_cornflowerBlueKey, Color.CornflowerBlue);
        public static Pen Cornsilk => GetPen(s_cornsilkKey, Color.Cornsilk);
        public static Pen Crimson => GetPen(s_crimsonKey, Color.Crimson);
        public static Pen Cyan => GetPen(s_cyanKey, Color.Cyan);

        public static Pen DarkBlue => GetPen(s_darkBlueKey, Color.DarkBlue);
        public static Pen DarkCyan => GetPen(s_darkCyanKey, Color.DarkCyan);
        public static Pen DarkGoldenrod => GetPen(s_darkGoldenrodKey, Color.DarkGoldenrod);
        public static Pen DarkGray => GetPen(s_darkGrayKey, Color.DarkGray);
        public static Pen DarkGreen => GetPen(s_darkGreenKey, Color.DarkGreen);
        public static Pen DarkKhaki => GetPen(s_darkKhakiKey, Color.DarkKhaki);
        public static Pen DarkMagenta => GetPen(s_darkMagentaKey, Color.DarkMagenta);
        public static Pen DarkOliveGreen => GetPen(s_darkOliveGreenKey, Color.DarkOliveGreen);
        public static Pen DarkOrange => GetPen(s_darkOrangeKey, Color.DarkOrange);
        public static Pen DarkOrchid => GetPen(s_darkOrchidKey, Color.DarkOrchid);
        public static Pen DarkRed => GetPen(s_darkRedKey, Color.DarkRed);
        public static Pen DarkSalmon => GetPen(s_darkSalmonKey, Color.DarkSalmon);
        public static Pen DarkSeaGreen => GetPen(s_darkSeaGreenKey, Color.DarkSeaGreen);
        public static Pen DarkSlateBlue => GetPen(s_darkSlateBlueKey, Color.DarkSlateBlue);
        public static Pen DarkSlateGray => GetPen(s_darkSlateGrayKey, Color.DarkSlateGray);
        public static Pen DarkTurquoise => GetPen(s_darkTurquoiseKey, Color.DarkTurquoise);
        public static Pen DarkViolet => GetPen(s_darkVioletKey, Color.DarkViolet);
        public static Pen DeepPink => GetPen(s_deepPinkKey, Color.DeepPink);
        public static Pen DeepSkyBlue => GetPen(s_deepSkyBlueKey, Color.DeepSkyBlue);
        public static Pen DimGray => GetPen(s_dimGrayKey, Color.DimGray);
        public static Pen DodgerBlue => GetPen(s_dodgerBlueKey, Color.DodgerBlue);

        public static Pen Firebrick => GetPen(s_firebrickKey, Color.Firebrick);
        public static Pen FloralWhite => GetPen(s_floralWhiteKey, Color.FloralWhite);
        public static Pen ForestGreen => GetPen(s_forestGreenKey, Color.ForestGreen);
        public static Pen Fuchsia => GetPen(s_fuchsiaKey, Color.Fuchsia);

        public static Pen Gainsboro => GetPen(s_gainsboroKey, Color.Gainsboro);
        public static Pen GhostWhite => GetPen(s_ghostWhiteKey, Color.GhostWhite);
        public static Pen Gold => GetPen(s_goldKey, Color.Gold);
        public static Pen Goldenrod => GetPen(s_goldenrodKey, Color.Goldenrod);
        public static Pen Gray => GetPen(s_grayKey, Color.Gray);
        public static Pen Green => GetPen(s_greenKey, Color.Green);
        public static Pen GreenYellow => GetPen(s_greenYellowKey, Color.GreenYellow);

        public static Pen Honeydew => GetPen(s_honeydewKey, Color.Honeydew);
        public static Pen HotPink => GetPen(s_hotPinkKey, Color.HotPink);

        public static Pen IndianRed => GetPen(s_indianRedKey, Color.IndianRed);
        public static Pen Indigo => GetPen(s_indigoKey, Color.Indigo);
        public static Pen Ivory => GetPen(s_ivoryKey, Color.Ivory);

        public static Pen Khaki => GetPen(s_khakiKey, Color.Khaki);

        public static Pen Lavender => GetPen(s_lavenderKey, Color.Lavender);
        public static Pen LavenderBlush => GetPen(s_lavenderBlushKey, Color.LavenderBlush);
        public static Pen LawnGreen => GetPen(s_lawnGreenKey, Color.LawnGreen);
        public static Pen LemonChiffon => GetPen(s_lemonChiffonKey, Color.LemonChiffon);
        public static Pen LightBlue => GetPen(s_lightBlueKey, Color.LightBlue);
        public static Pen LightCoral => GetPen(s_lightCoralKey, Color.LightCoral);
        public static Pen LightCyan => GetPen(s_lightCyanKey, Color.LightCyan);
        public static Pen LightGoldenrodYellow => GetPen(s_lightGoldenrodYellowKey, Color.LightGoldenrodYellow);
        public static Pen LightGreen => GetPen(s_lightGreenKey, Color.LightGreen);
        public static Pen LightGray => GetPen(s_lightGrayKey, Color.LightGray);
        public static Pen LightPink => GetPen(s_lightPinkKey, Color.LightPink);
        public static Pen LightSalmon => GetPen(s_lightSalmonKey, Color.LightSalmon);
        public static Pen LightSeaGreen => GetPen(s_lightSeaGreenKey, Color.LightSeaGreen);
        public static Pen LightSkyBlue => GetPen(s_lightSkyBlueKey, Color.LightSkyBlue);
        public static Pen LightSlateGray => GetPen(s_lightSlateGrayKey, Color.LightSlateGray);
        public static Pen LightSteelBlue => GetPen(s_lightSteelBlueKey, Color.LightSteelBlue);
        public static Pen LightYellow => GetPen(s_lightYellowKey, Color.LightYellow);
        public static Pen Lime => GetPen(s_limeKey, Color.Lime);
        public static Pen LimeGreen => GetPen(s_limeGreenKey, Color.LimeGreen);
        public static Pen Linen => GetPen(s_linenKey, Color.Linen);

        public static Pen Magenta => GetPen(s_magentaKey, Color.Magenta);
        public static Pen Maroon => GetPen(s_maroonKey, Color.Maroon);
        public static Pen MediumAquamarine => GetPen(s_mediumAquamarineKey, Color.MediumAquamarine);
        public static Pen MediumBlue => GetPen(s_mediumBlueKey, Color.MediumBlue);
        public static Pen MediumOrchid => GetPen(s_mediumOrchidKey, Color.MediumOrchid);
        public static Pen MediumPurple => GetPen(s_mediumPurpleKey, Color.MediumPurple);
        public static Pen MediumSeaGreen => GetPen(s_mediumSeaGreenKey, Color.MediumSeaGreen);
        public static Pen MediumSlateBlue => GetPen(s_mediumSlateBlueKey, Color.MediumSlateBlue);
        public static Pen MediumSpringGreen => GetPen(s_mediumSpringGreenKey, Color.MediumSpringGreen);
        public static Pen MediumTurquoise => GetPen(s_mediumTurquoiseKey, Color.MediumTurquoise);
        public static Pen MediumVioletRed => GetPen(s_mediumVioletRedKey, Color.MediumVioletRed);
        public static Pen MidnightBlue => GetPen(s_midnightBlueKey, Color.MidnightBlue);
        public static Pen MintCream => GetPen(s_mintCreamKey, Color.MintCream);
        public static Pen MistyRose => GetPen(s_mistyRoseKey, Color.MistyRose);
        public static Pen Moccasin => GetPen(s_moccasinKey, Color.Moccasin);

        public static Pen NavajoWhite => GetPen(s_navajoWhiteKey, Color.NavajoWhite);
        public static Pen Navy => GetPen(s_navyKey, Color.Navy);

        public static Pen OldLace => GetPen(s_oldLaceKey, Color.OldLace);
        public static Pen Olive => GetPen(s_oliveKey, Color.Olive);
        public static Pen OliveDrab => GetPen(s_oliveDrabKey, Color.OliveDrab);
        public static Pen Orange => GetPen(s_orangeKey, Color.Orange);
        public static Pen OrangeRed => GetPen(s_orangeRedKey, Color.OrangeRed);
        public static Pen Orchid => GetPen(s_orchidKey, Color.Orchid);

        public static Pen PaleGoldenrod => GetPen(s_paleGoldenrodKey, Color.PaleGoldenrod);
        public static Pen PaleGreen => GetPen(s_paleGreenKey, Color.PaleGreen);
        public static Pen PaleTurquoise => GetPen(s_paleTurquoiseKey, Color.PaleTurquoise);
        public static Pen PaleVioletRed => GetPen(s_paleVioletRedKey, Color.PaleVioletRed);
        public static Pen PapayaWhip => GetPen(s_papayaWhipKey, Color.PapayaWhip);
        public static Pen PeachPuff => GetPen(s_peachPuffKey, Color.PeachPuff);
        public static Pen Peru => GetPen(s_peruKey, Color.Peru);
        public static Pen Pink => GetPen(s_pinkKey, Color.Pink);
        public static Pen Plum => GetPen(s_plumKey, Color.Plum);
        public static Pen PowderBlue => GetPen(s_powderBlueKey, Color.PowderBlue);
        public static Pen Purple => GetPen(s_purpleKey, Color.Purple);

        public static Pen Red => GetPen(s_redKey, Color.Red);
        public static Pen RosyBrown => GetPen(s_rosyBrownKey, Color.RosyBrown);
        public static Pen RoyalBlue => GetPen(s_royalBlueKey, Color.RoyalBlue);

        public static Pen SaddleBrown => GetPen(s_saddleBrownKey, Color.SaddleBrown);
        public static Pen Salmon => GetPen(s_salmonKey, Color.Salmon);
        public static Pen SandyBrown => GetPen(s_sandyBrownKey, Color.SandyBrown);
        public static Pen SeaGreen => GetPen(s_seaGreenKey, Color.SeaGreen);
        public static Pen SeaShell => GetPen(s_seaShellKey, Color.SeaShell);
        public static Pen Sienna => GetPen(s_siennaKey, Color.Sienna);
        public static Pen Silver => GetPen(s_silverKey, Color.Silver);
        public static Pen SkyBlue => GetPen(s_skyBlueKey, Color.SkyBlue);
        public static Pen SlateBlue => GetPen(s_slateBlueKey, Color.SlateBlue);
        public static Pen SlateGray => GetPen(s_slateGrayKey, Color.SlateGray);
        public static Pen Snow => GetPen(s_snowKey, Color.Snow);
        public static Pen SpringGreen => GetPen(s_springGreenKey, Color.SpringGreen);
        public static Pen SteelBlue => GetPen(s_steelBlueKey, Color.SteelBlue);

        public static Pen Tan => GetPen(s_tanKey, Color.Tan);
        public static Pen Teal => GetPen(s_tealKey, Color.Teal);
        public static Pen Thistle => GetPen(s_thistleKey, Color.Thistle);
        public static Pen Tomato => GetPen(s_tomatoKey, Color.Tomato);
        public static Pen Turquoise => GetPen(s_turquoiseKey, Color.Turquoise);

        public static Pen Violet => GetPen(s_violetKey, Color.Violet);

        public static Pen Wheat => GetPen(s_wheatKey, Color.Wheat);
        public static Pen White => GetPen(s_whiteKey, Color.White);
        public static Pen WhiteSmoke => GetPen(s_whiteSmokeKey, Color.WhiteSmoke);

        public static Pen Yellow => GetPen(s_yellowKey, Color.Yellow);
        public static Pen YellowGreen => GetPen(s_yellowGreenKey, Color.YellowGreen);

        private static Pen GetPen(object key, Color color)
        {
            Pen Pen = (Pen)Gdip.ThreadData[key];
            if (Pen == null)
            {
                Pen = new Pen(color, true);
                Gdip.ThreadData[key] = Pen;
            }
            return Pen;
        }
    }
}
