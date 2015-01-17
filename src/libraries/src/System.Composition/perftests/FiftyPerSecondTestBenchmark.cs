// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompositionThroughput
{
    internal class FiftyPerSecondTestBenchmark : Benchmark
    {
        public override bool SelfTest()
        {
            return true;
        }

        public override string Description
        {
            get
            {
                return "Timing test 50/s";
            }
        }

        public override Action GetOperation()
        {
            return () => Thread.Sleep(1000 / 50);
        }
    }
}
