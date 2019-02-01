// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.Runtime.BindingFlagSupport;
using System.Threading;
using RuntimeTypeInfo = System.Reflection.TypeLoading.RoType;

namespace System.Reflection.TypeLoading
{
    internal abstract partial class RoType
    {
        /// <summary>
        /// TypeComponentsCache objects are allocated on-demand on a per-Type basis to cache hot data for key scenarios.
        /// To maximize throughput once the cache is created, the object creates all of its internal caches up front
        /// and holds entries strongly (and relying on the fact that Types themselves are held weakly to avoid immortality.)
        ///
        /// Note that it is possible that two threads racing to query the same TypeInfo may allocate and query two different
        /// cache objects. Thus, this object must not be relied upon to preserve object identity.
        /// </summary>

        private sealed class TypeComponentsCache
        {
            public TypeComponentsCache(RuntimeTypeInfo type)
            {
                _type = type;

                _perNameQueryCaches_CaseSensitive = CreatePerNameQueryCaches(type, ignoreCase: false, immediateTypeOnly: false);
                _perNameQueryCaches_CaseInsensitive = CreatePerNameQueryCaches(type, ignoreCase: true, immediateTypeOnly: false);

                _perNameQueryCaches_CaseSensitive_ImmediateTypeOnly = CreatePerNameQueryCaches(type, ignoreCase: false, immediateTypeOnly: true);
                _perNameQueryCaches_CaseInsensitive_ImmediateTypeOnly = CreatePerNameQueryCaches(type, ignoreCase: true, immediateTypeOnly: true);

                _nameAgnosticQueryCaches = new object[MemberTypeIndex.Count];
            }

            //
            // Returns the cached result of a name-specific query on the Type's members, as if you'd passed in
            //
            //  BindingFlags == Public | NonPublic | Instance | Static | FlattenHierarchy  (immediateTypeOnly == false)
            //                  Public | NonPublic | Instance | Static | DeclaredOnly      (immediateTypeOnly == true)
            //
            public QueriedMemberList<M> GetQueriedMembers<M>(string name, bool ignoreCase, bool immediateTypeOnly) where M : MemberInfo
            {
                int index = MemberPolicies<M>.MemberTypeIndex;
                object[] cacheArray = ignoreCase ?
                    (immediateTypeOnly ? _perNameQueryCaches_CaseInsensitive_ImmediateTypeOnly : _perNameQueryCaches_CaseInsensitive) :
                    (immediateTypeOnly ? _perNameQueryCaches_CaseSensitive_ImmediateTypeOnly : _perNameQueryCaches_CaseSensitive);

                object unifierAsObject = cacheArray[index];
                PerNameQueryCache<M> unifier = (PerNameQueryCache<M>)unifierAsObject;
                QueriedMemberList<M> result = unifier.GetOrAdd(name);
                return result;
            }

            //
            // Returns the cached result of a name-agnostic query on the Type's members, as if you'd passed in
            //
            //  BindingFlags == Public | NonPublic | Instance | Static | FlattenHierarchy  (immediateTypeOnly == false)
            //                  Public | NonPublic | Instance | Static | DeclaredOnly      (immediateTypeOnly == true)
            //
            public QueriedMemberList<M> GetQueriedMembers<M>(bool immediateTypeOnly) where M : MemberInfo
            {
                int index = MemberPolicies<M>.MemberTypeIndex;
                object result = Volatile.Read(ref _nameAgnosticQueryCaches[index]);
                if (result == null)
                {
                    QueriedMemberList<M> newResult = QueriedMemberList<M>.Create(_type, filter: null, ignoreCase: false, immediateTypeOnly: immediateTypeOnly);
                    newResult.Compact();
                    Volatile.Write(ref _nameAgnosticQueryCaches[index], newResult);
                    return newResult;
                }
                QueriedMemberList<M> list = (QueriedMemberList<M>)result;
                if (list.ImmediateTypeOnly && !immediateTypeOnly)
                {
                    QueriedMemberList<M> newResult = QueriedMemberList<M>.Create(_type, filter: null, ignoreCase: false, immediateTypeOnly: false);
                    newResult.Compact();
                    Volatile.Write(ref _nameAgnosticQueryCaches[index], newResult);
                    return newResult;
                }
                return list;
            }

            private static object[] CreatePerNameQueryCaches(RuntimeTypeInfo type, bool ignoreCase, bool immediateTypeOnly)
            {
                object[] perNameCaches = new object[MemberTypeIndex.Count];
                perNameCaches[MemberTypeIndex.Constructor] = new PerNameQueryCache<ConstructorInfo>(type, ignoreCase: ignoreCase, immediateTypeOnly: immediateTypeOnly);
                perNameCaches[MemberTypeIndex.Event] = new PerNameQueryCache<EventInfo>(type, ignoreCase: ignoreCase, immediateTypeOnly: immediateTypeOnly);
                perNameCaches[MemberTypeIndex.Field] = new PerNameQueryCache<FieldInfo>(type, ignoreCase: ignoreCase, immediateTypeOnly: immediateTypeOnly);
                perNameCaches[MemberTypeIndex.Method] = new PerNameQueryCache<MethodInfo>(type, ignoreCase: ignoreCase, immediateTypeOnly: immediateTypeOnly);
                perNameCaches[MemberTypeIndex.Property] = new PerNameQueryCache<PropertyInfo>(type, ignoreCase: ignoreCase, immediateTypeOnly: immediateTypeOnly);
                perNameCaches[MemberTypeIndex.NestedType] = new PerNameQueryCache<Type>(type, ignoreCase: ignoreCase, immediateTypeOnly: immediateTypeOnly);
                return perNameCaches;
            }

            // This array holds six PerNameQueryCache<M> objects, one for each of the possible M types (ConstructorInfo, EventInfo, etc.)
            // The caches are configured to do a case-sensitive query.
            private readonly object[] _perNameQueryCaches_CaseSensitive;

            // This array holds six PerNameQueryCache<M> objects, one for each of the possible M types (ConstructorInfo, EventInfo, etc.)
            // The caches are configured to do a case-insensitive query.
            private readonly object[] _perNameQueryCaches_CaseInsensitive;

            // This array holds six PerNameQueryCache<M> objects, one for each of the possible M types (ConstructorInfo, EventInfo, etc.)
            // The caches are configured to do a case-sensitive query but do not contain results from base types.
            private readonly object[] _perNameQueryCaches_CaseSensitive_ImmediateTypeOnly;

            // This array holds six PerNameQueryCache<M> objects, one for each of the possible M types (ConstructorInfo, EventInfo, etc.)
            // The caches are configured to do a case-insensitive query but do not contain results from base types.
            private readonly object[] _perNameQueryCaches_CaseInsensitive_ImmediateTypeOnly;

            // This array holds six lazily created QueriedMemberList<M> objects, one for each of the possible M types (ConstructorInfo, EventInfo, etc.).
            // The objects are the results of a name-agnostic query.
            private readonly object[] _nameAgnosticQueryCaches;

            private readonly RuntimeTypeInfo _type;

            //
            // Each PerName cache persists the results of a Type.Get(name, bindingFlags) for a particular MemberInfoType "M".
            //
            // where "bindingFlags" == Public | NonPublic | Instance | Static | FlattenHierarchy for the total caches and
            //                         Public | NonPublic | Instance | Static | DeclaredOnly for the ImmediateTypeOnly caches.
            //
            // In addition, if "ignoreCase" was passed to the constructor, BindingFlags.IgnoreCase is also in effect.
            //
            private sealed class PerNameQueryCache<M> : ConcurrentUnifier<string, QueriedMemberList<M>> where M : MemberInfo
            {
                public PerNameQueryCache(RuntimeTypeInfo type, bool ignoreCase, bool immediateTypeOnly)
                {
                    _type = type;
                    _ignoreCase = ignoreCase;
                    _immediateTypeOnly = immediateTypeOnly;
                }

                protected sealed override QueriedMemberList<M> Factory(string key)
                {
                    QueriedMemberList<M> result = QueriedMemberList<M>.Create(_type, key, ignoreCase: _ignoreCase, immediateTypeOnly: _immediateTypeOnly);
                    result.Compact();
                    return result;
                }

                private readonly RuntimeTypeInfo _type;
                private readonly bool _ignoreCase;
                private readonly bool _immediateTypeOnly;
            }
        }
    }
}
