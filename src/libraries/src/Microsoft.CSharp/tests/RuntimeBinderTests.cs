// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class RuntimeBinderTests
    {
        [Fact]
        public void MultipleUseOfSameLocalInSameScope()
        {
            dynamic d0 = 23;
            dynamic d1 = 14;
            if (d0 == 23)
            {
                dynamic d2 = 19;
                d0 = d0 - d1 + d2;
                Assert.Equal(28, new string(' ', d0).Length);
            }
            dynamic dr = d0 * d1 + d0 + d0 + d0 / d1 - Math.Pow(d1, 2);
            Assert.Equal(254, dr);
        }

        private class Value<T>
        {
            public T Quantity { get; set; }
        }

        private class Holder
        {
            private object _value;

            public void Assign<T>(T value) => _value = value;

            public T Value<T>() => (T)_value;
        }

        [Fact]
        public void GenericNameMatchesPredefined()
        {
            dynamic d = 3;
            dynamic v = new Value<int> {Quantity = d};
            dynamic r = v.Quantity;
            Assert.Equal(3, r);
            dynamic h = new Holder();
            h.Assign<int>(1);
            Assert.Equal(1, h.Value<int>());
            h.Assign(2);
            Assert.Equal(2, h.Value<int>());
        }
    }
}
