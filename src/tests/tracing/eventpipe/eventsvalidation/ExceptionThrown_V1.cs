// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Collections.Generic;
using Tracing.Tests.Common;
using Microsoft.Diagnostics.NETCore.Client;

namespace Tracing.Tests.ExceptionThrown_V1
{
    public class ProviderValidation
    {
        public static int Main(string[] args)
        {
            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose),
                //ExceptionKeyword (0x8000): 0b1000_0000_0000_0000
                new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Warning, 0b1000_0000_0000_0000)
            };

            return IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, providers, 1024);
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            { "Microsoft-Windows-DotNETRuntime", new ExpectedEventCount(1000, 0.2f) },
            { "Microsoft-Windows-DotNETRuntimeRundown", -1 },
            { "Microsoft-DotNETCore-SampleProfiler", -1 }
        };

        private static Action _eventGeneratingAction = () =>
        {
            for (int i = 0; i < 1000; i++)
            {
                if (i % 100 == 0)
                    Logger.logger.Log($"Thrown an exception {i} times...");
                try
                {
                    throw new ArgumentNullException("Throw ArgumentNullException");
                }
                catch (Exception e)
                {
                    //Do nothing
                }
            }
        };
    }
}
