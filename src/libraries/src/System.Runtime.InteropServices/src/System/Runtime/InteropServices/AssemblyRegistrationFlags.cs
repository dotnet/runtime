// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.InteropServices
{
    [Flags]
    public enum AssemblyRegistrationFlags
    {
        None = 0x00000000,
        SetCodeBase = 0x00000001,
    }
}
