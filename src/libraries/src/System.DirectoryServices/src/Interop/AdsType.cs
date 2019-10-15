// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Interop
{
    internal enum AdsType
    {
        ADSTYPE_INVALID = 0,
        ADSTYPE_DN_STRING = 1,
        ADSTYPE_CASE_EXACT_STRING = 2,
        ADSTYPE_CASE_IGNORE_STRING = 3,
        ADSTYPE_PRINTABLE_STRING = 4,
        ADSTYPE_NUMERIC_STRING = 5,
        ADSTYPE_BOOLEAN = 6,
        ADSTYPE_INTEGER = 7,
        ADSTYPE_OCTET_STRING = 8,
        ADSTYPE_UTC_TIME = 9,
        ADSTYPE_LARGE_INTEGER = 10,
        ADSTYPE_PROV_SPECIFIC = 11,
        ADSTYPE_OBJECT_CLASS = 12,
        ADSTYPE_CASEIGNORE_LIST = 13,
        ADSTYPE_OCTET_LIST = 14,
        ADSTYPE_PATH = 15,
        ADSTYPE_POSTALADDRESS = 16,
        ADSTYPE_TIMESTAMP = 17,
        ADSTYPE_BACKLINK = 18,
        ADSTYPE_TYPEDNAME = 19,
        ADSTYPE_HOLD = 20,
        ADSTYPE_NETADDRESS = 21,
        ADSTYPE_REPLICAPOINTER = 22,
        ADSTYPE_FAXNUMBER = 23,
        ADSTYPE_EMAIL = 24,
        ADSTYPE_NT_SECURITY_DESCRIPTOR = 25,
        ADSTYPE_UNKNOWN = 26,
        ADSTYPE_DN_WITH_BINARY = 27,
        ADSTYPE_DN_WITH_STRING = 28
    }
}
