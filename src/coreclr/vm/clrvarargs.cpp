// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CLRVarArgs.cpp
//

//
// Variant-specific marshalling.


#include "common.h"
#include "clrvarargs.h"

DWORD VARARGS::CalcVaListSize(VARARGS *data)
{
    LIMITED_METHOD_CONTRACT;

    // Calculate how much space we need for the marshaled stack.
    // This assumes that the vararg managed and unmanaged calling conventions are similar-enough,
    // so we can simply use the size stored in the VASigCookie. This actually overestimates
    // the value since it counts the fixed args as well as the varargs. But that's harmless.

    DWORD dwVaListSize = data->ArgCookie->sizeOfArgs;
#ifndef TARGET_X86
    dwVaListSize += ARGUMENTREGISTERS_SIZE;
#endif
    return dwVaListSize;
}

void VARARGS::MarshalToManagedVaList(va_list va, VARARGS *dataout)
{
    WRAPPER_NO_CONTRACT

#ifndef TARGET_UNIX
    _ASSERTE(dataout != NULL);
    dataout->SigPtr = SigPointer(NULL, 0);
    dataout->ArgCookie = NULL;
    dataout->ArgPtr = (BYTE*)va;
#else
    PORTABILITY_ASSERT("Implement for Unix");
#endif
}

////////////////////////////////////////////////////////////////////////////////
// Marshal a ArgIterator to a pre-allocated va_list
////////////////////////////////////////////////////////////////////////////////
void
VARARGS::MarshalToUnmanagedVaList(
    va_list va, DWORD cbVaListSize, const VARARGS * data)
{
#ifndef TARGET_UNIX
    BYTE * pdstbuffer = (BYTE *)va;

    int    remainingArgs = data->RemainingArgs;
    BYTE * psrc = (BYTE *)(data->ArgPtr);
    BYTE * pdst = pdstbuffer;

    SigPointer sp = data->SigPtr;
    SigTypeContext typeContext; // This is an empty type context.  This is OK because the vararg methods may not be generic
    while (remainingArgs--)
    {
        CorElementType elemType = sp.PeekElemTypeClosed(data->ArgCookie->pModule, &typeContext);
        switch (elemType)
        {
            case ELEMENT_TYPE_I1:
            case ELEMENT_TYPE_U1:
            case ELEMENT_TYPE_I2:
            case ELEMENT_TYPE_U2:
            case ELEMENT_TYPE_I4:
            case ELEMENT_TYPE_U4:
            case ELEMENT_TYPE_I8:
            case ELEMENT_TYPE_U8:
            case ELEMENT_TYPE_R4:
            case ELEMENT_TYPE_R8:
            case ELEMENT_TYPE_I:
            case ELEMENT_TYPE_U:
            case ELEMENT_TYPE_PTR:
                {
                    const bool isValueType = false;
                    const bool isFloatHfa = false;
                    DWORD cbSize = StackElemSize(CorTypeInfo::Size(elemType), isValueType, isFloatHfa);

                    #ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
                    if (cbSize > ENREGISTERED_PARAMTYPE_MAXSIZE)
                        cbSize = sizeof(void*);
                    #endif

#ifdef TARGET_ARM
                    if (cbSize == 8)
                    {
                        // 64-bit primitives come from and must be copied to 64-bit aligned locations.
                        psrc = (BYTE*)ALIGN_UP(psrc, 8);
                        pdst = (BYTE*)ALIGN_UP(pdst, 8);
                    }
#endif // TARGET_ARM

                    #ifdef STACK_GROWS_DOWN_ON_ARGS_WALK
                    psrc -= cbSize;
                    #endif // STACK_GROWS_DOWN_ON_ARGS_WALK

                    if (pdst + cbSize > pdstbuffer + cbVaListSize)
                        COMPlusThrow(kArgumentException);

                    CopyMemory(pdst, psrc, cbSize);

                    #ifdef STACK_GROWS_UP_ON_ARGS_WALK
                    psrc += cbSize;
                    #endif // STACK_GROWS_UP_ON_ARGS_WALK

                    pdst += cbSize;
                    IfFailThrow(sp.SkipExactlyOne());
                }
                break;

            default:
                // non-IJW data type - we don't support marshaling these inside a va_list.
                COMPlusThrow(kNotSupportedException);
        }
    }
#else
    PORTABILITY_ASSERT("Implement for Unix");
#endif
} // VARARGS::MarshalToUnmanagedVaList
