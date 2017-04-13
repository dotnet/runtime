// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef DEBUGGER_DUMPCOMMON_H
#define DEBUGGER_DUMPCOMMON_H

#ifdef FEATURE_PAL
typedef enum _MINIDUMP_TYPE {
    MiniDumpNormal                         = 0x00000000,
    MiniDumpWithDataSegs                   = 0x00000001,
    MiniDumpWithFullMemory                 = 0x00000002,
    MiniDumpWithHandleData                 = 0x00000004,
    MiniDumpFilterMemory                   = 0x00000008,
    MiniDumpScanMemory                     = 0x00000010,
    MiniDumpWithUnloadedModules            = 0x00000020,
    MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
    MiniDumpFilterModulePaths              = 0x00000080,
    MiniDumpWithProcessThreadData          = 0x00000100,
    MiniDumpWithPrivateReadWriteMemory     = 0x00000200,
    MiniDumpWithoutOptionalData            = 0x00000400,
    MiniDumpWithFullMemoryInfo             = 0x00000800,
    MiniDumpWithThreadInfo                 = 0x00001000,
    MiniDumpWithCodeSegs                   = 0x00002000,
    MiniDumpWithoutAuxiliaryState          = 0x00004000,
    MiniDumpWithFullAuxiliaryState         = 0x00008000,
    MiniDumpWithPrivateWriteCopyMemory     = 0x00010000,
    MiniDumpIgnoreInaccessibleMemory       = 0x00020000,
    MiniDumpWithTokenInformation           = 0x00040000,
    MiniDumpWithModuleHeaders              = 0x00080000,
    MiniDumpFilterTriage                   = 0x00100000,
    MiniDumpWithAvxXStateContext           = 0x00200000,
    MiniDumpValidTypeFlags                 = 0x003fffff,
} MINIDUMP_TYPE;
#endif // FEATURE_PAL

#if defined(DACCESS_COMPILE) || defined(RIGHT_SIDE_COMPILE)

// When debugging against minidumps, we frequently need to ignore errors
// due to the dump not having memory content.
// You should be VERY careful using these macros.  Because our code does not
//  distinguish target types, when you allow memory to be missing because a dump
//  target may not have that memory content by-design you are also implicitly
//  allowing that same data to be missing from a live debugging target.
// Also, be aware that these macros exist in code under vm\.  You must be careful to
//  only allow them to change execution for DAC and DBI.
// Be careful state is such that execution can continue if the target is missing
//  memory.
// In general, there are two solutions to this problem:
//  a) add the memory to all minidumps
//  b) stop forcing the memory to always be present
// All decisions between a & b focus on cost.  For a, cost is adding the memory & a complete
//  path to locate it to the dump, both in terms of dump generation time and most
//  especially in terms of dump size (we cannot make MiniDumpNormal many MB for trivial
//  apps).
//  For b, cost is that we lose some of our validation when we have to turn off asserts
//  and other checks for targets that should always have the missing memory present
//  because we have no concept of allowing it to be missing only from a dump.

// This seemingly awkward try block starting tag is so that when the macro is used over
//  multiple source lines we don't create a useless try/catch block.  This is important
//  when using the macros in vm\ code.
#define EX_TRY_ALLOW_DATATARGET_MISSING_MEMORY EX_TRY
#define EX_END_CATCH_ALLOW_DATATARGET_MISSING_MEMORY                                \
    EX_CATCH                                                                        \
    {                                                                               \
        if ((GET_EXCEPTION()->GetHR() != HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY)) && \
            (GET_EXCEPTION()->GetHR() != CORDBG_E_READVIRTUAL_FAILURE) )            \
        {                                                                           \
            EX_RETHROW;                                                             \
        }                                                                           \
    }                                                                               \
    EX_END_CATCH(SwallowAllExceptions)

#define EX_TRY_ALLOW_DATATARGET_MISSING_MEMORY_WITH_HANDLER EX_TRY
#define EX_CATCH_ALLOW_DATATARGET_MISSING_MEMORY_WITH_HANDLER                       \
    EX_CATCH                                                                        \
    {                                                                               \
        if ((GET_EXCEPTION()->GetHR() != HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY)) && \
            (GET_EXCEPTION()->GetHR() != CORDBG_E_READVIRTUAL_FAILURE) )            \
        {                                                                           \
            EX_RETHROW;                                                             \
        }                                                                           \
        else                                                                        \

#define EX_TRY_ALLOW_DATATARGET_MISSING_OR_INCONSISTENT_MEMORY EX_TRY
#define EX_END_CATCH_ALLOW_DATATARGET_MISSING_OR_INCONSISTENT_MEMORY                \
    EX_CATCH                                                                        \
    {                                                                               \
        if ((GET_EXCEPTION()->GetHR() != HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY)) && \
            (GET_EXCEPTION()->GetHR() != CORDBG_E_READVIRTUAL_FAILURE) &&           \
            (GET_EXCEPTION()->GetHR() != CORDBG_E_TARGET_INCONSISTENT))             \
        {                                                                           \
            EX_RETHROW;                                                             \
        }                                                                           \
    }                                                                               \
    EX_END_CATCH(SwallowAllExceptions)


#define EX_END_CATCH_ALLOW_DATATARGET_MISSING_MEMORY_WITH_HANDLER                  \
    }                                                                               \
    EX_END_CATCH(SwallowAllExceptions)

// Only use this version for wrapping single source lines, or you'll make debugging
// painful.
#define ALLOW_DATATARGET_MISSING_MEMORY(sourceCode)                             \
    EX_TRY                                                                      \
    {                                                                           \
        sourceCode                                                              \
    }                                                                           \
    EX_END_CATCH_ALLOW_DATATARGET_MISSING_MEMORY

#define ALLOW_DATATARGET_MISSING_OR_INCONSISTENT_MEMORY(sourceCode)             \
    EX_TRY                                                                      \
    {                                                                           \
        sourceCode                                                              \
    }                                                                           \
    EX_END_CATCH_ALLOW_DATATARGET_MISSING_OR_INCONSISTENT_MEMORY

#else
#define EX_TRY_ALLOW_DATATARGET_MISSING_MEMORY
#define EX_END_CATCH_ALLOW_DATATARGET_MISSING_MEMORY
#define EX_TRY_ALLOW_DATATARGET_MISSING_MEMORY_WITH_HANDLER \
    #error This macro is only intended for use in DAC code!
#define EX_CATCH_ALLOW_DATATARGET_MISSING_MEMORY_WITH_HANDLER \
    #error This macro is only intended for use in DAC code!
#define EX_END_CATCH_ALLOW_DATATARGET_MISSING_MEMORY_WITH_HANDLER \
    #error This macro is only intended for use in DAC code!


#define ALLOW_DATATARGET_MISSING_MEMORY(sourceCode)                             \
    sourceCode

#endif // defined(DACCESS_COMPILE) || defined(RIGHT_SIDE_COMPILE)


#endif //DEBUGGER_DUMPCOMMON_H
