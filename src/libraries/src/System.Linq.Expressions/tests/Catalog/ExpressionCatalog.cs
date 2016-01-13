// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace System.Linq.Expressions.Tests
{
    public static partial class ExpressionCatalog
    {
        private static IDictionary<ExpressionType, IEnumerable<Expression>> s_Catalog;
        private static int s_countEx = 0;

        internal static readonly IDictionary<Type, object[]> s_values = new Dictionary<Type, object[]>
        {
            { typeof(bool), new object[] { false, true } },
            { typeof(byte), new object[] { (byte)0, (byte)1, (byte)2, (byte)(byte.MaxValue - 1), byte.MaxValue } },
            { typeof(sbyte), new object[] { sbyte.MinValue, (sbyte)(sbyte.MinValue + 1), (sbyte)-2, (sbyte)-1, (sbyte)0, (sbyte)1, (sbyte)2, (sbyte)(sbyte.MaxValue - 1), sbyte.MaxValue } },
            { typeof(ushort), new object[] { (ushort)0, (ushort)1, (ushort)2, (ushort)(ushort.MaxValue - 1), ushort.MaxValue } },
            { typeof(short), new object[] { short.MinValue, (short)(short.MinValue + 1), (short)-2, (short)-1, (short)0, (short)1, (short)2, (short)(short.MaxValue - 1), short.MaxValue } },
            { typeof(uint), new object[] { (uint)0, (uint)1, (uint)2, (uint)(uint.MaxValue - 1), uint.MaxValue } },
            { typeof(int), new object[] { int.MinValue, (int)(int.MinValue + 1), (int)-2, (int)-1, (int)0, (int)1, (int)2, (int)(int.MaxValue - 1), int.MaxValue } },
            { typeof(ulong), new object[] { (ulong)0, (ulong)1, (ulong)2, (ulong)(ulong.MaxValue - 1), ulong.MaxValue } },
            { typeof(long), new object[] { long.MinValue, (long)(long.MinValue + 1), (long)-2, (long)-1, (long)0, (long)1, (long)2, (long)(long.MaxValue - 1), long.MaxValue } },
            { typeof(double), new object[] { (double)0.0, (double)1.0, (double)-1.0, Math.PI, Math.E, double.NegativeInfinity, double.PositiveInfinity, double.NaN, double.MinValue, double.MaxValue } },
            { typeof(float), new object[] { (float)0.0, (float)1.0, (float)-1.0, (float)Math.PI, (float)Math.E, float.NegativeInfinity, float.PositiveInfinity, float.NaN, float.MinValue, float.MaxValue } },
            { typeof(string), new object[] { "", "bar", "foo" } },
            { typeof(char), new object[] { char.MinValue, char.MaxValue, 'a', '0' } },
            { typeof(S1), new[] { int.MinValue, (int)(int.MinValue + 1), (int)-2, (int)-1, (int)0, (int)1, (int)2, (int)(int.MaxValue - 1), int.MaxValue }.Select(x => (object)new S1(x)).ToArray() },
            { typeof(S2), new[] { false, true }.Select(b => (object)new S2(b)).ToArray() },
            { typeof(E), new object[] { E.Red, E.Green, E.Blue } },
        };

        internal static readonly IDictionary<Type, Expression[]> s_consts = s_values.ToDictionary(kv => kv.Key, kv => kv.Value.Select(v => (Expression)Expression.Constant(v, kv.Key)).ToArray());
        internal static readonly IDictionary<Type, Expression[]> s_exprs = s_consts.ToDictionary(kv => kv.Key, kv => kv.Value.Concat(new Expression[] { Expression.Throw(Expression.Constant(new Exception("Error " + s_countEx++)), kv.Key) }).ToArray());
        internal static readonly IDictionary<Type, Expression[]> s_nullableConsts = s_values.ToDictionary(kv => kv.Key, kv => GetNullableConstantExpressions(kv.Key, kv.Value));
        internal static readonly IDictionary<Type, Expression[]> s_nullableExprs = s_nullableConsts.ToDictionary(kv => kv.Key, kv => kv.Value.Concat(new Expression[] { Expression.Throw(Expression.Constant(new Exception("Error " + s_countEx++)), GetNullableType(kv.Key)) }).ToArray());

        private static Expression[] GetNullableConstantExpressions(Type type, object[] nonNulls)
        {
            var n = GetNullableType(type);
            return nonNulls.Concat(new object[] { null }).Select(v => (Expression)Expression.Constant(v, n)).ToArray();
        }

        private static Type GetNullableType(Type type)
        {
            return type.GetTypeInfo().IsValueType ? typeof(Nullable<>).MakeGenericType(type) : type;
        }

        public static IEnumerable<Expression> Expressions
        {
            get
            {
                return Catalog.SelectMany(g => g.Value);
            }
        }

        public static IDictionary<ExpressionType, IEnumerable<Expression>> Catalog
        {
            get
            {
                if (s_Catalog == null)
                {
                    var all = Enumerable.Empty<KeyValuePair<ExpressionType, Expression>>();

                    var simpleMethods = typeof(ExpressionCatalog).GetTypeInfo().DeclaredMethods.Where(m => m.IsStatic && m.ReturnType == typeof(IEnumerable<Expression>) && m.GetParameters().Length == 0 && !m.Name.StartsWith("get_"));
                    var richMethods = typeof(ExpressionCatalog).GetTypeInfo().DeclaredMethods.Where(m => m.IsStatic && m.ReturnType == typeof(IEnumerable<KeyValuePair<ExpressionType, Expression>>) && m.GetParameters().Length == 0 && !m.Name.StartsWith("get_"));

                    foreach (var method in simpleMethods)
                    {
                        var values = (IEnumerable<Expression>)method.Invoke(null, new object[0]);
                        all = all.Concat(values.Select(v => new KeyValuePair<ExpressionType, Expression>(v.NodeType, v)));
                    }

                    foreach (var method in richMethods)
                    {
                        var values = (IEnumerable<KeyValuePair<ExpressionType, Expression>>)method.Invoke(null, new object[0]);
                        all = all.Concat(values);
                    }

                    s_Catalog = all.GroupBy(kv => kv.Key).ToDictionary(g => g.Key, g => g.Select(x => x.Value).ToList().AsEnumerable());
                }

                return s_Catalog;
            }
        }
    }
}