// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    public abstract class ParameterExpressionTests
    {
        private static IEnumerable<Type> ValidElementTypes
        {
            get
            {
                return new[]
                {
                    typeof(bool), typeof(byte), typeof(char), typeof(DateTime), typeof(decimal), typeof(double), typeof(short),
                    typeof(int), typeof(long), typeof(object), typeof(sbyte), typeof(float), typeof(string), typeof(ushort),
                    typeof(uint), typeof(ulong), typeof(ParameterExpressionTests), typeof(ExpressionType), typeof(Uri),
                    typeof(DateTimeOffset), typeof(Exception), typeof(InvalidOperationException)
                };
            }
        }

        private static IEnumerable<Type> ValidTypes
        {
            get
            {
                return ValidElementTypes.Concat(ValidElementTypes.Select(i => i.MakeArrayType()));
            }
        }

        private static IEnumerable<Type> ByRefTypes
        {
            get
            {
                return ValidTypes.Select(i => i.MakeByRefType());
            }
        }

        public static IEnumerable<object[]> ValidTypeData()
        {
            return ValidTypes.Select(i => new object[] { i });
        }

        public static IEnumerable<object[]> ByRefTypeData()
        {
            return ByRefTypes.Select(i => new object[] { i });
        }

        private static IEnumerable<object> Values
        {
            get
            {
                return new object[]
                {
                    true, false, (byte)3, '!', DateTime.MinValue, 23, 23m, 23.0, 23U, new object(), 23L, DateTimeOffset.MaxValue,
                    new Uri("http://example.net"), "1Q84", ExpressionType.Parameter
                };
            }
        }

        public static IEnumerable<object[]> ValueData()
        {
            return Values.Select(i => new object[] { i });
        }
    }
}
