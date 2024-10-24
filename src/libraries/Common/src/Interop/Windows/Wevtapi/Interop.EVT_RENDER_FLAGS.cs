// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        internal enum EVT_RENDER_FLAGS
        {
            EvtRenderEventValues = 0,       // Variants
            EvtRenderEventXml = 1,          // XML
            EvtRenderBookmark = 2           // Bookmark
        }
    }
}
