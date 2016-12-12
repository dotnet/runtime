//------------------------------------------------------------------------------
// <copyright file="AdsSearchPreferences.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.Interop {

    internal enum AdsSearchPreferences {
        ASYNCHRONOUS = 0,
        DEREF_ALIASES = 1,
        SIZE_LIMIT = 2,
        TIME_LIMIT = 3,
        ATTRIBTYPES_ONLY = 4,
        SEARCH_SCOPE = 5,
        TIMEOUT = 6,
        PAGESIZE = 7,
        PAGED_TIME_LIMIT = 8,
        CHASE_REFERRALS = 9,
        SORT_ON = 10,
        CACHE_RESULTS = 11,
        DIRSYNC = 12,
        TOMBSTONE = 13,
        VLV = 14,
        ATTRIBUTE_QUERY = 15,
        SECURITY_MASK = 16,
        DIRSYNC_FLAG = 17,
        EXTENDED_DN = 18
    }

}

