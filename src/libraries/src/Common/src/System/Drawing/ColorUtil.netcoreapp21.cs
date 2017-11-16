// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System.Drawing
{
    internal static class ColorUtil
    {
        public static Color FromKnownColor(KnownColor color) => Color.FromKnownColor(color);
        public static bool IsSystemColor(this Color color) => color.IsSystemColor;
    }
}