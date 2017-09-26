// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.Drawing
{
    internal static class ColorTable
    {
        private static readonly Lazy<Dictionary<string, Color>> s_colorConstants = new Lazy<Dictionary<string, Color>>(GetColors);

        private static Dictionary<string, Color> GetColors()
        {
            var dict = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            FillConstants(dict, typeof(Color));
            return dict;
        }

        internal static Dictionary<string, Color> Colors => s_colorConstants.Value;

        private static void FillConstants(Dictionary<string, Color> colors, Type enumType)
        {
            const MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.Static;
            foreach (PropertyInfo prop in enumType.GetProperties())
            {
                if (prop.PropertyType == typeof(Color))
                {
                    Debug.Assert(prop.GetGetMethod() != null);
                    Debug.Assert((prop.GetGetMethod().Attributes & attrs) == attrs);
                    colors[prop.Name] = (Color)prop.GetValue(null, null);
                }
            }
        }

        internal static bool TryGetNamedColor(string name, out Color result) =>
            Colors.TryGetValue(name, out result);

        internal static bool IsKnownNamedColor(string name)
        {
            Color result;
            return Colors.TryGetValue(name, out result);
        }
    }
}