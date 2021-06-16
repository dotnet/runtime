// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    internal static class Utils
    {
        public static readonly object BoxedFalse = false;
        public static readonly object BoxedTrue = true;

        public static readonly object BoxedIntM1 = -1;
        public static readonly object BoxedInt0 = 0;
        public static readonly object BoxedInt1 = 1;
        public static readonly object BoxedInt2 = 2;
        public static readonly object BoxedInt3 = 3;

        public static readonly object BoxedDefaultSByte = default(sbyte);
        public static readonly object BoxedDefaultChar = default(char);
        public static readonly object BoxedDefaultInt16 = default(short);
        public static readonly object BoxedDefaultInt64 = default(long);
        public static readonly object BoxedDefaultByte = default(byte);
        public static readonly object BoxedDefaultUInt16 = default(ushort);
        public static readonly object BoxedDefaultUInt32 = default(uint);
        public static readonly object BoxedDefaultUInt64 = default(ulong);
        public static readonly object BoxedDefaultSingle = default(float);
        public static readonly object BoxedDefaultDouble = default(double);
        public static readonly object BoxedDefaultDecimal = default(decimal);
        public static readonly object BoxedDefaultDateTime = default(DateTime);

        private static readonly ConstantExpression s_true = Expression.Constant(BoxedTrue);
        private static readonly ConstantExpression s_false = Expression.Constant(BoxedFalse);

        private static readonly ConstantExpression s_m1 = Expression.Constant(BoxedIntM1);
        private static readonly ConstantExpression s_0 = Expression.Constant(BoxedInt0);
        private static readonly ConstantExpression s_1 = Expression.Constant(BoxedInt1);
        private static readonly ConstantExpression s_2 = Expression.Constant(BoxedInt2);
        private static readonly ConstantExpression s_3 = Expression.Constant(BoxedInt3);

        public static readonly DefaultExpression Empty = Expression.Empty();
        public static readonly ConstantExpression Null = Expression.Constant(null);

        public static ConstantExpression Constant(bool value) => value ? s_true : s_false;

        public static ConstantExpression Constant(int value) =>
            value switch
            {
                -1 => s_m1,
                0 => s_0,
                1 => s_1,
                2 => s_2,
                3 => s_3,
                _ => Expression.Constant(value),
            };

        [DynamicDependency("Value", typeof(StrongBox<>))]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The field will be preserved by the DynamicDependency")]
        public static MemberExpression GetStrongBoxValueField(Expression strongbox)
        {
            return Expression.Field(strongbox, "Value");
        }
    }
}
