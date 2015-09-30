// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
