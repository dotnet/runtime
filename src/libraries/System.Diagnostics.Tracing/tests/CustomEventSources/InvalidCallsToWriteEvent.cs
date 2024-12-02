// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

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
