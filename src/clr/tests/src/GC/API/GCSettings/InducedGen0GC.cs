// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime;

namespace GCLatencyTest
{
    public class InducedGen0GC : ILatencyTest
    {
        private int _numGen0Collections = 0;
        public void Test()
        {
            _numGen0Collections = GC.CollectionCount(0);
            GC.Collect(0);
            _numGen0Collections = GC.CollectionCount(0) - _numGen0Collections;
        }

        public void Cleanup()
        {
        }

        public bool Pass(GCLatencyMode gcMode, int numCollections)
        {
            return (_numGen0Collections > 0);
        }
    }
}
