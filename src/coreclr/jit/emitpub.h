// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/************************************************************************/
/*       Overall emitter control (including startup and shutdown)       */
/************************************************************************/

static void emitInit();
static void emitDone();

void emitBegCG(Compiler* comp, COMP_HANDLE cmpHandle);
void emitEndCG();

void emitBegFN(bool hasFramePtr
#if defined(DEBUG)
               ,
               bool checkAlign
#endif
               );

void emitEndFN();

void emitComputeCodeSizes();

unsigned emitEndCodeGen(Compiler* comp,
                        bool      contTrkPtrLcls,
                        bool      fullyInt,
                        bool      fullPtrMap,
                        unsigned  xcptnsCount,
                        unsigned* prologSize,
                        unsigned* epilogSize,
                        void**    codeAddr,
                        void**    codeAddrRW,
                        void**    coldCodeAddr,
                        void**    coldCodeAddrRW,
                        void**    consAddr,
                        void** consAddrRW DEBUGARG(unsigned* instrCount));

/************************************************************************/
/*                      Method prolog and epilog                        */
/************************************************************************/

unsigned emitGetEpilogCnt();

template <typename Callback>
bool emitGenNoGCLst(Callback& cb);

void     emitBegProlog();
unsigned emitGetPrologOffsetEstimate();
void     emitMarkPrologEnd();
void     emitEndProlog();

void emitCreatePlaceholderIG(insGroupPlaceholderType igType,
                             BasicBlock*             igBB,
                             VARSET_VALARG_TP        GCvars,
                             regMaskGpr              gcrefRegs,
                             regMaskGpr              byrefRegs,
                             bool                    last);

void emitGeneratePrologEpilog();
void emitStartPrologEpilogGeneration();
void emitFinishPrologEpilogGeneration();

/************************************************************************/
/*           Record a code position and later convert it to offset      */
/************************************************************************/

void*    emitCurBlock();
unsigned emitCurOffset();
unsigned emitSpecifiedOffset(unsigned insCount, unsigned igSize);

UNATIVE_OFFSET emitCodeOffset(void* blockPtr, unsigned codeOffs);

#ifdef DEBUG
const char* emitOffsetToLabel(unsigned offs);
#endif // DEBUG

/************************************************************************/
/*                   Emit initialized data sections                     */
/************************************************************************/

UNATIVE_OFFSET emitDataGenBeg(unsigned size, unsigned alignment, var_types dataType);

UNATIVE_OFFSET emitBBTableDataGenBeg(unsigned numEntries, bool relativeAddr);

void emitDataGenData(unsigned offs, const void* data, UNATIVE_OFFSET size);

void emitDataGenData(unsigned offs, BasicBlock* label);

void emitDataGenEnd();

static const UNATIVE_OFFSET INVALID_UNATIVE_OFFSET = (UNATIVE_OFFSET)-1;

UNATIVE_OFFSET emitDataGenFind(const void* cnsAddr, unsigned size, unsigned alignment, var_types dataType);

UNATIVE_OFFSET emitDataConst(const void* cnsAddr, unsigned cnsSize, unsigned cnsAlign, var_types dataType);

UNATIVE_OFFSET emitDataSize();

/************************************************************************/
/*                   Instruction information                            */
/************************************************************************/

#ifdef TARGET_XARCH
static bool instrIs3opImul(instruction ins);
static bool instrIsExtendedReg3opImul(instruction ins);
static bool instrHasImplicitRegPairDest(instruction ins);
static void      check3opImulValues();
static regNumber inst3opImulReg(instruction ins);
static instruction inst3opImulForReg(regNumber reg);
#endif

/************************************************************************/
/*                   Emit PDB offset translation information            */
/************************************************************************/

#ifdef TRANSLATE_PDB

static void SetILBaseOfCode(BYTE* pTextBase);
static void SetILMethodBase(BYTE* pMethodEntry);
static void SetILMethodStart(BYTE* pMethodCode);
static void SetImgBaseOfCode(BYTE* pTextBase);

void SetIDBaseToProlog();
void SetIDBaseToOffset(int methodOffset);

static void DisablePDBTranslation();
static bool IsPDBEnabled();

static void InitTranslationMaps(int ilCodeSize);
static void DeleteTranslationMaps();
static void InitTranslator(PDBRewriter* pPDB, int* rgSecMap, IMAGE_SECTION_HEADER** rgpHeader, int numSections);
#endif

/************************************************************************/
/*                   Interface for generating unwind information        */
/************************************************************************/

#if defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

bool emitIsFuncEnd(emitLocation* emitLoc, emitLocation* emitLocNextFragment = NULL);

void emitSplit(emitLocation*         startLoc,
               emitLocation*         endLoc,
               UNATIVE_OFFSET        maxSplitSize,
               void*                 context,
               emitSplitCallbackType callbackFunc);

void emitUnwindNopPadding(emitLocation* locFrom, Compiler* comp);

#endif // TARGET_ARMARCH || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

#if defined(TARGET_ARM)

unsigned emitGetInstructionSize(emitLocation* emitLoc);

#endif // defined(TARGET_ARM)
