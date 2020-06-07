// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tracing.Tests.Common;

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Tracing.Tests
{
    public static class TraceControlTest
    {
        public static int Main(string[] args)
        {
            return new TraceControlTraceTest().Execute();
        }
    }

    public class TraceControlTraceTest : AbstractTraceTest
    {
        private bool pass;

        private static string ConfigFileContents = @"
OutputPath=.
CircularMB=2048
Providers=*:0xFFFFFFFFFFFFFFFF:5:
";
        protected override string GetConfigFileContents()
        {
            return ConfigFileContents;
        }

        protected override void InstallValidationCallbacks(TraceEventDispatcher trace)
        {
            string gcReasonInduced = GCReason.Induced.ToString();
            trace.Clr.GCTriggered += delegate (GCTriggeredTraceData data)
            {
                if (gcReasonInduced.Equals(data.Reason.ToString()))
                {
                    Console.WriteLine("Detected an induced GC");
                    pass = true;
                }
            };
        }

        protected override bool Pass()
        {
            return this.pass;
        }
    }
}
