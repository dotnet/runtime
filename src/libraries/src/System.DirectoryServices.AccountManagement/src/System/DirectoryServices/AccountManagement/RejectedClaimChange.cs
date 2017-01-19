// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.DirectoryServices.AccountManagement
{
    internal enum RejectedClaimChange
    {
        Allowed = 0,        // change allowed
        TimeRangeNotAllowed = 1,        // change rejected: store doesn't support time-limited claims
        DuplicateTypeNotAllowed = 2,        // change rejected: store doesn't allow multiple of this type of claim
        TypeNotAllowed = 3,        // change rejected: store doesn't support claims of this type
        NotAllowed = 4         // change rejected: other reason (e.g., removal of a mandatory claim)
    }
}
