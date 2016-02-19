// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
            Assert.Throws<ArgumentException>(() => Expression.Continue(target));
        }

        [Theory]
        [MemberData(nameof(TypesData))]
        public void NonVoidTargetContinueHasNoValueTypeExplicit(Type type)
        {
            LabelTarget target = Expression.Label(type);
            Assert.Throws<ArgumentException>(() => Expression.Continue(target, type));
        }

        [Fact]
        public void ContinueVoidNoValue()
        {
            LabelTarget target = Expression.Label();
            Expression block = Expression.Block(
                Expression.Continue(target),
                Expression.Throw(Expression.Constant(new InvalidOperationException())),
                Expression.Label(target)
                );
            Expression.Lambda<Action>(block).Compile()();
        }

        [Fact]
        public void ContinueExplicitVoidNoValue()
        {
            LabelTarget target = Expression.Label();
            Expression block = Expression.Block(
                Expression.Continue(target, typeof(void)),
                Expression.Throw(Expression.Constant(new InvalidOperationException())),
                Expression.Label(target)
                );
            Expression.Lambda<Action>(block).Compile()();
        }

        [Theory]
        [MemberData(nameof(TypesData))]
        public void NullValueOnNonVoidContinue(Type type)
        {
            Assert.Throws<ArgumentException>(() => Expression.Continue(Expression.Label(type)));
        }

        [Theory]
        [MemberData(nameof(ConstantValueData))]
        public void ExplicitNullTypeWithValue(object value)
        {
            Assert.Throws<ArgumentException>(() => Expression.Continue(Expression.Label(value.GetType()), default(Type)));
        }
    }
}
