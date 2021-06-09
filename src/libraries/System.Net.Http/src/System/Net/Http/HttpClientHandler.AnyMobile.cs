// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        private static bool IsSocketHandler => IsNativeHandlerEnabled();

        // check to see if this is linker friendly or not.
        private static bool IsNativeHandlerEnabled()
        {
            if (!AppContext.TryGetSwitch("System.Net.Http.UseNativeHttpHandler", out bool isEnabled))
            {
                return false;
            }

            return isEnabled;
        }
    }
}