// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class IsEquivalentToReflectionTests
    {
        [Fact]
        public void IsEquivalentTo()
        {
            Assert.True(typeof(string).GetTypeInfo().IsEquivalentTo(typeof(string)));
            Assert.False(typeof(object).GetTypeInfo().IsEquivalentTo(typeof(string)));

            Object o = "stringAsObject";
            string s = "stringAsString";

            Assert.True(o.GetType().GetTypeInfo().IsEquivalentTo(s.GetType()));
            Assert.True(typeof(IsEquivalentToReflectionTests.MyClass1).GetTypeInfo().IsEquivalentTo(typeof(IsEquivalentToReflectionTests.MyClass1)));
        }

        public class MyClass1 { }
    }

}
