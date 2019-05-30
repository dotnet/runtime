// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System.Drawing
{
    static internal class KnownColorTable
    {
        private static Func<EventArgs, int> s_categoryGetter;
        private static int[] s_colorTable;
        private static string[] s_colorNameTable;

        /**
         * Shift count and bit mask for A, R, G, B components
         */
        private const int AlphaShift = 24;
        private const int RedShift = 16;
        private const int GreenShift = 8;
        private const int BlueShift = 0;

        private const int Win32RedShift = 0;
        private const int Win32GreenShift = 8;
        private const int Win32BlueShift = 16;

        public static Color ArgbToKnownColor(int targetARGB)
        {
            EnsureColorTable();
            for (int index = 0; index < s_colorTable.Length; ++index)
            {
                int argb = s_colorTable[index];
                if (argb == targetARGB)
                {
                    Color color = ColorUtil.FromKnownColor((KnownColor)index);
                    if (!ColorUtil.IsSystemColor(color))
                        return color;
                }
            }

            return Color.FromArgb(targetARGB);
        }

        private static void EnsureColorTable()
        {
            // no need to lock... worse case is a double create of the table...
            //
            if (s_colorTable == null)
            {
                InitColorTable();
            }
        }

        private static void InitColorTable()
        {
            int[] values = new int[(unchecked((int)KnownColor.MenuHighlight)) + 1];

            // When Microsoft.Win32.SystemEvents is available, we can react to the UserPreferenceChanging events by updating
            // SystemColors. In order to avoid a static dependency on SystemEvents since it is not available on all platforms 
            // and as such we don't want to bring it into the shared framework. We use the desktop identity for SystemEvents
            // since it is stable and will remain stable for compatibility whereas the core identity could change.
            Type systemEventsType = Type.GetType("Microsoft.Win32.SystemEvents, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", throwOnError: false);
            EventInfo upEventInfo = systemEventsType?.GetEvent("UserPreferenceChanging", BindingFlags.Public | BindingFlags.Static);

            if (upEventInfo != null)
            {
                // Delegate TargetType
                Type userPrefChangingDelegateType = Type.GetType("Microsoft.Win32.UserPreferenceChangingEventHandler, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", throwOnError: false);
                Debug.Assert(userPrefChangingDelegateType != null);

                if (userPrefChangingDelegateType != null)
                {
                    // we are using MethodInfo overload because it allows relaxed signature binding i.e. the types dont need to be exact match. It allows base classes as well.
                    MethodInfo mi = typeof(KnownColorTable).GetMethod(nameof(OnUserPreferenceChanging), BindingFlags.NonPublic | BindingFlags.Static);
                    Debug.Assert(mi != null);

                    if (mi != null)
                    {
                        // Creating a delegate to use it as event handler.
                        Delegate handler = Delegate.CreateDelegate(userPrefChangingDelegateType, mi);
                        upEventInfo.AddEventHandler(null, handler);
                    }

                    // Retrieving getter of the category property of the UserPreferenceChangingEventArgs.
                    Type argsType = Type.GetType("Microsoft.Win32.UserPreferenceChangingEventArgs, System, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089", throwOnError: false);
                    PropertyInfo categoryProperty = argsType?.GetProperty("Category", BindingFlags.Instance | BindingFlags.Public);
                    MethodInfo categoryGetter = categoryProperty?.GetGetMethod();

                    Debug.Assert(categoryGetter != null);
                    if (categoryGetter != null)
                    {
                        // Creating a Delegate pointing to the getter method. 
                        s_categoryGetter = (Func<EventArgs, int>)typeof(KnownColorTable).GetMethod(nameof(CreateGetter), BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(argsType, categoryGetter.ReturnType)
                            .Invoke(null, new object[] { categoryGetter });
                    }
                }
            }

            UpdateSystemColors(values);

            // just consts...
            //
            values[(int)KnownColor.Transparent] = 0x00FFFFFF;
            values[(int)KnownColor.AliceBlue] = unchecked((int)0xFFF0F8FF);
            values[(int)KnownColor.AntiqueWhite] = unchecked((int)0xFFFAEBD7);
            values[(int)KnownColor.Aqua] = unchecked((int)0xFF00FFFF);
            values[(int)KnownColor.Aquamarine] = unchecked((int)0xFF7FFFD4);
            values[(int)KnownColor.Azure] = unchecked((int)0xFFF0FFFF);
            values[(int)KnownColor.Beige] = unchecked((int)0xFFF5F5DC);
            values[(int)KnownColor.Bisque] = unchecked(unchecked((int)0xFFFFE4C4));
            values[(int)KnownColor.Black] = unchecked((int)0xFF000000);
            values[(int)KnownColor.BlanchedAlmond] = unchecked((int)0xFFFFEBCD);
            values[(int)KnownColor.Blue] = unchecked((int)0xFF0000FF);
            values[(int)KnownColor.BlueViolet] = unchecked((int)0xFF8A2BE2);
            values[(int)KnownColor.Brown] = unchecked((int)0xFFA52A2A);
            values[(int)KnownColor.BurlyWood] = unchecked((int)0xFFDEB887);
            values[(int)KnownColor.CadetBlue] = unchecked((int)0xFF5F9EA0);
            values[(int)KnownColor.Chartreuse] = unchecked((int)0xFF7FFF00);
            values[(int)KnownColor.Chocolate] = unchecked((int)0xFFD2691E);
            values[(int)KnownColor.Coral] = unchecked((int)0xFFFF7F50);
            values[(int)KnownColor.CornflowerBlue] = unchecked((int)0xFF6495ED);
            values[(int)KnownColor.Cornsilk] = unchecked((int)0xFFFFF8DC);
            values[(int)KnownColor.Crimson] = unchecked((int)0xFFDC143C);
            values[(int)KnownColor.Cyan] = unchecked((int)0xFF00FFFF);
            values[(int)KnownColor.DarkBlue] = unchecked((int)0xFF00008B);
            values[(int)KnownColor.DarkCyan] = unchecked((int)0xFF008B8B);
            values[(int)KnownColor.DarkGoldenrod] = unchecked((int)0xFFB8860B);
            values[(int)KnownColor.DarkGray] = unchecked((int)0xFFA9A9A9);
            values[(int)KnownColor.DarkGreen] = unchecked((int)0xFF006400);
            values[(int)KnownColor.DarkKhaki] = unchecked((int)0xFFBDB76B);
            values[(int)KnownColor.DarkMagenta] = unchecked((int)0xFF8B008B);
            values[(int)KnownColor.DarkOliveGreen] = unchecked((int)0xFF556B2F);
            values[(int)KnownColor.DarkOrange] = unchecked((int)0xFFFF8C00);
            values[(int)KnownColor.DarkOrchid] = unchecked((int)0xFF9932CC);
            values[(int)KnownColor.DarkRed] = unchecked((int)0xFF8B0000);
            values[(int)KnownColor.DarkSalmon] = unchecked((int)0xFFE9967A);
            values[(int)KnownColor.DarkSeaGreen] = unchecked((int)0xFF8FBC8B);
            values[(int)KnownColor.DarkSlateBlue] = unchecked((int)0xFF483D8B);
            values[(int)KnownColor.DarkSlateGray] = unchecked((int)0xFF2F4F4F);
            values[(int)KnownColor.DarkTurquoise] = unchecked((int)0xFF00CED1);
            values[(int)KnownColor.DarkViolet] = unchecked((int)0xFF9400D3);
            values[(int)KnownColor.DeepPink] = unchecked((int)0xFFFF1493);
            values[(int)KnownColor.DeepSkyBlue] = unchecked((int)0xFF00BFFF);
            values[(int)KnownColor.DimGray] = unchecked((int)0xFF696969);
            values[(int)KnownColor.DodgerBlue] = unchecked((int)0xFF1E90FF);
            values[(int)KnownColor.Firebrick] = unchecked((int)0xFFB22222);
            values[(int)KnownColor.FloralWhite] = unchecked((int)0xFFFFFAF0);
            values[(int)KnownColor.ForestGreen] = unchecked((int)0xFF228B22);
            values[(int)KnownColor.Fuchsia] = unchecked((int)0xFFFF00FF);
            values[(int)KnownColor.Gainsboro] = unchecked((int)0xFFDCDCDC);
            values[(int)KnownColor.GhostWhite] = unchecked((int)0xFFF8F8FF);
            values[(int)KnownColor.Gold] = unchecked((int)0xFFFFD700);
            values[(int)KnownColor.Goldenrod] = unchecked((int)0xFFDAA520);
            values[(int)KnownColor.Gray] = unchecked((int)0xFF808080);
            values[(int)KnownColor.Green] = unchecked((int)0xFF008000);
            values[(int)KnownColor.GreenYellow] = unchecked((int)0xFFADFF2F);
            values[(int)KnownColor.Honeydew] = unchecked((int)0xFFF0FFF0);
            values[(int)KnownColor.HotPink] = unchecked((int)0xFFFF69B4);
            values[(int)KnownColor.IndianRed] = unchecked((int)0xFFCD5C5C);
            values[(int)KnownColor.Indigo] = unchecked((int)0xFF4B0082);
            values[(int)KnownColor.Ivory] = unchecked((int)0xFFFFFFF0);
            values[(int)KnownColor.Khaki] = unchecked((int)0xFFF0E68C);
            values[(int)KnownColor.Lavender] = unchecked((int)0xFFE6E6FA);
            values[(int)KnownColor.LavenderBlush] = unchecked((int)0xFFFFF0F5);
            values[(int)KnownColor.LawnGreen] = unchecked((int)0xFF7CFC00);
            values[(int)KnownColor.LemonChiffon] = unchecked((int)0xFFFFFACD);
            values[(int)KnownColor.LightBlue] = unchecked((int)0xFFADD8E6);
            values[(int)KnownColor.LightCoral] = unchecked((int)0xFFF08080);
            values[(int)KnownColor.LightCyan] = unchecked((int)0xFFE0FFFF);
            values[(int)KnownColor.LightGoldenrodYellow] = unchecked((int)0xFFFAFAD2);
            values[(int)KnownColor.LightGray] = unchecked((int)0xFFD3D3D3);
            values[(int)KnownColor.LightGreen] = unchecked((int)0xFF90EE90);
            values[(int)KnownColor.LightPink] = unchecked((int)0xFFFFB6C1);
            values[(int)KnownColor.LightSalmon] = unchecked((int)0xFFFFA07A);
            values[(int)KnownColor.LightSeaGreen] = unchecked((int)0xFF20B2AA);
            values[(int)KnownColor.LightSkyBlue] = unchecked((int)0xFF87CEFA);
            values[(int)KnownColor.LightSlateGray] = unchecked((int)0xFF778899);
            values[(int)KnownColor.LightSteelBlue] = unchecked((int)0xFFB0C4DE);
            values[(int)KnownColor.LightYellow] = unchecked((int)0xFFFFFFE0);
            values[(int)KnownColor.Lime] = unchecked((int)0xFF00FF00);
            values[(int)KnownColor.LimeGreen] = unchecked((int)0xFF32CD32);
            values[(int)KnownColor.Linen] = unchecked((int)0xFFFAF0E6);
            values[(int)KnownColor.Magenta] = unchecked((int)0xFFFF00FF);
            values[(int)KnownColor.Maroon] = unchecked((int)0xFF800000);
            values[(int)KnownColor.MediumAquamarine] = unchecked((int)0xFF66CDAA);
            values[(int)KnownColor.MediumBlue] = unchecked((int)0xFF0000CD);
            values[(int)KnownColor.MediumOrchid] = unchecked((int)0xFFBA55D3);
            values[(int)KnownColor.MediumPurple] = unchecked((int)0xFF9370DB);
            values[(int)KnownColor.MediumSeaGreen] = unchecked((int)0xFF3CB371);
            values[(int)KnownColor.MediumSlateBlue] = unchecked((int)0xFF7B68EE);
            values[(int)KnownColor.MediumSpringGreen] = unchecked((int)0xFF00FA9A);
            values[(int)KnownColor.MediumTurquoise] = unchecked((int)0xFF48D1CC);
            values[(int)KnownColor.MediumVioletRed] = unchecked((int)0xFFC71585);
            values[(int)KnownColor.MidnightBlue] = unchecked((int)0xFF191970);
            values[(int)KnownColor.MintCream] = unchecked((int)0xFFF5FFFA);
            values[(int)KnownColor.MistyRose] = unchecked((int)0xFFFFE4E1);
            values[(int)KnownColor.Moccasin] = unchecked((int)0xFFFFE4B5);
            values[(int)KnownColor.NavajoWhite] = unchecked((int)0xFFFFDEAD);
            values[(int)KnownColor.Navy] = unchecked((int)0xFF000080);
            values[(int)KnownColor.OldLace] = unchecked((int)0xFFFDF5E6);
            values[(int)KnownColor.Olive] = unchecked((int)0xFF808000);
            values[(int)KnownColor.OliveDrab] = unchecked((int)0xFF6B8E23);
            values[(int)KnownColor.Orange] = unchecked((int)0xFFFFA500);
            values[(int)KnownColor.OrangeRed] = unchecked((int)0xFFFF4500);
            values[(int)KnownColor.Orchid] = unchecked((int)0xFFDA70D6);
            values[(int)KnownColor.PaleGoldenrod] = unchecked((int)0xFFEEE8AA);
            values[(int)KnownColor.PaleGreen] = unchecked((int)0xFF98FB98);
            values[(int)KnownColor.PaleTurquoise] = unchecked((int)0xFFAFEEEE);
            values[(int)KnownColor.PaleVioletRed] = unchecked((int)0xFFDB7093);
            values[(int)KnownColor.PapayaWhip] = unchecked((int)0xFFFFEFD5);
            values[(int)KnownColor.PeachPuff] = unchecked((int)0xFFFFDAB9);
            values[(int)KnownColor.Peru] = unchecked((int)0xFFCD853F);
            values[(int)KnownColor.Pink] = unchecked((int)0xFFFFC0CB);
            values[(int)KnownColor.Plum] = unchecked((int)0xFFDDA0DD);
            values[(int)KnownColor.PowderBlue] = unchecked((int)0xFFB0E0E6);
            values[(int)KnownColor.Purple] = unchecked((int)0xFF800080);
            values[(int)KnownColor.Red] = unchecked((int)0xFFFF0000);
            values[(int)KnownColor.RosyBrown] = unchecked((int)0xFFBC8F8F);
            values[(int)KnownColor.RoyalBlue] = unchecked((int)0xFF4169E1);
            values[(int)KnownColor.SaddleBrown] = unchecked((int)0xFF8B4513);
            values[(int)KnownColor.Salmon] = unchecked((int)0xFFFA8072);
            values[(int)KnownColor.SandyBrown] = unchecked((int)0xFFF4A460);
            values[(int)KnownColor.SeaGreen] = unchecked((int)0xFF2E8B57);
            values[(int)KnownColor.SeaShell] = unchecked((int)0xFFFFF5EE);
            values[(int)KnownColor.Sienna] = unchecked((int)0xFFA0522D);
            values[(int)KnownColor.Silver] = unchecked((int)0xFFC0C0C0);
            values[(int)KnownColor.SkyBlue] = unchecked((int)0xFF87CEEB);
            values[(int)KnownColor.SlateBlue] = unchecked((int)0xFF6A5ACD);
            values[(int)KnownColor.SlateGray] = unchecked((int)0xFF708090);
            values[(int)KnownColor.Snow] = unchecked((int)0xFFFFFAFA);
            values[(int)KnownColor.SpringGreen] = unchecked((int)0xFF00FF7F);
            values[(int)KnownColor.SteelBlue] = unchecked((int)0xFF4682B4);
            values[(int)KnownColor.Tan] = unchecked((int)0xFFD2B48C);
            values[(int)KnownColor.Teal] = unchecked((int)0xFF008080);
            values[(int)KnownColor.Thistle] = unchecked((int)0xFFD8BFD8);
            values[(int)KnownColor.Tomato] = unchecked((int)0xFFFF6347);
            values[(int)KnownColor.Turquoise] = unchecked((int)0xFF40E0D0);
            values[(int)KnownColor.Violet] = unchecked((int)0xFFEE82EE);
            values[(int)KnownColor.Wheat] = unchecked((int)0xFFF5DEB3);
            values[(int)KnownColor.White] = unchecked((int)0xFFFFFFFF);
            values[(int)KnownColor.WhiteSmoke] = unchecked((int)0xFFF5F5F5);
            values[(int)KnownColor.Yellow] = unchecked((int)0xFFFFFF00);
            values[(int)KnownColor.YellowGreen] = unchecked((int)0xFF9ACD32);
            s_colorTable = values;
        }

        // Generic method that we specialize at runtime once we've loaded the UserPreferenceChangingEventArgs type.
        // It permits creating an unbound delegate so that we can avoid reflection after the initial
        // lightup code completes.
        private static Func<EventArgs, int> CreateGetter<TInstance, TProperty>(MethodInfo method) where TInstance : EventArgs where TProperty : Enum
        {
            Func<TInstance, TProperty> typedDelegate = (Func<TInstance, TProperty>)Delegate.CreateDelegate(typeof(Func<TInstance, TProperty>), null, method);
            return (EventArgs instance) => (int)(object)typedDelegate((TInstance)instance);
        }

        private static void EnsureColorNameTable()
        {
            // no need to lock... worse case is a double create of the table...
            //
            if (s_colorNameTable == null)
            {
                InitColorNameTable();
            }
        }

        private static void InitColorNameTable()
        {
            string[] values = new string[((int)KnownColor.MenuHighlight) + 1];

            // just consts...
            //
            values[(int)KnownColor.ActiveBorder] = "ActiveBorder";
            values[(int)KnownColor.ActiveCaption] = "ActiveCaption";
            values[(int)KnownColor.ActiveCaptionText] = "ActiveCaptionText";
            values[(int)KnownColor.AppWorkspace] = "AppWorkspace";
            values[(int)KnownColor.ButtonFace] = "ButtonFace";
            values[(int)KnownColor.ButtonHighlight] = "ButtonHighlight";
            values[(int)KnownColor.ButtonShadow] = "ButtonShadow";
            values[(int)KnownColor.Control] = "Control";
            values[(int)KnownColor.ControlDark] = "ControlDark";
            values[(int)KnownColor.ControlDarkDark] = "ControlDarkDark";
            values[(int)KnownColor.ControlLight] = "ControlLight";
            values[(int)KnownColor.ControlLightLight] = "ControlLightLight";
            values[(int)KnownColor.ControlText] = "ControlText";
            values[(int)KnownColor.Desktop] = "Desktop";
            values[(int)KnownColor.GradientActiveCaption] = "GradientActiveCaption";
            values[(int)KnownColor.GradientInactiveCaption] = "GradientInactiveCaption";
            values[(int)KnownColor.GrayText] = "GrayText";
            values[(int)KnownColor.Highlight] = "Highlight";
            values[(int)KnownColor.HighlightText] = "HighlightText";
            values[(int)KnownColor.HotTrack] = "HotTrack";
            values[(int)KnownColor.InactiveBorder] = "InactiveBorder";
            values[(int)KnownColor.InactiveCaption] = "InactiveCaption";
            values[(int)KnownColor.InactiveCaptionText] = "InactiveCaptionText";
            values[(int)KnownColor.Info] = "Info";
            values[(int)KnownColor.InfoText] = "InfoText";
            values[(int)KnownColor.Menu] = "Menu";
            values[(int)KnownColor.MenuBar] = "MenuBar";
            values[(int)KnownColor.MenuHighlight] = "MenuHighlight";
            values[(int)KnownColor.MenuText] = "MenuText";
            values[(int)KnownColor.ScrollBar] = "ScrollBar";
            values[(int)KnownColor.Window] = "Window";
            values[(int)KnownColor.WindowFrame] = "WindowFrame";
            values[(int)KnownColor.WindowText] = "WindowText";

            values[(int)KnownColor.Transparent] = "Transparent";
            values[(int)KnownColor.AliceBlue] = "AliceBlue";
            values[(int)KnownColor.AntiqueWhite] = "AntiqueWhite";
            values[(int)KnownColor.Aqua] = "Aqua";
            values[(int)KnownColor.Aquamarine] = "Aquamarine";
            values[(int)KnownColor.Azure] = "Azure";
            values[(int)KnownColor.Beige] = "Beige";
            values[(int)KnownColor.Bisque] = "Bisque";
            values[(int)KnownColor.Black] = "Black";
            values[(int)KnownColor.BlanchedAlmond] = "BlanchedAlmond";
            values[(int)KnownColor.Blue] = "Blue";
            values[(int)KnownColor.BlueViolet] = "BlueViolet";
            values[(int)KnownColor.Brown] = "Brown";
            values[(int)KnownColor.BurlyWood] = "BurlyWood";
            values[(int)KnownColor.CadetBlue] = "CadetBlue";
            values[(int)KnownColor.Chartreuse] = "Chartreuse";
            values[(int)KnownColor.Chocolate] = "Chocolate";
            values[(int)KnownColor.Coral] = "Coral";
            values[(int)KnownColor.CornflowerBlue] = "CornflowerBlue";
            values[(int)KnownColor.Cornsilk] = "Cornsilk";
            values[(int)KnownColor.Crimson] = "Crimson";
            values[(int)KnownColor.Cyan] = "Cyan";
            values[(int)KnownColor.DarkBlue] = "DarkBlue";
            values[(int)KnownColor.DarkCyan] = "DarkCyan";
            values[(int)KnownColor.DarkGoldenrod] = "DarkGoldenrod";
            values[(int)KnownColor.DarkGray] = "DarkGray";
            values[(int)KnownColor.DarkGreen] = "DarkGreen";
            values[(int)KnownColor.DarkKhaki] = "DarkKhaki";
            values[(int)KnownColor.DarkMagenta] = "DarkMagenta";
            values[(int)KnownColor.DarkOliveGreen] = "DarkOliveGreen";
            values[(int)KnownColor.DarkOrange] = "DarkOrange";
            values[(int)KnownColor.DarkOrchid] = "DarkOrchid";
            values[(int)KnownColor.DarkRed] = "DarkRed";
            values[(int)KnownColor.DarkSalmon] = "DarkSalmon";
            values[(int)KnownColor.DarkSeaGreen] = "DarkSeaGreen";
            values[(int)KnownColor.DarkSlateBlue] = "DarkSlateBlue";
            values[(int)KnownColor.DarkSlateGray] = "DarkSlateGray";
            values[(int)KnownColor.DarkTurquoise] = "DarkTurquoise";
            values[(int)KnownColor.DarkViolet] = "DarkViolet";
            values[(int)KnownColor.DeepPink] = "DeepPink";
            values[(int)KnownColor.DeepSkyBlue] = "DeepSkyBlue";
            values[(int)KnownColor.DimGray] = "DimGray";
            values[(int)KnownColor.DodgerBlue] = "DodgerBlue";
            values[(int)KnownColor.Firebrick] = "Firebrick";
            values[(int)KnownColor.FloralWhite] = "FloralWhite";
            values[(int)KnownColor.ForestGreen] = "ForestGreen";
            values[(int)KnownColor.Fuchsia] = "Fuchsia";
            values[(int)KnownColor.Gainsboro] = "Gainsboro";
            values[(int)KnownColor.GhostWhite] = "GhostWhite";
            values[(int)KnownColor.Gold] = "Gold";
            values[(int)KnownColor.Goldenrod] = "Goldenrod";
            values[(int)KnownColor.Gray] = "Gray";
            values[(int)KnownColor.Green] = "Green";
            values[(int)KnownColor.GreenYellow] = "GreenYellow";
            values[(int)KnownColor.Honeydew] = "Honeydew";
            values[(int)KnownColor.HotPink] = "HotPink";
            values[(int)KnownColor.IndianRed] = "IndianRed";
            values[(int)KnownColor.Indigo] = "Indigo";
            values[(int)KnownColor.Ivory] = "Ivory";
            values[(int)KnownColor.Khaki] = "Khaki";
            values[(int)KnownColor.Lavender] = "Lavender";
            values[(int)KnownColor.LavenderBlush] = "LavenderBlush";
            values[(int)KnownColor.LawnGreen] = "LawnGreen";
            values[(int)KnownColor.LemonChiffon] = "LemonChiffon";
            values[(int)KnownColor.LightBlue] = "LightBlue";
            values[(int)KnownColor.LightCoral] = "LightCoral";
            values[(int)KnownColor.LightCyan] = "LightCyan";
            values[(int)KnownColor.LightGoldenrodYellow] = "LightGoldenrodYellow";
            values[(int)KnownColor.LightGray] = "LightGray";
            values[(int)KnownColor.LightGreen] = "LightGreen";
            values[(int)KnownColor.LightPink] = "LightPink";
            values[(int)KnownColor.LightSalmon] = "LightSalmon";
            values[(int)KnownColor.LightSeaGreen] = "LightSeaGreen";
            values[(int)KnownColor.LightSkyBlue] = "LightSkyBlue";
            values[(int)KnownColor.LightSlateGray] = "LightSlateGray";
            values[(int)KnownColor.LightSteelBlue] = "LightSteelBlue";
            values[(int)KnownColor.LightYellow] = "LightYellow";
            values[(int)KnownColor.Lime] = "Lime";
            values[(int)KnownColor.LimeGreen] = "LimeGreen";
            values[(int)KnownColor.Linen] = "Linen";
            values[(int)KnownColor.Magenta] = "Magenta";
            values[(int)KnownColor.Maroon] = "Maroon";
            values[(int)KnownColor.MediumAquamarine] = "MediumAquamarine";
            values[(int)KnownColor.MediumBlue] = "MediumBlue";
            values[(int)KnownColor.MediumOrchid] = "MediumOrchid";
            values[(int)KnownColor.MediumPurple] = "MediumPurple";
            values[(int)KnownColor.MediumSeaGreen] = "MediumSeaGreen";
            values[(int)KnownColor.MediumSlateBlue] = "MediumSlateBlue";
            values[(int)KnownColor.MediumSpringGreen] = "MediumSpringGreen";
            values[(int)KnownColor.MediumTurquoise] = "MediumTurquoise";
            values[(int)KnownColor.MediumVioletRed] = "MediumVioletRed";
            values[(int)KnownColor.MidnightBlue] = "MidnightBlue";
            values[(int)KnownColor.MintCream] = "MintCream";
            values[(int)KnownColor.MistyRose] = "MistyRose";
            values[(int)KnownColor.Moccasin] = "Moccasin";
            values[(int)KnownColor.NavajoWhite] = "NavajoWhite";
            values[(int)KnownColor.Navy] = "Navy";
            values[(int)KnownColor.OldLace] = "OldLace";
            values[(int)KnownColor.Olive] = "Olive";
            values[(int)KnownColor.OliveDrab] = "OliveDrab";
            values[(int)KnownColor.Orange] = "Orange";
            values[(int)KnownColor.OrangeRed] = "OrangeRed";
            values[(int)KnownColor.Orchid] = "Orchid";
            values[(int)KnownColor.PaleGoldenrod] = "PaleGoldenrod";
            values[(int)KnownColor.PaleGreen] = "PaleGreen";
            values[(int)KnownColor.PaleTurquoise] = "PaleTurquoise";
            values[(int)KnownColor.PaleVioletRed] = "PaleVioletRed";
            values[(int)KnownColor.PapayaWhip] = "PapayaWhip";
            values[(int)KnownColor.PeachPuff] = "PeachPuff";
            values[(int)KnownColor.Peru] = "Peru";
            values[(int)KnownColor.Pink] = "Pink";
            values[(int)KnownColor.Plum] = "Plum";
            values[(int)KnownColor.PowderBlue] = "PowderBlue";
            values[(int)KnownColor.Purple] = "Purple";
            values[(int)KnownColor.Red] = "Red";
            values[(int)KnownColor.RosyBrown] = "RosyBrown";
            values[(int)KnownColor.RoyalBlue] = "RoyalBlue";
            values[(int)KnownColor.SaddleBrown] = "SaddleBrown";
            values[(int)KnownColor.Salmon] = "Salmon";
            values[(int)KnownColor.SandyBrown] = "SandyBrown";
            values[(int)KnownColor.SeaGreen] = "SeaGreen";
            values[(int)KnownColor.SeaShell] = "SeaShell";
            values[(int)KnownColor.Sienna] = "Sienna";
            values[(int)KnownColor.Silver] = "Silver";
            values[(int)KnownColor.SkyBlue] = "SkyBlue";
            values[(int)KnownColor.SlateBlue] = "SlateBlue";
            values[(int)KnownColor.SlateGray] = "SlateGray";
            values[(int)KnownColor.Snow] = "Snow";
            values[(int)KnownColor.SpringGreen] = "SpringGreen";
            values[(int)KnownColor.SteelBlue] = "SteelBlue";
            values[(int)KnownColor.Tan] = "Tan";
            values[(int)KnownColor.Teal] = "Teal";
            values[(int)KnownColor.Thistle] = "Thistle";
            values[(int)KnownColor.Tomato] = "Tomato";
            values[(int)KnownColor.Turquoise] = "Turquoise";
            values[(int)KnownColor.Violet] = "Violet";
            values[(int)KnownColor.Wheat] = "Wheat";
            values[(int)KnownColor.White] = "White";
            values[(int)KnownColor.WhiteSmoke] = "WhiteSmoke";
            values[(int)KnownColor.Yellow] = "Yellow";
            values[(int)KnownColor.YellowGreen] = "YellowGreen";
            s_colorNameTable = values;
        }

        public static int KnownColorToArgb(KnownColor color)
        {
            Debug.Assert(color > 0 && color <= KnownColor.MenuHighlight);
            EnsureColorTable();
            return s_colorTable[unchecked((int)color)];
        }

        public static string KnownColorToName(KnownColor color)
        {
            Debug.Assert(color > 0 && color <= KnownColor.MenuHighlight);
            EnsureColorNameTable();
            return s_colorNameTable[unchecked((int)color)];
        }

#if FEATURE_WINDOWS_SYSTEM_COLORS
        private static int SystemColorToArgb(int index)
        {
            return FromWin32Value(Interop.User32.GetSysColor(index));
        }

        private static int Encode(int alpha, int red, int green, int blue)
        {
            return red << RedShift | green << GreenShift | blue << BlueShift | alpha << AlphaShift;
        }

        private static int FromWin32Value(int value)
        {
            return Encode(255,
                          (value >> Win32RedShift) & 0xFF,
                          (value >> Win32GreenShift) & 0xFF,
                          (value >> Win32BlueShift) & 0xFF);
        }
#endif

        private static void OnUserPreferenceChanging(object sender, EventArgs args)
        {
            Debug.Assert(s_categoryGetter != null);
            // Access UserPreferenceChangingEventArgs.Category value through cached delegate.
            if (s_colorTable != null && s_categoryGetter != null && s_categoryGetter(args) == 2) // UserPreferenceCategory.Color = 2
            {
                UpdateSystemColors(s_colorTable);
            }
        }

        private static void UpdateSystemColors(int[] colorTable)
        {
#if FEATURE_WINDOWS_SYSTEM_COLORS
            colorTable[(int)KnownColor.ActiveBorder] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.ActiveBorder);
            colorTable[(int)KnownColor.ActiveCaption] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.ActiveCaption);
            colorTable[(int)KnownColor.ActiveCaptionText] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.ActiveCaptionText);
            colorTable[(int)KnownColor.AppWorkspace] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.AppWorkspace);
            colorTable[(int)KnownColor.ButtonFace] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.ButtonFace);
            colorTable[(int)KnownColor.ButtonHighlight] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.ButtonHighlight);
            colorTable[(int)KnownColor.ButtonShadow] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.ButtonShadow);
            colorTable[(int)KnownColor.Control] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.Control);
            colorTable[(int)KnownColor.ControlDark] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.ControlDark);
            colorTable[(int)KnownColor.ControlDarkDark] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.ControlDarkDark);
            colorTable[(int)KnownColor.ControlLight] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.ControlLight);
            colorTable[(int)KnownColor.ControlLightLight] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.ControlLightLight);
            colorTable[(int)KnownColor.ControlText] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.ControlText);
            colorTable[(int)KnownColor.Desktop] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.Desktop);
            colorTable[(int)KnownColor.GradientActiveCaption] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.GradientActiveCaption);
            colorTable[(int)KnownColor.GradientInactiveCaption] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.GradientInactiveCaption);
            colorTable[(int)KnownColor.GrayText] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.GrayText);
            colorTable[(int)KnownColor.Highlight] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.Highlight);
            colorTable[(int)KnownColor.HighlightText] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.HighlightText);
            colorTable[(int)KnownColor.HotTrack] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.HotTrack);
            colorTable[(int)KnownColor.InactiveBorder] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.InactiveBorder);
            colorTable[(int)KnownColor.InactiveCaption] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.InactiveCaption);
            colorTable[(int)KnownColor.InactiveCaptionText] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.InactiveCaptionText);
            colorTable[(int)KnownColor.Info] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.Info);
            colorTable[(int)KnownColor.InfoText] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.InfoText);
            colorTable[(int)KnownColor.Menu] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.Menu);
            colorTable[(int)KnownColor.MenuBar] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.MenuBar);
            colorTable[(int)KnownColor.MenuHighlight] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.MenuHighlight);
            colorTable[(int)KnownColor.MenuText] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.MenuText);
            colorTable[(int)KnownColor.ScrollBar] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.ScrollBar);
            colorTable[(int)KnownColor.Window] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.Window);
            colorTable[(int)KnownColor.WindowFrame] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.WindowFrame);
            colorTable[(int)KnownColor.WindowText] = SystemColorToArgb((int)Interop.User32.Win32SystemColors.WindowText);
#else
            // Hard-coded constants, based on default Windows settings.
            colorTable[(int)KnownColor.ActiveBorder] = unchecked((int)0xFFD4D0C8);
            colorTable[(int)KnownColor.ActiveCaption] = unchecked((int)0xFF0054E3);
            colorTable[(int)KnownColor.ActiveCaptionText] = unchecked((int)0xFFFFFFFF);
            colorTable[(int)KnownColor.AppWorkspace] = unchecked((int)0xFF808080);
            colorTable[(int)KnownColor.ButtonFace] = unchecked((int)0xFFF0F0F0);
            colorTable[(int)KnownColor.ButtonHighlight] = unchecked((int)0xFFFFFFFF);
            colorTable[(int)KnownColor.ButtonShadow] = unchecked((int)0xFFA0A0A0);
            colorTable[(int)KnownColor.Control] = unchecked((int)0xFFECE9D8);
            colorTable[(int)KnownColor.ControlDark] = unchecked((int)0xFFACA899);
            colorTable[(int)KnownColor.ControlDarkDark] = unchecked((int)0xFF716F64);
            colorTable[(int)KnownColor.ControlLight] = unchecked((int)0xFFF1EFE2);
            colorTable[(int)KnownColor.ControlLightLight] = unchecked((int)0xFFFFFFFF);
            colorTable[(int)KnownColor.ControlText] = unchecked((int)0xFF000000);
            colorTable[(int)KnownColor.Desktop] = unchecked((int)0xFF004E98);
            colorTable[(int)KnownColor.GradientActiveCaption] = unchecked((int)0xFFB9D1EA);
            colorTable[(int)KnownColor.GradientInactiveCaption] = unchecked((int)0xFFD7E4F2);
            colorTable[(int)KnownColor.GrayText] = unchecked((int)0xFFACA899);
            colorTable[(int)KnownColor.Highlight] = unchecked((int)0xFF316AC5);
            colorTable[(int)KnownColor.HighlightText] = unchecked((int)0xFFFFFFFF);
            colorTable[(int)KnownColor.HotTrack] = unchecked((int)0xFF000080);
            colorTable[(int)KnownColor.InactiveBorder] = unchecked((int)0xFFD4D0C8);
            colorTable[(int)KnownColor.InactiveCaption] = unchecked((int)0xFF7A96DF);
            colorTable[(int)KnownColor.InactiveCaptionText] = unchecked((int)0xFFD8E4F8);
            colorTable[(int)KnownColor.Info] = unchecked((int)0xFFFFFFE1);
            colorTable[(int)KnownColor.InfoText] = unchecked((int)0xFF000000);
            colorTable[(int)KnownColor.Menu] = unchecked((int)0xFFFFFFFF);
            colorTable[(int)KnownColor.MenuBar] = unchecked((int)0xFFF0F0F0);
            colorTable[(int)KnownColor.MenuHighlight] = unchecked((int)0xFF3399FF);
            colorTable[(int)KnownColor.MenuText] = unchecked((int)0xFF000000);
            colorTable[(int)KnownColor.ScrollBar] = unchecked((int)0xFFD4D0C8);
            colorTable[(int)KnownColor.Window] = unchecked((int)0xFFFFFFFF);
            colorTable[(int)KnownColor.WindowFrame] = unchecked((int)0xFF000000);
            colorTable[(int)KnownColor.WindowText] = unchecked((int)0xFF000000);
#endif
        }
    }
}
