// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class TypeInfo_UnderlyingSystemType
    {
        [Fact]
        public static void UnderlyingSystemType()
        {
            Assert.Equal(typeof(object).GetTypeInfo().UnderlyingSystemType, typeof(object));
            Assert.Equal(typeof(int).GetTypeInfo().UnderlyingSystemType, typeof(int));
            Assert.Equal(typeof(MyCustomType).GetTypeInfo().UnderlyingSystemType, typeof(MyCustomType));

            Type genericListType = typeof(List<>);
            Type listOfObjectType = genericListType.MakeGenericType(typeof(object));
            Assert.Equal(listOfObjectType.GetTypeInfo().UnderlyingSystemType, typeof(List<object>));
        }

        public class MyCustomType { }
    }
}