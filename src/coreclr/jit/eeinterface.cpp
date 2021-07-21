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

#pragma warning(push)
#pragma warning(disable : 4701) // difficult to get rid of C4701 with 'sig' below

/*****************************************************************************/

/*****************************************************************************
 *
 *  Filter wrapper to handle exception filtering.
 *  On Unix compilers don't support SEH.
 */

struct FilterSuperPMIExceptionsParam_eeinterface
{
    Compiler*               pThis;
    Compiler::Info*         pJitInfo;
    bool                    hasThis;
    size_t                  siglength;
    CORINFO_SIG_INFO        sig;
    CORINFO_ARG_LIST_HANDLE argLst;
    CORINFO_METHOD_HANDLE   hnd;
    const char*             returnType;
    const char**            pArgNames;
    EXCEPTION_POINTERS      exceptionPointers;
};

const char* Compiler::eeGetMethodFullName(CORINFO_METHOD_HANDLE hnd)
{
    const char* className;
    const char* methodName = eeGetMethodName(hnd, &className);
    if ((eeGetHelperNum(hnd) != CORINFO_HELP_UNDEF) || eeIsNativeMethod(hnd))
    {
        return methodName;
    }

    FilterSuperPMIExceptionsParam_eeinterface param;
    param.returnType = nullptr;
    param.pThis      = this;
    param.hasThis    = false;
    param.siglength  = 0;
    param.hnd        = hnd;
    param.pJitInfo   = &info;

    size_t   length = 0;
    unsigned i;

    /* Generating the full signature is a two-pass process. First we have to walk
       the components in order to assess the total size, then we allocate the buffer
       and copy the elements into it.
     */

    /* Right now there is a race-condition in the EE, className can be nullptr */

    /* initialize length with length of className and '.' */

    if (className)
    {
        length = strlen(className) + 1;
    }
    else
    {
        assert(strlen("<NULL>.") == 7);
        length = 7;
    }

    /* add length of methodName and opening bracket */
    length += strlen(methodName) + 1;

    bool success = eeRunWithSPMIErrorTrap<FilterSuperPMIExceptionsParam_eeinterface>(
        [](FilterSuperPMIExceptionsParam_eeinterface* pParam) {

            /* figure out the signature */

            pParam->pThis->eeGetMethodSig(pParam->hnd, &pParam->sig);

            // allocate space to hold the class names for each of the parameters

            if (pParam->sig.numArgs > 0)
            {
                pParam->pArgNames =
                    pParam->pThis->getAllocator(CMK_DebugOnly).allocate<const char*>(pParam->sig.numArgs);
            }
            else
            {
                pParam->pArgNames = nullptr;
            }

            unsigned i;
            pParam->argLst = pParam->sig.args;

            for (i = 0; i < pParam->sig.numArgs; i++)
            {
                var_types type = pParam->pThis->eeGetArgType(pParam->argLst, &pParam->sig);
                switch (type)
                {
                    case TYP_REF:
                    case TYP_STRUCT:
                    {
                        CORINFO_CLASS_HANDLE clsHnd = pParam->pThis->eeGetArgClass(&pParam->sig, pParam->argLst);
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
                pParam->argLst = pParam->pJitInfo->compCompHnd->getArgNext(pParam->argLst);
            }

            /* add ',' if there is more than one argument */

            if (pParam->sig.numArgs > 1)
            {
                pParam->siglength += (pParam->sig.numArgs - 1);
            }

            var_types retType = JITtype2varType(pParam->sig.retType);
            if (retType != TYP_VOID)
            {
                switch (retType)
                {
                    case TYP_REF:
                    case TYP_STRUCT:
                    {
                        CORINFO_CLASS_HANDLE clsHnd = pParam->sig.retTypeClass;
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

            // Does it have a 'this' pointer? Don't count explicit this, which has the this pointer type as the first
            // element of the arg type list
            if (pParam->sig.hasThis() && !pParam->sig.hasExplicitThis())
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

    /* add closing bracket and null terminator */

    length += param.siglength + 2;

    char* retName = getAllocator(CMK_DebugOnly).allocate<char>(length);

    /* Now generate the full signature string in the allocated buffer */

    if (className)
    {
        strcpy_s(retName, length, className);
        strcat_s(retName, length, ":");
    }
    else
    {
        strcpy_s(retName, length, "<NULL>.");
    }

    strcat_s(retName, length, methodName);

    // append the signature
    strcat_s(retName, length, "(");

    if (param.siglength > 0)
    {
        param.argLst = param.sig.args;

        for (i = 0; i < param.sig.numArgs; i++)
        {
            var_types type = eeGetArgType(param.argLst, &param.sig);
            strcat_s(retName, length, param.pArgNames[i]);
            param.argLst = info.compCompHnd->getArgNext(param.argLst);
            if (i + 1 < param.sig.numArgs)
            {
                strcat_s(retName, length, ",");
            }
        }
    }

    strcat_s(retName, length, ")");

    if (param.returnType != nullptr)
    {
        strcat_s(retName, length, ":");
        strcat_s(retName, length, param.returnType);
    }

    if (param.hasThis)
    {
        strcat_s(retName, length, ":this");
    }

    assert(strlen(retName) == (length - 1));

    return (retName);
}

#pragma warning(pop)

#endif // defined(DEBUG) || defined(FEATURE_JIT_METHOD_PERF) || defined(FEATURE_SIMD)

/*****************************************************************************/
