// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SslStress.Utils;

namespace SslStress
{

    public abstract partial class SslClientBase : IAsyncDisposable
    {
        protected readonly Configuration _config;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly StressResultAggregator _aggregator;
        private readonly Lazy<Task> _clientTask;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public SslClientBase(Configuration config)
        {
            if (config.MaxConnections < 1) throw new ArgumentOutOfRangeException(nameof(config.MaxConnections));

            _config = config;
            _aggregator = new StressResultAggregator(config.MaxConnections);
            _clientTask = new Lazy<Task>(Task.Run(StartCore));
        }

        protected abstract Task HandleConnection(int workerId, SslStream stream, TcpClient client, Random random, CancellationToken token);

        protected virtual async Task<SslStream> EstablishSslStream(Stream networkStream, Random random, CancellationToken token)
        {
            var sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false);
            var clientOptions = new SslClientAuthenticationOptions
            {
                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 },
                RemoteCertificateValidationCallback = ((x, y, z, w) => true),
                TargetHost = SslServerBase.Hostname,
            };

            await sslStream.AuthenticateAsClientAsync(clientOptions, token);
            return sslStream;
        }

        public ValueTask DisposeAsync() => StopAsync();

        public void Start()
        {
            if (_cts.IsCancellationRequested) throw new ObjectDisposedException(nameof(SslClientBase));
            _ = _clientTask.Value;
        }

        public async ValueTask StopAsync()
        {
            _cts.Cancel();
            await _clientTask.Value;
        }

        public Task Task
        {
            get
            {
                if (!_clientTask.IsValueCreated) throw new InvalidOperationException("Client has not been started yet");
                return _clientTask.Value;
            }
        }

        public long TotalErrorCount => _aggregator.TotalFailures;

        private Task StartCore()
        {
            _stopwatch.Start();

            // Spin up a thread dedicated to outputting stats for each defined interval
            new Thread(() =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    Thread.Sleep(_config.DisplayInterval);
                    lock (Console.Out) { _aggregator.PrintCurrentResults(_stopwatch.Elapsed, showAggregatesOnly: false); }
                }
            })
            { IsBackground = true }.Start();

            IEnumerable<Task> workers = CreateWorkerSeeds().Select(x => RunSingleWorker(x.workerId, x.random));
            return Task.WhenAll(workers);

            async Task RunSingleWorker(int workerId, Random random)
            {
                StreamCounter counter = _aggregator.GetCounters(workerId);

                for (long testId = 0; !_cts.IsCancellationRequested; testId++)
                {
                    TimeSpan duration = _config.MinConnectionLifetime + random.NextDouble() * (_config.MaxConnectionLifetime - _config.MinConnectionLifetime);
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                    cts.CancelAfter(duration);

                    try
                    {
                        using var client = new TcpClient();
                        await client.ConnectAsync(_config.ServerEndpoint.Address, _config.ServerEndpoint.Port);
                        var stream = new CountingStream(client.GetStream(), counter);
                        using SslStream sslStream = await EstablishSslStream(stream, random, cts.Token);
                        await HandleConnection(workerId, sslStream, client, random, cts.Token);

                        _aggregator.RecordSuccess(workerId);
                    }
                    catch (OperationCanceledException) when (cts.IsCancellationRequested)
                    {
                        _aggregator.RecordSuccess(workerId);
                    }
                    catch (Exception e)
                    {
                        _aggregator.RecordFailure(workerId, e);
                    }
                }
            }

            IEnumerable<(int workerId, Random random)> CreateWorkerSeeds()
            {
                // deterministically generate random instance for each individual worker
                Random random = new Random(_config.RandomSeed);
                for (int workerId = 0; workerId < _config.MaxConnections; workerId++)
                {
                    yield return (workerId, random.NextRandom());
                }
            }
        }

        public void PrintFinalReport()
        {
            lock (Console.Out)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("SslStress Run Final Report");
                Console.WriteLine();

                _aggregator.PrintCurrentResults(_stopwatch.Elapsed, showAggregatesOnly: true);
                _aggregator.PrintFailureTypes();
            }
        }
    }
}
