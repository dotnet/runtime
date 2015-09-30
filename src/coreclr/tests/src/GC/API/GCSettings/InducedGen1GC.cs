// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
