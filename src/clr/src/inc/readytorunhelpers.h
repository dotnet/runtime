// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// ReadyToRunHelpers.h
//

//
// Mapping between regular JIT helpers and ready to run helpers
//

#ifndef OPTIMIZEFORSPEED
#define OPTIMIZEFORSPEED
#endif

HELPER(READYTORUN_HELPER_Throw,                     CORINFO_HELP_THROW,                             OPTIMIZEFORSIZE)
HELPER(READYTORUN_HELPER_Rethrow,                   CORINFO_HELP_RETHROW,                           OPTIMIZEFORSIZE)
HELPER(READYTORUN_HELPER_Overflow,                  CORINFO_HELP_OVERFLOW,                          OPTIMIZEFORSIZE)
HELPER(READYTORUN_HELPER_RngChkFail,                CORINFO_HELP_RNGCHKFAIL,                        OPTIMIZEFORSIZE)
HELPER(READYTORUN_HELPER_FailFast,                  CORINFO_HELP_FAIL_FAST,                         OPTIMIZEFORSIZE)
HELPER(READYTORUN_HELPER_ThrowNullRef,              CORINFO_HELP_THROWNULLREF,                      OPTIMIZEFORSIZE)
HELPER(READYTORUN_HELPER_ThrowDivZero,              CORINFO_HELP_THROWDIVZERO,                      OPTIMIZEFORSIZE)

HELPER(READYTORUN_HELPER_WriteBarrier,              CORINFO_HELP_ASSIGN_REF,                        )
HELPER(READYTORUN_HELPER_CheckedWriteBarrier,       CORINFO_HELP_CHECKED_ASSIGN_REF,                )
HELPER(READYTORUN_HELPER_ByRefWriteBarrier,         CORINFO_HELP_ASSIGN_BYREF,                      )

HELPER(READYTORUN_HELPER_Stelem_Ref,                CORINFO_HELP_ARRADDR_ST,                        )
HELPER(READYTORUN_HELPER_Ldelema_Ref,               CORINFO_HELP_LDELEMA_REF,                       )

HELPER(READYTORUN_HELPER_MemSet,                    CORINFO_HELP_MEMSET,                            )
HELPER(READYTORUN_HELPER_MemCpy,                    CORINFO_HELP_MEMCPY,                            )

HELPER(READYTORUN_HELPER_GetRuntimeTypeHandle,      CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE,         )
HELPER(READYTORUN_HELPER_GetRuntimeMethodHandle,    CORINFO_HELP_METHODDESC_TO_STUBRUNTIMEMETHOD,   )
HELPER(READYTORUN_HELPER_GetRuntimeFieldHandle,     CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD,     )

HELPER(READYTORUN_HELPER_Box,                       CORINFO_HELP_BOX,                               )
HELPER(READYTORUN_HELPER_Box_Nullable,              CORINFO_HELP_BOX_NULLABLE,                      )
HELPER(READYTORUN_HELPER_Unbox,                     CORINFO_HELP_UNBOX,                             )
HELPER(READYTORUN_HELPER_Unbox_Nullable,            CORINFO_HELP_UNBOX_NULLABLE,                    )
HELPER(READYTORUN_HELPER_NewMultiDimArr,            CORINFO_HELP_NEW_MDARR,                         )

HELPER(READYTORUN_HELPER_LMul,                      CORINFO_HELP_LMUL,                              )
HELPER(READYTORUN_HELPER_LMulOfv,                   CORINFO_HELP_LMUL_OVF,                          )
HELPER(READYTORUN_HELPER_ULMulOvf,                  CORINFO_HELP_ULMUL_OVF,                         )
HELPER(READYTORUN_HELPER_LDiv,                      CORINFO_HELP_LDIV,                              )
HELPER(READYTORUN_HELPER_LMod,                      CORINFO_HELP_LMOD,                              )
HELPER(READYTORUN_HELPER_ULDiv,                     CORINFO_HELP_ULDIV,                             )
HELPER(READYTORUN_HELPER_ULMod,                     CORINFO_HELP_ULMOD,                             )
HELPER(READYTORUN_HELPER_LLsh,                      CORINFO_HELP_LLSH,                              )
HELPER(READYTORUN_HELPER_LRsh,                      CORINFO_HELP_LRSH,                              )
HELPER(READYTORUN_HELPER_LRsz,                      CORINFO_HELP_LRSZ,                              )
HELPER(READYTORUN_HELPER_Lng2Dbl,                   CORINFO_HELP_LNG2DBL,                           )
HELPER(READYTORUN_HELPER_ULng2Dbl,                  CORINFO_HELP_ULNG2DBL,                          )

HELPER(READYTORUN_HELPER_Div,                       CORINFO_HELP_DIV,                               )
HELPER(READYTORUN_HELPER_Mod,                       CORINFO_HELP_MOD,                               )
HELPER(READYTORUN_HELPER_UDiv,                      CORINFO_HELP_UDIV,                              )
HELPER(READYTORUN_HELPER_UMod,                      CORINFO_HELP_UMOD,                              )

HELPER(READYTORUN_HELPER_Dbl2Int,                   CORINFO_HELP_DBL2INT,                           )
HELPER(READYTORUN_HELPER_Dbl2IntOvf,                CORINFO_HELP_DBL2INT_OVF,                       )
HELPER(READYTORUN_HELPER_Dbl2Lng,                   CORINFO_HELP_DBL2LNG,                           )
HELPER(READYTORUN_HELPER_Dbl2LngOvf,                CORINFO_HELP_DBL2LNG_OVF,                       )
HELPER(READYTORUN_HELPER_Dbl2UInt,                  CORINFO_HELP_DBL2UINT,                          )
HELPER(READYTORUN_HELPER_Dbl2UIntOvf,               CORINFO_HELP_DBL2UINT_OVF,                      )
HELPER(READYTORUN_HELPER_Dbl2ULng,                  CORINFO_HELP_DBL2ULNG,                          )
HELPER(READYTORUN_HELPER_Dbl2ULngOvf,               CORINFO_HELP_DBL2ULNG_OVF,                      )

HELPER(READYTORUN_HELPER_DblRem,                    CORINFO_HELP_FLTREM,                            )
HELPER(READYTORUN_HELPER_FltRem,                    CORINFO_HELP_DBLREM,                            )
HELPER(READYTORUN_HELPER_DblRound,                  CORINFO_HELP_FLTROUND,                          )
HELPER(READYTORUN_HELPER_FltRound,                  CORINFO_HELP_DBLROUND,                          )

#ifndef _TARGET_X86_
HELPER(READYTORUN_HELPER_PersonalityRoutine,        CORINFO_HELP_EE_PERSONALITY_ROUTINE,            OPTIMIZEFORSIZE)
HELPER(READYTORUN_HELPER_PersonalityRoutineFilterFunclet, CORINFO_HELP_EE_PERSONALITY_ROUTINE_FILTER_FUNCLET, OPTIMIZEFORSIZE)
#endif

#ifdef _TARGET_X86_
HELPER(READYTORUN_HELPER_WriteBarrier_EAX,          CORINFO_HELP_ASSIGN_REF_EAX,                    )
HELPER(READYTORUN_HELPER_WriteBarrier_EBX,          CORINFO_HELP_ASSIGN_REF_EBX,                    )
HELPER(READYTORUN_HELPER_WriteBarrier_ECX,          CORINFO_HELP_ASSIGN_REF_ECX,                    )
HELPER(READYTORUN_HELPER_WriteBarrier_ESI,          CORINFO_HELP_ASSIGN_REF_ESI,                    )
HELPER(READYTORUN_HELPER_WriteBarrier_EDI,          CORINFO_HELP_ASSIGN_REF_EDI,                    )
HELPER(READYTORUN_HELPER_WriteBarrier_EBP,          CORINFO_HELP_ASSIGN_REF_EBP,                    )
HELPER(READYTORUN_HELPER_CheckedWriteBarrier_EAX,   CORINFO_HELP_CHECKED_ASSIGN_REF_EAX,            )
HELPER(READYTORUN_HELPER_CheckedWriteBarrier_EBX,   CORINFO_HELP_CHECKED_ASSIGN_REF_EBX,            )
HELPER(READYTORUN_HELPER_CheckedWriteBarrier_ECX,   CORINFO_HELP_CHECKED_ASSIGN_REF_ECX,            )
HELPER(READYTORUN_HELPER_CheckedWriteBarrier_ESI,   CORINFO_HELP_CHECKED_ASSIGN_REF_ESI,            )
HELPER(READYTORUN_HELPER_CheckedWriteBarrier_EDI,   CORINFO_HELP_CHECKED_ASSIGN_REF_EDI,            )
HELPER(READYTORUN_HELPER_CheckedWriteBarrier_EBP,   CORINFO_HELP_CHECKED_ASSIGN_REF_EBP,            )

HELPER(READYTORUN_HELPER_EndCatch,                  CORINFO_HELP_ENDCATCH,                          OPTIMIZEFORSIZE)
#endif

#undef HELPER
#undef OPTIMIZEFORSPEED
