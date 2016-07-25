// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class Continue : GotoExpressionTests
    {
        [Theory]
        [MemberData(nameof(TypesData))]
        public void NonVoidTargetContinueHasNoValue(Type type)
        {
            LabelTarget target = Expression.Label(type);
            Assert.Throws<ArgumentException>("target", () => Expression.Continue(target));
        }

        [Theory]
        [MemberData(nameof(TypesData))]
        public void NonVoidTargetContinueHasNoValueTypeExplicit(Type type)
        {
            LabelTarget target = Expression.Label(type);
            Assert.Throws<ArgumentException>("target", () => Expression.Continue(target, type));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void ContinueVoidNoValue(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Expression block = Expression.Block(
                Expression.Continue(target),
                Expression.Throw(Expression.Constant(new InvalidOperationException())),
                Expression.Label(target)
                );
            Expression.Lambda<Action>(block).Compile(useInterpreter)();
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void ContinueExplicitVoidNoValue(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Expression block = Expression.Block(
                Expression.Continue(target, typeof(void)),
                Expression.Throw(Expression.Constant(new InvalidOperationException())),
                Expression.Label(target)
                );
            Expression.Lambda<Action>(block).Compile(useInterpreter)();
        }

        [Theory]
        [MemberData(nameof(TypesData))]
        public void NullValueOnNonVoidContinue(Type type)
        {
            Assert.Throws<ArgumentException>("target", () => Expression.Continue(Expression.Label(type)));
        }

        [Theory]
        [MemberData(nameof(ConstantValueData))]
        public void ExplicitNullTypeWithValue(object value)
        {
            Assert.Throws<ArgumentException>("target", () => Expression.Continue(Expression.Label(value.GetType()), default(Type)));
        }

        [Fact]
        public void OpenGenericType()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.Continue(Expression.Label(typeof(void)), typeof(List<>)));
        }

        [Fact]
        public static void TypeContainsGenericParameters()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.Continue(Expression.Label(typeof(void)), typeof(List<>.Enumerator)));
            Assert.Throws<ArgumentException>("type", () => Expression.Continue(Expression.Label(typeof(void)), typeof(List<>).MakeGenericType(typeof(List<>))));
        }

        [Fact]
        public void PointerType()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.Continue(Expression.Label(typeof(void)), typeof(int).MakePointerType()));
        }

        [Fact]
        public void ByRefType()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.Continue(Expression.Label(typeof(void)), typeof(int).MakeByRefType()));
        }
    }
}
