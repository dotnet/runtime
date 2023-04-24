// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Caching.Resources;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Diagnostics;

// Every member of this class is thread-safe.
//
// Derived classes begin monitoring during construction, so that a user can know if the
// dependency changed any time after construction.  For example, suppose we have a
// FileChangeMonitor class that derives from ChangeMonitor.  A user might create an instance
// of FileChangeMonitor for an XML file, and then read the file to populate an object representation.
// The user would then cache the object with the FileChangeMonitor.  The user could optionally check the
// HasChanged property of the FileChangeMonitor, to see if the XML file changed while the object
// was being populated, and if it had changed, they could call Dispose and start over, without
// inserting the item into the cache.  However, in a multi-threaded environment, for cleaner, easier
// to maintain code, it's usually appropriate to just insert without checking HasChanged, since the
// cache implementer will handle this for you, and the next thread to attempt to get the object
// will recreate and insert it.
//
// The following contract must be followed by derived classes, cache implementers, and users of the
// derived class:
//
// 1. The constructor of a derived class must set UniqueId, begin monitoring for dependency
//    changes, and call InitializationComplete before returning.  If a dependency changes
//    before initialization is complete, for example, if a dependent cache key is not found
//    in the cache, the constructor must invoke OnChanged.  The constructor can only call
//    Dispose after InitializationComplete is called, because Dispose will throw
//    InvalidOperationException if initialization is not complete.
// 2. Once constructed, the user must either insert the ChangeMonitor into an ObjectCache, or
//    if they're not going to use it, they must call Dispose.
// 3. Once inserted into an ObjectCache, the ObjectCache implementation must ensure that the
//    ChangeMonitor is eventually disposed.  Even if the insert is invalid, and results in an
//    exception being thrown, the ObjectCache implementation must call Dispose.  If this we're not
//    a requirement, users of the ChangeMonitor would need exception handling around each insert
//    into the cache that carefully ensures the dependency is disposed.  While this would work, we
//    think it is better to put this burden on the ObjectCache implementer, since users are far more
//    numerous than cache implementers.
// 4. After the ChangeMonitor is inserted into a cache, the ObjectCache implementer must call
//    NotifyOnChanged, passing in an OnChangedCallback.  NotifyOnChanged can only be called once,
//    and will throw InvalidOperationException on subsequent calls.  If the dependency has already
//    changed, the OnChangedCallback will be called when NotifyOnChanged is called.  Otherwise, the
//    OnChangedCallback will be called exactly once, when OnChanged is invoked or when Dispose
//    is invoked, which ever happens first.
// 5. The OnChangedCallback provided by the cache implementer should remove the cache entry, and specify
//    a reason of CacheEntryRemovedReason.DependencyChanged.  Care should be taken to remove the specific
//    entry having this dependency, and not it's replacement, which will have the same key.
// 6. In general, it is okay for OnChanged to be called at any time.  If OnChanged is called before
//    NotifyOnChanged is called, the "state" from the original call to OnChanged will be saved, and the
//    callback to NotifyOnChange will be called immediately when NotifyOnChanged is invoked.
// 7. A derived class must implement Dispose(bool disposing) to release all managed and unmanaged
//    resources when "disposing" is true.  Dispose(true) is only called once, when the instance is
//    disposed.  The derived class must not call Dispose(true) directly--it should only be called by
//    the ChangeMonitor class, when disposed.  Although a derived class could implement a finalizer and
//    invoke Dispose(false), this is generally not necessary.  Dependency monitoring is typically performed
//    by a service that maintains a reference to the ChangeMonitor, preventing it from being garbage collected,
//    and making finalizers useless.  To help prevent leaks, when a dependency changes, OnChanged disposes
//    the ChangeMonitor, unless initialization has not yet completed.
// 8. Dispose() must be called, and is designed to be called, in one of the following three ways:
//    - The user must call Dispose() if they decide not to insert the ChangeMonitor into a cache.  Otherwise,
//      the ChangeMonitor will continue monitoring for changes and be unavailable for garbage collection.
//    - The cache implementor is responsible for calling Dispose() once an attempt is made to insert it.
//      Even if the insert throws, the cache implementor must dispose the dependency.
//      Even if the entry is removed, the cache implementor must dispose the dependency.
//    - The OnChanged method will automatically call Dispose if initialization is complete.  Otherwise, when
//      the derived class' constructor calls InitializationComplete, the instance will be automatically disposed.
//
//    Before inserted into the cache, the user must ensure the dependency is disposed.  Once inserted into the
//    cache, the cache implementer must ensure that Dispose is called, even if the insert fails.  After being inserted
//    into a cache, the user should not dispose the dependency.  When Dispose is called, it is treated as if the dependency
//    changed, and OnChanged is automatically invoked.
// 9. HasChanged will be true after OnChanged is called by the derived class, regardless of whether an OnChangedCallback has been set
//    by a call to NotifyOnChanged.

namespace System.Runtime.Caching
{
    public abstract class ChangeMonitor : IDisposable
    {
        private const int INITIALIZED = 0x01; // initialization complete
        private const int CHANGED = 0x02; // dependency changed
        private const int INVOKED = 0x04; // OnChangedCallback has been invoked
        private const int DISPOSED = 0x08; // Dispose(true) called, or about to be called
        private static readonly object s_NOT_SET = new object();

        private SafeBitVector32 _flags;
        private OnChangedCallback _onChangedCallback;
        private object _onChangedState = s_NOT_SET;

        // The helper routines (OnChangedHelper and DisposeHelper) are used to prevent
        // an infinite loop, where Dispose calls OnChanged and OnChanged calls Dispose.
        private void DisposeHelper()
        {
            // if not initialized, return without doing anything.
            if (_flags[INITIALIZED])
            {
                if (_flags.ChangeValue(DISPOSED, true))
                {
                    Dispose(true);
                    GC.SuppressFinalize(this);
                }
            }
        }

        // The helper routines (OnChangedHelper and DisposeHelper) are used to prevent
        // an infinite loop, where Dispose calls OnChanged and OnChanged calls Dispose.
        private void OnChangedHelper(object state)
        {
            _flags[CHANGED] = true;

            // the callback is only invoked once, after NotifyOnChanged is called, so
            // remember "state" on the first call and use it when invoking the callback
            Interlocked.CompareExchange(ref _onChangedState, state, s_NOT_SET);

            OnChangedCallback onChangedCallback = _onChangedCallback;
            if (onChangedCallback != null)
            {
                // only invoke the callback once
                if (_flags.ChangeValue(INVOKED, true))
                {
                    onChangedCallback(_onChangedState);
                }
            }
        }

        //
        // protected members
        //

        // Derived classes must implement this.  When "disposing" is true,
        // all managed and unmanaged resources are disposed and any references to this
        // object are released so that the ChangeMonitor can be garbage collected.
        // It is guaranteed that ChangeMonitor.Dispose() will only invoke
        // Dispose(bool disposing) once.
        protected abstract void Dispose(bool disposing);

        // Derived classes must call InitializationComplete
        protected void InitializationComplete()
        {
            _flags[INITIALIZED] = true;

            // If the dependency has already changed, or someone tried to dispose us, then call Dispose now.
            Debug.Assert(_flags[INITIALIZED], "It is critical that INITIALIZED is set before CHANGED is checked below");
            if (_flags[CHANGED])
            {
                Dispose();
            }
        }

        // Derived classes call OnChanged when the dependency changes.  Optionally,
        // they may pass state which will be passed to the OnChangedCallback.  The
        // OnChangedCallback is only invoked once, and only after NotifyOnChanged is
        // called by the cache implementer.  OnChanged is also invoked when the instance
        // is disposed, but only has an affect if the callback has not already been invoked.
        protected void OnChanged(object state)
        {
            OnChangedHelper(state);

            // OnChanged will also invoke Dispose, but only after initialization is complete
            Debug.Assert(_flags[CHANGED], "It is critical that CHANGED is set before INITIALIZED is checked below.");
            if (_flags[INITIALIZED])
            {
                DisposeHelper();
            }
        }

        //
        // public members
        //

        // set to true when the dependency changes, specifically, when OnChanged is called.
        public bool HasChanged { get { return _flags[CHANGED]; } }

        // set to true when this instance is disposed, specifically, after
        // Dispose(bool disposing) is called by Dispose().
        public bool IsDisposed { get { return _flags[DISPOSED]; } }

        // a unique ID representing this ChangeMonitor, typically consisting of
        // the dependency names and last-modified times.
        public abstract string UniqueId { get; }

        // Dispose must be called to release the ChangeMonitor.  In order to
        // prevent derived classes from overriding Dispose, it is not an explicit
        // interface implementation.
        //
        // Before cache insertion, if the user decides not to do a cache insert, they
        // must call this to dispose the dependency; otherwise, the ChangeMonitor will
        // be referenced and unable to be garbage collected until the dependency changes.
        //
        // After cache insertion, the cache implementer must call this when the cache entry
        // is removed, for whatever reason.  Even if an exception is thrown during insert.
        //
        // After cache insertion, the user should not call Dispose.  However, since there's
        // no way to prevent this, doing so will invoke the OnChanged event handler, if it
        // hasn't already been invoked, and the cache entry will be notified as if the
        // dependency has changed.
        //
        // Dispose() will only invoke the Dispose(bool disposing) method of derived classes
        // once, the first time it is called.  Subsequent calls to Dispose() perform no
        // operation.  After Dispose is called, the IsDisposed property will be true.
        public void Dispose()
        {
            OnChangedHelper(null);

            // If not initialized, throw, so the derived class understands that it must call InitializeComplete before Dispose.
            Debug.Assert(_flags[CHANGED], "It is critical that CHANGED is set before INITIALIZED is checked below.");
            if (!_flags[INITIALIZED])
            {
                throw new InvalidOperationException(SR.Init_not_complete);
            }

            DisposeHelper();
        }

        // Cache implementers must call this to be notified of any dependency changes.
        // NotifyOnChanged can only be invoked once, and will throw InvalidOperationException
        // on subsequent calls.  The OnChangedCallback is guaranteed to be called exactly once.
        // It will be called when the dependency changes, or if it has already changed, it will
        // be called immediately (on the same thread??).
        public void NotifyOnChanged(OnChangedCallback onChangedCallback)
        {
            if (onChangedCallback is null)
            {
                throw new ArgumentNullException(nameof(onChangedCallback));
            }

            if (Interlocked.CompareExchange(ref _onChangedCallback, onChangedCallback, null) != null)
            {
                throw new InvalidOperationException(SR.Method_already_invoked);
            }

            // if it already changed, raise the event now.
            if (_flags[CHANGED])
            {
                OnChanged(null);
            }
        }
    }
}
