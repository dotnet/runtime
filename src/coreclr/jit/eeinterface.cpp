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
// StringPrinter::Grow:
//   Grow the internal buffer to a new specified size.
//
// Arguments:
//    newSize - the new size.
//
void StringPrinter::Grow(size_t newSize)
{
    assert(newSize > m_bufferMax);
    char* newBuffer = m_alloc.allocate<char>(newSize);
    memcpy(newBuffer, m_buffer, m_bufferIndex + 1); // copy null terminator too

    m_buffer    = newBuffer;
    m_bufferMax = newSize;
}

//------------------------------------------------------------------------
// StringPrinter::Append:
//   Append a substring to the internal buffer.
//
// Arguments:
//    str - the substring to append
//
void StringPrinter::Append(const char* str)
{
    size_t strLen = strlen(str);

    size_t newIndex = m_bufferIndex + strLen;

    if (newIndex >= m_bufferMax)
    {
        size_t newSize = m_bufferMax * 2;
        while (newIndex >= newSize)
        {
            newSize *= 2;
        }

        Grow(newSize);
    }

    memcpy(&m_buffer[m_bufferIndex], str, strLen + 1);
    m_bufferIndex += strLen;
}

//------------------------------------------------------------------------
// StringPrinter::Append:
//   Append a single character to the internal buffer.
//
// Arguments:
//    chr - the character
//
void StringPrinter::Append(char chr)
{
    if (m_bufferIndex + 1 >= m_bufferMax)
    {
        Grow(m_bufferMax * 2);
    }

    m_buffer[m_bufferIndex]     = chr;
    m_buffer[m_bufferIndex + 1] = '\0';
    m_bufferIndex++;
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
    printer->Append(varTypeName(jitType));
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

            printer->Append('[');
            for (unsigned i = 1; i < arrayRank; i++)
            {
                printer->Append(',');
            }
            printer->Append(']');
            return;
        }

        namespaceName = nullptr;
        className     = "<unnamed>";
    }

    if (includeNamespace && (namespaceName != nullptr) && (namespaceName[0] != '\0'))
    {
        printer->Append(namespaceName);
        printer->Append('.');
    }

    printer->Append(className);

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

        printer->Append(pref);
        pref = ',';
        eePrintTypeOrJitAlias(printer, typeArg, includeNamespace, true);
    }

    if (pref != '[')
    {
        printer->Append(']');
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
        printer->Append(':');
    }

    const char* methName = info.compCompHnd->getMethodName(methHnd, nullptr);
    printer->Append(methName);

    if (includeMethodInstantiation && (sig->sigInst.methInstCount > 0))
    {
        printer->Append('[');
        for (unsigned i = 0; i < sig->sigInst.methInstCount; i++)
        {
            if (i > 0)
            {
                printer->Append(',');
            }

            eePrintTypeOrJitAlias(printer, sig->sigInst.methInst[i], includeNamespaces, true);
        }
        printer->Append(']');
    }

    if (includeSignature)
    {
        printer->Append('(');

        CORINFO_ARG_LIST_HANDLE argLst = sig->args;
        for (unsigned i = 0; i < sig->numArgs; i++)
        {
            if (i > 0)
                printer->Append(',');

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

        printer->Append(')');

        if (includeReturnType)
        {
            var_types retType = JitType2PreciseVarType(sig->retType);
            if (retType != TYP_VOID)
            {
                printer->Append(':');
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
            printer->Append(":this");
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
    p.Append("hackishClassName:hackishMethodName(?)");
    return p.GetBuffer();
}

#endif // defined(DEBUG) || defined(FEATURE_JIT_METHOD_PERF) || defined(FEATURE_SIMD)

/*****************************************************************************/
