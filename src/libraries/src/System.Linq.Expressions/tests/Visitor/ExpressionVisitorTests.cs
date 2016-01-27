// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static partial class ExpressionVisitorTests
    {
        [Fact]
        public static void ExpressionVisitor_NoCloning()
        {
            var v = new NoOpVisitor();

            foreach (var e in ExpressionCatalog.Expressions.Where(e => e.NodeType != ExpressionType.Extension))
            {
                Assert.Same(e, v.Visit(e));
            }
        }

        class NoOpVisitor : ExpressionVisitor
        {
        }
    }
}
