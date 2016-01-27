// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Enum:   FileSecurityStateAccess
** 
** 
**
**
** Purpose: FileSecurityState enum
**
**
===========================================================*/

using System;

namespace System.IO
{
    [Flags]
    internal enum FileSecurityStateAccess
    {
        NoAccess = 0,
        Read = 1,
        Write = 2,
        Append = 4,
        PathDiscovery = 8,
        AllAccess = 15
    }
}

