// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public abstract partial class HttpClientHandler_SslProtocols_Test
    {
        private bool BackendSupportsSslConfiguration =>
            UseSocketsHttpHandler || TestHelper.NativeHandlerSupportsSslConfiguration();
    }
}
