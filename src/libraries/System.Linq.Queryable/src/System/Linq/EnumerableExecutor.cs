// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Linq
{
    // Must remain public for Silverlight
    public abstract class EnumerableExecutor
    {
        internal abstract object ExecuteBoxed();

        internal static EnumerableExecutor Create(Expression expression)
        {
            Type execType = typeof(EnumerableExecutor<>).MakeGenericType(expression.Type);
            return (EnumerableExecutor)Activator.CreateInstance(execType, new object[] { expression });
        }
    }

    // Must remain public for Silverlight
    public class EnumerableExecutor<T> : EnumerableExecutor
    {
        private Expression _expression;
        private Func<T> _func;

        // Must remain public for Silverlight
        public EnumerableExecutor(Expression expression)
        {
            _expression = expression;
        }

        internal override object ExecuteBoxed()
        {
            return this.Execute();
        }

        internal T Execute()
        {
            if (_func == null)
            {
                EnumerableRewriter rewriter = new EnumerableRewriter();
                Expression body = rewriter.Visit(_expression);
                Expression<Func<T>> f = Expression.Lambda<Func<T>>(body, (IEnumerable<ParameterExpression>)null);
                _func = f.Compile();
            }
            return _func();
        }
    }
}
