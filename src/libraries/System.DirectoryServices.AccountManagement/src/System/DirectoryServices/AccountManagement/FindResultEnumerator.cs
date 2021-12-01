// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.DirectoryServices.AccountManagement
{
    internal sealed class FindResultEnumerator<T> : IEnumerator<T>, IEnumerator
    {
        //
        // Public properties
        //

        // Checks to make sure we're not before the start (beforeStart == true) or after
        // the end (endReached == true) of the FindResult<T> collection, then retrieves the current
        // principal from resultSet.  If T == typeof(Principal), calls resultSet.CurrentAsPrincipal.

        public T Current
        {
            get
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "FindResultEnumerator", "Entering Current, T={0}", typeof(T));

                CheckDisposed();

                if (_beforeStart == true || _endReached == true || _resultSet == null)
                {
                    // Either we're before the beginning or after the end of the collection.
                    GlobalDebug.WriteLineIf(
                                        GlobalDebug.Warn,
                                        "FindResultEnumerator",
                                        "Current: bad position, beforeStart={0}, endReached={1}, resultSet={2}",
                                        _beforeStart,
                                        _endReached,
                                        _resultSet);

                    throw new InvalidOperationException(SR.FindResultEnumInvalidPos);
                }

                Debug.Assert(typeof(T) == typeof(System.DirectoryServices.AccountManagement.Principal) || typeof(T).IsSubclassOf(typeof(System.DirectoryServices.AccountManagement.Principal)));
                return (T)_resultSet.CurrentAsPrincipal;
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

        // Calls resultSet.MoveNext() to advance to the next principal in the ResultSet.
        // Returns false when it reaches the end of the last ResultSet in resultSets, and sets endReached to true.
        public bool MoveNext()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "FindResultEnumerator", "Entering MoveNext, T={0}", typeof(T));

            CheckDisposed();

            // If we previously reached the end, nothing more to move on to
            if (_endReached)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "FindResultEnumerator", "MoveNext: end previously reached");
                return false;
            }

            // No ResultSet, so we've already reached the end
            if (_resultSet == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "FindResultEnumerator", "MoveNext: no resultSet");
                return false;
            }

            bool f;

            lock (_resultSet)
            {
                // If before the first ResultSet, move to the first ResultSet
                if (_beforeStart == true)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "FindResultEnumerator", "MoveNext: Moving to first resultSet");

                    _beforeStart = false;

                    // In case we  previously iterated over this ResultSet,
                    // and are now back to the start because our Reset() method was called.
                    // Or in case another instance of FindResultEnumerator previously iterated over this ResultSet.
                    _resultSet.Reset();
                }

                f = _resultSet.MoveNext();
            }

            // If f is false, we must have reached the end of resultSet.
            if (!f)
            {
                // we've reached the end
                _endReached = true;
            }

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "FindResultEnumerator", "MoveNext: returning {0}", f);
            return f;
        }

        bool IEnumerator.MoveNext()
        {
            return MoveNext();
        }

        // Repositions us to the beginning by setting beforeStart to true.  Also clears endReached
        // by setting it back to false;
        public void Reset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "FindResultEnumerator", "Entering Reset");

            CheckDisposed();

            _endReached = false;
            _beforeStart = true;
        }

        void IEnumerator.Reset()
        {
            Reset();
        }

        public void Dispose()
        {
            // We really don't have anything to Dispose, since our ResultSet is actually
            // owned by our parent FindResult<T>.  However, IEnumerable<T> requires us to implement
            // IDisposable.

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "FindResultEnumerator", "Dispose: disposing");

            _disposed = true;
        }

        //
        // Internal Constructors
        //

        // Constructs an enumerator to enumerate over the supplied of ResultSet
        // Note that resultSet can be null
        internal FindResultEnumerator(ResultSet resultSet)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "FindResultEnumerator", "Ctor");

            _resultSet = resultSet;
        }

        //
        // Private Implementation
        //

        //
        // SYNCHRONIZATION
        //   Access to:
        //      resultSet
        //   must be synchronized, since multiple enumerators could be iterating over us at once.
        //   Synchronize by locking on resultSet (if resultSet is non-null).

        // The ResultSet over which we're enumerating, passed to us from the FindResult<T>.
        // Note that there's conceptually one FindResultEnumerator per FindResult, but can be multiple
        // actual FindResultEnumerator objects per FindResult, so there's no risk
        // of multiple FindResultEnumerators "interfering" with each other by trying to enumerate
        // over the same ResultSet.
        //
        // Note that S.DS (based on code review and testing) and Sys.Storage (based on code review)
        // both seem fine with the "one enumerator per result set" model.
        private readonly ResultSet _resultSet;

        // if true, we're before the start of the ResultSet
        private bool _beforeStart = true;

        // if true, we've reached the end of the ResultSet
        private bool _endReached;

        // true if Dispose() has been called
        private bool _disposed;

        //
        private void CheckDisposed()
        {
            if (_disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "FindResultEnumerator", "CheckDisposed: accessing disposed object");
                throw new ObjectDisposedException("FindResultEnumerator");
            }
        }
    }
}
