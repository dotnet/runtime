// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

