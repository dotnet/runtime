// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class FakeDisposableCallbackInnerService : FakeDisposableCallbackService, IFakeMultipleService
    {
        public FakeDisposableCallbackInnerService(FakeDisposeCallback callback) : base(callback)
        {
        }
    }
}
