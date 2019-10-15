// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime;

namespace GCLatencyTest
{
    public class InducedGen1GC : ILatencyTest
    {
        private int _numGen1Collections = 0;
        public void Test()
        {
            _numGen1Collections = GC.CollectionCount(1);
            GC.Collect(1);
            _numGen1Collections = GC.CollectionCount(1) - _numGen1Collections;
        }

        public void Cleanup()
        {
        }

        public bool Pass(GCLatencyMode gcMode, int numCollections)
        {
            return (_numGen1Collections > 0);
        }
    }
}
