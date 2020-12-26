// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/*****************************************************************************
 *                                  GCDump.cpp
 *
 * Defines functions to display the GCInfo as defined by the GC-encoding
 * spec. The GC information may be either dynamically created by a
 * Just-In-Time compiler conforming to the standard code-manager spec,
 * or may be persisted by a managed native code compiler conforming
 * to the standard code-manager spec.
 */

#ifndef TARGET_UNIX
#include "utilcode.h"           // For _ASSERTE()
#endif //!TARGET_UNIX
#include "gcdump.h"

/*****************************************************************************/



GCDump::GCDump(UINT32 gcInfoVer, bool encBytes, unsigned maxEncBytes, bool dumpCodeOffs)
  : gcInfoVersion   (gcInfoVer),
    fDumpEncBytes   (encBytes    ),
    cMaxEncBytes    (maxEncBytes ),
    fDumpCodeOffsets(dumpCodeOffs)
{
    // By default, use the standard printf function to dump
    GCDump::gcPrintf = (printfFtn) ::printf;
}

/*****************************************************************************
 *
 *  Display the byte encodings for the given range of the GC tables.
 */

PTR_CBYTE GCDump::DumpEncoding(PTR_CBYTE gcInfoBlock, size_t cDumpBytes)
{
    _ASSERTE((cDumpBytes >= 0) && (cMaxEncBytes < 256));

    if  (fDumpEncBytes)
    {
        PTR_CBYTE       pCurPos;
        unsigned        count;
        size_t          cBytesLeft;

        for (count = cMaxEncBytes, cBytesLeft = cDumpBytes, pCurPos = gcInfoBlock;
             count > 0;
             count--, pCurPos++, cBytesLeft--)
        {
            if  (cBytesLeft > 0)
            {
                if  (cBytesLeft > 1 && count == 1)
                    gcPrintf("...");
                else
                    gcPrintf("%02X ", *pCurPos);
            }
            else
                gcPrintf("   ");
        }

        gcPrintf("| ");
    }

    return  gcInfoBlock + cDumpBytes;
}

/*****************************************************************************/

void                GCDump::DumpOffset(unsigned o)
{
    gcPrintf("%04X", o);
}

void                GCDump::DumpOffsetEx(unsigned o)
{
    if (fDumpCodeOffsets)
        DumpOffset(o);
}

/*****************************************************************************/
