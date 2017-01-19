// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.DirectoryServices.AccountManagement
{
    internal enum SidType
    {
        RealObject = 0,        // Account SID (S-1-5-21-....)
        RealObjectFakeDomain = 1,        // BUILTIN SID (S-1-5-32-....)
        FakeObject = 2         // everything else: S-1-1-0 (\Everyone), S-1-2-0 (\LOCAL),
                               //   S-1-5-X for X != 21 and X != 32 (NT AUTHORITY), etc.
    }
}

