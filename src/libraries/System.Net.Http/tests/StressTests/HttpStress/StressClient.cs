// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        private const string UNENCRYPTED_HTTP2_ENV_VAR = "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2UNENCRYPTEDSUPPORT";

        private readonly (string name, Func<RequestContext, Task> operation)[] _clientOperations;
        private readonly Uri _baseAddress;
        private readonly Configuration _config;
        private readonly StressResultAggregator _aggregator;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task? _clientTask;

        public long TotalErrorCount => _aggregator.TotalErrorCount;

        public StressClient((string name, Func<RequestContext, Task> operation)[] clientOperations, Configuration configuration)
        {
            _clientOperations = clientOperations;
            _config = configuration;
            _baseAddress = new Uri(configuration.ServerUri);
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
            if (_baseAddress.Scheme == "http")
            {
                Environment.SetEnvironmentVariable(UNENCRYPTED_HTTP2_ENV_VAR, "1");
            }

            HttpMessageHandler CreateHttpHandler()
            {
                if (_config.UseWinHttpHandler)
                {
                    return new System.Net.Http.WinHttpHandler()
                    {
                        ServerCertificateValidationCallback = delegate { return true; }
                    };
                }
                else
                {
                    return new SocketsHttpHandler()
                    {
                        PooledConnectionLifetime = _config.ConnectionLifetime.GetValueOrDefault(Timeout.InfiniteTimeSpan),
                        SslOptions = new SslClientAuthenticationOptions
                        {
                            RemoteCertificateValidationCallback = delegate { return true; }
                        }
                    };
                }
            }

            HttpClient CreateHttpClient() => 
                new HttpClient(CreateHttpHandler()) 
                { 
                    BaseAddress = _baseAddress,
                    Timeout = _config.DefaultTimeout,
                    DefaultRequestVersion = _config.HttpVersion,
                };

            using HttpClient client = CreateHttpClient();

            Console.WriteLine($"Trying connect to the server {_baseAddress}.");

            // Before starting the full-blown test, make sure can communicate with the server
            // Needed for scenaria where we're deploying server & client in separate containers, simultaneously.
            await SendTestRequestToServer(maxRetries: 10);

            Console.WriteLine($"Connected succesfully.");

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

            Console.WriteLine($"Spinning up {_config.ConcurrentRequests} concurrent workers.");

            // Start N workers, each of which sits in a loop making requests.
            Task[] tasks = Enumerable.Range(0, _config.ConcurrentRequests).Select(RunWorker).ToArray();
            await Task.WhenAll(tasks);

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
            
            async Task SendTestRequestToServer(int maxRetries)
            {
                using HttpClient client = CreateHttpClient();
                for (int remainingRetries = maxRetries; ; remainingRetries--)
                {
                    try
                    {
                        await client.GetAsync("/");
                        break;
                    }
                    catch (HttpRequestException) when (remainingRetries > 0)
                    {
                        Console.WriteLine($"Stress client could not connect to host {_baseAddress}, {remainingRetries} attempts remaining");
                        await Task.Delay(millisecondsDelay: 1000);
                    }
                }
            }
        }

        /// <summary>Aggregate view of a particular stress failure type</summary>
        private sealed class StressFailureType
        {
            // Representative error text of stress failure
            public string ErrorText { get; }
            // Operation id => failure timestamps
            public Dictionary<int, List<DateTime>> Failures { get; }

            public StressFailureType(string errorText)
            {
                ErrorText = errorText;
                Failures = new Dictionary<int, List<DateTime>>();
            }

            public int FailureCount => Failures.Values.Select(x => x.Count).Sum();
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

            public long TotalErrorCount => _failures.Sum();

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
                DateTime timestamp = DateTime.Now;
                
                Interlocked.Increment(ref _totalRequests);
                Interlocked.Increment(ref _failures[operationIndex]);

                _latencies.Add(elapsed.TotalMilliseconds);

                RecordFailureType();
                PrintToConsole();

                // record exception according to failure type classification
                void RecordFailureType()
                {
                    (Type, string, string)[] key = ClassifyFailure(exn);

                    StressFailureType failureType = _failureTypes.GetOrAdd(key, _ => new StressFailureType(exn.ToString()));

                    lock (failureType)
                    {
                        if(!failureType.Failures.TryGetValue(operationIndex, out List<DateTime>? timestamps))
                        {
                            timestamps = new List<DateTime>();
                            failureType.Failures.Add(operationIndex, timestamps);
                        }

                        timestamps.Add(timestamp);
                    }

                    (Type exception, string message, string callSite)[] ClassifyFailure(Exception exn)
                    {
                        var acc = new List<(Type exception, string message, string callSite)>();

                        for (Exception? e = exn; e != null; )
                        {
                            acc.Add((e.GetType(), e.Message ?? "", new StackTrace(e, true).GetFrame(0)?.ToString() ?? ""));
                            e = e.InnerException;
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
                    foreach (KeyValuePair<int, List<DateTime>> operation in failure.Failures)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($"\t{_operationNames[operation.Key].PadRight(30)}");
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("Fail: ");
                        Console.ResetColor();
                        Console.Write(operation.Value.Count);
                        Console.WriteLine($"\tTimestamps: {string.Join(", ", operation.Value.Select(x => x.ToString("HH:mm:ss")))}");
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
            public bool Equals(T? left, T? right) => left != null && left.Equals(right, StructuralComparisons.StructuralEqualityComparer);
            public int GetHashCode(T value) => value.GetHashCode(StructuralComparisons.StructuralEqualityComparer);
        }
    }
}
