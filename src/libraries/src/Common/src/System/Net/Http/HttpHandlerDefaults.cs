// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Http
{
    /// <summary>
    /// Defines default values for http handler properties which is meant to be re-used across WinHttp & UnixHttp Handlers
    /// </summary>
    internal static class HttpHandlerDefaults
    {
        public const int DefaultMaxAutomaticRedirections = 50;
        public const DecompressionMethods DefaultAutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        public const bool DefaultAutomaticRedirection = true;
        public const bool DefaultUseCookies = true;
        public const bool DefaultPreAuthenticate = false;
    }
}