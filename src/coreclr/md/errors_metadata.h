// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <corerror.h>
#include <winerror.h>

// Index into heap/table is too large.
#define METADATA_E_INDEX_NOTFOUND       CLDB_E_INDEX_NOTFOUND
    // Options:
    //  * CLDB_E_INDEX_NOTFOUND
    //  * VLDTR_E_BLOB_INVALID
    //  * VLDTR_E_GUID_INVALID
    //  * VLDTR_E_STRING_INVALID
    //  * VLDTR_E_RID_OUTOFRANGE

// Internal error, it's a runtime assert check to avoid security errors. If this is returned, then there's
// something wrong with MetaData code.
#define METADATA_E_INTERNAL_ERROR       CLDB_E_INTERNALERROR
    // Options:
    //  * CLDB_E_INTERNALERROR
    //  * COR_E_EXECUTIONENGINE

// MetaData space (heap/table) is full, cannot store more items.
#define METADATA_E_HEAP_FULL            META_E_STRINGSPACE_FULL
    // Options:
    //  * META_E_STRINGSPACE_FULL
    //  * CLDB_E_TOO_BIG

// Invalid heap (blob, user string) data encoding.
#define METADATA_E_INVALID_HEAP_DATA    META_E_BADMETADATA
    // Options:
    //  * META_E_BADMETADATA
    //  * META_E_CA_INVALID_BLOB
    //  * META_E_BAD_SIGNATURE
    //  * CLDB_E_FILE_CORRUPT
    //  * COR_E_BADIMAGEFORMAT

// The data is too big to encode (the string/blob is larger than possible heap size).
#define METADATA_E_DATA_TOO_BIG         CLDB_E_TOO_BIG
    // Options:
    //  * CLDB_E_TOO_BIG

// Invalid MetaData format (headers, etc.).
#define METADATA_E_INVALID_FORMAT       COR_E_BADIMAGEFORMAT
    // Options:
    //  * META_E_BADMETADATA
    //  * META_E_CA_INVALID_BLOB
    //  * META_E_BAD_SIGNATURE
    //  * CLDB_E_FILE_CORRUPT
    //  * COR_E_BADIMAGEFORMAT

//
// Other used error codes:
//  * COR_E_OUTOFMEMORY ... defined as E_OUTOFMEMORY
//      Alternatives:
//          * E_OUTOFMEMORY (from IfNullGo/IfNullRet macros)
//  * COR_E_OVERFLOW
//      Alternatives:
//          * COR_E_ARITHMETIC
//
