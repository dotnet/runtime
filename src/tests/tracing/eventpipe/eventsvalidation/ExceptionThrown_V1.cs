// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Tracing.Tests.Common;

namespace Tracing.Tests.ExceptionThrown_V1
{
    public class ProviderValidation
    {
        public static int Main(string[] args)
        {
            var providers = new List<Provider>()
            {
                new Provider("Microsoft-DotNETCore-SampleProfiler"),
                //ExceptionKeyword (0x8000): 0b1000_0000_0000_0000
                new Provider("Microsoft-Windows-DotNETRuntime", 0b1000_0000_0000_0000, EventLevel.Warning)
            };

            var configuration = new SessionConfiguration(circularBufferSizeMB: 1024, format: EventPipeSerializationFormat.NetTrace,  providers: providers);
            return IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, configuration);
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
                    Logger.logger.Log($"Thrown an excpetion {i} times...");
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