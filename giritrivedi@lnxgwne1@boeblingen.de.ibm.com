Function name is s390xHw
****** START compiling Program:s390xHw() (MethodHash=a394a364)
Generating code for Unix s390x
OPTIONS: compCodeOpt = BLENDED_CODE
OPTIONS: compDbgCode = true
OPTIONS: compDbgInfo = true
OPTIONS: compDbgEnC  = false
OPTIONS: compProcedureSplitting   = false
OPTIONS: compProcedureSplittingEH = false
OPTIONS: optimizer should use profile data
IL to import:
IL_0000  00                nop         
IL_0001  28 01 00 00 06    call         0x6000001
IL_0006  00                nop         
IL_0007  2a                ret         

lvaGrabTemp returning 0 (V00 tmp0) (a long lifetime temp) called for OutgoingArgSpace.

Local V00 should not be enregistered because: it is address exposed
; Initial local variable assignments
;
;  V00 OutArgs        struct <0> do-not-enreg[XS] addr-exposed "OutgoingArgSpace"
*************** In compInitDebuggingInfo() for Program:s390xHw()
getVars() returned cVars = 0, extendOthers = true
info.compStmtOffsetsCount    = 0
info.compStmtOffsetsImplicit = 0007h ( STACK_EMPTY NOP CALL_SITE )
*************** In fgFindBasicBlocks() for Program:s390xHw()
Jump targets:
  none
New Basic Block BB01 [0000] created.
BB01 [0000] [000..008)
CLFLG_MINOPT set for method Program:s390xHw()
IL Code Size,Instr    8,   4, Basic Block count   1, Local Variable Num,Ref count   1,  0 for method Program:s390xHw()
IL Code Size,Instr    8,   4, Basic Block count   1, Local Variable Num,Ref count   1,  0 for method Program:s390xHw()
OPTIONS: opts.MinOpts() == true
Basic block list for 'Program:s390xHw()'

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB01 [0000]  1                             1    [000..008)                           (return)                     
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

*************** Starting PHASE Pre-import

*************** Finishing PHASE Pre-import
Trees after Pre-import

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB01 [0000]  1                             1    [000..008)                           (return)                     
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

------------ BB01 [0000] [000..008) (return), preds={} succs={}

-------------------------------------------------------------------------------------------------------------------
*************** In fgDebugCheckBBlist

*************** Starting PHASE Profile incorporation
not optimizing, so not incorporating any profile data

*************** Finishing PHASE Profile incorporation [no changes]

*************** Starting PHASE Importation

impImportBlockPending for BB01

Importing BB01 (PC=000) of 'Program:s390xHw()'
    [ 0]   0 (0x000) nop

STMT00000 ( 0x000[E-] ... ??? )
               [000000] -----------                         *  NO_OP     void  

    [ 0]   1 (0x001) call 06000001
In Compiler::impImportCall: opcode is call, kind=0, callRetType is void, structSize is 0


STMT00001 ( 0x001[E-] ... ??? )
               [000001] --C-G------                         *  CALL      void   Program:foo()

    [ 0]   6 (0x006) nop

STMT00002 ( 0x006[E-] ... ??? )
               [000002] -----------                         *  NO_OP     void  

    [ 0]   7 (0x007) ret

STMT00003 ( 0x007[E-] ... ??? )
               [000003] -----------                         *  RETURN    void  

*************** Finishing PHASE Importation
Trees after Importation

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB01 [0000]  1                             1    [000..008)                           (return)                     i
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

------------ BB01 [0000] [000..008) (return), preds={} succs={}

***** BB01 [0000]
STMT00000 ( 0x000[E-] ... 0x000 )
               [000000] -----------                         *  NO_OP     void  

***** BB01 [0000]
STMT00001 ( 0x001[E-] ... 0x006 )
               [000001] --C-G------                         *  CALL      void   Program:foo()

***** BB01 [0000]
STMT00002 ( 0x006[E-] ... ??? )
               [000002] -----------                         *  NO_OP     void  

***** BB01 [0000]
STMT00003 ( 0x007[E-] ... 0x007 )
               [000003] -----------                         *  RETURN    void  

-------------------------------------------------------------------------------------------------------------------
*************** In fgDebugCheckBBlist
[deferred prior check failed -- skipping this check]

*************** Starting PHASE Expand patchpoints

 -- no patchpoints to transform

*************** Finishing PHASE Expand patchpoints [no changes]

*************** Starting PHASE Indirect call transform

 -- no candidates to transform

*************** Finishing PHASE Indirect call transform [no changes]

*************** Starting PHASE Post-import

*************** Finishing PHASE Post-import [no changes]

*************** Starting PHASE Morph - Init

New BlockSet epoch 1, # of blocks (including unused BB00): 2, bitset array size: 1 (short)

*************** Finishing PHASE Morph - Init [no changes]

*************** Starting PHASE Morph - Inlining

*************** Finishing PHASE Morph - Inlining [no changes]

*************** Starting PHASE Allocate Objects
no newobjs in this method; punting

*************** Finishing PHASE Allocate Objects [no changes]

*************** Starting PHASE Morph - Add internal blocks
New Basic Block BB02 [0001] created.
setting likelihood of BB02 -> BB01 to 1
New scratch BB02

*************** After fgAddInternal()

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB02 [0001]  1                             1    [???..???)-> BB01(1)                 (always)                     i keep internal
BB01 [0000]  1       BB02                  1    [000..008)                           (return)                     i
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

***************  Exception Handling table is empty

*************** Finishing PHASE Morph - Add internal blocks
Trees after Morph - Add internal blocks

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB02 [0001]  1                             1    [???..???)-> BB01(1)                 (always)                     i keep internal
BB01 [0000]  1       BB02                  1    [000..008)                           (return)                     i
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

------------ BB02 [0001] [???..???) -> BB01(1) (always), preds={} succs={BB01}

***** BB02 [0001]
STMT00004 ( ??? ... ??? )
               [000011] --C-G------                         *  QMARK     void  
               [000007] ----G------    if                   +--*  EQ        int   
               [000005] n---G------                         |  +--*  IND       int   
               [000004] H----------                         |  |  \--*  CNS_INT(h) long   0x3ff12d67530 global ptr
               [000006] -----------                         |  \--*  CNS_INT   int    0
               [000010] --C-G------    if                   \--*  COLON     void  
               [000008] --C-G------ else                       +--*  CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE
               [000009] ----------- then                       \--*  NOP       void  

------------ BB01 [0000] [000..008) (return), preds={BB02} succs={}

***** BB01 [0000]
STMT00000 ( 0x000[E-] ... 0x000 )
               [000000] -----------                         *  NO_OP     void  

***** BB01 [0000]
STMT00001 ( 0x001[E-] ... 0x006 )
               [000001] --C-G------                         *  CALL      void   Program:foo()

***** BB01 [0000]
STMT00002 ( 0x006[E-] ... ??? )
               [000002] -----------                         *  NO_OP     void  

***** BB01 [0000]
STMT00003 ( 0x007[E-] ... 0x007 )
               [000003] -----------                         *  RETURN    void  

-------------------------------------------------------------------------------------------------------------------
*************** In fgDebugCheckBBlist
[deferred prior check failed -- skipping this check]

*************** Starting PHASE Remove empty try

*************** In fgRemoveEmptyTry()
No EH in this method, nothing to remove.

*************** Finishing PHASE Remove empty try [no changes]

*************** Starting PHASE Remove empty finally
No EH in this method, nothing to remove.

*************** Finishing PHASE Remove empty finally [no changes]

*************** Starting PHASE Merge callfinally chains
No EH in this method, nothing to merge.

*************** Finishing PHASE Merge callfinally chains [no changes]

*************** Starting PHASE Clone finally
No EH in this method, no cloning.

*************** Finishing PHASE Clone finally [no changes]

*************** Starting PHASE Morph - Promote Structs
  promotion opt flag not enabled

*************** Finishing PHASE Morph - Promote Structs [no changes]

*************** Starting PHASE Morph - Structs/AddrExp
LocalAddressVisitor visiting statement:
STMT00004 ( ??? ... ??? )
               [000011] --C-G------                         *  QMARK     void  
               [000007] ----G------    if                   +--*  EQ        int   
               [000005] n---G------                         |  +--*  IND       int   
               [000004] H----------                         |  |  \--*  CNS_INT(h) long   0x3ff12d67530 global ptr
               [000006] -----------                         |  \--*  CNS_INT   int    0
               [000010] --C-G------    if                   \--*  COLON     void  
               [000008] --C-G------ else                       +--*  CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE
               [000009] ----------- then                       \--*  NOP       void  

LocalAddressVisitor visiting statement:
STMT00000 ( 0x000[E-] ... 0x000 )
               [000000] -----------                         *  NO_OP     void  

LocalAddressVisitor visiting statement:
STMT00001 ( 0x001[E-] ... 0x006 )
               [000001] --C-G------                         *  CALL      void   Program:foo()

LocalAddressVisitor visiting statement:
STMT00002 ( 0x006[E-] ... ??? )
               [000002] -----------                         *  NO_OP     void  

LocalAddressVisitor visiting statement:
STMT00003 ( 0x007[E-] ... 0x007 )
               [000003] -----------                         *  RETURN    void  


*************** Finishing PHASE Morph - Structs/AddrExp [no changes]

*************** Starting PHASE Early liveness

*************** Finishing PHASE Early liveness [no changes]

*************** Starting PHASE Forward Substitution

*************** Finishing PHASE Forward Substitution [no changes]

*************** Starting PHASE Physical promotion

*************** Finishing PHASE Physical promotion [no changes]

*************** Starting PHASE Identify candidates for implicit byref copy omission

*************** Finishing PHASE Identify candidates for implicit byref copy omission [no changes]

*************** Starting PHASE Morph - ByRefs

*************** Finishing PHASE Morph - ByRefs [no changes]

*************** Starting PHASE Morph - Global
compEnregLocals() is false, setting doNotEnreg flag for all locals.
Local V00 should not be enregistered because: opts.compFlags & CLFLG_REGVAR is not set

Morphing BB02

fgMorphTree BB02, STMT00004 (before)
               [000011] --C-G------                         *  QMARK     void  
               [000007] ----G------    if                   +--*  EQ        int   
               [000005] n---G------                         |  +--*  IND       int   
               [000004] H----------                         |  |  \--*  CNS_INT(h) long   0x3ff12d67530 global ptr
               [000006] -----------                         |  \--*  CNS_INT   int    0
               [000010] --C-G------    if                   \--*  COLON     void  
               [000008] --C-G------ else                       +--*  CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE
               [000009] ----------- then                       \--*  NOP       void  
Initializing arg info for 8.CALL:
Args for call [000008] CALL after AddFinalArgsAndDetermineABIInfo:

Morphing args for 8.CALL:
Args for [000008].CALL after fgMorphArgs:
OutgoingArgsStackSize is 0


Morphing BB01

fgMorphTree BB01, STMT00000 (before)
               [000000] -----------                         *  NO_OP     void  

fgMorphTree BB01, STMT00001 (before)
               [000001] --C-G------                         *  CALL      void   Program:foo()
Initializing arg info for 1.CALL:
Args for call [000001] CALL after AddFinalArgsAndDetermineABIInfo:

Morphing args for 1.CALL:
Args for [000001].CALL after fgMorphArgs:
OutgoingArgsStackSize is 0


fgMorphTree BB01, STMT00002 (before)
               [000002] -----------                         *  NO_OP     void  

fgMorphTree BB01, STMT00003 (before)
               [000003] -----------                         *  RETURN    void  

*************** Finishing PHASE Morph - Global
Trees after Morph - Global

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB02 [0001]  1                             1    [???..???)-> BB01(1)                 (always)                     i keep internal hascall
BB01 [0000]  1       BB02                  1    [000..008)                           (return)                     i hascall gcsafe
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

------------ BB02 [0001] [???..???) -> BB01(1) (always), preds={} succs={BB01}

***** BB02 [0001]
STMT00004 ( ??? ... ??? )
               [000011] --C-G+-----                         *  QMARK     void  
               [000007] J---G+-N---    if                   +--*  EQ        int   
               [000005] n---G+-----                         |  +--*  IND       int   
               [000004] H----+-----                         |  |  \--*  CNS_INT(h) long   0x3ff12d67530 global ptr
               [000006] -----+-----                         |  \--*  CNS_INT   int    0
               [000010] --C-G+?----    if                   \--*  COLON     void  
               [000008] --C-G+?---- else                       +--*  CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE
               [000009] -----+?---- then                       \--*  NOP       void  

------------ BB01 [0000] [000..008) (return), preds={BB02} succs={}

***** BB01 [0000]
STMT00000 ( 0x000[E-] ... 0x000 )
               [000000] -----+-----                         *  NO_OP     void  

***** BB01 [0000]
STMT00001 ( 0x001[E-] ... 0x006 )
               [000001] --CXG+-----                         *  CALL      void   Program:foo()

***** BB01 [0000]
STMT00002 ( 0x006[E-] ... ??? )
               [000002] -----+-----                         *  NO_OP     void  

***** BB01 [0000]
STMT00003 ( 0x007[E-] ... 0x007 )
               [000003] -----+-----                         *  RETURN    void  

-------------------------------------------------------------------------------------------------------------------
*************** In fgDebugCheckBBlist
[deferred prior check failed -- skipping this check]

*************** Starting PHASE Post-Morph

*************** In fgMarkDemotedImplicitByRefArgs()

Expanding top-level qmark in BB02 (before)

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB02 [0001]  1                             1    [???..???)-> BB01(1)                 (always)                     i keep internal hascall
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

------------ BB02 [0001] [???..???) -> BB01(1) (always), preds={} succs={BB01}

***** BB02 [0001]
STMT00004 ( ??? ... ??? )
               [000011] --C-G+-----                         *  QMARK     void  
               [000007] J---G+-N---    if                   +--*  EQ        int   
               [000005] n---G+-----                         |  +--*  IND       int   
               [000004] H----+-----                         |  |  \--*  CNS_INT(h) long   0x3ff12d67530 global ptr
               [000006] -----+-----                         |  \--*  CNS_INT   int    0
               [000010] --C-G+?----    if                   \--*  COLON     void  
               [000008] --C-G+?---- else                       +--*  CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE
               [000009] -----+?---- then                       \--*  NOP       void  

-------------------------------------------------------------------------------------------------------------------
New Basic Block BB03 [0002] created.
BB01 previous predecessor was BB02, now is BB03
setting likelihood of BB03 -> BB01 from 1 to 1
setting likelihood of BB02 -> BB03 to 1
New Basic Block BB04 [0003] created.
New Basic Block BB05 [0004] created.
setting likelihood of BB04 -> BB05 to 1
setting likelihood of BB05 -> BB03 to 1
setting likelihood of BB04 -> BB03 to 0.5
setting likelihood of BB04 -> BB05 from 1 to 0.5

removing useless STMT00004 ( ??? ... ??? )
               [000011] --C-G+-----                         *  QMARK     void  
               [000007] J---G+-N---    if                   +--*  EQ        int   
               [000005] n---G+-----                         |  +--*  IND       int   
               [000004] H----+-----                         |  |  \--*  CNS_INT(h) long   0x3ff12d67530 global ptr
               [000006] -----+-----                         |  \--*  CNS_INT   int    0
               [000010] --C-G+?----    if                   \--*  COLON     void  
               [000008] --C-G+?---- else                       +--*  CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE
               [000009] -----+?---- then                       \--*  NOP       void  
 from BB02

BB02 becomes empty

Expanding top-level qmark in BB02 (after)

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB02 [0001]  1                             1    [???..???)-> BB04(1)                 (always)                     i keep internal hascall
BB04 [0003]  1       BB02                  1    [???..???)-> BB03(0.5),BB05(0.5)     ( cond )                     internal
BB05 [0004]  1       BB04                  0.50 [???..???)-> BB03(1)                 (always)                     internal
BB03 [0002]  2       BB04,BB05             1    [???..???)-> BB01(1)                 (always)                     i keep internal hascall
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

------------ BB02 [0001] [???..???) -> BB04(1) (always), preds={} succs={BB04}

------------ BB04 [0003] [???..???) -> BB03(0.5),BB05(0.5) (cond), preds={BB02} succs={BB05,BB03}

***** BB04 [0003]
STMT00005 ( ??? ... ??? )
               [000012] ----G------                         *  JTRUE     void  
               [000007] J---G+-N---                         \--*  EQ        int   
               [000005] n---G+-----                            +--*  IND       int   
               [000004] H----+-----                            |  \--*  CNS_INT(h) long   0x3ff12d67530 global ptr
               [000006] -----+-----                            \--*  CNS_INT   int    0

------------ BB05 [0004] [???..???) -> BB03(1) (always), preds={BB04} succs={BB03}

***** BB05 [0004]
STMT00006 ( ??? ... ??? )
               [000008] --C-G+?----                         *  CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE

------------ BB03 [0002] [???..???) -> BB01(1) (always), preds={BB04,BB05} succs={BB01}

-------------------------------------------------------------------------------------------------------------------

*************** Finishing PHASE Post-Morph
Trees after Post-Morph

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB02 [0001]  1                             1    [???..???)-> BB04(1)                 (always)                     i keep internal hascall
BB04 [0003]  1       BB02                  1    [???..???)-> BB03(0.5),BB05(0.5)     ( cond )                     internal
BB05 [0004]  1       BB04                  0.50 [???..???)-> BB03(1)                 (always)                     internal
BB03 [0002]  2       BB04,BB05             1    [???..???)-> BB01(1)                 (always)                     i keep internal hascall
BB01 [0000]  1       BB03                  1    [000..008)                           (return)                     i hascall gcsafe
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

------------ BB02 [0001] [???..???) -> BB04(1) (always), preds={} succs={BB04}

------------ BB04 [0003] [???..???) -> BB03(0.5),BB05(0.5) (cond), preds={BB02} succs={BB05,BB03}

***** BB04 [0003]
STMT00005 ( ??? ... ??? )
               [000012] ----G------                         *  JTRUE     void  
               [000007] J---G+-N---                         \--*  EQ        int   
               [000005] n---G+-----                            +--*  IND       int   
               [000004] H----+-----                            |  \--*  CNS_INT(h) long   0x3ff12d67530 global ptr
               [000006] -----+-----                            \--*  CNS_INT   int    0

------------ BB05 [0004] [???..???) -> BB03(1) (always), preds={BB04} succs={BB03}

***** BB05 [0004]
STMT00006 ( ??? ... ??? )
               [000008] --C-G+?----                         *  CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE

------------ BB03 [0002] [???..???) -> BB01(1) (always), preds={BB04,BB05} succs={BB01}

------------ BB01 [0000] [000..008) (return), preds={BB03} succs={}

***** BB01 [0000]
STMT00000 ( 0x000[E-] ... 0x000 )
               [000000] -----+-----                         *  NO_OP     void  

***** BB01 [0000]
STMT00001 ( 0x001[E-] ... 0x006 )
               [000001] --CXG+-----                         *  CALL      void   Program:foo()

***** BB01 [0000]
STMT00002 ( 0x006[E-] ... ??? )
               [000002] -----+-----                         *  NO_OP     void  

***** BB01 [0000]
STMT00003 ( 0x007[E-] ... 0x007 )
               [000003] -----+-----                         *  RETURN    void  

-------------------------------------------------------------------------------------------------------------------
*************** In fgDebugCheckBBlist
[deferred prior check failed -- skipping this check]

*************** Before renumbering the basic blocks

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB02 [0001]  1                             1    [???..???)-> BB04(1)                 (always)                     i keep internal hascall
BB04 [0003]  1       BB02                  1    [???..???)-> BB03(0.5),BB05(0.5)     ( cond )                     internal
BB05 [0004]  1       BB04                  0.50 [???..???)-> BB03(1)                 (always)                     internal
BB03 [0002]  2       BB04,BB05             1    [???..???)-> BB01(1)                 (always)                     i keep internal hascall
BB01 [0000]  1       BB03                  1    [000..008)                           (return)                     i hascall gcsafe
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

***************  Exception Handling table is empty
Renumber BB02 to BB01
Renumber BB04 to BB02
Renumber BB05 to BB03
Renumber BB03 to BB04
Renumber BB01 to BB05

*************** After renumbering the basic blocks

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB01 [0001]  1                             1    [???..???)-> BB02(1)                 (always)                     i keep internal hascall
BB02 [0003]  1       BB01                  1    [???..???)-> BB04(0.5),BB03(0.5)     ( cond )                     internal
BB03 [0004]  1       BB02                  0.50 [???..???)-> BB04(1)                 (always)                     internal
BB04 [0002]  2       BB02,BB03             1    [???..???)-> BB05(1)                 (always)                     i keep internal hascall
BB05 [0000]  1       BB04                  1    [000..008)                           (return)                     i hascall gcsafe
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

***************  Exception Handling table is empty

New BlockSet epoch 2, # of blocks (including unused BB00): 6, bitset array size: 1 (short)

*************** Starting PHASE GS Cookie
No GS security needed

*************** Finishing PHASE GS Cookie [no changes]

*************** Starting PHASE Compute block weights

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB01 [0001]  1                             1    [???..???)-> BB02(1)                 (always)                     i keep internal hascall
BB02 [0003]  1       BB01                  1    [???..???)-> BB04(0.5),BB03(0.5)     ( cond )                     internal
BB03 [0004]  1       BB02                  0.50 [???..???)-> BB04(1)                 (always)                     internal
BB04 [0002]  2       BB02,BB03             1    [???..???)-> BB05(1)                 (always)                     i keep internal hascall
BB05 [0000]  1       BB04                  1    [000..008)                           (return)                     i hascall gcsafe
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

 -- no profile data, so using default called count

*************** Finishing PHASE Compute block weights [no changes]

*************** Starting PHASE Create EH funclets

*************** Finishing PHASE Create EH funclets [no changes]

*************** Starting PHASE Morph array ops
No multi-dimensional array references in the function

*************** Finishing PHASE Morph array ops [no changes]

*************** Starting PHASE Mark local vars

*************** In lvaMarkLocalVars()
*** lvaComputeRefCounts ***

*************** Finishing PHASE Mark local vars [no changes]

*************** Starting PHASE Find oper order
*************** In fgFindOperOrder()

*************** Finishing PHASE Find oper order
Trees after Find oper order

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB01 [0001]  1                             1    [???..???)-> BB02(1)                 (always)                     i keep internal hascall
BB02 [0003]  1       BB01                  1    [???..???)-> BB04(0.5),BB03(0.5)     ( cond )                     internal
BB03 [0004]  1       BB02                  0.50 [???..???)-> BB04(1)                 (always)                     internal
BB04 [0002]  2       BB02,BB03             1    [???..???)-> BB05(1)                 (always)                     i keep internal hascall
BB05 [0000]  1       BB04                  1    [000..008)                           (return)                     i hascall gcsafe
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

------------ BB01 [0001] [???..???) -> BB02(1) (always), preds={} succs={BB02}

------------ BB02 [0003] [???..???) -> BB04(0.5),BB03(0.5) (cond), preds={BB01} succs={BB03,BB04}

***** BB02 [0003]
STMT00005 ( ??? ... ??? )
               [000012] ----G------                         *  JTRUE     void  
               [000007] J---G+-N---                         \--*  EQ        int   
               [000005] n---G+-----                            +--*  IND       int   
               [000004] H----+-----                            |  \--*  CNS_INT(h) long   0x3ff12d67530 global ptr
               [000006] -----+-----                            \--*  CNS_INT   int    0

------------ BB03 [0004] [???..???) -> BB04(1) (always), preds={BB02} succs={BB04}

***** BB03 [0004]
STMT00006 ( ??? ... ??? )
               [000008] --C-G+?----                         *  CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE

------------ BB04 [0002] [???..???) -> BB05(1) (always), preds={BB02,BB03} succs={BB05}

------------ BB05 [0000] [000..008) (return), preds={BB04} succs={}

***** BB05 [0000]
STMT00000 ( 0x000[E-] ... 0x000 )
               [000000] -----+-----                         *  NO_OP     void  

***** BB05 [0000]
STMT00001 ( 0x001[E-] ... 0x006 )
               [000001] --CXG+-----                         *  CALL      void   Program:foo()

***** BB05 [0000]
STMT00002 ( 0x006[E-] ... ??? )
               [000002] -----+-----                         *  NO_OP     void  

***** BB05 [0000]
STMT00003 ( 0x007[E-] ... 0x007 )
               [000003] -----+-----                         *  RETURN    void  

-------------------------------------------------------------------------------------------------------------------
*************** In fgDebugCheckBBlist
[deferred prior check failed -- skipping this check]

*************** Starting PHASE Set block order
*************** In fgSetBlockOrder()
The biggest BB has    5 tree nodes

*************** Finishing PHASE Set block order
Trees after Set block order

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB01 [0001]  1                             1    [???..???)-> BB02(1)                 (always)                     i keep internal hascall
BB02 [0003]  1       BB01                  1    [???..???)-> BB04(0.5),BB03(0.5)     ( cond )                     internal
BB03 [0004]  1       BB02                  0.50 [???..???)-> BB04(1)                 (always)                     internal
BB04 [0002]  2       BB02,BB03             1    [???..???)-> BB05(1)                 (always)                     i keep internal hascall
BB05 [0000]  1       BB04                  1    [000..008)                           (return)                     i hascall gcsafe
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

------------ BB01 [0001] [???..???) -> BB02(1) (always), preds={} succs={BB02}

------------ BB02 [0003] [???..???) -> BB04(0.5),BB03(0.5) (cond), preds={BB01} succs={BB03,BB04}

***** BB02 [0003]
STMT00005 ( ??? ... ??? )
N005 (???,???) [000012] ----G------                         *  JTRUE     void  
N004 (???,???) [000007] J---G+-N---                         \--*  EQ        int   
N002 (???,???) [000005] n---G+-----                            +--*  IND       int   
N001 (???,???) [000004] H----+-----                            |  \--*  CNS_INT(h) long   0x3ff12d67530 global ptr
N003 (???,???) [000006] -----+-----                            \--*  CNS_INT   int    0

------------ BB03 [0004] [???..???) -> BB04(1) (always), preds={BB02} succs={BB04}

***** BB03 [0004]
STMT00006 ( ??? ... ??? )
N001 (???,???) [000008] --C-G+?----                         *  CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE

------------ BB04 [0002] [???..???) -> BB05(1) (always), preds={BB02,BB03} succs={BB05}

------------ BB05 [0000] [000..008) (return), preds={BB04} succs={}

***** BB05 [0000]
STMT00000 ( 0x000[E-] ... 0x000 )
N001 (???,???) [000000] -----+-----                         *  NO_OP     void  

***** BB05 [0000]
STMT00001 ( 0x001[E-] ... 0x006 )
N001 (???,???) [000001] --CXG+-----                         *  CALL      void   Program:foo()

***** BB05 [0000]
STMT00002 ( 0x006[E-] ... ??? )
N001 (???,???) [000002] -----+-----                         *  NO_OP     void  

***** BB05 [0000]
STMT00003 ( 0x007[E-] ... 0x007 )
N001 (???,???) [000003] -----+-----                         *  RETURN    void  

-------------------------------------------------------------------------------------------------------------------
*************** In fgDebugCheckBBlist
[deferred prior check failed -- skipping this check]

*************** Starting PHASE Stress gtSplitTree

*************** Finishing PHASE Stress gtSplitTree [no changes]

*************** Starting PHASE Expand casts

*************** Finishing PHASE Expand casts [no changes]

*************** Starting PHASE Expand runtime lookups

*************** Finishing PHASE Expand runtime lookups [no changes]

*************** Starting PHASE Expand static init
Nothing to expand.

*************** Finishing PHASE Expand static init [no changes]

*************** Starting PHASE Expand TLS access
Nothing to expand.

*************** Finishing PHASE Expand TLS access [no changes]

*************** Starting PHASE Insert GC Polls

*************** Finishing PHASE Insert GC Polls [no changes]

*************** Starting PHASE Create throw helper blocks

*************** Finishing PHASE Create throw helper blocks [no changes]

*************** Starting PHASE Determine first cold block
No procedure splitting will be done for this method

*************** Finishing PHASE Determine first cold block [no changes]

*************** Starting PHASE Rationalize IR

*************** Finishing PHASE Rationalize IR
Trees after Rationalize IR

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB01 [0001]  1                             1    [???..???)-> BB02(1)                 (always)                     i LIR keep internal hascall
BB02 [0003]  1       BB01                  1    [???..???)-> BB04(0.5),BB03(0.5)     ( cond )                     LIR internal
BB03 [0004]  1       BB02                  0.50 [???..???)-> BB04(1)                 (always)                     LIR internal
BB04 [0002]  2       BB02,BB03             1    [???..???)-> BB05(1)                 (always)                     i LIR keep internal hascall
BB05 [0000]  1       BB04                  1    [000..008)                           (return)                     i LIR hascall gcsafe
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

------------ BB01 [0001] [???..???) -> BB02(1) (always), preds={} succs={BB02}

------------ BB02 [0003] [???..???) -> BB04(0.5),BB03(0.5) (cond), preds={BB01} succs={BB03,BB04}
N001 (???,???) [000004] H----+-----                    t4 =    CNS_INT(h) long   0x3ff12d67530 global ptr
                                                            /--*  t4     long   
N002 (???,???) [000005] n---G+-----                    t5 = *  IND       int   
N003 (???,???) [000006] -----+-----                    t6 =    CNS_INT   int    0
                                                            /--*  t5     int    
                                                            +--*  t6     int    
N004 (???,???) [000007] J---G+-N---                    t7 = *  EQ        int   
                                                            /--*  t7     int    
N005 (???,???) [000012] ----G------                         *  JTRUE     void  

------------ BB03 [0004] [???..???) -> BB04(1) (always), preds={BB02} succs={BB04}
N001 (???,???) [000008] --C-G+?----                            CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE

------------ BB04 [0002] [???..???) -> BB05(1) (always), preds={BB02,BB03} succs={BB05}

------------ BB05 [0000] [000..008) (return), preds={BB04} succs={}
               [000013] -----------                            IL_OFFSET void   INLRT @ 0x000[E-]
N001 (???,???) [000000] -----+-----                            NO_OP     void  
               [000014] -----------                            IL_OFFSET void   INLRT @ 0x001[E-]
N001 (???,???) [000001] --CXG+-----                            CALL      void   Program:foo()
               [000015] -----------                            IL_OFFSET void   INLRT @ 0x006[E-]
N001 (???,???) [000002] -----+-----                            NO_OP     void  
               [000016] -----------                            IL_OFFSET void   INLRT @ 0x007[E-]
N001 (???,???) [000003] -----+-----                            RETURN    void  

-------------------------------------------------------------------------------------------------------------------
*************** In fgDebugCheckBBlist
[deferred prior check failed -- skipping this check]

*************** Starting PHASE Lowering nodeinfo
compEnregLocals() is false, setting doNotEnreg flag for all locals.
Local V00 should not be enregistered because: opts.compFlags & CLFLG_REGVAR is not set
Lowering JTRUE:
N001 (???,???) [000004] H----+-----                    t4 =    CNS_INT(h) long   0x3ff12d67530 global ptr
                                                            /--*  t4     long   
N002 (???,???) [000005] n---G+-----                    t5 = *  IND       int   
N003 (???,???) [000006] -c---+-----                    t6 =    CNS_INT   int    0
                                                            /--*  t5     int    
                                                            +--*  t6     int    
N004 (???,???) [000007] J---G+-N---                    t7 = *  EQ        int   
                                                            /--*  t7     int    
N005 (???,???) [000012] ----G------                         *  JTRUE     void  

Lowering condition:
N001 (???,???) [000004] H----+-----                    t4 =    CNS_INT(h) long   0x3ff12d67530 global ptr
                                                            /--*  t4     long   
N002 (???,???) [000005] n---G+-----                    t5 = *  IND       int   
N003 (???,???) [000006] -c---+-----                    t6 =    CNS_INT   int    0
                                                            /--*  t5     int    
                                                            +--*  t6     int    
N004 (???,???) [000007] J---G+-N---                    t7 = *  EQ        int   

Lowering JTRUE Result:
N001 (???,???) [000004] H----+-----                    t4 =    CNS_INT(h) long   0x3ff12d67530 global ptr
                                                            /--*  t4     long   
N002 (???,???) [000005] n---G+-----                    t5 = *  IND       int   
N003 (???,???) [000006] -c---+-----                    t6 =    CNS_INT   int    0
                                                            /--*  t5     int    
                                                            +--*  t6     int    
N004 (???,???) [000007] ----G+-N---                         *  CMP       void  
N005 (???,???) [000012] ----G------                            JCC       void   cond=UEQ

lowering call (before):
N001 (???,???) [000008] --C-G+?----                            CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE

args:
======

late:
======
lowering call (after):
N001 (???,???) [000008] --C-G+?----                            CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE

lowering call (before):
N001 (???,???) [000001] --CXG+-----                            CALL      void   Program:foo()

args:
======

late:
======
lowering call (after):
N001 (???,???) [000001] --CXG+-----                            CALL      void   Program:foo()

lowering return node
N001 (???,???) [000003] -----+-----                         *  RETURN    void  
============
Lower has completed modifying nodes.

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB01 [0001]  1                             1    [???..???)-> BB02(1)                 (always)                     i LIR keep internal hascall
BB02 [0003]  1       BB01                  1    [???..???)-> BB04(0.5),BB03(0.5)     ( cond )                     LIR internal
BB03 [0004]  1       BB02                  0.50 [???..???)-> BB04(1)                 (always)                     LIR internal
BB04 [0002]  2       BB02,BB03             1    [???..???)-> BB05(1)                 (always)                     i LIR keep internal hascall
BB05 [0000]  1       BB04                  1    [000..008)                           (return)                     i LIR hascall gcsafe
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

------------ BB01 [0001] [???..???) -> BB02(1) (always), preds={} succs={BB02}

------------ BB02 [0003] [???..???) -> BB04(0.5),BB03(0.5) (cond), preds={BB01} succs={BB03,BB04}
N001 (???,???) [000004] H----+-----                    t4 =    CNS_INT(h) long   0x3ff12d67530 global ptr
                                                            /--*  t4     long   
N002 (???,???) [000005] n---G+-----                    t5 = *  IND       int   
N003 (???,???) [000006] -c---+-----                    t6 =    CNS_INT   int    0
                                                            /--*  t5     int    
                                                            +--*  t6     int    
N004 (???,???) [000007] ----G+-N---                         *  CMP       void  
N005 (???,???) [000012] ----G------                            JCC       void   cond=UEQ

------------ BB03 [0004] [???..???) -> BB04(1) (always), preds={BB02} succs={BB04}
N001 (???,???) [000008] --C-G+?----                            CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE

------------ BB04 [0002] [???..???) -> BB05(1) (always), preds={BB02,BB03} succs={BB05}

------------ BB05 [0000] [000..008) (return), preds={BB04} succs={}
               [000013] -----------                            IL_OFFSET void   INLRT @ 0x000[E-]
N001 (???,???) [000000] -----+-----                            NO_OP     void  
               [000014] -----------                            IL_OFFSET void   INLRT @ 0x001[E-]
N001 (???,???) [000001] --CXG+-----                            CALL      void   Program:foo()
               [000015] -----------                            IL_OFFSET void   INLRT @ 0x006[E-]
N001 (???,???) [000002] -----+-----                            NO_OP     void  
               [000016] -----------                            IL_OFFSET void   INLRT @ 0x007[E-]
N001 (???,???) [000003] -----+-----                            RETURN    void  

-------------------------------------------------------------------------------------------------------------------

*** lvaComputeRefCounts ***

*************** Finishing PHASE Lowering nodeinfo
Trees after Lowering nodeinfo

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB01 [0001]  1                             1    [???..???)-> BB02(1)                 (always)                     i LIR keep internal hascall
BB02 [0003]  1       BB01                  1    [???..???)-> BB04(0.5),BB03(0.5)     ( cond )                     LIR internal
BB03 [0004]  1       BB02                  0.50 [???..???)-> BB04(1)                 (always)                     LIR internal
BB04 [0002]  2       BB02,BB03             1    [???..???)-> BB05(1)                 (always)                     i LIR keep internal hascall
BB05 [0000]  1       BB04                  1    [000..008)                           (return)                     i LIR hascall gcsafe
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

------------ BB01 [0001] [???..???) -> BB02(1) (always), preds={} succs={BB02}

------------ BB02 [0003] [???..???) -> BB04(0.5),BB03(0.5) (cond), preds={BB01} succs={BB03,BB04}
N001 (???,???) [000004] H----+-----                    t4 =    CNS_INT(h) long   0x3ff12d67530 global ptr
                                                            /--*  t4     long   
N002 (???,???) [000005] n---G+-----                    t5 = *  IND       int   
N003 (???,???) [000006] -c---+-----                    t6 =    CNS_INT   int    0
                                                            /--*  t5     int    
                                                            +--*  t6     int    
N004 (???,???) [000007] ----G+-N---                         *  CMP       void  
N005 (???,???) [000012] ----G------                            JCC       void   cond=UEQ

------------ BB03 [0004] [???..???) -> BB04(1) (always), preds={BB02} succs={BB04}
N001 (???,???) [000008] --C-G+?----                            CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE

------------ BB04 [0002] [???..???) -> BB05(1) (always), preds={BB02,BB03} succs={BB05}

------------ BB05 [0000] [000..008) (return), preds={BB04} succs={}
               [000013] -----------                            IL_OFFSET void   INLRT @ 0x000[E-]
N001 (???,???) [000000] -----+-----                            NO_OP     void  
               [000014] -----------                            IL_OFFSET void   INLRT @ 0x001[E-]
N001 (???,???) [000001] --CXG+-----                            CALL      void   Program:foo()
               [000015] -----------                            IL_OFFSET void   INLRT @ 0x006[E-]
N001 (???,???) [000002] -----+-----                            NO_OP     void  
               [000016] -----------                            IL_OFFSET void   INLRT @ 0x007[E-]
N001 (???,???) [000003] -----+-----                            RETURN    void  

-------------------------------------------------------------------------------------------------------------------
*************** In fgDebugCheckBBlist
[deferred prior check failed -- skipping this check]

*************** Starting PHASE Calculate stack level slots

*************** Finishing PHASE Calculate stack level slots [no changes]

*************** Starting PHASE Linear scan register alloc
Clearing modified regs.

buildIntervals ========

-----------------
LIVENESS:
-----------------
BB01
use: {}
def: {}
 in: {}
out: {}
BB02
use: {}
def: {}
 in: {}
out: {}
BB03
use: {}
def: {}
 in: {}
out: {}
BB04
use: {}
def: {}
 in: {}
out: {}
BB05
use: {}
def: {}
 in: {}
out: {}

FP callee save candidate vars: None

floatVarCount = 0; hasLoops = false, singleExit = true
; Decided to create an EBP based frame for ETW stackwalking (Debug Code)
TUPLE STYLE DUMP BEFORE LSRA
Start LSRA Block Sequence: 
Current block: BB01
	Succ block: BB02, Criteria: weight, Worklist: [BB02 ]
Current block: BB02
	Succ block: BB03, Criteria: weight, Worklist: [BB03 ]
	Succ block: BB04, Criteria: weight, Worklist: [BB03 BB04 ]
Current block: BB03
Current block: BB04
	Succ block: BB05, Criteria: bbNum, Worklist: [BB05 ]
Current block: BB05
Final LSRA Block Sequence:
BB01 (  1   )
BB02 (  1   ) critical-out
BB03 (  0.50)
BB04 (  1   ) critical-in
BB05 (  1   )

BB01 [0001] [???..???) -> BB02(1) (always), preds={} succs={BB02}
=====

BB02 [0003] [???..???) -> BB04(0.5),BB03(0.5) (cond), preds={BB01} succs={BB03,BB04}
=====
  N001. t4              =  CNS_INT(h) 0x3ff12d67530 global ptr
  N002. t5              =  IND      ; t4
  N003.                    CNS_INT   0
  N004.                    CMP      ; t5
  N005.                    JCC       cond=UEQ

BB03 [0004] [???..???) -> BB04(1) (always), preds={BB02} succs={BB04}
=====
  N001.                    CALL help

BB04 [0002] [???..???) -> BB05(1) (always), preds={BB02,BB03} succs={BB05}
=====

BB05 [0000] [000..008) (return), preds={BB04} succs={}
=====
  N000.                    IL_OFFSET INLRT @ 0x000[E-]
  N001.                    NO_OP    
  N000.                    IL_OFFSET INLRT @ 0x001[E-]
  N001.                    CALL     
  N000.                    IL_OFFSET INLRT @ 0x006[E-]
  N001.                    NO_OP    
  N000.                    IL_OFFSET INLRT @ 0x007[E-]
  N001.                    RETURN   




buildIntervals second part ========

NEW BLOCK BB01
<RefPosition #0   @0   RefTypeBB BB01 regmask=[] minReg=1 wt=100.00>

<RefPosition #1   @3   RefTypeKill BB01 regmask=[r1-r5 r9 f0-f7] minReg=1>

NEW BLOCK BB02


Setting BB01 as the predecessor for determining incoming variable registers of BB02
<RefPosition #2   @4   RefTypeBB BB02 regmask=[] minReg=1 wt=100.00>

DefList: {  }
N006 (???,???) [000004] H----+-----                         *  CNS_INT(h) long   0x3ff12d67530 global ptr REG NA
Interval  0: long RefPositions {} physReg:NA Preferences=[allInt] Aversions=[]
<RefPosition #3   @7   RefTypeDef <Ivl:0> CNS_INT BB02 regmask=[allInt] minReg=1 wt=400.00>

DefList: { N006.t4. CNS_INT }
N008 (???,???) [000005] n---G+-----                         *  IND       int    REG NA
<RefPosition #4   @8   RefTypeUse <Ivl:0> BB02 regmask=[allInt] minReg=1 last wt=100.00>
Interval  1: int RefPositions {} physReg:NA Preferences=[allInt] Aversions=[]
<RefPosition #5   @9   RefTypeDef <Ivl:1> IND BB02 regmask=[allInt] minReg=1 wt=400.00>

DefList: { N008.t5. IND }
N010 (???,???) [000006] -c---+-----                         *  CNS_INT   int    0 REG NA
Contained
DefList: { N008.t5. IND }
N012 (???,???) [000007] ----G+-N---                         *  CMP       void   REG NA
<RefPosition #6   @12  RefTypeUse <Ivl:1> BB02 regmask=[allInt] minReg=1 last wt=100.00>

DefList: {  }
N014 (???,???) [000012] ----G------                         *  JCC       void   cond=UEQ REG NA


NEW BLOCK BB03


Setting BB02 as the predecessor for determining incoming variable registers of BB03
<RefPosition #7   @16  RefTypeBB BB03 regmask=[] minReg=1 wt=50.00>

DefList: {  }
N018 (???,???) [000008] --C-G+?----                         *  CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE REG NA
<RefPosition #8   @19  RefTypeKill BB03 regmask=[r1-r5] minReg=1>


NEW BLOCK BB04


Setting BB02 as the predecessor for determining incoming variable registers of BB04
<RefPosition #9   @20  RefTypeBB BB04 regmask=[] minReg=1 wt=100.00>


NEW BLOCK BB05


Setting BB04 as the predecessor for determining incoming variable registers of BB05
<RefPosition #10  @22  RefTypeBB BB05 regmask=[] minReg=1 wt=100.00>

DefList: {  }
N024 (???,???) [000013] -----------                         *  IL_OFFSET void   INLRT @ 0x000[E-] REG NA

DefList: {  }
N026 (???,???) [000000] -----+-----                         *  NO_OP     void   REG NA

DefList: {  }
N028 (???,???) [000014] -----------                         *  IL_OFFSET void   INLRT @ 0x001[E-] REG NA

DefList: {  }
N030 (???,???) [000001] --CXG+-----                         *  CALL      void   Program:foo() REG NA
<RefPosition #11  @31  RefTypeKill BB05 regmask=[r1-r5] minReg=1>

DefList: {  }
N032 (???,???) [000015] -----------                         *  IL_OFFSET void   INLRT @ 0x006[E-] REG NA

DefList: {  }
N034 (???,???) [000002] -----+-----                         *  NO_OP     void   REG NA

DefList: {  }
N036 (???,???) [000016] -----------                         *  IL_OFFSET void   INLRT @ 0x007[E-] REG NA

DefList: {  }
N038 (???,???) [000003] -----+-----                         *  RETURN    void   REG NA


Linear scan intervals BEFORE VALIDATING INTERVALS:
Interval  0: long (constant) RefPositions {#3@7 #4@8} physReg:NA Preferences=[allInt] Aversions=[]
Interval  1: int RefPositions {#5@9 #6@12} physReg:NA Preferences=[allInt] Aversions=[]

------------
REFPOSITIONS BEFORE VALIDATING INTERVALS: 
------------
<RefPosition #0   @0   RefTypeBB BB01 regmask=[] minReg=1 wt=100.00>
<RefPosition #1   @3   RefTypeKill BB01 regmask=[r1-r5 r9 f0-f7] minReg=1>
<RefPosition #2   @4   RefTypeBB BB02 regmask=[] minReg=1 wt=100.00>
<RefPosition #3   @7   RefTypeDef <Ivl:0> CNS_INT BB02 regmask=[allInt] minReg=1 wt=400.00>
<RefPosition #4   @8   RefTypeUse <Ivl:0> BB02 regmask=[allInt] minReg=1 last wt=100.00>
<RefPosition #5   @9   RefTypeDef <Ivl:1> IND BB02 regmask=[allInt] minReg=1 wt=400.00>
<RefPosition #6   @12  RefTypeUse <Ivl:1> BB02 regmask=[allInt] minReg=1 last wt=100.00>
<RefPosition #7   @16  RefTypeBB BB03 regmask=[] minReg=1 wt=50.00>
<RefPosition #8   @19  RefTypeKill BB03 regmask=[r1-r5] minReg=1>
<RefPosition #9   @20  RefTypeBB BB04 regmask=[] minReg=1 wt=100.00>
<RefPosition #10  @22  RefTypeBB BB05 regmask=[] minReg=1 wt=100.00>
<RefPosition #11  @31  RefTypeKill BB05 regmask=[r1-r5] minReg=1>
TUPLE STYLE DUMP WITH REF POSITIONS
Incoming Parameters: 
BB01 [0001] [???..???) -> BB02(1) (always), preds={} succs={BB02}
=====

  N006.                    CNS_INT(h) 0x3ff12d67530 global ptr
  N008.                    IND      
  N010.                    CNS_INT   0
  N012.                    CMP      
  N014.                    JCC       cond=UEQ

  N018.                    CALL help


  N024.                    IL_OFFSET INLRT @ 0x000[E-]
  N026.                    NO_OP    
  N028.                    IL_OFFSET INLRT @ 0x001[E-]
  N030.                    CALL     
  N032.                    IL_OFFSET INLRT @ 0x006[E-]
  N034.                    NO_OP    
  N036.                    IL_OFFSET INLRT @ 0x007[E-]
  N038.                    RETURN   




Linear scan intervals after buildIntervals:
Interval  0: long (constant) RefPositions {#3@7 #4@8} physReg:NA Preferences=[allInt] Aversions=[]
Interval  1: int RefPositions {#5@9 #6@12} physReg:NA Preferences=[allInt] Aversions=[]

*************** In LinearScan::allocateRegistersMinimal()

Linear scan intervals before allocateRegistersMinimal:
Interval  0: long (constant) RefPositions {#3@7 #4@8} physReg:NA Preferences=[allInt] Aversions=[]
Interval  1: int RefPositions {#5@9 #6@12} physReg:NA Preferences=[allInt] Aversions=[]

------------
REFPOSITIONS BEFORE ALLOCATION: 
------------
<RefPosition #0   @0   RefTypeBB BB01 regmask=[] minReg=1 wt=100.00>
<RefPosition #1   @3   RefTypeKill BB01 regmask=[r1-r5 r9 f0-f7] minReg=1>
<RefPosition #2   @4   RefTypeBB BB02 regmask=[] minReg=1 wt=100.00>
<RefPosition #3   @7   RefTypeDef <Ivl:0> CNS_INT BB02 regmask=[allInt] minReg=1 wt=400.00>
<RefPosition #4   @8   RefTypeUse <Ivl:0> BB02 regmask=[allInt] minReg=1 last wt=100.00>
<RefPosition #5   @9   RefTypeDef <Ivl:1> IND BB02 regmask=[allInt] minReg=1 wt=400.00>
<RefPosition #6   @12  RefTypeUse <Ivl:1> BB02 regmask=[allInt] minReg=1 last wt=100.00>
<RefPosition #7   @16  RefTypeBB BB03 regmask=[] minReg=1 wt=50.00>
<RefPosition #8   @19  RefTypeKill BB03 regmask=[r1-r5] minReg=1>
<RefPosition #9   @20  RefTypeBB BB04 regmask=[] minReg=1 wt=100.00>
<RefPosition #10  @22  RefTypeBB BB05 regmask=[] minReg=1 wt=100.00>
<RefPosition #11  @31  RefTypeKill BB05 regmask=[r1-r5] minReg=1>


Allocating Registers
--------------------
The following table has one or more rows for each RefPosition that is handled during allocation.
The columns are: (1) Loc: LSRA location, (2) RP#: RefPosition number, (3) Name, (4) Type (e.g. Def, Use,
Fixd, Parm, DDef (Dummy Def), ExpU (Exposed Use), Kill) followed by a '*' if it is a last use, and a 'D'
if it is delayRegFree, (5) Action taken during allocation. Some actions include (a) Alloc a new register,
(b) Keep an existing register, (c) Spill a register, (d) ReLod (Reload) a register. If an ALL-CAPS name
such as COVRS is displayed, it is a score name from lsra_score.h, with a trailing '(A)' indicating alloc,
'(C)' indicating copy, and '(R)' indicating re-use. See dumpLsraAllocationEvent() for details.
The subsequent columns show the Interval occupying each register, if any, followed by 'a' if it is
active, 'p' if it is a large vector that has been partially spilled, and 'i' if it is inactive.
Columns are only printed up to the last modified register, which may increase during allocation,
in which case additional columns will appear. Registers which are not marked modified have ---- in
their column.

------------------------------------------+----+----+----+----+----+----+----+----+----+----+
TreeID   LocRP# Name Type  Action    Reg  |r0  |r1  |r2  |r3  |r4  |r5  |r6  |r7  |r8  |r9  |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
                                          |    |    |    |    |    |    |    |    |    |    |
          0.#0  BB1 PredBB0               |    |    |    |    |    |    |    |    |    |    |
          3.#1       Kill   None     [r1-r5 r9 f0-f7]
                                          |    |    |    |    |    |    |    |    |    |    |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
TreeID   LocRP# Name Type  Action    Reg  |r0  |r1  |r2  |r3  |r4  |r5  |r6  |r7  |r8  |r9  |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
          4.#2  BB2 PredBB1               |    |    |    |    |    |    |    |    |    |    |
[000004]  7.#3  C0   Def    ORDER(A) r1   |    |C0 a|    |    |    |    |    |    |    |    |
[000005]  8.#4  C0   Use *  Keep     r1   |    |C0 a|    |    |    |    |    |    |    |    |
          9.#5  I1   Def    ORDER(A) r1   |    |I1 a|    |    |    |    |    |    |    |    |
[000007] 12.#6  I1   Use *  Keep     r1   |    |I1 a|    |    |    |    |    |    |    |    |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
TreeID   LocRP# Name Type  Action    Reg  |r0  |r1  |r2  |r3  |r4  |r5  |r6  |r7  |r8  |r9  |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
         16.#7  BB3 PredBB2               |    |    |    |    |    |    |    |    |    |    |
[000008] 19.#8       Kill   None     [r1-r5]
                                          |    |    |    |    |    |    |    |    |    |    |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
TreeID   LocRP# Name Type  Action    Reg  |r0  |r1  |r2  |r3  |r4  |r5  |r6  |r7  |r8  |r9  |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
         20.#9  BB4 PredBB2               |    |    |    |    |    |    |    |    |    |    |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
TreeID   LocRP# Name Type  Action    Reg  |r0  |r1  |r2  |r3  |r4  |r5  |r6  |r7  |r8  |r9  |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
         22.#10 BB5 PredBB4               |    |    |    |    |    |    |    |    |    |    |
[000001] 31.#11      Kill   None     [r1-r5]
                                          |    |    |    |    |    |    |    |    |    |    |

------------
REFPOSITIONS AFTER ALLOCATION: 
------------
<RefPosition #0   @0   RefTypeBB BB01 regmask=[] minReg=1 wt=100.00>
<RefPosition #1   @3   RefTypeKill BB01 regmask=[r1-r5 r9 f0-f7] minReg=1>
<RefPosition #2   @4   RefTypeBB BB02 regmask=[] minReg=1 wt=100.00>
<RefPosition #3   @7   RefTypeDef <Ivl:0> CNS_INT BB02 regmask=[r1] minReg=1 wt=400.00>
<RefPosition #4   @8   RefTypeUse <Ivl:0> BB02 regmask=[r1] minReg=1 last wt=100.00>
<RefPosition #5   @9   RefTypeDef <Ivl:1> IND BB02 regmask=[r1] minReg=1 wt=400.00>
<RefPosition #6   @12  RefTypeUse <Ivl:1> BB02 regmask=[r1] minReg=1 last wt=100.00>
<RefPosition #7   @16  RefTypeBB BB03 regmask=[] minReg=1 wt=50.00>
<RefPosition #8   @19  RefTypeKill BB03 regmask=[r1-r5] minReg=1>
<RefPosition #9   @20  RefTypeBB BB04 regmask=[] minReg=1 wt=100.00>
<RefPosition #10  @22  RefTypeBB BB05 regmask=[] minReg=1 wt=100.00>
<RefPosition #11  @31  RefTypeKill BB05 regmask=[r1-r5] minReg=1>
Active intervals at end of allocation:

Trees after linear scan register allocator (LSRA)

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB01 [0001]  1                             1    [???..???)-> BB02(1)                 (always)                     i LIR keep internal hascall
BB02 [0003]  1       BB01                  1    [???..???)-> BB04(0.5),BB03(0.5)     ( cond )                     LIR internal
BB03 [0004]  1       BB02                  0.50 [???..???)-> BB04(1)                 (always)                     LIR internal
BB04 [0002]  2       BB02,BB03             1    [???..???)-> BB05(1)                 (always)                     i LIR keep internal hascall
BB05 [0000]  1       BB04                  1    [000..008)                           (return)                     i LIR hascall gcsafe
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

------------ BB01 [0001] [???..???) -> BB02(1) (always), preds={} succs={BB02}

------------ BB02 [0003] [???..???) -> BB04(0.5),BB03(0.5) (cond), preds={BB01} succs={BB03,BB04}
N006 (???,???) [000004] H----+-----                    t4 =    CNS_INT(h) long   0x3ff12d67530 global ptr REG r1
                                                            /--*  t4     long   
N008 (???,???) [000005] n---G+-----                    t5 = *  IND       int    REG r1
N010 (???,???) [000006] -c---+-----                    t6 =    CNS_INT   int    0 REG NA
                                                            /--*  t5     int    
                                                            +--*  t6     int    
N012 (???,???) [000007] ----G+-N---                         *  CMP       void   REG NA
N014 (???,???) [000012] ----G------                            JCC       void   cond=UEQ REG NA

------------ BB03 [0004] [???..???) -> BB04(1) (always), preds={BB02} succs={BB04}
N018 (???,???) [000008] --C-G+?----                            CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE REG NA

------------ BB04 [0002] [???..???) -> BB05(1) (always), preds={BB02,BB03} succs={BB05}

------------ BB05 [0000] [000..008) (return), preds={BB04} succs={}
N024 (???,???) [000013] -----------                            IL_OFFSET void   INLRT @ 0x000[E-] REG NA
N026 (???,???) [000000] -----+-----                            NO_OP     void   REG NA
N028 (???,???) [000014] -----------                            IL_OFFSET void   INLRT @ 0x001[E-] REG NA
N030 (???,???) [000001] --CXG+-----                            CALL      void   Program:foo() REG NA
N032 (???,???) [000015] -----------                            IL_OFFSET void   INLRT @ 0x006[E-] REG NA
N034 (???,???) [000002] -----+-----                            NO_OP     void   REG NA
N036 (???,???) [000016] -----------                            IL_OFFSET void   INLRT @ 0x007[E-] REG NA
N038 (???,???) [000003] -----+-----                            RETURN    void   REG NA

-------------------------------------------------------------------------------------------------------------------

Final allocation
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
TreeID   LocRP# Name Type  Action    Reg  |r0  |r1  |r2  |r3  |r4  |r5  |r6  |r7  |r8  |r9  |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
          0.#0  BB1 PredBB0               |    |    |    |    |    |    |    |    |    |    |
          3.#1       Kill   None     [r1-r5 r9 f0-f7]
                                          |    |    |    |    |    |    |    |    |    |    |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
TreeID   LocRP# Name Type  Action    Reg  |r0  |r1  |r2  |r3  |r4  |r5  |r6  |r7  |r8  |r9  |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
          4.#2  BB2 PredBB1               |    |    |    |    |    |    |    |    |    |    |
[000004]  7.#3  C0   Def    Alloc    r1   |    |C0 a|    |    |    |    |    |    |    |    |
[000005]  8.#4  C0   Use *  Keep     r1   |    |C0 i|    |    |    |    |    |    |    |    |
          9.#5  I1   Def    Alloc    r1   |    |I1 a|    |    |    |    |    |    |    |    |
[000007] 12.#6  I1   Use *  Keep     r1   |    |I1 i|    |    |    |    |    |    |    |    |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
TreeID   LocRP# Name Type  Action    Reg  |r0  |r1  |r2  |r3  |r4  |r5  |r6  |r7  |r8  |r9  |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
         16.#7  BB3 PredBB2               |    |    |    |    |    |    |    |    |    |    |
[000008] 19.#8       Kill   None     [r1-r5]
                                          |    |    |    |    |    |    |    |    |    |    |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
TreeID   LocRP# Name Type  Action    Reg  |r0  |r1  |r2  |r3  |r4  |r5  |r6  |r7  |r8  |r9  |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
         20.#9  BB4 PredBB2               |    |    |    |    |    |    |    |    |    |    |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
TreeID   LocRP# Name Type  Action    Reg  |r0  |r1  |r2  |r3  |r4  |r5  |r6  |r7  |r8  |r9  |
------------------------------------------+----+----+----+----+----+----+----+----+----+----+
         22.#10 BB5 PredBB4               |    |    |    |    |    |    |    |    |    |    |
[000001] 31.#11      Kill   None     [r1-r5]
                                          |    |    |    |    |    |    |    |    |    |    |

Recording the maximum number of concurrent spills:

----------
LSRA Stats
----------
Register selection order: ABCDEFGHIJKLMNOPQ
Total Tracked Vars:  0
Total Reg Cand Vars: 0
Total number of Intervals: 1
Total number of RefPositions: 11
Total Number of spill temps created: 0
..........
BB02 [  100.00]: REG_ORDER = 2
..........
Total SpillCount : 0   Weighted: 0.000000
Total CopyReg : 0   Weighted: 0.000000
Total ResolutionMovs : 0   Weighted: 0.000000
Total SplitEdges : 0   Weighted: 0.000000
..........
Total REG_ORDER [#13] : 2   Weighted: 200.000000

TUPLE STYLE DUMP WITH REGISTER ASSIGNMENTS
Incoming Parameters: 
BB01 [0001] [???..???) -> BB02(1) (always), preds={} succs={BB02}
=====

BB02 [0003] [???..???) -> BB04(0.5),BB03(0.5) (cond), preds={BB01} succs={BB03,BB04}
=====
  N006. r1              =  CNS_INT(h) 0x3ff12d67530 global ptr
  N008. r1              =  IND      ; r1
  N010.                    CNS_INT   0
  N012.                    CMP      ; r1
  N014.                    JCC       cond=UEQ

BB03 [0004] [???..???) -> BB04(1) (always), preds={BB02} succs={BB04}
=====
  N018.                    CALL help

BB04 [0002] [???..???) -> BB05(1) (always), preds={BB02,BB03} succs={BB05}
=====

BB05 [0000] [000..008) (return), preds={BB04} succs={}
=====
  N024.                    IL_OFFSET INLRT @ 0x000[E-]
  N026.                    NO_OP    
  N028.                    IL_OFFSET INLRT @ 0x001[E-]
  N030.                    CALL     
  N032.                    IL_OFFSET INLRT @ 0x006[E-]
  N034.                    NO_OP    
  N036.                    IL_OFFSET INLRT @ 0x007[E-]
  N038.                    RETURN   




*************** Finishing PHASE Linear scan register alloc
Trees after Linear scan register alloc

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB01 [0001]  1                             1    [???..???)-> BB02(1)                 (always)                     i LIR keep internal hascall
BB02 [0003]  1       BB01                  1    [???..???)-> BB04(0.5),BB03(0.5)     ( cond )                     LIR internal
BB03 [0004]  1       BB02                  0.50 [???..???)-> BB04(1)                 (always)                     LIR internal
BB04 [0002]  2       BB02,BB03             1    [???..???)-> BB05(1)                 (always)                     i LIR keep internal hascall
BB05 [0000]  1       BB04                  1    [000..008)                           (return)                     i LIR hascall gcsafe
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

------------ BB01 [0001] [???..???) -> BB02(1) (always), preds={} succs={BB02}

------------ BB02 [0003] [???..???) -> BB04(0.5),BB03(0.5) (cond), preds={BB01} succs={BB03,BB04}
N006 (???,???) [000004] H----+-----                    t4 =    CNS_INT(h) long   0x3ff12d67530 global ptr REG r1
                                                            /--*  t4     long   
N008 (???,???) [000005] n---G+-----                    t5 = *  IND       int    REG r1
N010 (???,???) [000006] -c---+-----                    t6 =    CNS_INT   int    0 REG NA
                                                            /--*  t5     int    
                                                            +--*  t6     int    
N012 (???,???) [000007] ----G+-N---                         *  CMP       void   REG NA
N014 (???,???) [000012] ----G------                            JCC       void   cond=UEQ REG NA

------------ BB03 [0004] [???..???) -> BB04(1) (always), preds={BB02} succs={BB04}
N018 (???,???) [000008] --C-G+?----                            CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE REG NA

------------ BB04 [0002] [???..???) -> BB05(1) (always), preds={BB02,BB03} succs={BB05}

------------ BB05 [0000] [000..008) (return), preds={BB04} succs={}
N024 (???,???) [000013] -----------                            IL_OFFSET void   INLRT @ 0x000[E-] REG NA
N026 (???,???) [000000] -----+-----                            NO_OP     void   REG NA
N028 (???,???) [000014] -----------                            IL_OFFSET void   INLRT @ 0x001[E-] REG NA
N030 (???,???) [000001] --CXG+-----                            CALL      void   Program:foo() REG NA
N032 (???,???) [000015] -----------                            IL_OFFSET void   INLRT @ 0x006[E-] REG NA
N034 (???,???) [000002] -----+-----                            NO_OP     void   REG NA
N036 (???,???) [000016] -----------                            IL_OFFSET void   INLRT @ 0x007[E-] REG NA
N038 (???,???) [000003] -----+-----                            RETURN    void   REG NA

-------------------------------------------------------------------------------------------------------------------
*************** In fgDebugCheckBBlist
[deferred prior check failed -- skipping this check]

*************** Starting PHASE Place 'align' instructions
*************** In placeLoopAlignInstructions()
Not aligning loops; ShouldAlignLoops is false

*************** Finishing PHASE Place 'align' instructions [no changes]
*************** In genGenerateCode()

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB01 [0001]  1                             1    [???..???)-> BB02(1)                 (always)                     i LIR keep internal hascall
BB02 [0003]  1       BB01                  1    [???..???)-> BB04(0.5),BB03(0.5)     ( cond )                     LIR internal
BB03 [0004]  1       BB02                  0.50 [???..???)-> BB04(1)                 (always)                     LIR internal
BB04 [0002]  2       BB02,BB03             1    [???..???)-> BB05(1)                 (always)                     i LIR keep internal hascall
BB05 [0000]  1       BB04                  1    [000..008)                           (return)                     i LIR hascall gcsafe
---------------------------------------------------------------------------------------------------------------------------------------------------------------------

*************** Starting PHASE Generate code
*************** In fgDebugCheckBBlist
; Assembly listing for method Program:s390xHw() (MinOpts)
; Emitting BLENDED_CODE for generic S390X - Unix
; MinOpts code
; debuggable code
; fp based frame
; fully interruptible
; No PGO data
Finalizing stack frame
Modified regs: [r1-r5 r9 f0-f7]
Callee-saved registers pushed: 1 [r9]
*************** In lvaAssignFrameOffsets(FINAL_FRAME_LAYOUT)
--- virtual stack offset to actual stack offset delta is 0
-- V00 was 0, now 0
; Final local variable assignments
;
;# V00 OutArgs      [V00    ] (  1,  1   )  struct ( 0) [r15+0x00]  do-not-enreg[XS] addr-exposed "OutgoingArgSpace"
;
; Lcl frame size = 8
Created:
      G_M23707_IG02:        ; offs=0x000000, size=0x0000, bbWeight=1, gcrefRegs=0000 {}
Mark labels for codegen
  BB01 : first block
  BB04 : branch target
*************** After genMarkLabelsForCodegen()

---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BBnum BBid ref try hnd preds           weight   [IL range]   [jump]                            [EH region]        [flags]
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
BB01 [0001]  1                             1    [???..???)-> BB02(1)                 (always)                     i LIR keep internal label hascall
BB02 [0003]  1       BB01                  1    [???..???)-> BB04(0.5),BB03(0.5)     ( cond )                     LIR internal
BB03 [0004]  1       BB02                  0.50 [???..???)-> BB04(1)                 (always)                     LIR internal
BB04 [0002]  2       BB02,BB03             1    [???..???)-> BB05(1)                 (always)                     i LIR keep internal label hascall
BB05 [0000]  1       BB04                  1    [000..008)                           (return)                     i LIR hascall gcsafe
---------------------------------------------------------------------------------------------------------------------------------------------------------------------
Setting stack level from -572662307 to 0

=============== Generating BB01 [0001] [???..???) -> BB02(1) (always), preds={} succs={BB02} flags=0x00000000.10008039: i LIR keep internal label hascall
BB01 IN (0)={}
     OUT(0)={}

Liveness not changing: 0000000000000000 {}
							Live regs: (unchanged) 0000000000000000 {}
							GC regs: (unchanged) 0000 {}
							Byref regs: (unchanged) 0000 {}

      L_M23707_BB01:
Label: G_M23707_IG02, GCvars=0000000000000000 {}, gcrefRegs=0000 {}, byrefRegs=0000 {}

Variable Live Range History Dump for BB01
..None..

=============== Generating BB02 [0003] [???..???) -> BB04(0.5),BB03(0.5) (cond), preds={BB01} succs={BB03,BB04} flags=0x00000000.00000021: LIR internal
BB02 IN (0)={}
     OUT(0)={}

Liveness not changing: 0000000000000000 {}
							Live regs: (unchanged) 0000000000000000 {}
							GC regs: (unchanged) 0000 {}
							Byref regs: (unchanged) 0000 {}

      L_M23707_BB02:
Added IP mapping: NO_MAP (G_M23707_IG02,ins#0,ofs#0) label
Generating: N006 (???,???) [000004] H----+-----                    t4 =    CNS_INT(h) long   0x3ff12d67530 global ptr REG r1
Mapped BB02 to G_M23707_IG02
                                                                        /--*  t4     long   
Generating: N008 (???,???) [000005] n---G+-----                    t5 = *  IND       int    REG r1
IN0003:             l       
Generating: N010 (???,???) [000006] -c---+-----                    t6 =    CNS_INT   int    0 REG NA
                                                                        /--*  t5     int    
                                                                        +--*  t6     int    
Generating: N012 (???,???) [000007] ----G+-N---                         *  CMP       void   REG NA
Generating: N014 (???,???) [000012] ----G------                            JCC       void   cond=UEQ REG NA
IN0005:             brcl    

Variable Live Range History Dump for BB02
..None..

=============== Generating BB03 [0004] [???..???) -> BB04(1) (always), preds={BB02} succs={BB04} flags=0x00000000.00000021: LIR internal
BB03 IN (0)={}
     OUT(0)={}

Liveness not changing: 0000000000000000 {}
							Live regs: (unchanged) 0000000000000000 {}
							GC regs: (unchanged) 0000 {}
							Byref regs: (unchanged) 0000 {}

      L_M23707_BB03:
Adding label due to BB weight difference: BBJ_COND BB02 with weight 100 different from BB03 with weight 50
Saved:
      G_M23707_IG02:        ; offs=0x000000, size=0x001A, bbWeight=1, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB02 [0003], byref
Created:
      G_M23707_IG03:        ; offs=0x00001A, size=0x0000, bbWeight=0.50, gcrefRegs=0000 {}
Label: G_M23707_IG03, GCvars=0000000000000000 {}, gcrefRegs=0000 {}, byrefRegs=0000 {}
genIPmappingAdd: ignoring duplicate IL offset 0xffffffff
Generating: N018 (???,???) [000008] --C-G+?----                            CALL help void   CORINFO_HELP_DBG_IS_JUST_MY_CODE REG NA
Generating call for helper
Mapped BB03 to G_M23707_IG03
Call generation complete

Variable Live Range History Dump for BB03
..None..

=============== Generating BB04 [0002] [???..???) -> BB05(1) (always), preds={BB02,BB03} succs={BB05} flags=0x00000000.10008039: i LIR keep internal label hascall
BB04 IN (0)={}
     OUT(0)={}

Liveness not changing: 0000000000000000 {}
							Live regs: (unchanged) 0000000000000000 {}
							GC regs: (unchanged) 0000 {}
							Byref regs: (unchanged) 0000 {}

      L_M23707_BB04:
Saved:
      G_M23707_IG03:        ; offs=0x00001A, size=0x0006, bbWeight=0.50, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB03 [0004], byref
Created:
      G_M23707_IG04:        ; offs=0x000020, size=0x0000, bbWeight=1, gcrefRegs=0000 {}
Label: G_M23707_IG04, GCvars=0000000000000000 {}, gcrefRegs=0000 {}, byrefRegs=0000 {}
genIPmappingAdd: ignoring duplicate IL offset 0xffffffff

Variable Live Range History Dump for BB04
..None..

=============== Generating BB05 [0000] [000..008) (return), preds={BB04} succs={} flags=0x00000000.10080011: i LIR hascall gcsafe
BB05 IN (0)={}
     OUT(0)={}

Liveness not changing: 0000000000000000 {}
							Live regs: (unchanged) 0000000000000000 {}
							GC regs: (unchanged) 0000 {}
							Byref regs: (unchanged) 0000 {}

      L_M23707_BB05:
Added IP mapping: 0x0000 STACK_EMPTY (G_M23707_IG04,ins#0,ofs#0) label
Generating: N024 (???,???) [000013] -----------                            IL_OFFSET void   INLRT @ 0x000[E-] REG NA
Generating: N026 (???,???) [000000] -----+-----                            NO_OP     void   REG NA
Mapped BB05 to G_M23707_IG04
IN0007:             nop     
Added IP mapping: 0x0001 STACK_EMPTY (G_M23707_IG04,ins#1,ofs#2)
Generating: N028 (???,???) [000014] -----------                            IL_OFFSET void   INLRT @ 0x001[E-] REG NA
Generating: N030 (???,???) [000001] --CXG+-----                            CALL      void   Program:foo() REG NA
Generating call for user function
Call generation complete
Added IP mapping: 0x0006 STACK_EMPTY (G_M23707_IG04,ins#2,ofs#4)
Generating: N032 (???,???) [000015] -----------                            IL_OFFSET void   INLRT @ 0x006[E-] REG NA
Generating: N034 (???,???) [000002] -----+-----                            NO_OP     void   REG NA
IN0009:             nop     
Added IP mapping: 0x0007 STACK_EMPTY (G_M23707_IG04,ins#3,ofs#6)
Generating: N036 (???,???) [000016] -----------                            IL_OFFSET void   INLRT @ 0x007[E-] REG NA
Generating: N038 (???,???) [000003] -----+-----                            RETURN    void   REG NA
IN000a:             nop     
Added IP mapping: EPILOG (G_M23707_IG04,ins#4,ofs#8) label
Reserving epilog IG for block BB05
Saved:
      G_M23707_IG04:        ; offs=0x000020, size=0x0008, bbWeight=1, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB05 [0000], byref
Created:
      G_M23707_IG05:        ; offs=0x000028, size=0x0000, bbWeight=1, gcrefRegs=0000 {}
*************** After placeholder IG creation
G_M23707_IG01:        ; func=00, offs=0x000000, size=0x0000, bbWeight=1, gcrefRegs=0000 {} <-- Prolog IG
G_M23707_IG02:        ; offs=0x000000, size=0x001A, bbWeight=1, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB02 [0003], byref
G_M23707_IG03:        ; offs=0x00001A, size=0x0006, bbWeight=0.50, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB03 [0004], byref
G_M23707_IG04:        ; offs=0x000020, size=0x0008, bbWeight=1, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB05 [0000], byref
G_M23707_IG05:        ; epilog placeholder, next placeholder=<END>, BB05 [0000], epilog, extend <-- First placeholder <-- Last placeholder
                      ;   PrevGCVars=0000000000000000 {}, PrevGCrefRegs=0000 {}, PrevByrefRegs=0000 {}
                      ;   InitGCVars=0000000000000000 {}, InitGCrefRegs=0000 {}, InitByrefRegs=0000 {}

Variable Live Range History Dump for BB05
..None..
Liveness not changing: 0000000000000000 {}

# compCycleEstimate = -572662307, compSizeEstimate = -572662307 Program:s390xHw()
; Final local variable assignments
;
;# V00 OutArgs      [V00    ] (  1,  1   )  struct ( 0) [r15+0x00]  do-not-enreg[XS] addr-exposed "OutgoingArgSpace"
;
; Lcl frame size = 8
*************** Before prolog / epilog generation
G_M23707_IG01:        ; func=00, offs=0x000000, size=0x0000, bbWeight=1, gcrefRegs=0000 {} <-- Prolog IG
G_M23707_IG02:        ; offs=0x000000, size=0x001A, bbWeight=1, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB02 [0003], byref
G_M23707_IG03:        ; offs=0x00001A, size=0x0006, bbWeight=0.50, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB03 [0004], byref
G_M23707_IG04:        ; offs=0x000020, size=0x0008, bbWeight=1, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB05 [0000], byref
G_M23707_IG05:        ; epilog placeholder, next placeholder=<END>, BB05 [0000], epilog, extend <-- First placeholder <-- Last placeholder
                      ;   PrevGCVars=0000000000000000 {}, PrevGCrefRegs=0000 {}, PrevByrefRegs=0000 {}
                      ;   InitGCVars=0000000000000000 {}, InitGCrefRegs=0000 {}, InitByrefRegs=0000 {}
*************** In genFnProlog()
Added IP mapping to front: PROLOG (G_M23707_IG01,ins#0,ofs#0) label

__prolog:
Frame info. #outsz=0; #framesz=168; LclFrameSize=8;
Save float regs: []
Save int   regs: [r9]
Frame info. #outsz=0; #framesz=168; lcl=8
IN000b:             stmg    
IN000c:             lgr     
IN000d:             stg     
IN000e:             lay     
IN000f:             stg     
IN0010:             lgr     
IN0011:             lay     
*************** In genEnregisterIncomingStackArgs()

Saved:
      G_M23707_IG01:        ; offs=0x000000, size=0x0026, bbWeight=1, gcrefRegs=0000 {}, byrefRegs=0000 {}, byref, nogc
*************** In genFnEpilog()

__epilog:
gcVarPtrSetCur=0000000000000000 {}, gcRegGCrefSetCur=0000 {}, gcRegByrefSetCur=0000 {}
IN0012:             lmg     
IN0013:             ret     
Saved:
      G_M23707_IG05:        ; offs=0x000028, size=0x0008, bbWeight=1, epilog, nogc, extend
0 prologs, 1 epilogs, 0 funclet prologs, 0 funclet epilogs
*************** After prolog / epilog generation
G_M23707_IG01:        ; func=00, offs=0x000000, size=0x0026, bbWeight=1, gcrefRegs=0000 {}, byrefRegs=0000 {}, byref, nogc <-- Prolog IG
G_M23707_IG02:        ; offs=0x000026, size=0x001A, bbWeight=1, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB02 [0003], byref
G_M23707_IG03:        ; offs=0x000040, size=0x0006, bbWeight=0.50, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB03 [0004], byref
G_M23707_IG04:        ; offs=0x000046, size=0x0008, bbWeight=1, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB05 [0000], byref
G_M23707_IG05:        ; offs=0x00004E, size=0x0008, bbWeight=1, epilog, nogc, extend
*************** In emitJumpDistBind()
Emitter Jump List:
IG02 IN0005 brcl[6] -> IG04 (long)
  total jump count: 1
Binding: IN0005:             brcl    
Binding L_M23707_BB04 to G_M23707_IG04
Estimate of fwd jump [13E0869C/005]: 003A -> 0046 = 000C

*************** Finishing PHASE Generate code

*************** Starting PHASE Emit code

Hot  code size = 0x56 bytes
Cold code size = 0x0 bytes
*************** In emitEndCodeGen()
Converting emitMaxStackDepth from bytes (0) to elements (0)

***************************************************************************
Instructions as they come out of the scheduler


G_M23707_IG01:        ; offs=0x000000, size=0x0026, bbWeight=1, gcrefRegs=0000 {}, byrefRegs=0000 {}, byref, nogc <-- Prolog IG
IN000b: 000000      stmg    
IN000c: 000006      lgr     
IN000d: 00000A      stg     
IN000e: 000010      lay     
IN000f: 000016      stg     
IN0010: 00001C      lgr     
IN0011: 000020      lay     
						;; size=38 bbWeight=1 PerfScore 0.00
G_M23707_IG02:        ; offs=0x000026, size=0x001A, bbWeight=1, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB02 [0003], byref
IN0001: 000026      iihf    
IN0002: 00002C      iilf    
IN0003: 000032      l       
IN0004: 000036      chi     
IN0005: 00003A      brcl    
						;; size=26 bbWeight=1 PerfScore 0.00
G_M23707_IG03:        ; offs=0x000040, size=0x0006, bbWeight=0.50, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB03 [0004], byref
IN0006: 000040      brasl   
						;; size=6 bbWeight=0.50 PerfScore 0.00
G_M23707_IG04:        ; offs=0x000046, size=0x0008, bbWeight=1, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB05 [0000], byref
IN0007: 000046      nop     
IN0008: 000048      basr    
IN0009: 00004A      nop     
IN000a: 00004C      nop     
						;; size=8 bbWeight=1 PerfScore 0.00
G_M23707_IG05:        ; offs=0x00004E, size=0x0008, bbWeight=1, epilog, nogc, extend
IN0012: 00004E      lmg     
IN0013: 000054      ret     
						;; size=8 bbWeight=1 PerfScore 0.00


Allocated method code size =   86 , actual size =   86, unused size =    0

; Total bytes of code 86, prolog size 38, PerfScore 0.00, instruction count 19, allocated bytes for code 86 (MethodHash=a394a364) for method Program:s390xHw() (MinOpts)
; ============================================================

*************** After end code gen, before unwindEmit()
G_M23707_IG01:        ; func=00, offs=0x000000, size=0x0026, bbWeight=1, PerfScore 0.00, gcrefRegs=0000 {}, byrefRegs=0000 {}, byref, nogc <-- Prolog IG

IN000b: 000000      stmg    
IN000c: 000006      lgr     
IN000d: 00000A      stg     
IN000e: 000010      lay     
IN000f: 000016      stg     
IN0010: 00001C      lgr     
IN0011: 000020      lay     

G_M23707_IG02:        ; offs=0x000026, size=0x001A, bbWeight=1, PerfScore 0.00, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB02 [0003], byref

IN0001: 000026      iihf    
IN0002: 00002C      iilf    
IN0003: 000032      l       
IN0004: 000036      chi     
IN0005: 00003A      brcl    

G_M23707_IG03:        ; offs=0x000040, size=0x0006, bbWeight=0.50, PerfScore 0.00, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB03 [0004], byref

IN0006: 000040      brasl   

G_M23707_IG04:        ; offs=0x000046, size=0x0008, bbWeight=1, PerfScore 0.00, gcrefRegs=0000 {}, byrefRegs=0000 {}, BB05 [0000], byref

IN0007: 000046      nop     
IN0008: 000048      basr    
IN0009: 00004A      nop     
IN000a: 00004C      nop     

G_M23707_IG05:        ; offs=0x00004E, size=0x0008, bbWeight=1, PerfScore 0.00, epilog, nogc, extend

IN0012: 00004E      lmg     
IN0013: 000054      ret     


*************** Finishing PHASE Emit code

*************** Starting PHASE Emit GC+EH tables
*************** In genIPmappingGen()
IP mapping count : 7
IL offs PROLOG : 0x00000000 ( STACK_EMPTY )
IL offs NO_MAP : 0x00000026 ( STACK_EMPTY )
IL offs 0x0000 : 0x00000046 ( STACK_EMPTY )
IL offs 0x0001 : 0x00000048 ( STACK_EMPTY )
IL offs 0x0006 : 0x0000004A ( STACK_EMPTY )
IL offs 0x0007 : 0x0000004C ( STACK_EMPTY )
IL offs EPILOG : 0x0000004E ( STACK_EMPTY )

*************** In genSetScopeInfo()
VarLocInfo count is 0
; Variable debug info: 0 live ranges, 0 vars for method Program:s390xHw()

*************** Finishing PHASE Emit GC+EH tables
   1: JIT compiled Program:s390xHw() [MinOpts, IL size=8, code size=86, hash=0xa394a364]
Method code size: 86

Allocations for Program:s390xHw() (MethodHash=a394a364)
count:        231, size:      27942, max =       6336
allocateMemory:      65536, nraUsed:      31104

Alloc'd bytes by kind:
                  kind |       size |     pct
  ---------------------+------------+--------
                   ABI |          0 |   0.00%
         AssertionProp |          0 |   0.00%
               ASTNode |       2688 |   9.62%
              InstDesc |       3544 |  12.68%
              ImpStack |        384 |   1.37%
            BasicBlock |       1768 |   6.33%
              CallArgs |          0 |   0.00%
              FlowEdge |        200 |   0.72%
      DepthFirstSearch |         64 |   0.23%
                 Loops |          0 |   0.00%
     TreeStatementList |          0 |   0.00%
               SiScope |          0 |   0.00%
       DominatorMemory |          0 |   0.00%
                  LSRA |       8664 |  31.01%
         LSRA_Interval |        192 |   0.69%
      LSRA_RefPosition |        960 |   3.44%
          Reachability |          0 |   0.00%
                   SSA |          0 |   0.00%
           ValueNumber |          0 |   0.00%
              LvaTable |       1536 |   5.50%
            UnwindInfo |          0 |   0.00%
                hashBv |         40 |   0.14%
                bitset |         32 |   0.11%
          FixedBitVect |         16 |   0.06%
               Generic |        806 |   2.88%
   LocalAddressVisitor |          0 |   0.00%
         FieldSeqStore |          0 |   0.00%
          MemorySsaMap |          0 |   0.00%
          MemoryPhiArg |          0 |   0.00%
                   CSE |          0 |   0.00%
                    GC |          0 |   0.00%
       CorTailCallInfo |          0 |   0.00%
              Inlining |        248 |   0.89%
            ArrayStack |          0 |   0.00%
             DebugInfo |        336 |   1.20%
             DebugOnly |       5104 |  18.27%
               Codegen |       1072 |   3.84%
               LoopOpt |          0 |   0.00%
             LoopClone |          0 |   0.00%
            LoopUnroll |          0 |   0.00%
             LoopHoist |          0 |   0.00%
            LoopIVOpts |          0 |   0.00%
               Unknown |         48 |   0.17%
            RangeCheck |          0 |   0.00%
              CopyProp |          0 |   0.00%
             Promotion |        120 |   0.43%
           SideEffects |          0 |   0.00%
       ObjectAllocator |          0 |   0.00%
    VariableLiveRanges |         40 |   0.14%
           ClassLayout |         80 |   0.29%
       TailMergeThrows |          0 |   0.00%
             EarlyProp |          0 |   0.00%
              ZeroInit |          0 |   0.00%
                   Pgo |          0 |   0.00%

Final metrics:
PhysicallyPromotedFields                  : 0
LoopsFoundDuringOpts                      : 0
LoopsCloned                               : 0
LoopsUnrolled                             : 0
LoopAlignmentCandidates                   : 0
LoopsAligned                              : 0
LoopsIVWidened                            : 0
WidenedIVs                                : 0
UnusedIVsRemoved                          : 0
LoopsMadeDownwardsCounted                 : 0
LoopsStrengthReduced                      : 0
VarsInSsa                                 : 0
HoistedExpressions                        : 0
RedundantBranchesEliminated               : 0
JumpThreadingsPerformed                   : 0
CseCount                                  : 0
BasicBlocksAtCodegen                      : 5
PerfScore                                 : 0.000000
BytesAllocated                            : 31104
ImporterBranchFold                        : 0
ImporterSwitchFold                        : 0
DevirtualizedCall                         : 0
DevirtualizedCallUnboxedEntry             : 0
DevirtualizedCallRemovedBox               : 0
GDV                                       : 0
ClassGDV                                  : 0
MethodGDV                                 : 0
MultiGuessGDV                             : 0
ChainedGDV                                : 0
InlinerBranchFold                         : 0
InlineAttempt                             : 0
InlineCount                               : 0
ProfileConsistentBeforeInline             : 0
ProfileConsistentAfterInline              : 0
ProfileSynthesizedBlendedOrRepaired       : 0
ProfileInconsistentInitially              : 0
ProfileInconsistentResetLeave             : 0
ProfileInconsistentImporterBranchFold     : 0
ProfileInconsistentImporterSwitchFold     : 0
ProfileInconsistentChainedGDV             : 0
ProfileInconsistentScratchBB              : 0
ProfileInconsistentInlinerBranchFold      : 0
ProfileInconsistentInlineeScale           : 0
ProfileInconsistentInlinee                : 0
ProfileInconsistentNoReturnInlinee        : 0
ProfileInconsistentMayThrowInlinee        : 0
NewRefClassHelperCalls                    : 0
StackAllocatedRefClasses                  : 0
NewBoxedValueClassHelperCalls             : 0
StackAllocatedBoxedValueClasses           : 0

****** DONE compiling Program:s390xHw()
