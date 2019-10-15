// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Internal
{
    /// <summary>
    /// Represent the device type the context refers to.
    /// </summary>
    internal enum DeviceContextType
    {
        // Unknown device
        Unknown = 0x00,

        // Display DC - obtained from GetDC/GetDCEx/BeginPaint.
        Display = 0x01,

        // Window DC including non-client area - obtained from GetWindowDC
        NCWindow = 0x02,

        // Printer DC - obtained from CreateDC.
        NamedDevice = 0x03,

        // Information context - obtained from CreateIC.
        Information = 0x04,

        // Memory dc - obtained from CreateCompatibleDC.
        Memory = 0x05,

        // Metafile dc - obtained from CreateEnhMetafile.
        Metafile = 0x06 // currently not supported.
    }
}
