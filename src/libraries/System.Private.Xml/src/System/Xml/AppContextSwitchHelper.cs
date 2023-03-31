// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml
{
    internal static class AppContextSwitchHelper
    {
        public static bool AllowResolvingUrlsByDefault { get; } =
            AppContext.TryGetSwitch(
                switchName: "System.Xml.AllowResolvingUrlsByDefault",
                isEnabled: out bool value)
            ? value : true;
    }
}
