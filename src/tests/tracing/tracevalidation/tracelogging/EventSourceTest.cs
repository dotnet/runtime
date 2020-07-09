// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace Tracing.Tests.Common
{
    public sealed class EventSourceTestSuite
    {
        private NetPerfFile m_file;
        private List<EventSource> m_eventSources = new List<EventSource>();
        private List<EventSourceTest> m_tests = new List<EventSourceTest>();
        private int m_nextTestVerificationIndex = 0;

        public EventSourceTestSuite(
            NetPerfFile file)
        {
            if(file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            m_file = file;
        }

        public void AddEventSource(EventSource source)
        {
            if(source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            m_eventSources.Add(source);
        }

        public void AddTest(EventSourceTest test)
        {
            if(test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }
            m_tests.Add(test);
        }

        public void RunTests()
        {
            // Get the configuration and start tracing.
            TraceConfiguration traceConfig = GenerateConfiguration();
            TraceControl.Enable(traceConfig);

            // Run the tests.
            foreach(EventSourceTest test in m_tests)
            {
                test.LogEvent();
            }

            // Stop tracing.
            TraceControl.Disable();

            // Open the trace file.
            string traceLogPath = TraceLog.CreateFromEventPipeDataFile(m_file.Path);
            using(TraceLog traceLog = new TraceLog(traceLogPath))
            {
                TraceEventDispatcher dispatcher = traceLog.Events.GetSource();

                dispatcher.Dynamic.All += delegate(TraceEvent data)
                {
                    if(data.ProviderName.EndsWith("Rundown"))
                    {
                        return;
                    }

                    if(data.ProviderName.Equals("Microsoft-DotNETCore-EventPipe"))
                    {
                        return;
                    }

                    Assert.True($"m_nextTestVerificationIndex({m_nextTestVerificationIndex}) < m_tests.Count({m_tests.Count})", m_nextTestVerificationIndex < m_tests.Count);
                    try
                    {
                        Console.WriteLine($"Verifying Event: {data.ToString()}");
                        m_tests[m_nextTestVerificationIndex].VerifyEvent(data);
                    }
                    catch
                    {
                        Console.WriteLine($"Failure during test '{m_tests[m_nextTestVerificationIndex].Name}'.");
                        throw;
                    }

                    m_nextTestVerificationIndex++;
                };

                dispatcher.Process();
                Assert.Equal("Test Count", m_tests.Count, m_nextTestVerificationIndex);
            }
        }

        private TraceConfiguration GenerateConfiguration()
        {
            uint circularBufferMB = 1024; // 1 GB

            TraceConfiguration traceConfig = new TraceConfiguration(m_file.Path, circularBufferMB);

            // Add each of the registered EventSources.
            foreach(EventSource source in m_eventSources)
            {
                traceConfig.EnableProvider(
                    source.Name,        // ProviderName
                    0xFFFFFFFFFFFFFFFF, // Keywords
                    5);                 // Level
            }

            return traceConfig;
        }
    }

    public delegate void LogEventDelegate();
    public delegate void VerifyEventDelegate(TraceEvent eventData);

    public sealed class EventSourceTest
    {
        private string m_name;
        private LogEventDelegate m_logEvent;
        private VerifyEventDelegate m_verifyEvent;

        public EventSourceTest(
            string name,
            LogEventDelegate logEvent,
            VerifyEventDelegate verifyEvent)
        {
            if(String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if(logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }
            if(verifyEvent == null)
            {
                throw new ArgumentNullException(nameof(verifyEvent));
            }

            m_name = name;
            m_logEvent = logEvent;
            m_verifyEvent = verifyEvent;
        }
        
        public string Name
        {
            get { return m_name; }
        }

        public void LogEvent()
        {
            m_logEvent();
        }

        public void VerifyEvent(TraceEvent eventData)
        {
            m_verifyEvent(eventData);
        }
    }
}
