// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize("tgy", on)
#endif

/*===============================IsFastSort===============================
**Action: Call the helper to walk the string and see if we have any high chars.
**Returns: void.  The appropriate bits are set on the String.
**Arguments: vThisRef - The string to be checked.
**Exceptions: None.
==============================================================================*/
FCIMPL1(FC_BOOL_RET, COMString::IsFastSort, StringObject* thisRef) {
    FCALL_CONTRACT;

    VALIDATEOBJECT(thisRef);
    _ASSERTE(thisRef!=NULL);
    DWORD state = thisRef->GetHighCharState();
    if (IS_STRING_STATE_UNDETERMINED(state)) {
        state = (STRINGREF(thisRef))->InternalCheckHighChars();
        FC_GC_POLL_RET();
    }
    else {
        FC_GC_POLL_NOT_NEEDED();
    }
    FC_RETURN_BOOL(IS_FAST_SORT(state)); //This can indicate either high chars or special sorting chars.
}
FCIMPLEND


/*==================================GETCHARAT===================================
**Returns the character at position index.  Thows IndexOutOfRangeException as
**appropriate.
**This method is not actually used. JIT will generate code for indexer method on string class.
**
==============================================================================*/
FCIMPL2(FC_CHAR_RET, COMString::GetCharAt, StringObject* str, INT32 index) {
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();
    VALIDATEOBJECT(str);
    if (str == NULL) {
        FCThrow(kNullReferenceException);
    }
    _ASSERTE(str->GetMethodTable() == g_pStringClass);

    if (index >=0 && index < (INT32)str->GetStringLength()) {
        //Return the appropriate character.
        return str->GetBuffer()[index];
    }

    FCThrow(kIndexOutOfRangeException);
}
FCIMPLEND


/*==================================LENGTH=================================== */

FCIMPL1(INT32, COMString::Length, StringObject* str) {
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();
    if (str == NULL)
        FCThrow(kNullReferenceException);

    FCUnique(0x11);
    return str->GetStringLength();
}
FCIMPLEND


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
#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize ("", on)
#endif
