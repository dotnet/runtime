// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
    internal class PrincipalCollectionEnumerator : IEnumerator<Principal>, IEnumerator
    {
        //
        // Public properties
        //

        public Principal Current
        {
            get
            {
                CheckDisposed();

                // Since MoveNext() saved off the current value for us, this is largely trivial.

                if (_endReached == true || _currentMode == CurrentEnumeratorMode.None)
                {
                    // Either we're at the end or before the beginning
                    //  (CurrentEnumeratorMode.None implies we're _before_ the first value)

                    GlobalDebug.WriteLineIf(
                                        GlobalDebug.Warn,
                                        "PrincipalCollectionEnumerator",
                                        "Current: bad position, endReached={0}, currentMode={1}",
                                        _endReached,
                                        _currentMode);

                    throw new InvalidOperationException(SR.PrincipalCollectionEnumInvalidPos);
                }

                Debug.Assert(_current != null);
                return _current;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        //
        // Public methods
        //

        public bool MoveNext()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "Entering MoveNext");

            CheckDisposed();
            CheckChanged();

            // We previously reached the end, nothing more to do
            if (_endReached)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: endReached");
                return false;
            }

            lock (_resultSet)
            {
                if (_currentMode == CurrentEnumeratorMode.None)
                {
                    // At the very beginning

                    // In case this ResultSet was previously used with another PrincipalCollectionEnumerator instance
                    // (e.g., two foreach loops in a row)
                    _resultSet.Reset();

                    if (!_memberCollection.Cleared && !_memberCollection.ClearCompleted)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: None mode, starting with existing values");

                        // Start by enumerating the existing values in the store
                        _currentMode = CurrentEnumeratorMode.ResultSet;
                        _enumerator = null;
                    }
                    else
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: None mode, skipping existing values");

                        // The member collection was cleared.  Skip the ResultSet phase
                        _currentMode = CurrentEnumeratorMode.InsertedValuesCompleted;
                        _enumerator = (IEnumerator<Principal>)_insertedValuesCompleted.GetEnumerator();
                    }
                }

                Debug.Assert(_resultSet != null);

                if (_currentMode == CurrentEnumeratorMode.ResultSet)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: ResultSet mode");

                    bool needToRepeat = false;

                    do
                    {
                        bool f = _resultSet.MoveNext();

                        if (f)
                        {
                            Principal principal = (Principal)_resultSet.CurrentAsPrincipal;

                            if (_removedValuesCompleted.Contains(principal) || _removedValuesPending.Contains(principal))
                            {
                                // It's a value that's been removed (either a pending remove that hasn't completed, or a remove
                                // that completed _after_ we loaded the ResultSet from the store).    
                                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: ResultSet mode, found remove, skipping");

                                needToRepeat = true;
                                continue;
                            }
                            else if (_insertedValuesCompleted.Contains(principal) || _insertedValuesPending.Contains(principal))
                            {
                                // insertedValuesCompleted: We must have gotten the ResultSet after the inserted committed.
                                // We don't want to return
                                // the principal twice, so we'll skip it here and later return it in 
                                // the CurrentEnumeratorMode.InsertedValuesCompleted mode.
                                //
                                // insertedValuesPending: The principal must have been originally in the ResultSet, but then
                                // removed, saved, and re-added, with the re-add still pending.
                                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: ResultSet mode, found insert, skipping");

                                needToRepeat = true;
                                continue;
                            }
                            else
                            {
                                needToRepeat = false;
                                _current = principal;
                                return true;
                            }
                        }
                        else
                        {
                            // No more values left to retrieve.  Now try the insertedValuesCompleted list.
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: ResultSet mode, moving to InsValuesComp mode");

                            _currentMode = CurrentEnumeratorMode.InsertedValuesCompleted;
                            _enumerator = (IEnumerator<Principal>)_insertedValuesCompleted.GetEnumerator();
                            needToRepeat = false;
                        }
                    }
                    while (needToRepeat);
                }

                // These are values whose insertion has completed, but after we already loaded the ResultSet from the store.
                if (_currentMode == CurrentEnumeratorMode.InsertedValuesCompleted)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: InsValuesComp mode");

                    bool f = _enumerator.MoveNext();

                    if (f)
                    {
                        _current = _enumerator.Current;
                        return true;
                    }
                    else
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: InsValuesComp mode, moving to InsValuesPend mode");

                        _currentMode = CurrentEnumeratorMode.InsertedValuesPending;
                        _enumerator = (IEnumerator<Principal>)_insertedValuesPending.GetEnumerator();
                    }
                }

                // These are values whose insertion has not yet been committed to the store.
                if (_currentMode == CurrentEnumeratorMode.InsertedValuesPending)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: InsValuesPend mode");

                    bool f = _enumerator.MoveNext();

                    if (f)
                    {
                        _current = _enumerator.Current;
                        return true;
                    }
                    else
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "MoveNext: InsValuesPend mode, nothing left");

                        _endReached = true;
                        return false;
                    }
                }
            }

            Debug.Fail(String.Format(CultureInfo.CurrentCulture, "PrincipalCollectionEnumerator.MoveNext: fell off end of function, mode = {0}", _currentMode.ToString()));
            return false;
        }

        bool IEnumerator.MoveNext()
        {
            return MoveNext();
        }

        public void Reset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "Reset");

            CheckDisposed();
            CheckChanged();

            // Set us up to start enumerating from the very beginning again
            _endReached = false;
            _enumerator = null;
            _currentMode = CurrentEnumeratorMode.None;
        }

        void IEnumerator.Reset()
        {
            Reset();
        }

        public void Dispose()   // IEnumerator<Principal> inherits from IDisposable
        {
            _disposed = true;
        }

        //
        // Internal constructors
        //
        internal PrincipalCollectionEnumerator(
                                    ResultSet resultSet,
                                    PrincipalCollection memberCollection,
                                    List<Principal> removedValuesCompleted,
                                    List<Principal> removedValuesPending,
                                    List<Principal> insertedValuesCompleted,
                                    List<Principal> insertedValuesPending
                                    )
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalCollectionEnumerator", "Ctor");

            Debug.Assert(resultSet != null);

            _resultSet = resultSet;
            _memberCollection = memberCollection;
            _removedValuesCompleted = removedValuesCompleted;
            _removedValuesPending = removedValuesPending;
            _insertedValuesCompleted = insertedValuesCompleted;
            _insertedValuesPending = insertedValuesPending;
        }

        //
        // Private implementation
        //

        private Principal _current;

        // Remember: these are references to objects held by the PrincipalCollection class from which we came.
        // We don't own these, and shouldn't Dispose the ResultSet.
        //
        // SYNCHRONIZATION
        //   Access to:
        //      resultSet
        //   must be synchronized, since multiple enumerators could be iterating over us at once.
        //   Synchronize by locking on resultSet.

        private ResultSet _resultSet;
        private List<Principal> _insertedValuesPending;
        private List<Principal> _insertedValuesCompleted;
        private List<Principal> _removedValuesPending;
        private List<Principal> _removedValuesCompleted;

        private bool _endReached = false;    // true if there are no results left to iterate over

        private IEnumerator<Principal> _enumerator = null;   // The insertedValues{Completed,Pending} enumerator, used by MoveNext

        private enum CurrentEnumeratorMode          // The set of values that MoveNext is currently iterating over
        {
            None,
            ResultSet,
            InsertedValuesCompleted,
            InsertedValuesPending
        }

        private CurrentEnumeratorMode _currentMode = CurrentEnumeratorMode.None;

        // To support IDisposable
        private bool _disposed = false;

        private void CheckDisposed()
        {
            if (_disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "PrincipalCollectionEnumerator", "CheckDisposed: accessing disposed object");
                throw new ObjectDisposedException("PrincipalCollectionEnumerator");
            }
        }

        // When this enumerator was constructed, to detect changes made to the PrincipalCollection after it was constructed
        private DateTime _creationTime = DateTime.UtcNow;

        private PrincipalCollection _memberCollection = null;

        private void CheckChanged()
        {
            // Make sure the app hasn't changed our underlying list
            if (_memberCollection.LastChange > _creationTime)
            {
                GlobalDebug.WriteLineIf(
                            GlobalDebug.Warn,
                            "PrincipalCollectionEnumerator",
                            "CheckChanged: has changed (last change={0}, creation={1})",
                            _memberCollection.LastChange,
                            _creationTime);

                throw new InvalidOperationException(SR.PrincipalCollectionEnumHasChanged);
            }
        }
    }
}

