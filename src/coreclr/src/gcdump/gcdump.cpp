// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*****************************************************************************
 *                                  GCDump.cpp
 *
 * Defines functions to display the GCInfo as defined by the GC-encoding 
 * spec. The GC information may be either dynamically created by a 
 * Just-In-Time compiler conforming to the standard code-manager spec,
 * or may be persisted by a managed native code compiler conforming
 * to the standard code-manager spec.
 */

#include "utilcode.h"           // For _ASSERTE()
#include "gcdump.h"

/*****************************************************************************/



GCDump::GCDump(bool encBytes, unsigned maxEncBytes, bool dumpCodeOffs)
  : fDumpEncBytes   (encBytes    ), 
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

PTR_CBYTE GCDump::DumpEncoding(PTR_CBYTE table, int cDumpBytes)
{
    _ASSERTE((cDumpBytes >= 0) && (cMaxEncBytes < 256));

    if  (fDumpEncBytes)
    {
        PTR_CBYTE       pCurPos;
        unsigned        count;
        int             cBytesLeft;

        for (count = cMaxEncBytes, cBytesLeft = cDumpBytes, pCurPos = table; 
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

    return  table + cDumpBytes;
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
