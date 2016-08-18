// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DPStressHarness
{
    public class PerfCounters
    {
        private long _requestsCounter;
        //private long rpsCounter;

        private long _exceptionsCounter;
        //private long epsCounter;

        public PerfCounters()
        {

        }

        public void IncrementRequestsCounter()
        {
            _requestsCounter++;
        }

        public void IncrementExceptionsCounter()
        {
            _exceptionsCounter++;
        }
    }
}
