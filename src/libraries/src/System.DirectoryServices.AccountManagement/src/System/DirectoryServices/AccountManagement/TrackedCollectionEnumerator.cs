// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace System.DirectoryServices.AccountManagement
{
    internal class TrackedCollectionEnumerator<T> : IEnumerator, IEnumerator<T>
    {
        //
        // Public properties
        //

        public T Current
        {
            get
            {
                CheckDisposed();

                // Since MoveNext() saved off the current value for us, this is largely trivial.

                if (_endReached == true || _enumerator == null)
                {
                    // Either we're at the end or before the beginning
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "TrackedCollectionEnumerator", "Current: bad position, endReached={0}", _endReached);
                    throw new InvalidOperationException(SR.TrackedCollectionEnumInvalidPos);
                }

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
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollectionEnumerator", "Entering MoveNext");

            CheckDisposed();
            CheckChanged();

            if (_endReached)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollectionEnumerator", "MoveNext: endReached");
                return false;
            }

            if (_enumerator == null)
            {
                // Must be at the very beginning
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollectionEnumerator", "MoveNext: at beginning");

                _enumerator = ((IEnumerable)_combinedValues).GetEnumerator();
                Debug.Assert(_enumerator != null);
            }

            bool gotNextValue = _enumerator.MoveNext();

            // If we got the next value,
            // save it off so that Current can later return it.
            if (gotNextValue)
            {
                // Have to handle differently, since inserted values are just a T, while
                // original value are a Pair<T,T>, where Pair.Right is the current value
                TrackedCollection<T>.ValueEl el = (TrackedCollection<T>.ValueEl)_enumerator.Current;

                if (el.isInserted)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollectionEnumerator", "MoveNext: current ({0}) is inserted", _current);
                    _current = el.insertedValue;
                }
                else
                {
                    _current = el.originalValue.Right;
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollectionEnumerator", "MoveNext: current ({0}) is original", _current);
                }
            }
            else
            {
                // Nothing more to enumerate
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollectionEnumerator", "MoveNext: nothing more to enumerate");

                _endReached = true;
            }

            return gotNextValue;
        }

        bool IEnumerator.MoveNext()
        {
            return MoveNext();
        }

        public void Reset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollectionEnumerator", "Reset");

            CheckDisposed();
            CheckChanged();

            _endReached = false;
            _enumerator = null;
        }

        void IEnumerator.Reset()
        {
            Reset();
        }

        public void Dispose()
        {
            _disposed = true;
        }

        //
        // Internal constructors
        //
        internal TrackedCollectionEnumerator(string outerClassName, TrackedCollection<T> trackedCollection, List<TrackedCollection<T>.ValueEl> combinedValues)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollectionEnumerator", "Ctor");

            _outerClassName = outerClassName;
            _trackedCollection = trackedCollection;
            _combinedValues = combinedValues;
        }

        //
        // Private implementation
        //

        // Have we been disposed?
        private bool _disposed = false;

        //  The name of our outer class. Used when throwing an ObjectDisposedException.
        private string _outerClassName;

        private List<TrackedCollection<T>.ValueEl> _combinedValues = null;

        // The value we're currently positioned at
        private T _current;

        // The enumerator for our inner list, combinedValues.
        private IEnumerator _enumerator = null;

        // True when we reach the end of combinedValues (no more values to enumerate in the TrackedCollection)
        private bool _endReached = false;

        // When this enumerator was constructed, to detect changes made to the TrackedCollection after it was constructed
        private DateTime _creationTime = DateTime.UtcNow;
        private TrackedCollection<T> _trackedCollection = null;

        private void CheckDisposed()
        {
            if (_disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "TrackedCollectionEnumerator", "CheckDisposed: accessing disposed object");
                throw new ObjectDisposedException(_outerClassName);
            }
        }

        private void CheckChanged()
        {
            // Make sure the app hasn't changed our underlying list
            if (_trackedCollection.LastChange > _creationTime)
            {
                GlobalDebug.WriteLineIf(
                            GlobalDebug.Warn,
                            "TrackedCollectionEnumerator",
                            "CheckChanged: has changed (last change={0}, creation={1})",
                            _trackedCollection.LastChange,
                            _creationTime);

                throw new InvalidOperationException(SR.TrackedCollectionEnumHasChanged);
            }
        }
    }
}

