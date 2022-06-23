// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    public partial class HttpListener
    {
        internal static SslStream CreateSslStream(Stream innerStream, bool ownsStream, RemoteCertificateValidationCallback callback)
        {
            return new SslStream(innerStream, ownsStream, callback);
        }

        internal static X509Certificate? LoadCertificateAndKey(IPAddress addr, int port)
        {
            // TODO https://github.com/dotnet/runtime/issues/19752: Implement functionality to read SSL certificate.
            return null;
        }
    }
}
