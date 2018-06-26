// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class MethodBaseNetcoreTests
    {
        [Fact]
        public static void Test_GetCurrentMethod_ConstructedGenericMethod()
        {
            MethodInfo mi = typeof(MethodBaseNetcoreTests).GetMethod(nameof(MyFakeGenericMethod));
            MethodBase m = mi.MakeGenericMethod(typeof(byte));

            Assert.Equal(nameof(MyFakeGenericMethod), m.Name);
            Assert.Equal(typeof(MethodBaseNetcoreTests), m.ReflectedType);
            Assert.True(m.IsGenericMethod);
            Assert.False(m.IsGenericMethodDefinition);
            Assert.True(m.IsConstructedGenericMethod);
            Assert.Equal(1, m.GetGenericArguments().Length);
            Assert.Equal(typeof(byte), m.GetGenericArguments()[0]);
        }

        [Fact]
        public static void Test_GetCurrentMethod_GenericMethodDefinition()
        {
            MethodBase m = typeof(MethodBaseNetcoreTests).GetMethod(nameof(MyFakeGenericMethod));

            Assert.Equal(nameof(MyFakeGenericMethod), m.Name);
            Assert.Equal(typeof(MethodBaseNetcoreTests), m.ReflectedType);
            Assert.True(m.IsGenericMethod);
            Assert.True(m.IsGenericMethodDefinition);
            Assert.False(m.IsConstructedGenericMethod);
            Assert.Equal(1, m.GetGenericArguments().Length);
            Assert.Equal("T", m.GetGenericArguments()[0].Name);
        }

        public void MyFakeGenericMethod<T>()
        {
        }
    }
}
