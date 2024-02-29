// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
HELPER(READYTORUN_HELPER_ByRefWriteBarrierBatch,    CORINFO_HELP_ASSIGN_BYREF_BATCH,                )

HELPER(READYTORUN_HELPER_Stelem_Ref,                CORINFO_HELP_ARRADDR_ST,                        )
HELPER(READYTORUN_HELPER_Ldelema_Ref,               CORINFO_HELP_LDELEMA_REF,                       )

HELPER(READYTORUN_HELPER_MemSet,                    CORINFO_HELP_MEMSET,                            )
HELPER(READYTORUN_HELPER_MemZero,                   CORINFO_HELP_MEMZERO,                           )
HELPER(READYTORUN_HELPER_NativeMemSet,              CORINFO_HELP_NATIVE_MEMSET,                     )
HELPER(READYTORUN_HELPER_MemCpy,                    CORINFO_HELP_MEMCPY,                            )

HELPER(READYTORUN_HELPER_LogMethodEnter,            CORINFO_HELP_BBT_FCN_ENTER,                     )

HELPER(READYTORUN_HELPER_GetRuntimeTypeHandle,      CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE,         )
HELPER(READYTORUN_HELPER_GetRuntimeMethodHandle,    CORINFO_HELP_METHODDESC_TO_STUBRUNTIMEMETHOD,   )
HELPER(READYTORUN_HELPER_GetRuntimeFieldHandle,     CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD,     )

HELPER(READYTORUN_HELPER_Box,                       CORINFO_HELP_BOX,                               )
HELPER(READYTORUN_HELPER_Box_Nullable,              CORINFO_HELP_BOX_NULLABLE,                      )
HELPER(READYTORUN_HELPER_Unbox,                     CORINFO_HELP_UNBOX,                             )
HELPER(READYTORUN_HELPER_Unbox_Nullable,            CORINFO_HELP_UNBOX_NULLABLE,                    )
HELPER(READYTORUN_HELPER_NewMultiDimArr,            CORINFO_HELP_NEW_MDARR,                         )

HELPER(READYTORUN_HELPER_NewObject,                 CORINFO_HELP_NEWFAST,                           )
HELPER(READYTORUN_HELPER_NewArray,                  CORINFO_HELP_NEWARR_1_DIRECT,                   )
HELPER(READYTORUN_HELPER_CheckCastAny,              CORINFO_HELP_CHKCASTANY,                        )
HELPER(READYTORUN_HELPER_CheckInstanceAny,          CORINFO_HELP_ISINSTANCEOFANY,                   )

HELPER(READYTORUN_HELPER_GenericGcStaticBase,       CORINFO_HELP_GETGENERICS_GCSTATIC_BASE,         )
HELPER(READYTORUN_HELPER_GenericNonGcStaticBase,    CORINFO_HELP_GETGENERICS_NONGCSTATIC_BASE,      )
HELPER(READYTORUN_HELPER_GenericGcTlsBase,          CORINFO_HELP_GETGENERICS_GCTHREADSTATIC_BASE,   )
HELPER(READYTORUN_HELPER_GenericNonGcTlsBase,       CORINFO_HELP_GETGENERICS_NONGCTHREADSTATIC_BASE,)

HELPER(READYTORUN_HELPER_VirtualFuncPtr,            CORINFO_HELP_VIRTUAL_FUNC_PTR,                  )
HELPER(READYTORUN_HELPER_IsInstanceOfException,     CORINFO_HELP_ISINSTANCEOF_EXCEPTION,            )
HELPER(READYTORUN_HELPER_NewMaybeFrozenArray,       CORINFO_HELP_NEWARR_1_MAYBEFROZEN,              )
HELPER(READYTORUN_HELPER_NewMaybeFrozenObject,      CORINFO_HELP_NEWFAST_MAYBEFROZEN,               )

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

HELPER(READYTORUN_HELPER_FltRem,                    CORINFO_HELP_FLTREM,                            )
HELPER(READYTORUN_HELPER_DblRem,                    CORINFO_HELP_DBLREM,                            )
HELPER(READYTORUN_HELPER_FltRound,                  CORINFO_HELP_FLTROUND,                          )
HELPER(READYTORUN_HELPER_DblRound,                  CORINFO_HELP_DBLROUND,                          )

#ifndef TARGET_X86
HELPER(READYTORUN_HELPER_PersonalityRoutine,        CORINFO_HELP_EE_PERSONALITY_ROUTINE,            OPTIMIZEFORSIZE)
HELPER(READYTORUN_HELPER_PersonalityRoutineFilterFunclet, CORINFO_HELP_EE_PERSONALITY_ROUTINE_FILTER_FUNCLET, OPTIMIZEFORSIZE)
#endif

#ifdef TARGET_X86
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

HELPER(READYTORUN_HELPER_PInvokeBegin,              CORINFO_HELP_JIT_PINVOKE_BEGIN,                 )
HELPER(READYTORUN_HELPER_PInvokeEnd,                CORINFO_HELP_JIT_PINVOKE_END,                   )
HELPER(READYTORUN_HELPER_GCPoll,                    CORINFO_HELP_POLL_GC,                           )
HELPER(READYTORUN_HELPER_ReversePInvokeEnter,       CORINFO_HELP_JIT_REVERSE_PINVOKE_ENTER,         )
HELPER(READYTORUN_HELPER_ReversePInvokeExit,        CORINFO_HELP_JIT_REVERSE_PINVOKE_EXIT,          )

HELPER(READYTORUN_HELPER_MonitorEnter,              CORINFO_HELP_MON_ENTER,                         )
HELPER(READYTORUN_HELPER_MonitorExit,               CORINFO_HELP_MON_EXIT,                          )

#ifndef TARGET_ARM64
HELPER(READYTORUN_HELPER_StackProbe,                CORINFO_HELP_STACK_PROBE,                       )
#endif

HELPER(READYTORUN_HELPER_GetCurrentManagedThreadId, CORINFO_HELP_GETCURRENTMANAGEDTHREADID,         )

#undef HELPER
#undef OPTIMIZEFORSPEED
