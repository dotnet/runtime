// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public class TypeDelegatorNetcoreTests
    {
        private static IEnumerable<object[]> SZArrayOrNotTypes()
        {
            yield return new object[] { typeof(int[]), true };
            yield return new object[] { typeof(string[]), true };
            yield return new object[] { typeof(void), false };
            yield return new object[] { typeof(int), false };
            yield return new object[] { typeof(int[]).MakeByRefType(), false };
            yield return new object[] { typeof(int[,]), false };
            yield return new object[] { typeof(TypeInfoTests), false };
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
