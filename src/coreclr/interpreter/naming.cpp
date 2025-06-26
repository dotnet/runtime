// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "interpreter.h"

void AppendType(COMP_HANDLE comp, TArray<char>* printer, CORINFO_CLASS_HANDLE clsHnd, bool includeInstantiation);
void AppendCorInfoType(TArray<char>* printer, CorInfoType corInfoType);
void AppendTypeOrJitAlias(COMP_HANDLE comp, TArray<char>* printer, CORINFO_CLASS_HANDLE clsHnd, bool includeInstantiation);

void AppendString(TArray<char>* array, const char* str)
{
    if (str != nullptr)
    {
        size_t strLen = strlen(str);
        array->Append(str, static_cast<int32_t>(strLen));
    }
}

void AppendCorInfoTypeWithModModifiers(TArray<char>* printer, CorInfoTypeWithMod corInfoTypeWithMod)
{
    if ((corInfoTypeWithMod & CORINFO_TYPE_MOD_PINNED) == CORINFO_TYPE_MOD_PINNED)
    {
        printer->Append("PINNED__", 7);
    }
    if ((corInfoTypeWithMod & CORINFO_TYPE_MOD_COPY_WITH_HELPER) == CORINFO_TYPE_MOD_COPY_WITH_HELPER)
    {
        printer->Append("COPY_WITH_HELPER__", 17);
    }
}

void AppendCorInfoType(TArray<char>* printer, CorInfoType corInfoType)
{
    static const char* preciseVarTypeMap[CORINFO_TYPE_COUNT] = {
        // see the definition of enum CorInfoType in file inc/corinfo.h
        "<UNDEF>",
        "void",
        "bool",
        "char",
        "sbyte",
        "byte",
        "short",
        "ushort",
        "int",
        "uint",
        "long",
        "ulong",
        "nint",
        "nuint",
        "float",
        "double",
        "string",
        "ptr",
        "byref",
        "struct",
        "class",
        "typedbyref",
        "var"
    };

    const char *corInfoTypeName = "CORINFO_TYPE_INVALID";
    if (corInfoType >= 0 && corInfoType < CORINFO_TYPE_COUNT)
    {
        corInfoTypeName = preciseVarTypeMap[corInfoType];
    }
    
    printer->Append(corInfoTypeName, static_cast<int32_t>(strlen(corInfoTypeName)));
}

void AppendTypeOrJitAlias(COMP_HANDLE comp, TArray<char>* printer, CORINFO_CLASS_HANDLE clsHnd, bool includeInstantiation)
{
    CorInfoType typ = comp->asCorInfoType(clsHnd);
    if ((typ == CORINFO_TYPE_CLASS) || (typ == CORINFO_TYPE_VALUECLASS))
    {
        AppendType(comp, printer, clsHnd, includeInstantiation);
    }
    else
    {
        AppendCorInfoType(printer, typ);
    }
}

void AppendType(COMP_HANDLE comp, TArray<char>* printer, CORINFO_CLASS_HANDLE clsHnd, bool includeInstantiation)
{
    unsigned arrayRank = comp->getArrayRank(clsHnd);
    if (arrayRank > 0)
    {
        CORINFO_CLASS_HANDLE childClsHnd;
        CorInfoType          childType = comp->getChildType(clsHnd, &childClsHnd);
        if ((childType == CORINFO_TYPE_CLASS) || (childType == CORINFO_TYPE_VALUECLASS))
        {
            AppendType(comp, printer, childClsHnd, includeInstantiation);
        }
        else
        {
            AppendCorInfoType(printer, childType);
        }

        printer->Add('[');
        for (unsigned i = 1; i < arrayRank; i++)
        {
            printer->Add(',');
        }
        printer->Add(']');
        return;
    }

    size_t bufferSizeNeeded = 0;
    comp->printClassName(clsHnd, NULL, 0, &bufferSizeNeeded);
    if (bufferSizeNeeded != 0)
    {
        int32_t oldBufferSize = printer->GetSize();
        printer->GrowBy(static_cast<int32_t>(bufferSizeNeeded));
        comp->printClassName(clsHnd, (printer->GetUnderlyingArray() + oldBufferSize), bufferSizeNeeded, &bufferSizeNeeded);
        printer->RemoveAt(printer->GetSize() - 1);
    }

    if (!includeInstantiation)
    {
        return;
    }

    char pref = '[';
    for (unsigned typeArgIndex = 0;; typeArgIndex++)
    {
        CORINFO_CLASS_HANDLE typeArg = comp->getTypeInstantiationArgument(clsHnd, typeArgIndex);

        if (typeArg == NO_CLASS_HANDLE)
        {
            break;
        }

        printer->Add(pref);
        pref = ',';
        AppendTypeOrJitAlias(comp, printer, typeArg, true);
    }

    if (pref != '[')
    {
        printer->Add(']');
    }
}

void AppendMethodName(COMP_HANDLE comp,
                            TArray<char>* printer,
                            CORINFO_CLASS_HANDLE  clsHnd,
                            CORINFO_METHOD_HANDLE methHnd,
                            CORINFO_SIG_INFO*     sig,
                            bool                  includeAssembly,
                            bool                  includeClass,
                            bool                  includeClassInstantiation,
                            bool                  includeMethodInstantiation,
                            bool                  includeSignature,
                            bool                  includeReturnType,
                            bool                  includeThisSpecifier)
{
    TArray<char> result;

    if (includeAssembly)
    {
        const char *pAssemblyName = comp->getClassAssemblyName(clsHnd);
        AppendString(printer, pAssemblyName);
        printer->Add('!');
    }

    if (includeClass)
    {
        AppendType(comp, printer, clsHnd, includeClassInstantiation);
        printer->Add(':');
    }

    size_t bufferSizeNeeded = 0;
    comp->printMethodName(methHnd, NULL, 0, &bufferSizeNeeded);
    if (bufferSizeNeeded != 0)
    {
        int32_t oldBufferSize = printer->GetSize();
        printer->GrowBy(static_cast<int32_t>(bufferSizeNeeded));
        comp->printMethodName(methHnd, (printer->GetUnderlyingArray() + oldBufferSize), bufferSizeNeeded, &bufferSizeNeeded);
        printer->RemoveAt(printer->GetSize() - 1); // Remove null terminator
    }

    if (includeMethodInstantiation && (sig->sigInst.methInstCount > 0))
    {
        printer->Add('[');
        for (unsigned i = 0; i < sig->sigInst.methInstCount; i++)
        {
            if (i > 0)
            {
                printer->Add(',');
            }

            AppendTypeOrJitAlias(comp, printer, sig->sigInst.methInst[i], true);
        }
        printer->Add(']');
    }

    if (includeSignature)
    {
        printer->Add('(');

        CORINFO_ARG_LIST_HANDLE argLst = sig->args;
        for (unsigned i = 0; i < sig->numArgs; i++)
        {
            if (i > 0)
                printer->Add(',');

            CORINFO_CLASS_HANDLE vcClsHnd;
            CorInfoTypeWithMod withMod =  comp->getArgType(sig, argLst, &vcClsHnd);
            AppendCorInfoTypeWithModModifiers(printer, withMod);
            CorInfoType type = strip(withMod);
            switch (type)
            {
                case CORINFO_TYPE_STRING:
                case CORINFO_TYPE_CLASS:
                case CORINFO_TYPE_VAR:
                case CORINFO_TYPE_VALUECLASS:
                case CORINFO_TYPE_REFANY:
                {
                    CORINFO_CLASS_HANDLE clsHnd = comp->getArgClass(sig, argLst);
                    // For some SIMD struct types we can get a nullptr back from eeGetArgClass on Linux/X64
                    if (clsHnd != NO_CLASS_HANDLE)
                    {
                        AppendType(comp, printer, clsHnd, true);
                        break;
                    }
                }

                    FALLTHROUGH;
                default:
                    AppendCorInfoType(printer, type);
                    break;
            }

            argLst = comp->getArgNext(argLst);
        }

        printer->Add(')');

        if (includeReturnType)
        {
            CorInfoType retType = sig->retType;
            if (retType != CORINFO_TYPE_VOID)
            {
                printer->Add(':');
                switch (retType)
                {
                    case CORINFO_TYPE_STRING:
                    case CORINFO_TYPE_CLASS:
                    case CORINFO_TYPE_VAR:
                    case CORINFO_TYPE_VALUECLASS:
                    case CORINFO_TYPE_REFANY:
                    {
                        CORINFO_CLASS_HANDLE clsHnd = sig->retTypeClass;
                        if (clsHnd != NO_CLASS_HANDLE)
                        {
                            AppendType(comp, printer, clsHnd, true);
                            break;
                        }
                    }
                        FALLTHROUGH;
                    default:
                        AppendCorInfoType(printer, retType);
                        break;
                }
            }
        }

        // Does it have a 'this' pointer? Don't count explicit this, which has
        // the this pointer type as the first element of the arg type list
        if (includeThisSpecifier && sig->hasThis() && !sig->hasExplicitThis())
        {
            printer->Append(":this", 5);
        }
    }
}

TArray<char> PrintMethodName(COMP_HANDLE comp,
                            CORINFO_CLASS_HANDLE  clsHnd,
                            CORINFO_METHOD_HANDLE methHnd,
                            CORINFO_SIG_INFO*     sig,
                            bool                  includeAssembly,
                            bool                  includeClass,
                            bool                  includeClassInstantiation,
                            bool                  includeMethodInstantiation,
                            bool                  includeSignature,
                            bool                  includeReturnType,
                            bool                  includeThisSpecifier)
{
    TArray<char> printer;
    AppendMethodName(comp, &printer, clsHnd, methHnd, sig,
                     includeAssembly, includeClass,
                     includeClassInstantiation, includeMethodInstantiation,
                     includeSignature, includeReturnType, includeThisSpecifier);
    printer.Add('\0'); // Ensure null-termination
    return printer;
}
