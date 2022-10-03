// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace System.Net.Http
{
    internal sealed partial class HttpTelemetry
    {
#pragma warning disable CA1822
        public void Http11RequestLeftQueue(double timeOnQueueMilliseconds)
        {
        }

        public void Http20RequestLeftQueue(double timeOnQueueMilliseconds)
        {
        }
#pragma warning restore CA1822

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
        }
    }
}
