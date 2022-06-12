// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;

namespace System.Reflection.Runtime.BindingFlagSupport
{
    //
    // Stores the result of a member filtering that's further filtered by the Public, NonPublic, Instance, Static and FlatternHierarchy bits of BindingFlags.
    // This object is not considered a candidate for long term caching.
    //
    // Note: The uninitialized state ("qr = default(QueryResult<M>)) is considered a valid state for this object, and represents an empty list of members.
    //
    internal partial struct QueryResult<M> where M : MemberInfo
    {
        public QueryResult(BindingFlags bindingAttr, QueriedMemberList<M> queriedMembers)
        {
            _lazyCount = 0;
            _bindingAttr = bindingAttr;
            _queriedMembers = queriedMembers;
        }

        public QueryResultEnumerator GetEnumerator() => new QueryResultEnumerator(this);

        /// <summary>
        /// Returns the number of matching results.
        /// </summary>
        public int Count
        {
            get
            {
                int count = _lazyCount;
                if (count == 0)
                {
                    if (_queriedMembers == null)
                        return 0;  // This is an uninitialized QueryResult<M>, which is supported and represents a 0-length list of matches.

                    int unfilteredCount = UnfilteredCount;
                    for (int i = 0; i < unfilteredCount; i++)
                    {
                        if (_queriedMembers.Matches(i, _bindingAttr))
                            count++;
                    }

                    if (count == 0)
                    {
                        // If no matches were found, set ourselves back to the "uninitialized" state so that future
                        // calls to Count won't go through this calculation again.
                        _queriedMembers = null;
                    }

                    _lazyCount = count;

                }
                return count;
            }
        }

        /// <summary>
        /// Copies the results to a freshly allocated array. Use this at api boundary points.
        /// </summary>
        public M[] ToArray()
        {
            int count = Count;
            if (count == 0)
                return Array.Empty<M>();

            M[] newArray = new M[count];
            CopyTo(newArray, 0);
            return newArray;
        }

        /// <summary>
        /// Copies the results into an existing array.
        /// </summary>
        public void CopyTo(MemberInfo[] array, int startIndex)
        {
            if (_queriedMembers == null)
                return; // This is an uninitialized QueryResult<M>, which is supported and represents a 0-length list of matches.

            int unfilteredCount = UnfilteredCount;
            for (int i = 0; i < unfilteredCount; i++)
            {
                if (_queriedMembers.Matches(i, _bindingAttr))
                {
                    array[startIndex++] = _queriedMembers[i];
                }
            }
        }

        /// <summary>
        /// Returns a single member, null or throws AmbigousMatchException, for the Type.Get*(string name,...) family of apis.
        /// </summary>
        public M? Disambiguate()
        {
            if (_queriedMembers == null)
                return null; // This is an uninitialized QueryResult<M>, which is supported and represents a 0-length list of matches.

            int unfilteredCount = UnfilteredCount;

            M? match = null;
            for (int i = 0; i < unfilteredCount; i++)
            {
                if (_queriedMembers.Matches(i, _bindingAttr))
                {
                    if (match != null)
                    {
                        M challenger = _queriedMembers[i];

                        // Assuming the policy says it's ok to ignore the ambiguity, we're to resolve in favor of the member
                        // declared by the most derived type. Since QueriedMemberLists are sorted in order of decreasing derivation,
                        // that means we let the first match win - unless, of course, they're both the "most derived member".
                        if (match.DeclaringType.Equals(challenger.DeclaringType))
                            throw new AmbiguousMatchException();

                        MemberPolicies<M> policies = MemberPolicies<M>.Default;
                        if (!policies.OkToIgnoreAmbiguity(match, challenger))
                            throw new AmbiguousMatchException();
                    }
                    else
                    {
                        match = _queriedMembers[i];
                    }
                }
            }
            return match;
        }

        private int UnfilteredCount => ((_bindingAttr & BindingFlags.DeclaredOnly) != 0) ? _queriedMembers.DeclaredOnlyCount : _queriedMembers.TotalCount;

        private readonly BindingFlags _bindingAttr;
        private int _lazyCount; // Intentionally not marking as volatile. QueryResult is for short-term use within a single method call - no aspiration to be thread-safe.
        private QueriedMemberList<M> _queriedMembers;
    }
}
