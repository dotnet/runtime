// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Threading;

namespace System.Net
{
    internal sealed class HttpRequestQueueV2Handle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public HttpRequestQueueV2Handle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return (Interop.HttpApi.HttpCloseRequestQueue(handle) == Interop.HttpApi.ERROR_SUCCESS);
        }
    }
}
