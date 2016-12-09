/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    TrackedCollectionEnumerator.cs

Abstract:

    Implements the TrackedCollectionEnumerator<T> class.

History:

    10-May-2004    MattRim     Created

--*/

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace System.DirectoryServices.AccountManagement
{
    class TrackedCollectionEnumerator<T> : IEnumerator, IEnumerator<T>
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

                if (this.endReached == true || this.enumerator == null)
                {
                    // Either we're at the end or before the beginning
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "TrackedCollectionEnumerator", "Current: bad position, endReached={0}", this.endReached);                                 
                    throw new InvalidOperationException(StringResources.TrackedCollectionEnumInvalidPos);
                }
                
                return this.current;
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

            if (this.endReached)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollectionEnumerator", "MoveNext: endReached");                             
                return false;
            }

            if (this.enumerator == null)
            {
                // Must be at the very beginning
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollectionEnumerator", "MoveNext: at beginning");                             

                this.enumerator = ((IEnumerable)this.combinedValues).GetEnumerator();
                Debug.Assert(this.enumerator != null);
            }

            bool gotNextValue = this.enumerator.MoveNext();

            // If we got the next value,
            // save it off so that Current can later return it.
            if (gotNextValue)
            {
                // Have to handle differently, since inserted values are just a T, while
                // original value are a Pair<T,T>, where Pair.Right is the current value
                TrackedCollection<T>.ValueEl el = (TrackedCollection<T>.ValueEl) this.enumerator.Current;
                
                if (el.isInserted)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollectionEnumerator", "MoveNext: current ({0}) is inserted", this.current);                                             
                    this.current = el.insertedValue;
                }
                else
                {
                    this.current = el.originalValue.Right;
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollectionEnumerator", "MoveNext: current ({0}) is original", this.current);                    
                }
            }
            else
            {
                // Nothing more to enumerate
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollectionEnumerator", "MoveNext: nothing more to enumerate");                             
                
                this.endReached = true;
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
            
            this.endReached = false;
            this.enumerator = null;
        }

        void IEnumerator.Reset()
        {
            Reset();
        }

        public void Dispose()
        {
            this.disposed = true;
        }


        //
        // Internal constructors
        //
        internal TrackedCollectionEnumerator(string outerClassName, TrackedCollection<T> trackedCollection, List<TrackedCollection<T>.ValueEl> combinedValues)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollectionEnumerator", "Ctor");            
        
            this.outerClassName = outerClassName;
            this.trackedCollection = trackedCollection;
            this.combinedValues = combinedValues;
        }


        //
        // Private implementation
        //

        // Have we been disposed?
        bool disposed = false;

        //  The name of our outer class. Used when throwing an ObjectDisposedException.
        string outerClassName;

        List<TrackedCollection<T>.ValueEl> combinedValues = null;

        // The value we're currently positioned at
        T current;

        // The enumerator for our inner list, combinedValues.
        IEnumerator enumerator = null;

        // True when we reach the end of combinedValues (no more values to enumerate in the TrackedCollection)
        bool endReached = false;

        // When this enumerator was constructed, to detect changes made to the TrackedCollection after it was constructed
        DateTime creationTime = DateTime.UtcNow;
        TrackedCollection<T> trackedCollection = null;

        void CheckDisposed()
        {
            if (this.disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "TrackedCollectionEnumerator", "CheckDisposed: accessing disposed object");             
                throw new ObjectDisposedException(this.outerClassName);
            }
        }

        void CheckChanged()
        {
            // Make sure the app hasn't changed our underlying list
            if (this.trackedCollection.LastChange > this.creationTime)
            {
                GlobalDebug.WriteLineIf(
                            GlobalDebug.Warn,
                            "TrackedCollectionEnumerator", 
                            "CheckChanged: has changed (last change={0}, creation={1})",
                            this.trackedCollection.LastChange,
                            this.creationTime);
                            
                throw new InvalidOperationException(StringResources.TrackedCollectionEnumHasChanged);
            }
        }

    }
}

