// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;

namespace System.Net.NetworkInformation
{
    internal static partial class BrowserNetworkInterfaceInterop
    {
        [JSImport("INTERNAL.network_wasm_is_online")]
        public static partial bool IsOnline();

        [JSImport("INTERNAL.network_wasm_set_change_listener")]
        public static partial void SetChangeListener([JSMarshalAs<JSType.Function<JSType.Boolean>>] Action<bool> handler);

        [JSImport("INTERNAL.network_wasm_remove_change_listener")]
        public static partial void RemoveChangeListener();
    }
}
