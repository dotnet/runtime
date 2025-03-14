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
        private Thread? _collectThread;

        public void Start()
        {
            // if already started or already stopped we can't be started again
            Debug.Assert(_collectThread == null && !_cts.IsCancellationRequested);
            Debug.Assert(CollectionPeriod.TotalSeconds >= MinCollectionTimeSecs);

            // This explicitly uses a Thread and not a Task so that metrics still work
            // even when an app is experiencing thread-pool starvation. Although we
            // can't make in-proc metrics robust to everything, this is a common enough
            // problem in .NET apps that it feels worthwhile to take the precaution.
            _collectThread = new Thread(() => CollectWorker(_cts.Token));
            _collectThread.IsBackground = true;
            _collectThread.Name = "MetricsEventSource CollectWorker";
#pragma warning disable CA1416 // 'Thread.Start' is unsupported on: 'browser', there the actual implementation is in AggregationManager.Wasm.cs
            _collectThread.Start();
#pragma warning restore CA1416

            _listener.Start();
            _initialInstrumentEnumerationComplete();
        }

        private void CollectWorker(CancellationToken cancelToken)
        {
            try
            {
                double collectionIntervalSecs = -1;
                lock (this)
                {
                    collectionIntervalSecs = CollectionPeriod.TotalSeconds;
                }
                Debug.Assert(collectionIntervalSecs >= MinCollectionTimeSecs);

                DateTime startTime = DateTime.UtcNow;
                DateTime intervalStartTime = startTime;
                while (!cancelToken.IsCancellationRequested)
                {
                    // pause until the interval is complete
                    DateTime now = DateTime.UtcNow;
                    DateTime nextIntervalStartTime = CalculateDelayTime(now, startTime, intervalStartTime, collectionIntervalSecs);
                    TimeSpan delayTime = nextIntervalStartTime - now;
                    if (cancelToken.WaitHandle.WaitOne(delayTime))
                    {
                        // don't do collection if timer may not have run to completion
                        break;
                    }

                    // collect statistics for the completed interval
                    _beginCollection(intervalStartTime, nextIntervalStartTime);
                    Collect();
                    _endCollection(intervalStartTime, nextIntervalStartTime);
                    intervalStartTime = nextIntervalStartTime;
                }
            }
            catch (Exception e)
            {
                _collectionError(e);
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            if (_collectThread != null)
            {
                _collectThread.Join();
                _collectThread = null;
            }
            _listener.Dispose();
        }
    }
}
