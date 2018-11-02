// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class FakeDisposableCallbackService: IDisposable
    {
        private static int _globalId;
        private readonly int _id;
        private readonly FakeDisposeCallback _callback;

        public FakeDisposableCallbackService(FakeDisposeCallback callback)
        {
            _id = _globalId++;
            _callback = callback;
        }

        public void Dispose()
        {
            _callback.Disposed.Add(this);
        }

        public override string ToString()
        {
            return _id.ToString();
        }
    }
}