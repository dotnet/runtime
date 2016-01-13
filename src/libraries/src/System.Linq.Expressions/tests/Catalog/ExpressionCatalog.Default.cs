// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<Expression> Default()
        {
            foreach (var t in new[] { typeof(object), typeof(int), typeof(string), typeof(int?), typeof(TimeSpan), typeof(TimeSpan?) })
            {
                yield return Expression.Default(t);
            }
        }
    }
}