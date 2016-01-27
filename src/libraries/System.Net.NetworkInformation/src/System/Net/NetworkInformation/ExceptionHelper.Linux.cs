// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    internal static class ExceptionHelper
    {
        public static NetworkInformationException CreateForParseFailure()
        {
            return new NetworkInformationException(SR.net_FailedToParseNetworkFile);
        }

        public static NetworkInformationException CreateForInformationUnavailable()
        {
            return new NetworkInformationException(SR.net_InformationUnavailableOnPlatform);
        }
    }
}
