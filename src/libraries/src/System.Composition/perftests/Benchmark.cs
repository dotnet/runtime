// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompositionThroughput
{
    internal abstract class Benchmark
    {
        public abstract Action GetOperation();
        public abstract bool SelfTest();
        public virtual string Description { get { return GetType().Name.Replace("Benchmark", ""); } }
        public virtual Version Version { get { return new Version(0, 0); } }
    }
}
