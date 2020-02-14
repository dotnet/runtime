// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM
using System.Linq.Expressions;

using System;

namespace Microsoft.Scripting.ComInterop {

    // Miscellaneous helpers that don't belong anywhere else
    internal static class Helpers {

        internal static Expression Convert(Expression expression, Type type) {
            if (expression.Type == type) {
                return expression;
            }
            return Expression.Convert(expression, type);
        }
    }
}
#endif
