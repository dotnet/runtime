// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ExpressionIterator is an iterator for EXPRLISTs, designed to be used
    // in the following way:
    //
    //     for (ExpressionIterator it(list); !it.AtEnd(); it.MoveNext())
    //     {
    //         EXPR expr = it.Current();
    //         ...
    //         ... // work with expr
    //     }
    //
    // The constructor takes an EXPR which can point to either an EXPRLIST
    // or a non-list EXPR, or nothing. 
    //
    // Upon construction, the iterator's current element is the first element
    // in the list, which is to say it's not necessary to call MoveNext
    // before usage. AtEnd is true iff the iterator's current element is
    // beyond the last element of the list. These semantics differ from
    // IEnumerator in the framework, but are natural for usage in the C++
    // for-loop as above.
    //
    // Outside of a for loop, usage might look like this:
    //
    //     ExpressionIterator it(list);
    //     EXPR expr1 = it.Current();
    //     it.MoveNext();
    //     EXPR expr2 = it.Current();
    //     it.MoveNext();
    //     EXPR expr3 = it.Current();
    //     it.MoveNext();
    //
    // This would get the first three elements in a list. Also available is the
    // static Count:
    //
    //     int n = ExpressionIterator::Count(list);

    internal sealed class ExpressionIterator
    {
        public ExpressionIterator(Expr pExpr) { Init(pExpr); }

        public bool AtEnd() { return _pCurrent == null && _pList == null; }

        public Expr Current() { return _pCurrent; }

        public void MoveNext()
        {
            if (AtEnd())
            {
                return;
            }
            else if (_pList == null)
            {
                _pCurrent = null;
            }
            else
            {
                Init(_pList.OptionalNextListNode);
            }
        }

        public static int Count(Expr pExpr)
        {
            int c = 0;
            for (ExpressionIterator it = new ExpressionIterator(pExpr); !it.AtEnd(); it.MoveNext())
            {
                ++c;
            }
            return c;
        }

        private ExprList _pList;
        private Expr _pCurrent;

        private void Init(Expr pExpr)
        {
            if (pExpr == null)
            {
                _pList = null;
                _pCurrent = null;
            }
            else if (pExpr is ExprList pList)
            {
                _pList = pList;
                _pCurrent = pList.OptionalElement;
            }
            else
            {
                _pList = null;
                _pCurrent = pExpr;
            }
        }
    }
}
