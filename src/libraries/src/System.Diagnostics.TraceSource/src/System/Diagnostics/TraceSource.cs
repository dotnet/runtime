// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;

namespace System.Diagnostics
{
    public class TraceSource
    {
        private static List<WeakReference> s_tracesources = new List<WeakReference>();
        private static int s_LastCollectionCount;

        private volatile SourceSwitch _internalSwitch;
        private volatile TraceListenerCollection _listeners;
        private SourceLevels _switchLevel;
        private volatile string _sourceName;
        internal volatile bool _initCalled = false;   // Whether we've called Initialize already.
        private StringDictionary _attributes;

        public TraceSource(string name)
            : this(name, SourceLevels.Off)
        {
        }

        public TraceSource(string name, SourceLevels defaultLevel)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0)
                throw new ArgumentException(SR.Format(SR.InvalidNullEmptyArgument, nameof(name)), nameof(name));

            _sourceName = name;
            _switchLevel = defaultLevel;

            // Add a weakreference to this source and cleanup invalid references
            lock (s_tracesources)
            {
                _pruneCachedTraceSources();
                s_tracesources.Add(new WeakReference(this));
            }
        }

        private static void _pruneCachedTraceSources()
        {
            lock (s_tracesources)
            {
                if (s_LastCollectionCount != GC.CollectionCount(2))
                {
                    List<WeakReference> buffer = new List<WeakReference>(s_tracesources.Count);
                    for (int i = 0; i < s_tracesources.Count; i++)
                    {
                        TraceSource tracesource = ((TraceSource)s_tracesources[i].Target);
                        if (tracesource != null)
                        {
                            buffer.Add(s_tracesources[i]);
                        }
                    }
                    if (buffer.Count < s_tracesources.Count)
                    {
                        s_tracesources.Clear();
                        s_tracesources.AddRange(buffer);
                        s_tracesources.TrimExcess();
                    }
                    s_LastCollectionCount = GC.CollectionCount(2);
                }
            }
        }

        private void Initialize()
        {
            if (!_initCalled)
            {
                lock (this)
                {
                    if (_initCalled)
                        return;

                    NoConfigInit();

                    _initCalled = true;
                }
            }
        }

        private void NoConfigInit()
        {
            _internalSwitch = new SourceSwitch(_sourceName, _switchLevel.ToString());
            _listeners = new TraceListenerCollection();
            _listeners.Add(new DefaultTraceListener());
        }

        public void Close()
        {
            // No need to call Initialize()
            if (_listeners != null)
            {
                // Use global lock
                lock (TraceInternal.critSec)
                {
                    foreach (TraceListener listener in _listeners)
                    {
                        listener.Close();
                    }
                }
            }
        }

        public void Flush()
        {
            // No need to call Initialize()
            if (_listeners != null)
            {
                if (TraceInternal.UseGlobalLock)
                {
                    lock (TraceInternal.critSec)
                    {
                        foreach (TraceListener listener in _listeners)
                        {
                            listener.Flush();
                        }
                    }
                }
                else
                {
                    foreach (TraceListener listener in _listeners)
                    {
                        if (!listener.IsThreadSafe)
                        {
                            lock (listener)
                            {
                                listener.Flush();
                            }
                        }
                        else
                        {
                            listener.Flush();
                        }
                    }
                }
            }
        }

        protected internal virtual string[] GetSupportedAttributes() => null;

        internal static void RefreshAll()
        {
            lock (s_tracesources)
            {
                _pruneCachedTraceSources();
                for (int i = 0; i < s_tracesources.Count; i++)
                {
                    TraceSource tracesource = ((TraceSource)s_tracesources[i].Target);
                    if (tracesource != null)
                    {
                        tracesource.Refresh();
                    }
                }
            }
        }

        internal void Refresh()
        {
            if (!_initCalled)
            {
                Initialize();
                return;
            }
        }

        [Conditional("TRACE")]
        public void TraceEvent(TraceEventType eventType, int id)
        {
            Initialize();

            if (_internalSwitch.ShouldTrace(eventType) && _listeners != null)
            {
                TraceEventCache manager = new TraceEventCache();

                if (TraceInternal.UseGlobalLock)
                {
                    // we lock on the same object that Trace does because we're writing to the same Listeners.
                    lock (TraceInternal.critSec)
                    {
                        for (int i = 0; i < _listeners.Count; i++)
                        {
                            TraceListener listener = _listeners[i];
                            listener.TraceEvent(manager, Name, eventType, id);
                            if (Trace.AutoFlush) listener.Flush();
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _listeners.Count; i++)
                    {
                        TraceListener listener = _listeners[i];
                        if (!listener.IsThreadSafe)
                        {
                            lock (listener)
                            {
                                listener.TraceEvent(manager, Name, eventType, id);
                                if (Trace.AutoFlush) listener.Flush();
                            }
                        }
                        else
                        {
                            listener.TraceEvent(manager, Name, eventType, id);
                            if (Trace.AutoFlush) listener.Flush();
                        }
                    }
                }
            }
        }

        [Conditional("TRACE")]
        public void TraceEvent(TraceEventType eventType, int id, string message)
        {
            Initialize();

            if (_internalSwitch.ShouldTrace(eventType) && _listeners != null)
            {
                TraceEventCache manager = new TraceEventCache();

                if (TraceInternal.UseGlobalLock)
                {
                    // we lock on the same object that Trace does because we're writing to the same Listeners.
                    lock (TraceInternal.critSec)
                    {
                        for (int i = 0; i < _listeners.Count; i++)
                        {
                            TraceListener listener = _listeners[i];
                            listener.TraceEvent(manager, Name, eventType, id, message);
                            if (Trace.AutoFlush) listener.Flush();
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _listeners.Count; i++)
                    {
                        TraceListener listener = _listeners[i];
                        if (!listener.IsThreadSafe)
                        {
                            lock (listener)
                            {
                                listener.TraceEvent(manager, Name, eventType, id, message);
                                if (Trace.AutoFlush) listener.Flush();
                            }
                        }
                        else
                        {
                            listener.TraceEvent(manager, Name, eventType, id, message);
                            if (Trace.AutoFlush) listener.Flush();
                        }
                    }
                }
            }
        }

        [Conditional("TRACE")]
        public void TraceEvent(TraceEventType eventType, int id, string format, params object[] args)
        {
            Initialize();
            
            if (_internalSwitch.ShouldTrace(eventType) && _listeners != null)
            {
                TraceEventCache manager = new TraceEventCache();

                if (TraceInternal.UseGlobalLock)
                {
                    // we lock on the same object that Trace does because we're writing to the same Listeners.
                    lock (TraceInternal.critSec)
                    {
                        for (int i = 0; i < _listeners.Count; i++)
                        {
                            TraceListener listener = _listeners[i];
                            listener.TraceEvent(manager, Name, eventType, id, format, args);
                            if (Trace.AutoFlush) listener.Flush();
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _listeners.Count; i++)
                    {
                        TraceListener listener = _listeners[i];
                        if (!listener.IsThreadSafe)
                        {
                            lock (listener)
                            {
                                listener.TraceEvent(manager, Name, eventType, id, format, args);
                                if (Trace.AutoFlush) listener.Flush();
                            }
                        }
                        else
                        {
                            listener.TraceEvent(manager, Name, eventType, id, format, args);
                            if (Trace.AutoFlush) listener.Flush();
                        }
                    }
                }
            }
        }

        [Conditional("TRACE")]
        public void TraceData(TraceEventType eventType, int id, object data)
        {
            Initialize();

            if (_internalSwitch.ShouldTrace(eventType) && _listeners != null)
            {
                TraceEventCache manager = new TraceEventCache();

                if (TraceInternal.UseGlobalLock)
                {
                    // we lock on the same object that Trace does because we're writing to the same Listeners.
                    lock (TraceInternal.critSec)
                    {
                        for (int i = 0; i < _listeners.Count; i++)
                        {
                            TraceListener listener = _listeners[i];
                            listener.TraceData(manager, Name, eventType, id, data);
                            if (Trace.AutoFlush) listener.Flush();
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _listeners.Count; i++)
                    {
                        TraceListener listener = _listeners[i];
                        if (!listener.IsThreadSafe)
                        {
                            lock (listener)
                            {
                                listener.TraceData(manager, Name, eventType, id, data);
                                if (Trace.AutoFlush) listener.Flush();
                            }
                        }
                        else
                        {
                            listener.TraceData(manager, Name, eventType, id, data);
                            if (Trace.AutoFlush) listener.Flush();
                        }
                    }
                }
            }
        }

        [Conditional("TRACE")]
        public void TraceData(TraceEventType eventType, int id, params object[] data)
        {
            Initialize();

            if (_internalSwitch.ShouldTrace(eventType) && _listeners != null)
            {
                TraceEventCache manager = new TraceEventCache();

                if (TraceInternal.UseGlobalLock)
                {
                    // we lock on the same object that Trace does because we're writing to the same Listeners.
                    lock (TraceInternal.critSec)
                    {
                        for (int i = 0; i < _listeners.Count; i++)
                        {
                            TraceListener listener = _listeners[i];
                            listener.TraceData(manager, Name, eventType, id, data);
                            if (Trace.AutoFlush) listener.Flush();
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _listeners.Count; i++)
                    {
                        TraceListener listener = _listeners[i];
                        if (!listener.IsThreadSafe)
                        {
                            lock (listener)
                            {
                                listener.TraceData(manager, Name, eventType, id, data);
                                if (Trace.AutoFlush) listener.Flush();
                            }
                        }
                        else
                        {
                            listener.TraceData(manager, Name, eventType, id, data);
                            if (Trace.AutoFlush) listener.Flush();
                        }
                    }
                }
            }
        }

        [Conditional("TRACE")]
        public void TraceInformation(string message)
        { // eventType= TraceEventType.Info, id=0
            // No need to call Initialize()
            TraceEvent(TraceEventType.Information, 0, message, null);
        }

        [Conditional("TRACE")]
        public void TraceInformation(string format, params object[] args)
        {
            // No need to call Initialize()
            TraceEvent(TraceEventType.Information, 0, format, args);
        }

        [Conditional("TRACE")]
        public void TraceTransfer(int id, string message, Guid relatedActivityId)
        {
            // Ensure that config is loaded 
            Initialize();

            TraceEventCache manager = new TraceEventCache();

            if (_internalSwitch.ShouldTrace(TraceEventType.Transfer) && _listeners != null)
            {
                if (TraceInternal.UseGlobalLock)
                {
                    // we lock on the same object that Trace does because we're writing to the same Listeners.
                    lock (TraceInternal.critSec)
                    {
                        for (int i = 0; i < _listeners.Count; i++)
                        {
                            TraceListener listener = _listeners[i];
                            listener.TraceTransfer(manager, Name, id, message, relatedActivityId);
                            
                            if (Trace.AutoFlush)
                            {
                                listener.Flush();
                            }
                        }
                    }
                }
                else 
                {
                    for (int i = 0; i < _listeners.Count; i++)
                    {
                        TraceListener listener = _listeners[i];
                        
                        if (!listener.IsThreadSafe)
                        {
                            lock (listener)
                            {
                                listener.TraceTransfer(manager, Name, id, message, relatedActivityId);
                                if (Trace.AutoFlush)
                                {
                                    listener.Flush();
                                }
                            }
                        }
                        else 
                        {
                            listener.TraceTransfer(manager, Name, id, message, relatedActivityId);
                            if (Trace.AutoFlush)
                            {
                                listener.Flush();
                            }
                        }
                    }
                }
            }
        }

        public StringDictionary Attributes {
            get {
                // Ensure that config is loaded 
                Initialize();

                if (_attributes == null)
                    _attributes = new StringDictionary();

                return _attributes;
            }
        }

        public string Name
        {
            get
            {
                return _sourceName;
            }
        }

        public TraceListenerCollection Listeners
        {
            get
            {
                Initialize();

                return _listeners;
            }
        }

        public SourceSwitch Switch
        {
            // No need for security demand here. SourceSwitch.set_Level is protected already.
            get
            {
                Initialize();

                return _internalSwitch;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(Switch));

                Initialize();
                _internalSwitch = value;
            }
        }
    }
}
