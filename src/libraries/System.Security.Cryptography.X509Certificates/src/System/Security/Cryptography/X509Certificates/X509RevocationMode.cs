// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    public enum X509RevocationMode
    {
        NoCheck = 0,
        Online = 1,
        Offline = 2,
    }
}
