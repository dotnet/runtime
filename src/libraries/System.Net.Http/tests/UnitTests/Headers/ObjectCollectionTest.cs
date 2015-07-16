// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class ObjectCollectionTest
    {
        [Fact]
        public void Ctor_ExecuteBothOverloads_MatchExpectation()
        {
            // Use default validator
            ObjectCollection<string> c = new ObjectCollection<string>();

            c.Add("value1");
            c.Insert(0, "value2");

            Assert.Throws<ArgumentNullException>(() => { c.Add(null); });
            Assert.Throws<ArgumentNullException>(() => { c[0] = null; });

            Assert.Equal(2, c.Count);
            Assert.Equal("value2", c[0]);
            Assert.Equal("value1", c[1]);

            // Use custom validator
            c = new ObjectCollection<string>(item =>
            {
                if (item == null)
                {
                    throw new InvalidOperationException("custom");
                }
            });

            c.Add("value1");
            c[0] = "value2";

            Assert.Throws<InvalidOperationException>(() => { c.Add(null); });
            Assert.Throws<InvalidOperationException>(() => { c[0] = null; });

            Assert.Equal(1, c.Count);
            Assert.Equal("value2", c[0]);
        }
    }
}
