// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Internal
{
    /// <summary>
    /// Enumeration defining the different Graphics properties to apply to a WindowsGraphics when creating it from a
    /// Graphics object.
    /// </summary>
    [Flags]
    internal enum ApplyGraphicsProperties
    {
        // No properties to be applied to the DC obtained from the Graphics object.
        None = 0x00000000,
        // Apply clipping region.
        Clipping = 0x00000001,
        // Apply coordinate transformation.
        TranslateTransform = 0x00000002,
        // Apply all supported Graphics properties.
        All = Clipping | TranslateTransform
    }
}
