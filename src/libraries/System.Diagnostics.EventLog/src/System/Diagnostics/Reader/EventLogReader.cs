// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// This public class is used for reading event records from event log.
    /// </summary>
    public class EventLogReader : IDisposable
    {
        private readonly EventLogQuery _eventQuery;

        private int _batchSize;

        //
        // access to the data member reference is safe, while
        // invoking methods on it is marked SecurityCritical as appropriate.
        //
        private readonly EventLogHandle _handle;

        /// <summary>
        /// events buffer holds batched event (handles).
        /// </summary>
        private IntPtr[] _eventsBuffer;
        /// <summary>
        /// The current index where the function GetNextEvent is (inside the eventsBuffer).
        /// </summary>
        private int _currentIndex;
        /// <summary>
        /// The number of events read from the batch into the eventsBuffer
        /// </summary>
        private int _eventCount;

        /// <summary>
        /// When the reader finishes (will always return only ERROR_NO_MORE_ITEMS).
        /// For subscription, this means we need to wait for next event.
        /// </summary>
        private bool _isEof;

        /// <summary>
        /// Maintains cached display / metadata information returned from
        /// EventRecords that were obtained from this reader.
        /// </summary>
        private readonly ProviderMetadataCachedInformation _cachedMetadataInformation;

        public EventLogReader(string path)
            : this(new EventLogQuery(path, PathType.LogName), null)
        {
        }

        public EventLogReader(string path, PathType pathType)
            : this(new EventLogQuery(path, pathType), null)
        {
        }

        public EventLogReader(EventLogQuery eventQuery)
            : this(eventQuery, null)
        {
        }

        public EventLogReader(EventLogQuery eventQuery, EventBookmark bookmark)
        {
            ArgumentNullException.ThrowIfNull(eventQuery);

            string logfile = null;
            if (eventQuery.ThePathType == PathType.FilePath)
                logfile = eventQuery.Path;

            _cachedMetadataInformation = new ProviderMetadataCachedInformation(eventQuery.Session, logfile, 50);

            // Explicit data
            _eventQuery = eventQuery;

            // Implicit
            _batchSize = 64;
            _eventsBuffer = new IntPtr[_batchSize];

            //
            // compute the flag.
            //
            int flag = 0;

            if (_eventQuery.ThePathType == PathType.LogName)
                flag |= (int)UnsafeNativeMethods.EvtQueryFlags.EvtQueryChannelPath;
            else
                flag |= (int)UnsafeNativeMethods.EvtQueryFlags.EvtQueryFilePath;

            if (_eventQuery.ReverseDirection)
                flag |= (int)UnsafeNativeMethods.EvtQueryFlags.EvtQueryReverseDirection;

            if (_eventQuery.TolerateQueryErrors)
                flag |= (int)UnsafeNativeMethods.EvtQueryFlags.EvtQueryTolerateQueryErrors;

            _handle = NativeWrapper.EvtQuery(_eventQuery.Session.Handle,
                _eventQuery.Path, _eventQuery.Query,
                flag);

            EventLogHandle bookmarkHandle = EventLogRecord.GetBookmarkHandleFromBookmark(bookmark);

            if (!bookmarkHandle.IsInvalid)
            {
                using (bookmarkHandle)
                {
                    NativeWrapper.EvtSeek(_handle, 1, bookmarkHandle, 0, UnsafeNativeMethods.EvtSeekFlags.EvtSeekRelativeToBookmark);
                }
            }
        }

        public int BatchSize
        {
            get
            {
                return _batchSize;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _batchSize = value;
            }
        }

        private bool GetNextBatch(TimeSpan ts)
        {
            int timeout = -1;
            if (ts != TimeSpan.MaxValue)
                timeout = (int)ts.TotalMilliseconds;

            // batchSize was changed by user, reallocate buffer.
            if (_batchSize != _eventsBuffer.Length)
                _eventsBuffer = new IntPtr[_batchSize];

            int newEventCount = 0;
            bool results = NativeWrapper.EvtNext(_handle, _batchSize, _eventsBuffer, timeout, 0, ref newEventCount);

            if (!results)
            {
                _eventCount = 0;
                _currentIndex = 0;
                return false; // No more events in the result set
            }

            _currentIndex = 0;
            _eventCount = newEventCount;
            return true;
        }

        public EventRecord ReadEvent()
        {
            return ReadEvent(TimeSpan.MaxValue);
        }

        public EventRecord ReadEvent(TimeSpan timeout)
        {
            if (_isEof)
                throw new InvalidOperationException();

            if (_currentIndex >= _eventCount)
            {
                // buffer is empty, get next batch.
                GetNextBatch(timeout);

                if (_currentIndex >= _eventCount)
                {
                    _isEof = true;
                    return null;
                }
            }

            EventLogRecord eventInstance = new EventLogRecord(new EventLogHandle(_eventsBuffer[_currentIndex], true), _eventQuery.Session, _cachedMetadataInformation);
            _currentIndex++;
            return eventInstance;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            while (_currentIndex < _eventCount)
            {
                NativeWrapper.EvtClose(_eventsBuffer[_currentIndex]);
                _currentIndex++;
            }

            if (_handle != null && !_handle.IsInvalid)
                _handle.Dispose();
        }

        internal void SeekReset()
        {
            //
            // Close all unread event handles in the buffer
            //
            while (_currentIndex < _eventCount)
            {
                NativeWrapper.EvtClose(_eventsBuffer[_currentIndex]);
                _currentIndex++;
            }

            // Reset the indexes used by Next
            _currentIndex = 0;
            _eventCount = 0;
            _isEof = false;
        }

        internal void SeekCommon(long offset)
        {
            //
            // modify offset that we're going to send to service to account for the
            // fact that we've already read some events in our buffer that the user
            // hasn't seen yet.
            //
            offset -= (_eventCount - _currentIndex);

            SeekReset();

            NativeWrapper.EvtSeek(_handle, offset, EventLogHandle.Zero, 0, UnsafeNativeMethods.EvtSeekFlags.EvtSeekRelativeToCurrent);
        }

        public void Seek(EventBookmark bookmark)
        {
            Seek(bookmark, 0);
        }

        public void Seek(EventBookmark bookmark, long offset)
        {
            ArgumentNullException.ThrowIfNull(bookmark);

            SeekReset();
            using (EventLogHandle bookmarkHandle = EventLogRecord.GetBookmarkHandleFromBookmark(bookmark))
            {
                NativeWrapper.EvtSeek(_handle, offset, bookmarkHandle, 0, UnsafeNativeMethods.EvtSeekFlags.EvtSeekRelativeToBookmark);
            }
        }

        public void Seek(SeekOrigin origin, long offset)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:

                    SeekReset();
                    NativeWrapper.EvtSeek(_handle, offset, EventLogHandle.Zero, 0, UnsafeNativeMethods.EvtSeekFlags.EvtSeekRelativeToFirst);
                    return;

                case SeekOrigin.End:

                    SeekReset();
                    NativeWrapper.EvtSeek(_handle, offset, EventLogHandle.Zero, 0, UnsafeNativeMethods.EvtSeekFlags.EvtSeekRelativeToLast);
                    return;

                case SeekOrigin.Current:
                    if (offset >= 0)
                    {
                        // We can reuse elements in the batch.
                        if (_currentIndex + offset < _eventCount)
                        {
                            //
                            // We don't call Seek here, we can reposition within the batch.
                            //

                            // Close all event handles between [currentIndex, currentIndex + offset)
                            int index = _currentIndex;
                            while (index < _currentIndex + offset)
                            {
                                NativeWrapper.EvtClose(_eventsBuffer[index]);
                                index++;
                            }

                            _currentIndex = (int)(_currentIndex + offset);
                            // Leave the eventCount unchanged
                            // Leave the same Eof
                        }
                        else
                        {
                            SeekCommon(offset);
                        }
                    }
                    else
                    {
                        SeekCommon(offset);
                    }
                    return;
            }
        }

        public void CancelReading()
        {
            NativeWrapper.EvtCancel(_handle);
        }

        public IList<EventLogStatus> LogStatus
        {
            get
            {
                EventLogHandle queryHandle = _handle;

                if (queryHandle.IsInvalid)
                    throw new InvalidOperationException();

                string[] channelNames = (string[])NativeWrapper.EvtGetQueryInfo(queryHandle, UnsafeNativeMethods.EvtQueryPropertyId.EvtQueryNames);
                int[] errorStatuses = (int[])NativeWrapper.EvtGetQueryInfo(queryHandle, UnsafeNativeMethods.EvtQueryPropertyId.EvtQueryStatuses);

                if (channelNames.Length != errorStatuses.Length)
                    throw new InvalidOperationException();

                var list = new List<EventLogStatus>(channelNames.Length);
                for (int i = 0; i < channelNames.Length; i++)
                {
                    EventLogStatus cs = new EventLogStatus(channelNames[i], errorStatuses[i]);
                    list.Add(cs);
                }
                return list.AsReadOnly();
            }
        }
    }
}
