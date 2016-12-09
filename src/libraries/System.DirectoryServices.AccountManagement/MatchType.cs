/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    MatchType.cs

Abstract:

    Implements the MatchType enum.
    
History:

    05-May-2004    MattRim     Created

--*/

using System;

namespace System.DirectoryServices.AccountManagement
{

    public enum MatchType
    {
        Equals              =   0,
        NotEquals           =   1,
        GreaterThan         =   2,
        GreaterThanOrEquals =   3,
        LessThan            =   4,
        LessThanOrEquals    =   5
    }
}
