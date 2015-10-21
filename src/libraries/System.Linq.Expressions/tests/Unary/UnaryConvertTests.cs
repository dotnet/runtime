// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Unary
{
    public static class UnaryConvertTests
    {
        #region Test methods

        [Fact] // [Issue(4019, "https://github.com/dotnet/corefx/issues/4019")]
        public static void CheckUnaryConvertBooleanToNumericTest()
        {
            foreach (var kv in ConvertBooleanToNumeric())
            {
                VerifyUnaryConvert(kv.Key, kv.Value);
            }
        }

        private static IEnumerable<KeyValuePair<Expression, object>> ConvertBooleanToNumeric()
        {
            var boolF = Expression.Constant(false);
            var boolT = Expression.Constant(true);

            var factories = new Func<Expression, Type, Expression>[] { Expression.Convert, Expression.ConvertChecked };

            foreach (var factory in factories)
            {
                foreach (var b in new[] { false, true })
                {
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(byte)), (byte)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(sbyte)), (sbyte)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(ushort)), (ushort)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(short)), (short)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(uint)), (uint)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(int)), (int)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(ulong)), (ulong)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(long)), (long)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(float)), (float)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(double)), (double)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(char)), (char)(b ? 1 : 0));
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyUnaryConvert(Expression e, object o)
        {
            Expression<Func<object>> f =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(e, typeof(object)));

            Func<object> c = f.Compile();
            Assert.Equal(o, c());

#if FEATURE_INTERPRET
            Func<object> i = f.Compile(true);
            Assert.Equal(o, i());
#endif
        }

        #endregion
    }
}
