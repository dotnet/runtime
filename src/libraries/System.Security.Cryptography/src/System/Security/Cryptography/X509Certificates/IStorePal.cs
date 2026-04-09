// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates
{
    internal interface IStorePal : IDisposable
    {
        void CloneTo(X509Certificate2Collection collection);
        void Add(ICertificatePal cert);
        void Remove(ICertificatePal cert);
        SafeHandle? SafeHandle { get; }
    }
}
