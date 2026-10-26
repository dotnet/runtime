// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System;

public static partial class GC
{
    /// <summary>Registers a callback for individual garbage collection pauses.</summary>
    /// <param name="callback">The method to invoke on the finalizer thread.</param>
    /// <returns>A handle that stops the subscription when disposed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
    /// <exception cref="PlatformNotSupportedException">The runtime or loaded garbage collector does not support pause callbacks.</exception>
    /// <remarks>
    /// The subscription receives pauses captured after registration, not older
    /// buffered records. Callbacks can begin before this method returns.
    /// Capture uses a shared, bounded buffer and does not wait for consumers.
    /// When it fills, new records are skipped and counted in the next callback's
    /// <c>missedRecords</c> argument.
    /// Disposing the handle does not wait for a delivery already selected, which
    /// may complete after disposal returns. A callback can dispose its own handle.
    /// The caller must dispose the handle to release the subscription.
    /// </remarks>
    public static IDisposable RegisterPauseDataCallback(GCPauseDataCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return PauseDataCallbacks.Register(callback);
    }

    static unsafe partial void ConfigurePauseDataCallback(void* callback, bool enabled, ref long sequence, ref long missedRecords, ref bool supported);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePauseData
    {
        public long SequenceNumber;
        public long CollectionIndex;
        public long DurationMicroseconds;
        public int Generation;
        public int Concurrent;
    }

    private static class PauseDataCallbacks
    {
        private static readonly Lock s_lock = new();
        private static Registration[] s_registrations = [];

        internal static IDisposable Register(GCPauseDataCallback callback)
        {
            Registration registration = new(callback);
            lock (s_lock)
            {
                Registration[] registrations = new Registration[checked(s_registrations.Length + 1)];
                s_registrations.CopyTo(registrations, 0);
                Configure(enabled: true, out registration._firstSequence, out registration._lastMissedRecords);
                registrations[^1] = registration;
                s_registrations = registrations;
            }

            return registration;
        }

        private static unsafe void Configure(bool enabled, out long sequence, out long missedRecords)
        {
            sequence = 0;
            missedRecords = 0;
            // An unimplemented runtime hook leaves support disabled.
            bool supported = false;
            ConfigurePauseDataCallback(
                (void*)(delegate* unmanaged[Cdecl]<NativePauseData*, long, void>)&OnPause,
                enabled, ref sequence, ref missedRecords, ref supported);
            if (!supported)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_GCPauseDataCallback);
            }
        }

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant.
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
#pragma warning restore CS3016
        private static unsafe void OnPause(NativePauseData* data, long missedRecords)
        {
            Debug.Assert(sizeof(NativePauseData) == 32);
            GCPauseData pause = new(data->SequenceNumber, data->CollectionIndex,
                new TimeSpan(checked(data->DurationMicroseconds * TimeSpan.TicksPerMicrosecond)),
                data->Generation, data->Concurrent != 0);
            Registration[] registrations;
            lock (s_lock)
            {
                registrations = s_registrations;
            }

            foreach (Registration registration in registrations)
            {
                GCPauseDataCallback? callback;
                long missed;
                lock (s_lock)
                {
                    callback = registration._callback;
                    if (callback is null || pause.SequenceNumber <= registration._firstSequence)
                    {
                        continue;
                    }

                    missed = missedRecords - registration._lastMissedRecords;
                    registration._lastMissedRecords = missedRecords;
                }

                callback(pause, missed);
            }
        }

        private sealed class Registration(GCPauseDataCallback callback) : IDisposable
        {
            internal GCPauseDataCallback? _callback = callback;
            internal long _firstSequence;
            internal long _lastMissedRecords;

            public void Dispose()
            {
                lock (s_lock)
                {
                    if (_callback is null)
                    {
                        return;
                    }

                    int index = Array.IndexOf(s_registrations, this);
                    Debug.Assert(index >= 0);
                    Registration[] registrations = s_registrations.Length == 1
                        ? Array.Empty<Registration>()
                        : new Registration[s_registrations.Length - 1];
                    Array.Copy(s_registrations, 0, registrations, 0, index);
                    Array.Copy(s_registrations, index + 1, registrations, index, registrations.Length - index);
                    if (registrations.Length == 0)
                    {
                        Configure(enabled: false, out _, out _);
                    }

                    _callback = null;
                    s_registrations = registrations;
                }
            }
        }
    }
}
