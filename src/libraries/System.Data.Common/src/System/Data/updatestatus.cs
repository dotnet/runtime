// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data
{
    public enum UpdateStatus
    {
        Continue = 0,

        ErrorsOccurred = 1,

        SkipCurrentRow = 2,

        SkipAllRemainingRows = 3,
    }
}
