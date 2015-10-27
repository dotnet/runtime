// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Tests.ExpressionCompiler.Array
{
    public static class ArrayAccessTests
    {
        [Fact]
        public static void ArrayAccess_MultiDimensionalOf1()
        {
            var arrayType = typeof(int).MakeArrayType(1);
            var arrayCtor = arrayType.GetTypeInfo().DeclaredConstructors.Single(ctor => ctor.GetParameters().Length == 2);
            var arr = (System.Array)arrayCtor.Invoke(new object[] { 1, 1 });
            var c = Expression.Constant(arr);
            var e = Expression.ArrayAccess(c, Expression.Constant(1));

            var set = Expression.Lambda<Action>(Expression.Assign(e, Expression.Constant(42))).Compile();
            set();
            Assert.Equal(42, arr.GetValue(1));

            var get = Expression.Lambda<Func<int>>(e).Compile();
            Assert.Equal(42, get());
        }

        [Fact]
        public static void ArrayIndex_MultiDimensionalOf1()
        {
            var arrayType = typeof(int).MakeArrayType(1);
            var arrayCtor = arrayType.GetTypeInfo().DeclaredConstructors.Single(ctor => ctor.GetParameters().Length == 2);
            var arr = (System.Array)arrayCtor.Invoke(new object[] { 1, 1 });
            arr.SetValue(42, 1);
            var c = Expression.Constant(arr);
            var e = Expression.ArrayIndex(c, Expression.Constant(1));

            var get = Expression.Lambda<Func<int>>(e).Compile();
            Assert.Equal(42, get());
        }
    }
}
