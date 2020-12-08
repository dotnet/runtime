// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.FileProviders.Composite
{
    public class MockChangeToken : IChangeToken
    {
        private readonly List<Tuple<Action<object>, object, MockDisposable>> _callbacks = new List<Tuple<Action<object>, object, MockDisposable>>();

        public bool ActiveChangeCallbacks { get; set; }

        public bool HasChanged { get; set; }

        public List<Tuple<Action<object>, object, MockDisposable>> Callbacks
        {
            get
            {
                return _callbacks;
            }
        }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            var disposable = new MockDisposable();
            _callbacks.Add(Tuple.Create(callback, state, disposable));
            return disposable;
        }

        internal void RaiseCallback(object item)
        {
            foreach (var callback in _callbacks)
            {
                callback.Item1(item);
            }
        }
    }
}
