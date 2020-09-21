// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
        public void IsAssignableTo()
        {
            TypeDelegator td = new TypeDelegator(typeof(int));

            Assert.True(td.IsAssignableTo(typeof(int)));
            Assert.False(td.IsAssignableTo(typeof(string)));
            Assert.True(typeof(int).IsAssignableTo(td));
            Assert.False(typeof(string).IsAssignableTo(td));
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

        public static IEnumerable<object[]> SZArrayOrNotTypes()
        {
            yield return new object[] { typeof(int[]), true };
            yield return new object[] { typeof(string[]), true };
            yield return new object[] { typeof(void), false };
            yield return new object[] { typeof(int), false };
            yield return new object[] { typeof(int[]).MakeByRefType(), false };
            yield return new object[] { typeof(int[,]), false };
            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                yield return new object[] { Array.CreateInstance(typeof(int), new[] { 2 }, new[] { -1 }).GetType(), false };
                yield return new object[] { Array.CreateInstance(typeof(int), new[] { 2 }, new[] { 1 }).GetType(), false };
            }
            yield return new object[] { Array.CreateInstance(typeof(int), new[] { 2 }, new[] { 0 }).GetType(), true };
            yield return new object[] { typeof(int[][]), true };
            yield return new object[] { Type.GetType("System.Int32[]"), true };
            yield return new object[] { Type.GetType("System.Int32[*]"), false };
            yield return new object[] { Type.GetType("System.Int32"), false };
            yield return new object[] { typeof(int).MakeArrayType(), true };
            yield return new object[] { typeof(int).MakeArrayType(1), false };
            yield return new object[] { typeof(int).MakeArrayType().MakeArrayType(), true };
            yield return new object[] { typeof(int).MakeArrayType(2), false };
            yield return new object[] { typeof(Outside<int>.Inside<string>), false };
            yield return new object[] { typeof(Outside<int>.Inside<string>[]), true };
            yield return new object[] { typeof(Outside<int>.Inside<string>[,]), false };
            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                yield return new object[] { Array.CreateInstance(typeof(Outside<int>.Inside<string>), new[] { 2 }, new[] { -1 }).GetType(), false };
            }
        }

        [Theory, MemberData(nameof(SZArrayOrNotTypes))]
        public void IsSZArray(Type type, bool expected)
        {
            Assert.Equal(expected, new TypeDelegator(type).IsSZArray);
        }
    }
}
