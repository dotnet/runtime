// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Numerics.Tests
{
    public sealed class IExponentialFunctionsTests
    {
        const float baseValue = 2.1f;
        static readonly ExponentialFunctionsDimHelper helperValue = new ExponentialFunctionsDimHelper(baseValue);

        [Fact]
        public static void Exp10M1Test()
        {
            Assert.Equal(float.Exp10M1(baseValue), ExponentialFunctionsHelper<ExponentialFunctionsDimHelper>.Exp10M1(helperValue).Value);
        }
    }
}
