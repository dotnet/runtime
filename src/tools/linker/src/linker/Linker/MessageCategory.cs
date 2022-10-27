// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Mono.Linker
{
    public enum MessageCategory
    {
        Error = 0,
        Warning,
        Info,
        Diagnostic,

        WarningAsError = 0xFF
    }
}
