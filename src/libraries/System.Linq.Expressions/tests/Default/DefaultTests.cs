// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class DefaultTests
    {
        private enum MyEnum
        {
            Value
        }

        private class EnumOutLambdaClass
        {
            public static void BarRef(out MyEnum o)
            {
                o = MyEnum.Value;
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void DefaultEnumRef(bool useInterpreter)
        {
            ParameterExpression x = Expression.Variable(typeof(MyEnum), "x");

            Expression<Action> expression = Expression.Lambda<Action>(
                            Expression.Block(
                            new[] { x },
                            Expression.Assign(x, Expression.Default(typeof(MyEnum))),
                            Expression.Call(null, typeof(EnumOutLambdaClass).GetMethod(nameof(EnumOutLambdaClass.BarRef)), x)));

            expression.Compile(useInterpreter)();
        }

        [Fact]
        public void DefaultNotSingleton()
        {
            Assert.NotSame(Expression.Empty(), Expression.Empty());
            Assert.NotSame(Expression.Default(typeof(void)), Expression.Default(typeof(void)));
            Assert.NotSame(Expression.Default(typeof(object)), Expression.Default(typeof(object)));
        }

        [Fact]
        public void NullType()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Default(null));
        }

        [Fact]
        public void ByRefType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Default(typeof(int).MakeByRefType()));
        }

        [Fact]
        public void PointerType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Default(typeof(int).MakePointerType()));
        }

        [Fact]
        public void GenericType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Default(typeof(List<>)));
        }

        [Fact]
        public void TypeContainsGenericParameters()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Default(typeof(List<>.Enumerator)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Default(typeof(List<>).MakeGenericType(typeof(List<>))));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void DbNull(bool useInterpreter)
        {
            Expression<Func<DBNull>> lambda = Expression.Lambda<Func<DBNull>>(Expression.Default(typeof(DBNull)));
            Func<DBNull> func = lambda.Compile(useInterpreter);
            Assert.Null(func());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void StructType(bool useInterpreter)
        {
            Expression<Func<Sp>> lambda = Expression.Lambda<Func<Sp>>(Expression.Default(typeof(Sp)));
            Func<Sp> func = lambda.Compile(useInterpreter);
            Sp defaultValue = func();
            Assert.Equal(0, defaultValue.I);
            Assert.Equal(0.0, defaultValue.D);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void PrivateStructType(bool useInterpreter)
        {
            Expression<Func<StPri>> lambda = Expression.Lambda<Func<StPri>>(Expression.Default(typeof(StPri)));
            Func<StPri> func = lambda.Compile(useInterpreter);
            StPri defaultValue = func();
            Assert.Equal(0, defaultValue.IntProperty);
        }

        private struct StPri
        {
            public int IntProperty { get; set; }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void StructTypeWithParameterlessConstructor(bool useInterpreter)
        {
            Expression<Func<ValueTypeWithParameterlessConstructor>> lambda =
                Expression.Lambda<Func<ValueTypeWithParameterlessConstructor>>(
                    Expression.Default(typeof(ValueTypeWithParameterlessConstructor)));
            Func<ValueTypeWithParameterlessConstructor> func = lambda.Compile(useInterpreter);
            ValueTypeWithParameterlessConstructor defaultValue = func();
            Assert.False(defaultValue.ConstructorWasRun);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void StructTypeWithParameterlessConstructorThatThrows(bool useInterpreter)
        {
            Expression<Func<ValueTypeWithParameterlessConstructorThatThrows>> lambda =
                Expression.Lambda<Func<ValueTypeWithParameterlessConstructorThatThrows>>(
                    Expression.Default(typeof(ValueTypeWithParameterlessConstructorThatThrows)));
            Func<ValueTypeWithParameterlessConstructorThatThrows> func = lambda.Compile(useInterpreter);
            ValueTypeWithParameterlessConstructorThatThrows defaultValue = func();
            Assert.Null(defaultValue.Value);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void NullableStructTypeWithParameterlessConstructor(bool useInterpreter)
        {
            Expression<Func<ValueTypeWithParameterlessConstructorThatThrows?>> lambda =
                Expression.Lambda<Func<ValueTypeWithParameterlessConstructorThatThrows?>>(
                    Expression.Default(typeof(ValueTypeWithParameterlessConstructorThatThrows?)));
            Func<ValueTypeWithParameterlessConstructorThatThrows?> func = lambda.Compile(useInterpreter);
            ValueTypeWithParameterlessConstructorThatThrows? defaultValue = func();
            Assert.Null(defaultValue);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void GetValueOrDefault(bool useInterpreter)
        {
            Expression<Func<ValueTypeWithParameterlessConstructor?, ValueTypeWithParameterlessConstructor>> e =
                (ValueTypeWithParameterlessConstructor? x) => x.GetValueOrDefault();

            Func<ValueTypeWithParameterlessConstructor?, ValueTypeWithParameterlessConstructor> f = e.Compile(useInterpreter);
            Assert.True(f(new ValueTypeWithParameterlessConstructor()).ConstructorWasRun);
            Assert.False(f(null).ConstructorWasRun);
        }
    }
}
