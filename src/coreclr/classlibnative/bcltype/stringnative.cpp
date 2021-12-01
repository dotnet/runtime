// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: StringNative.cpp
//

//
// Purpose: The implementation of the String class.
//

//

#include "common.h"

#include "object.h"
#include "utilcode.h"
#include "excep.h"
#include "frames.h"
#include "field.h"
#include "vars.hpp"
#include "stringnative.h"
#include "comutilnative.h"
#include "metasig.h"
#include "excep.h"

// Compile the string functionality with these pragma flags (equivalent of the command line /Ox flag)
// Compiling this functionality differently gives us significant throughout gain in some cases.
#if defined(_MSC_VER) && defined(TARGET_X86)
#pragma optimize("tgy", on)
#endif

FCIMPL2(FC_BOOL_RET, COMString::FCTryGetTrailByte, StringObject* thisRefUNSAFE, UINT8 *pbData)
{
    FCALL_CONTRACT;

    STRINGREF thisRef = ObjectToSTRINGREF(thisRefUNSAFE);
    FC_RETURN_BOOL(thisRef->GetTrailByte(pbData));
}
FCIMPLEND

FCIMPL2(VOID, COMString::FCSetTrailByte, StringObject* thisRefUNSAFE, UINT8 bData)
{
    FCALL_CONTRACT;

    STRINGREF thisRef = ObjectToSTRINGREF(thisRefUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(thisRef);

    thisRef->SetTrailByte(bData);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

// Revert to command line compilation flags
#if defined(_MSC_VER) && defined(TARGET_X86)
#pragma optimize ("", on)
#endif
