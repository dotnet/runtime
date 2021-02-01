// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MDFileFormat.h
//

//
// This file contains a set of helpers to verify and read the file format.
// This code does not handle the paging of the data, or different types of
// I/O.  See the StgTiggerStorage and StgIO code for this level of support.
//
//*****************************************************************************
#ifndef __MDFileFormat_h__
#define __MDFileFormat_h__

#include <metamodelpub.h>
#include "utilcode.h"

//*****************************************************************************
// The signature ULONG is the first 4 bytes of the file format.  The second
// signature string starts the header containing the stream list.  It is used
// for an integrity check when reading the header in lieu of a more complicated
// system.
//*****************************************************************************
#define STORAGE_MAGIC_SIG   0x424A5342  // BSJB



//*****************************************************************************
// These values get written to the signature at the front of the file.  Changing
// these values should not be done lightly because all old files will no longer
// be supported.  In a future revision if a format change is required, a
// backwards compatible migration path must be provided.
//*****************************************************************************

#define FILE_VER_MAJOR  1
#define FILE_VER_MINOR  1

// These are the last legitimate 0.x version macros.  The file format has
// sinced move up to 1.x (see macros above).  After COM+ 1.0/NT 5 RTM's, these
// macros should no longer be required or ever seen.
#define FILE_VER_MAJOR_v0   0

#define FILE_VER_MINOR_v0   19


#define MAXSTREAMNAME   32

enum
{
    STGHDR_NORMAL           = 0x00,     // Normal default flags.
    STGHDR_EXTRADATA        = 0x01,     // Additional data exists after header.
};


//*****************************************************************************
// This is the formal signature area at the front of the file. This structure
// is not allowed to change, the shim depends on it staying the same size.
// Use the reserved pointer if it must extended.
//*****************************************************************************
struct STORAGESIGNATURE;
typedef STORAGESIGNATURE UNALIGNED * PSTORAGESIGNATURE;

#include "pshpack1.h"
struct STORAGESIGNATURE
{
METADATA_FIELDS_PROTECTION:
    ULONG       lSignature;             // "Magic" signature.
    USHORT      iMajorVer;              // Major file version.
    USHORT      iMinorVer;              // Minor file version.
    ULONG       iExtraData;             // Offset to next structure of information
    ULONG       iVersionString;         // Length of version string
public:
    BYTE        pVersion[0];            // Version string
    ULONG GetSignature()
    {
        return VAL32(lSignature);
    }
    void SetSignature(ULONG Signature)
    {
        lSignature = VAL32(Signature);
    }

    USHORT GetMajorVer()
    {
        return VAL16(iMajorVer);
    }
    void SetMajorVer(USHORT MajorVer)
    {
        iMajorVer = VAL16(MajorVer);
    }

    USHORT GetMinorVer()
    {
        return VAL16(iMinorVer);
    }
    void SetMinorVer(USHORT MinorVer)
    {
        iMinorVer = VAL16(MinorVer);
    }

    ULONG GetExtraDataOffset()
    {
        return VAL32(iExtraData);
    }
    void SetExtraDataOffset(ULONG ExtraDataOffset)
    {
        iExtraData = VAL32(ExtraDataOffset);
    }

    ULONG GetVersionStringLength()
    {
        return VAL32(iVersionString);
    }
    void SetVersionStringLength(ULONG VersionStringLength)
    {
        iVersionString = VAL32(VersionStringLength);
    }
};
#include "poppack.h"


//*****************************************************************************
// The header of the storage format.
//*****************************************************************************
struct STORAGEHEADER;
typedef STORAGEHEADER UNALIGNED * PSTORAGEHEADER;

#include "pshpack1.h"
struct STORAGEHEADER
{
METADATA_FIELDS_PROTECTION:
    BYTE        fFlags;                 // STGHDR_xxx flags.
    BYTE        pad;
    USHORT      iStreams;               // How many streams are there.
public:
    BYTE GetFlags()
    {
        return fFlags;
    }
    void SetFlags(BYTE flags)
    {
        fFlags = flags;
    }
    void AddFlags(BYTE flags)
    {
        fFlags |= flags;
    }


    USHORT GetiStreams()
    {
        return VAL16(iStreams);
    }
    void SetiStreams(USHORT iStreamsCount)
    {
        iStreams = VAL16(iStreamsCount);
    }
};
#include "poppack.h"


//*****************************************************************************
// Each stream is described by this struct, which includes the offset and size
// of the data.  The name is stored in ANSI null terminated.
//*****************************************************************************
struct STORAGESTREAM;
typedef STORAGESTREAM UNALIGNED * PSTORAGESTREAM;

#include "pshpack1.h"
struct STORAGESTREAM
{
METADATA_FIELDS_PROTECTION:
    ULONG       iOffset;                // Offset in file for this stream.
    ULONG       iSize;                  // Size of the file.
    char        rcName[MAXSTREAMNAME];  // Start of name, null terminated.
public:
    // Returns pointer to the next stream. Doesn't validate the structure.
    inline PSTORAGESTREAM NextStream()
    {
        int         iLen = (int)(strlen(rcName) + 1);
        iLen = ALIGN4BYTE(iLen);
        return ((PSTORAGESTREAM) (((BYTE*)this) + (sizeof(ULONG) * 2) + iLen));
    }
    // Returns pointer to the next stream.
    // Returns NULL if the structure has invalid format.
    inline PSTORAGESTREAM NextStream_Verify()
    {
        // Check existence of null-terminator in the name
        if (memchr(rcName, 0, MAXSTREAMNAME) == NULL)
        {
            return NULL;
        }
        return NextStream();
    }

    inline ULONG GetStreamSize()
    {
        return (ULONG)(strlen(rcName) + 1 + (sizeof(STORAGESTREAM) - sizeof(rcName)));
    }

    inline char* GetName()
    {
        return rcName;
    }
    inline LPCWSTR GetName(__inout_ecount (iMaxSize) LPWSTR szName, int iMaxSize)
    {
        VERIFY(::WszMultiByteToWideChar(CP_ACP, 0, rcName, -1, szName, iMaxSize));
        return (szName);
    }
    inline void SetName(LPCWSTR szName)
    {
        int size;
        size = WszWideCharToMultiByte(CP_ACP, 0, szName, -1, rcName, MAXSTREAMNAME, 0, 0);
        _ASSERTE(size > 0);
    }

    ULONG GetSize()
    {
        return VAL32(iSize);
    }
    void SetSize(ULONG Size)
    {
        iSize = VAL32(Size);
    }

    ULONG GetOffset()
    {
        return VAL32(iOffset);
    }
    void SetOffset(ULONG Offset)
    {
        iOffset = VAL32(Offset);
    }
};
#include "poppack.h"


class MDFormat
{
public:
//*****************************************************************************
// Verify the signature at the front of the file to see what type it is.
//*****************************************************************************
    static HRESULT VerifySignature(
        PSTORAGESIGNATURE pSig,         // The signature to check.
        ULONG             cbData);      // Size of metadata.

//*****************************************************************************
// Skip over the header and find the actual stream data.
// It doesn't perform any checks for buffer overflow - use GetFirstStream_Verify
// instead.
//*****************************************************************************
    static PSTORAGESTREAM GetFirstStream(// Return pointer to the first stream.
        PSTORAGEHEADER pHeader,             // Return copy of header struct.
        const void *pvMd);                  // Pointer to the full file.
//*****************************************************************************
// Skip over the header and find the actual stream data.  Secure version of
// GetFirstStream method.
// The header is supposed to be verified by VerifySignature.
//
// Caller has to check available buffer size before using the first stream.
//*****************************************************************************
    static PSTORAGESTREAM GetFirstStream_Verify(// Return pointer to the first stream.
        PSTORAGEHEADER pHeader,             // Return copy of header struct.
        const void    *pvMd,                // Pointer to the full file.
        ULONG         *pcbMd);              // [in, out] Size of pvMd buffer (we don't want to read behind it)

};

#endif // __MDFileFormat_h__
