// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
