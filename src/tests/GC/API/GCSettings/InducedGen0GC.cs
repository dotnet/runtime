// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
