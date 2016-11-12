// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Linq.Expressions
{
    internal static class Utils
    {
        private static readonly ConstantExpression s_true = Expression.Constant(true);
        private static readonly ConstantExpression s_false = Expression.Constant(false);

        private static readonly ConstantExpression s_m1 = Expression.Constant(-1);
        private static readonly ConstantExpression s_0 = Expression.Constant(0);
        private static readonly ConstantExpression s_1 = Expression.Constant(1);
        private static readonly ConstantExpression s_2 = Expression.Constant(2);
        private static readonly ConstantExpression s_3 = Expression.Constant(3);

        public static readonly DefaultExpression Empty = Expression.Empty();
        public static readonly ConstantExpression Null = Expression.Constant(null);

        public static ConstantExpression Constant(bool value) => value ? s_true : s_false;

        public static ConstantExpression Constant(int value)
        {
            switch (value)
            {
                case -1: return s_m1;
                case 0: return s_0;
                case 1: return s_1;
                case 2: return s_2;
                case 3: return s_3;
                default: return Expression.Constant(value);
            }
        }
    }
}
