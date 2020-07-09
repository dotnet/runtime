// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.TypeSystem;

namespace Internal.JitInterface
{
    internal class UnboxingMethodDescFactory : ConcurrentDictionary<MethodDesc, UnboxingMethodDesc>
    {
        private Func<MethodDesc, UnboxingMethodDesc> _factoryDelegate;
        private UnboxingMethodDesc CreateUnboxingMethod(MethodDesc method)
        {
            return new UnboxingMethodDesc(method, this);
        }

        public UnboxingMethodDescFactory()
        {
            _factoryDelegate = CreateUnboxingMethod;
        }

        public UnboxingMethodDesc GetUnboxingMethod(MethodDesc method)
        {
            return GetOrAdd(method, _factoryDelegate);
        }
    }
}
