// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace Microsoft.Extensions.Internal.ClockQuantization
{
    // Isolate some of the context/metronome madness from the core ClockQuantizer implementation
    internal class TemporalContextDriver : ClockQuantization.ISystemClock, ISystemClockTemporalContext, IDisposable, IAsyncDisposable
    {
        private readonly ClockQuantization.ISystemClock _clock;
        private readonly TimeSpan _metronomeIntervalTimeSpan;
        private System.Threading.Timer? _metronome;
        private EventArgs? _pendingClockAdjustedEventArgs;

        public TemporalContextDriver(ISystemClock clock, TimeSpan? metronomeIntervalTimeSpan = null)
        {
            AttachExternalTemporalContext(this, clock, out var externalMetronomeIntervalTimeSpan, out var clockIsManual);

            if (externalMetronomeIntervalTimeSpan.HasValue)
            {
                IsQuiescent = false;
                _metronomeIntervalTimeSpan = externalMetronomeIntervalTimeSpan.Value;
            }
            else
            {
                if (!metronomeIntervalTimeSpan.HasValue)
                {
                    // Need metronome interval timespan
                    DetachExternalTemporalContext();
                    throw new ArgumentNullException(nameof(metronomeIntervalTimeSpan), "Must provide a valid metronome interval timespan or supply an external metronome.");
                }

                _metronomeIntervalTimeSpan = metronomeIntervalTimeSpan.Value;
            }

            _clock = clock;
            ClockIsManual = clockIsManual;

            static void AttachExternalTemporalContext(TemporalContextDriver driver, ISystemClock clock, out TimeSpan? externalMetronomeIntervalTimeSpan, out bool clockIsManual)
            {
                externalMetronomeIntervalTimeSpan = null;
                clockIsManual = default;

                if (clock is ISystemClockTemporalContext context)
                {
                    context.ClockAdjusted += driver.Context_ClockAdjusted;
                    if (context.MetronomeIntervalTimeSpan.HasValue)
                    {
                        externalMetronomeIntervalTimeSpan = context.MetronomeIntervalTimeSpan.Value;
                        // Allow external "pulse" on metronome ticks
                        context.MetronomeTicked += driver.Context_MetronomeTicked;
                    }

                    clockIsManual = context.ClockIsManual;
                }
            }
        }

        private int _isExternalTemporalContextDetached;
        private void DetachExternalTemporalContext()
        {
            if (Interlocked.CompareExchange(ref _isExternalTemporalContextDetached, 1, 0) == 0)
            {
                if (_clock is ISystemClockTemporalContext context)
                {
                    context.ClockAdjusted -= Context_ClockAdjusted;
                    if (context.MetronomeIntervalTimeSpan.HasValue)
                    {
                        context.MetronomeTicked -= Context_MetronomeTicked;
                    }
                }
            }
        }

        public bool ClockIsManual { get; private set; }

        public bool HasInternalMetronome
        {
            get => _clock is not ISystemClockTemporalContext context || !context.MetronomeIntervalTimeSpan.HasValue;
        }

        private void EnsureInternalMetronome()
        {
            if (_metronome is null && HasInternalMetronome)
            {
                // Create a paused metronome timer
                var metronome = new Timer(Metronome_TimerCallback, null, Timeout.InfiniteTimeSpan, _metronomeIntervalTimeSpan);
                if (Interlocked.CompareExchange(ref _metronome, metronome, null) is null)
                {
                    // Resume the newly created metronome timer
                    metronome.Change(_metronomeIntervalTimeSpan, _metronomeIntervalTimeSpan);
                }
                else
                {
                    // Wooops... another thread outpaced us...
                    metronome.Dispose();
                }
            }
        }

        private void DisposeInternalMetronome()
        {
            if (Interlocked.Exchange(ref _metronome, null) is Timer metronome)
            {
                metronome.Dispose();
            }
        }

        private async ValueTask DisposeInternalMetronomeAsync()
        {
#if !(NETSTANDARD2_1 || NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0 || NET5_0_OR_GREATER)
            await default(ValueTask).ConfigureAwait(continueOnCapturedContext: false);
#endif

            if (Interlocked.Exchange(ref _metronome, null) is Timer metronome)
            {
#if NETSTANDARD2_1 || NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0 || NET5_0_OR_GREATER
                if (metronome is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
                    return;
                }
#endif
                metronome.Dispose();
            }
        }

        public bool IsQuiescent { get; private set; } = true;

        public void Quiesce()
        {
            IsQuiescent = true;

            // Dispose of internal metronome, if applicable.
            DisposeInternalMetronome();
        }

        private readonly object _unquiescingLockObject = new object();
        public bool Unquiesce()
        {
            bool unquiescing = IsQuiescent;

            if (unquiescing)
            {
                EventArgs? pendingClockAdjustedEventArgs = Interlocked.Exchange(ref _pendingClockAdjustedEventArgs, null);

                if (pendingClockAdjustedEventArgs is not null)
                {
                    // Make sure that we briefly postpone any metronome event that may occur during the process of unquiescing
                    lock (_unquiescingLockObject)
                    {
                        IsQuiescent = false;    // Set to false already to make sure that the ClockAdjusted event fires
                        OnClockAdjusted(pendingClockAdjustedEventArgs);
                    }
                }

                IsQuiescent = false;
            }

            // Ensure that we have an internal metronome, if applicable.
            EnsureInternalMetronome();

            return unquiescing;
        }

        protected virtual void OnMetronomeTicked(EventArgs e)
        {
            if (!IsQuiescent)
            {
                // Make sure that we briefly postpone a metronome event that occurs during the process of unquiescing
                lock (_unquiescingLockObject)
                {
                    MetronomeTicked?.Invoke(this, e);
                }
            }
        }

        protected virtual void OnClockAdjusted(EventArgs e)
        {
            if (!IsQuiescent)
            {
                ClockAdjusted?.Invoke(this, e);
            }
            else
            {
                // Retain the latest ClockAdjusted event until we unquiesce
                Interlocked.Exchange(ref _pendingClockAdjustedEventArgs, e);
            }
        }

        private void Context_ClockAdjusted(object? _, EventArgs __) => OnClockAdjusted(EventArgs.Empty);

        private void Context_MetronomeTicked(object? _, EventArgs __) => OnMetronomeTicked(EventArgs.Empty);

        private void Metronome_TimerCallback(object? _) => OnMetronomeTicked(EventArgs.Empty);


        #region ISystemClock

        /// <inheritdoc/>
        public DateTimeOffset UtcNow { get => _clock.UtcNow; }
        /// <inheritdoc/>
        public long UtcNowClockOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _clock.UtcNowClockOffset; }
        /// <inheritdoc/>
        public long ClockOffsetUnitsPerMillisecond { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _clock.ClockOffsetUnitsPerMillisecond; }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTimeOffset ClockOffsetToUtcDateTimeOffset(long offset) => _clock.ClockOffsetToUtcDateTimeOffset(offset);
        /// <inheritdoc/>
        public long DateTimeOffsetToClockOffset(DateTimeOffset offset) => _clock.DateTimeOffsetToClockOffset(offset);

        #endregion

        #region ISystemClockTemporalContext

        /// <inheritdoc/>
        public TimeSpan? MetronomeIntervalTimeSpan { get => _metronomeIntervalTimeSpan; }
        /// <inheritdoc/>
        public event EventHandler? ClockAdjusted;
        /// <inheritdoc/>
        public event EventHandler? MetronomeTicked;

        #endregion

        #region IAsyncDisposable/IDisposable

        /// <inheritdoc/>
        public void Dispose()
        {
            IsQuiescent = true;

            // This method is re-entrant
            DetachExternalTemporalContext();

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            IsQuiescent = true;

            // This method is re-entrant
            DetachExternalTemporalContext();

            await DisposeAsyncCore().ConfigureAwait(false);

            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // This method is re-entrant
                DisposeInternalMetronome();
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            // This method is re-entrant
            await DisposeInternalMetronomeAsync().ConfigureAwait(false);
        }

        #endregion
    }
}

#nullable restore
