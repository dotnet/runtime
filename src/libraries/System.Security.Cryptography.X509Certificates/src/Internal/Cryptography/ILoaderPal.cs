// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal interface ILoaderPal : IDisposable
    {
        void MoveTo(X509Certificate2Collection collection);
    }
}
