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
// eeAppendPrint:
//   Append the output of one of the JIT-EE 'print' functions to a StringPrinter.
//
// Arguments:
//    printer - the printer
//    print   - A functor to print the string that follows the conventions of the JIT-EE print* functions.
//
template <typename TPrint>
void Compiler::eeAppendPrint(StringPrinter* printer, TPrint print)
{
    size_t requiredBufferSize;
    char   buffer[256];
    size_t printed = print(buffer, sizeof(buffer), &requiredBufferSize);
    if (requiredBufferSize <= sizeof(buffer))
    {
        assert(printed == requiredBufferSize - 1);
        printer->Append(buffer);
    }
    else
    {
        char* pBuffer = new (this, CMK_DebugOnly) char[requiredBufferSize];
        printed       = print(pBuffer, requiredBufferSize, nullptr);
        assert(printed == requiredBufferSize - 1);
        printer->Append(pBuffer);
    }
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
void Compiler::eePrintType(StringPrinter* printer, CORINFO_CLASS_HANDLE clsHnd, bool includeInstantiation)
{
    unsigned arrayRank = info.compCompHnd->getArrayRank(clsHnd);
    if (arrayRank > 0)
    {
        CORINFO_CLASS_HANDLE childClsHnd;
        CorInfoType          childType = info.compCompHnd->getChildType(clsHnd, &childClsHnd);
        if ((childType == CORINFO_TYPE_CLASS) || (childType == CORINFO_TYPE_VALUECLASS))
        {
            eePrintType(printer, childClsHnd, includeInstantiation);
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

    eeAppendPrint(printer, [&](char* buffer, size_t bufferSize, size_t* requiredBufferSize) {
        return info.compCompHnd->printClassName(clsHnd, buffer, bufferSize, requiredBufferSize);
    });

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
        eePrintTypeOrJitAlias(printer, typeArg, true);
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
//    includeInstantiation - Whether to print the instantiation of the class
//
void Compiler::eePrintTypeOrJitAlias(StringPrinter* printer, CORINFO_CLASS_HANDLE clsHnd, bool includeInstantiation)
{
    CorInfoType typ = info.compCompHnd->asCorInfoType(clsHnd);
    if ((typ == CORINFO_TYPE_CLASS) || (typ == CORINFO_TYPE_VALUECLASS))
    {
        eePrintType(printer, clsHnd, includeInstantiation);
    }
    else
    {
        eePrintJitType(printer, JitType2PreciseVarType(typ));
    }
}

static const char* s_jitHelperNames[CORINFO_HELP_COUNT] = {
#define JITHELPER(code, pfnHelper, sig)        #code,
#define DYNAMICJITHELPER(code, pfnHelper, sig) #code,
#include "jithelpers.h"
};

//------------------------------------------------------------------------
// eePrintMethod:
//   Print a method given by a method handle, its owning class handle and its
//   signature.
//
// Arguments:
//    printer                    - the printer
//    clsHnd                     - Handle for the owning class, or NO_CLASS_HANDLE to not print the class.
//    sig                        - The signature of the method.
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
                             bool                  includeClassInstantiation,
                             bool                  includeMethodInstantiation,
                             bool                  includeSignature,
                             bool                  includeReturnType,
                             bool                  includeThisSpecifier)
{
    CorInfoHelpFunc helper = eeGetHelperNum(methHnd);
    if (helper != CORINFO_HELP_UNDEF)
    {
        assert(helper < CORINFO_HELP_COUNT);
        printer->Append(s_jitHelperNames[helper]);
        return;
    }

    if (clsHnd != NO_CLASS_HANDLE)
    {
        eePrintType(printer, clsHnd, includeClassInstantiation);
        printer->Append(':');
    }

    eeAppendPrint(printer, [&](char* buffer, size_t bufferSize, size_t* requiredBufferSize) {
        return info.compCompHnd->printMethodName(methHnd, buffer, bufferSize, requiredBufferSize);
    });

    if (includeMethodInstantiation && (sig->sigInst.methInstCount > 0))
    {
        printer->Append('[');
        for (unsigned i = 0; i < sig->sigInst.methInstCount; i++)
        {
            if (i > 0)
            {
                printer->Append(',');
            }

            eePrintTypeOrJitAlias(printer, sig->sigInst.methInst[i], true);
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
                        eePrintType(printer, clsHnd, true);
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
                            eePrintType(printer, clsHnd, true);
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
// eePrintField:
//   Print a field name to a StringPrinter.
//
// Arguments:
//    printer     - the printer
//    fld         - The field
//    includeType - Whether to prefix the string by <class name>:
//
void Compiler::eePrintField(StringPrinter* printer, CORINFO_FIELD_HANDLE fld, bool includeType)
{
    if (includeType)
    {
        CORINFO_CLASS_HANDLE cls = info.compCompHnd->getFieldClass(fld);
        eePrintType(printer, cls, true);
        printer->Append(':');
    }

    eeAppendPrint(printer, [&](char* buffer, size_t bufferSize, size_t* requiredBufferSize) {
        return info.compCompHnd->printFieldName(fld, buffer, bufferSize, requiredBufferSize);
    });
}

//------------------------------------------------------------------------
// eeGetMethodFullName:
//   Get a string describing a method.
//
// Arguments:
//    hnd                  - the method handle
//    includeReturnType    - Whether to include the return type in the string
//    includeThisSpecifier - Whether to include a specifier for whether this is an instance method.
//    buffer               - Preexisting buffer to use (can be nullptr to allocate on heap).
//    bufferSize           - Size of preexisting buffer.
//
// Remarks:
//   If the final string is larger than the preexisting buffer can contain then
//   the string will be jit memory allocated.
//
// Returns:
//   The string.
//
const char* Compiler::eeGetMethodFullName(
    CORINFO_METHOD_HANDLE hnd, bool includeReturnType, bool includeThisSpecifier, char* buffer, size_t bufferSize)
{
    CorInfoHelpFunc helper = eeGetHelperNum(hnd);
    if (helper != CORINFO_HELP_UNDEF)
    {
        return s_jitHelperNames[helper];
    }

    StringPrinter        p(getAllocator(CMK_DebugOnly), buffer, bufferSize);
    CORINFO_CLASS_HANDLE clsHnd  = NO_CLASS_HANDLE;
    bool                 success = eeRunFunctorWithSPMIErrorTrap([&]() {
        clsHnd = info.compCompHnd->getMethodClass(hnd);
        CORINFO_SIG_INFO sig;
        eeGetMethodSig(hnd, &sig);
        eePrintMethod(&p, clsHnd, hnd, &sig,
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
    p.Append("<unknown method>");
    return p.GetBuffer();
}

//------------------------------------------------------------------------
// eeGetMethodName:
//   Get the name of a method.
//
// Arguments:
//    hnd                  - the method handle
//    buffer               - Preexisting buffer to use (can be nullptr to allocate on heap).
//    bufferSize           - Size of preexisting buffer.
//
// Remarks:
//   See eeGetMethodFullName for documentation about the buffer.
//
// Returns:
//   The string.
//
const char* Compiler::eeGetMethodName(CORINFO_METHOD_HANDLE methHnd, char* buffer, size_t bufferSize)
{
    StringPrinter p(getAllocator(CMK_DebugOnly), buffer, bufferSize);
    bool          success = eeRunFunctorWithSPMIErrorTrap([&]() {
        eePrintMethod(&p, NO_CLASS_HANDLE, methHnd,
                               /* sig */ nullptr,
                               /* includeClassInstantiation */ false,
                               /* includeMethodInstantiation */ false,
                               /* includeSignature */ false,
                               /* includeReturnType */ false,
                               /* includeThisSpecifier */ false);
    });

    if (!success)
    {
        p.Truncate(0);
        p.Append("<unknown method>");
    }

    return p.GetBuffer();
}

//------------------------------------------------------------------------
// eeGetFieldName:
//   Get a string describing a field.
//
// Arguments:
//    fldHnd               - the field handle
//    includeType          - Whether to prefix the string with <type name>:
//    buffer               - Preexisting buffer to use (can be nullptr to allocate on heap).
//    bufferSize           - Size of preexisting buffer.
//
// Remarks:
//   See eeGetMethodFullName for documentation about the buffer.
//
// Returns:
//   The string.
//
const char* Compiler::eeGetFieldName(CORINFO_FIELD_HANDLE fldHnd, bool includeType, char* buffer, size_t bufferSize)
{
    StringPrinter p(getAllocator(CMK_DebugOnly), buffer, bufferSize);
    bool          success = eeRunFunctorWithSPMIErrorTrap([&]() {
        eePrintField(&p, fldHnd, includeType);
    });

    if (success)
    {
        return p.GetBuffer();
    }

    p.Truncate(0);

    if (includeType)
    {
        p.Append("<unknown class>:");

        success = eeRunFunctorWithSPMIErrorTrap([&]() {
            eePrintField(&p, fldHnd, false);
        });

        if (success)
        {
            return p.GetBuffer();
        }

        p.Truncate(0);
    }

    if (includeType)
    {
        p.Append("<unknown class>:");
    }

    p.Append("<unknown field>");
    return p.GetBuffer();
}

//------------------------------------------------------------------------
// eeGetClassName:
//   Get the name (including namespace and instantiation) of a type.
//   If missing information (in SPMI), then return a placeholder string.
//
// Parameters:
//   clsHnd - the handle of the class
//   buffer - a buffer to use for scratch space, or null pointer to allocate a new string.
//   bufferSize - the size of buffer. If the final class name is longer a new string will be allocated.
//
// Return value:
//   The name string.
//
const char* Compiler::eeGetClassName(CORINFO_CLASS_HANDLE clsHnd, char* buffer, size_t bufferSize)
{
    StringPrinter printer(getAllocator(CMK_DebugOnly), buffer, bufferSize);
    if (!eeRunFunctorWithSPMIErrorTrap([&]() {
        eePrintType(&printer, clsHnd, true);
    }))
    {
        printer.Truncate(0);
        printer.Append("<unknown class>");
    }

    return printer.GetBuffer();
}

//------------------------------------------------------------------------
// eeGetShortClassName: Returns class name with no instantiation.
//
// Arguments:
//   clsHnd - the class handle to get the type name of
//
// Return value:
//   String without instantiation.
//
const char* Compiler::eeGetShortClassName(CORINFO_CLASS_HANDLE clsHnd)
{
    StringPrinter printer(getAllocator(CMK_DebugOnly));
    if (!eeRunFunctorWithSPMIErrorTrap([&]() {
        eePrintType(&printer, clsHnd, false);
    }))
    {
        printer.Truncate(0);
        printer.Append("<unknown class>");
    }

    return printer.GetBuffer();
}

void Compiler::eePrintObjectDescription(const char* prefix, CORINFO_OBJECT_HANDLE handle)
{
    const size_t maxStrSize = 64;
    char         str[maxStrSize];
    size_t       actualLen = 0;

    // Ignore potential SPMI failures
    bool success = eeRunFunctorWithSPMIErrorTrap([&]() {
        actualLen = this->info.compCompHnd->printObjectDescription(handle, str, maxStrSize);
    });

    if (!success)
    {
        return;
    }

    for (size_t i = 0; i < actualLen; i++)
    {
        // Replace \n and \r symbols with whitespaces
        if (str[i] == '\n' || str[i] == '\r')
        {
            str[i] = ' ';
        }
    }

    printf("%s '%s'", prefix, str);
}
