// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    public class HttpTelemetry
    {
        public static readonly HttpTelemetry Log = new HttpTelemetry();

        public bool IsEnabled() => false;

        public void RequestStop() { }

        public void RequestAborted() { }
    }
}
