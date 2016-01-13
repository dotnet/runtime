// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<Expression> Extension()
        {
            yield return new Ext();
        }

        class Ext : Expression
        {
            public override Type Type
            {
                get
                {
                    return typeof(int);
                }
            }

            public override ExpressionType NodeType
            {
                get
                {
                    return ExpressionType.Extension;
                }
            }

            public override bool CanReduce
            {
                get
                {
                    return true;
                }
            }

            public override Expression Reduce()
            {
                return Expression.Constant(42);
            }
        }
    }
}