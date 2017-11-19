// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Threading;

namespace System
{
    internal static class InternalHelpers
    {
        internal static void SetErrorCode(this Exception ex, int code)
        {
            // Stub, until COM interop guys fix the exception logic
        }

        internal static void TryDeregister(this CancellationTokenRegistration ctr)
        {
            //nothing to do for projectN
        }
    }
}
