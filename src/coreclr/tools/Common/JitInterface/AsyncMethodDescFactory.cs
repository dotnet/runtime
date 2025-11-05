// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.TypeSystem;

namespace Internal.JitInterface
{
    internal class AsyncMethodDescFactory : Dictionary<MethodDesc, AsyncMethodDesc>
    {
        public AsyncMethodDesc GetAsyncMethod(MethodDesc method)
        {
            if (!TryGetValue(method, out AsyncMethodDesc result))
            {
                result = new AsyncMethodDesc(method, this);
                Add(method, result);
            }

            return result;
        }
    }
}
