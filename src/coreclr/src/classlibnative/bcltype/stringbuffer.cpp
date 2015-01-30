//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// File: StringBuffer.cpp
//

//
// Purpose: The implementation of the StringBuffer class.
//
// 

#include "common.h"

#include "object.h"
#include "excep.h"
#include "frames.h"
#include "vars.hpp"
#include "string.h"
#include "stringbuffer.h"

FCIMPL3(void, COMStringBuffer::ReplaceBufferInternal, StringBufferObject* thisRefUNSAFE, __in_ecount(newLength) WCHAR* newBuffer, INT32 newLength)
{
    FCALL_CONTRACT;

    STRINGBUFFERREF thisRef = (STRINGBUFFERREF)ObjectToOBJECTREF(thisRefUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(thisRef);

    StringBufferObject::ReplaceBuffer(&thisRef, newBuffer, newLength);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL3(void, COMStringBuffer::ReplaceBufferAnsiInternal, StringBufferObject* thisRefUNSAFE, __in_ecount(newCapacity) CHAR* newBuffer, INT32 newCapacity)
{
    FCALL_CONTRACT;

    STRINGBUFFERREF thisRef = (STRINGBUFFERREF)ObjectToOBJECTREF(thisRefUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(thisRef);

    StringBufferObject::ReplaceBufferAnsi(&thisRef, newBuffer, newCapacity);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


