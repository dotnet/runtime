// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tools.RuntimeClient;

namespace Tracing.Tests.Common
{
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
    // to insure that providers have finished being enabled
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
        private SentinelEventSource() {}
        public static SentinelEventSource Log = new SentinelEventSource();
        public void SentinelEvent() { WriteEvent(1, "SentinelEvent"); }
    }

    public static class SessionConfigurationExtensions
    {
        public static SessionConfiguration InjectSentinel(this SessionConfiguration sessionConfiguration)
        {
            var newProviderList = new List<Provider>(sessionConfiguration.Providers);
            newProviderList.Add(new Provider("SentinelEventSource"));
            return new SessionConfiguration(sessionConfiguration.CircularBufferSizeInMB, sessionConfiguration.Format, newProviderList.AsReadOnly());
        }
    }

    public class IpcTraceTest
    {
        // This Action is executed while the trace is being collected.
        private Action _eventGeneratingAction;

        // A dictionary of event providers to number of events.
        // A count of -1 indicates that you are only testing for the presence of the provider
        // and don't care about the number of events sent
        private Dictionary<string, ExpectedEventCount> _expectedEventCounts;
        private Dictionary<string, int> _actualEventCounts = new Dictionary<string, int>();
        private SessionConfiguration _sessionConfiguration;

        // A function to be called with the EventPipeEventSource _before_
        // the call to `source.Process()`.  The function should return another
        // function that will be called to check whether the optional test was validated.
        // Example in situ: providervalidation.cs
        private Func<EventPipeEventSource, Func<int>> _optionalTraceValidator;

        IpcTraceTest(
            Dictionary<string, ExpectedEventCount> expectedEventCounts,
            Action eventGeneratingAction,
            SessionConfiguration? sessionConfiguration = null,
            Func<EventPipeEventSource, Func<int>> optionalTraceValidator = null)
        {
            _eventGeneratingAction = eventGeneratingAction;
            _expectedEventCounts = expectedEventCounts;
            _sessionConfiguration = sessionConfiguration?.InjectSentinel() ?? new SessionConfiguration(
                circularBufferSizeMB: 1000,
                format: EventPipeSerializationFormat.NetTrace,
                providers: new List<Provider> { new Provider("Microsoft-Windows-DotNETRuntime") });
            _optionalTraceValidator = optionalTraceValidator;
        }

        private int Fail(string message = "")
        {
            Console.WriteLine("Test FAILED!");
            Console.WriteLine(message);
            Console.WriteLine("Configuration:");
            Console.WriteLine("{");
            Console.WriteLine($"\tbufferSize: {_sessionConfiguration.CircularBufferSizeInMB},");
            Console.WriteLine("\tproviders: [");
            foreach (var provider in _sessionConfiguration.Providers)
            {
                Console.WriteLine($"\t\t{provider.ToString()},");
            }
            Console.WriteLine("\t]");
            Console.WriteLine("}\n");
            Console.WriteLine("Expected:");
            Console.WriteLine("{");
            foreach (var (k, v) in _expectedEventCounts)
            {
                Console.WriteLine($"\t\"{k}\" = {v}");
            }
            Console.WriteLine("}\n");

            Console.WriteLine("Actual:");
            Console.WriteLine("{");
            foreach (var (k, v) in _actualEventCounts)
            {
                Console.WriteLine($"\t\"{k}\" = {v}");
            }
            Console.WriteLine("}");

            return -1;
        }

        private int Validate()
        {
            var processId = Process.GetCurrentProcess().Id;
            var binaryReader = EventPipeClient.CollectTracing(processId, _sessionConfiguration, out var eventpipeSessionId);
            if (eventpipeSessionId == 0)
                return -1;
            
            // CollectTracing returns before EventPipe::Enable has returned, so the
            // the sources we want to listen for may not have been enabled yet.
            // We'll use this sentinel EventSource to check if Enable has finished
            ManualResetEvent sentinelEventReceived = new ManualResetEvent(false);
            var sentinelTask = new Task(() =>
            {
                while (!sentinelEventReceived.WaitOne(50))
                {
                    SentinelEventSource.Log.SentinelEvent();
                }
            });
            sentinelTask.Start();

            EventPipeEventSource source = null;
            Func<int> optionalTraceValidationCallback = null;
            var readerTask = new Task(() =>
            {
                source = new EventPipeEventSource(binaryReader);
                source.Dynamic.All += (eventData) =>
                {
                    if (eventData.ProviderName == "SentinelEventSource")
                    {
                        sentinelEventReceived.Set();
                    }
                    else if (_actualEventCounts.TryGetValue(eventData.ProviderName, out _))
                    {
                        _actualEventCounts[eventData.ProviderName]++;
                    }
                    else
                    {
                        _actualEventCounts[eventData.ProviderName] = 1;
                    }
                };

                if (_optionalTraceValidator != null)
                {
                    optionalTraceValidationCallback = _optionalTraceValidator(source);
                }

                source.Process();
            });

            readerTask.Start();
            sentinelEventReceived.WaitOne();
            _eventGeneratingAction();
            EventPipeClient.StopTracing(processId, eventpipeSessionId);

            readerTask.Wait();

            foreach (var (provider, expectedCount) in _expectedEventCounts)
            {
                if (_actualEventCounts.TryGetValue(provider, out var actualCount))
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
                return optionalTraceValidationCallback();
            }
            else
            {
                return 100;
            }
        }

        public static int RunAndValidateEventCounts(
            Dictionary<string, ExpectedEventCount> expectedEventCounts,
            Action eventGeneratingAction,
            SessionConfiguration? sessionConfiguration = null,
            Func<EventPipeEventSource, Func<int>> optionalTraceValidator = null)
        {
            Console.WriteLine("TEST STARTING");
            var test = new IpcTraceTest(expectedEventCounts, eventGeneratingAction, sessionConfiguration, optionalTraceValidator);
            try
            {
                var ret = test.Validate();
                if (ret == 100)
                    Console.WriteLine("TEST PASSED!");
                return ret;
            }
            catch (Exception e)
            {
                Console.WriteLine("TEST FAILED!");
                Console.WriteLine(e);
                return -1;
            }
        }
    }
}