// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    /// This enum defines the Open modes. Read/ReadWrite/MaxAllowed are mutually exclusive.
    /// </summary>
    [Flags]
    public enum OpenFlags
    {
        ReadOnly = 0x00,
        ReadWrite = 0x01,
        MaxAllowed = 0x02,
        OpenExistingOnly = 0x04,
        IncludeArchived = 0x08,
    }
}

