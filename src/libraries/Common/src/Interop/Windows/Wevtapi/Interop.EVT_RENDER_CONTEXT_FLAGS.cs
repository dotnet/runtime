// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        internal enum EVT_RENDER_CONTEXT_FLAGS
        {
            EvtRenderContextValues = 0,      // Render specific properties
            EvtRenderContextSystem = 1,      // Render all system properties (System)
            EvtRenderContextUser = 2         // Render all user properties (User/EventData)
        }
    }
}
