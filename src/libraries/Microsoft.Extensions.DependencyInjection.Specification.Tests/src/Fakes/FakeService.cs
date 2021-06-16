// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class FakeService : IFakeEveryService, IDisposable
    {
        public PocoClass Value { get; set; }

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(FakeService));
            }

            Disposed = true;
        }
    }
}
