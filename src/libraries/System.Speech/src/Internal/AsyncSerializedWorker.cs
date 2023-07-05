// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Text;
using System.Threading;

namespace System.Speech.Internal
{
    internal interface IAsyncDispatch
    {
        void Post(object evt);
        void Post(object[] evt);
        void PostOperation(Delegate callback, params object[] parameters);
    }

    internal class AsyncSerializedWorker : IAsyncDispatch
    {
        #region Constructors

        internal AsyncSerializedWorker(WaitCallback defaultCallback, SynchronizationContext syncContext)
        {
            _syncContext = syncContext;
            _workerPostCallback = new SendOrPostCallback(WorkerProc);
            Initialize(defaultCallback);
        }

        private void Initialize(WaitCallback defaultCallback)
        {
            _queue = new Queue();
            _hasPendingPost = false;
            _workerCallback = new WaitCallback(WorkerProc);
            _defaultCallback = defaultCallback;
            _isAsyncMode = true;
            _isEnabled = true;
        }

        #endregion

        #region Public Methods

        public void Post(object evt)
        {
            AddItem(new AsyncWorkItem(DefaultCallback, evt));
        }

        public void Post(object[] evt)
        {
            int i;
            lock (_queue.SyncRoot)
            {
                if (Enabled)
                {
                    for (i = 0; i < evt.Length; i++)
                    {
                        AddItem(new AsyncWorkItem(DefaultCallback, evt[i]));
                    }
                }
            }
        }

        public void PostOperation(Delegate callback, params object[] parameters)
        {
            AddItem(new AsyncWorkItem(callback, parameters));
        }

        #endregion

        #region Internal Properties and Methods

        internal bool Enabled
        {
            get
            {
                lock (_queue.SyncRoot)
                {
                    return _isEnabled;
                }
            }
            set
            {
                lock (_queue.SyncRoot)
                {
                    _isEnabled = value;
                }
            }
        }

        internal void Purge()
        {
            lock (_queue.SyncRoot)
            {
                _queue.Clear();
            }
        }

        internal WaitCallback DefaultCallback
        {
            get
            {
                lock (_queue.SyncRoot)
                {
                    return _defaultCallback;
                }
            }
        }

        internal AsyncWorkItem NextWorkItem()
        {
            lock (_queue.SyncRoot)
            {
                if (_queue.Count == 0)
                {
                    return null;
                }
                else
                {
                    AsyncWorkItem workItem = (AsyncWorkItem)_queue.Dequeue();
                    if (_queue.Count == 0)
                    {
                        _hasPendingPost = false;
                    }
                    return workItem;
                }
            }
        }

        internal void ConsumeQueue()
        {
            AsyncWorkItem workItem;
            while (null != (workItem = NextWorkItem()))
            {
                workItem.Invoke();
            }
        }

        internal bool AsyncMode
        {
            get
            {
                lock (_queue.SyncRoot)
                {
                    return _isAsyncMode;
                }
            }
            set
            {
                bool notify = false;
                lock (_queue.SyncRoot)
                {
                    if (_isAsyncMode != value)
                    {
                        _isAsyncMode = value;
                        if (_queue.Count > 0)
                        {
                            notify = true;
                        }
                    }
                }

                // We need to resume the worker thread if there are post-events to process
                if (notify)
                {
                    OnWorkItemPending();
                }
            }
        }

        // event handler of this event should execute quickly and must not acquire any lock
        internal event WaitCallback WorkItemPending;

        #endregion
        #region Private/Protected Methods

        private void AddItem(AsyncWorkItem item)
        {
            bool processing = true;
            lock (_queue.SyncRoot)
            {
                if (Enabled)
                {
                    _queue.Enqueue(item);
                    if (!_hasPendingPost || !_isAsyncMode)
                    {
                        processing = false;
                        _hasPendingPost = true;
                    }
                }
            }

            if (!processing)
            {
                OnWorkItemPending();
            }
        }

        private void WorkerProc(object ignored)
        {
            AsyncWorkItem workItem;
            while (true)
            {
                lock (_queue.SyncRoot)
                {
                    if (_queue.Count > 0 && _isAsyncMode)
                    {
                        workItem = (AsyncWorkItem)_queue.Dequeue();
                    }
                    else
                    {
                        if (_queue.Count == 0)
                        {
                            _hasPendingPost = false;
                        }
                        break;
                    }
                }

                workItem.Invoke();
            }
        }

        private void OnWorkItemPending()
        {
            // No need to lock here
            if (_hasPendingPost)
            {
                if (AsyncMode)
                {
                    if (_syncContext == null)
                    {
                        ThreadPool.QueueUserWorkItem(_workerCallback, null);
                    }
                    else
                    {
                        _syncContext.Post(_workerPostCallback, null);
                    }
                }
                else
                {
                    WorkItemPending?.Invoke(null);
                }
            }
        }

        #endregion

        #region Private Fields

        private SynchronizationContext _syncContext;
        private SendOrPostCallback _workerPostCallback;

        private Queue _queue;
        private bool _hasPendingPost;
        private bool _isAsyncMode;
        private WaitCallback _workerCallback;
        private WaitCallback _defaultCallback;
        private bool _isEnabled;

        #endregion
    }

    internal class AsyncWorkItem
    {
        internal AsyncWorkItem(Delegate dynamicCallback, params object[] postData)
        {
            _dynamicCallback = dynamicCallback;
            _postData = postData;
        }

        internal void Invoke()
        {
            _dynamicCallback?.DynamicInvoke(_postData);
        }

        private Delegate _dynamicCallback;
        private object[] _postData;
    }
}
