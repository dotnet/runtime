// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
