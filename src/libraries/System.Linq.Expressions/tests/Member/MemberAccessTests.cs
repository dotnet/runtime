// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class MemberAccessTests
    {
        private class UnreadableIndexableClass
        {
            public int this[int index]
            {
                set { }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessStructInstanceFieldTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        Expression.Constant(new FS() { II = 42 }),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessStructStaticFieldTest(bool useInterpreter)
        {
            FS.SI = 42;
            try
            {
                Expression<Func<int>> e =
                    Expression.Lambda<Func<int>>(
                        Expression.Field(
                            null,
                            typeof(FS),
                            "SI"),
                        Enumerable.Empty<ParameterExpression>());
                Func<int> f = e.Compile(useInterpreter);

                Assert.Equal(42, f());
            }
            finally
            {
                FS.SI = 0;
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessStructConstFieldTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        null,
                        typeof(FS),
                        "CI"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessStructStaticReadOnlyFieldTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        null,
                        typeof(FS),
                        "RI"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessStructInstancePropertyTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Property(
                        Expression.Constant(new PS() { II = 42 }),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessStructStaticPropertyTest(bool useInterpreter)
        {
            PS.SI = 42;
            try
            {
                Expression<Func<int>> e =
                    Expression.Lambda<Func<int>>(
                        Expression.Property(
                            null,
                            typeof(PS),
                            "SI"),
                        Enumerable.Empty<ParameterExpression>());
                Func<int> f = e.Compile(useInterpreter);

                Assert.Equal(42, f());
            }
            finally
            {
                PS.SI = 0;
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassInstanceFieldTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        Expression.Constant(new FC() { II = 42 }),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassStaticFieldTest(bool useInterpreter)
        {
            FC.SI = 42;
            try
            {
                Expression<Func<int>> e =
                    Expression.Lambda<Func<int>>(
                        Expression.Field(
                            null,
                            typeof(FC),
                            "SI"),
                        Enumerable.Empty<ParameterExpression>());
                Func<int> f = e.Compile(useInterpreter);

                Assert.Equal(42, f());
            }
            finally
            {
                FC.SI = 0;
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassConstFieldTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        null,
                        typeof(FC),
                        "CI"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassStaticReadOnlyFieldTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        null,
                        typeof(FC),
                        "RI"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassInstancePropertyTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Property(
                        Expression.Constant(new PC() { II = 42 }),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(42, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassStaticPropertyTest(bool useInterpreter)
        {
            PC.SI = 42;
            try
            {
                Expression<Func<int>> e =
                    Expression.Lambda<Func<int>>(
                        Expression.Property(
                            null,
                            typeof(PC),
                            "SI"),
                        Enumerable.Empty<ParameterExpression>());
                Func<int> f = e.Compile(useInterpreter);

                Assert.Equal(42, f());
            }
            finally
            {
                PC.SI = 0;
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassInstanceFieldNullReferenceTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        Expression.Constant(null, typeof(FC)),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Throws<NullReferenceException>(() => f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassInstanceFieldAssignNullReferenceTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Assign(
                        Expression.Field(
                            Expression.Constant(null, typeof(FC)),
                            "II"),
                        Expression.Constant(1)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Throws<NullReferenceException>(() => f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassInstancePropertyNullReferenceTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Property(
                        Expression.Constant(null, typeof(PC)),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Throws<NullReferenceException>(() => f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassInstanceIndexerNullReferenceTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Property(
                        Expression.Constant(null, typeof(PC)),
                        "Item",
                        Expression.Constant(1)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Throws<NullReferenceException>(() => f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckMemberAccessClassInstanceIndexerAssignNullReferenceTest(bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Assign(
                        Expression.Property(
                            Expression.Constant(null, typeof(PC)),
                            "Item",
                            Expression.Constant(1)),
                        Expression.Constant(1)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Throws<NullReferenceException>(() => f());
        }

        [Fact]
        public static void AccessIndexedPropertyWithoutIndex()
        {
            Assert.Throws<ArgumentException>(null, () => Expression.Property(Expression.Default(typeof(List<int>)), typeof(List<int>).GetProperty("Item")));
        }

        [Fact]
        public static void AccessIndexedPropertyWithoutIndexWriteOnly()
        {
            Assert.Throws<ArgumentException>(null, () => Expression.Property(Expression.Default(typeof(UnreadableIndexableClass)), typeof(UnreadableIndexableClass).GetProperty("Item")));
        }
    }
}
