// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.TypeSystem;

namespace Internal.JitInterface
{
    internal class AsyncMethodDescFactory : ConcurrentDictionary<MethodDesc, AsyncMethodDesc>
    {
        private Func<MethodDesc, AsyncMethodDesc> _factoryDelegate;
        private AsyncMethodDesc CreateAsyncMethod(MethodDesc method)
        {
            return new AsyncMethodDesc(method, this);
        }

        public AsyncMethodDescFactory()
        {
            _factoryDelegate = CreateAsyncMethod;
        }

        public AsyncMethodDesc GetAsyncMethod(MethodDesc method)
        {
            return GetOrAdd(method, _factoryDelegate);
        }
    }
}
