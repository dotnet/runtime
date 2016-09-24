// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    internal static class ExceptionHelper
    {
        internal static NotSupportedException PropertyNotSupportedException => new NotSupportedException(SR.net_PropertyNotSupportedException);

        internal static WebException RequestAbortedException => new WebException(SR.net_reqaborted);

        internal static WebException TimeoutException => new WebException(SR.net_timeout);
    }
}
