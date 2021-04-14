// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class FakeOpenGenericServiceWithTwoTypeArguments<TVal1, TVal2> : IFakeOpenGenericService<TVal1>
    {
        public FakeOpenGenericServiceWithTwoTypeArguments(TVal1 value1, TVal2 value2)
        {
            Value = value1;
            Value2 = value2;
        }

        public TVal1 Value { get; }
        public TVal2 Value2 { get; }
    }
}
