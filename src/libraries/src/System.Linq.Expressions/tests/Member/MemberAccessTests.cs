// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.MemberAccess
{
    public static unsafe class MemberAccessTests
    {
        [Fact]
        public static void CheckMemberAccessStructInstanceFieldTest()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        Expression.Constant(new FS() { II = 42 }),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();

            Assert.Equal(42, f());
        }

        [Fact]
        public static void CheckMemberAccessStructStaticFieldTest()
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
                Func<int> f = e.Compile();

                Assert.Equal(42, f());
            }
            finally
            {
                FS.SI = 0;
            }
        }

        [Fact]
        public static void CheckMemberAccessStructConstFieldTest()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        null,
                        typeof(FS),
                        "CI"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();

            Assert.Equal(42, f());
        }

        [Fact]
        public static void CheckMemberAccessStructStaticReadOnlyFieldTest()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        null,
                        typeof(FS),
                        "RI"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();

            Assert.Equal(42, f());
        }

        [Fact]
        public static void CheckMemberAccessStructInstancePropertyTest()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Property(
                        Expression.Constant(new PS() { II = 42 }),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();

            Assert.Equal(42, f());
        }

        [Fact]
        public static void CheckMemberAccessStructStaticPropertyTest()
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
                Func<int> f = e.Compile();

                Assert.Equal(42, f());
            }
            finally
            {
                PS.SI = 0;
            }
        }

        [Fact]
        public static void CheckMemberAccessClassInstanceFieldTest()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        Expression.Constant(new FC() { II = 42 }),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();

            Assert.Equal(42, f());
        }

        [Fact]
        public static void CheckMemberAccessClassStaticFieldTest()
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
                Func<int> f = e.Compile();

                Assert.Equal(42, f());
            }
            finally
            {
                FC.SI = 0;
            }
        }

        [Fact]
        public static void CheckMemberAccessClassConstFieldTest()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        null,
                        typeof(FC),
                        "CI"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();

            Assert.Equal(42, f());
        }

        [Fact]
        public static void CheckMemberAccessClassStaticReadOnlyFieldTest()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        null,
                        typeof(FC),
                        "RI"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();

            Assert.Equal(42, f());
        }

        [Fact]
        public static void CheckMemberAccessClassInstancePropertyTest()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Property(
                        Expression.Constant(new PC() { II = 42 }),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();

            Assert.Equal(42, f());
        }

        [Fact]
        public static void CheckMemberAccessClassStaticPropertyTest()
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
                Func<int> f = e.Compile();

                Assert.Equal(42, f());
            }
            finally
            {
                PC.SI = 0;
            }
        }

        [Fact] // [Issue(3217, "https://github.com/dotnet/corefx/issues/3217")]
        public static void CheckMemberAccessClassInstanceFieldNullReferenceTest()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Field(
                        Expression.Constant(null, typeof(FC)),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();

            var err = default(Exception);
            try
            {
                f();
            }
            catch (NullReferenceException ex)
            {
                err = ex;
            }

            Assert.NotNull(err);
        }

        [Fact] // [Issue(3217, "https://github.com/dotnet/corefx/issues/3217")]
        public static void CheckMemberAccessClassInstancePropertyNullReferenceTest()
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Property(
                        Expression.Constant(null, typeof(PC)),
                        "II"),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();

            var err = default(Exception);
            try
            {
                f();
            }
            catch (NullReferenceException ex)
            {
                err = ex;
            }

            Assert.NotNull(err);
        }
    }
}
