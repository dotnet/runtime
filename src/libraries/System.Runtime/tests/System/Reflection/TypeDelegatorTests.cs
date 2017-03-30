// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class TypeDelegatorTests
    {
        [Fact]
        public void IsAssignableFrom()
        {
            TypeDelegator td = new TypeDelegator(typeof(int));

            Assert.True(typeof(int).IsAssignableFrom(td));
            Assert.False(typeof(string).IsAssignableFrom(td));
            Assert.True(td.IsAssignableFrom(typeof(int)));
            Assert.False(td.IsAssignableFrom(typeof(string)));
        }

        [Fact]
        public void CreateInstance()
        {
            Assert.Equal(typeof(int[]), Array.CreateInstance(new TypeDelegator(typeof(int)), 100).GetType());
        }

        [Fact]
        public void Properties()
        {
            Assert.False(new TypeDelegator(typeof(IComparable)).IsClass);
            Assert.False(new TypeDelegator(typeof(IComparable)).IsValueType);
            Assert.False(new TypeDelegator(typeof(IComparable)).IsEnum);
            Assert.True(new TypeDelegator(typeof(IComparable)).IsInterface);

            Assert.True(new TypeDelegator(typeof(TypeDelegatorTests)).IsClass);
            Assert.False(new TypeDelegator(typeof(TypeDelegatorTests)).IsValueType);
            Assert.False(new TypeDelegator(typeof(TypeDelegatorTests)).IsInterface);

            Assert.False(new TypeDelegator(typeof(TypeCode)).IsClass);
            Assert.False(new TypeDelegator(typeof(TypeCode)).IsInterface);
            Assert.True(new TypeDelegator(typeof(TypeCode)).IsValueType);
            Assert.True(new TypeDelegator(typeof(TypeCode)).IsEnum);
        }
    }
}
