// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics.Metrics
{
    internal sealed partial class AggregationManager
    {
        private Timer? _pollingTimer;
        private DateTime _startTime;
        private DateTime _intervalStartTime;
        private DateTime _nextIntervalStartTime;

        public void Start()
        {
            // if already started or already stopped we can't be started again
            Debug.Assert(_pollingTimer == null && !_cts.IsCancellationRequested);
            Debug.Assert(CollectionPeriod.TotalSeconds >= MinCollectionTimeSecs);

            DateTime now = _startTime = _intervalStartTime  = DateTime.UtcNow;
            _nextIntervalStartTime = CalculateDelayTime(now, _startTime, _intervalStartTime, CollectionPeriod.TotalSeconds);
            TimeSpan delayTime = _nextIntervalStartTime - now;
            _pollingTimer = new Timer(CollectOnce, null, (int)delayTime.TotalMilliseconds, 0);

            _listener.Start();
            _initialInstrumentEnumerationComplete();
        }

        private void CollectOnce(object? state)
        {
            try
            {
                if (_cts.Token.IsCancellationRequested)
                {
                    return;
                }

                // collect statistics for the completed interval
                _beginCollection(_intervalStartTime, _nextIntervalStartTime);
                Collect();
                _endCollection(_intervalStartTime, _nextIntervalStartTime);

                DateTime now = DateTime.UtcNow;
                _nextIntervalStartTime = CalculateDelayTime(now, _startTime, _intervalStartTime, CollectionPeriod.TotalSeconds);
                TimeSpan delayTime = _nextIntervalStartTime - now;
                _intervalStartTime = _nextIntervalStartTime;
                // schedule the next collection
                _pollingTimer!.Change((int)delayTime.TotalMilliseconds, 0);
            }
            catch (Exception e)
            {
                _collectionError(e);
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _pollingTimer?.Dispose();
            _listener.Dispose();
        }
    }
}
