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

void StringPrinter::Printf(const char* format, ...)
{
    va_list args;
    va_start(args, format);

    while (true)
    {
        size_t bufferLeft = m_bufferMax - m_bufferIndex;
        assert(bufferLeft >= 1); // always fit null terminator

        va_list argsCopy;
        va_copy(argsCopy, args);
        int printed = _vsnprintf_s(m_buffer + m_bufferIndex, bufferLeft, _TRUNCATE, format, argsCopy);
        va_end(argsCopy);

        if (printed < 0)
        {
            // buffer too small
            size_t newSize   = m_bufferMax * 2;
            char*  newBuffer = m_alloc.allocate<char>(newSize);
            memcpy(newBuffer, m_buffer, m_bufferIndex + 1); // copy null terminator too

            m_buffer    = newBuffer;
            m_bufferMax = newSize;
        }
        else
        {
            m_bufferIndex = m_bufferIndex + static_cast<size_t>(printed);
            break;
        }
    }

    va_end(args);
}

#if defined(DEBUG) || defined(FEATURE_JIT_METHOD_PERF) || defined(FEATURE_SIMD)

void Compiler::eePrintJitType(StringPrinter* p, var_types jitType)
{
    p->Printf("%s", varTypeName(jitType));
}

void Compiler::eePrintType(StringPrinter*       p,
                           CORINFO_CLASS_HANDLE clsHnd,
                           bool                 includeNamespace,
                           bool                 includeInstantiation)
{
    const char* namespaceName;
    const char* className = info.compCompHnd->getClassNameFromMetadata(clsHnd, &namespaceName);
    if (className == nullptr)
    {
        namespaceName = nullptr;
        className     = "<unnamed>";
    }

    if (includeNamespace && (namespaceName != nullptr) && (namespaceName[0] != '\0'))
    {
        p->Printf("%s.", namespaceName);
    }

    p->Printf("%s", className);

    if (!includeInstantiation)
    {
        return;
    }

    char pref = '[';
    for (unsigned typeArgIndex = 0;; typeArgIndex++)
    {
        CORINFO_CLASS_HANDLE typeArg = info.compCompHnd->getTypeInstantiationArgument(clsHnd, typeArgIndex);

        if (typeArg == NO_CLASS_HANDLE)
        {
            break;
        }

        p->Printf("%c", pref);
        pref = ',';
        eePrintTypeOrJitAlias(p, typeArg, includeNamespace, true);
    }

    if (pref != '[')
    {
        p->Printf("]");
    }
}

void Compiler::eePrintTypeOrJitAlias(StringPrinter*       p,
                                     CORINFO_CLASS_HANDLE clsHnd,
                                     bool                 includeNamespace,
                                     bool                 includeInstantiation)
{
    CorInfoType typ = info.compCompHnd->asCorInfoType(clsHnd);
    if ((typ == CORINFO_TYPE_CLASS) || (typ == CORINFO_TYPE_VALUECLASS))
    {
        eePrintType(p, clsHnd, includeNamespace, includeInstantiation);
    }
    else
    {
        eePrintJitType(p, JitType2PreciseVarType(typ));
    }
}

void Compiler::eePrintMethod(StringPrinter*        p,
                             CORINFO_CLASS_HANDLE  clsHnd,
                             CORINFO_METHOD_HANDLE methHnd,
                             CORINFO_SIG_INFO*     sig,
                             bool                  includeNamespaces,
                             bool                  includeClassInstantiation,
                             bool                  includeMethodInstantiation,
                             bool                  includeSignature,
                             bool                  includeReturnType,
                             bool                  includeThis)
{
    if (clsHnd != NO_CLASS_HANDLE)
    {
        eePrintType(p, clsHnd, includeNamespaces, includeClassInstantiation);
        p->Printf(":");
    }

    const char* methName = info.compCompHnd->getMethodName(methHnd, nullptr);
    p->Printf("%s", methName);

    if (includeMethodInstantiation && (sig->sigInst.methInstCount > 0))
    {
        p->Printf("[");
        for (unsigned i = 0; i < sig->sigInst.methInstCount; i++)
        {
            if (i > 0)
            {
                p->Printf(",");
            }

            eePrintTypeOrJitAlias(p, sig->sigInst.methInst[i], includeNamespaces, true);
        }
        p->Printf("]");
    }

    if (includeSignature)
    {
        p->Printf("(");

        CORINFO_ARG_LIST_HANDLE argLst = sig->args;
        for (unsigned i = 0; i < sig->numArgs; i++)
        {
            if (i > 0)
                p->Printf(",");

            CORINFO_CLASS_HANDLE vcClsHnd;
            var_types type = JitType2PreciseVarType(strip(info.compCompHnd->getArgType(sig, argLst, &vcClsHnd)));
            switch (type)
            {
                case TYP_REF:
                case TYP_STRUCT:
                {
                    CORINFO_CLASS_HANDLE clsHnd = eeGetArgClass(sig, argLst);
                    // For some SIMD struct types we can get a nullptr back from eeGetArgClass on Linux/X64
                    if (clsHnd != NO_CLASS_HANDLE)
                    {
                        eePrintType(p, clsHnd, includeNamespaces, true);
                        break;
                    }
                }

                    FALLTHROUGH;
                default:
                    eePrintJitType(p, type);
                    break;
            }

            argLst = info.compCompHnd->getArgNext(argLst);
        }

        p->Printf(")");

        if (includeReturnType)
        {
            var_types retType = JitType2PreciseVarType(sig->retType);
            if (retType != TYP_VOID)
            {
                p->Printf(":");
                switch (retType)
                {
                    case TYP_REF:
                    case TYP_STRUCT:
                    {
                        CORINFO_CLASS_HANDLE clsHnd = sig->retTypeClass;
                        if (clsHnd != NO_CLASS_HANDLE)
                        {
                            eePrintType(p, clsHnd, includeNamespaces, true);
                            break;
                        }
                    }
                        FALLTHROUGH;
                    default:
                        eePrintJitType(p, retType);
                        break;
                }
            }
        }

        // Does it have a 'this' pointer? Don't count explicit this, which has
        // the this pointer type as the first element of the arg type list
        if (includeThis && sig->hasThis() && !sig->hasExplicitThis())
        {
            p->Printf(":this");
        }
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

    StringPrinter        p(getAllocator(CMK_DebugOnly));
    CORINFO_CLASS_HANDLE clsHnd  = NO_CLASS_HANDLE;
    bool                 success = eeRunFunctorWithSPMIErrorTrap([&]() {
        clsHnd = info.compCompHnd->getMethodClass(hnd);
        CORINFO_SIG_INFO sig;
        eeGetMethodSig(hnd, &sig);
        eePrintMethod(&p, clsHnd, hnd, &sig,
                      /* includeNamespaces */ true,
                      /* includeClassInstantiation */ true,
                      /* includeMethodInstantiation */ true,
                      /* includeSignature */ true, includeReturnType, includeThisSpecifier);

    });

    if (!success)
    {
        // Try with bare minimum
        p.Truncate(0);

        success = eeRunFunctorWithSPMIErrorTrap([&]() {
            eePrintMethod(&p, clsHnd, hnd,
                          /* sig */ nullptr,
                          /* includeNamespaces */ true,
                          /* includeClassInstantiation */ false,
                          /* includeMethodInstantiation */ false,
                          /* includeSignature */ false, includeReturnType, includeThisSpecifier);
        });

        if (!success)
        {
            p.Truncate(0);
            p.Printf("hackishClassName:hackishMethodName(?)");
        }
    }

    return p.GetBuffer();
}

#endif // defined(DEBUG) || defined(FEATURE_JIT_METHOD_PERF) || defined(FEATURE_SIMD)

/*****************************************************************************/
