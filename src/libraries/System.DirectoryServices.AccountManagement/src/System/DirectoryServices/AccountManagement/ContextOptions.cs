// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.DirectoryServices.AccountManagement
{
    [Flags]
    public enum ContextOptions
    {
        Negotiate = 1,
        SimpleBind = 2,
        SecureSocketLayer = 4,
        Signing = 8,
        Sealing = 16,
        ServerBind = 32,
    }
}
