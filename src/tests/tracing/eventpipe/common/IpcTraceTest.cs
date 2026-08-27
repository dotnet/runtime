// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;

namespace Tracing.Tests.Common
{
    public class Logger
    {
        public static Logger logger = new();
        private TextWriter _log;
        private Stopwatch _sw;
        public Logger(TextWriter log = null)
        {
            _log = log ?? Console.Out;
            _sw = new Stopwatch();
        }

        public void Log(string message)
        {
            if (!_sw.IsRunning)
            {
                _sw.Start();
            }

            _log.WriteLine($"{_sw.Elapsed.TotalSeconds,5:f1}s: {message}");
        }
    }

    public class ExpectedEventCount
    {
        // The acceptable percent error on the expected value
        // represented as a floating point value in [0,1].
        public float Error { get; private set; }

        // The expected count of events. A value of -1 indicates
        // that count does not matter, and we are simply testing
        // that the provider exists in the trace.
        public int Count { get; private set; }

        public ExpectedEventCount(int count, float error = 0.0f)
        {
            Count = count;
            Error = error;
        }

        public bool Validate(int actualValue)
        {
            return Count == -1 || CheckErrorBounds(actualValue);
        }

        public bool CheckErrorBounds(int actualValue)
        {
            return Math.Abs(actualValue - Count) <= (Count * Error);
        }

        public static implicit operator ExpectedEventCount(int i)
        {
            return new ExpectedEventCount(i);
        }

        public override string ToString()
        {
            return $"{Count} +- {Count * Error}";
        }
    }

    // This event source is used by the test infra to
    // ensure that providers have finished being enabled
    // for the session being observed. Since the client API
    // returns the pipe for reading _before_ it finishes
    // enabling the providers to write to that session,
    // we need to guarantee that our providers are on before
    // sending events. This is a _unique_ problem I imagine
    // should _only_ affect scenarios like these tests
    // where the reading and sending of events are required
    // to synchronize.
    public sealed class SentinelEventSource : EventSource
    {
        private SentinelEventSource() { }
        public static SentinelEventSource Log = new();
        public void SentinelEvent() { WriteEvent(1, "SentinelEvent"); }
    }

    public class IpcTraceTest
    {
        // This Action is executed while the trace is being collected.
        private Action _eventGeneratingAction;

        // A dictionary of event providers to number of events.
        // A count of -1 indicates that you are only testing for the presence of the provider
        // and don't care about the number of events sent
        private Dictionary<string, ExpectedEventCount> _expectedEventCounts;
        private Dictionary<string, int> _actualEventCounts = new();
        private int _droppedEvents;

        // A function to be called with the EventPipeEventSource _before_
        // the call to `source.Process()`.  The function should return another
        // function that will be called to check whether the optional test was validated.
        // Example in situ: providervalidation.cs
        private Func<EventPipeEventSource, Func<int>> _optionalTraceValidator;

        /// <summary>
        /// This is list of the EventPipe providers to turn on for the test execution
        /// </summary>
        private List<EventPipeProvider> _testProviders;

        /// <summary>
        /// This represents the current EventPipeSession
        /// </summary>
        private EventPipeSession _eventPipeSession;

        // The buffer size requested for a session for storing events.
        private int _circularBufferMB;

        // Controls event writing behavior when buffers are full. Drop the event or Block until there is space available.
        private EventPipeBufferingMode _bufferingMode;

        // Whether to fail the test if any events are lost. Only Block buffer mode tests are required to retain all events.
        private bool _failOnEventsLost;

        /// <summary>
        /// This is the list of EventPipe providers for the sentinel EventSource that indicates that the process is ready
        /// </summary>
        private List<EventPipeProvider> _sentinelProviders = new()
        {
            new EventPipeProvider("SentinelEventSource", EventLevel.Verbose, -1)
        };

        private IpcTraceTest(
            Dictionary<string, ExpectedEventCount> expectedEventCounts,
            Action eventGeneratingAction,
            List<EventPipeProvider> providers,
            int circularBufferMB,
            Func<EventPipeEventSource, Func<int>> optionalTraceValidator = null,
            EventPipeBufferingMode bufferingMode = EventPipeBufferingMode.Drop,
            bool failOnEventsLost = false)
        {
            _eventGeneratingAction = eventGeneratingAction;
            _expectedEventCounts = expectedEventCounts;
            _testProviders = providers;
            _circularBufferMB = circularBufferMB;
            _optionalTraceValidator = optionalTraceValidator;
            _bufferingMode = bufferingMode;
            _failOnEventsLost = failOnEventsLost;
        }

        private int Fail(string message = "")
        {
            Logger.logger.Log("Test FAILED!");
            Logger.logger.Log(message);
            Logger.logger.Log("Configuration:");
            Logger.logger.Log("{");
            Logger.logger.Log("\tproviders: [");
            Logger.logger.Log("\t]");
            Logger.logger.Log("}\n");
            Logger.logger.Log("Expected:");
            Logger.logger.Log("{");
            foreach ((string k, ExpectedEventCount v) in _expectedEventCounts)
            {
                Logger.logger.Log($"\t\"{k}\" = {v}");
            }
            Logger.logger.Log("}\n");

            Logger.logger.Log("Actual:");
            Logger.logger.Log("{");
            foreach ((string k, int v) in _actualEventCounts)
            {
                Logger.logger.Log($"\t\"{k}\" = {v}");
            }
            Logger.logger.Log("}");

            return -1;
        }

        private int Validate(bool enableRundownProvider = true)
        {
            // FIXME: This is a bandaid fix for a deadlock in EventPipeEventSource caused by
            // the lazy caching in the Regex library.  The caching creates a ConcurrentDictionary
            // and because it is the first one in the process, it creates an EventSource which
            // results in a deadlock over a lock in EventPipe.  These lines should be removed once the
            // underlying issue is fixed by forcing these events to try to be written _before_ we shutdown.
            //
            // see: https://github.com/dotnet/runtime/pull/1794 for details on the issue
            //
            ConcurrentDictionary<string, string> emptyConcurrentDictionary = new();
            emptyConcurrentDictionary["foo"] = "bar";
            int __count = emptyConcurrentDictionary.Count;

            bool isClean = IpcTraceTest.EnsureCleanEnvironment();
            if (!isClean)
            {
                return -1;
            }
            // CollectTracing returns before EventPipe::Enable has returned, so the
            // the sources we want to listen for may not have been enabled yet.
            // We'll use this sentinel EventSource to check if Enable has finished
            ManualResetEvent sentinelEventReceived = new(false);
            Task sentinelTask = new(() => {
                Logger.logger.Log("Started sending sentinel events...");
                while (!sentinelEventReceived.WaitOne(50))
                {
                    SentinelEventSource.Log.SentinelEvent();
                }
                Logger.logger.Log("Stopped sending sentinel events");
            });
            sentinelTask.Start();

            int processId = Process.GetCurrentProcess().Id;
            object threadSync = new(); // for locking eventpipeSession access
            Func<int> optionalTraceValidationCallback = null;
            DiagnosticsClient client = new(processId);
#if DIAGNOSTICS_RUNTIME
            if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
            {
                client = new DiagnosticsClient(new IpcEndpointConfig("127.0.0.1:9000", IpcEndpointConfig.TransportType.TcpSocket, IpcEndpointConfig.PortType.Listen));
            }
#endif
            Task readerTask = new(() => {
                Logger.logger.Log("Connecting to EventPipe...");
                try
                {
                    EventPipeSessionConfiguration config = new(
                        _testProviders.Concat(_sentinelProviders),
                        circularBufferSizeMB: _circularBufferMB,
                        rundownKeyword: enableRundownProvider ? EventPipeSession.DefaultRundownKeyword : 0,
                        requestStackwalk: true,
                        bufferingMode: _bufferingMode);
                    _eventPipeSession = client.StartEventPipeSession(config);
                }
                catch (DiagnosticsClientException ex)
                {
                    Logger.logger.Log("Failed to connect to EventPipe!");
                    Logger.logger.Log(ex.ToString());
                    throw new ApplicationException("Failed to connect to EventPipe");
                }

                using StreamProxy eventPipeStream = new(_eventPipeSession.EventStream);
                Logger.logger.Log("Creating EventPipeEventSource...");
                using EventPipeEventSource source = new(eventPipeStream);
                Logger.logger.Log("EventPipeEventSource created");

                source.Dynamic.All += (eventData) => {
                    try
                    {
                        if (eventData.ProviderName == "SentinelEventSource")
                        {
                            if (!sentinelEventReceived.WaitOne(0))
                            {
                                Logger.logger.Log("Saw sentinel event");
                            }

                            sentinelEventReceived.Set();
                        }

                        else if (_actualEventCounts.TryGetValue(eventData.ProviderName, out _))
                        {
                            _actualEventCounts[eventData.ProviderName]++;
                        }
                        else
                        {
                            Logger.logger.Log($"Saw new provider '{eventData.ProviderName}'");
                            _actualEventCounts[eventData.ProviderName] = 1;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.logger.Log("Exception in Dynamic.All callback " + e.ToString());
                    }
                };
                Logger.logger.Log("Dynamic.All callback registered");

                if (_optionalTraceValidator != null)
                {
                    Logger.logger.Log("Running optional trace validator");
                    optionalTraceValidationCallback = _optionalTraceValidator(source);
                    Logger.logger.Log("Finished running optional trace validator");
                }

                Logger.logger.Log("Starting stream processing...");
                try
                {
                    source.Process();
                    _droppedEvents = source.EventsLost;
                }
                catch (Exception)
                {
                    Logger.logger.Log($"Exception thrown while reading; dumping culprit stream to disk...");
                    eventPipeStream.DumpStreamToDisk();
                    // rethrow it to fail the test
                    throw;
                }
                Logger.logger.Log("Stopping stream processing");
                Logger.logger.Log($"Dropped {source.EventsLost} events");
            });

            Task waitSentinelEventTask = new(() => {
                sentinelEventReceived.WaitOne();
            });

            readerTask.Start();
            waitSentinelEventTask.Start();

            // Runtime delta (dotnet/runtime#47529): wait on either task so a reader that faults during connect
            // (before signaling the sentinel) surfaces its exception instead of hanging here forever.
            Task.WaitAny(readerTask, waitSentinelEventTask);
            if (readerTask.IsCompleted)
            {
                sentinelEventReceived.Set();
                sentinelTask.Wait();
                readerTask.GetAwaiter().GetResult();
                return Fail("Reader task completed before event generation");
            }

            Logger.logger.Log("Starting event generating action...");
            _eventGeneratingAction();
            Logger.logger.Log("Stopping event generating action");

            // Should throw if the reader task throws any exceptions
            CancellationTokenSource tokenSource = new();
            CancellationToken ct = tokenSource.Token;
            readerTask.ContinueWith((task) => {
                // if our reader task died earlier, we need to break the infinite wait below.
                // We'll allow the AggregateException to be thrown and fail the test though.
                Logger.logger.Log($"Task stats: isFaulted: {task.IsFaulted}, Exception == null: {task.Exception == null}");
                if (task.IsFaulted || task.Exception != null)
                {
                    tokenSource.Cancel();
                }

                return task;
            });

            Task stopTask = Task.Run(() => {
                Logger.logger.Log("Sending StopTracing command...");
                lock (threadSync) // eventpipeSession
                {
                    _eventPipeSession.Stop();
                }
                Logger.logger.Log("Finished StopTracing command");
            }, ct);

            try
            {
                Task.WaitAll(new Task[] { readerTask, stopTask }, ct);
            }
            catch (OperationCanceledException)
            {
                Logger.logger.Log($"A task faulted");
                Logger.logger.Log($"\treaderTask.IsFaulted = {readerTask.IsFaulted}");
                if (readerTask.Exception != null)
                {
                    throw readerTask.Exception;
                }
                return -1;
            }

            Logger.logger.Log("Reader task finished");
            Logger.logger.Log($"Dropped {_droppedEvents} events");

            if (_failOnEventsLost && _droppedEvents > 0)
            {
                return Fail($"Expected zero dropped events, but the reader reported {_droppedEvents} dropped events");
            }

            foreach ((string provider, ExpectedEventCount expectedCount) in _expectedEventCounts)
            {
                if (_actualEventCounts.TryGetValue(provider, out int actualCount))
                {
                    if (!expectedCount.Validate(actualCount))
                    {
                        return Fail($"Event count mismatch for provider \"{provider}\": expected {expectedCount}, but saw {actualCount}");
                    }
                }
                else
                {
                    return Fail($"No events for provider \"{provider}\"");
                }
            }

            if (optionalTraceValidationCallback != null)
            {
                Logger.logger.Log("Validating optional callback...");
                // reader thread should be dead now, no need to lock
                return optionalTraceValidationCallback();
            }
            else
            {
                return 100;
            }
        }

        // Ensure that we have a clean environment for running the test.
        // Specifically check that we don't have more than one match for
        // Diagnostic IPC sockets in the TempPath.  These can be left behind
        // by bugs, catastrophic test failures, etc. from previous testing.
        // The tmp directory is only cleared on reboot, so it is possible to
        // run into these zombie pipes if there are failures over time.
        // Note: Windows has some guarantees about named pipes not living longer
        // the process that created them, so we don't need to check on that platform.
        // Runtime delta: diagnosticport performs this check before running its test cases.
        public static bool EnsureCleanEnvironment()
        {
            if (!OperatingSystem.IsWindows() && !OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi() && !OperatingSystem.IsIOS() && !OperatingSystem.IsTvOS())
            {
                Func<(IEnumerable<IGrouping<int, FileInfo>>, List<int>)> GetPidsAndSockets = () =>
                {
                    IEnumerable<IGrouping<int, FileInfo>> currentIpcs = Directory.GetFiles(Path.GetTempPath(), "dotnet-diagnostic*")
                        .Select(filename =>
                        {
                            Match match = Regex.Match(filename, @"dotnet-diagnostic-(?<pid>\d+)");
                            if (match.Success && match.Groups["pid"].Success && !string.IsNullOrEmpty(match.Groups["pid"].Value))
                            {
                                return new { pid = int.Parse(match.Groups["pid"].Value), fileInfo = new FileInfo(filename) };
                            }
                            return null;
                        })
                        .Where(fileInfoGroup => fileInfoGroup is not null)
                        .GroupBy(fileInfos => fileInfos.pid, fileInfos => fileInfos.fileInfo);
                    List<int> currentPids = System.Diagnostics.Process.GetProcesses().Select(pid => pid.Id).ToList();
                    return (currentIpcs, currentPids);
                };

                var (currentIpcs, currentPids) = GetPidsAndSockets();

                foreach (var ipc in currentIpcs)
                {
                    if (!currentPids.Contains(ipc.Key))
                    {
                        foreach (FileInfo fi in ipc)
                        {
                            Logger.logger.Log($"Attempting to delete the zombied pipe: {fi.FullName}");
                            fi.Delete();
                            Logger.logger.Log($"Deleted");
                        }
                    }
                    else
                    {
                        if (ipc.Count() > 1)
                        {
                            // delete zombied pipes except newest which is owned
                            var duplicates = ipc.OrderBy(fileInfo => fileInfo.CreationTime.Ticks).SkipLast(1);
                            foreach (FileInfo fi in duplicates)
                            {
                                Logger.logger.Log($"Attempting to delete the zombied pipe: {fi.FullName}");
                                fi.Delete();
                            }
                        }
                    }
                }
            }

            return true;
        }

        public static int RunAndValidateEventCounts(
            Dictionary<string, ExpectedEventCount> expectedEventCounts,
            Action eventGeneratingAction,
            List<EventPipeProvider> providers,
            int circularBufferMB = 1024,
            Func<EventPipeEventSource, Func<int>> optionalTraceValidator = null,
            bool enableRundownProvider = true,
            EventPipeBufferingMode bufferingMode = EventPipeBufferingMode.Drop,
            bool failOnEventsLost = false)
        {
            Logger.logger.Log("==TEST STARTING==");
            IpcTraceTest test = new(expectedEventCounts, eventGeneratingAction, providers, circularBufferMB, optionalTraceValidator, bufferingMode, failOnEventsLost);
            // Runtime delta: surface a clean failure (and log the exception) instead of letting it propagate.
            try
            {
                int ret = test.Validate(enableRundownProvider);
                if (ret == 100)
                {
                    Logger.logger.Log("==TEST FINISHED: PASSED!==");
                }
                else
                {
                    Logger.logger.Log("==TEST FINISHED: FAILED!==");
                }

                return ret;
            }
            catch (Exception e)
            {
                Logger.logger.Log(e.ToString());
                Logger.logger.Log("==TEST FINISHED: FAILED!==");
                return -1;
            }
        }
    }
}
