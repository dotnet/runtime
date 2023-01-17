// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.BindingFlagSupport;

using Internal.Reflection.Core.Execution;

using Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace System.Reflection.Runtime.TypeInfos
{
    internal abstract partial class RuntimeTypeInfo
    {
        //================================================================================================================
        // TypeComponentsCache objects are allocated on-demand on a per-Type basis to cache hot data for key scenarios.
        // To maximize throughput once the cache is created, the object creates all of its internal caches up front
        // and holds entries strongly (and relying on the fact that Types themselves are held weakly to avoid immortality.)
        //
        // Note that it is possible that two threads racing to query the same TypeInfo may allocate and query two different
        // cache objects. Thus, this object must not be relied upon to preserve object identity.
        //================================================================================================================

        private sealed class TypeComponentsCache
        {
            public TypeComponentsCache(RuntimeTypeInfo type)
            {
                _type = type;

                _perNameQueryCaches_CaseSensitive = CreatePerNameQueryCaches(type, ignoreCase: false);
                _perNameQueryCaches_CaseInsensitive = CreatePerNameQueryCaches(type, ignoreCase: true);

                _nameAgnosticQueryCaches = new object[MemberTypeIndex.Count];
            }

            //
            // Returns the cached result of a name-specific query on the Type's members, as if you'd passed in
            //
            //  BindingFlags == Public | NonPublic | Instance | Static | FlattenHierarchy
            //
            public QueriedMemberList<M> GetQueriedMembers<M>(string name, bool ignoreCase) where M : MemberInfo
            {
                int index = MemberPolicies<M>.MemberTypeIndex;
                object obj = ignoreCase ? _perNameQueryCaches_CaseInsensitive[index] : _perNameQueryCaches_CaseSensitive[index];
                Debug.Assert(obj is PerNameQueryCache<M>);
                PerNameQueryCache<M> unifier = Unsafe.As<PerNameQueryCache<M>>(obj);
                QueriedMemberList<M> result = unifier.GetOrAdd(name);
                return result;
            }

            //
            // Returns the cached result of a name-agnostic query on the Type's members, as if you'd passed in
            //
            //  BindingFlags == Public | NonPublic | Instance | Static | FlattenHierarchy
            //
            public QueriedMemberList<M> GetQueriedMembers<M>() where M : MemberInfo
            {
                int index = MemberPolicies<M>.MemberTypeIndex;
                object result = Volatile.Read(ref _nameAgnosticQueryCaches[index]);
                if (result == null)
                {
                    QueriedMemberList<M> newResult = QueriedMemberList<M>.Create(_type, optionalNameFilter: null, ignoreCase: false);
                    newResult.Compact();
                    result = newResult;
                    Volatile.Write(ref _nameAgnosticQueryCaches[index], result);
                }

                Debug.Assert(result is QueriedMemberList<M>);
                return Unsafe.As<QueriedMemberList<M>>(result);
            }

            private static object[] CreatePerNameQueryCaches(RuntimeTypeInfo type, bool ignoreCase)
            {
                object[] perNameCaches = new object[MemberTypeIndex.Count];
                perNameCaches[MemberTypeIndex.Constructor] = new PerNameQueryCache<ConstructorInfo>(type, ignoreCase: ignoreCase);
                perNameCaches[MemberTypeIndex.Event] = new PerNameQueryCache<EventInfo>(type, ignoreCase: ignoreCase);
                perNameCaches[MemberTypeIndex.Field] = new PerNameQueryCache<FieldInfo>(type, ignoreCase: ignoreCase);
                perNameCaches[MemberTypeIndex.Method] = new PerNameQueryCache<MethodInfo>(type, ignoreCase: ignoreCase);
                perNameCaches[MemberTypeIndex.Property] = new PerNameQueryCache<PropertyInfo>(type, ignoreCase: ignoreCase);
                perNameCaches[MemberTypeIndex.NestedType] = new PerNameQueryCache<Type>(type, ignoreCase: ignoreCase);
                return perNameCaches;
            }

            // This array holds six PerNameQueryCache<M> objects, one for each of the possible M types (ConstructorInfo, EventInfo, etc.)
            // The caches are configured to do a case-sensitive query.
            private readonly object[] _perNameQueryCaches_CaseSensitive;

            // This array holds six PerNameQueryCache<M> objects, one for each of the possible M types (ConstructorInfo, EventInfo, etc.)
            // The caches are configured to do a case-insensitive query.
            private readonly object[] _perNameQueryCaches_CaseInsensitive;

            // This array holds six lazily created QueriedMemberList<M> objects, one for each of the possible M types (ConstructorInfo, EventInfo, etc.).
            // The objects are the results of a name-agnostic query.
            private readonly object[] _nameAgnosticQueryCaches;

            private readonly RuntimeTypeInfo _type;

            // Generic cache for scenario specific data. For example, it is used to cache Enum names and values.
            internal object? _genericCache;

            // The sole purpose of this struct is to defeat generic code sharing of PerNameQueryCache below.
            // We eagerly construct PerNameQueryCache when the type components cache is created, but they may not be
            // actually used in an app that is light on reflection use. But we do have other canonically equivalent
            // ConcurrentUnifier instances in the program and their use could drag this implementation into the app.
            // Making the ConcurrentUnifier instance unshareable gives the compiler the ability to unshare this.
            private struct StringKey : IEquatable<StringKey>
            {
                public readonly string Key;
                public StringKey(string k) => Key = k;
                public bool Equals(StringKey other) => Key == other.Key;
                public override bool Equals([NotNullWhen(true)] object? obj) => obj is StringKey k && Equals(k);
                public override int GetHashCode() => Key.GetHashCode();
                public static implicit operator StringKey(string k) => new StringKey(k);
                public static implicit operator string(StringKey k) => k.Key;
            }

            //
            // Each PerName cache persists the results of a Type.Get(name, bindingFlags) for a particular MemberInfoType "M".
            //
            // where "bindingFlags" == Public | NonPublic | Instance | Static | FlattenHierarchy
            //
            // In addition, if "ignoreCase" was passed to the constructor, BindingFlags.IgnoreCase is also in effect.
            //
            private sealed class PerNameQueryCache<M> : ConcurrentUnifier<StringKey, QueriedMemberList<M>> where M : MemberInfo
            {
                public PerNameQueryCache(RuntimeTypeInfo type, bool ignoreCase)
                {
                    _type = type;
                    _ignoreCase = ignoreCase;
                }

                protected sealed override QueriedMemberList<M> Factory(StringKey key)
                {
                    QueriedMemberList<M> result = QueriedMemberList<M>.Create(_type, key, ignoreCase: _ignoreCase);
                    result.Compact();
                    return result;
                }

                private readonly RuntimeTypeInfo _type;
                private readonly bool _ignoreCase;
            }
        }
    }
}
