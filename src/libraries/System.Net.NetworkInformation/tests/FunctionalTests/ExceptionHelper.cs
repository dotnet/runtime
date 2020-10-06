// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    internal static class ExceptionHelper
    {
        public static NetworkInformationException CreateForParseFailure()
        {
            return new NetworkInformationException();
        }

        public static NetworkInformationException CreateForInformationUnavailable()
        {
            return new NetworkInformationException();
        }
    }
}
