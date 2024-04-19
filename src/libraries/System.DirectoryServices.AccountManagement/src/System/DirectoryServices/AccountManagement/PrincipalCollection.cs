// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.DirectoryServices.AccountManagement
{
    public class PrincipalCollection : ICollection<Principal>, ICollection, IEnumerable<Principal>, IEnumerable
    {
        //
        // ICollection
        //
        void ICollection.CopyTo(Array array, int index)
        {
            CheckDisposed();

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (array.Rank != 1)
                throw new ArgumentException(SR.PrincipalCollectionNotOneDimensional);

            if (index >= array.GetLength(0))
                throw new ArgumentException(SR.PrincipalCollectionIndexNotInArray);

            ArrayList tempArray = new ArrayList();

            lock (_resultSet)
            {
                ResultSetBookmark bookmark = null;

                try
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "CopyTo: bookmarking");

                    bookmark = _resultSet.BookmarkAndReset();

                    PrincipalCollectionEnumerator containmentEnumerator =
                                new PrincipalCollectionEnumerator(
                                                            _resultSet,
                                                            this,
                                                            _removedValuesCompleted,
                                                            _removedValuesPending,
                                                            _insertedValuesCompleted,
                                                            _insertedValuesPending);

                    int arraySize = array.GetLength(0) - index;
                    int tempArraySize = 0;

                    while (containmentEnumerator.MoveNext())
                    {
                        tempArray.Add(containmentEnumerator.Current);
                        checked { tempArraySize++; }

                        // Make sure the array has enough space, allowing for the "index" offset.
                        // We check inline, rather than doing a PrincipalCollection.Count upfront,
                        // because counting is just as expensive as enumerating over all the results, so we
                        // only want to do it once.
                        if (arraySize < tempArraySize)
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Warn,
                                                    "PrincipalCollection",
                                                    "CopyTo: array too small (has {0}, need >= {1}",
                                                    arraySize,
                                                    tempArraySize);

                            throw new ArgumentException(SR.PrincipalCollectionArrayTooSmall);
                        }
                    }
                }
                finally
                {
                    if (bookmark != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "CopyTo: restoring from bookmark");
                        _resultSet.RestoreBookmark(bookmark);
                    }
                }
            }

            foreach (object o in tempArray)
            {
                array.SetValue(o, index);
                checked { index++; }
            }
        }

        int ICollection.Count
        {
            get
            {
                return Count;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return IsSynchronized;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return SyncRoot;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public object SyncRoot
        {
            get
            {
                return this;
            }
        }

        //
        // IEnumerable
        //
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        //
        // ICollection<Principal>
        //
        public void CopyTo(Principal[] array, int index)
        {
            ((ICollection)this).CopyTo((Array)array, index);
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public int Count
        {
            get
            {
                CheckDisposed();

                // Yes, this is potentially quite expensive.  Count is unfortunately
                // an expensive operation to perform.
                lock (_resultSet)
                {
                    ResultSetBookmark bookmark = null;

                    try
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "Count: bookmarking");

                        bookmark = _resultSet.BookmarkAndReset();

                        PrincipalCollectionEnumerator containmentEnumerator =
                                    new PrincipalCollectionEnumerator(
                                                                _resultSet,
                                                                this,
                                                                _removedValuesCompleted,
                                                                _removedValuesPending,
                                                                _insertedValuesCompleted,
                                                                _insertedValuesPending);

                        int count = 0;

                        // Count all the members (including inserted members, but not including removed members)
                        while (containmentEnumerator.MoveNext())
                        {
                            count++;
                        }

                        return count;
                    }
                    finally
                    {
                        if (bookmark != null)
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "Count: restoring from bookmark");
                            _resultSet.Reset();
                            _resultSet.RestoreBookmark(bookmark);
                        }
                    }
                }
            }
        }

        //
        // IEnumerable<Principal>
        //
        public IEnumerator<Principal> GetEnumerator()
        {
            CheckDisposed();

            return new PrincipalCollectionEnumerator(
                                            _resultSet,
                                            this,
                                            _removedValuesCompleted,
                                            _removedValuesPending,
                                            _insertedValuesCompleted,
                                            _insertedValuesPending);
        }

        //
        // Add
        //

        public void Add(UserPrincipal user)
        {
            Add((Principal)user);
        }

        public void Add(GroupPrincipal group)
        {
            Add((Principal)group);
        }

        public void Add(ComputerPrincipal computer)
        {
            Add((Principal)computer);
        }

        public void Add(Principal principal)
        {
            CheckDisposed();

            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            if (Contains(principal))
                throw new PrincipalExistsException(SR.PrincipalExistsExceptionText);

            MarkChange();

            // If the value to be added is an uncommitted remove, just remove the uncommitted remove from the list.
            if (_removedValuesPending.Contains(principal))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "Add: removing from removedValuesPending");

                _removedValuesPending.Remove(principal);

                // If they did a Add(x) --> Save() --> Remove(x) --> Add(x), we'll end up with x in neither
                // the ResultSet or the insertedValuesCompleted list.  This is bad.  Avoid that by adding x
                // back to the insertedValuesCompleted list here.
                //
                // Note, this will add x to insertedValuesCompleted even if it's already in the resultSet.
                // This is not a problem --- PrincipalCollectionEnumerator will detect such a situation and
                // only return x once.
                if (!_insertedValuesCompleted.Contains(principal))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "Add: adding to insertedValuesCompleted");
                    _insertedValuesCompleted.Add(principal);
                }
            }
            else
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "Add: making it a pending insert");

                // make it a pending insert
                _insertedValuesPending.Add(principal);

                // in case the value to be added is also a previous-but-already-committed remove
                _removedValuesCompleted.Remove(principal);
            }
        }

        public void Add(PrincipalContext context, IdentityType identityType, string identityValue)
        {
            CheckDisposed();

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (identityValue == null)
                throw new ArgumentNullException(nameof(identityValue));

            Principal principal = Principal.FindByIdentity(context, identityType, identityValue);

            if (principal != null)
            {
                Add(principal);
            }
            else
            {
                // No Principal matching the IdentityReference could be found in the PrincipalContext
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "PrincipalCollection", "Add(urn/urn): no match");
                throw new NoMatchingPrincipalException(SR.NoMatchingPrincipalExceptionText);
            }
        }

        //
        // Clear
        //
        public void Clear()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "Clear");

            CheckDisposed();

            // Ask the StoreCtx to verify that this group can be cleared.  Right now, the only
            // reason it couldn't is if it's an AD group with one principals that are members of it
            // by virtue of their primaryGroupId.
            //
            // If storeCtxToUse == null, then we must be unpersisted, in which case there clearly
            // can't be any such primary group members pointing to this group on the store.
            StoreCtx storeCtxToUse = _owningGroup.GetStoreCtxToUse();
            string explanation;

            Debug.Assert(storeCtxToUse != null || _owningGroup.unpersisted);

            if ((storeCtxToUse != null) && (!storeCtxToUse.CanGroupBeCleared(_owningGroup, out explanation)))
                throw new InvalidOperationException(explanation);

            MarkChange();

            // We wipe out everything that's been inserted/removed
            _insertedValuesPending.Clear();
            _removedValuesPending.Clear();
            _insertedValuesCompleted.Clear();
            _removedValuesCompleted.Clear();

            // flag that the collection has been cleared, so the StoreCtx and PrincipalCollectionEnumerator
            // can take appropriate action
            _clearPending = true;
        }

        //
        // Remove
        //

        public bool Remove(UserPrincipal user)
        {
            return Remove((Principal)user);
        }

        public bool Remove(GroupPrincipal group)
        {
            return Remove((Principal)group);
        }

        public bool Remove(ComputerPrincipal computer)
        {
            return Remove((Principal)computer);
        }

        public bool Remove(Principal principal)
        {
            CheckDisposed();

            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            // Ask the StoreCtx to verify that this member can be removed.  Right now, the only
            // reason it couldn't is if it's actually a member by virtue of its primaryGroupId
            // pointing to this group.
            //
            // If storeCtxToUse == null, then we must be unpersisted, in which case there clearly
            // can't be any such primary group members pointing to this group on the store.
            StoreCtx storeCtxToUse = _owningGroup.GetStoreCtxToUse();
            string explanation;

            Debug.Assert(storeCtxToUse != null || _owningGroup.unpersisted);

            if ((storeCtxToUse != null) && (!storeCtxToUse.CanGroupMemberBeRemoved(_owningGroup, principal, out explanation)))
                throw new InvalidOperationException(explanation);

            bool removed = false;

            // If the value was previously inserted, we just remove it from insertedValuesPending.
            if (_insertedValuesPending.Contains(principal))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "Remove: removing from insertedValuesPending");

                MarkChange();
                _insertedValuesPending.Remove(principal);
                removed = true;

                // If they did a Remove(x) --> Save() --> Add(x) --> Remove(x), we'll end up with x in neither
                // the ResultSet or the removedValuesCompleted list.  This is bad.  Avoid that by adding x
                // back to the removedValuesCompleted list here.
                //
                // At worst, we end up with x on the removedValuesCompleted list even though it's not even in
                // resultSet.  That's not a problem.  The only thing we use the removedValuesCompleted list for
                // is deciding which values in resultSet to skip, anyway.
                if (!_removedValuesCompleted.Contains(principal))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "Remove: adding to removedValuesCompleted");
                    _removedValuesCompleted.Add(principal);
                }
            }
            else
            {
                // They're trying to remove an already-persisted value.  We add it to the
                // removedValues list.  Then, if it's already been loaded into insertedValuesCompleted,
                // we remove it from insertedValuesCompleted.

                removed = Contains(principal);
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "Remove: making it a pending remove, removed={0}", removed);

                if (removed)
                {
                    MarkChange();

                    _removedValuesPending.Add(principal);

                    // in case it's the result of a previous-but-already-committed insert
                    _insertedValuesCompleted.Remove(principal);
                }
            }

            return removed;
        }

        public bool Remove(PrincipalContext context, IdentityType identityType, string identityValue)
        {
            CheckDisposed();

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (identityValue == null)
                throw new ArgumentNullException(nameof(identityValue));

            Principal principal = Principal.FindByIdentity(context, identityType, identityValue);

            if (principal == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "PrincipalCollection", "Remove(urn/urn): no match");
                throw new NoMatchingPrincipalException(SR.NoMatchingPrincipalExceptionText);
            }

            return Remove(principal);
        }

        //
        // Contains
        //

        private bool ContainsEnumTest(Principal principal)
        {
            CheckDisposed();

            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            // Yes, this is potentially quite expensive.  Contains is unfortunately
            // an expensive operation to perform.
            lock (_resultSet)
            {
                ResultSetBookmark bookmark = null;

                try
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "ContainsEnumTest: bookmarking");

                    bookmark = _resultSet.BookmarkAndReset();

                    PrincipalCollectionEnumerator containmentEnumerator =
                                new PrincipalCollectionEnumerator(
                                                            _resultSet,
                                                            this,
                                                            _removedValuesCompleted,
                                                            _removedValuesPending,
                                                            _insertedValuesCompleted,
                                                            _insertedValuesPending);

                    while (containmentEnumerator.MoveNext())
                    {
                        Principal p = containmentEnumerator.Current;

                        if (p.Equals(principal))
                            return true;
                    }
                }
                finally
                {
                    if (bookmark != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "ContainsEnumTest: restoring from bookmark");
                        _resultSet.RestoreBookmark(bookmark);
                    }
                }
            }

            return false;
        }

        private bool ContainsNativeTest(Principal principal)
        {
            CheckDisposed();

            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            // If they explicitly inserted it, then we certainly contain it
            if (_insertedValuesCompleted.Contains(principal) || _insertedValuesPending.Contains(principal))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "ContainsNativeTest: found insert");
                return true;
            }

            // If they removed it, we don't contain it, regardless of the group membership on the store
            if (_removedValuesCompleted.Contains(principal) || _removedValuesPending.Contains(principal))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "ContainsNativeTest: found remove");
                return false;
            }

            // The list was cleared at some point and the principal has not been reinsterted yet
            if (_clearPending || _clearCompleted)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "ContainsNativeTest: Clear pending");
                return false;
            }

            // Otherwise, check the store
            if (_owningGroup.unpersisted == false && principal.unpersisted == false)
                return _owningGroup.GetStoreCtxToUse().IsMemberOfInStore(_owningGroup, principal);

            // We (or the principal) must not be persisted, so there's no store membership to check.
            // Out of things to check.  We must not contain it.
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "ContainsNativeTest: no store to check");
            return false;
        }

        public bool Contains(UserPrincipal user)
        {
            return Contains((Principal)user);
        }

        public bool Contains(GroupPrincipal group)
        {
            return Contains((Principal)group);
        }

        public bool Contains(ComputerPrincipal computer)
        {
            return Contains((Principal)computer);
        }

        public bool Contains(Principal principal)
        {
            StoreCtx storeCtxToUse = _owningGroup.GetStoreCtxToUse();

            // If the store has a shortcut for testing membership, use it.
            // Otherwise we enumerate all members and look for a match.
            if ((storeCtxToUse != null) && (storeCtxToUse.SupportsNativeMembershipTest))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                        "PrincipalCollection",
                                        "Contains: using native test (store ctx is null = {0})",
                                        (storeCtxToUse == null));

                return ContainsNativeTest(principal);
            }
            else
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "Contains: using enum test");
                return ContainsEnumTest(principal);
            }
        }

        public bool Contains(PrincipalContext context, IdentityType identityType, string identityValue)
        {
            CheckDisposed();

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (identityValue == null)
                throw new ArgumentNullException(nameof(identityValue));

            bool found = false;

            Principal principal = Principal.FindByIdentity(context, identityType, identityValue);

            if (principal != null)
                found = Contains(principal);

            return found;
        }

        //
        // Internal constructor
        //

        // Constructs a fresh PrincipalCollection based on the supplied ResultSet.
        // The ResultSet may not be null (use an EmptySet instead).
        internal PrincipalCollection(BookmarkableResultSet results, GroupPrincipal owningGroup)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "Ctor");

            Debug.Assert(results != null);

            _resultSet = results;
            _owningGroup = owningGroup;
        }

        //
        // Internal "disposer"
        //

        // Ideally, we'd like to implement IDisposable, since we need to dispose of the ResultSet.
        // But IDisposable would have to be a public interface, and we don't want the apps calling Dispose()
        // on us, only the Principal that owns us.  So we implement an "internal" form of Dispose().
        internal void Dispose()
        {
            if (!_disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "Dispose: disposing");

                lock (_resultSet)
                {
                    if (_resultSet != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "Dispose: disposing resultSet");
                        _resultSet.Dispose();
                    }
                }

                _disposed = true;
            }
        }

        //
        // Private implementation
        //

        // The group we're a PrincipalCollection of
        private readonly GroupPrincipal _owningGroup;

        //
        // SYNCHRONIZATION
        //   Access to:
        //      resultSet
        //   must be synchronized, since multiple enumerators could be iterating over us at once.
        //   Synchronize by locking on resultSet.

        // Represents the Principals retrieved from the store for this collection
        private readonly BookmarkableResultSet _resultSet;

        // Contains Principals inserted into this collection for which the insertion has not been persisted to the store
        private readonly List<Principal> _insertedValuesCompleted = new List<Principal>();
        private readonly List<Principal> _insertedValuesPending = new List<Principal>();

        // Contains Principals removed from this collection for which the removal has not been persisted
        // to the store
        private readonly List<Principal> _removedValuesCompleted = new List<Principal>();
        private readonly List<Principal> _removedValuesPending = new List<Principal>();

        // Has this collection been cleared?
        private bool _clearPending;
        private bool _clearCompleted;

        internal bool ClearCompleted
        {
            get { return _clearCompleted; }
        }

        // Used so our enumerator can detect changes to the collection and throw an exception
        private DateTime _lastChange = DateTime.UtcNow;

        internal DateTime LastChange
        {
            get { return _lastChange; }
        }

        internal void MarkChange()
        {
            _lastChange = DateTime.UtcNow;
        }

        // To support disposal
        private bool _disposed;

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("PrincipalCollection");
        }

        //
        // Load/Store Implementation
        //

        internal List<Principal> Inserted
        {
            get
            {
                return _insertedValuesPending;
            }
        }

        internal List<Principal> Removed
        {
            get
            {
                return _removedValuesPending;
            }
        }

        internal bool Cleared
        {
            get
            {
                return _clearPending;
            }
        }

        // Return true if the membership has changed (i.e., either insertedValuesPending or removedValuesPending is
        // non-empty)
        internal bool Changed
        {
            get
            {
                return ((_insertedValuesPending.Count > 0) || (_removedValuesPending.Count > 0) || (_clearPending));
            }
        }

        // Resets the change-tracking of the membership to 'unchanged', by moving all pending operations to completed status.
        internal void ResetTracking()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "ResetTracking");

            foreach (Principal p in _removedValuesPending)
            {
                Debug.Assert(!_removedValuesCompleted.Contains(p));
                _removedValuesCompleted.Add(p);
            }

            _removedValuesPending.Clear();

            foreach (Principal p in _insertedValuesPending)
            {
                Debug.Assert(!_insertedValuesCompleted.Contains(p));
                _insertedValuesCompleted.Add(p);
            }

            _insertedValuesPending.Clear();

            if (_clearPending)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollection", "ResetTracking: clearing");

                _clearCompleted = true;
                _clearPending = false;
            }
        }
    }
}
