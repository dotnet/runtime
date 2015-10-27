// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if FEATURE_INTERPRET
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Tests.Expressions
{
    partial class InterpreterTests
    {
        [Fact]
        public static void CompileInterpretCrossCheck_Call_WriteBacks()
        {
            foreach (var e in Call_WriteBacks())
            {
                Verify(e);
            }
        }

        private static IEnumerable<Expression> Call_WriteBacks()
        {
            foreach (var t in new[] { typeof(C), typeof(S) })
            {
                var p = Expression.Parameter(t);
                var m = Expression.PropertyOrField(p, "X");
                var mtd = t.GetTypeInfo().GetDeclaredMethod("Do");

                yield return Expression.Block(new[] { p }, Expression.Assign(p, Expression.New(t)), Expression.Call(p, mtd), m);
            }

            foreach (var t in new[] { typeof(C), typeof(S) })
            {
                var p = Expression.Parameter(t.MakeArrayType());
                var o = Expression.ArrayIndex(p, Expression.Constant(0));
                var m = Expression.PropertyOrField(o, "X");
                var mtd = t.GetTypeInfo().GetDeclaredMethod("Do");

                yield return Expression.Block(new[] { p }, Expression.Assign(p, Expression.NewArrayInit(t, Expression.New(t))), Expression.Call(o, mtd), m);
            }

            foreach (var t in new[] { typeof(C), typeof(S) })
            {
                var p = Expression.Parameter(t.MakeArrayType(2));
                var o = Expression.ArrayAccess(p, Expression.Constant(0), Expression.Constant(0));
                var m = Expression.PropertyOrField(o, "X");
                var mtd = t.GetTypeInfo().GetDeclaredMethod("Do");

                yield return Expression.Block(new[] { p }, Expression.Assign(p, Expression.NewArrayBounds(t, Expression.Constant(1), Expression.Constant(1))), Expression.Assign(o, Expression.New(t)), Expression.Call(o, mtd), m);
            }

            foreach (var t in new[] { typeof(C), typeof(S) })
            {
                var p = Expression.Parameter(typeof(Holder<>).MakeGenericType(t));
                var o = Expression.Property(p, "Value");
                var m = Expression.PropertyOrField(o, "X");
                var mtd = t.GetTypeInfo().GetDeclaredMethod("Do");

                yield return Expression.Block(new[] { p }, Expression.Assign(p, Expression.New(p.Type.GetTypeInfo().DeclaredConstructors.Single(), Expression.New(t))), Expression.Call(o, mtd), m);
            }

            foreach (var t in new[] { typeof(C), typeof(S) })
            {
                var p = Expression.Parameter(typeof(Holder<>).MakeGenericType(t));
                var o = Expression.Field(p, "_value");
                var m = Expression.PropertyOrField(o, "X");
                var mtd = t.GetTypeInfo().GetDeclaredMethod("Do");

                yield return Expression.Block(new[] { p }, Expression.Assign(p, Expression.New(p.Type.GetTypeInfo().DeclaredConstructors.Single(), Expression.New(t))), Expression.Call(o, mtd), m);
            }

            foreach (var t in new[] { typeof(C), typeof(S) })
            {
                var p = Expression.Parameter(typeof(List<>).MakeGenericType(t));
                var o = Expression.MakeIndex(p, p.Type.GetRuntimeProperty("Item"), new[] { Expression.Constant(0) });
                var m = Expression.PropertyOrField(o, "X");
                var mtd = t.GetTypeInfo().GetDeclaredMethod("Do");
                var add = p.Type.GetTypeInfo().GetDeclaredMethod("Add");

                yield return Expression.Block(new[] { p }, Expression.Assign(p, Expression.ListInit(Expression.New(p.Type), Expression.ElementInit(add, Expression.New(t)))), Expression.Call(o, mtd), m);
            }
        }

        class C
        {
            public int X;

            public void Do()
            {
                X = 1;
            }
        }

        struct S
        {
            public int X;

            public void Do()
            {
                X = 1;
            }
        }

        class Holder<T>
        {
            public T _value;

            public Holder(T value)
            {
                _value = value;
            }

            public T Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                }
            }
        }
    }
}
#endif