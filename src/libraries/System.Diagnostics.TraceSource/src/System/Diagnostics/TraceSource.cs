// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
    public class TraceSource
    {
        private static readonly List<WeakReference<TraceSource>> s_tracesources = new List<WeakReference<TraceSource>>();
        private static int s_LastCollectionCount;

        private volatile SourceSwitch? _internalSwitch;
        private TraceListenerCollection? _listeners;
        private readonly SourceLevels _switchLevel;
        private readonly string _sourceName;
        private StringDictionary? _attributes;

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
            var tracesources = s_tracesources;
            lock (tracesources)
            {
                PruneCachedTraceSources(tracesources);
                tracesources.Add(new WeakReference<TraceSource>(this));
            }
        }

        private static void PruneCachedTraceSources(List<WeakReference<TraceSource>> tracesources)
        {
            if (s_LastCollectionCount != GC.CollectionCount(2))
            {
                List<WeakReference<TraceSource>> buffer = new List<WeakReference<TraceSource>>(tracesources.Count);
                for (int i = 0; i < tracesources.Count; i++)
                {
                    if (tracesources[i].TryGetTarget(out _))
                    {
                        buffer.Add(tracesources[i]);
                    }
                }
                if (buffer.Count < tracesources.Count)
                {
                    tracesources.Clear();
                    tracesources.AddRange(buffer);
                    tracesources.TrimExcess();
                }
                s_LastCollectionCount = GC.CollectionCount(2);
            }
        }

        private SourceSwitch Initialize()
        {
            return _internalSwitch ?? Init();

            SourceSwitch Init()
            {
                lock (this)
                {
                    SourceSwitch? internalSwitch = _internalSwitch;
                    if (internalSwitch is null)
                    {
                        internalSwitch = new SourceSwitch(_sourceName, _switchLevel.ToString());
                        _listeners = new TraceListenerCollection { new DefaultTraceListener() };
                        _internalSwitch = internalSwitch;
                    }
                    return internalSwitch;
                }
            }
        }

        public void Close()
        {
            // No need to call Initialize()
            var listeners = _listeners;
            if (listeners != null)
            {
                // Use global lock
                lock (TraceInternal.critSec)
                {
                    var list = listeners.List;
                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i].Close();
                    }
                }
            }
        }

        public void Flush()
        {
            // No need to call Initialize()
            var listeners = _listeners;
            if (listeners != null)
            {
                if (TraceInternal.UseGlobalLock)
                {
                    lock (TraceInternal.critSec)
                    {
                        var list = listeners.List;
                        for (int i = 0; i < list.Count; i++)
                        {
                            list[i].Flush();
                        }
                    }
                }
                else
                {
                    var list = listeners.List;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var listener = list[i];
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

        protected internal virtual string[]? GetSupportedAttributes() => null;

        internal static void RefreshAll()
        {
            var tracesources = s_tracesources;
            lock (tracesources)
            {
                PruneCachedTraceSources(tracesources);
                for (int i = 0; i < tracesources.Count; i++)
                {
                    if (tracesources[i].TryGetTarget(out TraceSource? tracesource))
                    {
                        tracesource.Initialize();
                    }
                }
            }
        }

        [Conditional("TRACE")]
        public void TraceEvent(TraceEventType eventType, int id)
        {
            if (Switch.ShouldTrace(eventType))
            {
                TraceEventCache manager = new TraceEventCache();

                if (TraceInternal.UseGlobalLock)
                {
                    // we lock on the same object that Trace does because we're writing to the same Listeners.
                    lock (TraceInternal.critSec)
                    {
                        var listeners = _listeners!.List;
                        for (int i = 0; i < listeners.Count; i++)
                        {
                            TraceListener listener = listeners[i];
                            listener.TraceEvent(manager, Name, eventType, id);
                            if (Trace.AutoFlush) listener.Flush();
                        }
                    }
                }
                else
                {
                    var listeners = _listeners!.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        TraceListener listener = listeners[i];
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
        public void TraceEvent(TraceEventType eventType, int id, string? message)
        {
            if (Switch.ShouldTrace(eventType))
            {
                TraceEventCache manager = new TraceEventCache();

                if (TraceInternal.UseGlobalLock)
                {
                    // we lock on the same object that Trace does because we're writing to the same Listeners.
                    lock (TraceInternal.critSec)
                    {
                        var listeners = _listeners!.List;
                        for (int i = 0; i < listeners.Count; i++)
                        {
                            TraceListener listener = listeners[i];
                            listener.TraceEvent(manager, Name, eventType, id, message);
                            if (Trace.AutoFlush) listener.Flush();
                        }
                    }
                }
                else
                {
                    var listeners = _listeners!.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        TraceListener listener = listeners[i];
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
        public void TraceEvent(TraceEventType eventType, int id, string? format, params object?[]? args)
        {
            if (Switch.ShouldTrace(eventType))
            {
                TraceEventCache manager = new TraceEventCache();

                if (TraceInternal.UseGlobalLock)
                {
                    // we lock on the same object that Trace does because we're writing to the same Listeners.
                    lock (TraceInternal.critSec)
                    {
                        var listeners = _listeners!.List;
                        for (int i = 0; i < listeners.Count; i++)
                        {
                            TraceListener listener = listeners[i];
                            listener.TraceEvent(manager, Name, eventType, id, format, args);
                            if (Trace.AutoFlush) listener.Flush();
                        }
                    }
                }
                else
                {
                    var listeners = _listeners!.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        TraceListener listener = listeners[i];
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
        public void TraceData(TraceEventType eventType, int id, object? data)
        {
            if (Switch.ShouldTrace(eventType))
            {
                TraceEventCache manager = new TraceEventCache();

                if (TraceInternal.UseGlobalLock)
                {
                    // we lock on the same object that Trace does because we're writing to the same Listeners.
                    lock (TraceInternal.critSec)
                    {
                        var listeners = _listeners!.List;
                        for (int i = 0; i < listeners.Count; i++)
                        {
                            TraceListener listener = listeners[i];
                            listener.TraceData(manager, Name, eventType, id, data);
                            if (Trace.AutoFlush) listener.Flush();
                        }
                    }
                }
                else
                {
                    var listeners = _listeners!.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        TraceListener listener = listeners[i];
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
        public void TraceData(TraceEventType eventType, int id, params object?[]? data)
        {
            if (Switch.ShouldTrace(eventType))
            {
                TraceEventCache manager = new TraceEventCache();

                if (TraceInternal.UseGlobalLock)
                {
                    // we lock on the same object that Trace does because we're writing to the same Listeners.
                    lock (TraceInternal.critSec)
                    {
                        var listeners = _listeners!.List;
                        for (int i = 0; i < listeners.Count; i++)
                        {
                            TraceListener listener = listeners[i];
                            listener.TraceData(manager, Name, eventType, id, data);
                            if (Trace.AutoFlush) listener.Flush();
                        }
                    }
                }
                else
                {
                    var listeners = _listeners!.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        TraceListener listener = listeners[i];
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
        public void TraceInformation(string? message) => TraceEvent(TraceEventType.Information, 0, message, null);

        [Conditional("TRACE")]
        public void TraceInformation(string? format, params object?[]? args) => TraceEvent(TraceEventType.Information, 0, format, args);

        [Conditional("TRACE")]
        public void TraceTransfer(int id, string? message, Guid relatedActivityId)
        {
            if (Switch.ShouldTrace(TraceEventType.Transfer))
            {
                TraceEventCache manager = new TraceEventCache();

                if (TraceInternal.UseGlobalLock)
                {
                    // we lock on the same object that Trace does because we're writing to the same Listeners.
                    lock (TraceInternal.critSec)
                    {
                        var listeners = _listeners!.List;
                        for (int i = 0; i < listeners.Count; i++)
                        {
                            TraceListener listener = listeners[i];
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
                    var listeners = _listeners!.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        TraceListener listener = listeners[i];

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

        public StringDictionary Attributes => _attributes ??= new StringDictionary();

        public string Name => _sourceName;

        public TraceListenerCollection Listeners
        {
            get
            {
                Initialize();

                return _listeners!;
            }
        }

        public SourceSwitch Switch
        {
            get => Initialize();
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
