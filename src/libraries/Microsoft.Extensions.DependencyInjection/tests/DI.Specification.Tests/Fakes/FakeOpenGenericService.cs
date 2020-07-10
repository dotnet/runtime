// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class FakeOpenGenericService<TVal> : IFakeOpenGenericService<TVal>
    {
        public FakeOpenGenericService(TVal value)
        {
            Value = value;
        }

        public TVal Value { get; }
    }
}
