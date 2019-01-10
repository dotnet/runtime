// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.Win32;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Used for subscribing to event record notifications from 
    /// event log. 
    /// </summary>
    public class EventLogWatcher : IDisposable
    {
        public event EventHandler<EventRecordWrittenEventArgs> EventRecordWritten;

        private EventLogQuery _eventQuery;
        private EventBookmark _bookmark;
        private bool _readExistingEvents;

        private EventLogHandle _handle;
        private IntPtr[] _eventsBuffer;
        private int _numEventsInBuffer;
        private bool _isSubscribing;
        private int _callbackThreadId;
        private AutoResetEvent _subscriptionWaitHandle;
        private AutoResetEvent _unregisterDoneHandle;
        private RegisteredWaitHandle _registeredWaitHandle;

        /// <summary>
        /// Maintains cached display / metadata information returned from 
        /// EventRecords that were obtained from this reader.
        /// </summary>
        private ProviderMetadataCachedInformation cachedMetadataInformation;
        private EventLogException asyncException;

        public EventLogWatcher(string path)
            : this(new EventLogQuery(path, PathType.LogName), null, false)
        {
        }

        public EventLogWatcher(EventLogQuery eventQuery)
            : this(eventQuery, null, false)
        {
        }

        public EventLogWatcher(EventLogQuery eventQuery, EventBookmark bookmark)
            : this(eventQuery, bookmark, false)
        {
        }

        public EventLogWatcher(EventLogQuery eventQuery, EventBookmark bookmark, bool readExistingEvents)
        {

            if (eventQuery == null)
            {
                throw new ArgumentNullException(nameof(eventQuery));
            }

            if (bookmark != null)
            {
                readExistingEvents = false;
            }

            // Explicit data
            _eventQuery = eventQuery;
            _readExistingEvents = readExistingEvents;

            if (_eventQuery.ReverseDirection)
            {
                throw new InvalidOperationException();
            }

            _eventsBuffer = new IntPtr[64];
            cachedMetadataInformation = new ProviderMetadataCachedInformation(eventQuery.Session, null, 50);
            _bookmark = bookmark;
        }

        public bool Enabled
        {
            get
            {
                return _isSubscribing;
            }
            set
            {
                if (value && !_isSubscribing)
                {
                    StartSubscribing();
                }
                else if (!value && _isSubscribing)
                {
                    StopSubscribing();
                }
            }
        }

        internal void StopSubscribing()
        {
            // C:\public\System.Diagnostics.Eventing\Microsoft\Win32\SafeHandles;

            // Need to set isSubscribing to false before waiting for completion of callback.
            _isSubscribing = false;

            if (_registeredWaitHandle != null)
            {

                _registeredWaitHandle.Unregister(_unregisterDoneHandle);

                if (_callbackThreadId != Thread.CurrentThread.ManagedThreadId)
                {
                    // Not calling Stop from within callback - wait for 
                    // Any outstanding callbacks to complete.
                    if (_unregisterDoneHandle != null)
                    {
                        _unregisterDoneHandle.WaitOne();
                    }
                }

                _registeredWaitHandle = null;
            }

            if (_unregisterDoneHandle != null)
            {
                _unregisterDoneHandle.Close();
                _unregisterDoneHandle = null;
            }

            if (_subscriptionWaitHandle != null)
            {
                _subscriptionWaitHandle.Close();
                _subscriptionWaitHandle = null;
            }

            for (int i = 0; i < _numEventsInBuffer; i++)
            {

                if (_eventsBuffer[i] != IntPtr.Zero)
                {
                    UnsafeNativeMethods.EvtClose(_eventsBuffer[i]);
                    _eventsBuffer[i] = IntPtr.Zero;
                }
            }

            _numEventsInBuffer = 0;

            if (_handle != null && !_handle.IsInvalid)
            {
                _handle.Dispose();
            }
        }

        internal void StartSubscribing()
        {
            if (_isSubscribing)
            {
                throw new InvalidOperationException();
            }

            int flag = 0;
            if (_bookmark != null)
            {
                flag |= (int)UnsafeNativeMethods.EvtSubscribeFlags.EvtSubscribeStartAfterBookmark;
            }
            else if (_readExistingEvents)
            {
                flag |= (int)UnsafeNativeMethods.EvtSubscribeFlags.EvtSubscribeStartAtOldestRecord;
            }
            else
            {
                flag |= (int)UnsafeNativeMethods.EvtSubscribeFlags.EvtSubscribeToFutureEvents;
            }

            if (_eventQuery.TolerateQueryErrors)
            {
                flag |= (int)UnsafeNativeMethods.EvtSubscribeFlags.EvtSubscribeTolerateQueryErrors;
            }

            // C:\public\System.Diagnostics.Eventing\Microsoft\Win32\SafeHandles;

            _callbackThreadId = -1;
            _unregisterDoneHandle = new AutoResetEvent(false);
            _subscriptionWaitHandle = new AutoResetEvent(false);

            EventLogHandle bookmarkHandle = EventLogRecord.GetBookmarkHandleFromBookmark(_bookmark);

            using (bookmarkHandle)
            {

                _handle = UnsafeNativeMethods.EvtSubscribe(_eventQuery.Session.Handle,
                    _subscriptionWaitHandle.SafeWaitHandle,
                    _eventQuery.Path,
                    _eventQuery.Query,
                    bookmarkHandle,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    flag);
            }

            _isSubscribing = true;

            RequestEvents();

            _registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                _subscriptionWaitHandle,
                new WaitOrTimerCallback(SubscribedEventsAvailableCallback),
                null,
                -1,
                false);
        }

        internal void SubscribedEventsAvailableCallback(object state, bool timedOut)
        {
            _callbackThreadId = Thread.CurrentThread.ManagedThreadId;
            try
            {
                RequestEvents();
            }
            finally
            {
                _callbackThreadId = -1;
            }
        }

        private void RequestEvents()
        {
            // C:\public\System.Diagnostics.Eventing\Microsoft\Win32\SafeHandles;

            asyncException = null;
            Debug.Assert(_numEventsInBuffer == 0);

            bool results = false;

            do
            {
                if (!_isSubscribing)
                {
                    break;
                }

                try
                {
                    results = NativeWrapper.EvtNext(_handle, _eventsBuffer.Length, _eventsBuffer, 0, 0, ref _numEventsInBuffer);

                    if (!results)
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    asyncException = new EventLogException();
                    asyncException.Data.Add("RealException", e);
                }

                HandleEventsRequestCompletion();

            } while (results);
        }

        private void IssueCallback(EventRecordWrittenEventArgs eventArgs)
        {
            if (EventRecordWritten != null)
            {
                EventRecordWritten(this, eventArgs);
            }
        }

        private void HandleEventsRequestCompletion()
        {
            if (asyncException != null)
            {
                EventRecordWrittenEventArgs args = new EventRecordWrittenEventArgs(asyncException.Data["RealException"] as Exception);
                IssueCallback(args);
            }

            for (int i = 0; i < _numEventsInBuffer; i++)
            {
                if (!_isSubscribing)
                {
                    break;
                }

                EventLogRecord record = new EventLogRecord(new EventLogHandle(_eventsBuffer[i], true), _eventQuery.Session, cachedMetadataInformation);
                EventRecordWrittenEventArgs args = new EventRecordWrittenEventArgs(record);
                _eventsBuffer[i] = IntPtr.Zero;  // user is responsible for calling Dispose().
                IssueCallback(args);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopSubscribing();
                return;
            }

            for (int i = 0; i < _numEventsInBuffer; i++)
            {

                if (_eventsBuffer[i] != IntPtr.Zero)
                {
                    NativeWrapper.EvtClose(_eventsBuffer[i]);
                    _eventsBuffer[i] = IntPtr.Zero;
                }
            }

            _numEventsInBuffer = 0;
        }
    }
}
