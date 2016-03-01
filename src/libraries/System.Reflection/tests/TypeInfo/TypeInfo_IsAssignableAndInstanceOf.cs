// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace System.Reflection.Tests
{
    public class TypeInfo_IsAssignableAndInstanceOf
    {
        [Fact]
        public static void IsInstanceOfType()
        {
            Assert.True(typeof(Array).GetTypeInfo().IsInstanceOfType(new int[] { }));
            Assert.True(typeof(MyCustomType).GetTypeInfo().IsInstanceOfType(new MyCustomType()));
            Assert.True(typeof(IFace).GetTypeInfo().IsInstanceOfType(new MyCustomType()));
        }

        [Fact]
        public static void IsAssignableFrom()
        {
            Assert.True(typeof(MyCustomType).GetTypeInfo().IsAssignableFrom(typeof(MyCustomType)));
            Assert.True(typeof(IFace).GetTypeInfo().IsAssignableFrom(typeof(MyCustomType)));
            Assert.True(typeof(object).GetTypeInfo().IsAssignableFrom(typeof(MyCustomType)));
            Assert.True(typeof(Nullable<int>).GetTypeInfo().IsAssignableFrom(typeof(int)));
            Assert.False(typeof(List<int>).GetTypeInfo().IsAssignableFrom(typeof(List<>)));
        }

        public interface IFace { }
        public class MyCustomType : IFace { }
    }
}