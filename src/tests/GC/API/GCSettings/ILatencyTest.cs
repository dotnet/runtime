// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;

namespace GCLatencyTest
{
    public interface ILatencyTest
    {
        void Test();
        bool Pass(GCLatencyMode gcMode, int numCollections);
        void Cleanup();
    }
}
