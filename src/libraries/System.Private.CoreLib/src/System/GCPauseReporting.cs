// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace System;

internal static class GCPauseReporting
{
#if CORECLR
    private const int BufferCapacity = 4096;
    private const int BatchIntervalMilliseconds = 10;

    private static readonly object s_lock = new();
    private static Registration[] s_registrations = [];
    private static int s_registrationCount;
    private static GCPauseRecord[]? s_buffer;
    private static Thread? s_thread;
#endif

    internal static bool IsSupported()
    {
#if CORECLR
        return GC.IsGCPauseReportingSupported();
#else
        return false;
#endif
    }

    internal static IDisposable Register(Action<ulong, ulong, int, int> callback)
    {
#if CORECLR
        lock (s_lock)
        {
            Debug.Assert(GC.IsGCPauseReportingSupported());

            var registration = new Registration(callback);
            var registrations = new Registration[s_registrationCount + 1];
            int index = 0;
            foreach (Registration existing in s_registrations)
            {
                if (existing._callback is not null)
                {
                    registrations[index++] = existing;
                }
            }
            registrations[index] = registration;

            s_buffer ??= new GCPauseRecord[BufferCapacity];
            if (s_registrationCount == 0)
            {
                Thread? thread = s_thread is null ? new Thread(EventWorker)
                {
                    IsBackground = true,
                    Name = ".NET GC Pause Reporting"
                } : null;
                GC.ConfigureGCPauseReporting(true);
                try
                {
                    thread?.UnsafeStart();
                }
                catch
                {
                    GC.ConfigureGCPauseReporting(false);
                    throw;
                }
                s_thread ??= thread;
            }
            else
            {
                // Skip records captured before this registration without consuming another's backlog.
                Drain(null, out _, out registration._skipCount);
            }

            // Publish only after startup succeeds. The worker cannot enter until this lock is released.
            s_registrations = registrations;
            s_registrationCount++;
            return registration;
        }
#else
        throw new PlatformNotSupportedException();
#endif
    }

    internal static long GetDroppedCount()
    {
#if CORECLR
        lock (s_lock)
        {
            Drain(null, out ulong dropped, out _);
            return dropped <= long.MaxValue ? (long)dropped : long.MaxValue;
        }
#else
        return 0;
#endif
    }

#if CORECLR
    // Keep the record layout in sync with GCPauseRecord in gcinterface.h.
    internal struct GCPauseRecord
    {
        internal ulong DurationMicroseconds;
        internal ulong CollectionIndex;
        internal int Generation;
        internal int Kind;
    }

    private sealed class Registration(Action<ulong, ulong, int, int> callback) : IDisposable
    {
        internal Action<ulong, ulong, int, int>? _callback = callback;
        internal int _skipCount;

        public void Dispose()
        {
            lock (s_lock)
            {
                if (_callback is null)
                {
                    return;
                }

                // Existing dispatch snapshots may retain this token, but not its assembly or listeners.
                Volatile.Write(ref _callback, null);
                if (--s_registrationCount == 0)
                {
                    StopReporting();
                }
            }
        }
    }

    private static void StopReporting()
    {
        Debug.Assert(Monitor.IsEntered(s_lock));
        GC.ConfigureGCPauseReporting(false);
        foreach (Registration registration in s_registrations)
        {
            Volatile.Write(ref registration._callback, null);
        }
        s_registrations = [];
        s_registrationCount = 0;
        Monitor.PulseAll(s_lock);
    }

    private static unsafe int Drain(GCPauseRecord[]? buffer, out ulong dropped, out int remaining)
    {
        Debug.Assert(Monitor.IsEntered(s_lock));
        Debug.Assert(sizeof(GCPauseRecord) == 24);
        fixed (GCPauseRecord* records = buffer)
        {
            return GC.DrainGCPauseRecords(records, buffer?.Length ?? 0, out dropped, out remaining);
        }
    }

    private static bool Dispatch()
    {
        Registration[] registrations;
        GCPauseRecord[] buffer;
        int count;
        lock (s_lock)
        {
            if (s_registrationCount == 0)
            {
                return false;
            }

            Debug.Assert(s_buffer is not null);
            buffer = s_buffer;
            registrations = s_registrations;
            count = Drain(buffer, out _, out _);
        }

        // Only EventWorker dispatches. Process one snapshot: callbacks may themselves collect.
        foreach (Registration registration in registrations)
        {
            int skip = Math.Min(registration._skipCount, count);
            registration._skipCount -= skip;
            for (int i = skip; i < count; i++)
            {
                Action<ulong, ulong, int, int>? callback = Volatile.Read(ref registration._callback);
                if (callback is null)
                {
                    break;
                }

                ref GCPauseRecord record = ref buffer[i];
                callback(record.DurationMicroseconds, record.CollectionIndex, record.Generation, record.Kind);
            }
        }

        return count != 0;
    }

    private static void EventWorker()
    {
        try
        {
            while (true)
            {
                lock (s_lock)
                {
                    if (s_registrationCount == 0)
                    {
                        s_thread = null;
                        return;
                    }
                }

                if (!GC.WaitForGCPauseRecords(Timeout.Infinite))
                {
                    continue;
                }

                long nextBatch = Environment.TickCount64 + BatchIntervalMilliseconds;
                if (Dispatch())
                {
                    lock (s_lock)
                    {
                        // A signaled native queue must not turn listener-induced GCs into a busy loop.
                        long remaining;
                        while (s_registrationCount != 0 && (remaining = nextBatch - Environment.TickCount64) > 0)
                        {
                            Monitor.Wait(s_lock, (int)remaining);
                        }
                    }
                }
            }
        }
        finally
        {
            lock (s_lock)
            {
                if (ReferenceEquals(s_thread, Thread.CurrentThread))
                {
                    s_thread = null;
                    StopReporting();
                }
            }
        }
    }
#endif
}
