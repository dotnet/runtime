// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Reflection.Core.Execution;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Reflection.Runtime.TypeInfos;
using RuntimeTypeInfo = System.Reflection.TypeLoading.RoType;

namespace System.Reflection.Runtime.BindingFlagSupport
{
    //
    // Stores the result of a member filtering that's filtered by name and visibility from base class (as defined by the Type.Get*() family of apis).
    //
    // The results are as if you'd passed in a bindingFlags value of "Public | NonPublic | Instance | Static | FlattenHierarchy"
    // In addition, if "ignoreCase" was passed to Create(), BindingFlags.IgnoreCase is also in effect.
    //
    // Results are sorted by declaring type. The members declared by the most derived type appear first, then those declared by his base class, and so on.
    // The Disambiguation logic takes advantage of this.
    //
    // This object is a good candidate for long term caching.
    //
    // QueryMemberList's come in two flavors: ImmediateTypeOnly and full. The immediateTypeOnly only holds the results for one type, not any of its
    // base types. This is used when the binding flags passed to a Get() api limit the search to the immediate type only in order to avoid triggering
    // unnecessary assembly resolving and a lot of unnecessary ParameterInfo creation and comparison checks.
    //
    internal sealed class QueriedMemberList<M> where M : MemberInfo
    {
        private QueriedMemberList(bool immediateTypeOnly)
        {
            _members = new M[Grow];
            _allFlagsThatMustMatch = new BindingFlags[Grow];
            ImmediateTypeOnly = immediateTypeOnly;
        }

        private QueriedMemberList(int totalCount, int declaredOnlyCount, M[] members, BindingFlags[] allFlagsThatMustMatch, RuntimeTypeInfo typeThatBlockedBrowsing)
        {
            _totalCount = totalCount;
            _declaredOnlyCount = declaredOnlyCount;
            _members = members;
            _allFlagsThatMustMatch = allFlagsThatMustMatch;
            _typeThatBlockedBrowsing = typeThatBlockedBrowsing;
        }

        /// <summary>
        /// Returns the # of candidates for a non-DeclaredOnly search. Caution: Can throw MissingMetadataException. Use DeclaredOnlyCount if you don't want to search base classes.
        /// </summary>
        public int TotalCount
        {
            get
            {
                if (_typeThatBlockedBrowsing != null)
                    throw ReflectionCoreExecution.ExecutionDomain.CreateMissingMetadataException(_typeThatBlockedBrowsing);
                return _totalCount;
            }
        }

        /// <summary>
        /// Returns the # of candidates for a DeclaredOnly search
        /// </summary>
        public int DeclaredOnlyCount => _declaredOnlyCount;

        public bool ImmediateTypeOnly { get; } 

        public M this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Debug.Assert(index >= 0 && index < _totalCount);
                return _members[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(int index, BindingFlags bindingAttr)
        {
            Debug.Assert(index >= 0 && index < _totalCount);
            BindingFlags allFlagsThatMustMatch = _allFlagsThatMustMatch[index];
            return ((bindingAttr & allFlagsThatMustMatch) == allFlagsThatMustMatch);
        }

        public QueriedMemberList<M> Filter(Func<M, bool> predicate)
        {
            BindingFlags[] newAllFlagsThatMustMatch = new BindingFlags[_totalCount];
            M[] newMembers = new M[_totalCount];
            int newDeclaredOnlyCount = 0;
            int newTotalCount = 0;
            for (int i = 0; i < _totalCount; i++)
            {
                M member = _members[i];
                if (predicate(member))
                {
                    newMembers[newTotalCount] = member;
                    newAllFlagsThatMustMatch[newTotalCount] = _allFlagsThatMustMatch[i];
                    newTotalCount++;
                    if (i < _declaredOnlyCount)
                        newDeclaredOnlyCount++;
                }
            }

            return new QueriedMemberList<M>(newTotalCount, newDeclaredOnlyCount, newMembers, newAllFlagsThatMustMatch, _typeThatBlockedBrowsing);
        }

        //
        // Filter by name and visibility from the ReflectedType.
        //
        public static QueriedMemberList<M> Create(RuntimeTypeInfo type, string filter, bool ignoreCase, bool immediateTypeOnly)
        {
            RuntimeTypeInfo reflectedType = type;

            MemberPolicies<M> policies = MemberPolicies<M>.Default;

            NameFilter nameFilter;
            if (filter == null)
                nameFilter = null;
            else if (ignoreCase)
                nameFilter = new NameFilterCaseInsensitive(filter);
            else
                nameFilter = new NameFilterCaseSensitive(filter);

            bool inBaseClass = false;
            QueriedMemberList<M> queriedMembers = new QueriedMemberList<M>(immediateTypeOnly);
            while (type != null)
            {
                int numCandidatesInDerivedTypes = queriedMembers._totalCount;

                foreach (M member in policies.CoreGetDeclaredMembers(type, nameFilter, reflectedType))
                {
                    policies.GetMemberAttributes(member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot);

                    if (inBaseClass && visibility == MethodAttributes.Private)
                        continue;

                    if (numCandidatesInDerivedTypes != 0 && policies.IsSuppressedByMoreDerivedMember(member, queriedMembers._members, startIndex: 0, endIndex: numCandidatesInDerivedTypes))
                        continue;

                    BindingFlags allFlagsThatMustMatch = default;
                    allFlagsThatMustMatch |= (isStatic ? BindingFlags.Static : BindingFlags.Instance);
                    if (isStatic && inBaseClass)
                        allFlagsThatMustMatch |= BindingFlags.FlattenHierarchy;
                    allFlagsThatMustMatch |= ((visibility == MethodAttributes.Public) ? BindingFlags.Public : BindingFlags.NonPublic);

                    queriedMembers.Add(member, allFlagsThatMustMatch);
                }

                if (!inBaseClass)
                {
                    queriedMembers._declaredOnlyCount = queriedMembers._totalCount;

                    if (immediateTypeOnly)
                        break;

                    if (policies.AlwaysTreatAsDeclaredOnly)
                        break;
                    inBaseClass = true;
                }

                type = type.BaseType.CastToRuntimeTypeInfo();
                if (type != null && !type.CanBrowseWithoutMissingMetadataExceptions())
                {
                    // If we got here, one of the base classes is missing metadata. We don't want to throw a MissingMetadataException now because we may be 
                    // building a cached result for a caller who passed BindingFlags.DeclaredOnly. So we'll mark the results in a way that 
                    // it will throw a MissingMetadataException if a caller attempts to iterate past the declared-only subset.
                    queriedMembers._typeThatBlockedBrowsing = type;
                    queriedMembers._totalCount = queriedMembers._declaredOnlyCount;
                    break;
                }
            }

            return queriedMembers;
        }

        public void Compact()
        {
            Array.Resize(ref _members, _totalCount);
            Array.Resize(ref _allFlagsThatMustMatch, _totalCount);
        }

        private void Add(M member, BindingFlags allFlagsThatMustMatch)
        {
            const BindingFlags validBits = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            Debug.Assert((allFlagsThatMustMatch & ~validBits) == 0);
            Debug.Assert(((allFlagsThatMustMatch & BindingFlags.Public) == 0) != ((allFlagsThatMustMatch & BindingFlags.NonPublic) == 0));
            Debug.Assert(((allFlagsThatMustMatch & BindingFlags.Instance) == 0) != ((allFlagsThatMustMatch & BindingFlags.Static) == 0));
            Debug.Assert((allFlagsThatMustMatch & BindingFlags.FlattenHierarchy) == 0 || (allFlagsThatMustMatch & BindingFlags.Static) != 0);

            int count = _totalCount;
            if (count == _members.Length)
            {
                Array.Resize(ref _members, count + Grow);
                Array.Resize(ref _allFlagsThatMustMatch, count + Grow);
            }

            _members[count] = member;
            _allFlagsThatMustMatch[count] = allFlagsThatMustMatch;
            _totalCount++;
        }

        private int _totalCount; // # of entries including members in base classes.
        private int _declaredOnlyCount; // # of entries for members only in the most derived class.
        private M[] _members;  // Length is equal to or greater than _totalCount. Entries beyond _totalCount contain null or garbage and should be read.
        private BindingFlags[] _allFlagsThatMustMatch; // Length will be equal to _members.Length
        private RuntimeTypeInfo _typeThatBlockedBrowsing; // If non-null, one of the base classes was missing metadata.
        private const int Grow = 64;
    }
}
