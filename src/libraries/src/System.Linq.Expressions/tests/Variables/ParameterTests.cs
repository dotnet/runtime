// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class ParameterTests : ParameterExpressionTests
    {
        [Theory]
        [MemberData(nameof(ValidTypeData))]
        public void CreateParameterForValidTypeNoName(Type type)
        {
            ParameterExpression param = Expression.Parameter(type);
            Assert.Equal(type, param.Type);
            Assert.False(param.IsByRef);
            Assert.Null(param.Name);
        }

        [Theory]
        [MemberData(nameof(ValidTypeData))]
        public void CrateParamForValidTypeWithName(Type type)
        {
            ParameterExpression param = Expression.Parameter(type, "name");
            Assert.Equal(type, param.Type);
            Assert.False(param.IsByRef);
            Assert.Equal("name", param.Name);
        }

        [Fact]
        public void NameNeedNotBeCSharpValid()
        {
            ParameterExpression param = Expression.Parameter(typeof(int), "a name with characters not allowed in C# <, >, !, =, \0, \uFFFF, &c.");
            Assert.Equal("a name with characters not allowed in C# <, >, !, =, \0, \uFFFF, &c.", param.Name);
        }

        [Fact]
        public void ParameterCannotBeTypeVoid()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Parameter(typeof(void)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Parameter(typeof(void), "var"));
        }

        [Theory]
        [ClassData(typeof(InvalidTypesData))]
        public void OpenGenericType_ThrowsArgumentException(Type type)
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Parameter(type));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Parameter(type, "name"));
        }

        [Fact]
        public void NullType()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Parameter(null));
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Parameter(null, "var"));
        }

        [Theory]
        [MemberData(nameof(ByRefTypeData))]
        public void ParameterCanBeByRef(Type type)
        {
            ParameterExpression param = Expression.Parameter(type);
            Assert.Equal(type.GetElementType(), param.Type);
            Assert.True(param.IsByRef);
            Assert.Null(param.Name);
        }

        [Theory]
        [MemberData(nameof(ByRefTypeData))]
        public void NamedParameterCanBeByRef(Type type)
        {
            ParameterExpression param = Expression.Parameter(type, "name");
            Assert.Equal(type.GetElementType(), param.Type);
            Assert.True(param.IsByRef);
            Assert.Equal("name", param.Name);
        }

        [Theory]
        [PerCompilationType(nameof(ValueData))]
        public void CanWriteAndReadBack(object value, bool useInterpreter)
        {
            Type type = value.GetType();
            ParameterExpression param = Expression.Parameter(type);
            Assert.True(
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(value),
                        Expression.Block(
                            type,
                            new[] { param },
                            Expression.Assign(param, Expression.Constant(value)),
                            param
                            )
                        )
                    ).Compile(useInterpreter)()
                );
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanUseAsLambdaParameter(bool useInterpreter)
        {
            ParameterExpression param = Expression.Parameter(typeof(int));
            Func<int, int> addOne = Expression.Lambda<Func<int, int>>(
                Expression.Add(param, Expression.Constant(1)),
                param
                ).Compile(useInterpreter);
            Assert.Equal(3, addOne(2));
        }

        public delegate void ByRefFunc<T>(ref T arg);

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanUseAsLambdaByRefParameter(bool useInterpreter)
        {
            ParameterExpression param = Expression.Parameter(typeof(int).MakeByRefType());
            ByRefFunc<int> addOneInPlace = Expression.Lambda<ByRefFunc<int>>(
                Expression.PreIncrementAssign(param),
                param
                ).Compile(useInterpreter);
            int argument = 5;
            addOneInPlace(ref argument);
            Assert.Equal(6, argument);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanUseAsLambdaByRefParameter_String(bool useInterpreter)
        {
            ParameterExpression param = Expression.Parameter(typeof(string).MakeByRefType());
            ByRefFunc<string> f = Expression.Lambda<ByRefFunc<string>>(
                Expression.Assign(param, Expression.Call(param, typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes))),
                param
                ).Compile(useInterpreter);
            string argument = "bar";
            f(ref argument);
            Assert.Equal("BAR", argument);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanUseAsLambdaByRefParameter_Char(bool useInterpreter)
        {
            ParameterExpression param = Expression.Parameter(typeof(char).MakeByRefType());
            ByRefFunc<char> f = Expression.Lambda<ByRefFunc<char>>(
                Expression.Assign(param, Expression.Call(typeof(char).GetMethod(nameof(char.ToUpper), new[] { typeof(char) }), param)),
                param
                ).Compile(useInterpreter);
            char argument = 'a';
            f(ref argument);
            Assert.Equal('A', argument);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanUseAsLambdaByRefParameter_Bool(bool useInterpreter)
        {
            ParameterExpression param = Expression.Parameter(typeof(bool).MakeByRefType());
            ByRefFunc<bool> f = Expression.Lambda<ByRefFunc<bool>>(
                Expression.ExclusiveOrAssign(param, Expression.Constant(true)),
                param
                ).Compile(useInterpreter);

            bool b1 = false;
            f(ref b1);
            Assert.Equal(false ^ true, b1);

            bool b2 = true;
            f(ref b2);
            Assert.Equal(true ^ true, b2);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanReadFromRefParameter(bool useInterpreter)
        {
            AssertCanReadFromRefParameter<byte>(byte.MaxValue, useInterpreter);
            AssertCanReadFromRefParameter<sbyte>(sbyte.MaxValue, useInterpreter);
            AssertCanReadFromRefParameter<short>(short.MaxValue, useInterpreter);
            AssertCanReadFromRefParameter<ushort>(ushort.MaxValue, useInterpreter);
            AssertCanReadFromRefParameter<int>(int.MaxValue, useInterpreter);
            AssertCanReadFromRefParameter<uint>(uint.MaxValue, useInterpreter);
            AssertCanReadFromRefParameter<long>(long.MaxValue, useInterpreter);
            AssertCanReadFromRefParameter<ulong>(ulong.MaxValue, useInterpreter);

            AssertCanReadFromRefParameter<decimal>(49.94m, useInterpreter);
            AssertCanReadFromRefParameter<float>(3.1415926535897931f, useInterpreter);
            AssertCanReadFromRefParameter<double>(2.7182818284590451, useInterpreter);

            AssertCanReadFromRefParameter('a', useInterpreter);

            AssertCanReadFromRefParameter(ByteEnum.A, useInterpreter);
            AssertCanReadFromRefParameter(SByteEnum.A, useInterpreter);
            AssertCanReadFromRefParameter(Int16Enum.A, useInterpreter);
            AssertCanReadFromRefParameter(UInt16Enum.A, useInterpreter);
            AssertCanReadFromRefParameter(Int32Enum.A, useInterpreter);
            AssertCanReadFromRefParameter(UInt32Enum.A, useInterpreter);
            AssertCanReadFromRefParameter(Int64Enum.A, useInterpreter);
            AssertCanReadFromRefParameter(UInt64Enum.A, useInterpreter);

            AssertCanReadFromRefParameter(new DateTime(1983, 2, 11), useInterpreter);

            AssertCanReadFromRefParameter<object>(null, useInterpreter);
            AssertCanReadFromRefParameter<object>(new object(), useInterpreter);
            AssertCanReadFromRefParameter<string>("bar", useInterpreter);

            AssertCanReadFromRefParameter<int?>(null, useInterpreter);
            AssertCanReadFromRefParameter<int?>(int.MaxValue, useInterpreter);
            AssertCanReadFromRefParameter<Int64Enum?>(null, useInterpreter);
            AssertCanReadFromRefParameter<Int64Enum?>(Int64Enum.A, useInterpreter);
            AssertCanReadFromRefParameter<DateTime?>(null, useInterpreter);
            AssertCanReadFromRefParameter<DateTime?>(new DateTime(1983, 2, 11), useInterpreter);
        }

        public delegate T ByRefReadFunc<T>(ref T arg);

        private void AssertCanReadFromRefParameter<T>(T value, bool useInterpreter)
        {
            ParameterExpression @ref = Expression.Parameter(typeof(T).MakeByRefType());

            ByRefReadFunc<T> f =
                Expression.Lambda<ByRefReadFunc<T>>(
                    @ref,
                    @ref
                ).Compile(useInterpreter);

            Assert.Equal(value, f(ref value));
        }

        public delegate void ByRefWriteAction<T>(ref T arg, T value);

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanWriteToRefParameter(bool useInterpreter)
        {
            AssertCanWriteToRefParameter<byte>(byte.MaxValue, useInterpreter);
            AssertCanWriteToRefParameter<sbyte>(sbyte.MaxValue, useInterpreter);
            AssertCanWriteToRefParameter<short>(short.MaxValue, useInterpreter);
            AssertCanWriteToRefParameter<ushort>(ushort.MaxValue, useInterpreter);
            AssertCanWriteToRefParameter<int>(int.MaxValue, useInterpreter);
            AssertCanWriteToRefParameter<uint>(uint.MaxValue, useInterpreter);
            AssertCanWriteToRefParameter<long>(long.MaxValue, useInterpreter);
            AssertCanWriteToRefParameter<ulong>(ulong.MaxValue, useInterpreter);

            AssertCanWriteToRefParameter<decimal>(49.94m, useInterpreter);
            AssertCanWriteToRefParameter<float>(3.1415926535897931f, useInterpreter);
            AssertCanWriteToRefParameter<double>(2.7182818284590451, useInterpreter);

            AssertCanWriteToRefParameter('a', useInterpreter);

            AssertCanWriteToRefParameter(ByteEnum.A, useInterpreter);
            AssertCanWriteToRefParameter(SByteEnum.A, useInterpreter);
            AssertCanWriteToRefParameter(Int16Enum.A, useInterpreter);
            AssertCanWriteToRefParameter(UInt16Enum.A, useInterpreter);
            AssertCanWriteToRefParameter(Int32Enum.A, useInterpreter);
            AssertCanWriteToRefParameter(UInt32Enum.A, useInterpreter);
            AssertCanWriteToRefParameter(Int64Enum.A, useInterpreter);
            AssertCanWriteToRefParameter(UInt64Enum.A, useInterpreter);

            AssertCanWriteToRefParameter(new DateTime(1983, 2, 11), useInterpreter);

            AssertCanWriteToRefParameter<object>(null, useInterpreter);
            AssertCanWriteToRefParameter<object>(new object(), useInterpreter);
            AssertCanWriteToRefParameter<string>("bar", useInterpreter);

            AssertCanWriteToRefParameter<int?>(null, useInterpreter, original: 42);
            AssertCanWriteToRefParameter<int?>(int.MaxValue, useInterpreter);
            AssertCanWriteToRefParameter<Int64Enum?>(null, useInterpreter, original: Int64Enum.A);
            AssertCanWriteToRefParameter<Int64Enum?>(Int64Enum.A, useInterpreter);
            AssertCanWriteToRefParameter<DateTime?>(null, useInterpreter, original: new DateTime(1983, 2, 11));
            AssertCanWriteToRefParameter<DateTime?>(new DateTime(1983, 2, 11), useInterpreter);
        }

        private void AssertCanWriteToRefParameter<T>(T value, bool useInterpreter, T original = default(T))
        {
            ParameterExpression @ref = Expression.Parameter(typeof(T).MakeByRefType());
            ParameterExpression val = Expression.Parameter(typeof(T));

            ByRefWriteAction<T> f =
                Expression.Lambda<ByRefWriteAction<T>>(
                    Expression.Assign(@ref, val),
                    @ref, val
                ).Compile(useInterpreter);

            T res = original;
            f(ref res, value);

            Assert.Equal(res, value);
        }

        [Fact]
        public void CannotReduce()
        {
            ParameterExpression param = Expression.Parameter(typeof(int));
            Assert.False(param.CanReduce);
            Assert.Same(param, param.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => param.ReduceAndCheck());
        }

        [Fact]
        public void CannotBePointerType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Parameter(typeof(int*)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Parameter(typeof(int*), "pointer"));
        }

        [Theory]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "This test causes a fail fast on uapaot: https://github.com/dotnet/corefx/issues/19129")]
        [MemberData(nameof(ReadAndWriteRefCases))]
        public void ReadAndWriteRefParameters(bool useInterpreter, object value, object increment, object result)
        {
            Type type = value.GetType();

            MethodInfo method = typeof(ParameterTests).GetMethod(nameof(AssertReadAndWriteRefParameters), BindingFlags.NonPublic | BindingFlags.Static);

            method.MakeGenericMethod(type).Invoke(null, new object[] { useInterpreter, value, increment, result });
        }

        private static void AssertReadAndWriteRefParameters<T>(bool useInterpreter, T value, T increment, T result)
        {
            ParameterExpression param = Expression.Parameter(typeof(T).MakeByRefType());
            ByRefFunc<T> addOneInPlace = Expression.Lambda<ByRefFunc<T>>(
                Expression.AddAssign(param, Expression.Constant(increment, typeof(T))),
                param
                ).Compile(useInterpreter);
            T argument = value;
            addOneInPlace(ref argument);
            Assert.Equal(result, argument);
        }

        public static IEnumerable<object[]> ReadAndWriteRefCases()
        {
            foreach (var useInterpreter in new[] { true, false })
            {
                yield return new object[] { useInterpreter, (short)41, (short)1, (short)42 };
                yield return new object[] { useInterpreter, (ushort)41, (ushort)1, (ushort)42 };
                yield return new object[] { useInterpreter, 41, 1, 42 };
                yield return new object[] { useInterpreter, 41U, 1U, 42U };
                yield return new object[] { useInterpreter, 41L, 1L, 42L };
                yield return new object[] { useInterpreter, 41UL, 1UL, 42UL };
                yield return new object[] { useInterpreter, 41.0F, 1.0F, Apply((x, y) => x + y, 41.0F, 1.0F) };
                yield return new object[] { useInterpreter, 41.0D, 1.0D, Apply((x, y) => x + y, 41.0D, 1.0D) };
                yield return new object[] { useInterpreter, TimeSpan.FromSeconds(41), TimeSpan.FromSeconds(1), Apply((x, y) => x + y, TimeSpan.FromSeconds(41), TimeSpan.FromSeconds(1)) };
            }
        }

        private static T Apply<T>(Func<T, T, T> f, T x, T y) => f(x, y);
    }
}
