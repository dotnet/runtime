// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
    [SupportedOSPlatform("windows")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ComEventsHelper
    {
        public static void Combine(object rcw, Guid iid, int dispid, Delegate d)
        {
            throw new PlatformNotSupportedException();
        }

        public static Delegate? Remove(object rcw, Guid iid, int dispid, Delegate d)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
