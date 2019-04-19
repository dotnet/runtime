// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class ConcurrentLoadContext
    {
        public enum Mode
        {
            Sync,
            Async,
        }

        public sealed class Category
        {
            public int Id;
            public string Description;
        }

        private readonly DbProviderFactory _providerFactory;
        private readonly string _connectionString;
        private readonly string _query;
        private readonly int _warmupSeconds;
        private readonly int _executionSeconds;
        private readonly Mode _mode;

        private Func<Task> _start;
        private Func<Task> _stop;
        private Func<Task> _work;

        private int _counter;
        private int _totalTransactions;
        private int _threadCount;
        private int _running;
        private List<int> _results;
        private DateTime _startTime;
        private DateTime _stopTime;

        public ConcurrentLoadContext(DbProviderFactory providerFactory, string connectionString, Mode mode, int warmupSeconds, int executionSeconds, int threadCount)
        {
            _providerFactory = providerFactory;
            _connectionString = connectionString;
            _threadCount = threadCount;
            _warmupSeconds = warmupSeconds;
            _executionSeconds = executionSeconds;
            _query = "SELECT CategoryID, Description FROM Categories";
            _results = new List<int>(_executionSeconds);
            _mode = mode;
            _start = Start;
            _stop = Stop;
            switch (_mode)
            {
                case Mode.Sync:
                    _work = DoWorkSync;
                    break;
                case Mode.Async:
                    _work = DoWorkAsync;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        public void IncrementCounter() => Interlocked.Increment(ref _counter);

        public bool IsRunning
        {
            get => _running == 1;
            set => Interlocked.Exchange(ref _running, value ? 1 : 0);
        }

        public Task DoWorkSync()
        {
            while (IsRunning)
            {
                var results = new List<Category>();

                using (var connection = _providerFactory.CreateConnection())
                {
                    connection.ConnectionString = _connectionString;
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = _query;
                        command.Prepare();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(
                                    new Category
                                    {
                                        Id = reader.GetInt32(0),
                                        Description = reader.GetString(1)
                                    });
                            }
                        }
                    }
                }

                CheckResults(results);

                IncrementCounter();
            }

            return Task.CompletedTask;
        }

        public Task DoWorkSyncCaching()
        {
            using (var connection = _providerFactory.CreateConnection())
            {
                connection.ConnectionString = _connectionString;
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = _query;
                    command.Prepare();

                    while (IsRunning)
                    {
                        var results = new List<Category>();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(
                                    new Category
                                    {
                                        Id = reader.GetInt32(0),
                                        Description = reader.GetString(1)
                                    });
                            }
                        }

                        CheckResults(results);

                        IncrementCounter();
                    }
                }
            }

            return Task.CompletedTask;
        }

        public async Task DoWorkAsync()
        {
            while (IsRunning)
            {
                var results = new List<Category>();

                using (var connection = _providerFactory.CreateConnection())
                {
                    connection.ConnectionString = _connectionString;

                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = _query;
                        command.Prepare();

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(
                                    new Category
                                    {
                                        Id = reader.GetInt32(0),
                                        Description = reader.GetString(1)
                                    });
                            }
                        }
                    }
                }

                CheckResults(results);

                IncrementCounter();
            }
        }

        public async Task DoWorkAsyncCaching()
        {
            using (var connection = _providerFactory.CreateConnection())
            {
                connection.ConnectionString = _connectionString;

                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = _query;
                    command.Prepare();

                    while (IsRunning)
                    {
                        var results = new List<Category>();

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(
                                    new Category
                                    {
                                        Id = reader.GetInt32(0),
                                        Description = reader.GetString(1)
                                    });
                            }
                        }

                        CheckResults(results);

                        IncrementCounter();
                    }
                }
            }
        }

        public async Task<(int transactionPerSecond, double average, double stdDeviation)> Run()
        {
            IsRunning = true;

            await Task.WhenAll(CreateTasks());

            _results.Sort();
            _results.RemoveAt(0);
            _results.RemoveAt(_results.Count - 1);

            (double avg, double stdDev) = CalculateStdDev(_results);

            int totalTps = (int)(_totalTransactions / (_stopTime - _startTime).TotalSeconds);

            return (totalTps, avg, stdDev);
        }

        private async Task Start()
        {
            await Task.Delay(TimeSpan.FromSeconds(_warmupSeconds));

            Interlocked.Exchange(ref _counter, 0);

            _startTime = DateTime.UtcNow;
            var lastDisplay = _startTime;

            while (IsRunning)
            {
                await Task.Delay(200);

                DateTime now = DateTime.UtcNow;
                int tps = (int)(_counter / (now - lastDisplay).TotalSeconds);

                _results.Add(tps);

                lastDisplay = now;
                _totalTransactions += Interlocked.Exchange(ref _counter, 0);
            }
        }

        private async Task Stop()
        {
            await Task.Delay(TimeSpan.FromSeconds(_warmupSeconds + _executionSeconds));
            Interlocked.Exchange(ref _running, 0);
            _stopTime = DateTime.UtcNow;
        }

        private IEnumerable<Task> CreateTasks()
        {
            yield return Task.Run(_start);

            yield return Task.Run(_stop);

            foreach (var task in Enumerable.Range(0, _threadCount)
                .Select(_ => Task.Factory.StartNew(_work, TaskCreationOptions.LongRunning).Unwrap()))
            {
                yield return task;
            }
        }

        private static void CheckResults(ICollection<Category> results)
        {
            Assert.NotNull(results);
            Assert.Equal(8, results.Count);
        }

        private static (double, double) CalculateStdDev(ICollection<int> values)
        {
            double avg = values.Average();
            double sum = values.Sum(d => Math.Pow(d - avg, 2));

            return (avg, Math.Sqrt(sum / values.Count));
        }
    }    
}
