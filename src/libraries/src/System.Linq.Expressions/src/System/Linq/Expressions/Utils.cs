// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Linq.Expressions
{
    internal static class Utils
    {
        private static readonly DefaultExpression s_voidInstance = Expression.Empty();

        public static DefaultExpression Empty()
        {
            return s_voidInstance;
        }
    }
}
