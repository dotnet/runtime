// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
        private readonly CancellationTokenSource _cts;
        private readonly Task _clientTask;

        // TOCONSIDER: configuration class to avoid threading so many parameters
        public StressClient(Uri serverUri, (string name, Func<RequestContext, Task> operation)[] clientOperations, int concurrentRequests,
                                int maxContentLength, int maxRequestParameters, int maxRequestUriSize,
                                int randomSeed, double cancellationProbability, double http2Probability,
                                int? connectionLifetime, TimeSpan displayInterval)
        {
            _cts = new CancellationTokenSource();
            _clientTask = RunClient();

            async Task RunClient()
            {
                var handler = new SocketsHttpHandler()
                {
                    PooledConnectionLifetime = connectionLifetime.HasValue ? TimeSpan.FromMilliseconds(connectionLifetime.Value) : Timeout.InfiniteTimeSpan,
                    SslOptions = new SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = delegate { return true; }
                    }
                };

                string contentSource = string.Concat(Enumerable.Repeat("1234567890", maxContentLength / 10));

                using (var client = new HttpClient(handler) { BaseAddress = serverUri })
                {
                    // Track all successes and failures
                    long total = 0;
                    long[] success = new long[clientOperations.Length], cancel = new long[clientOperations.Length], fail = new long[clientOperations.Length];
                    long reuseAddressFailure = 0;

                    void Increment(ref long counter)
                    {
                        Interlocked.Increment(ref counter);
                        Interlocked.Increment(ref total);
                    }

                    // Spin up a thread dedicated to outputting stats for each defined interval
                    new Thread(() =>
                    {
                        long lastTotal = 0;
                        while (true)
                        {
                            Thread.Sleep(displayInterval);
                            lock (Console.Out)
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.Write("[" + DateTime.Now + "]");
                                Console.ResetColor();

                                if (lastTotal == total)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                }
                                lastTotal = total;
                                Console.WriteLine(" Total: " + total.ToString("N0"));
                                Console.ResetColor();

                                if (reuseAddressFailure > 0)
                                {
                                    Console.ForegroundColor = ConsoleColor.DarkRed;
                                    Console.WriteLine("~~ Reuse address failures: " + reuseAddressFailure.ToString("N0") + "~~");
                                    Console.ResetColor();
                                }

                                for (int i = 0; i < clientOperations.Length; i++)
                                {
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.Write("\t" + clientOperations[i].Item1.PadRight(30));
                                    Console.ResetColor();
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.Write("Success: ");
                                    Console.Write(success[i].ToString("N0"));
                                    Console.ResetColor();
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.Write("\tCanceled: ");
                                    Console.Write(cancel[i].ToString("N0"));
                                    Console.ResetColor();
                                    Console.ForegroundColor = ConsoleColor.DarkRed;
                                    Console.Write("\tFail: ");
                                    Console.ResetColor();
                                    Console.WriteLine(fail[i].ToString("N0"));
                                }
                                Console.WriteLine();
                            }
                        }
                    })
                    { IsBackground = true }.Start();

                    // Request context factory to be applied in the context of a single worker
                    Func<RequestContext> CreateRequestContextFactory(int taskNum)
                    {
                        // Creates a System.Random instance that is specific to the current client job
                        // Generated using the global seed and the task index
                        Random CreateRandomInstance()
                        {
                            // deterministic hashing copied from System.Runtime.Hashing
                            int Combine(int h1, int h2)
                            {
                                uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
                                return ((int)rol5 + h1) ^ h2;
                            }

                            return new Random(Seed: Combine(taskNum, randomSeed));
                        }

                        // Random instance should be shared across all requests made by same worker
                        Random random = CreateRandomInstance();

                        return () => new RequestContext(client, random, taskNum, contentSource, maxRequestParameters, maxRequestUriSize, cancellationProbability, http2Probability);
                    }

                    async Task RunWorker(int taskNum)
                    {
                        var contextFactory = CreateRequestContextFactory(taskNum);

                        for (long i = taskNum; ; i++)
                        {
                            if (_cts.IsCancellationRequested)
                                break;

                            long opIndex = i % clientOperations.Length;
                            (string operation, Func<RequestContext, Task> func) = clientOperations[opIndex];
                            var requestContext = contextFactory();
                            try
                            {
                                await func(requestContext);

                                Increment(ref success[opIndex]);
                            }
                            catch (OperationCanceledException) when (requestContext.IsCancellationRequested)
                            {
                                Increment(ref cancel[opIndex]);
                            }
                            catch (Exception e)
                            {
                                Increment(ref fail[opIndex]);

                                if (e is HttpRequestException hre && hre.InnerException is SocketException se && se.SocketErrorCode == SocketError.AddressAlreadyInUse)
                                {
                                    Interlocked.Increment(ref reuseAddressFailure);
                                }
                                else
                                {
                                    lock (Console.Out)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine($"Error from iteration {i} ({operation}) in task {taskNum} with {success.Sum()} successes / {fail.Sum()} fails:");
                                        Console.ResetColor();
                                        Console.WriteLine(e);
                                        Console.WriteLine();
                                    }
                                }
                            }
                        }
                    }

                    // Start N workers, each of which sits in a loop making requests.
                    Task[] tasks = Enumerable.Range(0, concurrentRequests).Select(RunWorker).ToArray();
                    await Task.WhenAll(tasks);
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel(); _clientTask.Wait();
        }
    }
}
