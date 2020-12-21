// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using SslStress.Utils;

namespace SslStress
{
    public abstract partial class SslClientBase
    {
        private class StressResultAggregator
        {
            private long _totalConnections = 0;
            private readonly long[] _successes, _failures, _cancellations;
            private readonly ErrorAggregator _errors = new ErrorAggregator();
            private readonly StreamCounter[] _currentCounters;
            private readonly StreamCounter[] _aggregateCounters;

            public StressResultAggregator(int workerCount)
            {
                _currentCounters = Enumerable.Range(0, workerCount).Select(_ => new StreamCounter()).ToArray();
                _aggregateCounters = Enumerable.Range(0, workerCount).Select(_ => new StreamCounter()).ToArray();
                _successes = new long[workerCount];
                _failures = new long[workerCount];
                _cancellations = new long[workerCount];
            }

            public long TotalConnections => _totalConnections;
            public long TotalFailures => _failures.Sum();

            public StreamCounter GetCounters(int workerId) => _currentCounters[workerId];

            public void RecordSuccess(int workerId)
            {
                _successes[workerId]++;
                Interlocked.Increment(ref _totalConnections);
                UpdateCounters(workerId);
            }

            public void RecordCancellation(int workerId)
            {
                _cancellations[workerId]++;
                Interlocked.Increment(ref _totalConnections);
                UpdateCounters(workerId);
            }

            public void RecordFailure(int workerId, Exception exn)
            {
                _failures[workerId]++;
                Interlocked.Increment(ref _totalConnections);
                _errors.RecordError(exn);
                UpdateCounters(workerId);

                lock (Console.Out)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"Worker #{workerId}: unhandled exception: {exn}");
                    Console.WriteLine();
                    Console.ResetColor();
                }
            }

            private void UpdateCounters(int workerId)
            {
                // need to synchronize with GetCounterView to avoid reporting bad data
                lock (_aggregateCounters)
                {
                    _aggregateCounters[workerId].Append(_currentCounters[workerId]);
                    _currentCounters[workerId].Reset();
                }
            }

            private (StreamCounter total, StreamCounter current)[] GetCounterView()
            {
                // generate a coherent view of counter state
                lock (_aggregateCounters)
                {
                    var view = new (StreamCounter total, StreamCounter current)[_aggregateCounters.Length];
                    for (int i = 0; i < _aggregateCounters.Length; i++)
                    {
                        StreamCounter current = _currentCounters[i].Clone();
                        StreamCounter total = _aggregateCounters[i].Clone().Append(current);
                        view[i] = (total, current);
                    }

                    return view;
                }
            }

            public void PrintFailureTypes() => _errors.PrintFailureTypes();

            public void PrintCurrentResults(TimeSpan elapsed, bool showAggregatesOnly)
            {
                (StreamCounter total, StreamCounter current)[] counters = GetCounterView();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"[{DateTime.Now}]");
                Console.ResetColor();
                Console.WriteLine(" Elapsed: " + elapsed.ToString(@"hh\:mm\:ss"));
                Console.ResetColor();

                for (int i = 0; i < _currentCounters.Length; i++)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"\tWorker #{i.ToString("N0")}:");
                    Console.ResetColor();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"\tPass: ");
                    Console.ResetColor();
                    Console.Write(_successes[i].ToString("N0"));
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("\tCancel: ");
                    Console.ResetColor();
                    Console.Write(_cancellations[i].ToString("N0"));
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write("\tFail: ");
                    Console.ResetColor();
                    Console.Write(_failures[i].ToString("N0"));

                    if (!showAggregatesOnly)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkBlue;
                        Console.Write($"\tCurr. Tx: ");
                        Console.ResetColor();
                        Console.Write(FmtBytes(counters[i].current.BytesWritten));
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        Console.Write($"\tCurr. Rx: ");
                        Console.ResetColor();
                        Console.Write(FmtBytes(counters[i].current.BytesRead));
                    }

                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.Write($"\tTotal Tx: ");
                    Console.ResetColor();
                    Console.Write(FmtBytes(counters[i].total.BytesWritten));
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.Write($"\tTotal Rx: ");
                    Console.ResetColor();
                    Console.Write(FmtBytes(counters[i].total.BytesRead));

                    Console.WriteLine();
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\tTOTAL :   ");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"\tPass: ");
                Console.ResetColor();
                Console.Write(_successes.Sum().ToString("N0"));
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("\tCancel: ");
                Console.ResetColor();
                Console.Write(_cancellations.Sum().ToString("N0"));
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write("\tFail: ");
                Console.ResetColor();
                Console.Write(_failures.Sum().ToString("N0"));

                if (!showAggregatesOnly)
                {
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.Write("\tCurr. Tx: ");
                    Console.ResetColor();
                    Console.Write(FmtBytes(counters.Select(c => c.current.BytesWritten).Sum()));
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.Write($"\tCurr. Rx: ");
                    Console.ResetColor();
                    Console.Write(FmtBytes(counters.Select(c => c.current.BytesRead).Sum()));
                }

                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Write("\tTotal Tx: ");
                Console.ResetColor();
                Console.Write(FmtBytes(counters.Select(c => c.total.BytesWritten).Sum()));
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.Write($"\tTotal Rx: ");
                Console.ResetColor();
                Console.Write(FmtBytes(counters.Select(c => c.total.BytesRead).Sum()));

                Console.WriteLine();
                Console.WriteLine();

                static string FmtBytes(long value) => HumanReadableByteSizeFormatter.Format(value);
            }
        }
    }
}
