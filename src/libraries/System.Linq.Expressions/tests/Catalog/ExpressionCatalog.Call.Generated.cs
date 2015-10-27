// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Tests.Expressions
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> Call()
        {
            var concat = ((MethodCallExpression)((Expression<Func<object, object, string>>)((o1, o2) => string.Concat(o1, o2))).Body).Method;

            var res0 = Expression.Parameter(typeof(int));
            var res1 = Expression.Parameter(typeof(string));
            var res2 = Expression.Parameter(typeof(bool));
            var res3 = Expression.Parameter(typeof(E));
            var res4 = Expression.Parameter(typeof(int?));

            var objc = Expression.Parameter(typeof(CallC));
            var objs = Expression.Parameter(typeof(CallS));

            var newObjc = ((NewExpression)((Expression<Func<CallC>>)(() => new CallC(default(Action<string>)))).Body).Constructor;
            var newObjs = ((NewExpression)((Expression<Func<CallS>>)(() => new CallS(default(Action<string>)))).Body).Constructor;

            // Static
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res4 }, Expression.Assign(res4, Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S0"))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S1"), ReturnWithLog(add, (E)E.Red)), summary));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S2"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res3 }, Expression.Assign(res3, Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S3"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res0 }, Expression.Assign(res0, Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S4"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42))), Expression.Call(concat, Expression.Convert(res0, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res1 }, Expression.Assign(res1, Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S5"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42))), Expression.Call(concat, Expression.Convert(res1, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S6"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res4 }, Expression.Assign(res4, Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S7"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S8"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res1 }, Expression.Assign(res1, Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S9"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res1, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S10"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S11"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res0 }, Expression.Assign(res0, Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S12"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red))), Expression.Call(concat, Expression.Convert(res0, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res3 }, Expression.Assign(res3, Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S13"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S14"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res0 }, Expression.Assign(res0, Expression.Call(typeof(CallC).GetTypeInfo().GetDeclaredMethod("S15"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red))), Expression.Call(concat, Expression.Convert(res0, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S0")), summary));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S1"), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res4 }, Expression.Assign(res4, Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S2"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res0 }, Expression.Assign(res0, Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S3"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42))), Expression.Call(concat, Expression.Convert(res0, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res1 }, Expression.Assign(res1, Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S4"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res1, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res0 }, Expression.Assign(res0, Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S5"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res0, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res4 }, Expression.Assign(res4, Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S6"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res3 }, Expression.Assign(res3, Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S7"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S8"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S9"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar")), summary));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res3 }, Expression.Assign(res3, Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S10"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res1 }, Expression.Assign(res1, Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S11"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red))), Expression.Call(concat, Expression.Convert(res1, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res0 }, Expression.Assign(res0, Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S12"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red))), Expression.Call(concat, Expression.Convert(res0, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S13"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), summary)));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S14"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red)), summary));
            yield return WithLog(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res3 }, Expression.Assign(res3, Expression.Call(typeof(CallS).GetTypeInfo().GetDeclaredMethod("S15"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), summary)));

            // Instance
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res0 }, Expression.Assign(res0, Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I0"))), Expression.Call(concat, Expression.Convert(res0, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res4 }, Expression.Assign(res4, Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I1"), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I2"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res4 }, Expression.Assign(res4, Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I3"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I4"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res0 }, Expression.Assign(res0, Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I5"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42))), Expression.Call(concat, Expression.Convert(res0, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res4 }, Expression.Assign(res4, Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I6"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I7"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res3 }, Expression.Assign(res3, Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I8"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I9"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I10"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I11"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res4 }, Expression.Assign(res4, Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I12"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I13"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I14"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res3 }, Expression.Assign(res3, Expression.Call(Expression.New(newObjc, add), typeof(CallC).GetTypeInfo().GetDeclaredMethod("I15"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res3 }, Expression.Assign(res3, Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I0"))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I1"), ReturnWithLog(add, (int)42)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res3 }, Expression.Assign(res3, Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I2"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res3 }, Expression.Assign(res3, Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I3"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res1 }, Expression.Assign(res1, Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I4"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res1, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I5"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res3 }, Expression.Assign(res3, Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I6"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I7"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res3 }, Expression.Assign(res3, Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I8"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res3 }, Expression.Assign(res3, Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I9"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res4 }, Expression.Assign(res4, Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I10"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I11"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I12"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res4 }, Expression.Assign(res4, Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I13"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res3 }, Expression.Assign(res3, Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I14"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { res2 }, Expression.Assign(res2, Expression.Call(Expression.New(newObjs, add), typeof(CallS).GetTypeInfo().GetDeclaredMethod("I15"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));

            // Instance variable non-null
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res0 }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Assign(res0, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I0"))), Expression.Call(concat, Expression.Convert(res0, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res4 }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Assign(res4, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I1"), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res2 }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Assign(res2, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I2"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res4 }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Assign(res4, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I3"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res2 }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Assign(res2, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I4"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res0 }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Assign(res0, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I5"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42))), Expression.Call(concat, Expression.Convert(res0, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res4 }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Assign(res4, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I6"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I7"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res3 }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Assign(res3, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I8"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res2 }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Assign(res2, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I9"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I10"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I11"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res4 }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Assign(res4, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I12"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res2 }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Assign(res2, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I13"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I14"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res3 }, Expression.Assign(objc, Expression.New(newObjc, add)), Expression.Assign(res3, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I15"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs, res3 }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Assign(res3, Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I0"))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I1"), ReturnWithLog(add, (int)42)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs, res3 }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Assign(res3, Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I2"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs, res3 }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Assign(res3, Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I3"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs, res1 }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Assign(res1, Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I4"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res1, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I5"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs, res3 }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Assign(res3, Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I6"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I7"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs, res3 }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Assign(res3, Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I8"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs, res3 }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Assign(res3, Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I9"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs, res4 }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Assign(res4, Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I10"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs, res2 }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Assign(res2, Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I11"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs, res2 }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Assign(res2, Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I12"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs, res4 }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Assign(res4, Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I13"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs, res3 }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Assign(res3, Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I14"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objs, res2 }, Expression.Assign(objs, Expression.New(newObjs, add)), Expression.Assign(res2, Expression.Call(objs, typeof(CallS).GetTypeInfo().GetDeclaredMethod("I15"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));

            // Instance variable null
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res0 }, Expression.Assign(res0, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I0"))), Expression.Call(concat, Expression.Convert(res0, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res4 }, Expression.Assign(res4, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I1"), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res2 }, Expression.Assign(res2, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I2"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res4 }, Expression.Assign(res4, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I3"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res2 }, Expression.Assign(res2, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I4"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res0 }, Expression.Assign(res0, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I5"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42))), Expression.Call(concat, Expression.Convert(res0, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res4 }, Expression.Assign(res4, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I6"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc }, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I7"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res3 }, Expression.Assign(res3, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I8"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res2 }, Expression.Assign(res2, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I9"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc }, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I10"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc }, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I11"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res4 }, Expression.Assign(res4, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I12"), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res4, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res2 }, Expression.Assign(res2, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I13"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true))), Expression.Call(concat, Expression.Convert(res2, typeof(object)), Expression.Invoke(summary))));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc }, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I14"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (bool)true)), Expression.Invoke(summary)));
            yield return WithLogExpr(ExpressionType.Call, (add, summary) => Expression.Block(new[] { objc, res3 }, Expression.Assign(res3, Expression.Call(objc, typeof(CallC).GetTypeInfo().GetDeclaredMethod("I15"), ReturnWithLog(add, (int)42), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (int)42), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (bool)true), ReturnWithLog(add, (string)"bar"), ReturnWithLog(add, (int?)42), ReturnWithLog(add, (E)E.Red), ReturnWithLog(add, (string)"bar"))), Expression.Call(concat, Expression.Convert(res3, typeof(object)), Expression.Invoke(summary))));
        }

        class CallC
        {
            private readonly Action<string> _addToLog;

            public CallC(Action<string> addToLog)
            {
                _addToLog = addToLog;
                _addToLog(".ctor");
            }

            public static int? S0()
            {
                return 42;
            }

            public static void S1(E p0)
            {
            }

            public static bool S2(int? p0, bool p1)
            {
                return true;
            }

            public static E S3(bool p0, string p1, string p2)
            {
                return E.Red;
            }

            public static int S4(string p0, E p1, int? p2, int p3)
            {
                return 42;
            }

            public static string S5(bool p0, bool p1, bool p2, string p3, int p4)
            {
                return "bar";
            }

            public static bool S6(string p0, int p1, E p2, int p3, int? p4, int? p5)
            {
                return true;
            }

            public static int? S7(string p0, int p1, string p2, E p3, E p4, E p5, int p6)
            {
                return 42;
            }

            public static bool S8(E p0, E p1, int? p2, int? p3, int? p4, E p5, E p6, E p7)
            {
                return true;
            }

            public static string S9(int p0, int? p1, bool p2, int? p3, string p4, E p5, bool p6, E p7, bool p8)
            {
                return "bar";
            }

            public static bool S10(string p0, bool p1, int p2, E p3, bool p4, E p5, int p6, int p7, int p8, string p9)
            {
                return true;
            }

            public static bool S11(int? p0, int p1, int p2, string p3, int? p4, E p5, bool p6, bool p7, string p8, E p9, E p10)
            {
                return true;
            }

            public static int S12(string p0, int? p1, int? p2, string p3, bool p4, int? p5, string p6, int? p7, int p8, string p9, bool p10, E p11)
            {
                return 42;
            }

            public static E S13(bool p0, int p1, string p2, string p3, int? p4, E p5, bool p6, bool p7, int? p8, int p9, int? p10, bool p11, bool p12)
            {
                return E.Red;
            }

            public static bool S14(int p0, int? p1, string p2, int p3, bool p4, E p5, int p6, E p7, int? p8, bool p9, string p10, bool p11, int p12, int? p13)
            {
                return true;
            }

            public static int S15(bool p0, E p1, E p2, string p3, int p4, string p5, E p6, int p7, int p8, bool p9, int p10, int p11, int p12, string p13, E p14)
            {
                return 42;
            }

            public int I0()
            {
                _addToLog("I0(" + string.Join(", ", new object[] { }) + ")");
                return 42;
            }

            public int? I1(int? p0)
            {
                _addToLog("I1(" + string.Join(", ", new object[] { p0 }) + ")");
                return 42;
            }

            public bool I2(string p0, bool p1)
            {
                _addToLog("I2(" + string.Join(", ", new object[] { p0, p1 }) + ")");
                return true;
            }

            public int? I3(bool p0, int? p1, E p2)
            {
                _addToLog("I3(" + string.Join(", ", new object[] { p0, p1, p2 }) + ")");
                return 42;
            }

            public bool I4(int p0, bool p1, int? p2, bool p3)
            {
                _addToLog("I4(" + string.Join(", ", new object[] { p0, p1, p2, p3 }) + ")");
                return true;
            }

            public int I5(string p0, int p1, int p2, E p3, int p4)
            {
                _addToLog("I5(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4 }) + ")");
                return 42;
            }

            public int? I6(int? p0, int? p1, E p2, int? p3, E p4, bool p5)
            {
                _addToLog("I6(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5 }) + ")");
                return 42;
            }

            public void I7(int? p0, int? p1, int p2, string p3, E p4, int p5, bool p6)
            {
                _addToLog("I7(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6 }) + ")");
            }

            public E I8(bool p0, string p1, bool p2, E p3, int? p4, int? p5, int p6, int? p7)
            {
                _addToLog("I8(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7 }) + ")");
                return E.Red;
            }

            public bool I9(string p0, int? p1, int? p2, int p3, bool p4, int? p5, int p6, string p7, E p8)
            {
                _addToLog("I9(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7, p8 }) + ")");
                return true;
            }

            public void I10(int p0, int p1, int? p2, string p3, int? p4, int? p5, E p6, int p7, E p8, E p9)
            {
                _addToLog("I10(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9 }) + ")");
            }

            public void I11(int? p0, int? p1, int p2, bool p3, int? p4, int? p5, E p6, int p7, int p8, string p9, int? p10)
            {
                _addToLog("I11(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10 }) + ")");
            }

            public int? I12(string p0, E p1, E p2, bool p3, E p4, bool p5, int p6, int? p7, int? p8, int? p9, E p10, string p11)
            {
                _addToLog("I12(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11 }) + ")");
                return 42;
            }

            public bool I13(bool p0, int p1, bool p2, E p3, string p4, int? p5, bool p6, int p7, E p8, string p9, int p10, int? p11, bool p12)
            {
                _addToLog("I13(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12 }) + ")");
                return true;
            }

            public void I14(int p0, bool p1, string p2, E p3, int p4, string p5, int p6, E p7, E p8, string p9, bool p10, int? p11, E p12, bool p13)
            {
                _addToLog("I14(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13 }) + ")");
            }

            public E I15(int p0, int? p1, string p2, E p3, int? p4, bool p5, E p6, int p7, string p8, int? p9, bool p10, string p11, int? p12, E p13, string p14)
            {
                _addToLog("I15(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14 }) + ")");
                return E.Red;
            }

        }

        struct CallS
        {
            private readonly Action<string> _addToLog;

            public CallS(Action<string> addToLog)
            {
                _addToLog = addToLog;
                _addToLog(".ctor");
            }

            public static void S0()
            {
            }

            public static bool S1(int? p0)
            {
                return true;
            }

            public static int? S2(int p0, int? p1)
            {
                return 42;
            }

            public static int S3(int p0, int? p1, int p2)
            {
                return 42;
            }

            public static string S4(int? p0, int p1, int? p2, int? p3)
            {
                return "bar";
            }

            public static int S5(bool p0, E p1, int p2, bool p3, bool p4)
            {
                return 42;
            }

            public static int? S6(int p0, string p1, string p2, bool p3, int? p4, int p5)
            {
                return 42;
            }

            public static E S7(bool p0, bool p1, int p2, string p3, int p4, bool p5, bool p6)
            {
                return E.Red;
            }

            public static bool S8(bool p0, E p1, bool p2, E p3, E p4, int? p5, string p6, int p7)
            {
                return true;
            }

            public static void S9(bool p0, string p1, string p2, string p3, bool p4, bool p5, int p6, int? p7, string p8)
            {
            }

            public static E S10(E p0, bool p1, int p2, E p3, int p4, bool p5, string p6, string p7, bool p8, int p9)
            {
                return E.Red;
            }

            public static string S11(int p0, string p1, bool p2, E p3, int p4, int? p5, bool p6, E p7, int? p8, string p9, E p10)
            {
                return "bar";
            }

            public static int S12(int? p0, int p1, bool p2, int? p3, bool p4, int p5, int? p6, int p7, bool p8, int? p9, bool p10, E p11)
            {
                return 42;
            }

            public static bool S13(string p0, int p1, bool p2, string p3, string p4, bool p5, E p6, int p7, int? p8, string p9, int? p10, E p11, bool p12)
            {
                return true;
            }

            public static void S14(E p0, bool p1, int p2, string p3, E p4, int? p5, string p6, string p7, bool p8, bool p9, E p10, int? p11, int p12, E p13)
            {
            }

            public static E S15(int p0, string p1, int? p2, int? p3, E p4, E p5, E p6, int? p7, bool p8, bool p9, E p10, int p11, string p12, int p13, int? p14)
            {
                return E.Red;
            }

            public E I0()
            {
                _addToLog("I0(" + string.Join(", ", new object[] { }) + ")");
                return E.Red;
            }

            public void I1(int p0)
            {
                _addToLog("I1(" + string.Join(", ", new object[] { p0 }) + ")");
            }

            public E I2(int p0, bool p1)
            {
                _addToLog("I2(" + string.Join(", ", new object[] { p0, p1 }) + ")");
                return E.Red;
            }

            public E I3(string p0, bool p1, int? p2)
            {
                _addToLog("I3(" + string.Join(", ", new object[] { p0, p1, p2 }) + ")");
                return E.Red;
            }

            public string I4(int? p0, string p1, string p2, string p3)
            {
                _addToLog("I4(" + string.Join(", ", new object[] { p0, p1, p2, p3 }) + ")");
                return "bar";
            }

            public void I5(int p0, int? p1, string p2, E p3, bool p4)
            {
                _addToLog("I5(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4 }) + ")");
            }

            public E I6(int p0, E p1, int? p2, string p3, string p4, int? p5)
            {
                _addToLog("I6(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5 }) + ")");
                return E.Red;
            }

            public void I7(E p0, int? p1, string p2, int p3, string p4, E p5, E p6)
            {
                _addToLog("I7(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6 }) + ")");
            }

            public E I8(E p0, bool p1, string p2, bool p3, E p4, E p5, E p6, string p7)
            {
                _addToLog("I8(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7 }) + ")");
                return E.Red;
            }

            public E I9(bool p0, E p1, int? p2, E p3, string p4, bool p5, E p6, int? p7, int? p8)
            {
                _addToLog("I9(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7, p8 }) + ")");
                return E.Red;
            }

            public int? I10(bool p0, string p1, int? p2, int? p3, string p4, E p5, E p6, int? p7, int p8, string p9)
            {
                _addToLog("I10(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9 }) + ")");
                return 42;
            }

            public bool I11(int p0, E p1, bool p2, string p3, bool p4, bool p5, int? p6, string p7, E p8, string p9, string p10)
            {
                _addToLog("I11(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10 }) + ")");
                return true;
            }

            public bool I12(string p0, int? p1, bool p2, int p3, int? p4, bool p5, bool p6, E p7, int p8, bool p9, int p10, string p11)
            {
                _addToLog("I12(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11 }) + ")");
                return true;
            }

            public int? I13(E p0, bool p1, int p2, E p3, int? p4, E p5, E p6, int? p7, int? p8, string p9, int p10, string p11, string p12)
            {
                _addToLog("I13(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12 }) + ")");
                return 42;
            }

            public E I14(int p0, E p1, E p2, int p3, bool p4, string p5, int? p6, bool p7, int p8, E p9, E p10, E p11, string p12, bool p13)
            {
                _addToLog("I14(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13 }) + ")");
                return E.Red;
            }

            public bool I15(E p0, bool p1, E p2, string p3, int? p4, E p5, int? p6, E p7, bool p8, int? p9, bool p10, bool p11, bool p12, bool p13, bool p14)
            {
                _addToLog("I15(" + string.Join(", ", new object[] { p0, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14 }) + ")");
                return true;
            }

        }
    }
}