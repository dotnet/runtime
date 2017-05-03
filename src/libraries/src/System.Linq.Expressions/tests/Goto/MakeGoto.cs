// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class MakeGoto
    {
        public static IEnumerable<object[]> GotoTypes
            => ((GotoExpressionKind[])Enum.GetValues(typeof(GotoExpressionKind))).Select(kind => new object[] {kind});

        [Theory]
        [MemberData(nameof(GotoTypes))]
        public void OpenGenericType(GotoExpressionKind kind)
        {
            AssertExtensions.Throws<ArgumentException>(
                "type", () => Expression.MakeGoto(kind, Expression.Label(typeof(void)), null, typeof(List<>)));
        }

        [Theory]
        [MemberData(nameof(GotoTypes))]
        public static void TypeContainsGenericParameters(GotoExpressionKind kind)
        {
            AssertExtensions.Throws<ArgumentException>(
                "type", () => Expression.MakeGoto(kind, Expression.Label(typeof(void)), null, typeof(List<>.Enumerator)));
            AssertExtensions.Throws<ArgumentException>(
                "type",
                () =>
                    Expression.MakeGoto(
                        kind, Expression.Label(typeof(void)), null, typeof(List<>).MakeGenericType(typeof(List<>))));
        }

        [Theory]
        [MemberData(nameof(GotoTypes))]
        public void PointerType(GotoExpressionKind kind)
        {
            AssertExtensions.Throws<ArgumentException>(
                "type",
                () => Expression.MakeGoto(kind, Expression.Label(typeof(void)), null, typeof(int).MakePointerType()));
        }

        [Theory]
        [MemberData(nameof(GotoTypes))]
        public void ByRefType(GotoExpressionKind kind)
        {
            AssertExtensions.Throws<ArgumentException>(
                "type",
                () => Expression.MakeGoto(kind, Expression.Label(typeof(void)), null, typeof(int).MakeByRefType()));
        }
    }
}
