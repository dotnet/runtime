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
    struct FilterSPMIParams
    {
        Compiler*         pThis;
        bool              hasThis;
        size_t            siglength;
        CORINFO_SIG_INFO* psig;
        const char*       returnType;
        const char**      pArgNames;
        bool              includeReturnType;
        bool              includeThis;
    };

    FilterSPMIParams param;
    param.pThis             = this;
    param.hasThis           = false;
    param.siglength         = 0;
    param.psig              = sig;
    param.returnType        = nullptr;
    param.pArgNames         = nullptr;
    param.includeReturnType = includeReturnType;
    param.includeThis       = includeThis;

    size_t   length = 0;
    unsigned i;

    /* Generating the full signature is a two-pass process. First we have to walk
       the components in order to assess the total size, then we allocate the buffer
       and copy the elements into it.
     */

    /* initialize length with length of className and ':' */

    if (className != nullptr)
    {
        length = strlen(className) + 1;
    }
    else
    {
        // no class name
        length = 0;
    }

    /* add length of methodName */
    length += strlen(methodName);

    if (sig != nullptr)
    {
        length += 2; // "()" for signature

        bool success = eeRunWithSPMIErrorTrap<FilterSPMIParams>(
            [](FilterSPMIParams* pParam) {

                // allocate space to hold the class names for each of the parameters

                if (pParam->psig->numArgs > 0)
                {
                    pParam->pArgNames =
                        pParam->pThis->getAllocator(CMK_DebugOnly).allocate<const char*>(pParam->psig->numArgs);
                }
                else
                {
                    pParam->pArgNames = nullptr;
                }

                unsigned                i;
                CORINFO_ARG_LIST_HANDLE argLst = pParam->psig->args;

                for (i = 0; i < pParam->psig->numArgs; i++)
                {
                    var_types type = pParam->pThis->eeGetArgType(argLst, pParam->psig);
                    switch (type)
                    {
                        case TYP_REF:
                        case TYP_STRUCT:
                        {
                            CORINFO_CLASS_HANDLE clsHnd = pParam->pThis->eeGetArgClass(pParam->psig, argLst);
                            // For some SIMD struct types we can get a nullptr back from eeGetArgClass on Linux/X64
                            if (clsHnd != NO_CLASS_HANDLE)
                            {
                                const char* clsName = pParam->pThis->eeGetClassName(clsHnd);
                                if (clsName != nullptr)
                                {
                                    pParam->pArgNames[i] = clsName;
                                    break;
                                }
                            }
                        }
                            FALLTHROUGH;
                        default:
                            pParam->pArgNames[i] = varTypeName(type);
                            break;
                    }
                    pParam->siglength += strlen(pParam->pArgNames[i]);
                    argLst = pParam->pThis->info.compCompHnd->getArgNext(argLst);
                }

                /* add ',' if there is more than one argument */

                if (pParam->psig->numArgs > 1)
                {
                    pParam->siglength += (pParam->psig->numArgs - 1);
                }

                if (pParam->includeReturnType)
                {
                    var_types retType = JITtype2varType(pParam->psig->retType);
                    if (retType != TYP_VOID)
                    {
                        switch (retType)
                        {
                            case TYP_REF:
                            case TYP_STRUCT:
                            {
                                CORINFO_CLASS_HANDLE clsHnd = pParam->psig->retTypeClass;
                                if (clsHnd != NO_CLASS_HANDLE)
                                {
                                    const char* clsName = pParam->pThis->eeGetClassName(clsHnd);
                                    if (clsName != nullptr)
                                    {
                                        pParam->returnType = clsName;
                                        break;
                                    }
                                }
                            }
                                FALLTHROUGH;
                            default:
                                pParam->returnType = varTypeName(retType);
                                break;
                        }
                        pParam->siglength += strlen(pParam->returnType) + 1; // don't forget the delimiter ':'
                    }
                }

                // Does it have a 'this' pointer? Don't count explicit this, which has the this pointer type as the
                // first
                // element of the arg type list
                if (pParam->includeThis && pParam->psig->hasThis() && !pParam->psig->hasExplicitThis())
                {
                    assert(strlen(":this") == 5);
                    pParam->siglength += 5;
                    pParam->hasThis = true;
                }
            },
            &param);

        if (!success)
        {
            param.siglength = 0;
        }
    }

    /* add null terminator */

    length += param.siglength + 1;

    char* retName = *buffer;
    if (length > bufferMax)
        *buffer = retName = getAllocator(CMK_DebugOnly).allocate<char>(length);

    retName[0] = '\0';
    /* Now generate the full signature string in the allocated buffer */

    if (className != nullptr)
    {
        strcpy_s(retName, length, className);
        strcat_s(retName, length, ":");
    }

    strcat_s(retName, length, methodName);

    if (sig != nullptr)
    {
        // append the signature
        strcat_s(retName, length, "(");

        if (param.siglength > 0)
        {
            CORINFO_ARG_LIST_HANDLE argLst = sig->args;

            for (i = 0; i < sig->numArgs; i++)
            {
                var_types type = eeGetArgType(argLst, sig);
                strcat_s(retName, length, param.pArgNames[i]);
                argLst = info.compCompHnd->getArgNext(argLst);
                if (i + 1 < sig->numArgs)
                {
                    strcat_s(retName, length, ",");
                }
            }
        }

        strcat_s(retName, length, ")");

        if (includeReturnType && param.returnType != nullptr)
        {
            strcat_s(retName, length, ":");
            strcat_s(retName, length, param.returnType);
        }

        if (includeThis && param.hasThis)
        {
            strcat_s(retName, length, ":this");
        }
    }

    assert(strlen(retName) == (length - 1));
}

const char* Compiler::eeGetMethodFullName(CORINFO_METHOD_HANDLE hnd)
{
    const char* className;
    const char* methodName = eeGetMethodName(hnd, &className);
    if ((eeGetHelperNum(hnd) != CORINFO_HELP_UNDEF) || eeIsNativeMethod(hnd))
    {
        return methodName;
    }

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
    eeFormatMethodName(&retName, 0, className, methodName, psig,
                       /* includeReturnType */ true,
                       /* includeThis */ true);
    return retName;
}

#endif // defined(DEBUG) || defined(FEATURE_JIT_METHOD_PERF) || defined(FEATURE_SIMD)

/*****************************************************************************/
