// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class ArrayAccessTests
    {
        private static IEnumerable<object[]> Ranks() => Enumerable.Range(1, 5).Select(i => new object[] {i});

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNonZeroLowerBoundArraySupported))]
        [ClassData(typeof(CompilationTypes))]
        public static void ArrayAccess_MultiDimensionalOf1(bool useInterpreter)
        {
            Type arrayType = typeof(int).MakeArrayType(1);
            ConstructorInfo arrayCtor = arrayType.GetTypeInfo().DeclaredConstructors.Single(ctor => ctor.GetParameters().Length == 2);
            var arr = (System.Array)arrayCtor.Invoke(new object[] { 1, 1 });
            ConstantExpression c = Expression.Constant(arr);
            IndexExpression e = Expression.ArrayAccess(c, Expression.Constant(1));

            Action set = Expression.Lambda<Action>(Expression.Assign(e, Expression.Constant(42))).Compile(useInterpreter);
            set();
            Assert.Equal(42, arr.GetValue(1));

            Func<int> get = Expression.Lambda<Func<int>>(e).Compile(useInterpreter);
            Assert.Equal(42, get());
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNonZeroLowerBoundArraySupported))]
        [ClassData(typeof(CompilationTypes))]
        public static void ArrayIndex_MultiDimensionalOf1(bool useInterpreter)
        {
            Type arrayType = typeof(int).MakeArrayType(1);
            ConstructorInfo arrayCtor = arrayType.GetTypeInfo().DeclaredConstructors.Single(ctor => ctor.GetParameters().Length == 2);
            var arr = (System.Array)arrayCtor.Invoke(new object[] { 1, 1 });
            arr.SetValue(42, 1);
            ConstantExpression c = Expression.Constant(arr);
            BinaryExpression e = Expression.ArrayIndex(c, Expression.Constant(1));

            Func<int> get = Expression.Lambda<Func<int>>(e).Compile(useInterpreter);
            Assert.Equal(42, get());
        }

        [Fact]
        public static void InstanceIsNotArray()
        {
            ConstantExpression instance = Expression.Constant(46);
            ConstantExpression index = Expression.Constant(2);
            AssertExtensions.Throws<ArgumentException>("array", () => Expression.ArrayAccess(instance, index));
        }

        [Fact]
        public static void WrongNumberIndices()
        {
            ConstantExpression instance = Expression.Constant(new int[2,3]);
            ConstantExpression index = Expression.Constant(2);
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.ArrayAccess(instance, index));
        }

        [Fact]
        public static void NonInt32Index()
        {
            ConstantExpression instance = Expression.Constant(new int[4]);
            ConstantExpression index = Expression.Constant("2");
            AssertExtensions.Throws<ArgumentException>("indexes", () => Expression.ArrayAccess(instance, index));
        }

        [Fact]
        public static void UnreadableIndex()
        {
            ConstantExpression instance = Expression.Constant(new int[4]);
            MemberExpression index = Expression.Property(null, typeof(Unreadable<int>).GetProperty(nameof(Unreadable<int>.WriteOnly)));
            AssertExtensions.Throws<ArgumentException>("indexes", () => Expression.ArrayAccess(instance, index));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNonZeroLowerBoundArraySupported))]
        [ClassData(typeof(CompilationTypes))]
        public static void NonZeroBasedOneDimensionalArrayAccess(bool useInterpreter)
        {
            Array arrayObj = Array.CreateInstance(typeof(int), new[] { 3 }, new[] { -1 });
            arrayObj.SetValue(5, -1);
            arrayObj.SetValue(6, 0);
            arrayObj.SetValue(7, 1);
            ConstantExpression array = Expression.Constant(arrayObj);
            IndexExpression indexM1 = Expression.ArrayAccess(array, Expression.Constant(-1));
            IndexExpression index0 = Expression.ArrayAccess(array, Expression.Constant(0));
            IndexExpression index1 = Expression.ArrayAccess(array, Expression.Constant(1));
            Action setValues = Expression.Lambda<Action>(
                Expression.Block(
                    Expression.Assign(indexM1, Expression.Constant(5)),
                    Expression.Assign(index0, Expression.Constant(6)),
                    Expression.Assign(index1, Expression.Constant(7)))).Compile(useInterpreter);
            setValues();
            Assert.Equal(5, arrayObj.GetValue(-1));
            Assert.Equal(6, arrayObj.GetValue(0));
            Assert.Equal(7, arrayObj.GetValue(1));
            Func<bool> testValues = Expression.Lambda<Func<bool>>(
                Expression.And(
                    Expression.Equal(indexM1, Expression.Constant(5)),
                    Expression.And(
                        Expression.Equal(index0, Expression.Constant(6)),
                        Expression.Equal(index1, Expression.Constant(7))))).Compile(useInterpreter);
            Assert.True(testValues());
        }

        [Theory, PerCompilationType(nameof(Ranks))]
        public static void DifferentRanks(int rank, bool useInterpreter)
        {
            Array arrayObj = Array.CreateInstance(typeof(string), Enumerable.Repeat(1, rank).ToArray());
            arrayObj.SetValue("solitary value", Enumerable.Repeat(0, rank).ToArray());
            ConstantExpression array = Expression.Constant(arrayObj);
            IEnumerable<DefaultExpression> indices = Enumerable.Repeat(Expression.Default(typeof(int)), rank);
            Func<string> func = Expression.Lambda<Func<string>>(
                Expression.ArrayAccess(array, indices)).Compile(useInterpreter);
            Assert.Equal("solitary value", func());
        }
    }
}
