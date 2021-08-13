// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Versioning;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        public virtual bool SupportsAutomaticDecompression => false;
        public virtual bool SupportsProxy => false;
        public virtual bool SupportsRedirectConfiguration => true;

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public DecompressionMethods AutomaticDecompression
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public bool UseProxy
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public IWebProxy? Proxy
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public int MaxAutomaticRedirections
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public bool PreAuthenticate
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }
    }
}
