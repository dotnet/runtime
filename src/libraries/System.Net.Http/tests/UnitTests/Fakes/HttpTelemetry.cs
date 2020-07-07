// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    internal class HttpTelemetry
    {
        public static HttpTelemetry Log => new HttpTelemetry();

        public void RequestStop() { }

        public void RequestAborted() { }
    }
}
