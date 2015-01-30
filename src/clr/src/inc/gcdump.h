//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

#include "gcinfo.h"     // For InfoHdr

#ifndef FASTCALL
#define FASTCALL __fastcall
#endif


class GCDump
{
public:

    GCDump                          (bool           encBytes     = true, 
                                     unsigned       maxEncBytes  = 5, 
                                     bool           dumpCodeOffs = true);

#ifdef _TARGET_X86_
    /*-------------------------------------------------------------------------
     * Dumps the InfoHdr to 'stdout'
     * table            : Start of the GC info block
     * verifyGCTables   : If the JIT has been compiled with VERIFY_GC_TABLES
     * Return value     : Size in bytes of the header encoding
     */

    unsigned FASTCALL   DumpInfoHdr (PTR_CBYTE      table,
                                     InfoHdr    *   header,         /* OUT */
                                     unsigned   *   methodSize,     /* OUT */
                                     bool           verifyGCTables = false);
#endif

    /*-------------------------------------------------------------------------
     * Dumps the GC tables to 'stdout'
     * table            : Ptr to the start of the table part of the GC info.
     *                      This immediately follows the GCinfo header
     * verifyGCTables   : If the JIT has been compiled with VERIFY_GC_TABLES
     * Return value     : Size in bytes of the GC table encodings
     */

    size_t   FASTCALL   DumpGCTable (PTR_CBYTE      table,
#ifdef _TARGET_X86_
                                     const InfoHdr& header,
#endif
                                     unsigned       methodSize,
                                     bool           verifyGCTables = false);

    /*-------------------------------------------------------------------------
     * Dumps the location of ptrs for the given code offset
     * verifyGCTables   : If the JIT has been compiled with VERIFY_GC_TABLES
     */

    void     FASTCALL   DumpPtrsInFrame(PTR_CBYTE   infoBlock,
                                     PTR_CBYTE      codeBlock,
                                     unsigned       offs,
                                     bool           verifyGCTables = false);


public:
	typedef void (*printfFtn)(const char* fmt, ...);
	printfFtn gcPrintf;	
    //-------------------------------------------------------------------------
protected:

    bool                fDumpEncBytes;
    unsigned            cMaxEncBytes;

    bool                fDumpCodeOffsets;

    /* Helper methods */

    PTR_CBYTE           DumpEncoding(PTR_CBYTE      table, 
                                     int            cDumpBytes);
    void                DumpOffset  (unsigned       o);
    void                DumpOffsetEx(unsigned       o);

};

/*****************************************************************************/
#endif // __GC_DUMP_H__
/*****************************************************************************/
