// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;

namespace GCLatencyTest
{
    public class InducedGen2GC : ILatencyTest
    {
        public void Test()
        {
            GC.Collect();
        }

        public void Cleanup()
        {
        }

        public bool Pass(GCLatencyMode gcMode, int numCollections)
        {
            return (numCollections > 0);
        }
    }
}
