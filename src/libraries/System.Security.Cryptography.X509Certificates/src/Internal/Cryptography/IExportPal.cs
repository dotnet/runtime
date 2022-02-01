// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal interface IExportPal : IDisposable
    {
        byte[]? Export(X509ContentType contentType, SafePasswordHandle password);
    }
}
