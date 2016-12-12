/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    RejectedClaimChange.cs

Abstract:

    Implements the RejectedClaimChange enum.
    
History:

    11-May-2004    MattRim     Created

--*/

using System;

namespace System.DirectoryServices.AccountManagement
{

    enum RejectedClaimChange
    {
        Allowed                 = 0,        // change allowed
        TimeRangeNotAllowed     = 1,        // change rejected: store doesn't support time-limited claims
        DuplicateTypeNotAllowed = 2,        // change rejected: store doesn't allow multiple of this type of claim
        TypeNotAllowed          = 3,        // change rejected: store doesn't support claims of this type
        NotAllowed              = 4         // change rejected: other reason (e.g., removal of a mandatory claim)
    }
}
