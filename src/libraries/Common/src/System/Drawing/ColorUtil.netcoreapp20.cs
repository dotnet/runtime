// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System.Drawing
{
    // System.Drawing.Common uses several members on System.Drawing.Common which are implemented in .NET Core 2.0, but
    // not exposed. This is a helper class which allows System.Drawing.Common to access those members through reflection.
    internal static class ColorUtil
    {
        private const short StateKnownColorValid = 0x0001;
        private const short StateNameValid = 0x0008;
        private const long NotDefinedValue = 0;

        private static readonly ConstructorInfo s_ctorKnownColor; // internal Color(KnownColor knownColor)
        private static readonly ConstructorInfo s_ctorAllValues; // private Color(long value, short state, string name, KnownColor knownColor)
        private static readonly FieldInfo s_fieldKnownColor; // private readonly short knownColor
        private static readonly FieldInfo s_fieldState; // private readonly short state

        static ColorUtil()
        {
            Type colorType = typeof(Color);
            Type knownColorType = colorType.Assembly.GetType("System.Drawing.KnownColor", true);
            Debug.Assert(knownColorType != null);
            const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            s_ctorKnownColor = colorType.GetConstructor(bindingFlags, null, new Type[] { knownColorType }, null);
            Debug.Assert(s_ctorKnownColor != null);

            s_ctorAllValues = colorType.GetConstructor(bindingFlags, null, new Type[] { typeof(long), typeof(short), typeof(string), knownColorType }, null);
            Debug.Assert(s_ctorAllValues != null);

            s_fieldKnownColor = colorType.GetField("knownColor", bindingFlags);
            Debug.Assert(s_fieldKnownColor != null);

            s_fieldState = colorType.GetField("state", bindingFlags);
            Debug.Assert(s_fieldState != null);
        }

        public static Color FromKnownColor(KnownColor color)
        {
            var value = (int)color;
            if (value < (int)KnownColor.ActiveBorder || value > (int)KnownColor.MenuHighlight)
            {
                return FromName(color.ToString());
            }

            return (Color)s_ctorKnownColor.Invoke(new object[] { value });
        }

        public static Color FromName(string name)
        {
            // try to get a known color first
            Color color;
            if (ColorTable.TryGetNamedColor(name, out color))
            {
                return color;
            }
            // otherwise treat it as a named color
            return (Color)s_ctorAllValues.Invoke(new object[] { NotDefinedValue, StateNameValid, name, 0 });
        }

        public static bool IsSystemColor(this Color color)
        {
            short knownColor = GetKnownColor(color);
            return GetIsKnownColor(color) && ((((KnownColor)knownColor) <= KnownColor.WindowText) || (((KnownColor)knownColor) > KnownColor.YellowGreen));
        }

        public static short GetKnownColor(this Color color)
        {
            return (short)s_fieldKnownColor.GetValue(color);
        }

        public static short GetState(this Color color)
        {
            return (short)s_fieldState.GetValue(color);
        }

        public static bool GetIsSystemColor(this Color color)
        {
            short knownColor = color.GetKnownColor();
            return GetIsKnownColor(color) && ((((KnownColor)knownColor) <= KnownColor.WindowText) || (((KnownColor)knownColor) > KnownColor.YellowGreen));
        }

        public static bool GetIsKnownColor(this Color color)
        {
            short state = GetState(color);
            return ((state & StateKnownColorValid) != 0);
        }

        public static KnownColor ToKnownColor(this Color c)
        {
            return (KnownColor)GetKnownColor(c);
        }
    }
}