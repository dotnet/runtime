// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http.Functional.Tests
{
    public partial class HttpClientHandler_ServerCertificates_Test
    {
        private static bool ShouldSuppressRevocationException => false;

        internal bool BackendSupportsCustomCertificateHandling => true;

        private bool BackendDoesNotSupportCustomCertificateHandling => !BackendSupportsCustomCertificateHandling;
    }
}
