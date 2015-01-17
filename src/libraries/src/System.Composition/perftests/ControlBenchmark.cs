// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompositionThroughput
{
    internal class ControlBenchmark : Benchmark
    {
        public override Action GetOperation()
        {
            return () => { };
        }

        public override string Description
        {
            get { return "Control (Empty)"; }
        }

        public override bool SelfTest()
        {
            return true;
        }
    }
}
