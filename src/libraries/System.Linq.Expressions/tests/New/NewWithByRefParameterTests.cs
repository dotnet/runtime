// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class NewWithByRefParameterTests
    {
        private readonly int Always2 = 2;

        private class ByRefNewType
        {
            public ByRefNewType(ref int x, ref int y)
            {
                if (x < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(x));
                }

                ++x;
                y *= x;
            }
        }

        private class OutNewType
        {
            public OutNewType(out int x)
            {
                x = 42;
            }
        }

        private delegate ByRefNewType ByRefNewFactory2(ref int x, ref int y);

        private delegate ByRefNewType ByRefNewFactory1(ref int x);

        private delegate OutNewType OutNewTypeFactory(out int x);

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CreateByRef(bool useInterpreter)
        {
            var pX = Expression.Parameter(typeof(int).MakeByRefType());
            var pY = Expression.Parameter(typeof(int).MakeByRefType());
            var del =
                Expression.Lambda<ByRefNewFactory2>(
                    Expression.New(typeof(ByRefNewType).GetConstructors()[0], pX, pY), pX, pY).Compile(useInterpreter);
            int x = 3;
            int y = 4;
            Assert.NotNull(del(ref x, ref y));
            Assert.Equal(4, x);
            Assert.Equal(16, y);
        }

        [Theory, InlineData(false)]
        public void CreateByRefAliasing(bool useInterpreter)
        {
            var pX = Expression.Parameter(typeof(int).MakeByRefType());
            var pY = Expression.Parameter(typeof(int).MakeByRefType());
            var del =
                Expression.Lambda<ByRefNewFactory2>(
                    Expression.New(typeof(ByRefNewType).GetConstructors()[0], pX, pY), pX, pY).Compile(useInterpreter);
            int x = 3;
            Assert.NotNull(del(ref x, ref x));
            Assert.Equal(16, x);
        }

        [Fact, ActiveIssue(13458)]
        public void CreateByRefAliasingInterpreted()
        {
            CreateByRefAliasing(true);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CreateByRefReferencingReadonly(bool useInterpreter)
        {
            var p = Expression.Parameter(typeof(int).MakeByRefType());
            var del =
                Expression.Lambda<ByRefNewFactory1>(
                    Expression.New(
                        typeof(ByRefNewType).GetConstructors()[0],
                        Expression.Field(Expression.Constant(this), "Always2"), p), p).Compile(useInterpreter);
            int x = 19;
            Assert.NotNull(del(ref x));
            Assert.Equal(2, Always2);
            Assert.Equal(57, x);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CreateByRefReferencingOnlyReadonly(bool useInterpreter)
        {
            var del =
                Expression.Lambda<Func<ByRefNewType>>(
                    Expression.New(
                        typeof(ByRefNewType).GetConstructors()[0],
                        Expression.Field(Expression.Constant(this), "Always2"),
                        Expression.Field(Expression.Constant(this), "Always2"))).Compile(useInterpreter);
            Assert.NotNull(del());
            Assert.Equal(2, Always2);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CreateByRefThrowing(bool useInterpreter)
        {
            var pX = Expression.Parameter(typeof(int).MakeByRefType());
            var pY = Expression.Parameter(typeof(int).MakeByRefType());
            var del =
                Expression.Lambda<ByRefNewFactory2>(
                    Expression.New(typeof(ByRefNewType).GetConstructors()[0], pX, pY), pX, pY).Compile(useInterpreter);
            int x = -9;
            int y = 4;
            Assert.Throws<ArgumentOutOfRangeException>("x", () => del(ref x, ref y));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CreateOut(bool useInterpreter)
        {
            var p = Expression.Parameter(typeof(int).MakeByRefType());
            var del =
                Expression.Lambda<OutNewTypeFactory>(Expression.New(typeof(OutNewType).GetConstructors()[0], p), p)
                    .Compile(useInterpreter);
            int x;
            Assert.NotNull(del(out x));
            Assert.Equal(42, x);
        }
    }
}
