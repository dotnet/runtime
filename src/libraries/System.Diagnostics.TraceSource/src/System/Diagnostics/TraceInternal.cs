// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Diagnostics
{
    internal static class TraceInternal
    {
        private sealed class TraceProvider : DebugProvider
        {
#pragma warning disable CS8770 // Method lacks `[DoesNotReturn]` annotation to match overridden member.
            public override void Fail(string? message, string? detailMessage) => TraceInternal.Fail(message, detailMessage);
#pragma warning restore CS8770
            public override void OnIndentLevelChanged(int indentLevel)
            {
                lock (critSec)
                {
                    var listeners = Listeners.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        listeners[i].IndentLevel = indentLevel;
                    }
                }
            }

            public override void OnIndentSizeChanged(int indentSize)
            {
                lock (critSec)
                {
                    var listeners = Listeners.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        listeners[i].IndentSize = indentSize;
                    }
                }
            }
            public override void Write(string? message) => TraceInternal.Write(message);
            public override void WriteLine(string? message) => TraceInternal.WriteLine(message);
        }

        private static volatile string? s_appName;
        private static TraceListenerCollection? s_listeners;
        private static volatile bool s_autoFlush;
        private static volatile bool s_noGlobalLock;

        // this is internal so TraceSource can use it.  We want to lock on the same object because both TraceInternal and
        // TraceSource could be writing to the same listeners at the same time.
        internal static object critSec => typeof(TraceInternal);

        public static TraceListenerCollection Listeners => s_listeners ?? Init();

        private static TraceListenerCollection Init()
        {
            lock (critSec)
            {
                var listeners = s_listeners;
                if (listeners == null)
                {
                    // This is where we override default DebugProvider because we know
                    // for sure that we have some Listeners to write to.
                    Debug.SetProvider(new TraceProvider());
                    // In the absence of config support, the listeners by default add
                    // DefaultTraceListener to the listener collection.
                    listeners = new TraceListenerCollection(new DefaultTraceListener());
                    s_listeners = listeners;
                }
                return listeners;
            }
        }

        internal static string AppName => s_appName ??= Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;

        public static bool AutoFlush
        {
            get => s_autoFlush;
            set => s_autoFlush = value;
        }

        public static bool UseGlobalLock
        {
            get => !s_noGlobalLock;
            set => s_noGlobalLock = !value;
        }

        public static void Flush()
        {
            var listeners = s_listeners;
            if (listeners != null)
            {
                if (UseGlobalLock)
                {
                    lock (critSec)
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

        public static void Close()
        {
            var listeners = s_listeners;
            if (listeners != null)
            {
                // Use global lock
                lock (critSec)
                {
                    var list = listeners.List;
                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i].Close();
                    }
                }
            }
        }

        public static void Assert(bool condition)
        {
            if (condition) return;
            Fail(string.Empty);
        }

        public static void Assert(bool condition, string? message)
        {
            if (condition) return;
            Fail(message);
        }

        public static void Assert(bool condition, string? message, string? detailMessage)
        {
            if (condition) return;
            Fail(message, detailMessage);
        }

        public static void Fail(string? message)
        {
            if (UseGlobalLock)
            {
                lock (critSec)
                {
                    var listeners = Listeners.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        var listener = listeners[i];
                        listener.Fail(message);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
            else
            {
                var listeners = Listeners.List;
                for (int i = 0; i < listeners.Count; i++)
                {
                    var listener = listeners[i];
                    if (!listener.IsThreadSafe)
                    {
                        lock (listener)
                        {
                            listener.Fail(message);
                            if (AutoFlush) listener.Flush();
                        }
                    }
                    else
                    {
                        listener.Fail(message);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
        }

        public static void Fail(string? message, string? detailMessage)
        {
            if (UseGlobalLock)
            {
                lock (critSec)
                {
                    var listeners = Listeners.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        var listener = listeners[i];
                        listener.Fail(message, detailMessage);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
            else
            {
                var listeners = Listeners.List;
                for (int i = 0; i < listeners.Count; i++)
                {
                    var listener = listeners[i];
                    if (!listener.IsThreadSafe)
                    {
                        lock (listener)
                        {
                            listener.Fail(message, detailMessage);
                            if (AutoFlush) listener.Flush();
                        }
                    }
                    else
                    {
                        listener.Fail(message, detailMessage);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
        }

        // This method refreshes all the data from the configuration file, so that updated to the configuration file are mirrored
        // in the System.Diagnostics.Trace class
        internal static void Refresh()
        {
            lock (critSec)
            {
                s_autoFlush = default;
                s_noGlobalLock = default;
                s_listeners = null;
                Debug.IndentSize = 4;
            }
        }

        public static void TraceEvent(TraceEventType eventType, int id, string? format, params object?[]? args)
        {
            var eventCache = new TraceEventCache();
            string appName = s_appName ?? AppName;

            if (UseGlobalLock)
            {
                lock (critSec)
                {
                    var listeners = Listeners.List;
                    if (args == null)
                    {
                        for (int i = 0; i < listeners.Count; i++)
                        {
                            var listener = listeners[i];
                            listener.TraceEvent(eventCache, appName, eventType, id, format);
                            if (AutoFlush) listener.Flush();
                        }
                    }
                    else
                    {
                        for (int i = 0; i < listeners.Count; i++)
                        {
                            var listener = listeners[i];
                            listener.TraceEvent(eventCache, appName, eventType, id, format!, args);
                            if (AutoFlush) listener.Flush();
                        }
                    }
                }
            }
            else
            {
                var listeners = Listeners.List;
                if (args == null)
                {
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        var listener = listeners[i];
                        if (!listener.IsThreadSafe)
                        {
                            lock (listener)
                            {
                                listener.TraceEvent(eventCache, appName, eventType, id, format);
                                if (AutoFlush) listener.Flush();
                            }
                        }
                        else
                        {
                            listener.TraceEvent(eventCache, appName, eventType, id, format);
                            if (AutoFlush) listener.Flush();
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        var listener = listeners[i];
                        if (!listener.IsThreadSafe)
                        {
                            lock (listener)
                            {
                                listener.TraceEvent(eventCache, appName, eventType, id, format!, args);
                                if (AutoFlush) listener.Flush();
                            }
                        }
                        else
                        {
                            listener.TraceEvent(eventCache, appName, eventType, id, format!, args);
                            if (AutoFlush) listener.Flush();
                        }
                    }
                }
            }
        }


        public static void Write(string? message)
        {
            if (UseGlobalLock)
            {
                lock (critSec)
                {
                    var listeners = Listeners.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        var listener = listeners[i];
                        listener.Write(message);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
            else
            {
                var listeners = Listeners.List;
                for (int i = 0; i < listeners.Count; i++)
                {
                    var listener = listeners[i];
                    if (!listener.IsThreadSafe)
                    {
                        lock (listener)
                        {
                            listener.Write(message);
                            if (AutoFlush) listener.Flush();
                        }
                    }
                    else
                    {
                        listener.Write(message);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
        }

        public static void Write(object? value)
        {
            if (UseGlobalLock)
            {
                lock (critSec)
                {
                    var listeners = Listeners.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        var listener = listeners[i];
                        listener.Write(value);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
            else
            {
                var listeners = Listeners.List;
                for (int i = 0; i < listeners.Count; i++)
                {
                    var listener = listeners[i];
                    if (!listener.IsThreadSafe)
                    {
                        lock (listener)
                        {
                            listener.Write(value);
                            if (AutoFlush) listener.Flush();
                        }
                    }
                    else
                    {
                        listener.Write(value);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
        }

        public static void Write(string? message, string? category)
        {
            if (UseGlobalLock)
            {
                lock (critSec)
                {
                    var listeners = Listeners.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        var listener = listeners[i];
                        listener.Write(message, category);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
            else
            {
                var listeners = Listeners.List;
                for (int i = 0; i < listeners.Count; i++)
                {
                    var listener = listeners[i];
                    if (!listener.IsThreadSafe)
                    {
                        lock (listener)
                        {
                            listener.Write(message, category);
                            if (AutoFlush) listener.Flush();
                        }
                    }
                    else
                    {
                        listener.Write(message, category);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
        }

        public static void Write(object? value, string? category)
        {
            if (UseGlobalLock)
            {
                lock (critSec)
                {
                    var listeners = Listeners.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        var listener = listeners[i];
                        listener.Write(value, category);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
            else
            {
                var listeners = Listeners.List;
                for (int i = 0; i < listeners.Count; i++)
                {
                    var listener = listeners[i];
                    if (!listener.IsThreadSafe)
                    {
                        lock (listener)
                        {
                            listener.Write(value, category);
                            if (AutoFlush) listener.Flush();
                        }
                    }
                    else
                    {
                        listener.Write(value, category);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
        }

        public static void WriteLine(string? message)
        {
            if (UseGlobalLock)
            {
                lock (critSec)
                {
                    var listeners = Listeners.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        var listener = listeners[i];
                        listener.WriteLine(message);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
            else
            {
                var listeners = Listeners.List;
                for (int i = 0; i < listeners.Count; i++)
                {
                    var listener = listeners[i];
                    if (!listener.IsThreadSafe)
                    {
                        lock (listener)
                        {
                            listener.WriteLine(message);
                            if (AutoFlush) listener.Flush();
                        }
                    }
                    else
                    {
                        listener.WriteLine(message);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
        }

        public static void WriteLine(object? value)
        {
            if (UseGlobalLock)
            {
                lock (critSec)
                {
                    var listeners = Listeners.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        var listener = listeners[i];
                        listener.WriteLine(value);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
            else
            {
                var listeners = Listeners.List;
                for (int i = 0; i < listeners.Count; i++)
                {
                    var listener = listeners[i];
                    if (!listener.IsThreadSafe)
                    {
                        lock (listener)
                        {
                            listener.WriteLine(value);
                            if (AutoFlush) listener.Flush();
                        }
                    }
                    else
                    {
                        listener.WriteLine(value);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
        }

        public static void WriteLine(string? message, string? category)
        {
            if (UseGlobalLock)
            {
                lock (critSec)
                {
                    var listeners = Listeners.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        var listener = listeners[i];
                        listener.WriteLine(message, category);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
            else
            {
                var listeners = Listeners.List;
                for (int i = 0; i < listeners.Count; i++)
                {
                    var listener = listeners[i];
                    if (!listener.IsThreadSafe)
                    {
                        lock (listener)
                        {
                            listener.WriteLine(message, category);
                            if (AutoFlush) listener.Flush();
                        }
                    }
                    else
                    {
                        listener.WriteLine(message, category);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
        }

        public static void WriteLine(object? value, string? category)
        {
            if (UseGlobalLock)
            {
                lock (critSec)
                {
                    var listeners = Listeners.List;
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        var listener = listeners[i];
                        listener.WriteLine(value, category);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
            else
            {
                var listeners = Listeners.List;
                for (int i = 0; i < listeners.Count; i++)
                {
                    var listener = listeners[i];
                    if (!listener.IsThreadSafe)
                    {
                        lock (listener)
                        {
                            listener.WriteLine(value, category);
                            if (AutoFlush) listener.Flush();
                        }
                    }
                    else
                    {
                        listener.WriteLine(value, category);
                        if (AutoFlush) listener.Flush();
                    }
                }
            }
        }
    }
}
