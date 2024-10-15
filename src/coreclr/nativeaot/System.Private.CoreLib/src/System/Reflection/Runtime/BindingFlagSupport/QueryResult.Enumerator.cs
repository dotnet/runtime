// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection.Runtime.BindingFlagSupport
{
    internal partial struct QueryResult<M> where M : MemberInfo
    {
        internal struct QueryResultEnumerator
        {
            public QueryResultEnumerator(QueryResult<M> queryResult)
            {
                _bindingAttr = queryResult._bindingAttr;
                _unfilteredCount = queryResult.UnfilteredCount;
                _queriedMembers = queryResult._queriedMembers;
                _index = -1;
            }

            public bool MoveNext()
            {
                while (++_index < _unfilteredCount && !_queriedMembers.Matches(_index, _bindingAttr))
                {
                }

                if (_index < _unfilteredCount)
                    return true;

                _index = _unfilteredCount; // guard against wiseguys calling MoveNext() over and over after the end.
                return false;
            }

            public M Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return _queriedMembers[_index];
                }
            }

            private int _index;
            private readonly int _unfilteredCount;
            private readonly BindingFlags _bindingAttr;
            private readonly QueriedMemberList<M> _queriedMembers;
        }
    }
}
