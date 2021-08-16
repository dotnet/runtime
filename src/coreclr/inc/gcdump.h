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

#include "gcinfotypes.h"     // For InfoHdr

#ifndef FASTCALL
#ifndef TARGET_UNIX
#define FASTCALL __fastcall
#else
#define FASTCALL
#endif
#endif


class GCDump
{
public:

    GCDump                          (UINT32         gcInfoVersion,
                                     bool           encBytes     = true,
                                     unsigned       maxEncBytes  = 5,
                                     bool           dumpCodeOffs = true);

#ifdef TARGET_X86
    /*-------------------------------------------------------------------------
     * Dumps the InfoHdr to 'stdout'
     * table            : Start of the GC info block
     * verifyGCTables   : If the JIT has been compiled with VERIFY_GC_TABLES
     * Return value     : Size in bytes of the header encoding
     */

    size_t FASTCALL   DumpInfoHdr (PTR_CBYTE   gcInfoBlock,
                                   InfoHdr    *   header,         /* OUT */
                                   unsigned   *   methodSize,     /* OUT */
                                   bool           verifyGCTables = false);
#endif

    /*-------------------------------------------------------------------------
     * Dumps the GC tables to 'stdout'
     * gcInfoBlock      : Start of the GC info block
     * verifyGCTables   : If the JIT has been compiled with VERIFY_GC_TABLES
     * Return value     : Size in bytes of the GC table encodings
     */

    size_t   FASTCALL   DumpGCTable (PTR_CBYTE      gcInfoBlock,
#ifdef TARGET_X86
                                     const InfoHdr& header,
#endif
                                     unsigned       methodSize,
                                     bool           verifyGCTables = false);

    /*-------------------------------------------------------------------------
     * Dumps the location of ptrs for the given code offset
     * verifyGCTables   : If the JIT has been compiled with VERIFY_GC_TABLES
     */

    void     FASTCALL   DumpPtrsInFrame(PTR_CBYTE   gcInfoBlock,
                                        PTR_CBYTE   codeBlock,
                                        unsigned    offs,
                                        bool        verifyGCTables = false);


public:
	typedef void (*printfFtn)(const char* fmt, ...);
	printfFtn gcPrintf;
    UINT32              gcInfoVersion;
    //-------------------------------------------------------------------------
protected:

    bool                fDumpEncBytes;
    unsigned            cMaxEncBytes;

    bool                fDumpCodeOffsets;

    /* Helper methods */

    PTR_CBYTE           DumpEncoding(PTR_CBYTE      gcInfoBlock,
                                     size_t         cDumpBytes);
    void                DumpOffset  (unsigned       o);
    void                DumpOffsetEx(unsigned       o);

};

/*****************************************************************************/
#endif // __GC_DUMP_H__
/*****************************************************************************/
