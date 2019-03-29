// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    public abstract class DelegateCreationTests
    {
        public static IEnumerable<object[]> ValidTypeArgs(bool includesReturnType)
        {
            for (int i = 1; i <= (includesReturnType ? 17 : 16); ++i)
            {
                yield return new object[] { Enumerable.Repeat(typeof(bool), i).ToArray() };
            }

            yield return new object[] { new[] { typeof(int) } };
            yield return new object[] { new[] { typeof(int), typeof(string) } };
            yield return new object[] { new[] { typeof(string), typeof(int), typeof(decimal) } };
            yield return new object[] { new[] { typeof(string), typeof(int), typeof(decimal), typeof(float) } };
            yield return new object[] { new[] { typeof(NWindProxy.Customer), typeof(string), typeof(int), typeof(decimal), typeof(float) } };
        }

        public static IEnumerable<object[]> OpenGenericTypeArgs(bool includesReturnType)
        {
            for (int i = 1; i <= (includesReturnType ? 17 : 16); ++i)
            {
                yield return new object[] { Enumerable.Repeat(typeof(List<>).MakeGenericType(typeof(List<>).GetGenericArguments()), i).ToArray() };
            }
        }

        public static IEnumerable<object> ExcessiveLengthTypeArgs()
        {
            yield return new object[] { Enumerable.Repeat(typeof(int), 18).ToArray() };
        }

        public static IEnumerable<object> ExcessiveLengthOpenGenericTypeArgs()
        {
            yield return new object[] { Enumerable.Repeat(typeof(List<int>), 18).ToArray() };
        }

        public static IEnumerable<object> EmptyTypeArgs()
        {
            yield return new object[] { Array.Empty<Type>() };
        }

        public static IEnumerable<object> ByRefTypeArgs()
        {
            yield return new object[] { new[] { typeof(int), typeof(int).MakeByRefType(), typeof(string) } };
            yield return new object[] { new[] { typeof(int).MakeByRefType() } };
            yield return new object[] { Enumerable.Repeat(typeof(double).MakeByRefType(), 20).ToArray() };
        }

        public static IEnumerable<object> ByRefLikeTypeArgs()
        {
            yield return new object[] { new[] { typeof(Span<char>) } };
        }

        public static IEnumerable<object> PointerTypeArgs()
        {
            yield return new object[] { new[] { typeof(int).MakePointerType() } };
            yield return new object[] { new[] { typeof(string), typeof(double).MakePointerType(), typeof(int) } };
            yield return new object[] { Enumerable.Repeat(typeof(int).MakePointerType(), 20).ToArray() };
        }

        public static IEnumerable<object> ManagedPointerTypeArgs()
        {
            yield return new object[] { new[] { typeof(string).MakePointerType() } };
            yield return new object[] { new[] { typeof(int), typeof(string).MakePointerType(), typeof(double) } };
            yield return new object[] { Enumerable.Repeat(typeof(string).MakePointerType(), 20).ToArray() };
        }

        public static IEnumerable<object> VoidTypeArgs(bool includeSingleVoid)
        {
            if (includeSingleVoid)
            {
                yield return new object[] { new[] { typeof(void) } };
            }

            yield return new object[] { new[] { typeof(string), typeof(void), typeof(int) } };
        }
    }
}
