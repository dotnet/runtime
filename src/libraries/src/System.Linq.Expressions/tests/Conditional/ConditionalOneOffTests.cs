// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class ConditionalOneOffTests
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
