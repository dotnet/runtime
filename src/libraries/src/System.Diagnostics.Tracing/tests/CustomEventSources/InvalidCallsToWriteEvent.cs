// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;

// We wish to test both Microsoft.Diagnostics.Tracing (Nuget)
// and System.Diagnostics.Tracing (Framewwork), we use this Ifdef make each kind 

namespace SdtEventSources
{
    public sealed class InvalidCallsToWriteEventEventSource : EventSource
    {
        public void WriteTooFewArgs(int m, int n)
        {
            WriteEvent(1, m);
        }

        public void WriteTooManyArgs(string msg)
        {
            WriteEvent(2, msg, "-");
        }
    }
}
