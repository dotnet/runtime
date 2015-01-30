//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


    /************************************************************************/
    /*       Overall emitter control (including startup and shutdown)       */
    /************************************************************************/

    static
    void            emitInit();
    static
    void            emitDone();

    void            emitBegCG(Compiler   *  comp,
                              COMP_HANDLE   cmpHandle);
    void            emitEndCG();

    void            emitBegFN(bool     hasFramePtr
#if defined(DEBUG)
                              , bool     checkAlign
#endif
#ifdef LEGACY_BACKEND
                              , unsigned lclSize
#endif // LEGACY_BACKEND
                              , unsigned maxTmpSize
                              );

    void            emitEndFN();

    void            emitComputeCodeSizes();

    unsigned        emitEndCodeGen(Compiler *comp,
                                   bool      contTrkPtrLcls,
                                   bool      fullyInt,
                                   bool      fullPtrMap,
                                   bool      returnsGCr,
                                   unsigned  xcptnsCount,
                                   unsigned *prologSize,
                                   unsigned *epilogSize, void **codeAddr,
                                                         void **coldCodeAddr,
                                                         void **consAddr);

    /************************************************************************/
    /*                      Method prolog and epilog                        */
    /************************************************************************/

    unsigned        emitGetEpilogCnt();

    template<typename Callback>
    bool            emitGenNoGCLst  (Callback & cb);

    void            emitBegProlog();
    unsigned        emitGetPrologOffsetEstimate();
    void            emitMarkPrologEnd();
    void            emitEndProlog();

    void            emitCreatePlaceholderIG(insGroupPlaceholderType igType,
                                            BasicBlock* igBB,
                                            VARSET_VALARG_TP GCvars,
                                            regMaskTP gcrefRegs,
                                            regMaskTP byrefRegs,
                                            bool last);

    void            emitGeneratePrologEpilog();
    void            emitStartPrologEpilogGeneration();
    void            emitFinishPrologEpilogGeneration();

    /************************************************************************/
    /*           Record a code position and later convert it to offset      */
    /************************************************************************/

    void    *       emitCurBlock ();
    unsigned        emitCurOffset();

    UNATIVE_OFFSET  emitCodeOffset(void *blockPtr, unsigned codeOffs);

#ifdef DEBUG
    const char*     emitOffsetToLabel(unsigned offs);
#endif // DEBUG

    /************************************************************************/
    /*                   Output target-independent instructions             */
    /************************************************************************/

    void            emitIns_J(instruction   ins,
                              BasicBlock *  dst,
                              int           instrCount = 0);

    /************************************************************************/
    /*                   Emit initialized data sections                     */
    /************************************************************************/

    UNATIVE_OFFSET  emitDataGenBeg (UNATIVE_OFFSET size,
                                    bool           dblAlign,
                                    bool           codeLtab);

    UNATIVE_OFFSET  emitBBTableDataGenBeg(unsigned numEntries, 
                                          bool relativeAddr);

    void            emitDataGenData(unsigned      offs,
                                    const void *  data,
                                    size_t        size);

    void            emitDataGenData(unsigned      offs,
                                    BasicBlock *  label);

    void            emitDataGenEnd();

    UNATIVE_OFFSET  emitDataConst(const void* cnsAddr, 
                                  unsigned cnsSize, 
                                  bool dblAlign);

    UNATIVE_OFFSET  emitDataSize();

    /************************************************************************/
    /*                   Instruction information                            */
    /************************************************************************/

#ifdef _TARGET_XARCH_
    static bool         instrIs3opImul              (instruction ins);
    static bool         instrIsExtendedReg3opImul   (instruction ins);
    static bool         instrHasImplicitRegPairDest (instruction ins);
    static void         check3opImulValues          ();
    static regNumber    inst3opImulReg              (instruction ins);
    static instruction  inst3opImulForReg           (regNumber   reg);
#endif

    /************************************************************************/
    /*                   Emit PDB offset translation information            */
    /************************************************************************/

#ifdef  TRANSLATE_PDB

    static void     SetILBaseOfCode ( BYTE    *pTextBase );
    static void     SetILMethodBase ( BYTE *pMethodEntry );
    static void     SetILMethodStart( BYTE  *pMethodCode );
    static void     SetImgBaseOfCode( BYTE    *pTextBase );
    
    void            SetIDBaseToProlog();
    void            SetIDBaseToOffset( int  methodOffset );
    
    static void     DisablePDBTranslation();
    static bool     IsPDBEnabled();
    
    static void     InitTranslationMaps( int  ilCodeSize );
    static void     DeleteTranslationMaps();
    static void     InitTranslator( PDBRewriter * pPDB,
                                    int *         rgSecMap,
                                    IMAGE_SECTION_HEADER **rgpHeader,
                                    int           numSections );
#endif


    /************************************************************************/
    /*                   Interface for generating unwind information        */
    /************************************************************************/

#ifdef _TARGET_ARMARCH_

    bool            emitIsFuncEnd(emitLocation* emitLoc, emitLocation* emitLocNextFragment = NULL);

    void            emitSplit(emitLocation* startLoc, emitLocation* endLoc, UNATIVE_OFFSET maxSplitSize, void* context, emitSplitCallbackType callbackFunc);

    void            emitUnwindNopPadding(emitLocation* locFrom, Compiler* comp);

#endif // _TARGET_ARMARCH_

#if defined(_TARGET_ARM_)

    unsigned        emitGetInstructionSize(emitLocation* emitLoc);

#endif // defined(_TARGET_ARM_)
