// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                              emit.cpp                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "hostallocator.h"
#include "instr.h"
#include "emit.h"
#include "codegen.h"

/*****************************************************************************
 *
 *  Represent an emitter location.
 */

void emitLocation::CaptureLocation(emitter* emit)
{
    ig      = emit->emitCurIG;
    codePos = emit->emitCurOffset();

    assert(Valid());
}

bool emitLocation::IsCurrentLocation(emitter* emit) const
{
    assert(Valid());
    return (ig == emit->emitCurIG) && (codePos == emit->emitCurOffset());
}

UNATIVE_OFFSET emitLocation::CodeOffset(emitter* emit) const
{
    assert(Valid());
    return emit->emitCodeOffset(ig, codePos);
}

int emitLocation::GetInsNum() const
{
    return emitGetInsNumFromCodePos(codePos);
}

// Get the instruction offset in the current instruction group, which must be a funclet prolog group.
// This is used to find an instruction offset used in unwind data.
// TODO-AMD64-Bug?: We only support a single main function prolog group, but allow for multiple funclet prolog
// groups (not that we actually use that flexibility, since the funclet prolog will be small). How to
// handle that?
UNATIVE_OFFSET emitLocation::GetFuncletPrologOffset(emitter* emit) const
{
    assert(ig->igFuncIdx != 0);
    assert((ig->igFlags & IGF_FUNCLET_PROLOG) != 0);
    assert(ig == emit->emitCurIG);

    return emit->emitCurIGsize;
}

#ifdef DEBUG
void emitLocation::Print() const
{
    unsigned insNum = emitGetInsNumFromCodePos(codePos);
    unsigned insOfs = emitGetInsOfsFromCodePos(codePos);
    printf("(G_M%03u_IG%02u,ins#%d,ofs#%d)", Compiler::s_compMethodsCount, ig->igNum, insNum, insOfs);
}
#endif // DEBUG

/*****************************************************************************
 *
 *  Return the name of an instruction format.
 */

#if defined(DEBUG) || EMITTER_STATS

const char* emitter::emitIfName(unsigned f)
{
    static const char* const ifNames[] = {
#define IF_DEF(en, op1, op2) "IF_" #en,
#include "emitfmts.h"
    };

    static char errBuff[32];

    if (f < _countof(ifNames))
    {
        return ifNames[f];
    }

    sprintf_s(errBuff, sizeof(errBuff), "??%u??", f);
    return errBuff;
}

#endif

#ifdef TRANSLATE_PDB

/* these are protected */

AddrMap*  emitter::emitPDBOffsetTable = 0;
LocalMap* emitter::emitPDBLocalTable  = 0;
bool      emitter::emitIsPDBEnabled   = true;
BYTE*     emitter::emitILBaseOfCode   = 0;
BYTE*     emitter::emitILMethodBase   = 0;
BYTE*     emitter::emitILMethodStart  = 0;
BYTE*     emitter::emitImgBaseOfCode  = 0;

void emitter::MapCode(int ilOffset, BYTE* imgDest)
{
    if (emitIsPDBEnabled)
    {
        emitPDBOffsetTable->MapSrcToDest(ilOffset, (int)(imgDest - emitImgBaseOfCode));
    }
}

void emitter::MapFunc(int                imgOff,
                      int                procLen,
                      int                dbgStart,
                      int                dbgEnd,
                      short              frameReg,
                      int                stkAdjust,
                      int                lvaCount,
                      OptJit::LclVarDsc* lvaTable,
                      bool               framePtr)
{
    if (emitIsPDBEnabled)
    {
        // this code stores information about local symbols for the PDB translation

        assert(lvaCount >= 0); // don't allow a negative count

        LvaDesc* rgLvaDesc = 0;

        if (lvaCount > 0)
        {
            rgLvaDesc = new LvaDesc[lvaCount];

            if (!rgLvaDesc)
            {
                NOMEM();
            }

            LvaDesc*           pDst = rgLvaDesc;
            OptJit::LclVarDsc* pSrc = lvaTable;
            for (int i = 0; i < lvaCount; ++i, ++pDst, ++pSrc)
            {
                pDst->slotNum = pSrc->lvSlotNum;
                pDst->isReg   = pSrc->lvRegister;
                pDst->reg     = (pSrc->lvRegister ? pSrc->lvRegNum : frameReg);
                pDst->off     = pSrc->lvStkOffs + stkAdjust;
            }
        }

        emitPDBLocalTable->AddFunc((int)(emitILMethodBase - emitILBaseOfCode), imgOff - (int)emitImgBaseOfCode, procLen,
                                   dbgStart - imgOff, dbgEnd - imgOff, lvaCount, rgLvaDesc, framePtr);
        // do not delete rgLvaDesc here -- responsibility is now on emitPDBLocalTable destructor
    }
}

/* these are public */

void emitter::SetILBaseOfCode(BYTE* pTextBase)
{
    emitILBaseOfCode = pTextBase;
}

void emitter::SetILMethodBase(BYTE* pMethodEntry)
{
    emitILMethodBase = pMethodEntry;
}

void emitter::SetILMethodStart(BYTE* pMethodCode)
{
    emitILMethodStart = pMethodCode;
}

void emitter::SetImgBaseOfCode(BYTE* pTextBase)
{
    emitImgBaseOfCode = pTextBase;
}

void emitter::SetIDBaseToProlog()
{
    emitInstrDescILBase = (int)(emitILMethodBase - emitILBaseOfCode);
}

void emitter::SetIDBaseToOffset(int methodOffset)
{
    emitInstrDescILBase = methodOffset + (int)(emitILMethodStart - emitILBaseOfCode);
}

void emitter::DisablePDBTranslation()
{
    // this function should disable PDB translation code
    emitIsPDBEnabled = false;
}

bool emitter::IsPDBEnabled()
{
    return emitIsPDBEnabled;
}

void emitter::InitTranslationMaps(int ilCodeSize)
{
    if (emitIsPDBEnabled)
    {
        emitPDBOffsetTable = AddrMap::Create(ilCodeSize);
        emitPDBLocalTable  = LocalMap::Create();
    }
}

void emitter::DeleteTranslationMaps()
{
    if (emitPDBOffsetTable)
    {
        delete emitPDBOffsetTable;
        emitPDBOffsetTable = 0;
    }
    if (emitPDBLocalTable)
    {
        delete emitPDBLocalTable;
        emitPDBLocalTable = 0;
    }
}

void emitter::InitTranslator(PDBRewriter* pPDB, int* rgSecMap, IMAGE_SECTION_HEADER** rgpHeader, int numSections)
{
    if (emitIsPDBEnabled)
    {
        pPDB->InitMaps(rgSecMap,           // new PE section header order
                       rgpHeader,          // array of section headers
                       numSections,        // number of sections
                       emitPDBOffsetTable, // code offset translation table
                       emitPDBLocalTable); // slot variable translation table
    }
}

#endif // TRANSLATE_PDB

/*****************************************************************************/

#if EMITTER_STATS

static unsigned totAllocdSize;
static unsigned totActualSize;

unsigned emitter::emitIFcounts[emitter::IF_COUNT];

static unsigned  emitSizeBuckets[] = {100, 1024 * 1, 1024 * 2, 1024 * 3, 1024 * 4, 1024 * 5, 1024 * 10, 0};
static Histogram emitSizeTable(emitSizeBuckets);

static unsigned  GCrefsBuckets[] = {0, 1, 2, 5, 10, 20, 50, 128, 256, 512, 1024, 0};
static Histogram GCrefsTable(GCrefsBuckets);

static unsigned  stkDepthBuckets[] = {0, 1, 2, 5, 10, 16, 32, 128, 1024, 0};
static Histogram stkDepthTable(stkDepthBuckets);

size_t emitter::emitSizeMethod;

size_t   emitter::emitTotMemAlloc;
unsigned emitter::emitTotalInsCnt;
unsigned emitter::emitCurPrologInsCnt;
size_t   emitter::emitCurPrologIGSize;
unsigned emitter::emitMaxPrologInsCnt;
size_t   emitter::emitMaxPrologIGSize;
unsigned emitter::emitTotalIGcnt;
unsigned emitter::emitTotalPhIGcnt;
unsigned emitter::emitTotalIGjmps;
unsigned emitter::emitTotalIGptrs;
unsigned emitter::emitTotalIGicnt;
size_t   emitter::emitTotalIGsize;
unsigned emitter::emitTotalIGmcnt;
unsigned emitter::emitTotalIGEmitAdd;

unsigned emitter::emitTotalIDescSmallCnt;
unsigned emitter::emitTotalIDescCnt;
unsigned emitter::emitTotalIDescJmpCnt;
#if !defined(_TARGET_ARM64_)
unsigned emitter::emitTotalIDescLblCnt;
#endif // !defined(_TARGET_ARM64_)
unsigned emitter::emitTotalIDescCnsCnt;
unsigned emitter::emitTotalIDescDspCnt;
unsigned emitter::emitTotalIDescCnsDspCnt;
#ifdef _TARGET_XARCH_
unsigned emitter::emitTotalIDescAmdCnt;
unsigned emitter::emitTotalIDescCnsAmdCnt;
#endif // _TARGET_XARCH_
unsigned emitter::emitTotalIDescCGCACnt;
#ifdef _TARGET_ARM_
unsigned emitter::emitTotalIDescRelocCnt;
#endif // _TARGET_ARM_

unsigned emitter::emitSmallDspCnt;
unsigned emitter::emitLargeDspCnt;

unsigned emitter::emitSmallCnsCnt;
unsigned emitter::emitLargeCnsCnt;
unsigned emitter::emitSmallCns[SMALL_CNS_TSZ];

void emitterStaticStats(FILE* fout)
{
    // insGroup members

    insGroup* igDummy = nullptr;

    fprintf(fout, "\n");
    fprintf(fout, "insGroup:\n");
    fprintf(fout, "Offset / size of igNext              = %2u / %2u\n", offsetof(insGroup, igNext),
            sizeof(igDummy->igNext));
#ifdef DEBUG
    fprintf(fout, "Offset / size of igSelf              = %2u / %2u\n", offsetof(insGroup, igSelf),
            sizeof(igDummy->igSelf));
#endif
    fprintf(fout, "Offset / size of igNum               = %2u / %2u\n", offsetof(insGroup, igNum),
            sizeof(igDummy->igNum));
    fprintf(fout, "Offset / size of igOffs              = %2u / %2u\n", offsetof(insGroup, igOffs),
            sizeof(igDummy->igOffs));
    fprintf(fout, "Offset / size of igFuncIdx           = %2u / %2u\n", offsetof(insGroup, igFuncIdx),
            sizeof(igDummy->igFuncIdx));
    fprintf(fout, "Offset / size of igFlags             = %2u / %2u\n", offsetof(insGroup, igFlags),
            sizeof(igDummy->igFlags));
    fprintf(fout, "Offset / size of igSize              = %2u / %2u\n", offsetof(insGroup, igSize),
            sizeof(igDummy->igSize));
    fprintf(fout, "Offset / size of igData              = %2u / %2u\n", offsetof(insGroup, igData),
            sizeof(igDummy->igData));
    fprintf(fout, "Offset / size of igPhData            = %2u / %2u\n", offsetof(insGroup, igPhData),
            sizeof(igDummy->igPhData));
#if EMIT_TRACK_STACK_DEPTH
    fprintf(fout, "Offset / size of igStkLvl            = %2u / %2u\n", offsetof(insGroup, igStkLvl),
            sizeof(igDummy->igStkLvl));
#endif
    fprintf(fout, "Offset / size of igGCregs            = %2u / %2u\n", offsetof(insGroup, igGCregs),
            sizeof(igDummy->igGCregs));
    fprintf(fout, "Offset / size of igInsCnt            = %2u / %2u\n", offsetof(insGroup, igInsCnt),
            sizeof(igDummy->igInsCnt));
    fprintf(fout, "\n");
    fprintf(fout, "Size of insGroup                     = %u\n", sizeof(insGroup));

    // insPlaceholderGroupData members

    fprintf(fout, "\n");
    fprintf(fout, "insPlaceholderGroupData:\n");
    fprintf(fout, "Offset of igPhNext                = %2u\n", offsetof(insPlaceholderGroupData, igPhNext));
    fprintf(fout, "Offset of igPhBB                  = %2u\n", offsetof(insPlaceholderGroupData, igPhBB));
    fprintf(fout, "Offset of igPhInitGCrefVars       = %2u\n", offsetof(insPlaceholderGroupData, igPhInitGCrefVars));
    fprintf(fout, "Offset of igPhInitGCrefRegs       = %2u\n", offsetof(insPlaceholderGroupData, igPhInitGCrefRegs));
    fprintf(fout, "Offset of igPhInitByrefRegs       = %2u\n", offsetof(insPlaceholderGroupData, igPhInitByrefRegs));
    fprintf(fout, "Offset of igPhPrevGCrefVars       = %2u\n", offsetof(insPlaceholderGroupData, igPhPrevGCrefVars));
    fprintf(fout, "Offset of igPhPrevGCrefRegs       = %2u\n", offsetof(insPlaceholderGroupData, igPhPrevGCrefRegs));
    fprintf(fout, "Offset of igPhPrevByrefRegs       = %2u\n", offsetof(insPlaceholderGroupData, igPhPrevByrefRegs));
    fprintf(fout, "Offset of igPhType                = %2u\n", offsetof(insPlaceholderGroupData, igPhType));
    fprintf(fout, "Size   of insPlaceholderGroupData = %u\n", sizeof(insPlaceholderGroupData));

    fprintf(fout, "\n");
    fprintf(fout, "SMALL_IDSC_SIZE           = %2u\n", SMALL_IDSC_SIZE);
    fprintf(fout, "Size   of instrDesc       = %2u\n", sizeof(emitter::instrDesc));
    // fprintf(fout, "Offset of _idIns      = %2u\n", offsetof(emitter::instrDesc, _idIns      ));
    // fprintf(fout, "Offset of _idInsFmt   = %2u\n", offsetof(emitter::instrDesc, _idInsFmt   ));
    // fprintf(fout, "Offset of _idOpSize   = %2u\n", offsetof(emitter::instrDesc, _idOpSize   ));
    // fprintf(fout, "Offset of idSmallCns  = %2u\n", offsetof(emitter::instrDesc, idSmallCns  ));
    // fprintf(fout, "Offset of _idAddrUnion= %2u\n", offsetof(emitter::instrDesc, _idAddrUnion));
    // fprintf(fout, "\n");
    // fprintf(fout, "Size   of _idAddrUnion= %2u\n", sizeof(((emitter::instrDesc*)0)->_idAddrUnion));

    fprintf(fout, "Size   of instrDescJmp    = %2u\n", sizeof(emitter::instrDescJmp));
#if !defined(_TARGET_ARM64_)
    fprintf(fout, "Size   of instrDescLbl    = %2u\n", sizeof(emitter::instrDescLbl));
#endif // !defined(_TARGET_ARM64_)
    fprintf(fout, "Size   of instrDescCns    = %2u\n", sizeof(emitter::instrDescCns));
    fprintf(fout, "Size   of instrDescDsp    = %2u\n", sizeof(emitter::instrDescDsp));
    fprintf(fout, "Size   of instrDescCnsDsp = %2u\n", sizeof(emitter::instrDescCnsDsp));
#ifdef _TARGET_XARCH_
    fprintf(fout, "Size   of instrDescAmd    = %2u\n", sizeof(emitter::instrDescAmd));
    fprintf(fout, "Size   of instrDescCnsAmd = %2u\n", sizeof(emitter::instrDescCnsAmd));
#endif // _TARGET_XARCH_
    fprintf(fout, "Size   of instrDescCGCA   = %2u\n", sizeof(emitter::instrDescCGCA));
#ifdef _TARGET_ARM_
    fprintf(fout, "Size   of instrDescReloc  = %2u\n", sizeof(emitter::instrDescReloc));
#endif // _TARGET_ARM_

    fprintf(fout, "\n");
    fprintf(fout, "SC_IG_BUFFER_SIZE             = %2u\n", SC_IG_BUFFER_SIZE);
    fprintf(fout, "SMALL_IDSC_SIZE per IG buffer = %2u\n", SC_IG_BUFFER_SIZE / SMALL_IDSC_SIZE);
    fprintf(fout, "instrDesc per IG buffer       = %2u\n", SC_IG_BUFFER_SIZE / sizeof(emitter::instrDesc));
    fprintf(fout, "instrDescJmp per IG buffer    = %2u\n", SC_IG_BUFFER_SIZE / sizeof(emitter::instrDescJmp));
#if !defined(_TARGET_ARM64_)
    fprintf(fout, "instrDescLbl per IG buffer    = %2u\n", SC_IG_BUFFER_SIZE / sizeof(emitter::instrDescLbl));
#endif // !defined(_TARGET_ARM64_)
    fprintf(fout, "instrDescCns per IG buffer    = %2u\n", SC_IG_BUFFER_SIZE / sizeof(emitter::instrDescCns));
    fprintf(fout, "instrDescDsp per IG buffer    = %2u\n", SC_IG_BUFFER_SIZE / sizeof(emitter::instrDescDsp));
    fprintf(fout, "instrDescCnsDsp per IG buffer = %2u\n", SC_IG_BUFFER_SIZE / sizeof(emitter::instrDescCnsDsp));
#ifdef _TARGET_XARCH_
    fprintf(fout, "instrDescAmd per IG buffer    = %2u\n", SC_IG_BUFFER_SIZE / sizeof(emitter::instrDescAmd));
    fprintf(fout, "instrDescCnsAmd per IG buffer = %2u\n", SC_IG_BUFFER_SIZE / sizeof(emitter::instrDescCnsAmd));
#endif // _TARGET_XARCH_
    fprintf(fout, "instrDescCGCA per IG buffer   = %2u\n", SC_IG_BUFFER_SIZE / sizeof(emitter::instrDescCGCA));
#ifdef _TARGET_ARM_
    fprintf(fout, "instrDescReloc per IG buffer  = %2u\n", SC_IG_BUFFER_SIZE / sizeof(emitter::instrDescReloc));
#endif // _TARGET_ARM_

    fprintf(fout, "\n");
    fprintf(fout, "GCInfo::regPtrDsc:\n");
    fprintf(fout, "Offset of rpdNext           = %2u\n", offsetof(GCInfo::regPtrDsc, rpdNext));
    fprintf(fout, "Offset of rpdOffs           = %2u\n", offsetof(GCInfo::regPtrDsc, rpdOffs));
    fprintf(fout, "Offset of <union>           = %2u\n", offsetof(GCInfo::regPtrDsc, rpdPtrArg));
    fprintf(fout, "Size   of GCInfo::regPtrDsc = %2u\n", sizeof(GCInfo::regPtrDsc));

    fprintf(fout, "\n");
}

void emitterStats(FILE* fout)
{
    if (totAllocdSize > 0)
    {
        assert(totActualSize <= totAllocdSize);

        fprintf(fout, "\nTotal allocated code size = %u\n", totAllocdSize);

        if (totActualSize < totAllocdSize)
        {
            fprintf(fout, "Total generated code size = %u  ", totActualSize);

            fprintf(fout, "(%4.3f%% waste)", 100 * ((totAllocdSize - totActualSize) / (double)totActualSize));
            fprintf(fout, "\n");
        }

        assert(emitter::emitTotalInsCnt > 0);

        fprintf(fout, "Average of %4.2f bytes of code generated per instruction\n",
                (double)totActualSize / emitter::emitTotalInsCnt);
    }

    fprintf(fout, "\nInstruction format frequency table:\n\n");

    unsigned f, ic = 0, dc = 0;

    for (f = 0; f < emitter::IF_COUNT; f++)
    {
        ic += emitter::emitIFcounts[f];
    }

    for (f = 0; f < emitter::IF_COUNT; f++)
    {
        unsigned c = emitter::emitIFcounts[f];

        if ((c > 0) && (1000 * c >= ic))
        {
            dc += c;
            fprintf(fout, "          %-14s %8u (%5.2f%%)\n", emitter::emitIfName(f), c, 100.0 * c / ic);
        }
    }

    fprintf(fout, "         ---------------------------------\n");
    fprintf(fout, "          %-14s %8u (%5.2f%%)\n", "Total shown", dc, 100.0 * dc / ic);

    if (emitter::emitTotalIGmcnt > 0)
    {
        fprintf(fout, "\n");
        fprintf(fout, "Total of %8u methods\n", emitter::emitTotalIGmcnt);
        fprintf(fout, "Total of %8u insGroup\n", emitter::emitTotalIGcnt);
        fprintf(fout, "Total of %8u insPlaceholderGroupData\n", emitter::emitTotalPhIGcnt);
        fprintf(fout, "Total of %8u emitAdd insGroup\n", emitter::emitTotalIGEmitAdd);
        fprintf(fout, "Total of %8u instructions\n", emitter::emitTotalIGicnt);
        fprintf(fout, "Total of %8u jumps\n", emitter::emitTotalIGjmps);
        fprintf(fout, "Total of %8u GC livesets\n", emitter::emitTotalIGptrs);
        fprintf(fout, "\n");
        fprintf(fout, "Max prolog instrDesc count: %8u\n", emitter::emitMaxPrologInsCnt);
        fprintf(fout, "Max prolog insGroup size  : %8u\n", emitter::emitMaxPrologIGSize);
        fprintf(fout, "\n");
        fprintf(fout, "Average of %8.1lf insGroup     per method\n",
                (double)emitter::emitTotalIGcnt / emitter::emitTotalIGmcnt);
        fprintf(fout, "Average of %8.1lf insPhGroup   per method\n",
                (double)emitter::emitTotalPhIGcnt / emitter::emitTotalIGmcnt);
        fprintf(fout, "Average of %8.1lf emitAdd IG   per method\n",
                (double)emitter::emitTotalIGEmitAdd / emitter::emitTotalIGmcnt);
        fprintf(fout, "Average of %8.1lf instructions per method\n",
                (double)emitter::emitTotalIGicnt / emitter::emitTotalIGmcnt);
        fprintf(fout, "Average of %8.1lf desc.  bytes per method\n",
                (double)emitter::emitTotalIGsize / emitter::emitTotalIGmcnt);
        fprintf(fout, "Average of %8.1lf jumps        per method\n",
                (double)emitter::emitTotalIGjmps / emitter::emitTotalIGmcnt);
        fprintf(fout, "Average of %8.1lf GC livesets  per method\n",
                (double)emitter::emitTotalIGptrs / emitter::emitTotalIGmcnt);
        fprintf(fout, "\n");
        fprintf(fout, "Average of %8.1lf instructions per group \n",
                (double)emitter::emitTotalIGicnt / emitter::emitTotalIGcnt);
        fprintf(fout, "Average of %8.1lf desc.  bytes per group \n",
                (double)emitter::emitTotalIGsize / emitter::emitTotalIGcnt);
        fprintf(fout, "Average of %8.1lf jumps        per group \n",
                (double)emitter::emitTotalIGjmps / emitter::emitTotalIGcnt);
        fprintf(fout, "\n");
        fprintf(fout, "Average of %8.1lf bytes        per instrDesc\n",
                (double)emitter::emitTotalIGsize / emitter::emitTotalIGicnt);
        fprintf(fout, "\n");
        fprintf(fout, "A total of %8u desc.  bytes\n", emitter::emitTotalIGsize);
        fprintf(fout, "\n");

        fprintf(fout, "Total instructions:    %8u\n", emitter::emitTotalInsCnt);
        fprintf(fout, "Total small instrDesc: %8u (%5.2f%%)\n", emitter::emitTotalIDescSmallCnt,
                100.0 * emitter::emitTotalIDescSmallCnt / emitter::emitTotalInsCnt);
        fprintf(fout, "Total instrDesc:       %8u (%5.2f%%)\n", emitter::emitTotalIDescCnt,
                100.0 * emitter::emitTotalIDescCnt / emitter::emitTotalInsCnt);
        fprintf(fout, "Total instrDescJmp:    %8u (%5.2f%%)\n", emitter::emitTotalIDescJmpCnt,
                100.0 * emitter::emitTotalIDescJmpCnt / emitter::emitTotalInsCnt);
#if !defined(_TARGET_ARM64_)
        fprintf(fout, "Total instrDescLbl:    %8u (%5.2f%%)\n", emitter::emitTotalIDescLblCnt,
                100.0 * emitter::emitTotalIDescLblCnt / emitter::emitTotalInsCnt);
#endif // !defined(_TARGET_ARM64_)
        fprintf(fout, "Total instrDescCns:    %8u (%5.2f%%)\n", emitter::emitTotalIDescCnsCnt,
                100.0 * emitter::emitTotalIDescCnsCnt / emitter::emitTotalInsCnt);
        fprintf(fout, "Total instrDescDsp:    %8u (%5.2f%%)\n", emitter::emitTotalIDescDspCnt,
                100.0 * emitter::emitTotalIDescDspCnt / emitter::emitTotalInsCnt);
        fprintf(fout, "Total instrDescCnsDsp: %8u (%5.2f%%)\n", emitter::emitTotalIDescCnsDspCnt,
                100.0 * emitter::emitTotalIDescCnsDspCnt / emitter::emitTotalInsCnt);
#ifdef _TARGET_XARCH_
        fprintf(fout, "Total instrDescAmd:    %8u (%5.2f%%)\n", emitter::emitTotalIDescAmdCnt,
                100.0 * emitter::emitTotalIDescAmdCnt / emitter::emitTotalInsCnt);
        fprintf(fout, "Total instrDescCnsAmd: %8u (%5.2f%%)\n", emitter::emitTotalIDescCnsAmdCnt,
                100.0 * emitter::emitTotalIDescCnsAmdCnt / emitter::emitTotalInsCnt);
#endif // _TARGET_XARCH_
        fprintf(fout, "Total instrDescCGCA:   %8u (%5.2f%%)\n", emitter::emitTotalIDescCGCACnt,
                100.0 * emitter::emitTotalIDescCGCACnt / emitter::emitTotalInsCnt);
#ifdef _TARGET_ARM_
        fprintf(fout, "Total instrDescReloc:  %8u (%5.2f%%)\n", emitter::emitTotalIDescRelocCnt,
                100.0 * emitter::emitTotalIDescRelocCnt / emitter::emitTotalInsCnt);
#endif // _TARGET_ARM_
        fprintf(fout, "\n");
    }

    fprintf(fout, "Descriptor size distribution:\n");
    emitSizeTable.dump(fout);
    fprintf(fout, "\n");

    fprintf(fout, "GC ref frame variable counts:\n");
    GCrefsTable.dump(fout);
    fprintf(fout, "\n");

    fprintf(fout, "Max. stack depth distribution:\n");
    stkDepthTable.dump(fout);
    fprintf(fout, "\n");

    if ((emitter::emitSmallCnsCnt > 0) || (emitter::emitLargeCnsCnt > 0))
    {
        fprintf(fout, "SmallCnsCnt = %6u\n", emitter::emitSmallCnsCnt);
        fprintf(fout, "LargeCnsCnt = %6u (%3u %% of total)\n", emitter::emitLargeCnsCnt,
                100 * emitter::emitLargeCnsCnt / (emitter::emitLargeCnsCnt + emitter::emitSmallCnsCnt));
    }

    // Print out the most common small constants.
    if (emitter::emitSmallCnsCnt > 0)
    {
        fprintf(fout, "\n\n");
        fprintf(fout, "Common small constants >= %2u, <= %2u\n", ID_MIN_SMALL_CNS, ID_MAX_SMALL_CNS);

        unsigned m = emitter::emitSmallCnsCnt / 1000 + 1;

        for (int i = ID_MIN_SMALL_CNS; (i <= ID_MAX_SMALL_CNS) && (i < SMALL_CNS_TSZ); i++)
        {
            unsigned c = emitter::emitSmallCns[i - ID_MIN_SMALL_CNS];
            if (c >= m)
            {
                if (i == SMALL_CNS_TSZ - 1)
                {
                    fprintf(fout, "cns[>=%4d] = %u\n", i, c);
                }
                else
                {
                    fprintf(fout, "cns[%4d] = %u\n", i, c);
                }
            }
        }
    }

    fprintf(fout, "%8u bytes allocated in the emitter\n", emitter::emitTotMemAlloc);
}

#endif // EMITTER_STATS

/*****************************************************************************/

const unsigned short emitTypeSizes[] = {
#define DEF_TP(tn, nm, jitType, verType, sz, sze, asze, st, al, tf, howUsed) sze,
#include "typelist.h"
#undef DEF_TP
};

const unsigned short emitTypeActSz[] = {
#define DEF_TP(tn, nm, jitType, verType, sz, sze, asze, st, al, tf, howUsed) asze,
#include "typelist.h"
#undef DEF_TP
};

/*****************************************************************************/
/*****************************************************************************
 *
 *  Initialize the emitter - called once, at DLL load time.
 */

void emitter::emitInit()
{
}

/*****************************************************************************
 *
 *  Shut down the emitter - called once, at DLL exit time.
 */

void emitter::emitDone()
{
}

/*****************************************************************************
 *
 *  Allocate memory.
 */

void* emitter::emitGetMem(size_t sz)
{
    assert(sz % sizeof(int) == 0);

#if EMITTER_STATS
    emitTotMemAlloc += sz;
#endif

    return emitComp->getAllocator(CMK_InstDesc).allocate<char>(sz);
}

/*****************************************************************************
 *
 *  emitLclVarAddr support methods
 */
void emitLclVarAddr::initLclVarAddr(int varNum, unsigned offset)
{
    if (varNum < 32768)
    {
        if (varNum >= 0)
        {
            if (offset < 32768)
            {
                _lvaTag    = LVA_STANDARD_ENCODING;
                _lvaExtra  = offset;           // offset known to be in [0..32767]
                _lvaVarNum = (unsigned)varNum; // varNum known to be in [0..32767]
            }
            else // offset >= 32768
            {
                // We could support larger local offsets here at the cost of less varNums
                if (offset >= 65536)
                {
                    IMPL_LIMITATION("JIT doesn't support offsets larger than 65535 into valuetypes\n");
                }

                _lvaTag    = LVA_LARGE_OFFSET;
                _lvaExtra  = (offset - 32768); // (offset-32768) is known to be in [0..32767]
                _lvaVarNum = (unsigned)varNum; // varNum known to be in [0..32767]
            }
        }
        else // varNum < 0, These are used for Compiler spill temps
        {
            if (varNum < -32767)
            {
                IMPL_LIMITATION("JIT doesn't support more than 32767 Compiler Spill temps\n");
            }
            if (offset > 32767)
            {
                IMPL_LIMITATION(
                    "JIT doesn't support offsets larger than 32767 into valuetypes for Compiler Spill temps\n");
            }

            _lvaTag    = LVA_COMPILER_TEMP;
            _lvaExtra  = offset;              //  offset known to be in [0..32767]
            _lvaVarNum = (unsigned)(-varNum); // -varNum known to be in [1..32767]
        }
    }
    else // varNum >= 32768
    {
        if (offset >= 256)
        {
            IMPL_LIMITATION("JIT doesn't support offsets larger than 255 into valuetypes for local vars > 32767\n");
        }
        if (varNum >= 0x00400000)
        { // 0x00400000 == 2^22
            IMPL_LIMITATION("JIT doesn't support more than 2^22 variables\n");
        }

        _lvaTag    = LVA_LARGE_VARNUM;
        _lvaVarNum = varNum & 0x00007FFF;         // varNum bits 14 to 0
        _lvaExtra  = (varNum & 0x003F8000) >> 15; // varNum bits 21 to 15 in _lvaExtra bits  6 to 0, 7 bits total
        _lvaExtra |= (offset << 7);               // offset bits  7 to 0  in _lvaExtra bits 14 to 7, 8 bits total
    }
}

// Returns the variable to access. Note that it returns a negative number for compiler spill temps.
int emitLclVarAddr::lvaVarNum()
{
    switch (_lvaTag)
    {
        case LVA_COMPILER_TEMP:
            return -((int)_lvaVarNum);
        case LVA_LARGE_VARNUM:
            return (int)(((_lvaExtra & 0x007F) << 15) + _lvaVarNum);
        default: // LVA_STANDARD_ENCODING or LVA_LARGE_OFFSET
            assert((_lvaTag == LVA_STANDARD_ENCODING) || (_lvaTag == LVA_LARGE_OFFSET));
            return (int)_lvaVarNum;
    }
}

unsigned emitLclVarAddr::lvaOffset() // returns the offset into the variable to access
{
    switch (_lvaTag)
    {
        case LVA_LARGE_OFFSET:
            return (32768 + _lvaExtra);
        case LVA_LARGE_VARNUM:
            return (_lvaExtra & 0x7F80) >> 7;
        default: // LVA_STANDARD_ENCODING or LVA_COMPILER_TEMP
            assert((_lvaTag == LVA_STANDARD_ENCODING) || (_lvaTag == LVA_COMPILER_TEMP));
            return _lvaExtra;
    }
}

/*****************************************************************************
 *
 *  Record some info about the method about to be emitted.
 */

void emitter::emitBegCG(Compiler* comp, COMP_HANDLE cmpHandle)
{
    emitComp      = comp;
    emitCmpHandle = cmpHandle;
}

void emitter::emitEndCG()
{
}

/*****************************************************************************
 *
 *  Prepare the given IG for emission of code.
 */

void emitter::emitGenIG(insGroup* ig)
{
    /* Set the "current IG" value */

    emitCurIG = ig;

#if EMIT_TRACK_STACK_DEPTH

    /* Record the stack level on entry to this group */

    ig->igStkLvl = emitCurStackLvl;

    // If we don't have enough bits in igStkLvl, refuse to compile

    if (ig->igStkLvl != emitCurStackLvl)
    {
        IMPL_LIMITATION("Too many arguments pushed on stack");
    }

//  printf("Start IG #%02u [stk=%02u]\n", ig->igNum, emitCurStackLvl);

#endif

    if (emitNoGCIG)
    {
        ig->igFlags |= IGF_NOGCINTERRUPT;
    }

    /* Prepare to issue instructions */

    emitCurIGinsCnt = 0;
    emitCurIGsize   = 0;

    assert(emitCurIGjmpList == nullptr);

    /* Allocate the temp instruction buffer if we haven't done so */

    if (emitCurIGfreeBase == nullptr)
    {
        emitIGbuffSize    = SC_IG_BUFFER_SIZE;
        emitCurIGfreeBase = (BYTE*)emitGetMem(emitIGbuffSize);
    }

    emitCurIGfreeNext = emitCurIGfreeBase;
    emitCurIGfreeEndp = emitCurIGfreeBase + emitIGbuffSize;
}

/*****************************************************************************
 *
 *  Finish and save the current IG.
 */

insGroup* emitter::emitSavIG(bool emitAdd)
{
    insGroup* ig;
    BYTE*     id;

    size_t sz;
    size_t gs;

    assert(emitCurIGfreeNext <= emitCurIGfreeEndp);

    /* Get hold of the IG descriptor */

    ig = emitCurIG;
    assert(ig);

    /* Compute how much code we've generated */

    sz = emitCurIGfreeNext - emitCurIGfreeBase;

    /* Compute the total size we need to allocate */

    gs = roundUp(sz);

    /* Do we need space for GC? */

    if (!(ig->igFlags & IGF_EMIT_ADD))
    {
        /* Is the initial set of live GC vars different from the previous one? */

        if (emitForceStoreGCState || !VarSetOps::Equal(emitComp, emitPrevGCrefVars, emitInitGCrefVars))
        {
            /* Remember that we will have a new set of live GC variables */

            ig->igFlags |= IGF_GC_VARS;

#if EMITTER_STATS
            emitTotalIGptrs++;
#endif

            /* We'll allocate extra space to record the liveset */

            gs += sizeof(VARSET_TP);
        }

        /* Is the initial set of live Byref regs different from the previous one? */

        /* Remember that we will have a new set of live GC variables */

        ig->igFlags |= IGF_BYREF_REGS;

        /* We'll allocate extra space (DWORD aligned) to record the GC regs */

        gs += sizeof(int);
    }

    /* Allocate space for the instructions and optional liveset */

    id = (BYTE*)emitGetMem(gs);

    /* Do we need to store the byref regs */

    if (ig->igFlags & IGF_BYREF_REGS)
    {
        /* Record the byref regs in front the of the instructions */

        *castto(id, unsigned*)++ = (unsigned)emitInitByrefRegs;
    }

    /* Do we need to store the liveset? */

    if (ig->igFlags & IGF_GC_VARS)
    {
        /* Record the liveset in front the of the instructions */
        VarSetOps::AssignNoCopy(emitComp, (*castto(id, VARSET_TP*)), VarSetOps::MakeEmpty(emitComp));
        VarSetOps::Assign(emitComp, (*castto(id, VARSET_TP*)++), emitInitGCrefVars);
    }

    /* Record the collected instructions */

    assert((ig->igFlags & IGF_PLACEHOLDER) == 0);
    ig->igData = id;

    memcpy(id, emitCurIGfreeBase, sz);

#ifdef DEBUG
    if (false && emitComp->verbose) // this is not useful in normal dumps (hence it is normally under if (false)
    {
        // If there's an error during emission, we may want to connect the post-copy address
        // of an instrDesc with the pre-copy address (the one that was originally created).  This
        // printing enables that.
        printf("copying instruction group from [0x%x..0x%x) to [0x%x..0x%x).\n", dspPtr(emitCurIGfreeBase),
               dspPtr(emitCurIGfreeBase + sz), dspPtr(id), dspPtr(id + sz));
    }
#endif

    /* Record how many instructions and bytes of code this group contains */

    noway_assert((BYTE)emitCurIGinsCnt == emitCurIGinsCnt);
    noway_assert((unsigned short)emitCurIGsize == emitCurIGsize);

    ig->igInsCnt = (BYTE)emitCurIGinsCnt;
    ig->igSize   = (unsigned short)emitCurIGsize;
    emitCurCodeOffset += emitCurIGsize;
    assert(IsCodeAligned(emitCurCodeOffset));

#if EMITTER_STATS
    emitTotalIGicnt += emitCurIGinsCnt;
    emitTotalIGsize += sz;
    emitSizeMethod += sz;

    if (emitIGisInProlog(ig))
    {
        emitCurPrologInsCnt += emitCurIGinsCnt;
        emitCurPrologIGSize += sz;

        // Keep track of the maximums.
        if (emitCurPrologInsCnt > emitMaxPrologInsCnt)
        {
            emitMaxPrologInsCnt = emitCurPrologInsCnt;
        }
        if (emitCurPrologIGSize > emitMaxPrologIGSize)
        {
            emitMaxPrologIGSize = emitCurPrologIGSize;
        }
    }
#endif

    // printf("Group [%08X]%3u has %2u instructions (%4u bytes at %08X)\n", ig, ig->igNum, emitCurIGinsCnt, sz, id);

    /* Record the live GC register set - if and only if it is not an emitter added block */

    if (!(ig->igFlags & IGF_EMIT_ADD))
    {
        ig->igGCregs = (regMaskSmall)emitInitGCrefRegs;
    }

    if (!emitAdd)
    {
        /* Update the previous recorded live GC ref sets, but not if
           if we are starting an "overflow" buffer. Note that this is
           only used to determine whether we need to store or not store
           the GC ref sets for the next IG, which is dependent on exactly
           what the state of the emitter GC ref sets will be when the
           next IG is processed in the emitter.
         */

        VarSetOps::Assign(emitComp, emitPrevGCrefVars, emitThisGCrefVars);
        emitPrevGCrefRegs = emitThisGCrefRegs;
        emitPrevByrefRegs = emitThisByrefRegs;

        emitForceStoreGCState = false;
    }

#ifdef DEBUG
    if (emitComp->opts.dspCode)
    {
        printf("\n      G_M%03u_IG%02u:", Compiler::s_compMethodsCount, ig->igNum);
        if (emitComp->verbose)
        {
            printf("        ; offs=%06XH, funclet=%02u", ig->igOffs, ig->igFuncIdx);
        }
        else
        {
            printf("        ; funclet=%02u", ig->igFuncIdx);
        }
        printf("\n");
    }
#endif

    /* Did we have any jumps in this group? */

    if (emitCurIGjmpList)
    {
        instrDescJmp* list = nullptr;
        instrDescJmp* last = nullptr;

        /* Move jumps to the global list, update their 'next' links */

        do
        {
            /* Grab the jump and remove it from the list */

            instrDescJmp* oj = emitCurIGjmpList;
            emitCurIGjmpList = oj->idjNext;

            /* Figure out the address of where the jump got copied */

            size_t        of = (BYTE*)oj - emitCurIGfreeBase;
            instrDescJmp* nj = (instrDescJmp*)(ig->igData + of);

            // printf("Jump moved from %08X to %08X\n", oj, nj);
            // printf("jmp [%08X] at %08X + %03u\n", nj, ig, nj->idjOffs);

            assert(nj->idjIG == ig);
            assert(nj->idIns() == oj->idIns());
            assert(nj->idjNext == oj->idjNext);

            /* Make sure the jumps are correctly ordered */

            assert(last == nullptr || last->idjOffs > nj->idjOffs);

            if (ig->igFlags & IGF_FUNCLET_PROLOG)
            {
                // Our funclet prologs have short jumps, if the prolog would ever have
                // long jumps, then we'd have to insert the list in sorted order than
                // just append to the emitJumpList.
                noway_assert(nj->idjShort);
                if (nj->idjShort)
                {
                    continue;
                }
            }

            /* Append the new jump to the list */

            nj->idjNext = list;
            list        = nj;

            if (last == nullptr)
            {
                last = nj;
            }
        } while (emitCurIGjmpList);

        if (last != nullptr)
        {
            /* Append the jump(s) from this IG to the global list */
            bool prologJump = (ig == emitPrologIG);
            if ((emitJumpList == nullptr) || prologJump)
            {
                last->idjNext = emitJumpList;
                emitJumpList  = list;
            }
            else
            {
                last->idjNext         = nullptr;
                emitJumpLast->idjNext = list;
            }

            if (!prologJump || (emitJumpLast == nullptr))
            {
                emitJumpLast = last;
            }
        }
    }

    /* Fix the last instruction field */

    if (sz != 0)
    {
        assert(emitLastIns != nullptr);
        assert(emitCurIGfreeBase <= (BYTE*)emitLastIns);
        assert((BYTE*)emitLastIns < emitCurIGfreeBase + sz);
        emitLastIns = (instrDesc*)((BYTE*)id + ((BYTE*)emitLastIns - (BYTE*)emitCurIGfreeBase));
    }

    /* Reset the buffer free pointers */

    emitCurIGfreeNext = emitCurIGfreeBase;

    return ig;
}

/*****************************************************************************
 *
 *  Start generating code to be scheduled; called once per method.
 */

void emitter::emitBegFN(bool hasFramePtr
#if defined(DEBUG)
                        ,
                        bool chkAlign
#endif
                        ,
                        unsigned maxTmpSize)
{
    insGroup* ig;

    /* Assume we won't need the temp instruction buffer */

    emitCurIGfreeBase = nullptr;
    emitIGbuffSize    = 0;

    /* Record stack frame info (the temp size is just an estimate) */

    emitHasFramePtr = hasFramePtr;

    emitMaxTmpSize = maxTmpSize;

#ifdef DEBUG
    emitChkAlign = chkAlign;
#endif

    /* We have no epilogs yet */

    emitEpilogSize = 0;
    emitEpilogCnt  = 0;

#ifdef _TARGET_XARCH_
    emitExitSeqBegLoc.Init();
    emitExitSeqSize = INT_MAX;
#endif // _TARGET_XARCH_

    emitPlaceholderList = emitPlaceholderLast = nullptr;

#ifdef JIT32_GCENCODER
    emitEpilogList = emitEpilogLast = nullptr;
#endif // JIT32_GCENCODER

    /* We don't have any jumps */

    emitJumpList = emitJumpLast = nullptr;
    emitCurIGjmpList            = nullptr;

    emitFwdJumps   = false;
    emitNoGCIG     = false;
    emitForceNewIG = false;

    /* We have not recorded any live sets */

    assert(VarSetOps::IsEmpty(emitComp, emitThisGCrefVars));
    assert(VarSetOps::IsEmpty(emitComp, emitInitGCrefVars));
    assert(VarSetOps::IsEmpty(emitComp, emitPrevGCrefVars));
    emitThisGCrefRegs = RBM_NONE;
    emitInitGCrefRegs = RBM_NONE;
    emitPrevGCrefRegs = RBM_NONE;
    emitThisByrefRegs = RBM_NONE;
    emitInitByrefRegs = RBM_NONE;
    emitPrevByrefRegs = RBM_NONE;

    emitForceStoreGCState = false;

#ifdef DEBUG

    emitIssuing = false;

#endif

    /* Assume there will be no GC ref variables */

    emitGCrFrameOffsMin = emitGCrFrameOffsMax = emitGCrFrameOffsCnt = 0;
#ifdef DEBUG
    emitGCrFrameLiveTab = nullptr;
#endif

    /* We have no groups / code at this point */

    emitIGlist = emitIGlast = nullptr;

    emitCurCodeOffset = 0;
    emitFirstColdIG   = nullptr;
    emitTotalCodeSize = 0;

#if EMITTER_STATS
    emitTotalIGmcnt++;
    emitSizeMethod      = 0;
    emitCurPrologInsCnt = 0;
    emitCurPrologIGSize = 0;
#endif

    emitInsCount = 0;

    /* The stack is empty now */

    emitCurStackLvl = 0;

#if EMIT_TRACK_STACK_DEPTH
    emitMaxStackDepth = 0;
    emitCntStackDepth = sizeof(int);
#endif

#ifdef PSEUDORANDOM_NOP_INSERTION
    // for random NOP insertion

    emitEnableRandomNops();
    emitComp->info.compRNG.Init(emitComp->info.compChecksum);
    emitNextNop           = emitNextRandomNop();
    emitInInstrumentation = false;
#endif // PSEUDORANDOM_NOP_INSERTION

    /* Create the first IG, it will be used for the prolog */

    emitNxtIGnum = 1;

    emitPrologIG = emitIGlist = emitIGlast = emitCurIG = ig = emitAllocIG();

    emitLastIns = nullptr;

    ig->igNext = nullptr;

#ifdef DEBUG
    emitScratchSigInfo = nullptr;
#endif // DEBUG

    /* Append another group, to start generating the method body */

    emitNewIG();
}

#ifdef PSEUDORANDOM_NOP_INSERTION
int emitter::emitNextRandomNop()
{
    return emitComp->info.compRNG.Next(1, 9);
}
#endif

/*****************************************************************************
 *
 *  Done generating code to be scheduled; called once per method.
 */

void emitter::emitEndFN()
{
}

// member function iiaIsJitDataOffset for idAddrUnion, defers to Compiler::eeIsJitDataOffs
bool emitter::instrDesc::idAddrUnion::iiaIsJitDataOffset() const
{
    return Compiler::eeIsJitDataOffs(iiaFieldHnd);
}

// member function iiaGetJitDataOffset for idAddrUnion, defers to Compiler::eeGetJitDataOffs
int emitter::instrDesc::idAddrUnion::iiaGetJitDataOffset() const
{
    assert(iiaIsJitDataOffset());
    return Compiler::eeGetJitDataOffs(iiaFieldHnd);
}

void emitter::dispIns(instrDesc* id)
{
#ifdef DEBUG
    emitInsSanityCheck(id);

    if (emitComp->opts.dspCode)
    {
        emitDispIns(id, true, false, false);
    }

#if EMIT_TRACK_STACK_DEPTH
    assert((int)emitCurStackLvl >= 0);
#endif
    size_t sz = emitSizeOfInsDsc(id);
    assert(id->idDebugOnlyInfo()->idSize == sz);
#endif // DEBUG

#if EMITTER_STATS
    emitIFcounts[id->idInsFmt()]++;
#endif
}

void emitter::appendToCurIG(instrDesc* id)
{
    emitCurIGsize += id->idCodeSize();
}

/*****************************************************************************
 *
 *  Display (optionally) an instruction offset.
 */

#ifdef DEBUG

void emitter::emitDispInsOffs(unsigned offs, bool doffs)
{
    if (doffs)
    {
        printf("%06X", offs);
    }
    else
    {
        printf("      ");
    }
}

#endif // DEBUG

#ifdef JIT32_GCENCODER

/*****************************************************************************
 *
 *  Call the specified function pointer for each epilog block in the current
 *  method with the epilog's relative code offset. Returns the sum of the
 *  values returned by the callback.
 */

size_t emitter::emitGenEpilogLst(size_t (*fp)(void*, unsigned), void* cp)
{
    EpilogList* el;
    size_t      sz;

    for (el = emitEpilogList, sz = 0; el != nullptr; el = el->elNext)
    {
        assert(el->elLoc.GetIG()->igFlags & IGF_EPILOG);

        // The epilog starts at the location recorded in the epilog list.
        sz += fp(cp, el->elLoc.CodeOffset(this));
    }

    return sz;
}

#endif // JIT32_GCENCODER

/*****************************************************************************
 *
 *  The following series of methods allocates instruction descriptors.
 */

void* emitter::emitAllocAnyInstr(size_t sz, emitAttr opsz)
{
    instrDesc* id;

#ifdef DEBUG
    // Under STRESS_EMITTER, put every instruction in its own instruction group.
    // We can't do this for a prolog, epilog, funclet prolog, or funclet epilog,
    // because those are generated out of order. We currently have a limitation
    // where the jump shortening pass uses the instruction group number to determine
    // if something is earlier or later in the code stream. This implies that
    // these groups cannot be more than a single instruction group. Note that
    // the prolog/epilog placeholder groups ARE generated in order, and are
    // re-used. But generating additional groups would not work.
    if (emitComp->compStressCompile(Compiler::STRESS_EMITTER, 1) && emitCurIGinsCnt && !emitIGisInProlog(emitCurIG) &&
        !emitIGisInEpilog(emitCurIG)
#if FEATURE_EH_FUNCLETS
        && !emitIGisInFuncletProlog(emitCurIG) && !emitIGisInFuncletEpilog(emitCurIG)
#endif // FEATURE_EH_FUNCLETS
            )
    {
        emitNxtIG(true);
    }
#endif

#ifdef PSEUDORANDOM_NOP_INSERTION
    // TODO-ARM-Bug?: PSEUDORANDOM_NOP_INSERTION is not defined for _TARGET_ARM_
    //     ARM - This is currently broken on _TARGET_ARM_
    //     When nopSize is odd we misalign emitCurIGsize
    //
    if (!emitComp->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT) && !emitInInstrumentation &&
        !emitIGisInProlog(emitCurIG) && // don't do this in prolog or epilog
        !emitIGisInEpilog(emitCurIG) &&
        emitRandomNops // sometimes we turn off where exact codegen is needed (pinvoke inline)
        )
    {
        if (emitNextNop == 0)
        {
            int nopSize           = 4;
            emitInInstrumentation = true;
            instrDesc* idnop      = emitNewInstr();
            emitInInstrumentation = false;
            idnop->idInsFmt(IF_NONE);
            idnop->idIns(INS_nop);
#if defined(_TARGET_XARCH_)
            idnop->idCodeSize(nopSize);
#else
#error "Undefined target for pseudorandom NOP insertion"
#endif

            emitCurIGsize += nopSize;
            emitNextNop = emitNextRandomNop();
        }
        else
            emitNextNop--;
    }
#endif // PSEUDORANDOM_NOP_INSERTION

    assert(IsCodeAligned(emitCurIGsize));

    /* Make sure we have enough space for the new instruction */

    if ((emitCurIGfreeNext + sz >= emitCurIGfreeEndp) || emitForceNewIG)
    {
        emitNxtIG(true);
    }

    /* Grab the space for the instruction */

    emitLastIns = id = (instrDesc*)emitCurIGfreeNext;
    emitCurIGfreeNext += sz;

    assert(sz >= sizeof(void*));
    memset(id, 0, sz);

    // These fields should have been zero-ed by the above
    assert(id->idReg1() == regNumber(0));
    assert(id->idReg2() == regNumber(0));
#ifdef _TARGET_XARCH_
    assert(id->idCodeSize() == 0);
#endif

    // Make sure that idAddrUnion is just a union of various pointer sized things
    C_ASSERT(sizeof(CORINFO_FIELD_HANDLE) <= sizeof(void*));
    C_ASSERT(sizeof(CORINFO_METHOD_HANDLE) <= sizeof(void*));
    C_ASSERT(sizeof(emitter::emitAddrMode) <= sizeof(void*));
    C_ASSERT(sizeof(emitLclVarAddr) <= sizeof(void*));
    C_ASSERT(sizeof(emitter::instrDesc) == (SMALL_IDSC_SIZE + sizeof(void*)));

    emitInsCount++;

#if defined(DEBUG)
    /* In debug mode we clear/set some additional fields */

    instrDescDebugInfo* info = (instrDescDebugInfo*)emitGetMem(sizeof(*info));

    info->idNum        = emitInsCount;
    info->idSize       = sz;
    info->idVarRefOffs = 0;
    info->idMemCookie  = 0;
#ifdef TRANSLATE_PDB
    info->idilStart = emitInstrDescILBase;
#endif
    info->idFinallyCall = false;
    info->idCatchRet    = false;
    info->idCallSig     = nullptr;

    id->idDebugOnlyInfo(info);

#endif // defined(DEBUG)

    /* Store the size and handle the two special values
       that indicate GCref and ByRef */

    if (EA_IS_GCREF(opsz))
    {
        /* A special value indicates a GCref pointer value */

        id->idGCref(GCT_GCREF);
        id->idOpSize(EA_PTRSIZE);
    }
    else if (EA_IS_BYREF(opsz))
    {
        /* A special value indicates a Byref pointer value */

        id->idGCref(GCT_BYREF);
        id->idOpSize(EA_PTRSIZE);
    }
    else
    {
        id->idGCref(GCT_NONE);
        id->idOpSize(EA_SIZE(opsz));
    }

    // Amd64: ip-relative addressing is supported even when not generating relocatable ngen code
    if (EA_IS_DSP_RELOC(opsz)
#ifndef _TARGET_AMD64_
        && emitComp->opts.compReloc
#endif //_TARGET_AMD64_
        )
    {
        /* Mark idInfo()->idDspReloc to remember that the            */
        /* address mode has a displacement that is relocatable       */
        id->idSetIsDspReloc();
    }

    if (EA_IS_CNS_RELOC(opsz) && emitComp->opts.compReloc)
    {
        /* Mark idInfo()->idCnsReloc to remember that the            */
        /* instruction has an immediate constant that is relocatable */
        id->idSetIsCnsReloc();
    }

#if EMITTER_STATS
    emitTotalInsCnt++;
#endif

    /* Update the instruction count */

    emitCurIGinsCnt++;

    return id;
}

#ifdef DEBUG

//------------------------------------------------------------------------
// emitCheckIGoffsets: Make sure the code offsets of all instruction groups look reasonable.
//
// Note: It checks that each instruction group starts right after the previous ig.
// For the first cold ig offset is also should be the last hot ig + its size.
// emitCurCodeOffs maintains distance for the split case to look like they are consistent.
// Also it checks total code size.
//
void emitter::emitCheckIGoffsets()
{
    size_t currentOffset = 0;

    for (insGroup* tempIG = emitIGlist; tempIG != nullptr; tempIG = tempIG->igNext)
    {
        if (tempIG->igOffs != currentOffset)
        {
            printf("Block #%u has offset %08X, expected %08X\n", tempIG->igNum, tempIG->igOffs, currentOffset);
            assert(!"bad block offset");
        }

        currentOffset += tempIG->igSize;
    }

    if (emitTotalCodeSize != 0 && emitTotalCodeSize != currentOffset)
    {
        printf("Total code size is %08X, expected %08X\n", emitTotalCodeSize, currentOffset);

        assert(!"bad total code size");
    }
}

#endif // DEBUG

/*****************************************************************************
 *
 *  Begin generating a method prolog.
 */

void emitter::emitBegProlog()
{
    assert(emitComp->compGeneratingProlog);

#if EMIT_TRACK_STACK_DEPTH

    /* Don't measure stack depth inside the prolog, it's misleading */

    emitCntStackDepth = 0;

    assert(emitCurStackLvl == 0);

#endif

    emitNoGCIG     = true;
    emitForceNewIG = false;

    /* Switch to the pre-allocated prolog IG */

    emitGenIG(emitPrologIG);

    /* Nothing is live on entry to the prolog */

    // These were initialized to Empty at the start of compilation.
    VarSetOps::ClearD(emitComp, emitInitGCrefVars);
    VarSetOps::ClearD(emitComp, emitPrevGCrefVars);
    emitInitGCrefRegs = RBM_NONE;
    emitPrevGCrefRegs = RBM_NONE;
    emitInitByrefRegs = RBM_NONE;
    emitPrevByrefRegs = RBM_NONE;
}

/*****************************************************************************
 *
 *  Return the code offset of the current location in the prolog.
 */

unsigned emitter::emitGetPrologOffsetEstimate()
{
    /* For now only allow a single prolog ins group */

    assert(emitPrologIG);
    assert(emitPrologIG == emitCurIG);

    return emitCurIGsize;
}

/*****************************************************************************
 *
 *  Mark the code offset of the current location as the end of the prolog,
 *  so it can be used later to compute the actual size of the prolog.
 */

void emitter::emitMarkPrologEnd()
{
    assert(emitComp->compGeneratingProlog);

    /* For now only allow a single prolog ins group */

    assert(emitPrologIG);
    assert(emitPrologIG == emitCurIG);

    emitPrologEndPos = emitCurOffset();
}

/*****************************************************************************
 *
 *  Finish generating a method prolog.
 */

void emitter::emitEndProlog()
{
    assert(emitComp->compGeneratingProlog);

    emitNoGCIG = false;

    /* Save the prolog IG if non-empty or if only one block */

    if (emitCurIGnonEmpty() || emitCurIG == emitPrologIG)
    {
        emitSavIG();
    }

#if EMIT_TRACK_STACK_DEPTH
    /* Reset the stack depth values */

    emitCurStackLvl   = 0;
    emitCntStackDepth = sizeof(int);
#endif
}

/*****************************************************************************
 *
 *  Create a placeholder instruction group to be used by a prolog or epilog,
 *  either for the main function, or a funclet.
 */

void emitter::emitCreatePlaceholderIG(insGroupPlaceholderType igType,
                                      BasicBlock*             igBB,
                                      VARSET_VALARG_TP        GCvars,
                                      regMaskTP               gcrefRegs,
                                      regMaskTP               byrefRegs,
                                      bool                    last)
{
    assert(igBB != nullptr);

    bool emitAdd = false;

    if (igType == IGPT_EPILOG
#if FEATURE_EH_FUNCLETS
        || igType == IGPT_FUNCLET_EPILOG
#endif // FEATURE_EH_FUNCLETS
        )
    {
#ifdef _TARGET_AMD64_
        emitOutputPreEpilogNOP();
#endif // _TARGET_AMD64_

        emitAdd = true;
    }

    if (emitCurIGnonEmpty())
    {
        emitNxtIG(emitAdd);
    }

    /* Update GC tracking for the beginning of the placeholder IG */

    if (!emitAdd)
    {
        VarSetOps::Assign(emitComp, emitThisGCrefVars, GCvars);
        VarSetOps::Assign(emitComp, emitInitGCrefVars, GCvars);
        emitThisGCrefRegs = emitInitGCrefRegs = gcrefRegs;
        emitThisByrefRegs = emitInitByrefRegs = byrefRegs;
    }

    /* Convert the group to a placeholder group */

    insGroup* igPh = emitCurIG;

    igPh->igFlags |= IGF_PLACEHOLDER;

    /* Note that we might be re-using a previously created but empty IG. In this
     * case, we need to make sure any re-used fields, such as igFuncIdx, are correct.
     */

    igPh->igFuncIdx = emitComp->compCurrFuncIdx;

    /* Create a separate block of memory to store placeholder information.
     * We could use unions to put some of this into the insGroup itself, but we don't
     * want to grow the insGroup, and it's difficult to make sure the
     * insGroup fields are getting set and used elsewhere.
     */

    igPh->igPhData = new (emitComp, CMK_InstDesc) insPlaceholderGroupData;

    igPh->igPhData->igPhNext = nullptr;
    igPh->igPhData->igPhType = igType;
    igPh->igPhData->igPhBB   = igBB;

    VarSetOps::AssignNoCopy(emitComp, igPh->igPhData->igPhPrevGCrefVars, VarSetOps::UninitVal());
    VarSetOps::Assign(emitComp, igPh->igPhData->igPhPrevGCrefVars, emitPrevGCrefVars);
    igPh->igPhData->igPhPrevGCrefRegs = emitPrevGCrefRegs;
    igPh->igPhData->igPhPrevByrefRegs = emitPrevByrefRegs;

    VarSetOps::AssignNoCopy(emitComp, igPh->igPhData->igPhInitGCrefVars, VarSetOps::UninitVal());
    VarSetOps::Assign(emitComp, igPh->igPhData->igPhInitGCrefVars, emitInitGCrefVars);
    igPh->igPhData->igPhInitGCrefRegs = emitInitGCrefRegs;
    igPh->igPhData->igPhInitByrefRegs = emitInitByrefRegs;

#if EMITTER_STATS
    emitTotalPhIGcnt += 1;
#endif

    // Mark function prologs and epilogs properly in the igFlags bits. These bits
    // will get used and propagated when the placeholder is converted to a non-placeholder
    // during prolog/epilog generation.

    if (igType == IGPT_EPILOG)
    {
        igPh->igFlags |= IGF_EPILOG;
    }
#if FEATURE_EH_FUNCLETS
    else if (igType == IGPT_FUNCLET_PROLOG)
    {
        igPh->igFlags |= IGF_FUNCLET_PROLOG;
    }
    else if (igType == IGPT_FUNCLET_EPILOG)
    {
        igPh->igFlags |= IGF_FUNCLET_EPILOG;
    }
#endif // FEATURE_EH_FUNCLETS

    /* Link it into the placeholder list */

    if (emitPlaceholderList)
    {
        emitPlaceholderLast->igPhData->igPhNext = igPh;
    }
    else
    {
        emitPlaceholderList = igPh;
    }

    emitPlaceholderLast = igPh;

    // Give an estimated size of this placeholder IG and
    // increment emitCurCodeOffset since we are not calling emitNewIG()
    //
    emitCurIGsize += MAX_PLACEHOLDER_IG_SIZE;
    emitCurCodeOffset += emitCurIGsize;

#if FEATURE_EH_FUNCLETS
    // Add the appropriate IP mapping debugging record for this placeholder
    // group. genExitCode() adds the mapping for main function epilogs.
    if (emitComp->opts.compDbgInfo)
    {
        if (igType == IGPT_FUNCLET_PROLOG)
        {
            codeGen->genIPmappingAdd((IL_OFFSETX)ICorDebugInfo::PROLOG, true);
        }
        else if (igType == IGPT_FUNCLET_EPILOG)
        {
            codeGen->genIPmappingAdd((IL_OFFSETX)ICorDebugInfo::EPILOG, true);
        }
    }
#endif // FEATURE_EH_FUNCLETS

    /* Start a new IG if more code follows */

    if (last)
    {
        emitCurIG = nullptr;
    }
    else
    {
        if (igType == IGPT_EPILOG
#if FEATURE_EH_FUNCLETS
            || igType == IGPT_FUNCLET_EPILOG
#endif // FEATURE_EH_FUNCLETS
            )
        {
            // If this was an epilog, then assume this is the end of any currently in progress
            // no-GC region. If a block after the epilog needs to be no-GC, it needs to call
            // emitter::emitDisableGC() directly. This behavior is depended upon by the fast
            // tailcall implementation, which disables GC at the beginning of argument setup,
            // but assumes that after the epilog it will be re-enabled.
            emitNoGCIG = false;
        }

        emitNewIG();

        // We don't know what the GC ref state will be at the end of the placeholder
        // group. So, force the next IG to store all the GC ref state variables;
        // don't omit them because emitPrev* is the same as emitInit*, because emitPrev*
        // will be inaccurate. (Note that, currently, GCrefRegs and ByrefRegs are always
        // saved anyway.)
        //
        // There is no need to re-initialize the emitPrev* variables, as they won't be used
        // with emitForceStoreGCState==true, and will be re-initialized just before
        // emitForceStoreGCState is set to false;

        emitForceStoreGCState = true;

        /* The group after the placeholder group doesn't get the "propagate" flags */

        emitCurIG->igFlags &= ~IGF_PROPAGATE_MASK;
    }

#ifdef DEBUG
    if (emitComp->verbose)
    {
        printf("*************** After placeholder IG creation\n");
        emitDispIGlist(false);
    }
#endif
}

/*****************************************************************************
 *
 *  Generate all prologs and epilogs
 */

void emitter::emitGeneratePrologEpilog()
{
#ifdef DEBUG
    unsigned prologCnt = 0;
    unsigned epilogCnt = 0;
#if FEATURE_EH_FUNCLETS
    unsigned funcletPrologCnt = 0;
    unsigned funcletEpilogCnt = 0;
#endif // FEATURE_EH_FUNCLETS
#endif // DEBUG

    insGroup* igPh;
    insGroup* igPhNext;

    // Generating the prolog/epilog is going to destroy the placeholder group,
    // so save the "next" pointer before that happens.

    for (igPh = emitPlaceholderList; igPh != nullptr; igPh = igPhNext)
    {
        assert(igPh->igFlags & IGF_PLACEHOLDER);

        igPhNext = igPh->igPhData->igPhNext;

        BasicBlock* igPhBB = igPh->igPhData->igPhBB;

        switch (igPh->igPhData->igPhType)
        {
            case IGPT_PROLOG: // currently unused
                INDEBUG(++prologCnt);
                break;

            case IGPT_EPILOG:
                INDEBUG(++epilogCnt);
                emitBegFnEpilog(igPh);
                codeGen->genFnEpilog(igPhBB);
                emitEndFnEpilog();
                break;

#if FEATURE_EH_FUNCLETS

            case IGPT_FUNCLET_PROLOG:
                INDEBUG(++funcletPrologCnt);
                emitBegFuncletProlog(igPh);
                codeGen->genFuncletProlog(igPhBB);
                emitEndFuncletProlog();
                break;

            case IGPT_FUNCLET_EPILOG:
                INDEBUG(++funcletEpilogCnt);
                emitBegFuncletEpilog(igPh);
                codeGen->genFuncletEpilog();
                emitEndFuncletEpilog();
                break;

#endif // FEATURE_EH_FUNCLETS

            default:
                unreached();
        }
    }

#ifdef DEBUG
    if (emitComp->verbose)
    {
        printf("%d prologs, %d epilogs", prologCnt, epilogCnt);
#if FEATURE_EH_FUNCLETS
        printf(", %d funclet prologs, %d funclet epilogs", funcletPrologCnt, funcletEpilogCnt);
#endif // FEATURE_EH_FUNCLETS
        printf("\n");

// prolog/epilog code doesn't use this yet
// noway_assert(prologCnt == 1);
// noway_assert(epilogCnt == emitEpilogCnt); // Is this correct?
#if FEATURE_EH_FUNCLETS
        assert(funcletPrologCnt == emitComp->ehFuncletCount());
#endif // FEATURE_EH_FUNCLETS
    }
#endif // DEBUG
}

/*****************************************************************************
 *
 *  Begin all prolog and epilog generation
 */

void emitter::emitStartPrologEpilogGeneration()
{
    /* Save the current IG if it's non-empty */

    if (emitCurIGnonEmpty())
    {
        emitSavIG();
    }
    else
    {
        assert(emitCurIG == nullptr);
    }
}

/*****************************************************************************
 *
 *  Finish all prolog and epilog generation
 */

void emitter::emitFinishPrologEpilogGeneration()
{
    /* Update the offsets of all the blocks */

    emitRecomputeIGoffsets();

    /* We should not generate any more code after this */

    emitCurIG = nullptr;
}

/*****************************************************************************
 *
 *  Common code for prolog / epilog beginning. Convert the placeholder group to actual code IG,
 *  and set it as the current group.
 */

void emitter::emitBegPrologEpilog(insGroup* igPh)
{
    assert(igPh->igFlags & IGF_PLACEHOLDER);

    /* Save the current IG if it's non-empty */

    if (emitCurIGnonEmpty())
    {
        emitSavIG();
    }

    /* Convert the placeholder group to a normal group.
     * We need to be very careful to re-initialize the IG properly.
     * It turns out, this means we only need to clear the placeholder bit
     * and clear the igPhData field, and emitGenIG() will do the rest,
     * since in the placeholder IG we didn't touch anything that is set by emitAllocIG().
     */

    igPh->igFlags &= ~IGF_PLACEHOLDER;
    emitNoGCIG     = true;
    emitForceNewIG = false;

    /* Set up the GC info that we stored in the placeholder */

    VarSetOps::Assign(emitComp, emitPrevGCrefVars, igPh->igPhData->igPhPrevGCrefVars);
    emitPrevGCrefRegs = igPh->igPhData->igPhPrevGCrefRegs;
    emitPrevByrefRegs = igPh->igPhData->igPhPrevByrefRegs;

    VarSetOps::Assign(emitComp, emitThisGCrefVars, igPh->igPhData->igPhInitGCrefVars);
    VarSetOps::Assign(emitComp, emitInitGCrefVars, igPh->igPhData->igPhInitGCrefVars);
    emitThisGCrefRegs = emitInitGCrefRegs = igPh->igPhData->igPhInitGCrefRegs;
    emitThisByrefRegs = emitInitByrefRegs = igPh->igPhData->igPhInitByrefRegs;

    igPh->igPhData = nullptr;

    /* Create a non-placeholder group pointer that we'll now use */

    insGroup* ig = igPh;

    /* Set the current function using the function index we stored */

    emitComp->funSetCurrentFunc(ig->igFuncIdx);

    /* Set the new IG as the place to generate code */

    emitGenIG(ig);

#if EMIT_TRACK_STACK_DEPTH

    /* Don't measure stack depth inside the prolog / epilog, it's misleading */

    emitCntStackDepth = 0;

    assert(emitCurStackLvl == 0);

#endif
}

/*****************************************************************************
 *
 *  Common code for end of prolog / epilog
 */

void emitter::emitEndPrologEpilog()
{
    emitNoGCIG = false;

    /* Save the IG if non-empty */

    if (emitCurIGnonEmpty())
    {
        emitSavIG();
    }

    assert(emitCurIGsize <= MAX_PLACEHOLDER_IG_SIZE);

#if EMIT_TRACK_STACK_DEPTH
    /* Reset the stack depth values */

    emitCurStackLvl   = 0;
    emitCntStackDepth = sizeof(int);
#endif
}

/*****************************************************************************
 *
 *  Begin generating a main function epilog.
 */

void emitter::emitBegFnEpilog(insGroup* igPh)
{
    emitEpilogCnt++;

    emitBegPrologEpilog(igPh);

#ifdef JIT32_GCENCODER

    EpilogList* el = new (emitComp, CMK_GC) EpilogList();

    if (emitEpilogLast != nullptr)
    {
        emitEpilogLast->elNext = el;
    }
    else
    {
        emitEpilogList = el;
    }

    emitEpilogLast = el;

#endif // JIT32_GCENCODER
}

/*****************************************************************************
 *
 *  Finish generating a funclet epilog.
 */

void emitter::emitEndFnEpilog()
{
    emitEndPrologEpilog();

#ifdef JIT32_GCENCODER
    assert(emitEpilogLast != nullptr);

    UNATIVE_OFFSET epilogBegCodeOffset          = emitEpilogLast->elLoc.CodeOffset(this);
    UNATIVE_OFFSET epilogExitSeqStartCodeOffset = emitExitSeqBegLoc.CodeOffset(this);
    UNATIVE_OFFSET newSize                      = epilogExitSeqStartCodeOffset - epilogBegCodeOffset;

    /* Compute total epilog size */
    assert(emitEpilogSize == 0 || emitEpilogSize == newSize); // All epilogs must be identical
    emitEpilogSize = newSize;

    UNATIVE_OFFSET epilogEndCodeOffset = emitCodeOffset(emitCurIG, emitCurOffset());
    assert(epilogExitSeqStartCodeOffset != epilogEndCodeOffset);

    newSize = epilogEndCodeOffset - epilogExitSeqStartCodeOffset;
    if (newSize < emitExitSeqSize)
    {
        // We expect either the epilog to be the same every time, or that
        // one will be a ret or a ret <n> and others will be a jmp addr or jmp [addr];
        // we make the epilogs the minimum of these.  Note that this ONLY works
        // because the only instruction is the last one and thus a slight
        // underestimation of the epilog size is harmless (since the EIP
        // can not be between instructions).
        assert(emitEpilogCnt == 1 ||
               (emitExitSeqSize - newSize) <= 5 // delta between size of various forms of jmp (size is either 6 or 5)
                                                // and various forms of ret (size is either 1 or 3). The combination can
                                                // be anything been 1 and 5.
               );
        emitExitSeqSize = newSize;
    }
#endif // JIT32_GCENCODER
}

#if FEATURE_EH_FUNCLETS

/*****************************************************************************
 *
 *  Begin generating a funclet prolog.
 */

void emitter::emitBegFuncletProlog(insGroup* igPh)
{
    emitBegPrologEpilog(igPh);
}

/*****************************************************************************
 *
 *  Finish generating a funclet prolog.
 */

void emitter::emitEndFuncletProlog()
{
    emitEndPrologEpilog();
}

/*****************************************************************************
 *
 *  Begin generating a funclet epilog.
 */

void emitter::emitBegFuncletEpilog(insGroup* igPh)
{
    emitBegPrologEpilog(igPh);
}

/*****************************************************************************
 *
 *  Finish generating a funclet epilog.
 */

void emitter::emitEndFuncletEpilog()
{
    emitEndPrologEpilog();
}

#endif // FEATURE_EH_FUNCLETS

#ifdef JIT32_GCENCODER

//
// emitter::emitStartEpilog:
//   Mark the current position so that we can later compute the total epilog size.
//
void emitter::emitStartEpilog()
{
    assert(emitEpilogLast != nullptr);
    emitEpilogLast->elLoc.CaptureLocation(this);
}

/*****************************************************************************
 *
 *  Return non-zero if the current method only has one epilog, which is
 *  at the very end of the method body.
 */

bool emitter::emitHasEpilogEnd()
{
    if (emitEpilogCnt == 1 && (emitIGlast->igFlags & IGF_EPILOG)) // This wouldn't work for funclets
        return true;
    else
        return false;
}

#endif // JIT32_GCENCODER

#ifdef _TARGET_XARCH_

/*****************************************************************************
 *
 *  Mark the beginning of the epilog exit sequence by remembering our position.
 */

void emitter::emitStartExitSeq()
{
    assert(emitComp->compGeneratingEpilog);

    emitExitSeqBegLoc.CaptureLocation(this);
}

#endif // _TARGET_XARCH_

/*****************************************************************************
 *
 *  The code generator tells us the range of GC ref locals through this
 *  method. Needless to say, locals and temps should be allocated so that
 *  the size of the range is as small as possible.
 *
 * offsLo - The FP offset from which the GC pointer range starts.
 * offsHi - The FP offset at which the GC pointer region ends (exclusive).
 */

void emitter::emitSetFrameRangeGCRs(int offsLo, int offsHi)
{
    assert(emitComp->compGeneratingProlog);
    assert(offsHi > offsLo);

#ifdef DEBUG

    //  A total of    47254 methods compiled.
    //
    //  GC ref frame variable counts:
    //
    //      <=         0 ===>  43175 count ( 91% of total)
    //       1 ..      1 ===>   2367 count ( 96% of total)
    //       2 ..      2 ===>    887 count ( 98% of total)
    //       3 ..      5 ===>    579 count ( 99% of total)
    //       6 ..     10 ===>    141 count ( 99% of total)
    //      11 ..     20 ===>     40 count ( 99% of total)
    //      21 ..     50 ===>     42 count ( 99% of total)
    //      51 ..    128 ===>     15 count ( 99% of total)
    //     129 ..    256 ===>      4 count ( 99% of total)
    //     257 ..    512 ===>      4 count (100% of total)
    //     513 ..   1024 ===>      0 count (100% of total)

    if (emitComp->verbose)
    {
        unsigned count = (offsHi - offsLo) / TARGET_POINTER_SIZE;
        printf("%u tracked GC refs are at stack offsets ", count);

        if (offsLo >= 0)
        {
            printf(" %04X ...  %04X\n", offsLo, offsHi);
            assert(offsHi >= 0);
        }
        else
#if defined(_TARGET_ARM_) && defined(PROFILING_SUPPORTED)
            if (!emitComp->compIsProfilerHookNeeded())
#endif
        {
#ifdef _TARGET_AMD64_
            // doesn't have to be all negative on amd
            printf("-%04X ... %04X\n", -offsLo, offsHi);
#else
            printf("-%04X ... -%04X\n", -offsLo, -offsHi);
            assert(offsHi <= 0);
#endif
        }
#if defined(_TARGET_ARM_) && defined(PROFILING_SUPPORTED)
        else
        {
            // Under profiler due to prespilling of arguments, offHi need not be < 0
            if (offsHi < 0)
                printf("-%04X ... -%04X\n", -offsLo, -offsHi);
            else
                printf("-%04X ... %04X\n", -offsLo, offsHi);
        }
#endif
    }

#endif // DEBUG

    assert(((offsHi - offsLo) % TARGET_POINTER_SIZE) == 0);
    assert((offsLo % TARGET_POINTER_SIZE) == 0);
    assert((offsHi % TARGET_POINTER_SIZE) == 0);

    emitGCrFrameOffsMin = offsLo;
    emitGCrFrameOffsMax = offsHi;
    emitGCrFrameOffsCnt = (offsHi - offsLo) / TARGET_POINTER_SIZE;
}

/*****************************************************************************
 *
 *  The code generator tells us the range of local variables through this
 *  method.
 */

void emitter::emitSetFrameRangeLcls(int offsLo, int offsHi)
{
}

/*****************************************************************************
 *
 *  The code generator tells us the range of used arguments through this
 *  method.
 */

void emitter::emitSetFrameRangeArgs(int offsLo, int offsHi)
{
}

/*****************************************************************************
 *
 *  A conversion table used to map an operand size value (in bytes) into its
 *  small encoding (0 through 3), and vice versa.
 */

const emitter::opSize emitter::emitSizeEncode[] = {
    emitter::OPSZ1, emitter::OPSZ2,  OPSIZE_INVALID, emitter::OPSZ4,  OPSIZE_INVALID, OPSIZE_INVALID, OPSIZE_INVALID,
    emitter::OPSZ8, OPSIZE_INVALID,  OPSIZE_INVALID, OPSIZE_INVALID,  OPSIZE_INVALID, OPSIZE_INVALID, OPSIZE_INVALID,
    OPSIZE_INVALID, emitter::OPSZ16, OPSIZE_INVALID, OPSIZE_INVALID,  OPSIZE_INVALID, OPSIZE_INVALID, OPSIZE_INVALID,
    OPSIZE_INVALID, OPSIZE_INVALID,  OPSIZE_INVALID, OPSIZE_INVALID,  OPSIZE_INVALID, OPSIZE_INVALID, OPSIZE_INVALID,
    OPSIZE_INVALID, OPSIZE_INVALID,  OPSIZE_INVALID, emitter::OPSZ32,
};

const emitAttr emitter::emitSizeDecode[emitter::OPSZ_COUNT] = {EA_1BYTE, EA_2BYTE,  EA_4BYTE,
                                                               EA_8BYTE, EA_16BYTE, EA_32BYTE};

/*****************************************************************************
 *
 *  Allocate an instruction descriptor for an instruction that uses both
 *  a displacement and a constant.
 */

emitter::instrDesc* emitter::emitNewInstrCnsDsp(emitAttr size, target_ssize_t cns, int dsp)
{
    if (dsp == 0)
    {
        if (instrDesc::fitsInSmallCns(cns))
        {
            instrDesc* id = emitAllocInstr(size);

            id->idSmallCns(cns);

#if EMITTER_STATS
            emitSmallCnsCnt++;
            if ((cns - ID_MIN_SMALL_CNS) >= (SMALL_CNS_TSZ - 1))
                emitSmallCns[SMALL_CNS_TSZ - 1]++;
            else
                emitSmallCns[cns - ID_MIN_SMALL_CNS]++;
            emitSmallDspCnt++;
#endif

            return id;
        }
        else
        {
            instrDescCns* id = emitAllocInstrCns(size, cns);

#if EMITTER_STATS
            emitLargeCnsCnt++;
            emitSmallDspCnt++;
#endif

            return id;
        }
    }
    else
    {
        if (instrDesc::fitsInSmallCns(cns))
        {
            instrDescDsp* id = emitAllocInstrDsp(size);

            id->idSetIsLargeDsp();
            id->iddDspVal = dsp;

            id->idSmallCns(cns);

#if EMITTER_STATS
            emitLargeDspCnt++;
            emitSmallCnsCnt++;
            if ((cns - ID_MIN_SMALL_CNS) >= (SMALL_CNS_TSZ - 1))
                emitSmallCns[SMALL_CNS_TSZ - 1]++;
            else
                emitSmallCns[cns - ID_MIN_SMALL_CNS]++;
#endif

            return id;
        }
        else
        {
            instrDescCnsDsp* id = emitAllocInstrCnsDsp(size);

            id->idSetIsLargeCns();
            id->iddcCnsVal = cns;

            id->idSetIsLargeDsp();
            id->iddcDspVal = dsp;

#if EMITTER_STATS
            emitLargeDspCnt++;
            emitLargeCnsCnt++;
#endif

            return id;
        }
    }
}

//------------------------------------------------------------------------
// emitNoGChelper: Returns true if garbage collection won't happen within the helper call.
//
// Notes:
//  There is no need to record live pointers for such call sites.
//
// Arguments:
//   helpFunc - a helper signature for the call, can be CORINFO_HELP_UNDEF, that means that the call is not a helper.
//
// Return value:
//   true if GC can't happen within this call, false otherwise.
bool emitter::emitNoGChelper(CorInfoHelpFunc helpFunc)
{
    // TODO-Throughput: Make this faster (maybe via a simple table of bools?)

    switch (helpFunc)
    {
        case CORINFO_HELP_UNDEF:
            return false;

        case CORINFO_HELP_PROF_FCN_LEAVE:
        case CORINFO_HELP_PROF_FCN_ENTER:
#if defined(_TARGET_XARCH_)
        case CORINFO_HELP_PROF_FCN_TAILCALL:
#endif
        case CORINFO_HELP_LLSH:
        case CORINFO_HELP_LRSH:
        case CORINFO_HELP_LRSZ:

//  case CORINFO_HELP_LMUL:
//  case CORINFO_HELP_LDIV:
//  case CORINFO_HELP_LMOD:
//  case CORINFO_HELP_ULDIV:
//  case CORINFO_HELP_ULMOD:

#ifdef _TARGET_X86_
        case CORINFO_HELP_ASSIGN_REF_EAX:
        case CORINFO_HELP_ASSIGN_REF_ECX:
        case CORINFO_HELP_ASSIGN_REF_EBX:
        case CORINFO_HELP_ASSIGN_REF_EBP:
        case CORINFO_HELP_ASSIGN_REF_ESI:
        case CORINFO_HELP_ASSIGN_REF_EDI:

        case CORINFO_HELP_CHECKED_ASSIGN_REF_EAX:
        case CORINFO_HELP_CHECKED_ASSIGN_REF_ECX:
        case CORINFO_HELP_CHECKED_ASSIGN_REF_EBX:
        case CORINFO_HELP_CHECKED_ASSIGN_REF_EBP:
        case CORINFO_HELP_CHECKED_ASSIGN_REF_ESI:
        case CORINFO_HELP_CHECKED_ASSIGN_REF_EDI:
#endif

        case CORINFO_HELP_ASSIGN_REF:
        case CORINFO_HELP_CHECKED_ASSIGN_REF:
        case CORINFO_HELP_ASSIGN_BYREF:

        case CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR:
        case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR:

        case CORINFO_HELP_INIT_PINVOKE_FRAME:
            return true;

        default:
            return false;
    }
}

//------------------------------------------------------------------------
// emitNoGChelper: Returns true if garbage collection won't happen within the helper call.
//
// Notes:
//  There is no need to record live pointers for such call sites.
//
// Arguments:
//   methHnd - a method handle for the call.
//
// Return value:
//   true if GC can't happen within this call, false otherwise.
bool emitter::emitNoGChelper(CORINFO_METHOD_HANDLE methHnd)
{
    CorInfoHelpFunc helpFunc = Compiler::eeGetHelperNum(methHnd);
    if (helpFunc == CORINFO_HELP_UNDEF)
    {
        return false;
    }
    return emitNoGChelper(helpFunc);
}

/*****************************************************************************
 *
 *  Mark the current spot as having a label.
 */

void* emitter::emitAddLabel(VARSET_VALARG_TP GCvars, regMaskTP gcrefRegs, regMaskTP byrefRegs, BOOL isFinallyTarget)
{
    /* Create a new IG if the current one is non-empty */

    if (emitCurIGnonEmpty())
    {
        emitNxtIG();
    }

    VarSetOps::Assign(emitComp, emitThisGCrefVars, GCvars);
    VarSetOps::Assign(emitComp, emitInitGCrefVars, GCvars);
    emitThisGCrefRegs = emitInitGCrefRegs = gcrefRegs;
    emitThisByrefRegs = emitInitByrefRegs = byrefRegs;

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    if (isFinallyTarget)
    {
        emitCurIG->igFlags |= IGF_FINALLY_TARGET;
    }
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)

#ifdef DEBUG
    if (EMIT_GC_VERBOSE)
    {
        printf("Label: IG%02u, GCvars=%s ", emitCurIG->igNum, VarSetOps::ToString(emitComp, GCvars));
        dumpConvertedVarSet(emitComp, GCvars);
        printf(", gcrefRegs=");
        printRegMaskInt(gcrefRegs);
        emitDispRegSet(gcrefRegs);
        printf(", byrefRegs=");
        printRegMaskInt(byrefRegs);
        emitDispRegSet(byrefRegs);
        printf("\n");
    }
#endif
    return emitCurIG;
}

#ifdef _TARGET_ARMARCH_

// Does the argument location point to an IG at the end of a function or funclet?
// We can ignore the codePos part of the location, since it doesn't affect the
// determination. If 'emitLocNextFragment' is non-NULL, it indicates the first
// IG of the next fragment, so it represents a function end.
bool emitter::emitIsFuncEnd(emitLocation* emitLoc, emitLocation* emitLocNextFragment /* = NULL */)
{
    assert(emitLoc);

    insGroup* ig = emitLoc->GetIG();
    assert(ig);

    // Are we at the end of the IG list?
    if ((emitLocNextFragment != NULL) && (ig->igNext == emitLocNextFragment->GetIG()))
        return true;

    // Safety check
    if (ig->igNext == NULL)
        return true;

    // Is the next IG the start of a funclet prolog?
    if (ig->igNext->igFlags & IGF_FUNCLET_PROLOG)
        return true;

#if FEATURE_EH_FUNCLETS

    // Is the next IG a placeholder group for a funclet prolog?
    if ((ig->igNext->igFlags & IGF_PLACEHOLDER) && (ig->igNext->igPhData->igPhType == IGPT_FUNCLET_PROLOG))
    {
        return true;
    }

#endif // FEATURE_EH_FUNCLETS

    return false;
}

/*****************************************************************************
 *
 * Split the region from 'startLoc' to 'endLoc' into fragments by calling
 * a callback function to indicate the beginning of a fragment. The initial code,
 * starting at 'startLoc', doesn't get a callback, but the first code fragment,
 * about 'maxSplitSize' bytes out does, as does the beginning of each fragment
 * after that. There is no callback for the end (only the beginning of the last
 * fragment gets a callback). A fragment must contain at least one instruction
 * group. It should be smaller than 'maxSplitSize', although it may be larger to
 * satisfy the "at least one instruction group" rule. Do not split prologs or
 * epilogs. (Currently, prologs exist in a single instruction group at the main
 * function beginning, so they aren't split. Funclets, however, might span IGs,
 * so we can't split in between them.)
 *
 * Note that the locations must be the start of instruction groups; the part of
 * the location indicating offset within a group must be zero.
 *
 * If 'startLoc' is NULL, it means the start of the code.
 * If 'endLoc'   is NULL, it means the end   of the code.
 */

void emitter::emitSplit(emitLocation*         startLoc,
                        emitLocation*         endLoc,
                        UNATIVE_OFFSET        maxSplitSize,
                        void*                 context,
                        emitSplitCallbackType callbackFunc)
{
    insGroup*      igStart = (startLoc == NULL) ? emitIGlist : startLoc->GetIG();
    insGroup*      igEnd   = (endLoc == NULL) ? NULL : endLoc->GetIG();
    insGroup*      igPrev;
    insGroup*      ig;
    insGroup*      igLastReported;
    insGroup*      igLastCandidate;
    UNATIVE_OFFSET curSize;
    UNATIVE_OFFSET candidateSize;

    for (igPrev = NULL, ig = igLastReported = igStart, igLastCandidate = NULL, candidateSize = 0, curSize = 0;
         ig != igEnd && ig != NULL; igPrev = ig, ig = ig->igNext)
    {
        // Keep looking until we've gone past the maximum split size
        if (curSize >= maxSplitSize)
        {
            bool reportCandidate = true;

            // Is there a candidate?
            if (igLastCandidate == NULL)
            {
#ifdef DEBUG
                if (EMITVERBOSE)
                    printf("emitSplit: can't split at IG%02u; we don't have a candidate to report\n", ig->igNum);
#endif
                reportCandidate = false;
            }

            // Don't report the same thing twice (this also happens for the first block, since igLastReported is
            // initialized to igStart).
            if (igLastCandidate == igLastReported)
            {
#ifdef DEBUG
                if (EMITVERBOSE)
                    printf("emitSplit: can't split at IG%02u; we already reported it\n", igLastCandidate->igNum);
#endif
                reportCandidate = false;
            }

            // Report it!
            if (reportCandidate)
            {
#ifdef DEBUG
                if (EMITVERBOSE && (candidateSize >= maxSplitSize))
                    printf("emitSplit: split at IG%02u is size %d, larger than requested maximum size of %d\n",
                           igLastCandidate->igNum, candidateSize, maxSplitSize);
#endif

                // hand memory ownership to the callback function
                emitLocation* pEmitLoc = new (emitComp, CMK_Unknown) emitLocation(igLastCandidate);
                callbackFunc(context, pEmitLoc);
                igLastReported  = igLastCandidate;
                igLastCandidate = NULL;
                curSize -= candidateSize;
            }
        }

        // Update the current candidate to be this block, if it isn't in the middle of a
        // prolog or epilog, which we can't split. All we know is that certain
        // IGs are marked as prolog or epilog. We don't actually know if two adjacent
        // IGs are part of the *same* prolog or epilog, so we have to assume they are.

        if (igPrev && (((igPrev->igFlags & IGF_FUNCLET_PROLOG) && (ig->igFlags & IGF_FUNCLET_PROLOG)) ||
                       ((igPrev->igFlags & IGF_EPILOG) && (ig->igFlags & IGF_EPILOG))))
        {
            // We can't update the candidate
        }
        else
        {
            igLastCandidate = ig;
            candidateSize   = curSize;
        }

        curSize += ig->igSize;

    } // end for loop
}

/*****************************************************************************
 *
 * Given an instruction group, find the array of instructions (instrDesc) and
 * number of instructions in the array. If the IG is the current IG, we assume
 * that igData does NOT hold the instructions; they are unsaved and pointed
 * to by emitCurIGfreeBase.
 *
 * This function can't be called for placeholder groups, which have no instrDescs.
 */

void emitter::emitGetInstrDescs(insGroup* ig, instrDesc** id, int* insCnt)
{
    assert(!(ig->igFlags & IGF_PLACEHOLDER));
    if (ig == emitCurIG)
    {
        *id     = (instrDesc*)emitCurIGfreeBase;
        *insCnt = emitCurIGinsCnt;
    }
    else
    {
        *id     = (instrDesc*)ig->igData;
        *insCnt = ig->igInsCnt;
    }

    assert(*id);
}

/*****************************************************************************
 *
 * Given a location (an 'emitLocation'), find the instruction group (IG) and
 * instruction descriptor (instrDesc) corresponding to that location. Returns
 * 'true' if there is an instruction, 'false' if there is no instruction
 * (i.e., we're at the end of the instruction list). Also, optionally return
 * the number of instructions that follow that instruction in the IG (in *pinsRemaining,
 * if pinsRemaining is non-NULL), which can be used for iterating over the
 * remaining instrDescs in the IG.
 *
 * We assume that emitCurIG points to the end of the instructions we care about.
 * For the prologs or epilogs, it points to the last IG of the prolog or epilog
 * that is being generated. For body code gen, it points to the place we are currently
 * adding code, namely, the end of currently generated code.
 */

bool emitter::emitGetLocationInfo(emitLocation* emitLoc,
                                  insGroup**    pig,
                                  instrDesc**   pid,
                                  int*          pinsRemaining /* = NULL */)
{
    assert(emitLoc != nullptr);
    assert(emitLoc->Valid());
    assert(emitLoc->GetIG() != nullptr);
    assert(pig != nullptr);
    assert(pid != nullptr);

    insGroup*  ig = emitLoc->GetIG();
    instrDesc* id;
    int        insNum = emitLoc->GetInsNum();
    int        insCnt;

    emitGetInstrDescs(ig, &id, &insCnt);
    assert(insNum <= insCnt);

    // There is a special-case: if the insNum points to the end, then we "wrap" and
    // consider that the instruction it is pointing at is actually the first instruction
    // of the next non-empty IG (which has its own valid emitLocation). This handles the
    // case where you capture a location, then the next instruction creates a new IG.

    if (insNum == insCnt)
    {
        if (ig == emitCurIG)
        {
            // No instructions beyond the current location.
            return false;
        }

        for (ig = ig->igNext; ig; ig = ig->igNext)
        {
            emitGetInstrDescs(ig, &id, &insCnt);

            if (insCnt > 0)
            {
                insNum = 0; // Pretend the index is 0 -- the first instruction
                break;
            }

            if (ig == emitCurIG)
            {
                // There aren't any instructions in the current IG, and this is
                // the current location, so we're at the end.
                return false;
            }
        }

        if (ig == NULL)
        {
            // 'ig' can't be NULL, or we went past the current IG represented by 'emitCurIG'.
            // Perhaps 'loc' was corrupt coming in?
            noway_assert(!"corrupt emitter location");
            return false;
        }
    }

    // Now find the instrDesc within this group that corresponds to the location

    assert(insNum < insCnt);

    int i;
    for (i = 0; i != insNum; ++i)
    {
        castto(id, BYTE*) += emitSizeOfInsDsc(id);
    }

    // Return the info we found

    *pig = ig;
    *pid = id;

    if (pinsRemaining)
    {
        *pinsRemaining = insCnt - insNum - 1;
    }

    return true;
}

/*****************************************************************************
 *
 * Compute the next instrDesc, either in this IG, or in a subsequent IG. 'id'
 * will point to this instrDesc. 'ig' and 'insRemaining' will also be updated.
 * Returns true if there is an instruction, or false if we've iterated over all
 * the instructions up to the current instruction (based on 'emitCurIG').
 */

bool emitter::emitNextID(insGroup*& ig, instrDesc*& id, int& insRemaining)
{
    if (insRemaining > 0)
    {
        castto(id, BYTE*) += emitSizeOfInsDsc(id);
        --insRemaining;
        return true;
    }

    // We're out of instrDesc in 'ig'. Is this the current IG? If so, we're done.

    if (ig == emitCurIG)
    {
        return false;
    }

    for (ig = ig->igNext; ig; ig = ig->igNext)
    {
        int insCnt;
        emitGetInstrDescs(ig, &id, &insCnt);

        if (insCnt > 0)
        {
            insRemaining = insCnt - 1;
            return true;
        }

        if (ig == emitCurIG)
        {
            return false;
        }
    }

    return false;
}

/*****************************************************************************
 *
 * Walk instrDesc's from the location given by 'locFrom', up to the current location.
 * For each instruction, call the callback function 'processFunc'. 'context' is simply
 * passed through to the callback function.
 */

void emitter::emitWalkIDs(emitLocation* locFrom, emitProcessInstrFunc_t processFunc, void* context)
{
    insGroup*  ig;
    instrDesc* id;
    int        insRemaining;

    if (!emitGetLocationInfo(locFrom, &ig, &id, &insRemaining))
        return; // no instructions at the 'from' location

    do
    {
        // process <<id>>
        (*processFunc)(id, context);

    } while (emitNextID(ig, id, insRemaining));
}

/*****************************************************************************
 *
 * A callback function for emitWalkIDs() that calls Compiler::unwindNop().
 */

void emitter::emitGenerateUnwindNop(instrDesc* id, void* context)
{
    Compiler* comp = (Compiler*)context;
#if defined(_TARGET_ARM_)
    comp->unwindNop(id->idCodeSize());
#elif defined(_TARGET_ARM64_)
    comp->unwindNop();
#endif // defined(_TARGET_ARM64_)
}

/*****************************************************************************
 *
 * emitUnwindNopPadding: call unwindNop() for every instruction from a given
 * location 'emitLoc' up to the current location.
 */

void emitter::emitUnwindNopPadding(emitLocation* locFrom, Compiler* comp)
{
    emitWalkIDs(locFrom, emitGenerateUnwindNop, comp);
}

#endif // _TARGET_ARMARCH_

#if defined(_TARGET_ARM_)

/*****************************************************************************
 *
 * Return the instruction size in bytes for the instruction at the specified location.
 * This is used to assert that the unwind code being generated on ARM has the
 * same size as the instruction for which it is being generated (since on ARM
 * the unwind codes have a one-to-one relationship with instructions, and the
 * unwind codes have an implicit instruction size that must match the instruction size.)
 * An instruction must exist at the specified location.
 */

unsigned emitter::emitGetInstructionSize(emitLocation* emitLoc)
{
    insGroup*  ig;
    instrDesc* id;

    bool anyInstrs = emitGetLocationInfo(emitLoc, &ig, &id);
    assert(anyInstrs); // There better be an instruction at this location (otherwise, we're at the end of the
                       // instruction list)
    return id->idCodeSize();
}

#endif // defined(_TARGET_ARM_)

/*****************************************************************************/
#ifdef DEBUG
/*****************************************************************************
 *
 *  Returns the name for the register to use to access frame based variables
 */

const char* emitter::emitGetFrameReg()
{
    if (emitHasFramePtr)
    {
        return STR_FPBASE;
    }
    else
    {
        return STR_SPBASE;
    }
}

/*****************************************************************************
 *
 *  Display a register set in a readable form.
 */

void emitter::emitDispRegSet(regMaskTP regs)
{
    regNumber reg;
    bool      sp = false;

    printf(" {");

    for (reg = REG_FIRST; reg < ACTUAL_REG_COUNT; reg = REG_NEXT(reg))
    {
        if ((regs & genRegMask(reg)) == 0)
        {
            continue;
        }

        if (sp)
        {
            printf(" ");
        }
        else
        {
            sp = true;
        }

        printf("%s", emitRegName(reg));
    }

    printf("}");
}

/*****************************************************************************
 *
 *  Display the current GC ref variable set in a readable form.
 */

void emitter::emitDispVarSet()
{
    unsigned vn;
    int      of;
    bool     sp = false;

    for (vn = 0, of = emitGCrFrameOffsMin; vn < emitGCrFrameOffsCnt; vn += 1, of += TARGET_POINTER_SIZE)
    {
        if (emitGCrFrameLiveTab[vn])
        {
            if (sp)
            {
                printf(" ");
            }
            else
            {
                sp = true;
            }

            printf("[%s", emitGetFrameReg());

            if (of < 0)
            {
                printf("-%02XH", -of);
            }
            else if (of > 0)
            {
                printf("+%02XH", +of);
            }

            printf("]");
        }
    }

    if (!sp)
    {
        printf("none");
    }
}

/*****************************************************************************/
#endif // DEBUG

#if MULTIREG_HAS_SECOND_GC_RET
//------------------------------------------------------------------------
// emitSetSecondRetRegGCType: Sets the GC type of the second return register for instrDescCGCA struct.
//
// Arguments:
//    id            - The large call instr descriptor to set the second GC return register type on.
//    secondRetSize - The EA_SIZE for second return register type.
//
// Return Value:
//    None
//

void emitter::emitSetSecondRetRegGCType(instrDescCGCA* id, emitAttr secondRetSize)
{
    if (EA_IS_GCREF(secondRetSize))
    {
        id->idSecondGCref(GCT_GCREF);
    }
    else if (EA_IS_BYREF(secondRetSize))
    {
        id->idSecondGCref(GCT_BYREF);
    }
    else
    {
        id->idSecondGCref(GCT_NONE);
    }
}
#endif // MULTIREG_HAS_SECOND_GC_RET

/*****************************************************************************
 *
 *  Allocate an instruction descriptor for an indirect call.
 *
 *  We use two different descriptors to save space - the common case records
 *  no GC variables and has both a very small argument count and an address
 *  mode displacement; the other case records the current GC var set,
 *  the call scope, and an arbitrarily large argument count and the
 *  address mode displacement.
 */

emitter::instrDesc* emitter::emitNewInstrCallInd(int              argCnt,
                                                 ssize_t          disp,
                                                 VARSET_VALARG_TP GCvars,
                                                 regMaskTP        gcrefRegs,
                                                 regMaskTP        byrefRegs,
                                                 emitAttr         retSizeIn
                                                     MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize))
{
    emitAttr retSize = (retSizeIn != EA_UNKNOWN) ? retSizeIn : EA_PTRSIZE;

    bool gcRefRegsInScratch = ((gcrefRegs & RBM_CALLEE_TRASH) != 0);

    // Allocate a larger descriptor if any GC values need to be saved
    // or if we have an absurd number of arguments or a large address
    // mode displacement, or we have some byref registers
    //
    // On Amd64 System V OSs a larger descriptor is also needed if the
    // call returns a two-register-returned struct and the second
    // register (RDX) is a GCRef or ByRef pointer.

    if (!VarSetOps::IsEmpty(emitComp, GCvars) || // any frame GCvars live
        (gcRefRegsInScratch) ||                  // any register gc refs live in scratch regs
        (byrefRegs != 0) ||                      // any register byrefs live
        (disp < AM_DISP_MIN) ||                  // displacement too negative
        (disp > AM_DISP_MAX) ||                  // displacement too positive
        (argCnt > ID_MAX_SMALL_CNS) ||           // too many args
        (argCnt < 0)                             // caller pops arguments
                                                 // There is a second ref/byref return register.
        MULTIREG_HAS_SECOND_GC_RET_ONLY(|| EA_IS_GCREF_OR_BYREF(secondRetSize)))
    {
        instrDescCGCA* id;

        id = emitAllocInstrCGCA(retSize);

        id->idSetIsLargeCall();

        VarSetOps::Assign(emitComp, id->idcGCvars, GCvars);
        id->idcGcrefRegs = gcrefRegs;
        id->idcByrefRegs = byrefRegs;
        id->idcArgCnt    = argCnt;
        id->idcDisp      = disp;

#if MULTIREG_HAS_SECOND_GC_RET
        emitSetSecondRetRegGCType(id, secondRetSize);
#endif // MULTIREG_HAS_SECOND_GC_RET

        return id;
    }
    else
    {
        instrDesc* id;

        id = emitNewInstrCns(retSize, argCnt);

        /* Make sure we didn't waste space unexpectedly */
        assert(!id->idIsLargeCns());

        /* Store the displacement and make sure the value fit */
        id->idAddr()->iiaAddrMode.amDisp = disp;
        assert(id->idAddr()->iiaAddrMode.amDisp == disp);

        /* Save the the live GC registers in the unused register fields */
        emitEncodeCallGCregs(gcrefRegs, id);

        return id;
    }
}

/*****************************************************************************
 *
 *  Allocate an instruction descriptor for a direct call.
 *
 *  We use two different descriptors to save space - the common case records
 *  with no GC variables or byrefs and has a very small argument count, and no
 *  explicit scope;
 *  the other case records the current GC var set, the call scope,
 *  and an arbitrarily large argument count.
 */

emitter::instrDesc* emitter::emitNewInstrCallDir(int              argCnt,
                                                 VARSET_VALARG_TP GCvars,
                                                 regMaskTP        gcrefRegs,
                                                 regMaskTP        byrefRegs,
                                                 emitAttr         retSizeIn
                                                     MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize))
{
    emitAttr retSize = (retSizeIn != EA_UNKNOWN) ? retSizeIn : EA_PTRSIZE;

    // Allocate a larger descriptor if new GC values need to be saved
    // or if we have an absurd number of arguments or if we need to
    // save the scope.
    //
    // On Amd64 System V OSs a larger descriptor is also needed if the
    // call returns a two-register-returned struct and the second
    // register (RDX) is a GCRef or ByRef pointer.

    bool gcRefRegsInScratch = ((gcrefRegs & RBM_CALLEE_TRASH) != 0);

    if (!VarSetOps::IsEmpty(emitComp, GCvars) || // any frame GCvars live
        gcRefRegsInScratch ||                    // any register gc refs live in scratch regs
        (byrefRegs != 0) ||                      // any register byrefs live
        (argCnt > ID_MAX_SMALL_CNS) ||           // too many args
        (argCnt < 0)                             // caller pops arguments
                                                 // There is a second ref/byref return register.
        MULTIREG_HAS_SECOND_GC_RET_ONLY(|| EA_IS_GCREF_OR_BYREF(secondRetSize)))
    {
        instrDescCGCA* id = emitAllocInstrCGCA(retSize);

        // printf("Direct call with GC vars / big arg cnt / explicit scope\n");

        id->idSetIsLargeCall();

        VarSetOps::Assign(emitComp, id->idcGCvars, GCvars);
        id->idcGcrefRegs = gcrefRegs;
        id->idcByrefRegs = byrefRegs;
        id->idcDisp      = 0;
        id->idcArgCnt    = argCnt;

#if MULTIREG_HAS_SECOND_GC_RET
        emitSetSecondRetRegGCType(id, secondRetSize);
#endif // MULTIREG_HAS_SECOND_GC_RET

        return id;
    }
    else
    {
        instrDesc* id = emitNewInstrCns(retSize, argCnt);

        // printf("Direct call w/o  GC vars / big arg cnt / explicit scope\n");

        /* Make sure we didn't waste space unexpectedly */
        assert(!id->idIsLargeCns());

        /* Save the the live GC registers in the unused register fields */
        emitEncodeCallGCregs(gcrefRegs, id);

        return id;
    }
}

/*****************************************************************************/
#ifdef DEBUG
/*****************************************************************************
 *
 *  Return a string with the name of the given class field (blank string (not
 *  NULL) is returned when the name isn't available).
 */

const char* emitter::emitFldName(CORINFO_FIELD_HANDLE fieldVal)
{
    if (emitComp->opts.varNames)
    {
        const char* memberName;
        const char* className;

        const int   TEMP_BUFFER_LEN = 1024;
        static char buff[TEMP_BUFFER_LEN];

        memberName = emitComp->eeGetFieldName(fieldVal, &className);

        sprintf_s(buff, TEMP_BUFFER_LEN, "'<%s>.%s'", className, memberName);
        return buff;
    }
    else
    {
        return "";
    }
}

/*****************************************************************************
 *
 *  Return a string with the name of the given function (blank string (not
 *  NULL) is returned when the name isn't available).
 */

const char* emitter::emitFncName(CORINFO_METHOD_HANDLE methHnd)
{
    return emitComp->eeGetMethodFullName(methHnd);
}

#endif // DEBUG

/*****************************************************************************
 *
 *  Be very careful, some instruction descriptors are allocated as "tiny" and
 *  don't have some of the tail fields of instrDesc (in particular, "idInfo").
 */

const BYTE emitter::emitFmtToOps[] = {
#define IF_DEF(en, op1, op2) ID_OP_##op2,
#include "emitfmts.h"
};

#ifdef DEBUG
const unsigned emitter::emitFmtCount = _countof(emitFmtToOps);
#endif

/*****************************************************************************
 *
 *  Display the current instruction group list.
 */

#ifdef DEBUG

void emitter::emitDispIGflags(unsigned flags)
{
    if (flags & IGF_GC_VARS)
    {
        printf(", gcvars");
    }
    if (flags & IGF_BYREF_REGS)
    {
        printf(", byref");
    }
#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    if (flags & IGF_FINALLY_TARGET)
    {
        printf(", ftarget");
    }
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    if (flags & IGF_FUNCLET_PROLOG)
    {
        printf(", funclet prolog");
    }
    if (flags & IGF_FUNCLET_EPILOG)
    {
        printf(", funclet epilog");
    }
    if (flags & IGF_EPILOG)
    {
        printf(", epilog");
    }
    if (flags & IGF_NOGCINTERRUPT)
    {
        printf(", nogc");
    }
    if (flags & IGF_UPD_ISZ)
    {
        printf(", isz");
    }
    if (flags & IGF_EMIT_ADD)
    {
        printf(", emitadd");
    }
}

void emitter::emitDispIG(insGroup* ig, insGroup* igPrev, bool verbose)
{
    const int TEMP_BUFFER_LEN = 40;
    char      buff[TEMP_BUFFER_LEN];

    sprintf_s(buff, TEMP_BUFFER_LEN, "G_M%03u_IG%02u:        ", Compiler::s_compMethodsCount, ig->igNum);
    printf("%s; ", buff);
    if ((igPrev == nullptr) || (igPrev->igFuncIdx != ig->igFuncIdx))
    {
        printf("func=%02u, ", ig->igFuncIdx);
    }

    if (ig->igFlags & IGF_PLACEHOLDER)
    {
        insGroup* igPh = ig;

        const char* pszType;
        switch (igPh->igPhData->igPhType)
        {
            case IGPT_PROLOG:
                pszType = "prolog";
                break;
            case IGPT_EPILOG:
                pszType = "epilog";
                break;
#if FEATURE_EH_FUNCLETS
            case IGPT_FUNCLET_PROLOG:
                pszType = "funclet prolog";
                break;
            case IGPT_FUNCLET_EPILOG:
                pszType = "funclet epilog";
                break;
#endif // FEATURE_EH_FUNCLETS
            default:
                pszType = "UNKNOWN";
                break;
        }
        printf("%s placeholder, next placeholder=", pszType);
        if (igPh->igPhData->igPhNext)
        {
            printf("IG%02u ", igPh->igPhData->igPhNext->igNum);
        }
        else
        {
            printf("<END>");
        }

        if (igPh->igPhData->igPhBB != nullptr)
        {
            printf(", %s", igPh->igPhData->igPhBB->dspToString());
        }

        emitDispIGflags(igPh->igFlags);

        if (ig == emitCurIG)
        {
            printf(" <-- Current IG");
        }
        if (igPh == emitPlaceholderList)
        {
            printf(" <-- First placeholder");
        }
        if (igPh == emitPlaceholderLast)
        {
            printf(" <-- Last placeholder");
        }
        printf("\n");

        printf("%*s;   PrevGCVars=%s ", strlen(buff), "",
               VarSetOps::ToString(emitComp, igPh->igPhData->igPhPrevGCrefVars));
        dumpConvertedVarSet(emitComp, igPh->igPhData->igPhPrevGCrefVars);
        printf(", PrevGCrefRegs=");
        printRegMaskInt(igPh->igPhData->igPhPrevGCrefRegs);
        emitDispRegSet(igPh->igPhData->igPhPrevGCrefRegs);
        printf(", PrevByrefRegs=");
        printRegMaskInt(igPh->igPhData->igPhPrevByrefRegs);
        emitDispRegSet(igPh->igPhData->igPhPrevByrefRegs);
        printf("\n");

        printf("%*s;   InitGCVars=%s ", strlen(buff), "",
               VarSetOps::ToString(emitComp, igPh->igPhData->igPhInitGCrefVars));
        dumpConvertedVarSet(emitComp, igPh->igPhData->igPhInitGCrefVars);
        printf(", InitGCrefRegs=");
        printRegMaskInt(igPh->igPhData->igPhInitGCrefRegs);
        emitDispRegSet(igPh->igPhData->igPhInitGCrefRegs);
        printf(", InitByrefRegs=");
        printRegMaskInt(igPh->igPhData->igPhInitByrefRegs);
        emitDispRegSet(igPh->igPhData->igPhInitByrefRegs);
        printf("\n");

        assert(!(ig->igFlags & IGF_GC_VARS));
        assert(!(ig->igFlags & IGF_BYREF_REGS));
    }
    else
    {
        printf("offs=%06XH, size=%04XH", ig->igOffs, ig->igSize);

        if (ig->igFlags & IGF_GC_VARS)
        {
            printf(", gcVars=%s ", VarSetOps::ToString(emitComp, ig->igGCvars()));
            dumpConvertedVarSet(emitComp, ig->igGCvars());
        }

        if (!(ig->igFlags & IGF_EMIT_ADD))
        {
            printf(", gcrefRegs=");
            printRegMaskInt(ig->igGCregs);
            emitDispRegSet(ig->igGCregs);
        }

        if (ig->igFlags & IGF_BYREF_REGS)
        {
            printf(", byrefRegs=");
            printRegMaskInt(ig->igByrefRegs());
            emitDispRegSet(ig->igByrefRegs());
        }

        emitDispIGflags(ig->igFlags);

        if (ig == emitCurIG)
        {
            printf(" <-- Current IG");
        }
        if (ig == emitPrologIG)
        {
            printf(" <-- Prolog IG");
        }
        printf("\n");

        if (verbose)
        {
            BYTE*          ins = ig->igData;
            UNATIVE_OFFSET ofs = ig->igOffs;
            unsigned       cnt = ig->igInsCnt;

            if (cnt)
            {
                printf("\n");

                do
                {
                    instrDesc* id = (instrDesc*)ins;

                    emitDispIns(id, false, true, false, ofs, nullptr, 0, ig);

                    ins += emitSizeOfInsDsc(id);
                    ofs += emitInstCodeSz(id);
                } while (--cnt);

                printf("\n");
            }
        }
    }
}

void emitter::emitDispIGlist(bool verbose)
{
    insGroup* ig;
    insGroup* igPrev;

    for (igPrev = nullptr, ig = emitIGlist; ig; igPrev = ig, ig = ig->igNext)
    {
        emitDispIG(ig, igPrev, verbose);
    }
}

void emitter::emitDispGCinfo()
{
    printf("Emitter GC tracking info:");
    printf("\n  emitPrevGCrefVars ");
    dumpConvertedVarSet(emitComp, emitPrevGCrefVars);
    printf("\n  emitPrevGCrefRegs(0x%p)=", dspPtr(&emitPrevGCrefRegs));
    printRegMaskInt(emitPrevGCrefRegs);
    emitDispRegSet(emitPrevGCrefRegs);
    printf("\n  emitPrevByrefRegs(0x%p)=", dspPtr(&emitPrevByrefRegs));
    printRegMaskInt(emitPrevByrefRegs);
    emitDispRegSet(emitPrevByrefRegs);
    printf("\n  emitInitGCrefVars ");
    dumpConvertedVarSet(emitComp, emitInitGCrefVars);
    printf("\n  emitInitGCrefRegs(0x%p)=", dspPtr(&emitInitGCrefRegs));
    printRegMaskInt(emitInitGCrefRegs);
    emitDispRegSet(emitInitGCrefRegs);
    printf("\n  emitInitByrefRegs(0x%p)=", dspPtr(&emitInitByrefRegs));
    printRegMaskInt(emitInitByrefRegs);
    emitDispRegSet(emitInitByrefRegs);
    printf("\n  emitThisGCrefVars ");
    dumpConvertedVarSet(emitComp, emitThisGCrefVars);
    printf("\n  emitThisGCrefRegs(0x%p)=", dspPtr(&emitThisGCrefRegs));
    printRegMaskInt(emitThisGCrefRegs);
    emitDispRegSet(emitThisGCrefRegs);
    printf("\n  emitThisByrefRegs(0x%p)=", dspPtr(&emitThisByrefRegs));
    printRegMaskInt(emitThisByrefRegs);
    emitDispRegSet(emitThisByrefRegs);
    printf("\n\n");
}

#endif // DEBUG

/*****************************************************************************
 *
 *  Issue the given instruction. Basically, this is just a thin wrapper around
 *  emitOutputInstr() that does a few debug checks.
 */

size_t emitter::emitIssue1Instr(insGroup* ig, instrDesc* id, BYTE** dp)
{
    size_t is;

    /* Record the beginning offset of the instruction */

    BYTE* curInsAdr = *dp;

    /* Issue the next instruction */

    // printf("[S=%02u] " , emitCurStackLvl);

    is = emitOutputInstr(ig, id, dp);

// printf("[S=%02u]\n", emitCurStackLvl);

#if EMIT_TRACK_STACK_DEPTH

    /*
        If we're generating a full pointer map and the stack
        is empty, there better not be any "pending" argument
        push entries.
     */

    assert(emitFullGCinfo == false || emitCurStackLvl != 0 || u2.emitGcArgTrackCnt == 0);

#endif

    /* Did the size of the instruction match our expectations? */

    UNATIVE_OFFSET csz = (UNATIVE_OFFSET)(*dp - curInsAdr);

    if (csz != id->idCodeSize())
    {
        /* It is fatal to under-estimate the instruction size */
        noway_assert(emitInstCodeSz(id) >= csz);

#if DEBUG_EMIT
        if (EMITVERBOSE)
        {
            printf("Instruction predicted size = %u, actual = %u\n", emitInstCodeSz(id), csz);
        }
#endif // DEBUG_EMIT

        /* The instruction size estimate wasn't accurate; remember this */

        ig->igFlags |= IGF_UPD_ISZ;
#if defined(_TARGET_XARCH_)
        id->idCodeSize(csz);
#elif defined(_TARGET_ARM_)
// This is done as part of emitSetShortJump();
// insSize isz = emitInsSize(id->idInsFmt());
// id->idInsSize(isz);
#else
        /* It is fatal to over-estimate the instruction size */
        IMPL_LIMITATION("Over-estimated instruction size");
#endif
    }

#ifdef DEBUG
    /* Make sure the instruction descriptor size also matches our expectations */
    if (is != emitSizeOfInsDsc(id))
    {
        printf("%s at %u: Expected size = %u , actual size = %u\n", emitIfName(id->idInsFmt()),
               id->idDebugOnlyInfo()->idNum, is, emitSizeOfInsDsc(id));
        assert(is == emitSizeOfInsDsc(id));
    }
#endif

    return is;
}

/*****************************************************************************
 *
 *  Update the offsets of all the instruction groups (note: please don't be
 *  lazy and call this routine frequently, it walks the list of instruction
 *  groups and thus it isn't cheap).
 */

void emitter::emitRecomputeIGoffsets()
{
    UNATIVE_OFFSET offs;
    insGroup*      ig;

    for (ig = emitIGlist, offs = 0; ig; ig = ig->igNext)
    {
        ig->igOffs = offs;
        assert(IsCodeAligned(ig->igOffs));
        offs += ig->igSize;
    }

    /* Set the total code size */

    emitTotalCodeSize = offs;

#ifdef DEBUG
    emitCheckIGoffsets();
#endif
}

/*****************************************************************************
 *  Bind targets of relative jumps to choose the smallest possible encoding.
 *  X86 and AMD64 have a small and large encoding.
 *  ARM has a small, medium, and large encoding. The large encoding is a pseudo-op
 *      to handle greater range than the conditional branch instructions can handle.
 *  ARM64 has a small and large encoding for both conditional branch and loading label addresses.
 *      The large encodings are pseudo-ops that represent a multiple instruction sequence, similar to ARM. (Currently
 *      NYI).
 */

void emitter::emitJumpDistBind()
{
#ifdef DEBUG
    if (emitComp->verbose)
    {
        printf("*************** In emitJumpDistBind()\n");
    }
    if (EMIT_INSTLIST_VERBOSE)
    {
        printf("\nInstruction list before jump distance binding:\n\n");
        emitDispIGlist(true);
    }
#endif

    instrDescJmp* jmp;

    UNATIVE_OFFSET minShortExtra; // The smallest offset greater than that required for a jump to be converted
                                  // to a small jump. If it is small enough, we will iterate in hopes of
                                  // converting those jumps we missed converting the first (or second...) time.

#if defined(_TARGET_ARM_)
    UNATIVE_OFFSET minMediumExtra; // Same as 'minShortExtra', but for medium-sized jumps.
#endif                             // _TARGET_ARM_

    UNATIVE_OFFSET adjIG;
    UNATIVE_OFFSET adjLJ;
    insGroup*      lstIG;
#ifdef DEBUG
    insGroup* prologIG = emitPrologIG;
#endif // DEBUG

    int jmp_iteration = 1;

/*****************************************************************************/
/* If we iterate to look for more jumps to shorten, we start again here.     */
/*****************************************************************************/

AGAIN:

#ifdef DEBUG
    emitCheckIGoffsets();
#endif

/*
    In the following loop we convert all jump targets from "BasicBlock *"
    to "insGroup *" values. We also estimate which jumps will be short.
 */

#ifdef DEBUG
    insGroup*     lastIG = nullptr;
    instrDescJmp* lastLJ = nullptr;
#endif

    lstIG         = nullptr;
    adjLJ         = 0;
    adjIG         = 0;
    minShortExtra = (UNATIVE_OFFSET)-1;

#if defined(_TARGET_ARM_)
    minMediumExtra = (UNATIVE_OFFSET)-1;
#endif // _TARGET_ARM_

    for (jmp = emitJumpList; jmp; jmp = jmp->idjNext)
    {
        insGroup* jmpIG;
        insGroup* tgtIG;

        UNATIVE_OFFSET jsz; // size of the jump instruction in bytes

        UNATIVE_OFFSET ssz = 0; // small  jump size
        NATIVE_OFFSET  nsd = 0; // small  jump max. neg distance
        NATIVE_OFFSET  psd = 0; // small  jump max. pos distance

#if defined(_TARGET_ARM_)
        UNATIVE_OFFSET msz = 0; // medium jump size
        NATIVE_OFFSET  nmd = 0; // medium jump max. neg distance
        NATIVE_OFFSET  pmd = 0; // medium jump max. pos distance
        NATIVE_OFFSET  mextra;  // How far beyond the medium jump range is this jump offset?
#endif                          // _TARGET_ARM_

        NATIVE_OFFSET  extra;           // How far beyond the short jump range is this jump offset?
        UNATIVE_OFFSET srcInstrOffs;    // offset of the source instruction of the jump
        UNATIVE_OFFSET srcEncodingOffs; // offset of the source used by the instruction set to calculate the relative
                                        // offset of the jump
        UNATIVE_OFFSET dstOffs;
        NATIVE_OFFSET  jmpDist; // the relative jump distance, as it will be encoded
        UNATIVE_OFFSET oldSize;
        UNATIVE_OFFSET sizeDif;

#ifdef _TARGET_XARCH_
        assert(jmp->idInsFmt() == IF_LABEL || jmp->idInsFmt() == IF_RWR_LABEL || jmp->idInsFmt() == IF_SWR_LABEL);

        /* Figure out the smallest size we can end up with */

        if (jmp->idInsFmt() == IF_LABEL)
        {
            if (emitIsCondJump(jmp))
            {
                ssz = JCC_SIZE_SMALL;
                nsd = JCC_DIST_SMALL_MAX_NEG;
                psd = JCC_DIST_SMALL_MAX_POS;
            }
            else
            {
                ssz = JMP_SIZE_SMALL;
                nsd = JMP_DIST_SMALL_MAX_NEG;
                psd = JMP_DIST_SMALL_MAX_POS;
            }
        }
#endif // _TARGET_XARCH_

#ifdef _TARGET_ARM_
        assert((jmp->idInsFmt() == IF_T2_J1) || (jmp->idInsFmt() == IF_T2_J2) || (jmp->idInsFmt() == IF_T1_I) ||
               (jmp->idInsFmt() == IF_T1_K) || (jmp->idInsFmt() == IF_T1_M) || (jmp->idInsFmt() == IF_T2_M1) ||
               (jmp->idInsFmt() == IF_T2_N1) || (jmp->idInsFmt() == IF_T1_J3) || (jmp->idInsFmt() == IF_LARGEJMP));

        /* Figure out the smallest size we can end up with */

        if (emitIsCondJump(jmp))
        {
            ssz = JCC_SIZE_SMALL;
            nsd = JCC_DIST_SMALL_MAX_NEG;
            psd = JCC_DIST_SMALL_MAX_POS;

            msz = JCC_SIZE_MEDIUM;
            nmd = JCC_DIST_MEDIUM_MAX_NEG;
            pmd = JCC_DIST_MEDIUM_MAX_POS;
        }
        else if (emitIsCmpJump(jmp))
        {
            ssz = JMP_SIZE_SMALL;
            nsd = 0;
            psd = 126;
        }
        else if (emitIsUncondJump(jmp))
        {
            ssz = JMP_SIZE_SMALL;
            nsd = JMP_DIST_SMALL_MAX_NEG;
            psd = JMP_DIST_SMALL_MAX_POS;
        }
        else if (emitIsLoadLabel(jmp))
        {
            ssz = LBL_SIZE_SMALL;
            nsd = LBL_DIST_SMALL_MAX_NEG;
            psd = LBL_DIST_SMALL_MAX_POS;
        }
        else
        {
            assert(!"Unknown jump instruction");
        }
#endif // _TARGET_ARM_

#ifdef _TARGET_ARM64_
        /* Figure out the smallest size we can end up with */

        if (emitIsCondJump(jmp))
        {
            ssz         = JCC_SIZE_SMALL;
            bool isTest = (jmp->idIns() == INS_tbz) || (jmp->idIns() == INS_tbnz);

            nsd = (isTest) ? TB_DIST_SMALL_MAX_NEG : JCC_DIST_SMALL_MAX_NEG;
            psd = (isTest) ? TB_DIST_SMALL_MAX_POS : JCC_DIST_SMALL_MAX_POS;
        }
        else if (emitIsUncondJump(jmp))
        {
            // Nothing to do; we don't shrink these.
            assert(jmp->idjShort);
            ssz = JMP_SIZE_SMALL;
        }
        else if (emitIsLoadLabel(jmp))
        {
            ssz = LBL_SIZE_SMALL;
            nsd = LBL_DIST_SMALL_MAX_NEG;
            psd = LBL_DIST_SMALL_MAX_POS;
        }
        else if (emitIsLoadConstant(jmp))
        {
            ssz = LDC_SIZE_SMALL;
            nsd = LDC_DIST_SMALL_MAX_NEG;
            psd = LDC_DIST_SMALL_MAX_POS;
        }
        else
        {
            assert(!"Unknown jump instruction");
        }
#endif // _TARGET_ARM64_

/* Make sure the jumps are properly ordered */

#ifdef DEBUG
        assert(lastLJ == nullptr || lastIG != jmp->idjIG || lastLJ->idjOffs < jmp->idjOffs);
        lastLJ = (lastIG == jmp->idjIG) ? jmp : nullptr;

        assert(lastIG == nullptr || lastIG->igNum <= jmp->idjIG->igNum || jmp->idjIG == prologIG ||
               emitNxtIGnum > unsigned(0xFFFF)); // igNum might overflow
        lastIG = jmp->idjIG;
#endif // DEBUG

        /* Get hold of the current jump size */

        jsz = emitSizeOfJump(jmp);

        /* Get the group the jump is in */

        jmpIG = jmp->idjIG;

        /* Are we in a group different from the previous jump? */

        if (lstIG != jmpIG)
        {
            /* Were there any jumps before this one? */

            if (lstIG)
            {
                /* Adjust the offsets of the intervening blocks */

                do
                {
                    lstIG = lstIG->igNext;
                    assert(lstIG);
#ifdef DEBUG
                    if (EMITVERBOSE)
                    {
                        printf("Adjusted offset of " FMT_BB " from %04X to %04X\n", lstIG->igNum, lstIG->igOffs,
                               lstIG->igOffs - adjIG);
                    }
#endif // DEBUG
                    lstIG->igOffs -= adjIG;
                    assert(IsCodeAligned(lstIG->igOffs));
                } while (lstIG != jmpIG);
            }

            /* We've got the first jump in a new group */

            adjLJ = 0;
            lstIG = jmpIG;
        }

        /* Apply any local size adjustment to the jump's relative offset */

        jmp->idjOffs -= adjLJ;

        // If this is a jump via register, the instruction size does not change, so we are done.
        CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(_TARGET_ARM64_)
        // JIT code and data will be allocated together for arm64 so the relative offset to JIT data is known.
        // In case such offset can be encodeable for `ldr` (+-1MB), shorten it.
        if (jmp->idAddr()->iiaIsJitDataOffset())
        {
            // Reference to JIT data
            assert(jmp->idIsBound());
            UNATIVE_OFFSET srcOffs = jmpIG->igOffs + jmp->idjOffs;

            int doff = jmp->idAddr()->iiaGetJitDataOffset();
            assert(doff >= 0);
            ssize_t imm = emitGetInsSC(jmp);
            assert((imm >= 0) && (imm < 0x1000)); // 0x1000 is arbitrary, currently 'imm' is always 0

            unsigned dataOffs = (unsigned)(doff + imm);
            assert(dataOffs < emitDataSize());

            // Conservately assume JIT data starts after the entire code size.
            // TODO-ARM64: we might consider only hot code size which will be computed later in emitComputeCodeSizes().
            assert(emitTotalCodeSize > 0);
            UNATIVE_OFFSET maxDstOffs = emitTotalCodeSize + dataOffs;

            // Check if the distance is within the encoding length.
            jmpDist = maxDstOffs - srcOffs;
            extra   = jmpDist - psd;
            if (extra <= 0)
            {
                goto SHORT_JMP;
            }

            // Keep the large form.
            continue;
        }
#endif

        /* Have we bound this jump's target already? */

        if (jmp->idIsBound())
        {
            /* Does the jump already have the smallest size? */

            if (jmp->idjShort)
            {
                assert(emitSizeOfJump(jmp) == ssz);

                // We should not be jumping/branching across funclets/functions
                emitCheckFuncletBranch(jmp, jmpIG);

                continue;
            }

            tgtIG = jmp->idAddr()->iiaIGlabel;
        }
        else
        {
            /* First time we've seen this label, convert its target */
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
            if (EMITVERBOSE)
            {
                printf("Binding: ");
                emitDispIns(jmp, false, false, false);
                printf("Binding L_M%03u_" FMT_BB, Compiler::s_compMethodsCount, jmp->idAddr()->iiaBBlabel->bbNum);
            }
#endif // DEBUG

            tgtIG = (insGroup*)emitCodeGetCookie(jmp->idAddr()->iiaBBlabel);

#ifdef DEBUG
            if (EMITVERBOSE)
            {
                if (tgtIG)
                {
                    printf("to G_M%03u_IG%02u\n", Compiler::s_compMethodsCount, tgtIG->igNum);
                }
                else
                {
                    printf("-- ERROR, no emitter cookie for " FMT_BB "; it is probably missing BBF_JMP_TARGET or "
                           "BBF_HAS_LABEL.\n",
                           jmp->idAddr()->iiaBBlabel->bbNum);
                }
            }
            assert(tgtIG);
#endif // DEBUG

            /* Record the bound target */

            jmp->idAddr()->iiaIGlabel = tgtIG;
            jmp->idSetIsBound();
        }

        // We should not be jumping/branching across funclets/functions
        emitCheckFuncletBranch(jmp, jmpIG);

#ifdef _TARGET_XARCH_
        /* Done if this is not a variable-sized jump */

        if ((jmp->idIns() == INS_push) || (jmp->idIns() == INS_mov) || (jmp->idIns() == INS_call) ||
            (jmp->idIns() == INS_push_hide))
        {
            continue;
        }
#endif
#ifdef _TARGET_ARM_
        if ((jmp->idIns() == INS_push) || (jmp->idIns() == INS_mov) || (jmp->idIns() == INS_movt) ||
            (jmp->idIns() == INS_movw))
        {
            continue;
        }
#endif
#ifdef _TARGET_ARM64_
        // There is only one size of unconditional branch; we don't support functions larger than 2^28 bytes (our branch
        // range).
        if (emitIsUncondJump(jmp))
        {
            continue;
        }
#endif

        /*
            In the following distance calculations, if we're not actually
            scheduling the code (i.e. reordering instructions), we can
            use the actual offset of the jump (rather than the beg/end of
            the instruction group) since the jump will not be moved around
            and thus its offset is accurate.

            First we need to figure out whether this jump is a forward or
            backward one; to do this we simply look at the ordinals of the
            group that contains the jump and the target.
         */

        srcInstrOffs = jmpIG->igOffs + jmp->idjOffs;

        /* Note that the destination is always the beginning of an IG, so no need for an offset inside it */
        dstOffs = tgtIG->igOffs;

#if defined(_TARGET_ARM_)
        srcEncodingOffs =
            srcInstrOffs + 4; // For relative branches, ARM PC is always considered to be the instruction address + 4
#elif defined(_TARGET_ARM64_)
        srcEncodingOffs =
            srcInstrOffs; // For relative branches, ARM64 PC is always considered to be the instruction address
#else
        srcEncodingOffs = srcInstrOffs + ssz; // Encoding offset of relative offset for small branch
#endif

        if (jmpIG->igNum < tgtIG->igNum)
        {
            /* Forward jump */

            /* Adjust the target offset by the current delta. This is a worst-case estimate, as jumps between
               here and the target could be shortened, causing the actual distance to shrink.
             */

            dstOffs -= adjIG;

            /* Compute the distance estimate */

            jmpDist = dstOffs - srcEncodingOffs;

            /* How much beyond the max. short distance does the jump go? */

            extra = jmpDist - psd;

#if DEBUG_EMIT
            assert(jmp->idDebugOnlyInfo() != nullptr);
            if (jmp->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM || INTERESTING_JUMP_NUM == 0)
            {
                if (INTERESTING_JUMP_NUM == 0)
                {
                    printf("[1] Jump %u:\n", jmp->idDebugOnlyInfo()->idNum);
                }
                printf("[1] Jump  block is at %08X\n", jmpIG->igOffs);
                printf("[1] Jump reloffset is %04X\n", jmp->idjOffs);
                printf("[1] Jump source is at %08X\n", srcEncodingOffs);
                printf("[1] Label block is at %08X\n", dstOffs);
                printf("[1] Jump  dist. is    %04X\n", jmpDist);
                if (extra > 0)
                {
                    printf("[1] Dist excess [S] = %d  \n", extra);
                }
            }
            if (EMITVERBOSE)
            {
                printf("Estimate of fwd jump [%08X/%03u]: %04X -> %04X = %04X\n", dspPtr(jmp),
                       jmp->idDebugOnlyInfo()->idNum, srcInstrOffs, dstOffs, jmpDist);
            }
#endif // DEBUG_EMIT

            if (extra <= 0)
            {
                /* This jump will be a short one */
                goto SHORT_JMP;
            }
        }
        else
        {
            /* Backward jump */

            /* Compute the distance estimate */

            jmpDist = srcEncodingOffs - dstOffs;

            /* How much beyond the max. short distance does the jump go? */

            extra = jmpDist + nsd;

#if DEBUG_EMIT
            assert(jmp->idDebugOnlyInfo() != nullptr);
            if (jmp->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM || INTERESTING_JUMP_NUM == 0)
            {
                if (INTERESTING_JUMP_NUM == 0)
                {
                    printf("[2] Jump %u:\n", jmp->idDebugOnlyInfo()->idNum);
                }
                printf("[2] Jump  block is at %08X\n", jmpIG->igOffs);
                printf("[2] Jump reloffset is %04X\n", jmp->idjOffs);
                printf("[2] Jump source is at %08X\n", srcEncodingOffs);
                printf("[2] Label block is at %08X\n", dstOffs);
                printf("[2] Jump  dist. is    %04X\n", jmpDist);
                if (extra > 0)
                {
                    printf("[2] Dist excess [S] = %d  \n", extra);
                }
            }
            if (EMITVERBOSE)
            {
                printf("Estimate of bwd jump [%08X/%03u]: %04X -> %04X = %04X\n", dspPtr(jmp),
                       jmp->idDebugOnlyInfo()->idNum, srcInstrOffs, dstOffs, jmpDist);
            }
#endif // DEBUG_EMIT

            if (extra <= 0)
            {
                /* This jump will be a short one */
                goto SHORT_JMP;
            }
        }

        /* We arrive here if the jump couldn't be made short, at least for now */

        /* We had better not have eagerly marked the jump as short
         * in emitIns_J(). If we did, then it has to be able to stay short
         * as emitIns_J() uses the worst case scenario, and blocks can
         * only move closer together after that.
         */
        assert(jmp->idjShort == 0);

        /* Keep track of the closest distance we got */

        if (minShortExtra > (unsigned)extra)
        {
            minShortExtra = (unsigned)extra;
        }

#if defined(_TARGET_ARM_)

        // If we're here, we couldn't convert to a small jump.
        // Handle conversion to medium-sized conditional jumps.
        // 'srcInstrOffs', 'srcEncodingOffs', 'dstOffs', 'jmpDist' have already been computed
        // and don't need to be recomputed.

        if (emitIsCondJump(jmp))
        {
            if (jmpIG->igNum < tgtIG->igNum)
            {
                /* Forward jump */

                /* How much beyond the max. medium distance does the jump go? */

                mextra = jmpDist - pmd;

#if DEBUG_EMIT
                assert(jmp->idDebugOnlyInfo() != NULL);
                if (jmp->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM || INTERESTING_JUMP_NUM == 0)
                {
                    if (mextra > 0)
                    {
                        if (INTERESTING_JUMP_NUM == 0)
                            printf("[6] Jump %u:\n", jmp->idDebugOnlyInfo()->idNum);
                        printf("[6] Dist excess [S] = %d  \n", mextra);
                    }
                }
#endif // DEBUG_EMIT

                if (mextra <= 0)
                {
                    /* This jump will be a medium one */
                    goto MEDIUM_JMP;
                }
            }
            else
            {
                /* Backward jump */

                /* How much beyond the max. medium distance does the jump go? */

                mextra = jmpDist + nmd;

#if DEBUG_EMIT
                assert(jmp->idDebugOnlyInfo() != NULL);
                if (jmp->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM || INTERESTING_JUMP_NUM == 0)
                {
                    if (mextra > 0)
                    {
                        if (INTERESTING_JUMP_NUM == 0)
                            printf("[7] Jump %u:\n", jmp->idDebugOnlyInfo()->idNum);
                        printf("[7] Dist excess [S] = %d  \n", mextra);
                    }
                }
#endif // DEBUG_EMIT

                if (mextra <= 0)
                {
                    /* This jump will be a medium one */
                    goto MEDIUM_JMP;
                }
            }

            /* We arrive here if the jump couldn't be made medium, at least for now */

            /* Keep track of the closest distance we got */

            if (minMediumExtra > (unsigned)mextra)
                minMediumExtra = (unsigned)mextra;
        }

#endif // _TARGET_ARM_

        /*****************************************************************************
         * We arrive here if the jump must stay long, at least for now.
         * Go try the next one.
         */

        continue;

    /*****************************************************************************/
    /* Handle conversion to short jump                                           */
    /*****************************************************************************/

    SHORT_JMP:

        /* Try to make this jump a short one */

        emitSetShortJump(jmp);

        if (!jmp->idjShort)
        {
            continue; // This jump must be kept long
        }

        /* This jump is becoming either short or medium */

        oldSize = jsz;
        jsz     = ssz;
        assert(oldSize >= jsz);
        sizeDif = oldSize - jsz;

#if defined(_TARGET_XARCH_)
        jmp->idCodeSize(jsz);
#elif defined(_TARGET_ARM_)
#if 0
        // This is done as part of emitSetShortJump():
        insSize isz = emitInsSize(jmp->idInsFmt());
        jmp->idInsSize(isz);
#endif
#elif defined(_TARGET_ARM64_)
        // The size of IF_LARGEJMP/IF_LARGEADR/IF_LARGELDC are 8 or 12.
        // All other code size is 4.
        assert((sizeDif == 4) || (sizeDif == 8));
#else
#error Unsupported or unset target architecture
#endif

        goto NEXT_JMP;

#if defined(_TARGET_ARM_)

    /*****************************************************************************/
    /* Handle conversion to medium jump                                          */
    /*****************************************************************************/

    MEDIUM_JMP:

        /* Try to make this jump a medium one */

        emitSetMediumJump(jmp);

        if (jmp->idCodeSize() > msz)
        {
            continue; // This jump wasn't shortened
        }
        assert(jmp->idCodeSize() == msz);

        /* This jump is becoming medium */

        oldSize = jsz;
        jsz     = msz;
        assert(oldSize >= jsz);
        sizeDif = oldSize - jsz;

        goto NEXT_JMP;

#endif // _TARGET_ARM_

    /*****************************************************************************/

    NEXT_JMP:

        /* Make sure the size of the jump is marked correctly */

        assert((0 == (jsz | jmpDist)) || (jsz == emitSizeOfJump(jmp)));

#ifdef DEBUG
        if (EMITVERBOSE)
        {
            printf("Shrinking jump [%08X/%03u]\n", dspPtr(jmp), jmp->idDebugOnlyInfo()->idNum);
        }
#endif
        noway_assert((unsigned short)sizeDif == sizeDif);

        adjIG += sizeDif;
        adjLJ += sizeDif;
        jmpIG->igSize -= (unsigned short)sizeDif;
        emitTotalCodeSize -= sizeDif;

        /* The jump size estimate wasn't accurate; flag its group */

        jmpIG->igFlags |= IGF_UPD_ISZ;

    } // end for each jump

    /* Did we shorten any jumps? */

    if (adjIG)
    {
        /* Adjust offsets of any remaining blocks */

        assert(lstIG);

        for (;;)
        {
            lstIG = lstIG->igNext;
            if (!lstIG)
            {
                break;
            }
#ifdef DEBUG
            if (EMITVERBOSE)
            {
                printf("Adjusted offset of " FMT_BB " from %04X to %04X\n", lstIG->igNum, lstIG->igOffs,
                       lstIG->igOffs - adjIG);
            }
#endif // DEBUG
            lstIG->igOffs -= adjIG;
            assert(IsCodeAligned(lstIG->igOffs));
        }

#ifdef DEBUG
        emitCheckIGoffsets();
#endif

        /* Is there a chance of other jumps becoming short? */
        CLANG_FORMAT_COMMENT_ANCHOR;
#ifdef DEBUG
#if defined(_TARGET_ARM_)
        if (EMITVERBOSE)
            printf("Total shrinkage = %3u, min extra short jump size = %3u, min extra medium jump size = %u\n", adjIG,
                   minShortExtra, minMediumExtra);
#else
        if (EMITVERBOSE)
        {
            printf("Total shrinkage = %3u, min extra jump size = %3u\n", adjIG, minShortExtra);
        }
#endif
#endif

        if ((minShortExtra <= adjIG)
#if defined(_TARGET_ARM_)
            || (minMediumExtra <= adjIG)
#endif // _TARGET_ARM_
                )
        {
            jmp_iteration++;

#ifdef DEBUG
            if (EMITVERBOSE)
            {
                printf("Iterating branch shortening. Iteration = %d\n", jmp_iteration);
            }
#endif

            goto AGAIN;
        }
    }
#ifdef DEBUG
    if (EMIT_INSTLIST_VERBOSE)
    {
        printf("\nLabels list after the jump dist binding:\n\n");
        emitDispIGlist(false);
    }

    emitCheckIGoffsets();
#endif // DEBUG
}

void emitter::emitCheckFuncletBranch(instrDesc* jmp, insGroup* jmpIG)
{
#ifdef DEBUG
    // We should not be jumping/branching across funclets/functions
    // Except possibly a 'call' to a finally funclet for a local unwind
    // or a 'return' from a catch handler (that can go just about anywhere)
    // This routine attempts to validate that any branches across funclets
    // meets one of those criteria...
    assert(jmp->idIsBound());

#ifdef _TARGET_XARCH_
    // An lea of a code address (for constant data stored with the code)
    // is treated like a jump for emission purposes but is not really a jump so
    // we don't have to check anything here.
    if (jmp->idIns() == INS_lea)
    {
        return;
    }
#endif

#ifdef _TARGET_ARMARCH_
    if (jmp->idAddr()->iiaHasInstrCount())
    {
        // Too hard to figure out funclets from just an instruction count
        // You're on your own!
        return;
    }
#endif // _TARGET_ARMARCH_

#ifdef _TARGET_ARM64_
    // No interest if it's not jmp.
    if (emitIsLoadLabel(jmp) || emitIsLoadConstant(jmp))
    {
        return;
    }
#endif // _TARGET_ARM64_

    insGroup* tgtIG = jmp->idAddr()->iiaIGlabel;
    assert(tgtIG);
    if (tgtIG->igFuncIdx != jmpIG->igFuncIdx)
    {
        if (jmp->idDebugOnlyInfo()->idFinallyCall)
        {
            // We don't record enough information to determine this accurately, so instead
            // we assume that any branch to the very start of a finally is OK.

            // No branches back to the root method
            assert(tgtIG->igFuncIdx > 0);
            FuncInfoDsc* tgtFunc = emitComp->funGetFunc(tgtIG->igFuncIdx);
            assert(tgtFunc->funKind == FUNC_HANDLER);
            EHblkDsc* tgtEH = emitComp->ehGetDsc(tgtFunc->funEHIndex);

            // Only branches to finallys (not faults, catches, filters, etc.)
            assert(tgtEH->HasFinallyHandler());

            // Only to the first block of the finally (which is properly marked)
            BasicBlock* tgtBlk = tgtEH->ebdHndBeg;
            assert(tgtBlk->bbFlags & BBF_FUNCLET_BEG);

            // And now we made it back to where we started
            assert(tgtIG == emitCodeGetCookie(tgtBlk));
            assert(tgtIG->igFuncIdx == emitComp->funGetFuncIdx(tgtBlk));
        }
        else if (jmp->idDebugOnlyInfo()->idCatchRet)
        {
            // Again there isn't enough information to prove this correct
            // so just allow a 'branch' to any other 'parent' funclet

            FuncInfoDsc* jmpFunc = emitComp->funGetFunc(jmpIG->igFuncIdx);
            assert(jmpFunc->funKind == FUNC_HANDLER);
            EHblkDsc* jmpEH = emitComp->ehGetDsc(jmpFunc->funEHIndex);

            // Only branches out of catches
            assert(jmpEH->HasCatchHandler());

            FuncInfoDsc* tgtFunc = emitComp->funGetFunc(tgtIG->igFuncIdx);
            assert(tgtFunc);
            if (tgtFunc->funKind == FUNC_HANDLER)
            {
                // An outward chain to the containing funclet/EH handler
                // Note that it might be anywhere within nested try bodies
                assert(jmpEH->ebdEnclosingHndIndex == tgtFunc->funEHIndex);
            }
            else
            {
                // This funclet is 'top level' and so it is branching back to the
                // root function, and should have no containing EH handlers
                // but it could be nested within try bodies...
                assert(tgtFunc->funKind == FUNC_ROOT);
                assert(jmpEH->ebdEnclosingHndIndex == EHblkDsc::NO_ENCLOSING_INDEX);
            }
        }
        else
        {
            printf("Hit an illegal branch between funclets!");
            assert(tgtIG->igFuncIdx == jmpIG->igFuncIdx);
        }
    }
#endif // DEBUG
}

/*****************************************************************************
 *
 *  Compute the code sizes that we're going to use to allocate the code buffers.
 *
 *  This sets:
 *
 *      emitTotalHotCodeSize
 *      emitTotalColdCodeSize
 *      Compiler::info.compTotalHotCodeSize
 *      Compiler::info.compTotalColdCodeSize
 */

void emitter::emitComputeCodeSizes()
{
    assert((emitComp->fgFirstColdBlock == nullptr) == (emitFirstColdIG == nullptr));

    if (emitFirstColdIG)
    {
        emitTotalHotCodeSize  = emitFirstColdIG->igOffs;
        emitTotalColdCodeSize = emitTotalCodeSize - emitTotalHotCodeSize;
    }
    else
    {
        emitTotalHotCodeSize  = emitTotalCodeSize;
        emitTotalColdCodeSize = 0;
    }

    emitComp->info.compTotalHotCodeSize  = emitTotalHotCodeSize;
    emitComp->info.compTotalColdCodeSize = emitTotalColdCodeSize;

#ifdef DEBUG
    if (emitComp->verbose)
    {
        printf("\nHot  code size = 0x%X bytes\n", emitTotalHotCodeSize);
        printf("Cold code size = 0x%X bytes\n", emitTotalColdCodeSize);
    }
#endif
}

/*****************************************************************************
 *
 *  Called at the end of code generation, this method creates the code, data
 *  and GC info blocks for the method.  Returns the size of the method (which must fit in an unsigned).
 */

unsigned emitter::emitEndCodeGen(Compiler* comp,
                                 bool      contTrkPtrLcls,
                                 bool      fullyInt,
                                 bool      fullPtrMap,
                                 bool      returnsGCr,
                                 unsigned  xcptnsCount,
                                 unsigned* prologSize,
                                 unsigned* epilogSize,
                                 void**    codeAddr,
                                 void**    coldCodeAddr,
                                 void**    consAddr)
{
#ifdef DEBUG
    if (emitComp->verbose)
    {
        printf("*************** In emitEndCodeGen()\n");
    }
#endif

    BYTE* consBlock;
    BYTE* codeBlock;
    BYTE* coldCodeBlock;
    BYTE* cp;

    assert(emitCurIG == nullptr);

    emitCodeBlock = nullptr;
    emitConsBlock = nullptr;

    /* Tell everyone whether we have fully interruptible code or not */

    emitFullyInt   = fullyInt;
    emitFullGCinfo = fullPtrMap;

#ifndef UNIX_X86_ABI
    emitFullArgInfo = !emitHasFramePtr;
#else
    emitFullArgInfo = fullPtrMap;
#endif

#if EMITTER_STATS
    GCrefsTable.record(emitGCrFrameOffsCnt);
    emitSizeTable.record(static_cast<unsigned>(emitSizeMethod));
    stkDepthTable.record(emitMaxStackDepth);
#endif // EMITTER_STATS

    // Default values, correct even if EMIT_TRACK_STACK_DEPTH is 0.
    emitSimpleStkUsed         = true;
    u1.emitSimpleStkMask      = 0;
    u1.emitSimpleByrefStkMask = 0;

#if EMIT_TRACK_STACK_DEPTH
    /* Convert max. stack depth from # of bytes to # of entries */

    unsigned maxStackDepthIn4ByteElements = emitMaxStackDepth / sizeof(int);
    JITDUMP("Converting emitMaxStackDepth from bytes (%d) to elements (%d)\n", emitMaxStackDepth,
            maxStackDepthIn4ByteElements);
    emitMaxStackDepth = maxStackDepthIn4ByteElements;

    /* Should we use the simple stack */

    if (emitMaxStackDepth > MAX_SIMPLE_STK_DEPTH || emitFullGCinfo)
    {
        /* We won't use the "simple" argument table */

        emitSimpleStkUsed = false;

        /* Allocate the argument tracking table */

        if (emitMaxStackDepth <= sizeof(u2.emitArgTrackLcl))
        {
            u2.emitArgTrackTab = (BYTE*)u2.emitArgTrackLcl;
        }
        else
        {
            u2.emitArgTrackTab = (BYTE*)emitGetMem(roundUp(emitMaxStackDepth));
        }

        u2.emitArgTrackTop   = u2.emitArgTrackTab;
        u2.emitGcArgTrackCnt = 0;
    }
#endif

    if (emitEpilogCnt == 0)
    {
        /* No epilogs, make sure the epilog size is set to 0 */

        emitEpilogSize = 0;

#ifdef _TARGET_XARCH_
        emitExitSeqSize = 0;
#endif // _TARGET_XARCH_
    }

    /* Return the size of the epilog to the caller */

    *epilogSize = emitEpilogSize;

#ifdef _TARGET_XARCH_
    *epilogSize += emitExitSeqSize;
#endif // _TARGET_XARCH_

#ifdef DEBUG
    if (EMIT_INSTLIST_VERBOSE)
    {
        printf("\nInstruction list before instruction issue:\n\n");
        emitDispIGlist(true);
    }

    emitCheckIGoffsets();
#endif

    /* Allocate the code block (and optionally the data blocks) */

    // If we're doing procedure splitting and we found cold blocks, then
    // allocate hot and cold buffers.  Otherwise only allocate a hot
    // buffer.

    coldCodeBlock = nullptr;

    CorJitAllocMemFlag allocMemFlag = CORJIT_ALLOCMEM_DEFAULT_CODE_ALIGN;

#ifdef _TARGET_X86_
    //
    // These are the heuristics we use to decide whether or not to force the
    // code to be 16-byte aligned.
    //
    // 1. For ngen code with IBC data, use 16-byte alignment if the method
    //    has been called more than BB_VERY_HOT_WEIGHT times.
    // 2. For JITed code and ngen code without IBC data, use 16-byte alignment
    //    when the code is 16 bytes or smaller. We align small getters/setters
    //    because of they are penalized heavily on certain hardware when not 16-byte
    //    aligned (VSWhidbey #373938). To minimize size impact of this optimization,
    //    we do not align large methods because of the penalty is amortized for them.
    //
    if (emitComp->fgHaveProfileData())
    {
        if (emitComp->fgCalledCount > (BB_VERY_HOT_WEIGHT * emitComp->fgProfileRunsCount()))
        {
            allocMemFlag = CORJIT_ALLOCMEM_FLG_16BYTE_ALIGN;
        }
    }
    else
    {
        if (emitTotalHotCodeSize <= 16)
        {
            allocMemFlag = CORJIT_ALLOCMEM_FLG_16BYTE_ALIGN;
        }
    }
#endif

    if (emitConsDsc.align16)
    {
        allocMemFlag = static_cast<CorJitAllocMemFlag>(allocMemFlag | CORJIT_ALLOCMEM_FLG_RODATA_16BYTE_ALIGN);
    }

#ifdef _TARGET_ARM64_
    // For arm64, we want to allocate JIT data always adjacent to code similar to what native compiler does.
    // This way allows us to use a single `ldr` to access such data like float constant/jmp table.
    if (emitTotalColdCodeSize > 0)
    {
        // JIT data might be far away from the cold code.
        NYI_ARM64("Need to handle fix-up to data from cold code.");
    }

    UNATIVE_OFFSET roDataAlignmentDelta = 0;
    if (emitConsDsc.dsdOffs)
    {
        UNATIVE_OFFSET roDataAlignment = TARGET_POINTER_SIZE; // 8 Byte align by default.
        roDataAlignmentDelta = (UNATIVE_OFFSET)ALIGN_UP(emitTotalHotCodeSize, roDataAlignment) - emitTotalHotCodeSize;
        assert((roDataAlignmentDelta == 0) || (roDataAlignmentDelta == 4));
    }
    emitCmpHandle->allocMem(emitTotalHotCodeSize + roDataAlignmentDelta + emitConsDsc.dsdOffs, emitTotalColdCodeSize, 0,
                            xcptnsCount, allocMemFlag, (void**)&codeBlock, (void**)&coldCodeBlock, (void**)&consBlock);

    consBlock = codeBlock + emitTotalHotCodeSize + roDataAlignmentDelta;

#else
    emitCmpHandle->allocMem(emitTotalHotCodeSize, emitTotalColdCodeSize, emitConsDsc.dsdOffs, xcptnsCount, allocMemFlag,
                            (void**)&codeBlock, (void**)&coldCodeBlock, (void**)&consBlock);
#endif

    // if (emitConsDsc.dsdOffs)
    //     printf("Cons=%08X\n", consBlock);

    /* Give the block addresses to the caller and other functions here */

    *codeAddr = emitCodeBlock = codeBlock;
    *coldCodeAddr = emitColdCodeBlock = coldCodeBlock;
    *consAddr = emitConsBlock = consBlock;

    /* Nothing has been pushed on the stack */
    CLANG_FORMAT_COMMENT_ANCHOR;

#if EMIT_TRACK_STACK_DEPTH
    emitCurStackLvl = 0;
#endif

    /* Assume no live GC ref variables on entry */

    VarSetOps::ClearD(emitComp, emitThisGCrefVars); // This is initialized to Empty at the start of codegen.
    emitThisGCrefRegs = emitThisByrefRegs = RBM_NONE;
    emitThisGCrefVset                     = true;

#ifdef DEBUG

    emitIssuing = true;

    // We don't use these after this point

    VarSetOps::AssignNoCopy(emitComp, emitPrevGCrefVars, VarSetOps::UninitVal());
    emitPrevGCrefRegs = emitPrevByrefRegs = 0xBAADFEED;

    VarSetOps::AssignNoCopy(emitComp, emitInitGCrefVars, VarSetOps::UninitVal());
    emitInitGCrefRegs = emitInitByrefRegs = 0xBAADFEED;

#endif

    /* Initialize the GC ref variable lifetime tracking logic */

    codeGen->gcInfo.gcVarPtrSetInit();

    emitSyncThisObjOffs = -1;     /* -1  means no offset set */
    emitSyncThisObjReg  = REG_NA; /* REG_NA  means not set */

#ifdef JIT32_GCENCODER
    if (emitComp->lvaKeepAliveAndReportThis())
    {
        assert(emitComp->lvaIsOriginalThisArg(0));
        LclVarDsc* thisDsc = &emitComp->lvaTable[0];

        /* If "this" (which is passed in as a register argument in REG_ARG_0)
           is enregistered, we normally spot the "mov REG_ARG_0 -> thisReg"
           in the prolog and note the location of "this" at that point.
           However, if 'this' is enregistered into REG_ARG_0 itself, no code
           will be generated in the prolog, so we explicitly need to note
           the location of "this" here.
           NOTE that we can do this even if "this" is not enregistered in
           REG_ARG_0, and it will result in more accurate "this" info over the
           prolog. However, as methods are not interruptible over the prolog,
           we try to save space by avoiding that.
         */

        if (thisDsc->lvRegister)
        {
            emitSyncThisObjReg = thisDsc->lvRegNum;

            if (emitSyncThisObjReg == (int)REG_ARG_0 &&
                (codeGen->intRegState.rsCalleeRegArgMaskLiveIn & genRegMask(REG_ARG_0)))
            {
                if (emitFullGCinfo)
                {
                    emitGCregLiveSet(GCT_GCREF, genRegMask(REG_ARG_0),
                                     emitCodeBlock, // from offset 0
                                     true);
                }
                else
                {
                    /* If emitFullGCinfo==false, then we don't use any
                       regPtrDsc's and so explictly note the location
                       of "this" in GCEncode.cpp
                     */
                }
            }
        }
    }
#endif // JIT32_GCENCODER

    emitContTrkPtrLcls = contTrkPtrLcls;

    /* Are there any GC ref variables on the stack? */

    if (emitGCrFrameOffsCnt)
    {
        size_t     siz;
        unsigned   cnt;
        unsigned   num;
        LclVarDsc* dsc;
        int*       tab;

        /* Allocate and clear emitGCrFrameLiveTab[]. This is the table
           mapping "stkOffs -> varPtrDsc". It holds a pointer to
           the liveness descriptor that was created when the
           variable became alive. When the variable becomes dead, the
           descriptor will be appended to the liveness descriptor list, and
           the entry in emitGCrFrameLiveTab[] will be made NULL.

           Note that if all GC refs are assigned consecutively,
           emitGCrFrameLiveTab[] can be only as big as the number of GC refs
           present, instead of lvaTrackedCount.
         */

        siz                 = emitGCrFrameOffsCnt * sizeof(*emitGCrFrameLiveTab);
        emitGCrFrameLiveTab = (varPtrDsc**)emitGetMem(roundUp(siz));
        memset(emitGCrFrameLiveTab, 0, siz);

        /* Allocate and fill in emitGCrFrameOffsTab[]. This is the table
           mapping "varIndex -> stkOffs".
           Non-ptrs or reg vars have entries of -1.
           Entries of Tracked stack byrefs have the lower bit set to 1.
        */

        emitTrkVarCnt = cnt = emitComp->lvaTrackedCount;
        assert(cnt);
        emitGCrFrameOffsTab = tab = (int*)emitGetMem(cnt * sizeof(int));

        memset(emitGCrFrameOffsTab, -1, cnt * sizeof(int));

        /* Now fill in all the actual used entries */

        for (num = 0, dsc = emitComp->lvaTable, cnt = emitComp->lvaCount; num < cnt; num++, dsc++)
        {
            if (!dsc->lvOnFrame || (dsc->lvIsParam && !dsc->lvIsRegArg))
            {
                continue;
            }

#if FEATURE_FIXED_OUT_ARGS
            if (num == emitComp->lvaOutgoingArgSpaceVar)
            {
                continue;
            }
#endif // FEATURE_FIXED_OUT_ARGS

            int offs = dsc->lvStkOffs;

            /* Is it within the interesting range of offsets */

            if (offs >= emitGCrFrameOffsMin && offs < emitGCrFrameOffsMax)
            {
                /* Are tracked stack ptr locals laid out contiguously?
                   If not, skip non-ptrs. The emitter is optimized to work
                   with contiguous ptrs, but for EditNContinue, the variables
                   are laid out in the order they occur in the local-sig.
                 */

                if (!emitContTrkPtrLcls)
                {
                    if (!emitComp->lvaIsGCTracked(dsc))
                    {
                        continue;
                    }
                }

                unsigned indx = dsc->lvVarIndex;

                assert(!dsc->lvRegister);
                assert(dsc->lvTracked);
                assert(dsc->lvRefCnt() != 0);

                assert(dsc->TypeGet() == TYP_REF || dsc->TypeGet() == TYP_BYREF);

                assert(indx < emitComp->lvaTrackedCount);

// printf("Variable #%2u/%2u is at stack offset %d\n", num, indx, offs);

#ifdef JIT32_GCENCODER
#ifndef WIN64EXCEPTIONS
                // Remember the frame offset of the "this" argument for synchronized methods.
                if (emitComp->lvaIsOriginalThisArg(num) && emitComp->lvaKeepAliveAndReportThis())
                {
                    emitSyncThisObjOffs = offs;
                    offs |= this_OFFSET_FLAG;
                }
#endif
#endif // JIT32_GCENCODER

                if (dsc->TypeGet() == TYP_BYREF)
                {
                    offs |= byref_OFFSET_FLAG;
                }
                tab[indx] = offs;
            }
        }
    }
    else
    {
#ifdef DEBUG
        emitTrkVarCnt       = 0;
        emitGCrFrameOffsTab = nullptr;
#endif
    }

#ifdef DEBUG
    if (emitComp->verbose)
    {
        printf("\n***************************************************************************\n");
        printf("Instructions as they come out of the scheduler\n\n");
    }
#endif

    /* Issue all instruction groups in order */
    cp = codeBlock;

#define DEFAULT_CODE_BUFFER_INIT 0xcc

    for (insGroup* ig = emitIGlist; ig != nullptr; ig = ig->igNext)
    {
        assert(!(ig->igFlags & IGF_PLACEHOLDER)); // There better not be any placeholder groups left

        /* Is this the first cold block? */
        if (ig == emitFirstColdIG)
        {
            assert(emitCurCodeOffs(cp) == emitTotalHotCodeSize);

            assert(coldCodeBlock);
            cp = coldCodeBlock;
#ifdef DEBUG
            if (emitComp->opts.disAsm || emitComp->opts.dspEmit || emitComp->verbose)
            {
                printf("\n************** Beginning of cold code **************\n");
            }
#endif
        }

        /* Are we overflowing? */
        if (ig->igNext && (ig->igNum + 1 != ig->igNext->igNum))
        {
            NO_WAY("Too many instruction groups");
        }

        // If this instruction group is returned to from a funclet implementing a finally,
        // on architectures where it is necessary generate GC info for the current instruction as
        // if it were the instruction following a call.
        emitGenGCInfoIfFuncletRetTarget(ig, cp);

        instrDesc* id = (instrDesc*)ig->igData;

#ifdef DEBUG

        /* Print the IG label, but only if it is a branch label */

        if (emitComp->opts.disAsm || emitComp->opts.dspEmit || emitComp->verbose)
        {
            if (emitComp->verbose)
            {
                printf("\n");
                emitDispIG(ig); // Display the flags, IG data, etc.
            }
            else
            {
                printf("\nG_M%03u_IG%02u:\n", Compiler::s_compMethodsCount, ig->igNum);
            }
        }

#endif // DEBUG

        BYTE* bp = cp;

        /* Record the actual offset of the block, noting the difference */

        emitOffsAdj = ig->igOffs - emitCurCodeOffs(cp);
        assert(emitOffsAdj >= 0);

#if DEBUG_EMIT
        if ((emitOffsAdj != 0) && emitComp->verbose)
        {
            printf("Block predicted offs = %08X, actual = %08X -> size adj = %d\n", ig->igOffs, emitCurCodeOffs(cp),
                   emitOffsAdj);
        }
#endif // DEBUG_EMIT

        ig->igOffs = emitCurCodeOffs(cp);
        assert(IsCodeAligned(ig->igOffs));

#if EMIT_TRACK_STACK_DEPTH

        /* Set the proper stack level if appropriate */

        if (ig->igStkLvl != emitCurStackLvl)
        {
            /* We are pushing stuff implicitly at this label */

            assert((unsigned)ig->igStkLvl > (unsigned)emitCurStackLvl);
            emitStackPushN(cp, (ig->igStkLvl - (unsigned)emitCurStackLvl) / sizeof(int));
        }

#endif

        /* Update current GC information for non-overflow IG (not added implicitly by the emitter) */

        if (!(ig->igFlags & IGF_EMIT_ADD))
        {
            /* Is there a new set of live GC ref variables? */

            if (ig->igFlags & IGF_GC_VARS)
            {
                emitUpdateLiveGCvars(ig->igGCvars(), cp);
            }
            else if (!emitThisGCrefVset)
            {
                emitUpdateLiveGCvars(emitThisGCrefVars, cp);
            }

            /* Update the set of live GC ref registers */

            {
                regMaskTP GCregs = ig->igGCregs;

                if (GCregs != emitThisGCrefRegs)
                {
                    emitUpdateLiveGCregs(GCT_GCREF, GCregs, cp);
                }
            }

            /* Is there a new set of live byref registers? */

            if (ig->igFlags & IGF_BYREF_REGS)
            {
                unsigned byrefRegs = ig->igByrefRegs();

                if (byrefRegs != emitThisByrefRegs)
                {
                    emitUpdateLiveGCregs(GCT_BYREF, byrefRegs, cp);
                }
            }
        }
        else
        {
            // These are not set for "overflow" groups
            assert(!(ig->igFlags & IGF_GC_VARS));
            assert(!(ig->igFlags & IGF_BYREF_REGS));
        }

        /* Issue each instruction in order */

        emitCurIG = ig;

        for (unsigned cnt = ig->igInsCnt; cnt; cnt--)
        {
            castto(id, BYTE*) += emitIssue1Instr(ig, id, &cp);
        }

        emitCurIG = nullptr;

        assert(ig->igSize >= cp - bp);

        // Is it the last ig in the hot part?
        bool lastHotIG = (emitFirstColdIG != nullptr && ig->igNext == emitFirstColdIG);
        if (lastHotIG)
        {
            unsigned actualHotCodeSize    = emitCurCodeOffs(cp);
            unsigned allocatedHotCodeSize = emitTotalHotCodeSize;
            assert(actualHotCodeSize <= allocatedHotCodeSize);
            if (actualHotCodeSize < allocatedHotCodeSize)
            {
                // The allocated chunk is bigger than used, fill in unused space in it.
                unsigned unusedSize = allocatedHotCodeSize - emitCurCodeOffs(cp);
                for (unsigned i = 0; i < unusedSize; ++i)
                {
                    *cp++ = DEFAULT_CODE_BUFFER_INIT;
                }
                assert(allocatedHotCodeSize == emitCurCodeOffs(cp));
            }
        }

        assert((ig->igSize >= cp - bp) || lastHotIG);
        ig->igSize = (unsigned short)(cp - bp);
    }

#if EMIT_TRACK_STACK_DEPTH
    assert(emitCurStackLvl == 0);
#endif

    /* Output any initialized data we may have */

    if (emitConsDsc.dsdOffs != 0)
    {
        emitOutputDataSec(&emitConsDsc, consBlock);
    }

    /* Make sure all GC ref variables are marked as dead */

    if (emitGCrFrameOffsCnt != 0)
    {
        unsigned    vn;
        int         of;
        varPtrDsc** dp;

        for (vn = 0, of = emitGCrFrameOffsMin, dp = emitGCrFrameLiveTab; vn < emitGCrFrameOffsCnt;
             vn++, of += TARGET_POINTER_SIZE, dp++)
        {
            if (*dp)
            {
                emitGCvarDeadSet(of, cp, vn);
            }
        }
    }

    /* No GC registers are live any more */

    if (emitThisByrefRegs)
    {
        emitUpdateLiveGCregs(GCT_BYREF, RBM_NONE, cp);
    }
    if (emitThisGCrefRegs)
    {
        emitUpdateLiveGCregs(GCT_GCREF, RBM_NONE, cp);
    }

    /* Patch any forward jumps */

    if (emitFwdJumps)
    {
        for (instrDescJmp* jmp = emitJumpList; jmp != nullptr; jmp = jmp->idjNext)
        {
#ifdef _TARGET_XARCH_
            assert(jmp->idInsFmt() == IF_LABEL || jmp->idInsFmt() == IF_RWR_LABEL || jmp->idInsFmt() == IF_SWR_LABEL);
#endif
            insGroup* tgt = jmp->idAddr()->iiaIGlabel;

            if (jmp->idjTemp.idjAddr == nullptr)
            {
                continue;
            }

            if (jmp->idjOffs != tgt->igOffs)
            {
                BYTE* adr = jmp->idjTemp.idjAddr;
                int   adj = jmp->idjOffs - tgt->igOffs;
#ifdef _TARGET_ARM_
                // On Arm, the offset is encoded in unit of 2 bytes.
                adj >>= 1;
#endif

#if DEBUG_EMIT
                if ((jmp->idDebugOnlyInfo()->idNum == (unsigned)INTERESTING_JUMP_NUM) || (INTERESTING_JUMP_NUM == 0))
                {
#ifdef _TARGET_ARM_
                    printf("[5] This output is broken for ARM, since it doesn't properly decode the jump offsets of "
                           "the instruction at adr\n");
#endif

                    if (INTERESTING_JUMP_NUM == 0)
                    {
                        printf("[5] Jump %u:\n", jmp->idDebugOnlyInfo()->idNum);
                    }

                    if (jmp->idjShort)
                    {
                        printf("[5] Jump        is at %08X\n", (adr + 1 - emitCodeBlock));
                        printf("[5] Jump distance is  %02X - %02X = %02X\n", *(BYTE*)adr, adj, *(BYTE*)adr - adj);
                    }
                    else
                    {
                        printf("[5] Jump        is at %08X\n", (adr + 4 - emitCodeBlock));
                        printf("[5] Jump distance is  %08X - %02X = %08X\n", *(int*)adr, adj, *(int*)adr - adj);
                    }
                }
#endif // DEBUG_EMIT

                if (jmp->idjShort)
                {
                    // Patch Forward Short Jump
                    CLANG_FORMAT_COMMENT_ANCHOR;
#if defined(_TARGET_XARCH_)
                    *(BYTE*)adr -= (BYTE)adj;
#elif defined(_TARGET_ARM_)
                    // The following works because the jump offset is in the low order bits of the instruction.
                    // Presumably we could also just call "emitOutputLJ(NULL, adr, jmp)", like for long jumps?
                    *(short int*)adr -= (short)adj;
#elif defined(_TARGET_ARM64_)
                    assert(!jmp->idAddr()->iiaHasInstrCount());
                    emitOutputLJ(NULL, adr, jmp);
#else
#error Unsupported or unset target architecture
#endif
                }
                else
                {
                    // Patch Forward non-Short Jump
                    CLANG_FORMAT_COMMENT_ANCHOR;
#if defined(_TARGET_XARCH_)
                    *(int*)adr -= adj;
#elif defined(_TARGET_ARMARCH_)
                    assert(!jmp->idAddr()->iiaHasInstrCount());
                    emitOutputLJ(NULL, adr, jmp);
#else
#error Unsupported or unset target architecture
#endif
                }
            }
        }
    }

#ifdef DEBUG
    if (emitComp->opts.disAsm)
    {
        printf("\n");
    }

    if (emitComp->verbose)
    {
        printf("Allocated method code size = %4u , actual size = %4u\n", emitTotalCodeSize, cp - codeBlock);
    }
#endif

    unsigned actualCodeSize = emitCurCodeOffs(cp);

#if EMITTER_STATS
    totAllocdSize += emitTotalCodeSize;
    totActualSize += actualCodeSize;
#endif

    // Fill in eventual unused space, but do not report this space as used.
    // If you add this padding during the emitIGlist loop, then it will
    // emit offsets after the loop with wrong value (for example for GC ref variables).
    unsigned unusedSize = emitTotalCodeSize - emitCurCodeOffs(cp);
    for (unsigned i = 0; i < unusedSize; ++i)
    {
        *cp++ = DEFAULT_CODE_BUFFER_INIT;
    }
    assert(emitTotalCodeSize == emitCurCodeOffs(cp));

    // Total code size is sum of all IG->size and doesn't include padding in the last IG.
    emitTotalCodeSize = actualCodeSize;

#ifdef DEBUG

    // Make sure these didn't change during the "issuing" phase

    assert(VarSetOps::MayBeUninit(emitPrevGCrefVars));
    assert(emitPrevGCrefRegs == 0xBAADFEED);
    assert(emitPrevByrefRegs == 0xBAADFEED);

    assert(VarSetOps::MayBeUninit(emitInitGCrefVars));
    assert(emitInitGCrefRegs == 0xBAADFEED);
    assert(emitInitByrefRegs == 0xBAADFEED);

    if (EMIT_INSTLIST_VERBOSE)
    {
        printf("\nLabels list after the end of codegen:\n\n");
        emitDispIGlist(false);
    }

    emitCheckIGoffsets();

#endif // DEBUG

    // Assign the real prolog size
    *prologSize = emitCodeOffset(emitPrologIG, emitPrologEndPos);

    /* Return the amount of code we've generated */

    return actualCodeSize;
}

// See specification comment at the declaration.
void emitter::emitGenGCInfoIfFuncletRetTarget(insGroup* ig, BYTE* cp)
{
#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    // We only emit this GC information on targets where finally's are implemented via funclets,
    // and the finally is invoked, during non-exceptional execution, via a branch with a predefined
    // link register, rather than a "true call" for which we would already generate GC info.  Currently,
    // this means precisely ARM.
    if (ig->igFlags & IGF_FINALLY_TARGET)
    {
        // We don't actually have a call instruction in this case, so we don't have
        // a real size for that instruction.  We'll use 1.
        emitStackPop(cp, /*isCall*/ true, /*callInstrSize*/ 1, /*args*/ 0);

        /* Do we need to record a call location for GC purposes? */
        if (!emitFullGCinfo)
        {
            emitRecordGCcall(cp, /*callInstrSize*/ 1);
        }
    }
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
}

/*****************************************************************************
 *
 *  We have an instruction in an insGroup and we need to know the
 *  instruction number for this instruction
 */

unsigned emitter::emitFindInsNum(insGroup* ig, instrDesc* idMatch)
{
    instrDesc* id = (instrDesc*)ig->igData;

    // Check if we are the first instruction in the group
    if (id == idMatch)
    {
        return 0;
    }

    /* Walk the list of instructions until we find a match */
    unsigned insNum       = 0;
    unsigned insRemaining = ig->igInsCnt;

    while (insRemaining > 0)
    {
        castto(id, BYTE*) += emitSizeOfInsDsc(id);
        insNum++;
        insRemaining--;

        if (id == idMatch)
        {
            return insNum;
        }
    }
    assert(!"emitFindInsNum failed");
    return -1;
}

/*****************************************************************************
 *
 *  We've been asked for the code offset of an instruction but alas one or
 *  more instruction sizes in the block have been mis-predicted, so we have
 *  to find the true offset by looking for the instruction within the group.
 */

UNATIVE_OFFSET emitter::emitFindOffset(insGroup* ig, unsigned insNum)
{
    instrDesc*     id = (instrDesc*)ig->igData;
    UNATIVE_OFFSET of = 0;

#ifdef DEBUG
    /* Make sure we were passed reasonable arguments */
    assert(ig && ig->igSelf == ig);
    assert(ig->igInsCnt >= insNum);
#endif

    /* Walk the instruction list until all are counted */

    while (insNum > 0)
    {
        of += emitInstCodeSz(id);

        castto(id, BYTE*) += emitSizeOfInsDsc(id);

        insNum--;
    }

    return of;
}

/*****************************************************************************
 *
 *  Start generating a constant data section for the current
 *  function. Returns the offset of the section in the appropriate data
 *  block.
 */

UNATIVE_OFFSET emitter::emitDataGenBeg(UNATIVE_OFFSET size, bool align)
{
    unsigned     secOffs;
    dataSection* secDesc;

    assert(emitDataSecCur == nullptr);

    /* The size better not be some kind of an odd thing */

    assert(size && size % sizeof(int) == 0);

    /* Get hold of the current offset */

    secOffs = emitConsDsc.dsdOffs;

    if (align)
    {
        // Data can have any size but since alignment is deduced from the size there's no
        // way to have a larger data size (e.g. 128) and request 4/8/16 byte alignment.
        // 32 bytes (and more) alignment requires VM support (see ICorJitInfo::allocMem).
        assert(size <= 16);

        if (size == 16)
        {
            emitConsDsc.align16 = true;
        }

        while ((secOffs % size) != 0)
        {
            /* Need to skip 4 bytes to honor alignment */
            /* Must allocate a dummy 4 byte integer */
            int zero = 0;
            emitDataGenBeg(4, false);
            emitDataGenData(0, &zero, 4);
            emitDataGenEnd();

            /* Get the new secOffs */
            secOffs = emitConsDsc.dsdOffs;
        }
    }

    /* Advance the current offset */

    emitConsDsc.dsdOffs += size;

    /* Allocate a data section descriptor and add it to the list */

    secDesc = emitDataSecCur = (dataSection*)emitGetMem(roundUp(sizeof(*secDesc) + size));

    secDesc->dsSize = size;

    secDesc->dsType = dataSection::data;

    secDesc->dsNext = nullptr;

    if (emitConsDsc.dsdLast)
    {
        emitConsDsc.dsdLast->dsNext = secDesc;
    }
    else
    {
        emitConsDsc.dsdList = secDesc;
    }
    emitConsDsc.dsdLast = secDesc;

    return secOffs;
}

//  Start generating a constant data section for the current function
//  populated with BasicBlock references.
//  You can choose the references to be either absolute pointers, or
//  4-byte relative addresses.
//  Currently the relative references are relative to the start of the
//  first block (this is somewhat arbitrary)

UNATIVE_OFFSET emitter::emitBBTableDataGenBeg(unsigned numEntries, bool relativeAddr)
{
    unsigned     secOffs;
    dataSection* secDesc;

    assert(emitDataSecCur == nullptr);

    UNATIVE_OFFSET emittedSize;

    if (relativeAddr)
    {
        emittedSize = numEntries * 4;
    }
    else
    {
        emittedSize = numEntries * TARGET_POINTER_SIZE;
    }

    /* Get hold of the current offset */

    secOffs = emitConsDsc.dsdOffs;

    /* Advance the current offset */

    emitConsDsc.dsdOffs += emittedSize;

    /* Allocate a data section descriptor and add it to the list */

    secDesc = emitDataSecCur = (dataSection*)emitGetMem(roundUp(sizeof(*secDesc) + numEntries * sizeof(BasicBlock*)));

    secDesc->dsSize = emittedSize;

    secDesc->dsType = relativeAddr ? dataSection::blockRelative32 : dataSection::blockAbsoluteAddr;

    secDesc->dsNext = nullptr;

    if (emitConsDsc.dsdLast)
    {
        emitConsDsc.dsdLast->dsNext = secDesc;
    }
    else
    {
        emitConsDsc.dsdList = secDesc;
    }

    emitConsDsc.dsdLast = secDesc;

    return secOffs;
}

/*****************************************************************************
 *
 *  Emit the given block of bits into the current data section.
 */

void emitter::emitDataGenData(unsigned offs, const void* data, size_t size)
{
    assert(emitDataSecCur && (emitDataSecCur->dsSize >= offs + size));

    assert(emitDataSecCur->dsType == dataSection::data);

    memcpy(emitDataSecCur->dsCont + offs, data, size);
}

/*****************************************************************************
 *
 *  Emit the address of the given basic block into the current data section.
 */

void emitter::emitDataGenData(unsigned index, BasicBlock* label)
{
    assert(emitDataSecCur != nullptr);
    assert(emitDataSecCur->dsType == dataSection::blockAbsoluteAddr ||
           emitDataSecCur->dsType == dataSection::blockRelative32);

    unsigned emittedElemSize = emitDataSecCur->dsType == dataSection::blockAbsoluteAddr ? TARGET_POINTER_SIZE : 4;

    assert(emitDataSecCur->dsSize >= emittedElemSize * (index + 1));

    ((BasicBlock**)(emitDataSecCur->dsCont))[index] = label;
}

/*****************************************************************************
 *
 *  We're done generating a data section.
 */

void emitter::emitDataGenEnd()
{

#ifdef DEBUG
    assert(emitDataSecCur);
    emitDataSecCur = nullptr;
#endif
}

/********************************************************************************
 * Generates a data section constant
 *
 * Parameters:
 *     cnsAddr  - memory location containing constant value
 *     cnsSize  - size of constant in bytes
 *     dblAlign - whether to double align the data section constant
 *
 * Returns constant number as offset into data section.
 */
UNATIVE_OFFSET emitter::emitDataConst(const void* cnsAddr, unsigned cnsSize, bool dblAlign)
{
    // When generating SMALL_CODE, we don't bother with dblAlign
    if (dblAlign && (emitComp->compCodeOpt() == Compiler::SMALL_CODE))
    {
        dblAlign = false;
    }

    UNATIVE_OFFSET cnum = emitDataGenBeg(cnsSize, dblAlign);
    emitDataGenData(0, cnsAddr, cnsSize);
    emitDataGenEnd();

    return cnum;
}

//------------------------------------------------------------------------
// emitAnyConst: Create a data section constant of arbitrary size.
//
// Arguments:
//    cnsAddr   - pointer to the data to be placed in the data section
//    cnsSize   - size of the data
//    alignment - indicates how to align the constant
//
// Return Value:
//    A field handle representing the data offset to access the constant.
//
CORINFO_FIELD_HANDLE emitter::emitAnyConst(const void* cnsAddr, unsigned cnsSize, emitDataAlignment alignment)
{
    bool align;

    switch (alignment)
    {
        case emitDataAlignment::None:
            align = false;
            break;
        case emitDataAlignment::Preferred:
            align = (emitComp->compCodeOpt() != Compiler::SMALL_CODE);
            break;
        case emitDataAlignment::Required:
        default:
            align = true;
            break;
    }

    UNATIVE_OFFSET cnum = emitDataGenBeg(cnsSize, align);
    emitDataGenData(0, cnsAddr, cnsSize);
    emitDataGenEnd();
    return emitComp->eeFindJitDataOffs(cnum);
}

//------------------------------------------------------------------------
// emitFltOrDblConst: Create a float or double data section constant.
//
// Arguments:
//    constValue - constant value
//    attr       - constant size
//
// Return Value:
//    A field handle representing the data offset to access the constant.
//
// Notes:
//    If attr is EA_4BYTE then the double value is converted to a float value.
//    If attr is EA_8BYTE then 8 byte alignment is automatically requested.
//
CORINFO_FIELD_HANDLE emitter::emitFltOrDblConst(double constValue, emitAttr attr)
{
    assert((attr == EA_4BYTE) || (attr == EA_8BYTE));

    void* cnsAddr;
    float f;
    bool  dblAlign;

    if (attr == EA_4BYTE)
    {
        f        = forceCastToFloat(constValue);
        cnsAddr  = &f;
        dblAlign = false;
    }
    else
    {
        cnsAddr  = &constValue;
        dblAlign = true;
    }

    // Access to inline data is 'abstracted' by a special type of static member
    // (produced by eeFindJitDataOffs) which the emitter recognizes as being a reference
    // to constant data, not a real static field.

    UNATIVE_OFFSET cnsSize = (attr == EA_4BYTE) ? 4 : 8;
    UNATIVE_OFFSET cnum    = emitDataConst(cnsAddr, cnsSize, dblAlign);
    return emitComp->eeFindJitDataOffs(cnum);
}

/*****************************************************************************
 *
 *  Output the given data section at the specified address.
 */

void emitter::emitOutputDataSec(dataSecDsc* sec, BYTE* dst)
{
#ifdef DEBUG
    if (EMITVERBOSE)
    {
        printf("\nEmitting data sections: %u total bytes\n", sec->dsdOffs);
    }

    if (emitComp->opts.disAsm)
    {
        emitDispDataSec(sec);
    }

    unsigned secNum = 0;
#endif

    assert(dst);
    assert(sec->dsdOffs);
    assert(sec->dsdList);

    /* Walk and emit the contents of all the data blocks */

    dataSection* dsc;

    for (dsc = sec->dsdList; dsc; dsc = dsc->dsNext)
    {
        size_t dscSize = dsc->dsSize;

        // absolute label table
        if (dsc->dsType == dataSection::blockAbsoluteAddr)
        {
            JITDUMP("  section %u, size %u, block absolute addr\n", secNum++, dscSize);

            assert(dscSize && dscSize % TARGET_POINTER_SIZE == 0);
            size_t         numElems = dscSize / TARGET_POINTER_SIZE;
            target_size_t* bDst     = (target_size_t*)dst;
            for (unsigned i = 0; i < numElems; i++)
            {
                BasicBlock* block = ((BasicBlock**)dsc->dsCont)[i];

                // Convert the BasicBlock* value to an IG address
                insGroup* lab = (insGroup*)emitCodeGetCookie(block);

                // Append the appropriate address to the destination
                BYTE* target = emitOffsetToPtr(lab->igOffs);

#ifdef _TARGET_ARM_
                target = (BYTE*)((size_t)target | 1); // Or in thumb bit
#endif
                bDst[i] = (target_size_t)target;
                if (emitComp->opts.compReloc)
                {
                    emitRecordRelocation(&(bDst[i]), target, IMAGE_REL_BASED_HIGHLOW);
                }

                JITDUMP("  " FMT_BB ": 0x%p\n", block->bbNum, bDst[i]);
            }
        }
        // relative label table
        else if (dsc->dsType == dataSection::blockRelative32)
        {
            JITDUMP("  section %u, size %u, block relative addr\n", secNum++, dscSize);

            unsigned  elemSize = 4;
            size_t    numElems = dscSize / 4;
            unsigned* uDst     = (unsigned*)dst;
            insGroup* labFirst = (insGroup*)emitCodeGetCookie(emitComp->fgFirstBB);

            for (unsigned i = 0; i < numElems; i++)
            {
                BasicBlock* block = ((BasicBlock**)dsc->dsCont)[i];

                // Convert the BasicBlock* value to an IG address
                insGroup* lab = (insGroup*)emitCodeGetCookie(block);

                assert(FitsIn<uint32_t>(lab->igOffs - labFirst->igOffs));
                uDst[i] = lab->igOffs - labFirst->igOffs;

                JITDUMP("  " FMT_BB ": 0x%x\n", block->bbNum, uDst[i]);
            }
        }
        else
        {
            JITDUMP("  section %u, size %u, raw data\n", secNum++, dscSize);

            // Simple binary data: copy the bytes to the target
            assert(dsc->dsType == dataSection::data);

            memcpy(dst, dsc->dsCont, dscSize);

#ifdef DEBUG
            if (EMITVERBOSE)
            {
                printf("  ");
                for (size_t i = 0; i < dscSize; i++)
                {
                    printf("%02x ", dsc->dsCont[i]);
                    if ((((i + 1) % 16) == 0) && (i + 1 != dscSize))
                    {
                        printf("\n  ");
                    }
                }
                printf("\n");
            }
#endif // DEBUG
        }
        dst += dscSize;
    }
}

#ifdef DEBUG

//------------------------------------------------------------------------
// emitDispDataSec: Dump a data section to stdout.
//
// Arguments:
//    section - the data section description
//
// Notes:
//    The output format attempts to mirror typical assembler syntax.
//    Data section entries lack type information so float/double entries
//    are displayed as if they are integers/longs.
//
void emitter::emitDispDataSec(dataSecDsc* section)
{
    printf("\n");

    unsigned offset = 0;

    for (dataSection* data = section->dsdList; data != nullptr; data = data->dsNext)
    {
        const char* labelFormat = "%-7s";
        char        label[64];
        sprintf_s(label, _countof(label), "RWD%02u", offset);
        printf(labelFormat, label);
        offset += data->dsSize;

        if ((data->dsType == dataSection::blockRelative32) || (data->dsType == dataSection::blockAbsoluteAddr))
        {
            insGroup* igFirst    = static_cast<insGroup*>(emitCodeGetCookie(emitComp->fgFirstBB));
            bool      isRelative = (data->dsType == dataSection::blockRelative32);
            size_t    blockCount = data->dsSize / (isRelative ? 4 : TARGET_POINTER_SIZE);

            for (unsigned i = 0; i < blockCount; i++)
            {
                if (i > 0)
                {
                    printf(labelFormat, "");
                }

                BasicBlock* block = reinterpret_cast<BasicBlock**>(data->dsCont)[i];
                insGroup*   ig    = static_cast<insGroup*>(emitCodeGetCookie(block));

                const char* blockLabelFormat = "G_M%03u_IG%02u";
                char        blockLabel[64];
                char        firstLabel[64];
                sprintf_s(blockLabel, _countof(blockLabel), blockLabelFormat, Compiler::s_compMethodsCount, ig->igNum);
                sprintf_s(firstLabel, _countof(firstLabel), blockLabelFormat, Compiler::s_compMethodsCount,
                          igFirst->igNum);

                if (isRelative)
                {
                    if (emitComp->opts.disDiffable)
                    {
                        printf("dd\t%s - %s\n", blockLabel, firstLabel);
                    }
                    else
                    {
                        printf("dd\t%08Xh", ig->igOffs - igFirst->igOffs);
                    }
                }
                else
                {
#ifndef _TARGET_64BIT_
                    // We have a 32-BIT target
                    if (emitComp->opts.disDiffable)
                    {
                        printf("dd\t%s\n", blockLabel);
                    }
                    else
                    {
                        printf("dd\t%08Xh", reinterpret_cast<uint32_t>(emitOffsetToPtr(ig->igOffs)));
                    }
#else  // _TARGET_64BIT_
                    // We have a 64-BIT target
                    if (emitComp->opts.disDiffable)
                    {
                        printf("dq\t%s\n", blockLabel);
                    }
                    else
                    {
                        printf("dq\t%016llXh", reinterpret_cast<uint64_t>(emitOffsetToPtr(ig->igOffs)));
                    }
#endif // _TARGET_64BIT_
                }

                if (!emitComp->opts.disDiffable)
                {
                    printf(" ; case %s\n", blockLabel);
                }
            }
        }
        else
        {
            assert(data->dsType == dataSection::data);
            switch (data->dsSize)
            {
                case 2:
                    printf("dw\t%04Xh\n", *reinterpret_cast<uint16_t*>(&data->dsCont));
                    break;
                case 4:
                    printf("dd\t%08Xh\n", *reinterpret_cast<uint32_t*>(&data->dsCont));
                    break;
                case 8:
                    printf("dq\t%016llXh\n", *reinterpret_cast<uint64_t*>(&data->dsCont));
                    break;
                default:
                    printf("db\t");
                    for (UNATIVE_OFFSET i = 0; i < data->dsSize; i++)
                    {
                        printf("%s0%02Xh", i > 0 ? ", " : "", data->dsCont[i]);
                    }
                    printf("\n");
            }
        }
    }
}
#endif

/*****************************************************************************/
/*****************************************************************************
 *
 *  Record the fact that the given variable now contains a live GC ref.
 */

void emitter::emitGCvarLiveSet(int offs, GCtype gcType, BYTE* addr, ssize_t disp)
{
    assert(emitIssuing);

    varPtrDsc* desc;

    assert((abs(offs) % TARGET_POINTER_SIZE) == 0);
    assert(needsGC(gcType));

    /* Compute the index into the GC frame table if the caller didn't do it */

    if (disp == -1)
    {
        disp = (offs - emitGCrFrameOffsMin) / TARGET_POINTER_SIZE;
    }

    assert((size_t)disp < emitGCrFrameOffsCnt);

    /* Allocate a lifetime record */

    desc = new (emitComp, CMK_GC) varPtrDsc;

    desc->vpdBegOfs = emitCurCodeOffs(addr);
#ifdef DEBUG
    desc->vpdEndOfs = 0xFACEDEAD;
#endif

    desc->vpdVarNum = offs;

    desc->vpdNext = nullptr;

#if !defined(JIT32_GCENCODER) || !defined(WIN64EXCEPTIONS)
    /* the lower 2 bits encode props about the stk ptr */

    if (offs == emitSyncThisObjOffs)
    {
        desc->vpdVarNum |= this_OFFSET_FLAG;
    }
#endif

    if (gcType == GCT_BYREF)
    {
        desc->vpdVarNum |= byref_OFFSET_FLAG;
    }

    /* Append the new entry to the end of the list */
    if (codeGen->gcInfo.gcVarPtrLast == nullptr)
    {
        assert(codeGen->gcInfo.gcVarPtrList == nullptr);
        codeGen->gcInfo.gcVarPtrList = codeGen->gcInfo.gcVarPtrLast = desc;
    }
    else
    {
        assert(codeGen->gcInfo.gcVarPtrList != nullptr);
        codeGen->gcInfo.gcVarPtrLast->vpdNext = desc;
        codeGen->gcInfo.gcVarPtrLast          = desc;
    }

    /* Record the variable descriptor in the table */

    assert(emitGCrFrameLiveTab[disp] == nullptr);
    emitGCrFrameLiveTab[disp] = desc;

#ifdef DEBUG
    if (EMITVERBOSE)
    {
        printf("[%08X] %s var born at [%s", dspPtr(desc), GCtypeStr(gcType), emitGetFrameReg());

        if (offs < 0)
        {
            printf("-%02XH", -offs);
        }
        else if (offs > 0)
        {
            printf("+%02XH", +offs);
        }

        printf("]\n");
    }
#endif

    /* The "global" live GC variable mask is no longer up-to-date */

    emitThisGCrefVset = false;
}

/*****************************************************************************
 *
 *  Record the fact that the given variable no longer contains a live GC ref.
 */

void emitter::emitGCvarDeadSet(int offs, BYTE* addr, ssize_t disp)
{
    assert(emitIssuing);

    varPtrDsc* desc;

    assert(abs(offs) % sizeof(int) == 0);

    /* Compute the index into the GC frame table if the caller didn't do it */

    if (disp == -1)
    {
        disp = (offs - emitGCrFrameOffsMin) / TARGET_POINTER_SIZE;
    }

    assert((unsigned)disp < emitGCrFrameOffsCnt);

    /* Get hold of the lifetime descriptor and clear the entry */

    desc                      = emitGCrFrameLiveTab[disp];
    emitGCrFrameLiveTab[disp] = nullptr;

    assert(desc);
    assert((desc->vpdVarNum & ~OFFSET_MASK) == (unsigned)offs);

    /* Record the death code offset */

    assert(desc->vpdEndOfs == 0xFACEDEAD);
    desc->vpdEndOfs = emitCurCodeOffs(addr);

#ifdef DEBUG
    if (EMITVERBOSE)
    {
        GCtype gcType = (desc->vpdVarNum & byref_OFFSET_FLAG) ? GCT_BYREF : GCT_GCREF;
#if !defined(JIT32_GCENCODER) || !defined(WIN64EXCEPTIONS)
        bool isThis = (desc->vpdVarNum & this_OFFSET_FLAG) != 0;

        printf("[%08X] %s%s var died at [%s", dspPtr(desc), GCtypeStr(gcType), isThis ? "this-ptr" : "",
               emitGetFrameReg());
#else
        bool isPinned = (desc->vpdVarNum & pinned_OFFSET_FLAG) != 0;

        printf("[%08X] %s%s var died at [%s", dspPtr(desc), GCtypeStr(gcType), isPinned ? "pinned" : "",
               emitGetFrameReg());
#endif

        if (offs < 0)
        {
            printf("-%02XH", -offs);
        }
        else if (offs > 0)
        {
            printf("+%02XH", +offs);
        }

        printf("]\n");
    }
#endif

    /* The "global" live GC variable mask is no longer up-to-date */

    emitThisGCrefVset = false;
}

/*****************************************************************************
 *
 *  Record a new set of live GC ref variables.
 */

void emitter::emitUpdateLiveGCvars(VARSET_VALARG_TP vars, BYTE* addr)
{
    assert(emitIssuing);

    // Don't track GC changes in epilogs
    if (emitIGisInEpilog(emitCurIG))
    {
        return;
    }

    /* Is the current set accurate and unchanged? */

    if (emitThisGCrefVset && VarSetOps::Equal(emitComp, emitThisGCrefVars, vars))
    {
        return;
    }

#ifdef DEBUG
    if (EMIT_GC_VERBOSE)
    {
        printf("New GC ref live vars=%s ", VarSetOps::ToString(emitComp, vars));
        dumpConvertedVarSet(emitComp, vars);
        printf("\n");
    }
#endif

    VarSetOps::Assign(emitComp, emitThisGCrefVars, vars);

    /* Are there any GC ref variables on the stack? */

    if (emitGCrFrameOffsCnt)
    {
        int*     tab;
        unsigned cnt = emitTrkVarCnt;
        unsigned num;

        /* Test all the tracked variable bits in the mask */

        for (num = 0, tab = emitGCrFrameOffsTab; num < cnt; num++, tab++)
        {
            int val = *tab;

            if (val != -1)
            {
                // byref_OFFSET_FLAG and this_OFFSET_FLAG are set
                //  in the table-offsets for byrefs and this-ptr

                int offs = val & ~OFFSET_MASK;

                // printf("var #%2u at %3d is now %s\n", num, offs, (vars & 1) ? "live" : "dead");

                if (VarSetOps::IsMember(emitComp, vars, num))
                {
                    GCtype gcType = (val & byref_OFFSET_FLAG) ? GCT_BYREF : GCT_GCREF;
                    emitGCvarLiveUpd(offs, INT_MAX, gcType, addr);
                }
                else
                {
                    emitGCvarDeadUpd(offs, addr);
                }
            }
        }
    }

    emitThisGCrefVset = true;
}

/*****************************************************************************
 *
 *  Record a call location for GC purposes (we know that this is a method that
 *  will not be fully interruptible).
 */

void emitter::emitRecordGCcall(BYTE* codePos, unsigned char callInstrSize)
{
    assert(emitIssuing);
    assert(!emitFullGCinfo);

    unsigned offs = emitCurCodeOffs(codePos);
    unsigned regs = (emitThisGCrefRegs | emitThisByrefRegs) & ~RBM_INTRET;
    callDsc* call;

#ifdef JIT32_GCENCODER
    // The JIT32 GCInfo encoder allows us to (as the comment previously here said):
    // "Bail if this is a totally boring call", but the GCInfoEncoder/Decoder interface
    // requires a definition for every call site, so we skip these "early outs" when we're
    // using the general encoder.
    if (regs == 0)
    {
#if EMIT_TRACK_STACK_DEPTH
        if (emitCurStackLvl == 0)
            return;
#endif
        /* Nope, only interesting calls get recorded */

        if (emitSimpleStkUsed)
        {
            if (!u1.emitSimpleStkMask)
                return;
        }
        else
        {
            if (u2.emitGcArgTrackCnt == 0)
                return;
        }
    }
#endif // JIT32_GCENCODER

#ifdef DEBUG

    if (EMIT_GC_VERBOSE)
    {
        printf("; Call at %04X [stk=%u], GCvars=", offs - callInstrSize, emitCurStackLvl);
        emitDispVarSet();
        printf(", gcrefRegs=");
        printRegMaskInt(emitThisGCrefRegs);
        emitDispRegSet(emitThisGCrefRegs);
        // printRegMaskInt(emitThisGCrefRegs & ~RBM_INTRET & RBM_CALLEE_SAVED);    // only display callee-saved
        // emitDispRegSet (emitThisGCrefRegs & ~RBM_INTRET & RBM_CALLEE_SAVED);    // only display callee-saved
        printf(", byrefRegs=");
        printRegMaskInt(emitThisByrefRegs);
        emitDispRegSet(emitThisByrefRegs);
        // printRegMaskInt(emitThisByrefRegs & ~RBM_INTRET & RBM_CALLEE_SAVED);    // only display callee-saved
        // emitDispRegSet (emitThisByrefRegs & ~RBM_INTRET & RBM_CALLEE_SAVED);    // only display callee-saved
        printf("\n");
    }

#endif

    /* Allocate a 'call site' descriptor and start filling it in */

    call = new (emitComp, CMK_GC) callDsc;

    call->cdBlock = nullptr;
    call->cdOffs  = offs;
#ifndef JIT32_GCENCODER
    call->cdCallInstrSize = callInstrSize;
#endif
    call->cdNext = nullptr;

    call->cdGCrefRegs = (regMaskSmall)emitThisGCrefRegs;
    call->cdByrefRegs = (regMaskSmall)emitThisByrefRegs;

#if EMIT_TRACK_STACK_DEPTH
#ifndef UNIX_AMD64_ABI
    noway_assert(FitsIn<USHORT>(emitCurStackLvl / ((unsigned)sizeof(unsigned))));
#endif // UNIX_AMD64_ABI
#endif

    // Append the call descriptor to the list */
    if (codeGen->gcInfo.gcCallDescLast == nullptr)
    {
        assert(codeGen->gcInfo.gcCallDescList == nullptr);
        codeGen->gcInfo.gcCallDescList = codeGen->gcInfo.gcCallDescLast = call;
    }
    else
    {
        assert(codeGen->gcInfo.gcCallDescList != nullptr);
        codeGen->gcInfo.gcCallDescLast->cdNext = call;
        codeGen->gcInfo.gcCallDescLast         = call;
    }

    /* Record the current "pending" argument list */

    if (emitSimpleStkUsed)
    {
        /* The biggest call is less than MAX_SIMPLE_STK_DEPTH. So use
           small format */

        call->u1.cdArgMask      = u1.emitSimpleStkMask;
        call->u1.cdByrefArgMask = u1.emitSimpleByrefStkMask;
        call->cdArgCnt          = 0;
    }
    else
    {
        /* The current call has too many arguments, so we need to report the
           offsets of each individual GC arg. */

        call->cdArgCnt = u2.emitGcArgTrackCnt;
        if (call->cdArgCnt == 0)
        {
            call->u1.cdArgMask = call->u1.cdByrefArgMask = 0;
            return;
        }

        call->cdArgTable = new (emitComp, CMK_GC) unsigned[u2.emitGcArgTrackCnt];

        unsigned gcArgs = 0;
        unsigned stkLvl = emitCurStackLvl / sizeof(int);

        for (unsigned i = 0; i < stkLvl; i++)
        {
            GCtype gcType = (GCtype)u2.emitArgTrackTab[stkLvl - i - 1];

            if (needsGC(gcType))
            {
                call->cdArgTable[gcArgs] = i * TARGET_POINTER_SIZE;

                if (gcType == GCT_BYREF)
                {
                    call->cdArgTable[gcArgs] |= byref_OFFSET_FLAG;
                }

                gcArgs++;
            }
        }

        assert(gcArgs == u2.emitGcArgTrackCnt);
    }
}

/*****************************************************************************
 *
 *  Record a new set of live GC ref registers.
 */

void emitter::emitUpdateLiveGCregs(GCtype gcType, regMaskTP regs, BYTE* addr)
{
    assert(emitIssuing);

    // Don't track GC changes in epilogs
    if (emitIGisInEpilog(emitCurIG))
    {
        return;
    }

    regMaskTP life;
    regMaskTP dead;
    regMaskTP chg;

#ifdef DEBUG
    if (EMIT_GC_VERBOSE)
    {
        printf("New %sReg live regs=", GCtypeStr(gcType));
        printRegMaskInt(regs);
        emitDispRegSet(regs);
        printf("\n");
    }
#endif

    assert(needsGC(gcType));

    regMaskTP& emitThisXXrefRegs = (gcType == GCT_GCREF) ? emitThisGCrefRegs : emitThisByrefRegs;
    regMaskTP& emitThisYYrefRegs = (gcType == GCT_GCREF) ? emitThisByrefRegs : emitThisGCrefRegs;
    assert(emitThisXXrefRegs != regs);

    if (emitFullGCinfo)
    {
        /* Figure out which GC registers are becoming live/dead at this point */

        dead = (emitThisXXrefRegs & ~regs);
        life = (~emitThisXXrefRegs & regs);

        /* Can't simultaneously become live and dead at the same time */

        assert((dead | life) != 0);
        assert((dead & life) == 0);

        /* Compute the 'changing state' mask */

        chg = (dead | life);

        do
        {
            regMaskTP bit = genFindLowestBit(chg);
            regNumber reg = genRegNumFromMask(bit);

            if (life & bit)
            {
                emitGCregLiveUpd(gcType, reg, addr);
            }
            else
            {
                emitGCregDeadUpd(reg, addr);
            }

            chg -= bit;
        } while (chg);

        assert(emitThisXXrefRegs == regs);
    }
    else
    {
        emitThisYYrefRegs &= ~regs; // Kill the regs from the other GC type (if live)
        emitThisXXrefRegs = regs;   // Mark them as live in the requested GC type
    }

    // The 2 GC reg masks can't be overlapping

    assert((emitThisGCrefRegs & emitThisByrefRegs) == 0);
}

/*****************************************************************************
 *
 *  Record the fact that the given register now contains a live GC ref.
 */

void emitter::emitGCregLiveSet(GCtype gcType, regMaskTP regMask, BYTE* addr, bool isThis)
{
    assert(emitIssuing);
    assert(needsGC(gcType));

    regPtrDsc* regPtrNext;

    assert(!isThis || emitComp->lvaKeepAliveAndReportThis());
    // assert(emitFullyInt || isThis);
    assert(emitFullGCinfo);

    assert(((emitThisGCrefRegs | emitThisByrefRegs) & regMask) == 0);

    /* Allocate a new regptr entry and fill it in */

    regPtrNext            = codeGen->gcInfo.gcRegPtrAllocDsc();
    regPtrNext->rpdGCtype = gcType;

    regPtrNext->rpdOffs            = emitCurCodeOffs(addr);
    regPtrNext->rpdArg             = FALSE;
    regPtrNext->rpdCall            = FALSE;
    regPtrNext->rpdIsThis          = isThis;
    regPtrNext->rpdCompiler.rpdAdd = (regMaskSmall)regMask;
    regPtrNext->rpdCompiler.rpdDel = 0;
}

/*****************************************************************************
 *
 *  Record the fact that the given register no longer contains a live GC ref.
 */

void emitter::emitGCregDeadSet(GCtype gcType, regMaskTP regMask, BYTE* addr)
{
    assert(emitIssuing);
    assert(needsGC(gcType));

    regPtrDsc* regPtrNext;

    // assert(emitFullyInt);
    assert(emitFullGCinfo);

    assert(((emitThisGCrefRegs | emitThisByrefRegs) & regMask) != 0);

    /* Allocate a new regptr entry and fill it in */

    regPtrNext            = codeGen->gcInfo.gcRegPtrAllocDsc();
    regPtrNext->rpdGCtype = gcType;

    regPtrNext->rpdOffs            = emitCurCodeOffs(addr);
    regPtrNext->rpdCall            = FALSE;
    regPtrNext->rpdIsThis          = FALSE;
    regPtrNext->rpdArg             = FALSE;
    regPtrNext->rpdCompiler.rpdAdd = 0;
    regPtrNext->rpdCompiler.rpdDel = (regMaskSmall)regMask;
}

/*****************************************************************************
 *
 *  Emit an 8-bit integer as code.
 */

unsigned char emitter::emitOutputByte(BYTE* dst, ssize_t val)
{
    *castto(dst, unsigned char*) = (unsigned char)val;

#ifdef DEBUG
    if (emitComp->opts.dspEmit)
    {
        printf("; emit_byte 0%02XH\n", val & 0xFF);
    }
#ifdef _TARGET_AMD64_
    // if we're emitting code bytes, ensure that we've already emitted the rex prefix!
    assert(((val & 0xFF00000000LL) == 0) || ((val & 0xFFFFFFFF00000000LL) == 0xFFFFFFFF00000000LL));
#endif // _TARGET_AMD64_
#endif

    return sizeof(unsigned char);
}

/*****************************************************************************
 *
 *  Emit a 16-bit integer as code.
 */

unsigned char emitter::emitOutputWord(BYTE* dst, ssize_t val)
{
    MISALIGNED_WR_I2(dst, (short)val);

#ifdef DEBUG
    if (emitComp->opts.dspEmit)
    {
        printf("; emit_word 0%02XH,0%02XH\n", (val & 0xFF), (val >> 8) & 0xFF);
    }
#ifdef _TARGET_AMD64_
    // if we're emitting code bytes, ensure that we've already emitted the rex prefix!
    assert(((val & 0xFF00000000LL) == 0) || ((val & 0xFFFFFFFF00000000LL) == 0xFFFFFFFF00000000LL));
#endif // _TARGET_AMD64_
#endif

    return sizeof(short);
}

/*****************************************************************************
 *
 *  Emit a 32-bit integer as code.
 */

unsigned char emitter::emitOutputLong(BYTE* dst, ssize_t val)
{
    MISALIGNED_WR_I4(dst, (int)val);

#ifdef DEBUG
    if (emitComp->opts.dspEmit)
    {
        printf("; emit_long 0%08XH\n", (int)val);
    }
#ifdef _TARGET_AMD64_
    // if we're emitting code bytes, ensure that we've already emitted the rex prefix!
    assert(((val & 0xFF00000000LL) == 0) || ((val & 0xFFFFFFFF00000000LL) == 0xFFFFFFFF00000000LL));
#endif // _TARGET_AMD64_
#endif

    return sizeof(int);
}

/*****************************************************************************
 *
 *  Emit a pointer-sized integer as code.
 */

unsigned char emitter::emitOutputSizeT(BYTE* dst, ssize_t val)
{
    MISALIGNED_WR_ST(dst, val);

#ifdef DEBUG
    if (emitComp->opts.dspEmit)
    {
#ifdef _TARGET_AMD64_
        printf("; emit_size_t 0%016llXH\n", val);
#else  // _TARGET_AMD64_
        printf("; emit_size_t 0%08XH\n", val);
#endif // _TARGET_AMD64_
    }
#endif // DEBUG

    return TARGET_POINTER_SIZE;
}

//------------------------------------------------------------------------
// Wrappers to emitOutputByte, emitOutputWord, emitOutputLong, emitOutputSizeT
// that take unsigned __int64 or size_t type instead of ssize_t. Used on RyuJIT/x86.
//
// Arguments:
//    dst - passed through
//    val - passed through
//
// Return Value:
//    Same as wrapped function.
//

#if defined(_TARGET_X86_)
unsigned char emitter::emitOutputByte(BYTE* dst, size_t val)
{
    return emitOutputByte(dst, (ssize_t)val);
}

unsigned char emitter::emitOutputWord(BYTE* dst, size_t val)
{
    return emitOutputWord(dst, (ssize_t)val);
}

unsigned char emitter::emitOutputLong(BYTE* dst, size_t val)
{
    return emitOutputLong(dst, (ssize_t)val);
}

unsigned char emitter::emitOutputSizeT(BYTE* dst, size_t val)
{
    return emitOutputSizeT(dst, (ssize_t)val);
}

unsigned char emitter::emitOutputByte(BYTE* dst, unsigned __int64 val)
{
    return emitOutputByte(dst, (ssize_t)val);
}

unsigned char emitter::emitOutputWord(BYTE* dst, unsigned __int64 val)
{
    return emitOutputWord(dst, (ssize_t)val);
}

unsigned char emitter::emitOutputLong(BYTE* dst, unsigned __int64 val)
{
    return emitOutputLong(dst, (ssize_t)val);
}

unsigned char emitter::emitOutputSizeT(BYTE* dst, unsigned __int64 val)
{
    return emitOutputSizeT(dst, (ssize_t)val);
}
#endif // defined(_TARGET_X86_)

/*****************************************************************************
 *
 *  Given a block cookie and a code position, return the actual code offset;
 *  this can only be called at the end of code generation.
 */

UNATIVE_OFFSET emitter::emitCodeOffset(void* blockPtr, unsigned codePos)
{
    insGroup* ig;

    UNATIVE_OFFSET of;
    unsigned       no = emitGetInsNumFromCodePos(codePos);

    /* Make sure we weren't passed some kind of a garbage thing */

    ig = (insGroup*)blockPtr;
#ifdef DEBUG
    assert(ig && ig->igSelf == ig);
#endif

    /* The first and last offsets are always easy */

    if (no == 0)
    {
        of = 0;
    }
    else if (no == ig->igInsCnt)
    {
        of = ig->igSize;
    }
    else if (ig->igFlags & IGF_UPD_ISZ)
    {
        /*
            Some instruction sizes have changed, so we'll have to figure
            out the instruction offset "the hard way".
         */

        of = emitFindOffset(ig, no);
    }
    else
    {
        /* All instructions correctly predicted, the offset stays the same */

        of = emitGetInsOfsFromCodePos(codePos);

        // printf("[IG=%02u;ID=%03u;OF=%04X] <= %08X\n", ig->igNum, emitGetInsNumFromCodePos(codePos), of, codePos);

        /* Make sure the offset estimate is accurate */

        assert(of == emitFindOffset(ig, emitGetInsNumFromCodePos(codePos)));
    }

    return ig->igOffs + of;
}

/*****************************************************************************
 *
 *  Record the fact that the given register now contains a live GC ref.
 */

void emitter::emitGCregLiveUpd(GCtype gcType, regNumber reg, BYTE* addr)
{
    assert(emitIssuing);

    // Don't track GC changes in epilogs
    if (emitIGisInEpilog(emitCurIG))
    {
        return;
    }

    assert(needsGC(gcType));

    regMaskTP regMask = genRegMask(reg);

    regMaskTP& emitThisXXrefRegs = (gcType == GCT_GCREF) ? emitThisGCrefRegs : emitThisByrefRegs;
    regMaskTP& emitThisYYrefRegs = (gcType == GCT_GCREF) ? emitThisByrefRegs : emitThisGCrefRegs;

    if ((emitThisXXrefRegs & regMask) == 0)
    {
        // If the register was holding the other GC type, that type should
        // go dead now

        if (emitThisYYrefRegs & regMask)
        {
            emitGCregDeadUpd(reg, addr);
        }

        // For synchronized methods, "this" is always alive and in the same register.
        // However, if we generate any code after the epilog block (where "this"
        // goes dead), "this" will come alive again. We need to notice that.
        // Note that we only expect isThis to be true at an insGroup boundary.

        bool isThis = (reg == emitSyncThisObjReg) ? true : false;

        if (emitFullGCinfo)
        {
            emitGCregLiveSet(gcType, regMask, addr, isThis);
        }

        emitThisXXrefRegs |= regMask;

#ifdef DEBUG
        if (EMIT_GC_VERBOSE)
        {
            printf("%sReg +[%s]\n", GCtypeStr(gcType), emitRegName(reg));
        }
#endif
    }

    // The 2 GC reg masks can't be overlapping

    assert((emitThisGCrefRegs & emitThisByrefRegs) == 0);
}

/*****************************************************************************
 *
 *  Record the fact that the given set of registers no longer contain live GC refs.
 */

void emitter::emitGCregDeadUpdMask(regMaskTP regs, BYTE* addr)
{
    assert(emitIssuing);

    // Don't track GC changes in epilogs
    if (emitIGisInEpilog(emitCurIG))
    {
        return;
    }

    // First, handle the gcref regs going dead

    regMaskTP gcrefRegs = emitThisGCrefRegs & regs;

    // "this" can never go dead in synchronized methods, except in the epilog
    // after the call to CORINFO_HELP_MON_EXIT.
    assert(emitSyncThisObjReg == REG_NA || (genRegMask(emitSyncThisObjReg) & regs) == 0);

    if (gcrefRegs)
    {
        assert((emitThisByrefRegs & gcrefRegs) == 0);

        if (emitFullGCinfo)
        {
            emitGCregDeadSet(GCT_GCREF, gcrefRegs, addr);
        }

        emitThisGCrefRegs &= ~gcrefRegs;

#ifdef DEBUG
        if (EMIT_GC_VERBOSE)
        {
            printf("gcrReg ");
            printRegMaskInt(gcrefRegs);
            printf(" -");
            emitDispRegSet(gcrefRegs);
            printf("\n");
        }
#endif
    }

    // Second, handle the byref regs going dead

    regMaskTP byrefRegs = emitThisByrefRegs & regs;

    if (byrefRegs)
    {
        assert((emitThisGCrefRegs & byrefRegs) == 0);

        if (emitFullGCinfo)
        {
            emitGCregDeadSet(GCT_BYREF, byrefRegs, addr);
        }

        emitThisByrefRegs &= ~byrefRegs;

#ifdef DEBUG
        if (EMIT_GC_VERBOSE)
        {
            printf("byrReg ");
            printRegMaskInt(byrefRegs);
            printf(" -");
            emitDispRegSet(byrefRegs);
            printf("\n");
        }
#endif
    }
}

/*****************************************************************************
 *
 *  Record the fact that the given register no longer contains a live GC ref.
 */

void emitter::emitGCregDeadUpd(regNumber reg, BYTE* addr)
{
    assert(emitIssuing);

    // Don't track GC changes in epilogs
    if (emitIGisInEpilog(emitCurIG))
    {
        return;
    }

    regMaskTP regMask = genRegMask(reg);

    if ((emitThisGCrefRegs & regMask) != 0)
    {
        assert((emitThisByrefRegs & regMask) == 0);

        if (emitFullGCinfo)
        {
            emitGCregDeadSet(GCT_GCREF, regMask, addr);
        }

        emitThisGCrefRegs &= ~regMask;

#ifdef DEBUG
        if (EMIT_GC_VERBOSE)
        {
            printf("%s -[%s]\n", "gcrReg", emitRegName(reg));
        }
#endif
    }
    else if ((emitThisByrefRegs & regMask) != 0)
    {
        if (emitFullGCinfo)
        {
            emitGCregDeadSet(GCT_BYREF, regMask, addr);
        }

        emitThisByrefRegs &= ~regMask;

#ifdef DEBUG
        if (EMIT_GC_VERBOSE)
        {
            printf("%s -[%s]\n", "byrReg", emitRegName(reg));
        }
#endif
    }
}

/*****************************************************************************
 *
 *  Record the fact that the given variable now contains a live GC ref.
 *  varNum may be INT_MAX or negative (indicating a spill temp) only if
 *    offs is guaranteed to be the offset of a tracked GC ref. Else we
 *    need a valid value to check if the variable is tracked or not.
 */

void emitter::emitGCvarLiveUpd(int offs, int varNum, GCtype gcType, BYTE* addr)
{
    assert(abs(offs) % sizeof(int) == 0);
    assert(needsGC(gcType));

#if FEATURE_FIXED_OUT_ARGS
    if ((unsigned)varNum == emitComp->lvaOutgoingArgSpaceVar)
    {
        if (emitFullGCinfo)
        {
            /* Append an "arg push" entry to track a GC written to the
               outgoing argument space.
               Allocate a new ptr arg entry and fill it in */

            regPtrDsc* regPtrNext = gcInfo->gcRegPtrAllocDsc();
            regPtrNext->rpdGCtype = gcType;
            regPtrNext->rpdOffs   = emitCurCodeOffs(addr);
            regPtrNext->rpdArg    = TRUE;
            regPtrNext->rpdCall   = FALSE;
            noway_assert(FitsIn<unsigned short>(offs));
            regPtrNext->rpdPtrArg  = (unsigned short)offs;
            regPtrNext->rpdArgType = (unsigned short)GCInfo::rpdARG_PUSH;
            regPtrNext->rpdIsThis  = FALSE;

#ifdef DEBUG
            if (EMIT_GC_VERBOSE)
            {
                printf("[%04X] %s arg write\n", offs, GCtypeStr(gcType));
            }
#endif
        }
    }
    else
#endif // FEATURE_FIXED_OUT_ARGS
    {
        /* Is the frame offset within the "interesting" range? */

        if (offs >= emitGCrFrameOffsMin && offs < emitGCrFrameOffsMax)
        {
            /* Normally all variables in this range must be tracked stack
               pointers. However, for EnC, we relax this condition. So we
               must check if this is not such a variable.
               Note that varNum might be negative, indicating a spill temp.
            */

            if (varNum != INT_MAX)
            {
                bool isTracked = false;
                if (varNum >= 0)
                {
                    // This is NOT a spill temp
                    LclVarDsc* varDsc = &emitComp->lvaTable[varNum];
                    isTracked         = emitComp->lvaIsGCTracked(varDsc);
                }
                else
                {
                    // Is it an untracked spill temp?
                    isTracked = TRACK_GC_TEMP_LIFETIMES;
                }
                if (!isTracked)
                {
#if DOUBLE_ALIGN
                    assert(!emitContTrkPtrLcls ||
                           // EBP based variables in the double-aligned frames are indeed input arguments.
                           // and we don't require them to fall into the "interesting" range.
                           ((emitComp->rpFrameType == FT_DOUBLE_ALIGN_FRAME) && (varNum >= 0) &&
                            (emitComp->lvaTable[varNum].lvFramePointerBased == 1)));
#else
                    assert(!emitContTrkPtrLcls);
#endif
                    return;
                }
            }

            size_t disp;

            /* Compute the index into the GC frame table */

            disp = (offs - emitGCrFrameOffsMin) / TARGET_POINTER_SIZE;
            assert(disp < emitGCrFrameOffsCnt);

            /* If the variable is currently dead, mark it as live */

            if (emitGCrFrameLiveTab[disp] == nullptr)
            {
                emitGCvarLiveSet(offs, gcType, addr, disp);
            }
        }
    }
}

/*****************************************************************************
 *
 *  Record the fact that the given variable no longer contains a live GC ref.
 */

void emitter::emitGCvarDeadUpd(int offs, BYTE* addr)
{
    assert(emitIssuing);
    assert(abs(offs) % sizeof(int) == 0);

    /* Is the frame offset within the "interesting" range? */

    if (offs >= emitGCrFrameOffsMin && offs < emitGCrFrameOffsMax)
    {
        size_t disp;

        /* Compute the index into the GC frame table */

        disp = (offs - emitGCrFrameOffsMin) / TARGET_POINTER_SIZE;
        assert(disp < emitGCrFrameOffsCnt);

        /* If the variable is currently live, mark it as dead */

        if (emitGCrFrameLiveTab[disp] != nullptr)
        {
            assert(!emitComp->lvaKeepAliveAndReportThis() || (offs != emitSyncThisObjOffs));
            emitGCvarDeadSet(offs, addr, disp);
        }
    }
}

/*****************************************************************************
 *
 *  Allocate a new IG and link it in to the global list after the current IG
 */

insGroup* emitter::emitAllocAndLinkIG()
{
    insGroup* ig = emitAllocIG();

    assert(emitCurIG);

    emitInsertIGAfter(emitCurIG, ig);

    /* Propagate some IG flags from the current group to the new group */

    ig->igFlags |= (emitCurIG->igFlags & IGF_PROPAGATE_MASK);

    /* Set the new IG as the current IG */

    emitCurIG = ig;

    return ig;
}

/*****************************************************************************
 *
 *  Allocate an instruction group descriptor and assign it the next index.
 */

insGroup* emitter::emitAllocIG()
{
    insGroup* ig;

    /* Allocate a group descriptor */

    size_t sz = sizeof(insGroup);
    ig        = (insGroup*)emitGetMem(sz);

#ifdef DEBUG
    ig->igSelf = ig;
#endif

#if EMITTER_STATS
    emitTotalIGcnt += 1;
    emitTotalIGsize += sz;
    emitSizeMethod += sz;
#endif

    /* Do basic initialization */

    emitInitIG(ig);

    return ig;
}

/*****************************************************************************
 *
 *  Initialize an instruction group
 */

void emitter::emitInitIG(insGroup* ig)
{
    /* Assign the next available index to the instruction group */

    ig->igNum = emitNxtIGnum;

    emitNxtIGnum++;

    /* Record the (estimated) code offset of the group */

    ig->igOffs = emitCurCodeOffset;
    assert(IsCodeAligned(ig->igOffs));

    /* Set the current function index */

    ig->igFuncIdx = emitComp->compCurrFuncIdx;

    ig->igFlags = 0;

    /* Zero out some fields to avoid printing garbage in JitDumps. These
       really only need to be set in DEBUG, but do it in all cases to make
       sure we act the same in non-DEBUG builds.
    */

    ig->igSize   = 0;
    ig->igGCregs = RBM_NONE;
    ig->igInsCnt = 0;
}

/*****************************************************************************
 *
 *  Insert instruction group 'ig' after 'igInsertAfterIG'
 */

void emitter::emitInsertIGAfter(insGroup* insertAfterIG, insGroup* ig)
{
    assert(emitIGlist);
    assert(emitIGlast);

    ig->igNext            = insertAfterIG->igNext;
    insertAfterIG->igNext = ig;

    if (emitIGlast == insertAfterIG)
    {
        // If we are inserting at the end, then update the 'last' pointer
        emitIGlast = ig;
    }
}

/*****************************************************************************
 *
 *  Save the current IG and start a new one.
 */

void emitter::emitNxtIG(bool emitAdd)
{
    /* Right now we don't allow multi-IG prologs */

    assert(emitCurIG != emitPrologIG);

    /* First save the current group */

    emitSavIG(emitAdd);

    /* Update the GC live sets for the group's start
     * Do it only if not an emitter added block */

    if (!emitAdd)
    {
        VarSetOps::Assign(emitComp, emitInitGCrefVars, emitThisGCrefVars);
        emitInitGCrefRegs = emitThisGCrefRegs;
        emitInitByrefRegs = emitThisByrefRegs;
    }

    /* Start generating the new group */

    emitNewIG();

    /* If this is an emitter added block, flag it */

    if (emitAdd)
    {
        emitCurIG->igFlags |= IGF_EMIT_ADD;

#if EMITTER_STATS
        emitTotalIGEmitAdd++;
#endif // EMITTER_STATS
    }

    // We've created a new IG; no need to force another one.
    emitForceNewIG = false;
}

/*****************************************************************************
 *
 *  emitGetInsSC: Get the instruction's constant value.
 */

target_ssize_t emitter::emitGetInsSC(instrDesc* id)
{
#ifdef _TARGET_ARM_ // should it be _TARGET_ARMARCH_? Why do we need this? Note that on ARM64 we store scaled immediates
                    // for some formats
    if (id->idIsLclVar())
    {
        int varNum = id->idAddr()->iiaLclVar.lvaVarNum();

        regNumber baseReg;
        int       offs = id->idAddr()->iiaLclVar.lvaOffset();
#if defined(_TARGET_ARM_)
        int adr =
            emitComp->lvaFrameAddress(varNum, id->idIsLclFPBase(), &baseReg, offs, CodeGen::instIsFP(id->idIns()));
        int dsp = adr + offs;
        if ((id->idIns() == INS_sub) || (id->idIns() == INS_subw))
            dsp = -dsp;
#elif defined(_TARGET_ARM64_)
        // TODO-ARM64-Cleanup: this is currently unreachable. Do we need it?
        bool FPbased;
        int  adr = emitComp->lvaFrameAddress(varNum, &FPbased);
        int  dsp = adr + offs;
        if (id->idIns() == INS_sub)
            dsp = -dsp;
#endif
        return dsp;
    }
    else
#endif // _TARGET_ARM_
        if (id->idIsLargeCns())
    {
        return ((instrDescCns*)id)->idcCnsVal;
    }
    else
    {
        return id->idSmallCns();
    }
}

#ifdef _TARGET_ARM_

BYTE* emitter::emitGetInsRelocValue(instrDesc* id)
{
    return ((instrDescReloc*)id)->idrRelocVal;
}

#endif // _TARGET_ARM_

/*****************************************************************************/
#if EMIT_TRACK_STACK_DEPTH
/*****************************************************************************
 *
 *  Record a push of a single dword on the stack.
 */

void emitter::emitStackPush(BYTE* addr, GCtype gcType)
{
#ifdef DEBUG
    assert(IsValidGCtype(gcType));
#endif

    if (emitSimpleStkUsed)
    {
        assert(!emitFullGCinfo); // Simple stk not used for emitFullGCinfo
        assert(emitCurStackLvl / sizeof(int) < MAX_SIMPLE_STK_DEPTH);

        u1.emitSimpleStkMask <<= 1;
        u1.emitSimpleStkMask |= (unsigned)needsGC(gcType);

        u1.emitSimpleByrefStkMask <<= 1;
        u1.emitSimpleByrefStkMask |= (gcType == GCT_BYREF);

        assert((u1.emitSimpleStkMask & u1.emitSimpleByrefStkMask) == u1.emitSimpleByrefStkMask);
    }
    else
    {
        emitStackPushLargeStk(addr, gcType);
    }

    emitCurStackLvl += sizeof(int);
}

/*****************************************************************************
 *
 *  Record a push of a bunch of non-GC dwords on the stack.
 */

void emitter::emitStackPushN(BYTE* addr, unsigned count)
{
    assert(count);

    if (emitSimpleStkUsed)
    {
        assert(!emitFullGCinfo); // Simple stk not used for emitFullGCinfo

        u1.emitSimpleStkMask <<= count;
        u1.emitSimpleByrefStkMask <<= count;
    }
    else
    {
        emitStackPushLargeStk(addr, GCT_NONE, count);
    }

    emitCurStackLvl += count * sizeof(int);
}

/*****************************************************************************
 *
 *  Record a pop of the given number of dwords from the stack.
 */

void emitter::emitStackPop(BYTE* addr, bool isCall, unsigned char callInstrSize, unsigned count)
{
    assert(emitCurStackLvl / sizeof(int) >= count);
    assert(!isCall || callInstrSize > 0);

    if (count)
    {
        if (emitSimpleStkUsed)
        {
            assert(!emitFullGCinfo); // Simple stk not used for emitFullGCinfo

            unsigned cnt = count;

            do
            {
                u1.emitSimpleStkMask >>= 1;
                u1.emitSimpleByrefStkMask >>= 1;
            } while (--cnt);
        }
        else
        {
            emitStackPopLargeStk(addr, isCall, callInstrSize, count);
        }

        emitCurStackLvl -= count * sizeof(int);
    }
    else
    {
        assert(isCall);

        // For the general encoder we do the call below always when it's a call, to ensure that the call is
        // recorded (when we're doing the ptr reg map for a non-fully-interruptible method).
        if (emitFullGCinfo
#ifndef JIT32_GCENCODER
            || (emitComp->genFullPtrRegMap && (!emitComp->genInterruptible) && isCall)
#endif // JIT32_GCENCODER
                )
        {
            emitStackPopLargeStk(addr, isCall, callInstrSize, 0);
        }
    }
}

/*****************************************************************************
 *
 *  Record a push of a single word on the stack for a full pointer map.
 */

void emitter::emitStackPushLargeStk(BYTE* addr, GCtype gcType, unsigned count)
{
    S_UINT32 level(emitCurStackLvl / sizeof(int));

    assert(IsValidGCtype(gcType));
    assert(count);
    assert(!emitSimpleStkUsed);

    do
    {
        /* Push an entry for this argument on the tracking stack */

        // printf("Pushed [%d] at lvl %2u [max=%u]\n", isGCref, emitArgTrackTop - emitArgTrackTab, emitMaxStackDepth);

        assert(level.IsOverflow() || u2.emitArgTrackTop == u2.emitArgTrackTab + level.Value());
        *u2.emitArgTrackTop++ = (BYTE)gcType;
        assert(u2.emitArgTrackTop <= u2.emitArgTrackTab + emitMaxStackDepth);

        if (emitFullArgInfo || needsGC(gcType))
        {
            if (emitFullGCinfo)
            {
                /* Append an "arg push" entry if this is a GC ref or
                   FPO method. Allocate a new ptr arg entry and fill it in */

                regPtrDsc* regPtrNext = codeGen->gcInfo.gcRegPtrAllocDsc();
                regPtrNext->rpdGCtype = gcType;

                regPtrNext->rpdOffs = emitCurCodeOffs(addr);
                regPtrNext->rpdArg  = TRUE;
                regPtrNext->rpdCall = FALSE;
                if (level.IsOverflow() || !FitsIn<unsigned short>(level.Value()))
                {
                    IMPL_LIMITATION("Too many/too big arguments to encode GC information");
                }
                regPtrNext->rpdPtrArg  = (unsigned short)level.Value();
                regPtrNext->rpdArgType = (unsigned short)GCInfo::rpdARG_PUSH;
                regPtrNext->rpdIsThis  = FALSE;

#ifdef DEBUG
                if (EMIT_GC_VERBOSE)
                {
                    printf("[%08X] %s arg push %u\n", dspPtr(regPtrNext), GCtypeStr(gcType), level.Value());
                }
#endif
            }

            /* This is an "interesting" argument push */

            u2.emitGcArgTrackCnt++;
        }
        level += 1;
        assert(!level.IsOverflow());
    } while (--count);
}

/*****************************************************************************
 *
 *  Record a pop of the given number of words from the stack for a full ptr
 *  map.
 */

void emitter::emitStackPopLargeStk(BYTE* addr, bool isCall, unsigned char callInstrSize, unsigned count)
{
    assert(emitIssuing);

    unsigned argStkCnt;
    S_UINT16 argRecCnt(0); // arg count for ESP, ptr-arg count for EBP
    unsigned gcrefRegs, byrefRegs;

#ifdef JIT32_GCENCODER
    // For the general encoder, we always need to record calls, so we make this call
    // even when emitSimpleStkUsed is true.
    assert(!emitSimpleStkUsed);
#endif

    /* Count how many pointer records correspond to this "pop" */

    for (argStkCnt = count; argStkCnt; argStkCnt--)
    {
        assert(u2.emitArgTrackTop > u2.emitArgTrackTab);

        GCtype gcType = (GCtype)(*--u2.emitArgTrackTop);

        assert(IsValidGCtype(gcType));

        // printf("Popped [%d] at lvl %u\n", GCtypeStr(gcType), emitArgTrackTop - emitArgTrackTab);

        // This is an "interesting" argument

        if (emitFullArgInfo || needsGC(gcType))
        {
            argRecCnt += 1;
        }
    }

    assert(u2.emitArgTrackTop >= u2.emitArgTrackTab);
    assert(u2.emitArgTrackTop == u2.emitArgTrackTab + emitCurStackLvl / sizeof(int) - count);
    noway_assert(!argRecCnt.IsOverflow());

    /* We're about to pop the corresponding arg records */

    u2.emitGcArgTrackCnt -= argRecCnt.Value();

#ifdef JIT32_GCENCODER
    // For the general encoder, we always have to record calls, so we don't take this early return.
    if (!emitFullGCinfo)
        return;
#endif

    // Do we have any interesting (i.e., callee-saved) registers live here?

    gcrefRegs = byrefRegs = 0;

    // We make a bitmask whose bits correspond to callee-saved register indices (in the sequence
    // of callee-saved registers only).
    for (unsigned calleeSavedRegIdx = 0; calleeSavedRegIdx < CNT_CALLEE_SAVED; calleeSavedRegIdx++)
    {
        regMaskTP calleeSavedRbm = raRbmCalleeSaveOrder[calleeSavedRegIdx];
        if (emitThisGCrefRegs & calleeSavedRbm)
        {
            gcrefRegs |= (1 << calleeSavedRegIdx);
        }
        if (emitThisByrefRegs & calleeSavedRbm)
        {
            byrefRegs |= (1 << calleeSavedRegIdx);
        }
    }

#ifdef JIT32_GCENCODER
    // For the general encoder, we always have to record calls, so we don't take this early return.    /* Are there any
    // args to pop at this call site?

    if (argRecCnt.Value() == 0)
    {
        /*
            Or do we have a partially interruptible EBP-less frame, and any
            of EDI,ESI,EBX,EBP are live, or is there an outer/pending call?
         */
        CLANG_FORMAT_COMMENT_ANCHOR;

#if !FPO_INTERRUPTIBLE
        if (emitFullyInt || (gcrefRegs == 0 && byrefRegs == 0 && u2.emitGcArgTrackCnt == 0))
#endif
            return;
    }
#endif // JIT32_GCENCODER

    /* Only calls may pop more than one value */
    // More detail:
    // _cdecl calls accomplish this popping via a post-call-instruction SP adjustment.
    // The "rpdCall" field below should be interpreted as "the instruction accomplishes
    // call-related popping, even if it's not itself a call".  Therefore, we don't just
    // use the "isCall" input argument, which means that the instruction actually is a call --
    // we use the OR of "isCall" or the "pops more than one value."

    bool isCallRelatedPop = (argRecCnt.Value() > 1);

    /* Allocate a new ptr arg entry and fill it in */

    regPtrDsc* regPtrNext = codeGen->gcInfo.gcRegPtrAllocDsc();
    regPtrNext->rpdGCtype = GCT_GCREF; // Pops need a non-0 value (??)

    regPtrNext->rpdOffs = emitCurCodeOffs(addr);
    regPtrNext->rpdCall = (isCall || isCallRelatedPop);
#ifndef JIT32_GCENCODER
    if (regPtrNext->rpdCall)
    {
        assert(isCall || callInstrSize == 0);
        regPtrNext->rpdCallInstrSize = callInstrSize;
    }
#endif
    regPtrNext->rpdCallGCrefRegs = gcrefRegs;
    regPtrNext->rpdCallByrefRegs = byrefRegs;
    regPtrNext->rpdArg           = TRUE;
    regPtrNext->rpdArgType       = (unsigned short)GCInfo::rpdARG_POP;
    regPtrNext->rpdPtrArg        = argRecCnt.Value();

#ifdef DEBUG
    if (EMIT_GC_VERBOSE)
    {
        printf("[%08X] ptr arg pop  %u\n", dspPtr(regPtrNext), count);
    }
#endif
}

/*****************************************************************************
 *  For caller-pop arguments, we report the arguments as pending arguments.
 *  However, any GC arguments are now dead, so we need to report them
 *  as non-GC.
 */

void emitter::emitStackKillArgs(BYTE* addr, unsigned count, unsigned char callInstrSize)
{
    assert(count > 0);

    if (emitSimpleStkUsed)
    {
        assert(!emitFullGCinfo); // Simple stk not used for emitFullGCInfo

        /* We don't need to report this to the GC info, but we do need
           to kill mark the ptrs on the stack as non-GC */

        assert(emitCurStackLvl / sizeof(int) >= count);

        for (unsigned lvl = 0; lvl < count; lvl++)
        {
            u1.emitSimpleStkMask &= ~(1 << lvl);
            u1.emitSimpleByrefStkMask &= ~(1 << lvl);
        }
    }
    else
    {
        BYTE*    argTrackTop = u2.emitArgTrackTop;
        S_UINT16 gcCnt(0);

        for (unsigned i = 0; i < count; i++)
        {
            assert(argTrackTop > u2.emitArgTrackTab);

            --argTrackTop;

            GCtype gcType = (GCtype)(*argTrackTop);
            assert(IsValidGCtype(gcType));

            if (needsGC(gcType))
            {
                // printf("Killed %s at lvl %u\n", GCtypeStr(gcType), argTrackTop - emitArgTrackTab);

                *argTrackTop = GCT_NONE;
                gcCnt += 1;
            }
        }

        noway_assert(!gcCnt.IsOverflow());

        /* We're about to kill the corresponding (pointer) arg records */

        if (!emitFullArgInfo)
        {
            u2.emitGcArgTrackCnt -= gcCnt.Value();
        }

        if (!emitFullGCinfo)
        {
            return;
        }

        /* Right after the call, the arguments are still sitting on the
           stack, but they are effectively dead. For fully-interruptible
           methods, we need to report that */

        if (emitFullGCinfo && gcCnt.Value())
        {
            /* Allocate a new ptr arg entry and fill it in */

            regPtrDsc* regPtrNext = codeGen->gcInfo.gcRegPtrAllocDsc();
            regPtrNext->rpdGCtype = GCT_GCREF; // Kills need a non-0 value (??)

            regPtrNext->rpdOffs = emitCurCodeOffs(addr);

            regPtrNext->rpdArg     = TRUE;
            regPtrNext->rpdArgType = (unsigned short)GCInfo::rpdARG_KILL;
            regPtrNext->rpdPtrArg  = gcCnt.Value();

#ifdef DEBUG
            if (EMIT_GC_VERBOSE)
            {
                printf("[%08X] ptr arg kill %u\n", dspPtr(regPtrNext), count);
            }
#endif
        }

        /* Now that ptr args have been marked as non-ptrs, we need to record
           the call itself as one that has no arguments. */

        emitStackPopLargeStk(addr, true, callInstrSize, 0);
    }
}

/*****************************************************************************
 *  A helper for recording a relocation with the EE.
 */
void emitter::emitRecordRelocation(void* location,            /* IN */
                                   void* target,              /* IN */
                                   WORD  fRelocType,          /* IN */
                                   WORD  slotNum /* = 0 */,   /* IN */
                                   INT32 addlDelta /* = 0 */) /* IN */
{
    assert(slotNum == 0); // It is unused on all supported platforms.

    // If we're an unmatched altjit, don't tell the VM anything. We still record the relocation for
    // late disassembly; maybe we'll need it?
    if (emitComp->info.compMatchedVM)
    {
        emitCmpHandle->recordRelocation(location, target, fRelocType, slotNum, addlDelta);
    }
#if defined(LATE_DISASM)
    codeGen->getDisAssembler().disRecordRelocation((size_t)location, (size_t)target);
#endif // defined(LATE_DISASM)
}

#ifdef _TARGET_ARM_
/*****************************************************************************
 *  A helper for handling a Thumb-Mov32 of position-independent (PC-relative) value
 *
 *  This routine either records relocation for the location with the EE,
 *  or creates a virtual relocation entry to perform offset fixup during
 *  compilation without recording it with EE - depending on which of
 *  absolute/relocative relocations mode are used for code section.
 */
void emitter::emitHandlePCRelativeMov32(void* location, /* IN */
                                        void* target)   /* IN */
{
    if (emitComp->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_RELATIVE_CODE_RELOCS))
    {
        emitRecordRelocation(location, target, IMAGE_REL_BASED_REL_THUMB_MOV32_PCREL);
    }
    else
    {
        emitRecordRelocation(location, target, IMAGE_REL_BASED_THUMB_MOV32);
    }
}
#endif // _TARGET_ARM_

/*****************************************************************************
 *  A helper for recording a call site with the EE.
 */
void emitter::emitRecordCallSite(ULONG                 instrOffset,  /* IN */
                                 CORINFO_SIG_INFO*     callSig,      /* IN */
                                 CORINFO_METHOD_HANDLE methodHandle) /* IN */
{
#if defined(DEBUG)
    // Since CORINFO_SIG_INFO is a heavyweight structure, in most cases we can
    // lazily obtain it here using the given method handle (we only save the sig
    // info when we explicitly need it, i.e. for CALLI calls, vararg calls, and
    // tail calls).
    if (callSig == nullptr)
    {
        assert(methodHandle != nullptr);

        if (Compiler::eeGetHelperNum(methodHandle) == CORINFO_HELP_UNDEF)
        {
            if (emitScratchSigInfo == nullptr)
            {
                emitScratchSigInfo = new (emitComp, CMK_CorSig) CORINFO_SIG_INFO;
            }

            emitComp->eeGetMethodSig(methodHandle, emitScratchSigInfo);
            callSig = emitScratchSigInfo;
        }
    }

    emitCmpHandle->recordCallSite(instrOffset, callSig, methodHandle);
#endif // defined(DEBUG)
}

/*****************************************************************************/
#endif // EMIT_TRACK_STACK_DEPTH
/*****************************************************************************/
/*****************************************************************************/

#ifdef DEBUG

/*****************************************************************************
 *  Given a code offset, return a string representing a label for that offset.
 *  If the code offset is just after the end of the code of the function, the
 *  label will be "END". If the code offset doesn't correspond to any known
 *  offset, the label will be "UNKNOWN". The strings are returned from static
 *  buffers. This function rotates amongst four such static buffers (there are
 *  cases where this function is called four times to provide data for a single
 *  printf()).
 */

const char* emitter::emitOffsetToLabel(unsigned offs)
{
    const size_t    TEMP_BUFFER_LEN = 40;
    static unsigned curBuf          = 0;
    static char     buf[4][TEMP_BUFFER_LEN];
    char*           retbuf;

    UNATIVE_OFFSET nextof = 0;

    for (insGroup* ig = emitIGlist; ig != nullptr; ig = ig->igNext)
    {
        // There is an eventual unused space after the last actual hot block
        // before the first allocated cold block.
        assert((nextof == ig->igOffs) || (ig == emitFirstColdIG));

        if (ig->igOffs == offs)
        {
            // Found it!
            sprintf_s(buf[curBuf], TEMP_BUFFER_LEN, "G_M%03u_IG%02u", Compiler::s_compMethodsCount, ig->igNum);
            retbuf = buf[curBuf];
            curBuf = (curBuf + 1) % 4;
            return retbuf;
        }
        else if (ig->igOffs > offs)
        {
            // We went past the requested offset but didn't find it.
            sprintf_s(buf[curBuf], TEMP_BUFFER_LEN, "UNKNOWN");
            retbuf = buf[curBuf];
            curBuf = (curBuf + 1) % 4;
            return retbuf;
        }

        nextof = ig->igOffs + ig->igSize;
    }

    if (nextof == offs)
    {
        // It's a pseudo-label to the end.
        sprintf_s(buf[curBuf], TEMP_BUFFER_LEN, "END");
        retbuf = buf[curBuf];
        curBuf = (curBuf + 1) % 4;
        return retbuf;
    }
    else
    {
        sprintf_s(buf[curBuf], TEMP_BUFFER_LEN, "UNKNOWN");
        retbuf = buf[curBuf];
        curBuf = (curBuf + 1) % 4;
        return retbuf;
    }
}

#endif // DEBUG

//------------------------------------------------------------------------
// emitGetGCRegsSavedOrModified: Returns the set of registers that keeps gcrefs and byrefs across the call.
//
// Notes: it returns union of two sets:
//        1) registers that could contain GC/byRefs before the call and call doesn't touch them;
//        2) registers that contain GC/byRefs before the call and call modifies them, but they still
//           contain GC/byRefs.
//
// Arguments:
//   methHnd - the method handler of the call.
//
// Return value:
//   the saved set of registers.
//
regMaskTP emitter::emitGetGCRegsSavedOrModified(CORINFO_METHOD_HANDLE methHnd)
{
    // Is it a helper with a special saved set?
    bool isNoGCHelper = emitNoGChelper(methHnd);
    if (isNoGCHelper)
    {
        CorInfoHelpFunc helpFunc = Compiler::eeGetHelperNum(methHnd);

        // Get the set of registers that this call kills and remove it from the saved set.
        regMaskTP savedSet = RBM_ALLINT & ~emitGetGCRegsKilledByNoGCCall(helpFunc);

#ifdef DEBUG
        if (emitComp->verbose)
        {
            printf("NoGC Call: savedSet=");
            printRegMaskInt(savedSet);
            emitDispRegSet(savedSet);
            printf("\n");
        }
#endif
        return savedSet;
    }
    else
    {
        // This is the saved set of registers after a normal call.
        return RBM_CALLEE_SAVED;
    }
}

//----------------------------------------------------------------------
// emitGetGCRegsKilledByNoGCCall: Gets a register mask that represents the set of registers that no longer
// contain GC or byref pointers, for "NO GC" helper calls. This is used by the emitter when determining
// what registers to remove from the current live GC/byref sets (and thus what to report as dead in the
// GC info). Note that for the CORINFO_HELP_ASSIGN_BYREF helper, in particular, the kill set reported by
// compHelperCallKillSet() doesn't match this kill set. compHelperCallKillSet() reports the dst/src
// address registers as killed for liveness purposes, since their values change. However, they still are
// valid byref pointers after the call, so the dst/src address registers are NOT reported as killed here.
//
// Note: This list may not be complete and defaults to the default RBM_CALLEE_TRASH_NOGC registers.
//
// Arguments:
//   helper - The helper being inquired about
//
// Return Value:
//   Mask of GC register kills
//
regMaskTP emitter::emitGetGCRegsKilledByNoGCCall(CorInfoHelpFunc helper)
{
    assert(emitNoGChelper(helper));
    regMaskTP result;
    switch (helper)
    {
        case CORINFO_HELP_ASSIGN_BYREF:
#if defined(_TARGET_X86_)
            // This helper only trashes ECX.
            result = RBM_ECX;
            break;
#elif defined(_TARGET_AMD64_)
            // This uses and defs RDI and RSI.
            result = RBM_CALLEE_TRASH_NOGC & ~(RBM_RDI | RBM_RSI);
            break;
#elif defined(_TARGET_ARMARCH_)
            result = RBM_CALLEE_GCTRASH_WRITEBARRIER_BYREF;
            break;
#else
            assert(!"unknown arch");
#endif

#if defined(_TARGET_XARCH_) || defined(_TARGET_ARM_)
        case CORINFO_HELP_PROF_FCN_ENTER:
            result = RBM_PROFILER_ENTER_TRASH;
            break;

        case CORINFO_HELP_PROF_FCN_LEAVE:
            result = RBM_PROFILER_LEAVE_TRASH;
            break;
#if defined(_TARGET_XARCH_)
        case CORINFO_HELP_PROF_FCN_TAILCALL:
            result = RBM_PROFILER_TAILCALL_TRASH;
            break;
#endif // defined(_TARGET_XARCH_)
#endif // defined(_TARGET_XARCH_) || defined(_TARGET_ARM_)

#if defined(_TARGET_ARMARCH_)
        case CORINFO_HELP_ASSIGN_REF:
        case CORINFO_HELP_CHECKED_ASSIGN_REF:
            result = RBM_CALLEE_GCTRASH_WRITEBARRIER;
            break;
#endif // defined(_TARGET_ARMARCH_)

#if defined(_TARGET_X86_)
        case CORINFO_HELP_INIT_PINVOKE_FRAME:
            result = RBM_INIT_PINVOKE_FRAME_TRASH;
            break;
#endif // defined(_TARGET_X86_)

        default:
            result = RBM_CALLEE_TRASH_NOGC;
            break;
    }

    // compHelperCallKillSet returns a superset of the registers which values are not guranteed to be the same
    // after the call, if a register loses its GC or byref it has to be in the compHelperCallKillSet set as well.
    assert((result & emitComp->compHelperCallKillSet(helper)) == result);

    return result;
}
