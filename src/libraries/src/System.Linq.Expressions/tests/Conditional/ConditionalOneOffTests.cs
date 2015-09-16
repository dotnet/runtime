// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using Xunit;

namespace Tests.Expressions.Conditional
{
    public static unsafe class ConditionalOneOffTests
    {
        [Fact] // [Issue(3223, "https://github.com/dotnet/corefx/issues/3223")]
        public static void VisitIfThenDoesNotCloneTree()
        {
            var ifTrue = ((Expression<Action>)(() => Nop())).Body;

            var e = Expression.IfThen(Expression.Constant(true), ifTrue);

            var r = new Visitor().Visit(e);

            Assert.Same(e, r);
        }

        private static void Nop()
        {
        }

        class Visitor : ExpressionVisitor
        {
        }
    }
}
