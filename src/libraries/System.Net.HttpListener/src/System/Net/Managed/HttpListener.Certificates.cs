// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    partial class HttpListener
    {
        internal SslStream CreateSslStream(Stream innerStream, bool ownsStream, RemoteCertificateValidationCallback callback)
        {
            return new SslStream(innerStream, ownsStream, callback);
        }

        internal X509Certificate LoadCertificateAndKey(IPAddress addr, int port)
        {
            // TODO #14691: Implement functionality to read SSL certificate.
            return null;
        }
    }
}
