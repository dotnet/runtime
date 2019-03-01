// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace System.Threading
{
    internal static class CancellationTokenExtensions
    {
        internal static CancellationTokenRegistration UnsafeRegister(this CancellationToken cancellationToken, Action<object> callback, object state)
        {
            return cancellationToken.Register(callback, state);
        }
    }
}
