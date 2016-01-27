// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Http;

namespace System.Net
{
    public partial class WebException : InvalidOperationException
    {
        internal static WebExceptionStatus GetStatusFromException(HttpRequestException ex)
        {
            WebExceptionStatus status;

            // Issue 2384: update WebException.GetStatusFromException after System.Net.Http API changes
            //
            // For now, we use the .HResult of the exception to help us map to a suitable
            // WebExceptionStatus enum value.  The .HResult is set into this exception by
            // the underlying .NET Core and .NET Native versions of the System.Net.Http stack.
            // In the future, the HttpRequestException will have its own .Status property that is
            // an enum type that is more compatible directly with the WebExceptionStatus enum.
            switch (ex.HResult)
            {
                case Interop.WININET_E_NAME_NOT_RESOLVED:
                    status = WebExceptionStatus.NameResolutionFailure;
                    break;
                default:
                    status = WebExceptionStatus.UnknownError;
                    break;
            }

            return status;
        }
    }
}
