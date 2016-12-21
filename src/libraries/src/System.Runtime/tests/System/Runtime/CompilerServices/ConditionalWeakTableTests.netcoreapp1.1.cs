// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public partial class ConditionalWeakTableTests
    {
        [Fact]
        public static void AddOrUpdateDataTest()
        {
            ConditionalWeakTable<string, string> cwt = new ConditionalWeakTable<string, string>();
            string key = "key1";
            cwt.AddOrUpdate(key, "value1");

            string value;
            Assert.True(cwt.TryGetValue(key, out value));
            Assert.Equal(value, "value1");
            Assert.Equal(value, cwt.GetOrCreateValue(key));
            Assert.Equal(value, cwt.GetValue(key, k => "value1"));

            Assert.Throws<ArgumentNullException>(() => cwt.AddOrUpdate(null, "value2"));

            cwt.AddOrUpdate(key, "value2");
            Assert.True(cwt.TryGetValue(key, out value));
            Assert.Equal(value, "value2");
            Assert.Equal(value, cwt.GetOrCreateValue(key));
            Assert.Equal(value, cwt.GetValue(key, k => "value1"));
        }
    }
}
