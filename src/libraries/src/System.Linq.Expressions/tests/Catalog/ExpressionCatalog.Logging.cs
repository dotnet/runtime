// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Tests.Expressions
{
    public static partial class ExpressionCatalog
    {
        private static KeyValuePair<ExpressionType, Expression> WithLogExpr(ExpressionType nodeType, Func<Expression<Action<string>>, Expression<Func<string>>, Expression> f)
        {
            return new KeyValuePair<ExpressionType, Expression>(nodeType, WithLogExpr(f));
        }

        private static KeyValuePair<ExpressionType, Expression> WithLog(ExpressionType nodeType, Func<Func<Expression, Expression>, Expression, Expression> f)
        {
            return new KeyValuePair<ExpressionType, Expression>(nodeType, WithLog(f));
        }

        private static Expression WithLogExpr(Func<Expression<Action<string>>, Expression<Func<string>>, Expression> f)
        {
            var newLogTemplate = (Expression<Func<List<string>>>)(() => new List<string>());
            var addToLogTemplate = (Expression<Action<List<string>, string>>)((log, value) => log.Add(value));
            var summarizeTemplate = (Expression<Func<List<string>, string>>)(log => string.Join(", ", log));

            var logParam = addToLogTemplate.Parameters[0];
            var newLog = Expression.Assign(logParam, newLogTemplate.Body);

            var valParam = addToLogTemplate.Parameters[1];
            var addToLog = Expression.Lambda<Action<string>>(addToLogTemplate.Body, valParam);
            var summarize = Expression.Lambda<Func<string>>(Expression.Invoke(summarizeTemplate, logParam));

            var res = Expression.Block(
                new[] { logParam },
                newLog,
                f(addToLog, summarize)
            );

            return res;
        }

        private static Expression WithLog(Func<Func<Expression, Expression>, Expression, Expression> f)
        {
            var newLogTemplate = (Expression<Func<List<string>>>)(() => new List<string>());
            var addToLogTemplate = (Expression<Action<List<string>, string>>)((log, value) => log.Add(value));
            var summarizeTemplate = (Expression<Func<List<string>, string>>)(log => string.Join(", ", log));

            var logParam = addToLogTemplate.Parameters[0];
            var newLog = Expression.Assign(logParam, newLogTemplate.Body);

            var valParam = addToLogTemplate.Parameters[1];
            var addMethod = ((MethodCallExpression)addToLogTemplate.Body).Method;
            var addToLog = (Func<Expression, Expression>)(e => Expression.Call(logParam, addMethod, e));
            var summarize = Expression.Invoke(summarizeTemplate, logParam);

            var res = Expression.Block(
                new[] { logParam },
                newLog,
                f(addToLog, summarize)
            );

            return res;
        }

        private static Expression ReturnWithLog<T>(Expression<Action<string>> addToLog, T value)
        {
            return ReturnWithLog(addToLog, Expression.Constant(value, typeof(T)));
        }

        private static Expression ReturnWithLog<T>(Func<Expression, Expression> addToLog, T value)
        {
            return ReturnWithLog(addToLog, Expression.Constant(value, typeof(T)));
        }

        private static Expression ReturnWithLog(Expression<Action<string>> addToLog, ConstantExpression valueExpr)
        {
            var toStringTemplate = (Expression<Func<object, string>>)(x => "Return(" + x.ToString() + ")");

            return
                Expression.Block(
                    Expression.Invoke(
                        addToLog,
                        Expression.Invoke(toStringTemplate, Expression.Convert(valueExpr, typeof(object)))
                    ),
                    valueExpr
                );
        }

        private static Expression ReturnWithLog(Func<Expression, Expression> addToLog, ConstantExpression valueExpr)
        {
            var toStringTemplate = (Expression<Func<object, string>>)(x => "Return(" + x.ToString() + ")");

            return
                Expression.Block(
                    addToLog(
                        Expression.Invoke(toStringTemplate, Expression.Convert(valueExpr, typeof(object)))
                    ),
                    valueExpr
                );
        }

        private static Expression ReturnAllConstants(Expression<Action<string>> addToLog, Expression expression)
        {
            return new ReturnVisitorExpr(addToLog, ReturnWithLog).Visit(expression);
        }

        private static Expression ReturnAllConstants(Func<Expression, Expression> addToLog, Expression expression)
        {
            return new ReturnVisitor(addToLog, ReturnWithLog).Visit(expression);
        }

        class ReturnVisitorExpr : ExpressionVisitor
        {
            private readonly Func<ConstantExpression, Expression> _returnWithLog;

            public ReturnVisitorExpr(Expression<Action<string>> addToLog, Func<Expression<Action<string>>, ConstantExpression, Expression> returnWithLog)
            {
                _returnWithLog = add => returnWithLog(addToLog, add);
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                return _returnWithLog(node);
            }
        }

        class ReturnVisitor : ExpressionVisitor
        {
            private readonly Func<ConstantExpression, Expression> _returnWithLog;

            public ReturnVisitor(Func<Expression, Expression> addToLog, Func<Func<Expression, Expression>, ConstantExpression, Expression> returnWithLog)
            {
                _returnWithLog = add => returnWithLog(addToLog, add);
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                return _returnWithLog(node);
            }
        }
    }
}