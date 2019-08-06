// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HttpStress
{
    public class StressClient : IDisposable
    {
        private readonly (string name, Func<RequestContext, Task> operation)[] _clientOperations;
        private readonly Configuration _config;
        private readonly StressResultAggregator _aggregator;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _clientTask;

        public StressClient((string name, Func<RequestContext, Task> operation)[] clientOperations, Configuration configuration)
        {
            _clientOperations = clientOperations;
            _config = configuration;
            _aggregator = new StressResultAggregator(clientOperations);
        }

        public void Start()
        {
            lock (_cts)
            {
                if (_cts.IsCancellationRequested)
                {
                    throw new ObjectDisposedException(nameof(StressClient));
                }
                if (_clientTask != null)
                {
                    throw new InvalidOperationException("Stress client already running");
                }

                _stopwatch.Start();
                _clientTask = StartCore();
            }
        }

        public void Stop()
        {
            _cts.Cancel();
            _clientTask?.Wait();
            _stopwatch.Stop();
            _cts.Dispose();
        }

        public void PrintFinalReport()
        {
            lock(Console.Out)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("HttpStress Run Final Report");
                Console.WriteLine();

                _aggregator.PrintCurrentResults(_stopwatch.Elapsed);
                _aggregator.PrintLatencies();
                _aggregator.PrintFailureTypes();
            }
        }

        public void Dispose() => Stop();

        private async Task StartCore()
        {
            var handler = new SocketsHttpHandler()
            {
                PooledConnectionLifetime = _config.ConnectionLifetime.GetValueOrDefault(Timeout.InfiniteTimeSpan),
                SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = delegate { return true; }
                }
            };

            using var client = new HttpClient(handler) { BaseAddress = _config.ServerUri, Timeout = _config.DefaultTimeout };

            async Task RunWorker(int taskNum)
            {
                // create random instance specific to the current worker
                var random = new Random(Combine(taskNum, _config.RandomSeed));
                var stopwatch = new Stopwatch();

                for (long i = taskNum; ; i++)
                {
                    if (_cts.IsCancellationRequested)
                        break;

                    int opIndex = (int)(i % _clientOperations.Length);
                    (string operation, Func<RequestContext, Task> func) = _clientOperations[opIndex];
                    var requestContext = new RequestContext(_config, client, random, _cts.Token, taskNum);
                    stopwatch.Restart();
                    try
                    {
                        await func(requestContext);

                        _aggregator.RecordSuccess(opIndex, stopwatch.Elapsed);
                    }
                    catch (OperationCanceledException) when (requestContext.IsCancellationRequested || _cts.IsCancellationRequested)
                    {
                        _aggregator.RecordCancellation(opIndex, stopwatch.Elapsed);
                    }
                    catch (Exception e)
                    {
                        _aggregator.RecordFailure(e, opIndex, stopwatch.Elapsed, taskNum: taskNum, iteration: i);
                    }
                }

                // deterministic hashing copied from System.Runtime.Hashing
                int Combine(int h1, int h2)
                {
                    uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
                    return ((int)rol5 + h1) ^ h2;
                }
            }

            // Spin up a thread dedicated to outputting stats for each defined interval
            new Thread(() =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    Thread.Sleep(_config.DisplayInterval);
                    lock (Console.Out) { _aggregator.PrintCurrentResults(_stopwatch.Elapsed); }
                }
            })
            { IsBackground = true }.Start();

            // Start N workers, each of which sits in a loop making requests.
            Task[] tasks = Enumerable.Range(0, _config.ConcurrentRequests).Select(RunWorker).ToArray();
            await Task.WhenAll(tasks);
        }

        /// <summary>Aggregate view of a particular stress failure type</summary>
        private sealed class StressFailureType
        {
            // Representative error text of stress failure
            public string ErrorText { get; }
            public ImmutableDictionary<int, int> Failures { get; }

            public StressFailureType(string errorText, ImmutableDictionary<int, int> failures)
            {
                ErrorText = errorText;
                Failures = failures;
            }

            public int FailureCount => Failures.Values.Sum();
        }

        private sealed class StressResultAggregator
        {
            private readonly string[] _operationNames;

            private long _totalRequests = 0;
            private readonly long[] _successes, _cancellations, _failures;
            private long _reuseAddressFailures = 0;
            private long _lastTotal = -1;

            private readonly ConcurrentDictionary<(Type exception, string message, string callSite)[], StressFailureType> _failureTypes;
            private readonly ConcurrentBag<double> _latencies = new ConcurrentBag<double>();

            public StressResultAggregator((string name, Func<RequestContext, Task>)[] operations)
            {
                _operationNames = operations.Select(x => x.name).ToArray();
                _successes = new long[operations.Length];
                _cancellations = new long[operations.Length];
                _failures = new long[operations.Length];
                _failureTypes = new ConcurrentDictionary<(Type, string, string)[], StressFailureType>(new StructuralEqualityComparer<(Type, string, string)[]>());
            }

            public void RecordSuccess(int operationIndex, TimeSpan elapsed)
            {
                Interlocked.Increment(ref _totalRequests);
                Interlocked.Increment(ref _successes[operationIndex]);

                _latencies.Add(elapsed.TotalMilliseconds);
            }

            public void RecordCancellation(int operationIndex, TimeSpan elapsed)
            {
                Interlocked.Increment(ref _totalRequests);
                Interlocked.Increment(ref _cancellations[operationIndex]);

                _latencies.Add(elapsed.TotalMilliseconds);
            }

            public void RecordFailure(Exception exn, int operationIndex, TimeSpan elapsed, int taskNum, long iteration)
            {
                Interlocked.Increment(ref _totalRequests);
                Interlocked.Increment(ref _failures[operationIndex]);

                _latencies.Add(elapsed.TotalMilliseconds);

                RecordFailureType();
                PrintToConsole();

                // record exception according to failure type classification
                void RecordFailureType()
                {
                    (Type, string, string)[] key = ClassifyFailure(exn);

                    _failureTypes.AddOrUpdate(key, Add, Update);

                    StressFailureType Add<T>(T key)
                    {
                        return new StressFailureType(exn.ToString(), ImmutableDictionary<int, int>.Empty.SetItem(operationIndex, 1));
                    }

                    StressFailureType Update<T>(T key, StressFailureType current)
                    {
                        current.Failures.TryGetValue(operationIndex, out int failureCount);
                        return new StressFailureType(current.ErrorText, current.Failures.SetItem(operationIndex, failureCount + 1));
                    }

                    (Type exception, string message, string callSite)[] ClassifyFailure(Exception exn)
                    {
                        var acc = new List<(Type exception, string message, string callSite)>();

                        while (exn != null)
                        {
                            acc.Add((exn.GetType(), exn.Message, new StackTrace(exn, true).GetFrame(0).ToString()));
                            exn = exn.InnerException;
                        }

                        return acc.ToArray();
                    }
                }

                void PrintToConsole()
                {
                    if (exn is HttpRequestException hre && hre.InnerException is SocketException se && se.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    {
                        Interlocked.Increment(ref _reuseAddressFailures);
                    }
                    else
                    {
                        lock (Console.Out)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Error from iteration {iteration} ({_operationNames[operationIndex]}) in task {taskNum} with {_successes.Sum()} successes / {_failures.Sum()} fails:");
                            Console.ResetColor();
                            Console.WriteLine(exn);
                            Console.WriteLine();
                        }
                    }
                }
            }

            public void PrintCurrentResults(TimeSpan runtime)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("[" + DateTime.Now + "]");
                Console.ResetColor();

                if (_lastTotal == _totalRequests)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                _lastTotal = _totalRequests;
                Console.Write(" Total: " + _totalRequests.ToString("N0"));
                Console.ResetColor();
                Console.WriteLine($" Runtime: " + runtime.ToString(@"hh\:mm\:ss"));

                if (_reuseAddressFailures > 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("~~ Reuse address failures: " + _reuseAddressFailures.ToString("N0") + "~~");
                    Console.ResetColor();
                }

                for (int i = 0; i < _operationNames.Length; i++)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"\t{_operationNames[i].PadRight(30)}");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("Success: ");
                    Console.Write(_successes[i].ToString("N0"));
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("\tCanceled: ");
                    Console.Write(_cancellations[i].ToString("N0"));
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write("\tFail: ");
                    Console.ResetColor();
                    Console.WriteLine(_failures[i].ToString("N0"));
                }
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\t    TOTAL".PadRight(31));
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Success: ");
                Console.Write(_successes.Sum().ToString("N0"));
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("\tCanceled: ");
                Console.Write(_cancellations.Sum().ToString("N0"));
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write("\tFail: ");
                Console.ResetColor();
                Console.WriteLine(_failures.Sum().ToString("N0"));
                Console.WriteLine();
            }

            public void PrintLatencies()
            {
                var latencies = _latencies.ToArray();
                Array.Sort(latencies);

                Console.WriteLine($"Latency(ms) : n={latencies.Length}, p50={Pc(0.5)}, p75={Pc(0.75)}, p99={Pc(0.99)}, p999={Pc(0.999)}, max={Pc(1)}");
                Console.WriteLine();

                double Pc(double percentile)
                {
                    int N = latencies.Length;
                    double n = (N - 1) * percentile + 1;
                    if (n == 1) return Rnd(latencies[0]);
                    else if (n == N) return Rnd(latencies[N - 1]);
                    else
                    {
                        int k = (int)n;
                        double d = n - k;
                        return Rnd(latencies[k - 1] + d * (latencies[k] - latencies[k - 1]));
                    }

                    double Rnd(double value) => Math.Round(value, 2);
                }
            }

            public void PrintFailureTypes()
            {
                if (_failureTypes.Count == 0)
                    return;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"There were a total of {_failures.Sum()} failures classified into {_failureTypes.Count} different types:");
                Console.WriteLine();
                Console.ResetColor();

                int i = 0;
                foreach (StressFailureType failure in _failureTypes.Values.OrderByDescending(x => x.FailureCount))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Failure Type {++i}/{_failureTypes.Count}:");
                    Console.ResetColor();
                    Console.WriteLine(failure.ErrorText);
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    foreach (KeyValuePair<int, int> operation in failure.Failures)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($"\t{_operationNames[operation.Key].PadRight(30)}");
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($"Fail: ");
                        Console.ResetColor();
                        Console.WriteLine(operation.Value);
                    }

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("\t    TOTAL".PadRight(31));
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"Fail: ");
                    Console.ResetColor();
                    Console.WriteLine(failure.FailureCount);
                    Console.WriteLine();
                }
            }
        }


        private class StructuralEqualityComparer<T> : IEqualityComparer<T> where T : IStructuralEquatable
        {
            public bool Equals(T left, T right) => left.Equals(right, StructuralComparisons.StructuralEqualityComparer);
            public int GetHashCode(T value) => value.GetHashCode(StructuralComparisons.StructuralEqualityComparer);
        }
    }
}
