// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          EEInterface                                      XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

// ONLY FUNCTIONS common to all variants of the JIT (EXE, DLL) should go here)
// otherwise they belong in the corresponding directory.

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(DEBUG) || defined(FEATURE_JIT_METHOD_PERF) || defined(FEATURE_SIMD)

/*****************************************************************************/

void Compiler::eeFormatMethodName(char**            buffer,
                                  size_t            bufferMax,
                                  const char*       className,
                                  const char*       methodName,
                                  CORINFO_SIG_INFO* sig,
                                  bool              includeReturnType,
                                  bool              includeThis)
{
    if (*buffer == nullptr)
    {
        *buffer   = new (this, CMK_DebugOnly) char[64];
        bufferMax = 64;
    }

    size_t bufferIndex = 0;
    auto ensureChars   = [&](size_t numChars) {
        if (bufferIndex + numChars >= bufferMax) // >= due to null terminator
        {
            size_t newSize = max(bufferMax, 32);
            while (bufferIndex + numChars >= newSize)
                newSize *= 2;

            char* newBuffer = new (this, CMK_DebugOnly) char[newSize];
            memcpy(newBuffer, *buffer, bufferIndex);

            *buffer   = newBuffer;
            bufferMax = newSize;
        }
    };

    if (className != nullptr)
    {
        ensureChars(strlen(className) + 1);
        bufferIndex += sprintf(*buffer + bufferIndex, "%s:", className);
    }

    ensureChars(strlen(methodName));
    bufferIndex += sprintf(*buffer + bufferIndex, "%s", methodName);

    if (sig != nullptr)
    {
        size_t signatureIndex = bufferIndex;
        bool   failed         = true;

        char sigTypeBuffer[512];

        auto closure = [&]() {
            ensureChars(1);
            bufferIndex += sprintf(*buffer + bufferIndex, "(");

            CORINFO_ARG_LIST_HANDLE argLst = sig->args;
            for (unsigned i = 0; i < sig->numArgs; i++)
            {
                const char* result;
                var_types   type = eeGetArgType(argLst, sig);
                switch (type)
                {
                    case TYP_REF:
                    case TYP_STRUCT:
                    {
                        CORINFO_CLASS_HANDLE clsHnd = eeGetArgClass(sig, argLst);
                        // For some SIMD struct types we can get a nullptr back from eeGetArgClass on Linux/X64
                        if (clsHnd != NO_CLASS_HANDLE)
                        {
                            char* sigParamType = sigTypeBuffer;
                            eeFormatClassName(&sigParamType, ArrLen(sigTypeBuffer), clsHnd);
                            result = sigParamType;
                            break;
                        }
                    }

                        FALLTHROUGH;
                    default:
                        result = varTypeName(type);
                        break;
                }

                ensureChars(strlen(result));
                bufferIndex += sprintf(*buffer + bufferIndex, "%s", result);
                if (i != sig->numArgs - 1)
                {
                    ensureChars(1);
                    bufferIndex += sprintf(*buffer + bufferIndex, ",");
                }

                argLst = info.compCompHnd->getArgNext(argLst);
            }

            ensureChars(1);
            bufferIndex += sprintf(*buffer + bufferIndex, ")");

            failed = false;
        };

        eeRunFunctorWithSPMIErrorTrap(closure);

        if (failed)
        {
            bufferIndex = signatureIndex;
            ensureChars(strlen("(<failed to print signature>)"));
            bufferIndex += sprintf(*buffer + bufferIndex, "(<failed to print signature>)");
        }

        if (includeReturnType)
        {
            const char* result;

            var_types retType = JITtype2varType(sig->retType);
            if (retType != TYP_VOID)
            {
                switch (retType)
                {
                    case TYP_REF:
                    case TYP_STRUCT:
                    {
                        CORINFO_CLASS_HANDLE clsHnd = sig->retTypeClass;
                        if (clsHnd != NO_CLASS_HANDLE)
                        {
                            char* sigType = sigTypeBuffer;
                            eeFormatClassName(&sigType, ArrLen(sigTypeBuffer), clsHnd);
                            result = sigType;
                            break;
                        }
                    }
                        FALLTHROUGH;
                    default:
                        result = varTypeName(retType);
                        break;
                }

                ensureChars(1 + strlen(result));
                bufferIndex += sprintf(*buffer + bufferIndex, ":%s", result);
            }
        }

        // Does it have a 'this' pointer? Don't count explicit this, which has the this pointer type as the
        // first
        // element of the arg type list
        if (includeThis && sig->hasThis() && !sig->hasExplicitThis())
        {
            ensureChars(strlen(":this"));
            bufferIndex += sprintf(*buffer + bufferIndex, ":this");
        }
    }

    (*buffer)[bufferIndex] = '\0';
}

void Compiler::eeFormatClassName(char** buffer, size_t bufferMax, CORINFO_CLASS_HANDLE clsHnd)
{
    bool failed  = true;
    auto closure = [&]() {
        int len       = 0;
        int lenNeeded = info.compCompHnd->appendClassName(nullptr, &len, clsHnd, true, false, false);
        lenNeeded++; // null terminator

        char16_t* classNameStr;
        if (lenNeeded < 4096)
            classNameStr = static_cast<char16_t*>(_alloca(static_cast<size_t>(lenNeeded) * sizeof(char16_t)));
        else
            classNameStr = new (this, CMK_DebugOnly) char16_t[lenNeeded];

        len                        = lenNeeded;
        char16_t* classNameStrCopy = classNameStr;
        int       result = info.compCompHnd->appendClassName(&classNameStrCopy, &len, clsHnd, true, false, false);
        assert(result == lenNeeded - 1);
        assert(classNameStr[result] == 0);

        int utf8Len = WszWideCharToMultiByte(CP_UTF8, 0, (wchar_t*)classNameStr, -1, nullptr, 0, nullptr, nullptr);
        if (utf8Len == 0)
            return;

        if (bufferMax < static_cast<size_t>(utf8Len))
        {
            *buffer   = new (this, CMK_DebugOnly) char[utf8Len];
            bufferMax = static_cast<size_t>(utf8Len);
        }

        failed =
            WszWideCharToMultiByte(CP_UTF8, 0, (wchar_t*)classNameStr, -1, *buffer, utf8Len, nullptr, nullptr) == 0;
    };

    eeRunFunctorWithSPMIErrorTrap(closure);

    if (failed)
    {
        const char placeholderName[] = "hackishClassName";
        if (bufferMax < ArrLen(placeholderName))
        {
            *buffer   = new (this, CMK_DebugOnly) char[ArrLen(placeholderName)];
            bufferMax = ArrLen(placeholderName);
        }

        strcpy_s(*buffer, bufferMax, placeholderName);
    }
}

const char* Compiler::eeGetMethodFullName(CORINFO_METHOD_HANDLE hnd, bool includeReturnType, bool includeThisSpecifier)
{
    const char* className;
    const char* methodName = eeGetMethodName(hnd, &className);
    if ((eeGetHelperNum(hnd) != CORINFO_HELP_UNDEF) || eeIsNativeMethod(hnd))
    {
        return methodName;
    }

    char* fullClassName = nullptr;
    eeFormatClassName(&fullClassName, 0, info.compCompHnd->getMethodClass(hnd));

    struct FilterSPMIParams
    {
        Compiler*             pThis;
        CORINFO_METHOD_HANDLE hnd;
        CORINFO_SIG_INFO      sig;
    };

    FilterSPMIParams param;
    param.pThis = this;
    param.hnd   = hnd;

    bool success = eeRunWithSPMIErrorTrap<
        FilterSPMIParams>([](FilterSPMIParams* pParam) { pParam->pThis->eeGetMethodSig(pParam->hnd, &pParam->sig); },
                          &param);

    CORINFO_SIG_INFO* psig    = success ? &param.sig : nullptr;
    char*             retName = nullptr;
    eeFormatMethodName(&retName, 0, fullClassName, methodName, psig, includeReturnType, includeThisSpecifier);
    return retName;
}

#endif // defined(DEBUG) || defined(FEATURE_JIT_METHOD_PERF) || defined(FEATURE_SIMD)

/*****************************************************************************/
