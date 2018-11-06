// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Options.Tests
{
    public class FakeChangeToken : IChangeToken, IDisposable
    {
        public bool ActiveChangeCallbacks { get; set; }
        public bool HasChanged { get; set; }
        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            _callback = () => callback(state);
            return this;
        }

        public void InvokeChangeCallback()
        {
            _callback?.Invoke();
        }

        public void Dispose()
        {
            _callback = null;
        }

        private Action _callback;
    }
}
