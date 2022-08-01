// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/*****************************************************************************
 *                                  GCDump.h
 *
 * Defines functions to display the GCInfo as defined by the GC-encoding
 * spec. The GC information may be either dynamically created by a
 * Just-In-Time compiler conforming to the standard code-manager spec,
 * or may be persisted by a managed native code compiler conforming
 * to the standard code-manager spec.
 */

/*****************************************************************************/
#ifndef __GCDUMP_H__
#define __GCDUMP_H__
/*****************************************************************************/

struct GCInfoHeader;

#ifndef FASTCALL
#define FASTCALL __fastcall
#endif


class GCDump
{
public:

    struct Tables
    {
        PTR_UInt8 pbDeltaShortcutTable;
        PTR_UInt8 pbUnwindInfoBlob;
        PTR_UInt8 pbCallsiteInfoBlob;
    };


    GCDump                     ();

    /*-------------------------------------------------------------------------
     * Dumps the GCInfoHeader to 'stdout'
     * gcInfo           : Start of the GC info block
     * Return value     : Size in bytes of the header encoding
     */

    size_t FASTCALL DumpInfoHeader(PTR_UInt8      gcInfo,
                                   Tables *       pTables,
                                   GCInfoHeader * header         /* OUT */
                                   );

    /*-------------------------------------------------------------------------
     * Dumps the GC tables to 'stdout'
     * gcInfo           : Ptr to the start of the table part of the GC info.
     *                      This immediately follows the GCinfo header
     * Return value     : Size in bytes of the GC table encodings
     */

    size_t FASTCALL DumpGCTable(PTR_UInt8           gcInfo,
                                Tables *            pTables,
                                const GCInfoHeader& header
                                );


    typedef void (*printfFtn)(const char* fmt, ...);
    printfFtn gcPrintf;



    //-------------------------------------------------------------------------
protected:

    void PrintLocalSlot(uint32_t slotNum, GCInfoHeader const * pHeader);
    void DumpCallsiteString(uint32_t callsiteOffset, PTR_UInt8 pbCallsiteString, GCInfoHeader const * pHeader);
};

/*****************************************************************************/
#endif // __GC_DUMP_H__
/*****************************************************************************/
