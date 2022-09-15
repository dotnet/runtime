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

//------------------------------------------------------------------------
// StringPrinter::Printf:
//   Print a formatted string.
//
// Arguments:
//    format - the format
//
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

//------------------------------------------------------------------------
// eePrintJitType:
//   Print a JIT type.
//
// Arguments:
//    printer - the printer
//    jitType - the JIT type
//
void Compiler::eePrintJitType(StringPrinter* printer, var_types jitType)
{
    printer->Printf("%s", varTypeName(jitType));
}

//------------------------------------------------------------------------
// eePrintType:
//   Print a type given by a class handle.
//
// Arguments:
//    printer              - the printer
//    clsHnd               - Handle for the class
//    includeNamespace     - Whether to print namespaces before type names
//    includeInstantiation - Whether to print the instantiation of the class
//
void Compiler::eePrintType(StringPrinter*       printer,
                           CORINFO_CLASS_HANDLE clsHnd,
                           bool                 includeNamespace,
                           bool                 includeInstantiation)
{
    const char* namespaceName;
    const char* className = info.compCompHnd->getClassNameFromMetadata(clsHnd, &namespaceName);
    if (className == nullptr)
    {
        unsigned arrayRank = info.compCompHnd->getArrayRank(clsHnd);
        if (arrayRank > 0)
        {
            CORINFO_CLASS_HANDLE childClsHnd;
            CorInfoType          childType = info.compCompHnd->getChildType(clsHnd, &childClsHnd);
            if ((childType == CORINFO_TYPE_CLASS) || (childType == CORINFO_TYPE_VALUECLASS))
            {
                eePrintType(printer, childClsHnd, includeNamespace, includeInstantiation);
            }
            else
            {
                eePrintJitType(printer, JitType2PreciseVarType(childType));
            }

            printer->Printf("[");
            for (unsigned i = 1; i < arrayRank; i++)
            {
                printer->Printf(",");
            }
            printer->Printf("]");
            return;
        }

        namespaceName = nullptr;
        className     = "<unnamed>";
    }

    if (includeNamespace && (namespaceName != nullptr) && (namespaceName[0] != '\0'))
    {
        printer->Printf("%s.", namespaceName);
    }

    printer->Printf("%s", className);

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

        printer->Printf("%c", pref);
        pref = ',';
        eePrintTypeOrJitAlias(printer, typeArg, includeNamespace, true);
    }

    if (pref != '[')
    {
        printer->Printf("]");
    }
}

//------------------------------------------------------------------------
// eePrintTypeOrJitAlias:
//   Print a type given by a class handle. If the type is a primitive type,
//   prints its JIT alias.
//
// Arguments:
//    printer              - the printer
//    clsHnd               - Handle for the class
//    includeNamespace     - Whether to print namespaces before type names
//    includeInstantiation - Whether to print the instantiation of the class
//
void Compiler::eePrintTypeOrJitAlias(StringPrinter*       printer,
                                     CORINFO_CLASS_HANDLE clsHnd,
                                     bool                 includeNamespace,
                                     bool                 includeInstantiation)
{
    CorInfoType typ = info.compCompHnd->asCorInfoType(clsHnd);
    if ((typ == CORINFO_TYPE_CLASS) || (typ == CORINFO_TYPE_VALUECLASS))
    {
        eePrintType(printer, clsHnd, includeNamespace, includeInstantiation);
    }
    else
    {
        eePrintJitType(printer, JitType2PreciseVarType(typ));
    }
}

//------------------------------------------------------------------------
// eePrintMethod:
//   Print a method given by a method handle, its owning class handle and its
//   signature.
//
// Arguments:
//    printer                    - the printer
//    clsHnd                     - Handle for the owning class, or NO_CLASS_HANDLE to not print the class.
//    sig                        - The signature of the method.
//    includeNamespaces          - Whether to print namespaces before type names.
//    includeClassInstantiation  - Whether to print the class instantiation. Only valid when clsHnd is passed.
//    includeMethodInstantiation - Whether to print the method instantiation. Requires the signature to be passed.
//    includeSignature           - Whether to print the signature.
//    includeReturnType          - Whether to include the return type at the end.
//    includeThisSpecifier       - Whether to include a specifier at the end for whether the method is an instance
//    method.
//
void Compiler::eePrintMethod(StringPrinter*        printer,
                             CORINFO_CLASS_HANDLE  clsHnd,
                             CORINFO_METHOD_HANDLE methHnd,
                             CORINFO_SIG_INFO*     sig,
                             bool                  includeNamespaces,
                             bool                  includeClassInstantiation,
                             bool                  includeMethodInstantiation,
                             bool                  includeSignature,
                             bool                  includeReturnType,
                             bool                  includeThisSpecifier)
{
    if (clsHnd != NO_CLASS_HANDLE)
    {
        eePrintType(printer, clsHnd, includeNamespaces, includeClassInstantiation);
        printer->Printf(":");
    }

    const char* methName = info.compCompHnd->getMethodName(methHnd, nullptr);
    printer->Printf("%s", methName);

    if (includeMethodInstantiation && (sig->sigInst.methInstCount > 0))
    {
        printer->Printf("[");
        for (unsigned i = 0; i < sig->sigInst.methInstCount; i++)
        {
            if (i > 0)
            {
                printer->Printf(",");
            }

            eePrintTypeOrJitAlias(printer, sig->sigInst.methInst[i], includeNamespaces, true);
        }
        printer->Printf("]");
    }

    if (includeSignature)
    {
        printer->Printf("(");

        CORINFO_ARG_LIST_HANDLE argLst = sig->args;
        for (unsigned i = 0; i < sig->numArgs; i++)
        {
            if (i > 0)
                printer->Printf(",");

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
                        eePrintType(printer, clsHnd, includeNamespaces, true);
                        break;
                    }
                }

                    FALLTHROUGH;
                default:
                    eePrintJitType(printer, type);
                    break;
            }

            argLst = info.compCompHnd->getArgNext(argLst);
        }

        printer->Printf(")");

        if (includeReturnType)
        {
            var_types retType = JitType2PreciseVarType(sig->retType);
            if (retType != TYP_VOID)
            {
                printer->Printf(":");
                switch (retType)
                {
                    case TYP_REF:
                    case TYP_STRUCT:
                    {
                        CORINFO_CLASS_HANDLE clsHnd = sig->retTypeClass;
                        if (clsHnd != NO_CLASS_HANDLE)
                        {
                            eePrintType(printer, clsHnd, includeNamespaces, true);
                            break;
                        }
                    }
                        FALLTHROUGH;
                    default:
                        eePrintJitType(printer, retType);
                        break;
                }
            }
        }

        // Does it have a 'this' pointer? Don't count explicit this, which has
        // the this pointer type as the first element of the arg type list
        if (includeThisSpecifier && sig->hasThis() && !sig->hasExplicitThis())
        {
            printer->Printf(":this");
        }
    }
}

//------------------------------------------------------------------------
// eeGetMethodFullName:
//   Get a string describing a method.
//
// Arguments:
//    hnd                  - the method handle
//    includeReturnType    - Whether to include the return type in the string
//    includeThisSpecifier - Whether to include a specifier for whether this is an instance method.
//
// Returns:
//   The string.
//
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

    if (success)
    {
        return p.GetBuffer();
    }

    // Try without signature
    p.Truncate(0);

    success = eeRunFunctorWithSPMIErrorTrap([&]() {
        eePrintMethod(&p, clsHnd, hnd,
                      /* sig */ nullptr,
                      /* includeNamespaces */ true,
                      /* includeClassInstantiation */ false,
                      /* includeMethodInstantiation */ false,
                      /* includeSignature */ false,
                      /* includeReturnType */ false,
                      /* includeThisSpecifier */ false);
    });

    if (success)
    {
        return p.GetBuffer();
    }

    // Try with bare minimum
    p.Truncate(0);

    success = eeRunFunctorWithSPMIErrorTrap([&]() {
        eePrintMethod(&p, nullptr, hnd,
                      /* sig */ nullptr,
                      /* includeNamespaces */ true,
                      /* includeClassInstantiation */ false,
                      /* includeMethodInstantiation */ false,
                      /* includeSignature */ false,
                      /* includeReturnType */ false,
                      /* includeThisSpecifier */ false);
    });

    if (success)
    {
        return p.GetBuffer();
    }

    p.Truncate(0);
    p.Printf("hackishClassName:hackishMethodName(?)");
    return p.GetBuffer();
}

#endif // defined(DEBUG) || defined(FEATURE_JIT_METHOD_PERF) || defined(FEATURE_SIMD)

/*****************************************************************************/
