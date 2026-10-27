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
    public static IDisposable RegisterPauseRecordCallback(GCPauseRecordCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return PauseRecordCallbacks.Register(callback);
    }

    static unsafe partial void ConfigurePauseRecordCallback(void* callback, bool enabled, ref PauseRecordCallbackState state);

    private struct PauseRecordCallbackState
    {
        public long Sequence;
        public long MissedRecords;
        public bool Supported;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePauseRecord
    {
        public long SequenceNumber;
        public long CollectionIndex;
        public long DurationMicroseconds;
        public int Generation;
        public int Concurrent;
    }

    private static class PauseRecordCallbacks
    {
        private static readonly Lock s_lock = new();
        private static Registration[] s_registrations = [];

        internal static IDisposable Register(GCPauseRecordCallback callback)
        {
            Registration registration = new(callback);
            lock (s_lock)
            {
                Registration[] registrations = new Registration[checked(s_registrations.Length + 1)];
                s_registrations.CopyTo(registrations, 0);
                PauseRecordCallbackState state = Configure(enabled: true);
                registration._firstSequence = state.Sequence;
                registration._lastMissedRecords = state.MissedRecords;
                registrations[^1] = registration;
                Volatile.Write(ref s_registrations, registrations);
            }

            return registration;
        }

        private static unsafe PauseRecordCallbackState Configure(bool enabled)
        {
            PauseRecordCallbackState state = default;
            ConfigurePauseRecordCallback(
                (void*)(delegate* unmanaged[Cdecl]<NativePauseRecord*, long, void>)&OnPause,
                enabled, ref state);
            if (!state.Supported)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_GCPauseRecordCallback);
            }

            return state;
        }

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant.
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
#pragma warning restore CS3016
        private static unsafe void OnPause(NativePauseRecord* data, long missedRecords)
        {
            Debug.Assert(sizeof(NativePauseRecord) == 32);
            GCPauseRecord pause = new(data->SequenceNumber, data->CollectionIndex,
                new TimeSpan(checked(data->DurationMicroseconds * TimeSpan.TicksPerMicrosecond)),
                data->Generation, data->Concurrent != 0);
            Registration[] registrations = Volatile.Read(ref s_registrations);

            foreach (Registration registration in registrations)
            {
                GCPauseRecordCallback? callback = Volatile.Read(ref registration._callback);
                if (callback is null || pause.SequenceNumber <= registration._firstSequence)
                {
                    continue;
                }

                // Registration initializes this before publication; only this thread advances it.
                long missed = missedRecords - registration._lastMissedRecords;
                registration._lastMissedRecords = missedRecords;
                callback(pause, missed);
            }
        }

        private sealed class Registration(GCPauseRecordCallback callback) : IDisposable
        {
            internal GCPauseRecordCallback? _callback = callback;
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
                        Configure(enabled: false);
                    }

                    Volatile.Write(ref _callback, null);
                    Volatile.Write(ref s_registrations, registrations);
                }
            }
        }
    }
}
