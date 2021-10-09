// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// FILE: dwbucketmanager.hpp
//
// This file contains the manager types for differents types of Watson buckets
// and various helper types.
//

//

//
// ============================================================================

#ifndef DWBUCKETMANAGER_HPP
#define DWBUCKETMANAGER_HPP

// this will be used as an index into g_WerEventTraits
enum WatsonBucketType
{
    CLR20r3 = 0,
    // insert new types above this line
    EndOfWerBucketTypes
};

const DWORD kInvalidParamsCount = 0xffffffff;

struct WerEventTypeTraits
{
    const LPCWSTR EventName;
    const DWORD CountParams;
    INDEBUG(const WatsonBucketType BucketType);

    WerEventTypeTraits(LPCWSTR name, DWORD params DEBUG_ARG(WatsonBucketType type))
        : EventName(name), CountParams(params) DEBUG_ARG(BucketType(type))
    {
        _ASSERTE(params < kInvalidParamsCount);
    }
};

const WerEventTypeTraits g_WerEventTraits[] =
{
    WerEventTypeTraits(W("CLR20r3"), 9 DEBUG_ARG(CLR20r3)),
};

DWORD GetCountBucketParamsForEvent(LPCWSTR wzEventName)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (wzEventName == NULL)
    {
        _ASSERTE(!"missing event name when retrieving bucket params count");
        return 10;
    }

    DWORD countParams = kInvalidParamsCount;
    for (int index = 0; index < EndOfWerBucketTypes; ++index)
    {
        if (wcscmp(wzEventName, g_WerEventTraits[index].EventName) == 0)
        {
            _ASSERTE(index == g_WerEventTraits[index].BucketType);
            countParams = g_WerEventTraits[index].CountParams;
            break;
        }
    }

    if (countParams == kInvalidParamsCount)
    {
        _ASSERTE(!"unknown event name when retrieving bucket params count");
        countParams = 10;
    }

    return countParams;
}

#ifndef DACCESS_COMPILE

#include "dwreport.h"
#include <msodwwrap.h>
#include "dbginterface.h"
#include <sha1.h>

//------------------------------------------------------------------------------
// Description
//   Converts an array of bytes to a string of base32 encoded characters.
//
// Constructor
//   pData   -- The bytes to be converted.
//   nData   -- Count of bytes to be converted.
//
// Convert
//   pOut    -- Put converted bytes here.
//   nOut    -- Max number of characters to put
//
//  returns  -- Number of characters put.
//
// Notes
//   Five bytes of input produces 8 characters of output.
//------------------------------------------------------------------------------
class BytesToBase32
{
private:
    // Five doesn't go into 8 very well, so we will wind up with 8 characters per
    //  five bytes of input.  Specifically, a block of 5 bytes will be formatted
    //  like this:
    //      7  6  5  4  3  2  1  0 <-- bit #
    //   0  1  1  1  1  1  2  2  2
    //   1  2  2  3  3  3  3  3  4    <-- which character does the bit go to?
    //   2  4  4  4  4  5  5  5  5
    //   3  5  6  6  6  6  6  7  7
    //   4  7  7  7  8  8  8  8  8
    // This structure defines 2 masks and 3 shift values per 5-bit value.
    //  The first mask is the mask from the first byte.  The first two
    //  shifts are a left- OR a right- shift for the bits obtained via that mask.
    //  If there is a second mask, that is to get bits from the next byte,
    //  shifted right by the second shift value.  Finally, there is a bit to
    //  indicate that the scanner should advance to the next byte.
    // Referring to the table above, the decoder values for the first 5-bit
    //  value will be:
    //    m1 : 0xf8   - mask
    //    l1 : 0      - no left shift
    //    r1 : 3      - right shift 3 bits
    //    m2 : 0      - no second mask
    //    r2 : 0      - no second right shift
    //    skip : 0    - don't skip to next byte (still 3 more bits, for the second 5-bits.
    struct decoder_
    {
        unsigned int m1 : 8;    // Mask 1
        unsigned int l1 : 4;    // Left shift 1
        unsigned int r1 : 4;    // Right shift 2
        unsigned int m2 : 8;    // Mask 2
        unsigned int r2 : 4;    // Right shift 2
        unsigned int skip:4;    // Skip to next input byte
    };

    static const decoder_ decoder[8]; // Array of decoder specs.
    static const WCHAR base32[33];    // Array of 33 characters: A-Z, 0-5, =

    BYTE    *pData;             // Pointer to data.
    int     nData;              // Total bytes of data.

    BYTE    *pEnd;

    int     nWhich;             // Where in the sequence of 8 5-bit datums?

public:
    BytesToBase32(BYTE *p, int n) : pData(p), nData(n), nWhich(0) { LIMITED_METHOD_CONTRACT; pEnd = pData + nData; }

    WCHAR GetNextChar();
    BOOL  MoreChars() { LIMITED_METHOD_CONTRACT; return pData < pEnd; }

    int Convert(__inout_ecount(nOut) LPWSTR pOut, int nOut);
};

// This table tells how to pick out 5-bits at a time (8 times) from 5-bytes of data.
const BytesToBase32::decoder_ BytesToBase32::decoder[8] =
{  //  m1 l1 r1    m2 r2 skip
    {0xf8, 0, 3, 0x00, 0, 0},
    {0x07, 2, 0, 0xc0, 6, 1},
    {0x3e, 0, 1, 0x00, 0, 0},
    {0x01, 4, 0, 0xf0, 4, 1},
    {0x0f, 1, 0, 0x80, 7, 1},
    {0x7c, 0, 2, 0x00, 0, 0},
    {0x03, 3, 0, 0xe0, 5, 1},
    {0x1f, 0, 0, 0x00, 0, 1},
};

// Array of characters with which to encode.
const WCHAR BytesToBase32::base32[33] = {'A','B','C','D','E','F','G','H','I','J','K','L', 'M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z','0','1','2','3','4','5','6'};

//------------------------------------------------------------------------------
// Description
//   Converts 5-bits to a character; fundamental base32 encoding.
//
// Parameters
//   none
//
// Returns
//   The next 5-bits, converted to a character.  Also advances the
//    character pointer.  When no characters remain to be converted,
//    returns W('6')
//
//------------------------------------------------------------------------------
WCHAR BytesToBase32::GetNextChar()
{
    LIMITED_METHOD_CONTRACT;

    unsigned int result = 0;

    _ASSERTE(pData <= pEnd);
    _ASSERTE(nWhich >= 0 && nWhich < lengthof(decoder));

    // If out of data, return signal value, > any valid char.
    if (pData == pEnd)
        return base32[lengthof(base32)-1];

#if defined(_DEBUG)
    if (decoder[nWhich].l1)
    {   // There is a l1 shift.
        _ASSERTE(decoder[nWhich].m1);       // There should be a m1 mask
        _ASSERTE(decoder[nWhich].r1 == 0);  // There should not be a r1 shift
        _ASSERTE(decoder[nWhich].m2);       // There shoulbe a m2 mask to fill in the rest of the bits.
        _ASSERTE(decoder[nWhich].r2);       // m2 bits never start in the right place; there must be a shift
        // The masks, shifted, and or'd together should equal 0x1f, 5-bits.
        _ASSERTE( ( (decoder[nWhich].m1 << decoder[nWhich].l1) | (decoder[nWhich].m2 >> decoder[nWhich].r2)) == 0x1f);
    }
    else
    {   // There is no l1 shift.
        _ASSERTE(decoder[nWhich].m2 == 0);  // There should not be any m2 bits
        _ASSERTE( (decoder[nWhich].m1 >> decoder[nWhich].r1) == 0x1f);  // The m1 bits, shifted should be 0x1f, 5-bits.
    }
#endif

    // Mask off the bits.
    result = *pData & decoder[nWhich].m1;

    // Shift left or right as needed.
    if (decoder[nWhich].l1)
    {   // Shift up to make space for low-order bits from next byte.
        result = result << decoder[nWhich].l1;
    }
    else
    if (decoder[nWhich].r1)
    {   // Shift down into position.  There should be no more bits from next byte.
        result = result >> decoder[nWhich].r1;
    }

    // Skip to next byte if appropriate.
    if (decoder[nWhich].skip)
        ++pData;

    // Grab more bits if specified, and more are available.
    if (pData < pEnd && decoder[nWhich].m2)
    {   // All second-byte data are shifted right, so just mask and shift.
        result |= ( (*pData & decoder[nWhich].m2) >> decoder[nWhich].r2);
    }

    // Advance the 'state machine' -- which 5-bits from an 8-byte block.
    if (++nWhich == lengthof(decoder))
        nWhich = 0;

    // Sanity check on value.
    _ASSERTE(result < lengthof(base32));

    return base32[result];
} // WCHAR BytesToBase32::GetNextChar()

//------------------------------------------------------------------------------
// Description
//   Performs the conversion of a buffer to base32.
//
// Parameters
//   pOut     -- Buffer to receive the characters.
//   nOut     -- Maximum characters to write to the buffer.
//
// Returns
//   the number of characters copied to the output buffer.
//
//------------------------------------------------------------------------------
int BytesToBase32::Convert(
    __inout_ecount(nOut) LPWSTR pOut,
    int nOut)
{
    WRAPPER_NO_CONTRACT;

    int         nWritten = 0;           // Count of bytes written to output.

    // Stop when the buffer is full, or the bytes are fully converted.
    while (nOut > 0 && MoreChars())
    {
        *pOut = GetNextChar();
        ++pOut;
        --nOut;
        ++nWritten;
    }

    return nWritten;
} // int BytesToBase32::Convert()

// this abstract class provides base functionality for populating a bucket parameter in the GMB with some data.
// the actual mapping of ordinal parameter to data type (eg parameter 1 is app name) is handled in subclasses
// of this type.  see GetBucketParamsManager() for retrieving a bucket params manager.
class BaseBucketParamsManager
{
private:
    GenericModeBlock* m_pGmb;
    TypeOfReportedError m_tore;
    Thread* m_pThread;
    OBJECTREF* m_pException;
    INDEBUG(size_t m_countParamsLogged);
    MethodDesc* m_pFaultingMD;
    PCODE m_faultingPc;

    // misc helper functions
    DWORD GetILOffset();
    bool GetFileVersionInfoForModule(Module* pModule, USHORT& major, USHORT& minor, USHORT& build, USHORT& revision);
    bool IsCodeContractsFrame(MethodDesc* pMD);
    OBJECTREF GetRealExceptionObject();
    WCHAR* GetParamBufferForIndex(BucketParameterIndex paramIndex);
    void LogParam(__in_z LPCWSTR paramValue, BucketParameterIndex paramIndex);

protected:
    ~BaseBucketParamsManager();

    typedef void (BaseBucketParamsManager::*DataPopulatorFunction)(__out_ecount(maxLength) WCHAR* targetParam, int maxLength);
    void PopulateBucketParameter(BucketParameterIndex paramIndex, DataPopulatorFunction pFnDataPopulator, int maxLength);

    void PopulateEventName(LPCWSTR eventTypeName);
    // functions for retrieving data to go into various bucket parameters
    void GetAppName(__out_ecount(maxLength) WCHAR* targetParam, int maxLength);
    void GetAppVersion(__out_ecount(maxLength) WCHAR* targetParam, int maxLength);
    void GetAppTimeStamp(__out_ecount(maxLength) WCHAR* targetParam, int maxLength);
    void GetModuleName(__out_ecount(maxLength) WCHAR* targetParam, int maxLength);
    void GetModuleVersion(__out_ecount(maxLength) WCHAR* targetParam, int maxLength);
    void GetModuleTimeStamp(__out_ecount(maxLength) WCHAR* targetParam, int maxLength);
    void GetMethodDef(__out_ecount(maxLength) WCHAR* targetParam, int maxLength);
    void GetIlOffset(__out_ecount(maxLength) WCHAR* targetParam, int maxLength);
    void GetExceptionName(__out_ecount(maxLength) WCHAR* targetParam, int maxLength);
    void GetPackageMoniker(__out_ecount(maxLength) WCHAR* targetParam, int maxLength);
    void GetPRAID(__out_ecount(maxLength) WCHAR* targetParam, int maxLength);
    void GetIlRva(__out_ecount(maxLength) WCHAR* targetParam, int maxLength);

public:
    BaseBucketParamsManager(GenericModeBlock* pGenericModeBlock, TypeOfReportedError typeOfError, PCODE initialFaultingPc, Thread* pFaultingThread, OBJECTREF* pThrownException);
    static int CopyStringToBucket(__out_ecount(targetMaxLength) LPWSTR pTargetParam, int targetMaxLength, __in_z LPCWSTR pSource, bool cannonicalize = false);
    // function that consumers should call to populate the GMB
    virtual void PopulateBucketParameters() = 0;
};

BaseBucketParamsManager::BaseBucketParamsManager(GenericModeBlock* pGenericModeBlock, TypeOfReportedError typeOfError, PCODE initialFaultingPc, Thread* pFaultingThread, OBJECTREF* pThrownException)
    : m_pGmb(pGenericModeBlock), m_tore(typeOfError), m_pThread(pFaultingThread), m_pException(pThrownException), m_pFaultingMD(NULL), m_faultingPc(initialFaultingPc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(m_pGmb);
    INDEBUG(m_countParamsLogged = 0);

    ZeroMemory(pGenericModeBlock, sizeof(GenericModeBlock));

    EECodeInfo codeInfo(initialFaultingPc);
    if (codeInfo.IsValid())
    {
        m_pFaultingMD = codeInfo.GetMethodDesc();
    }
}

BaseBucketParamsManager::~BaseBucketParamsManager()
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(m_countParamsLogged == GetCountBucketParamsForEvent(m_pGmb->wzEventTypeName));
}

void BaseBucketParamsManager::PopulateEventName(LPCWSTR eventTypeName)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    wcsncpy_s(m_pGmb->wzEventTypeName, DW_MAX_BUCKETPARAM_CWC, eventTypeName, _TRUNCATE);

    _ASSERTE(GetCountBucketParamsForEvent(eventTypeName));
    LOG((LF_EH, LL_INFO10, "Event     : %S\n", m_pGmb->wzEventTypeName));
}

WCHAR* BaseBucketParamsManager::GetParamBufferForIndex(BucketParameterIndex paramIndex)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(paramIndex < InvalidBucketParamIndex);
    switch (paramIndex)
    {
        case Parameter1:
            return m_pGmb->wzP1;
        case Parameter2:
            return m_pGmb->wzP2;
        case Parameter3:
            return m_pGmb->wzP3;
        case Parameter4:
            return m_pGmb->wzP4;
        case Parameter5:
            return m_pGmb->wzP5;
        case Parameter6:
            return m_pGmb->wzP6;
        case Parameter7:
            return m_pGmb->wzP7;
        case Parameter8:
            return m_pGmb->wzP8;
        case Parameter9:
            return m_pGmb->wzP9;
        default:
        {
            _ASSERTE(!"bad paramIndex");
            // this is a back-stop to prevent returning NULL and having to have
            // callers check for it.  we should never get here though anyways.
            return m_pGmb->wzP10;
        }
    }
}

void BaseBucketParamsManager::PopulateBucketParameter(BucketParameterIndex paramIndex, DataPopulatorFunction pFnDataPopulator, int maxLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(paramIndex < InvalidBucketParamIndex);
    WCHAR* targetParam = GetParamBufferForIndex(paramIndex);

    // verify that we haven't already written data to this param
    _ASSERTE(targetParam && targetParam[0] == W('\0'));
    (this->*pFnDataPopulator)(targetParam, maxLength);

    LogParam(targetParam, paramIndex);
}

void BaseBucketParamsManager::GetAppName(__out_ecount(maxLength) WCHAR* targetParam, int maxLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    HMODULE hModule = WszGetModuleHandle(NULL);
    PathString appPath;


    if (GetCurrentModuleFileName(appPath) == S_OK)
    {
        CopyStringToBucket(targetParam, maxLength, appPath);
    }
    else
    {
        wcsncpy_s(targetParam, maxLength, W("missing"), _TRUNCATE);
    }
}

void BaseBucketParamsManager::GetAppVersion(__out_ecount(maxLength) WCHAR* targetParam, int maxLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    HMODULE hModule = WszGetModuleHandle(NULL);
    PathString appPath;


    WCHAR verBuf[23] = {0};
    USHORT major, minor, build, revision;

    if ((GetCurrentModuleFileName(appPath) == S_OK) && SUCCEEDED(DwGetFileVersionInfo(appPath, major, minor, build, revision)))
    {
        _snwprintf_s(targetParam,
            maxLength,
            _TRUNCATE,
            W("%d.%d.%d.%d"),
            major, minor, build, revision);
    }
    else if (DwGetAssemblyVersion(appPath, verBuf, NumItems(verBuf)) != 0)
    {
        wcscpy_s(targetParam, maxLength, verBuf);
    }
    else
    {
        wcsncpy_s(targetParam, maxLength, W("missing"), _TRUNCATE);
    }
}

void BaseBucketParamsManager::GetAppTimeStamp(__out_ecount(maxLength) WCHAR* targetParam, int maxLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    EX_TRY
    {
        CONTRACT_VIOLATION(GCViolation);

        HMODULE hModule = WszGetModuleHandle(NULL);
        PEDecoder pe(hModule);

        ULONG ulTimeStamp = pe.GetTimeDateStamp();

        _snwprintf_s(targetParam,
                    maxLength,
                    _TRUNCATE,
                    W("%x"),
                    ulTimeStamp);
    }
    EX_CATCH
    {
        wcsncpy_s(targetParam, maxLength, W("missing"), _TRUNCATE);
    }
    EX_END_CATCH(SwallowAllExceptions)
}

void BaseBucketParamsManager::GetModuleName(__out_ecount(maxLength) WCHAR* targetParam, int maxLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    Module* pModule = NULL;

    if (m_pFaultingMD != NULL)
        pModule = m_pFaultingMD->GetModule();

    bool failed = false;

    if (pModule)
    {
        // Get the assembly name, and determine its length, including terminating NULL.
        Assembly* pAssembly = pModule->GetAssembly();
        LPCUTF8 utf8AssemblyName = pAssembly->GetSimpleName();
        const int assemblyNameLength = WszMultiByteToWideChar(CP_UTF8, 0, utf8AssemblyName, -1, NULL, 0);

        // full name and length.  minor assumption that this is not multi-module.
        WCHAR *fullName = NULL;
        int fullNameLength = assemblyNameLength;

        if (pModule->IsManifest())
        {
            // Single-module assembly; allocate a buffer and convert assembly name.
            fullName = reinterpret_cast< WCHAR* >(_alloca(sizeof(WCHAR)*(fullNameLength)));
            WszMultiByteToWideChar(CP_UTF8, 0, utf8AssemblyName, -1, fullName, fullNameLength);
        }
        else
        {   //  This is a non-manifest module, which means it is a multi-module assembly.
            //  Construct a name like 'assembly+module'.

            // Get the module name, and determine its length, including terminating NULL.
            LPCUTF8 utf8ModuleName = pModule->GetSimpleName();
            const int moduleNameLength = WszMultiByteToWideChar(CP_UTF8, 0, utf8ModuleName, -1, NULL, 0);

            //  Full name length is assembly name length + module name length + 1 char for '+'.
            //  However, both assemblyNameLength and moduleNameLength include space for terminating NULL,
            //  but of course only one NULL is needed, so the final length is just the sum of the two lengths.
            if (!ClrSafeInt<int>::addition(assemblyNameLength, moduleNameLength, fullNameLength))
            {
                failed = true;
            }
            else
            {
                // Allocate a buffer with proper prefast checks.
                int AllocLen;
                if (!ClrSafeInt<int>::multiply(sizeof(WCHAR), fullNameLength, AllocLen))
                {
                    failed = true;
                }
                else
                {
                    fullName = reinterpret_cast< WCHAR* >(_alloca(AllocLen));

                    // Convert the assembly name.
                    WszMultiByteToWideChar(CP_UTF8, 0, utf8AssemblyName, -1, fullName, assemblyNameLength);

                    // replace NULL with '+'
                    _ASSERTE(fullName[assemblyNameLength-1] == 0);
                    fullName[assemblyNameLength-1] = W('+');

                    // Convert the module name after the '+'
                    WszMultiByteToWideChar(CP_UTF8, 0, utf8ModuleName,-1, &fullName[assemblyNameLength], moduleNameLength);
                }
            }
        }

        if (!failed)
        {
            // Make sure NULL termination is right.
            _ASSERTE(fullName[fullNameLength - 1] == 0);

            // Copy name in, with possible truncation or hashing.
            CopyStringToBucket(targetParam, maxLength, fullName);
        }
    }

    if (!pModule || failed)
    {
        wcsncpy_s(targetParam, maxLength, W("missing"), _TRUNCATE);
    }
}

void BaseBucketParamsManager::GetModuleVersion(__out_ecount(maxLength) WCHAR* targetParam, int maxLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    Module* pModule = NULL;

    if (m_pFaultingMD != NULL)
        pModule = m_pFaultingMD->GetModule();

    bool failed = false;

    // @TODO: what if the it is in-memory module? It can have the version info.
    // But we will not retrieve it right.
    if (pModule)
    {
        USHORT major = 0, minor = 0, build = 0, revision = 0;

        bool gotFileVersion = GetFileVersionInfoForModule(pModule, major, minor, build, revision);

        // if we failed to get a version and this isn't the manifest module then try that
        if (!gotFileVersion && !pModule->IsManifest())
        {
            pModule = pModule->GetAssembly()->GetManifestModule();
            if (pModule)
                gotFileVersion = GetFileVersionInfoForModule(pModule, major, minor, build, revision);
        }

        if (!gotFileVersion)
        {
            // if we didn't get a file version then fall back to assembly version (typical for in-memory modules)
            if (FAILED(pModule->GetAssembly()->GetVersion(&major, &minor, &build, &revision)))
                failed = true;
        }

        if (!failed)
        {
            _snwprintf_s(targetParam,
                       maxLength,
                       _TRUNCATE,
                       W("%d.%d.%d.%d"),
                       major, minor, build, revision);
        }
    }

    if (!pModule || failed)
    {
        wcsncpy_s(targetParam, maxLength, W("missing"), _TRUNCATE);
    }
}

void BaseBucketParamsManager::GetModuleTimeStamp(__out_ecount(maxLength) WCHAR* targetParam, int maxLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    Module* pModule = NULL;

    if (m_pFaultingMD != NULL)
        pModule = m_pFaultingMD->GetModule();

    bool failed = false;

    if (pModule)
    {
        EX_TRY
        {
            // We only store the IL timestamp in the native image for the
            // manifest module.  We should consider fixing this for Orcas.
            PTR_PEFile pFile = pModule->GetAssembly()->GetManifestModule()->GetFile();

            // for dynamic modules use 0 as the time stamp
            ULONG ulTimeStamp = 0;

            if (!pFile->IsDynamic())
            {
                ulTimeStamp = pFile->GetILImageTimeDateStamp();
                _ASSERTE(ulTimeStamp != 0);
            }

            _snwprintf_s(targetParam,
                   maxLength,
                   _TRUNCATE,
                   W("%x"),
                   ulTimeStamp);
        }
        EX_CATCH
        {
            failed = true;
        }
        EX_END_CATCH(SwallowAllExceptions)
    }

    if (!pModule || failed)
    {
        wcsncpy_s(targetParam, maxLength, W("missing"), _TRUNCATE);
    }
}

void BaseBucketParamsManager::GetMethodDef(__out_ecount(maxLength) WCHAR* targetParam, int maxLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (m_pFaultingMD)
    {
        mdMethodDef methodDef = m_pFaultingMD->GetMemberDef();
        _snwprintf_s(targetParam,
                   maxLength,
                   _TRUNCATE,
                   W("%x"),
                   RidFromToken(methodDef));
    }
    else
    {
        wcsncpy_s(targetParam, maxLength, W("missing"), _TRUNCATE);
    }
}

void BaseBucketParamsManager::GetIlOffset(__out_ecount(maxLength) WCHAR* targetParam, int maxLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    DWORD ilOffset = GetILOffset();

    _snwprintf_s(targetParam,
                maxLength,
                _TRUNCATE,
                W("%x"),
                ilOffset);
}

void BaseBucketParamsManager::GetExceptionName(__out_ecount(maxLength) WCHAR* targetParam, int maxLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (m_tore.GetType() != TypeOfReportedError::StackOverflowException)
    {
        // At this point we have to switch to cooperative mode, because we need an OBJECTREF.
        GCX_COOP();

        OBJECTREF throwable = GetRealExceptionObject();

        LPCWSTR pExceptionName = NULL;

        if (throwable == NULL)
        {
            // Don't have an exception object.  Make up something reasonable.
            switch (m_tore.GetType())
            {
            case TypeOfReportedError::NativeThreadUnhandledException:
            case TypeOfReportedError::UnhandledException:
                pExceptionName = W("Exception");
                break;
            case TypeOfReportedError::FatalError:
                pExceptionName = W("FatalError");
                break;
            case TypeOfReportedError::UserBreakpoint:
                pExceptionName = W("Debugger.Break");
                break;
            case TypeOfReportedError::NativeBreakpoint:
                pExceptionName = W("Breakpoint");
                break;
            default:
                _ASSERTE(!"Unexpected TypeOfReportedError");
                break;
            }
        }
        else
        {
            MethodTable* pMT = OBJECTREFToObject(throwable)->GetMethodTable();
            DefineFullyQualifiedNameForClassWOnStack();

            EX_TRY
            {
                pExceptionName = GetFullyQualifiedNameForClassNestedAwareW(pMT);
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(SwallowAllExceptions);
        }

        _ASSERTE(pExceptionName);

        // Copy name in, with possible truncation or hashing.
        CopyStringToBucket(targetParam, maxLength, pExceptionName);
    }
    else // StackOverflowException
    {
        // During StackOverflowException processing we may be under ThreadStore lock and cannot spawn a managed thread (otherwise deadlock).
        // So we avoid using any managed heap objects and switching to GC_COOP.
        CopyStringToBucket(targetParam, maxLength, W("System.StackOverflowException"));
    }
}

void BaseBucketParamsManager::GetPackageMoniker(__out_ecount(maxLength) WCHAR* targetParam, int maxLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(!"AppX support NYI for CoreCLR");
}

void BaseBucketParamsManager::GetPRAID(__out_ecount(maxLength) WCHAR* targetParam, int maxLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(!"PRAID support NYI for CoreCLR");
}

void BaseBucketParamsManager::GetIlRva(__out_ecount(maxLength) WCHAR* targetParam, int maxLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    DWORD ilOffset = GetILOffset();

    if (ilOffset == MAXDWORD)
        ilOffset = 0;

    if (m_pFaultingMD)
        ilOffset += m_pFaultingMD->GetRVA();

    _snwprintf_s(targetParam,
                maxLength,
                _TRUNCATE,
                W("%x"),
                ilOffset);
}

// helper functions

DWORD BaseBucketParamsManager::GetILOffset()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    DWORD nativeOffset = 0;
    DWORD ilOffset = MAXDWORD;

    EECodeInfo codeInfo(m_faultingPc);
    if (codeInfo.IsValid())
    {
        nativeOffset = codeInfo.GetRelOffset();
        _ASSERTE(m_pFaultingMD == codeInfo.GetMethodDesc());
    }

    if (m_pFaultingMD)
    {
        EX_TRY
        {
            CONTRACT_VIOLATION(GCViolation);
            _ASSERTE(g_pDebugInterface != NULL);
            g_pDebugInterface->GetILOffsetFromNative(
                                m_pFaultingMD,
                                (const BYTE *)m_faultingPc,
                                nativeOffset,
                                &ilOffset);
        }
        EX_CATCH
        {
            // Swallow the exception, and just use MAXDWORD.
        }
        EX_END_CATCH(SwallowAllExceptions)
    }

    return ilOffset;
}

// attempts to get file version information for the specified module.
// returns true on success and all out params will contain data.
// on failure the out params are not touched.
// assumes that pModule is not NULL!!
bool BaseBucketParamsManager::GetFileVersionInfoForModule(Module* pModule, USHORT& major, USHORT& minor, USHORT& build, USHORT& revision)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pModule != NULL);
    }
    CONTRACTL_END;

    bool succeeded = false;

    PEFile* pFile = pModule->GetFile();
    if (pFile)
    {
        // if we failed to get the version info from the native image then fall back to the IL image.
        if (!succeeded)
        {
            LPCWSTR modulePath = pFile->GetPath().GetUnicode();
            if (modulePath != NULL && modulePath != SString::Empty() && SUCCEEDED(DwGetFileVersionInfo(modulePath, major, minor, build, revision)))
            {
                succeeded = true;
            }
        }
    }

    return succeeded;
}

// attempts to determine if the specified MethodDesc is one of the code contracts methods.
// this is defined as any method on the System.Diagnostics.Contracts.__ContractsRuntime type.
bool BaseBucketParamsManager::IsCodeContractsFrame(MethodDesc* pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pMD != NULL);
    }
    CONTRACTL_END;

    if (!pMD)
        return false;

    MethodTable* pMT = pMD->GetMethodTable_NoLogging();
    LPCUTF8 pszNamespace = NULL;
    LPCUTF8 pszName = NULL;
    pszName = pMT->GetFullyQualifiedNameInfo(&pszNamespace);

    if (!pszName || !pszNamespace)
        return false;

    LPCUTF8 pszContractsNamespace = "System.Diagnostics.Contracts";
    LPCUTF8 pszContractsRuntimeType = "__ContractsRuntime";

    if (strcmp(pszNamespace, pszContractsNamespace) == 0 &&
        strcmp(pszName, pszContractsRuntimeType) == 0)
        return true;

    return false;
}

// gets the "real" exception object.  it might be m_pException or the exception object on the thread
OBJECTREF BaseBucketParamsManager::GetRealExceptionObject()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF throwable = NULL;

    if (m_pException != NULL)
    {
        _ASSERTE(IsProtectedByGCFrame(m_pException));
        throwable = *m_pException;
    }
    else if (m_tore.IsException())
    {
        // If it is an exception, see if there is a Throwable object.
        if (m_pThread != NULL)
        {
            throwable = m_pThread->GetThrowable();

            // If the "Throwable" is null, try the "LastThrownObject"
            if (throwable == NULL)
                throwable = m_pThread->LastThrownObject();
        }
    }

    return throwable;
}

//------------------------------------------------------------------------------
// Description
//   Copies a string to a Watson bucket parameter.  If the offered string is
//   longer than the maxLen, the string will be shortened.
//
// Parameters
//   pTargetParam     -- the destination buffer.
//   targetMaxLength  -- the max length of the parameter.
//   pSource          -- the input string.
//   cannonicalize    -- if true, cannonicalize the filename (tolower)
//
// Returns
//   the number of characters copied to the output buffer.  zero indicates an
//     error.
//
// Notes
//   The truncation algorithm is this:
//    - if the value contains non-ascii characters, divide the maxLen by 4,
//      due to restrictions in Watson bucketing rules
//    - if the value fits, just copy it as-is
//    - if the value doesn't fit, strip any trailing ".dll", ".exe", ".netmodule",
//      or "Exception"
//    - if the value still doesn't fit, take a SHA1 hash of the source, and
//      encode in base32.
//    - if the value may require hashing, the maxlen should be at least 32,
//      because that is what a SHA1 hash coded in base32 will require.
//    - the maxlen does not include the terminating nul.
//------------------------------------------------------------------------------
int BaseBucketParamsManager::CopyStringToBucket(__out_ecount(targetMaxLength) LPWSTR pTargetParam, int targetMaxLength, __in_z LPCWSTR pSource, bool cannonicalize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Array of suffixes to truncate if necessary.
    static const LPCWSTR truncations[] =
    {
        W("Exception"),
        W(".dll"),
        W(".exe"),
        W(".netmodule"),
        0
    };

    int srcLen = static_cast<int>(wcslen(pSource));

    // If the source contains unicode characters, they'll be encoded at 4 chars per char.
    int targLen = ContainsUnicodeChars(pSource) ? targetMaxLength / 4 : targetMaxLength;

    // If the string is too long, see if there is a suffix that can be trimmed.
    if (srcLen > targLen)
    {
        for (int i = 0; truncations[i]; ++i)
        {
            // how long is this suffix?
            int slen = static_cast<int>(wcslen(truncations[i]));

            // Could the string have this suffix?
            if (slen < srcLen)
            {
                // maybe -- check.
                if (SString::_wcsicmp(&pSource[srcLen - slen], truncations[i]) == 0)
                {
                    // yes, the string does have this suffix.  drop it.
                    srcLen -= slen;
                    break;
                }
            }
        }
    }

    // If the (possibly truncated) value fits, copy it and return.
    if (srcLen <= targLen)
    {
        wcsncpy_s(pTargetParam, DW_MAX_BUCKETPARAM_CWC, pSource, srcLen);

        if (cannonicalize)
        {
            // cannonicalize filenames so that the same exceptions tend to the same buckets.
            _wcslwr_s(pTargetParam, DW_MAX_BUCKETPARAM_CWC);
        }
        return srcLen;
    }

    // String didn't fit, so hash it.
    SHA1Hash hash;
    hash.AddData(reinterpret_cast<BYTE*>(const_cast<LPWSTR>(pSource)), (static_cast<int>(wcslen(pSource))) * sizeof(WCHAR));

    // Encode in base32.  The hash is a fixed size; we'll accept up to maxLen characters of the encoding.
    BytesToBase32 b32(hash.GetHash(), SHA1_HASH_SIZE);
    targLen = b32.Convert(pTargetParam, targetMaxLength);
    pTargetParam[targLen] = W('\0');

    return targLen;
}

void BaseBucketParamsManager::LogParam(__in_z LPCWSTR paramValue, BucketParameterIndex paramIndex)
{
#ifdef _DEBUG
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(paramIndex < InvalidBucketParamIndex);
    // the BucketParameterIndex enum starts at 0 however we refer to Watson
    // bucket params with 1-based indices so we add one to paramIndex.
    LOG((LF_EH, LL_INFO10, "       p %d: %S\n", paramIndex + 1, paramValue));
    ++m_countParamsLogged;
#endif
}

// specific manager classes for the various watson bucket types that the CLR reports.
// each type is responsible for populating the GMB according to the event type schema.
// to add support for a new schema simply inherit from the BaseBucketParamsManager and
// in the PopulateBucketParameters() function fill out the GMB as required.  then update
// function GetBucketParamsManager() (and a few depedent functions) to return the new
// type as required.

class CLR20r3BucketParamsManager : public BaseBucketParamsManager
{
public:
    CLR20r3BucketParamsManager(GenericModeBlock* pGenericModeBlock, TypeOfReportedError typeOfError, PCODE faultingPC, Thread* pFaultingThread, OBJECTREF* pThrownException);
    ~CLR20r3BucketParamsManager();

    virtual void PopulateBucketParameters();
};

CLR20r3BucketParamsManager::CLR20r3BucketParamsManager(GenericModeBlock* pGenericModeBlock, TypeOfReportedError typeOfError, PCODE faultingPC, Thread* pFaultingThread, OBJECTREF* pThrownException)
    : BaseBucketParamsManager(pGenericModeBlock, typeOfError, faultingPC, pFaultingThread, pThrownException)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
}

CLR20r3BucketParamsManager::~CLR20r3BucketParamsManager()
{
    LIMITED_METHOD_CONTRACT;
}

void CLR20r3BucketParamsManager::PopulateBucketParameters()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Preempt to let GC suspend
    GCX_PREEMP();

    PopulateEventName(g_WerEventTraits[CLR20r3].EventName);

    // the "+ 1" is to explicitly indicate which fields need to specify space for NULL
    PopulateBucketParameter(Parameter1, &CLR20r3BucketParamsManager::GetAppName, 32);
    PopulateBucketParameter(Parameter2, &CLR20r3BucketParamsManager::GetAppVersion, 23 + 1);
    PopulateBucketParameter(Parameter3, &CLR20r3BucketParamsManager::GetAppTimeStamp, 8 + 1);
    PopulateBucketParameter(Parameter4, &CLR20r3BucketParamsManager::GetModuleName, 64);
    PopulateBucketParameter(Parameter5, &CLR20r3BucketParamsManager::GetModuleVersion, 23 + 1);
    PopulateBucketParameter(Parameter6, &CLR20r3BucketParamsManager::GetModuleTimeStamp, 8 + 1);
    PopulateBucketParameter(Parameter7, &CLR20r3BucketParamsManager::GetMethodDef, 6 + 1);
    PopulateBucketParameter(Parameter8, &CLR20r3BucketParamsManager::GetIlOffset, 8 + 1);
    PopulateBucketParameter(Parameter9, &CLR20r3BucketParamsManager::GetExceptionName, 32);
}

WatsonBucketType GetWatsonBucketType()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return CLR20r3;
}

#endif // DACCESS_COMPILE

#endif // DWBUCKETMANAGER_HPP
