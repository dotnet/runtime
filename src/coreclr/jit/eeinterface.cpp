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
        className     = "<no class name>";
    }

    if (includeNamespace && (namespaceName != nullptr) && (namespaceName[0] != '\0'))
        p->Printf("%s.%s", namespaceName, className);
    else
        p->Printf("%s", className);

    if (!includeInstantiation)
        return;

    char pref = '<';
    for (unsigned typeArgIndex = 0;; typeArgIndex++)
    {
        CORINFO_CLASS_HANDLE typeArg = info.compCompHnd->getTypeInstantiationArgument(clsHnd, typeArgIndex);

        if (typeArg == NO_CLASS_HANDLE)
            break;

        p->Printf("%c", pref);
        pref = ',';

        CorInfoType typ = info.compCompHnd->getTypeForPrimitiveValueClass(typeArg);
        if (typ == CORINFO_TYPE_UNDEF)
            eePrintType(p, typeArg, includeNamespace, true);
        else
            eePrintJitType(p, JITtype2varType(typ));
    }

    if (pref != '<')
        p->Printf(">");
}

void Compiler::eePrintMethod(StringPrinter*        p,
                             CORINFO_CLASS_HANDLE  clsHnd,
                             CORINFO_METHOD_HANDLE methHnd,
                             CORINFO_SIG_INFO*     sig,
                             bool                  includeNamespaces,
                             bool                  includeClassInstantiation,
                             bool                  includeMethodInstantiation,
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

    if (sig != nullptr)
    {
        size_t signatureIndex = p->GetLength();
        bool   failed         = true;

        auto closure = [&]() {
            p->Printf("(");

            CORINFO_ARG_LIST_HANDLE argLst = sig->args;
            for (unsigned i = 0; i < sig->numArgs; i++)
            {
                if (i > 0)
                    p->Printf(",");

                var_types type = eeGetArgType(argLst, sig);
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

            failed = false;
        };

        eeRunFunctorWithSPMIErrorTrap(closure);

        if (failed)
        {
            p->Truncate(signatureIndex);
            p->Printf("(<failed to print signature>)");
        }

        if (includeReturnType)
        {
            var_types retType = JITtype2varType(sig->retType);
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

    CORINFO_CLASS_HANDLE clsHnd = NO_CLASS_HANDLE;
    CORINFO_SIG_INFO     sig;
    bool                 success = eeRunFunctorWithSPMIErrorTrap([&]() {
        clsHnd = info.compCompHnd->getMethodClass(hnd);
        eeGetMethodSig(hnd, &sig);
    });

    CORINFO_SIG_INFO* pSig = success ? &sig : nullptr;
    StringPrinter     p(getAllocator(CMK_DebugOnly));
    eePrintMethod(&p, clsHnd, hnd, pSig,
                  /* includeNamespaces */ true,
                  /* includeClassInstantiation */ true,
                  /* includeMethodInstantiation */ true, includeReturnType, includeThisSpecifier);

    return p.GetBuffer();
}

#endif // defined(DEBUG) || defined(FEATURE_JIT_METHOD_PERF) || defined(FEATURE_SIMD)

/*****************************************************************************/
