// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal interface IStorePal : IDisposable
    {
        void CloneTo(X509Certificate2Collection collection);
        void Add(ICertificatePal cert);
        void Remove(ICertificatePal cert);
        SafeHandle SafeHandle { get; }
    }
}
