// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
