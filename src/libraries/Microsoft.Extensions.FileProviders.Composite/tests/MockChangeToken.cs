// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
