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
#include "stringbuffer.h"
#include "comutilnative.h"
#include "metasig.h"
#include "excep.h"

// Compile the string functionality with these pragma flags (equivalent of the command line /Ox flag)
// Compiling this functionality differently gives us significant throughout gain in some cases.
#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize("tgy", on)
#endif

//
//
//  CONSTRUCTORS
//
//

/*===========================StringInitSBytPtrPartialEx===========================
**Action:  Takes a byte *, startIndex, length, and encoding and turns this into a string.
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/

FCIMPL5(Object *, COMString::StringInitSBytPtrPartialEx, StringObject *thisString,
        I1 *ptr, INT32 startIndex, INT32 length, Object *encoding)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(thisString == 0);
        PRECONDITION(ptr != NULL);
    } CONTRACTL_END;

    STRINGREF pString = NULL;
    VALIDATEOBJECT(encoding);

    HELPER_METHOD_FRAME_BEGIN_RET_1(encoding);
    MethodDescCallSite createString(METHOD__STRING__CREATE_STRING);

    ARG_SLOT args[] = {
        PtrToArgSlot(ptr),
        startIndex,
        length,
        ObjToArgSlot(ObjectToOBJECTREF(encoding)),
    };

    pString = createString.Call_RetSTRINGREF(args);
    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(pString);
}
FCIMPLEND

/*==============================StringInitCharPtr===============================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
FCIMPL2(Object *, COMString::StringInitCharPtr, StringObject *stringThis, INT8 *ptr)
{
    FCALL_CONTRACT;

    _ASSERTE(stringThis == 0);      // This is the constructor
    Object *result = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_0();
    result = OBJECTREFToObject(StringObject::StringInitCharHelper((LPCSTR)ptr, -1));
    HELPER_METHOD_FRAME_END();
    return result;
}
FCIMPLEND

/*===========================StringInitCharPtrPartial===========================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
FCIMPL4(Object *, COMString::StringInitCharPtrPartial, StringObject *stringThis, INT8 *value,
        INT32 startIndex, INT32 length)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(stringThis ==0);
    } CONTRACTL_END;

    STRINGREF pString = NULL;

    //Verify the args.
    if (startIndex<0) {
        FCThrowArgumentOutOfRange(W("startIndex"), W("ArgumentOutOfRange_StartIndex"));
    }

    if (length<0) {
        FCThrowArgumentOutOfRange(W("length"), W("ArgumentOutOfRange_NegativeLength"));
    }

    // This is called directly now. There is no check in managed code.
    if( value == NULL) {
        FCThrowArgumentNull(W("value"));
    }

    LPCSTR pBase = (LPCSTR)value;
    LPCSTR pFrom = pBase + startIndex;
    if (pFrom < pBase) {
        // Check for overflow of pointer addition
        FCThrowArgumentOutOfRange(W("startIndex"), W("ArgumentOutOfRange_PartialWCHAR"));
    }

    HELPER_METHOD_FRAME_BEGIN_RET_0();
    
    pString = StringObject::StringInitCharHelper(pFrom, length);
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(pString);
}
FCIMPLEND

inline COMNlsHashProvider * GetCurrentNlsHashProvider()
{
    LIMITED_METHOD_CONTRACT;
    return &COMNlsHashProvider::s_NlsHashProvider;
}

FCIMPL1(INT32, COMString::Marvin32HashString, StringObject* thisRefUNSAFE) {
    FCALL_CONTRACT;

    int iReturnHash = 0;

    if (thisRefUNSAFE == NULL) {
        FCThrow(kNullReferenceException);
    }

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), FCThrow(kStackOverflowException));
    iReturnHash = GetCurrentNlsHashProvider()->HashString(thisRefUNSAFE->GetBuffer(), thisRefUNSAFE->GetStringLength());
    END_SO_INTOLERANT_CODE;

    FC_GC_POLL_RET();

    return iReturnHash;
}
FCIMPLEND

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

/*===============================IsAscii===============================
**Action: Call the helper to walk the string and see if we have any high chars.
**Returns: void.  The appropriate bits are set on the String.
**Arguments: vThisRef - The string to be checked.
**Exceptions: None.
==============================================================================*/
FCIMPL1(FC_BOOL_RET, COMString::IsAscii, StringObject* thisRef) {
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
    FC_RETURN_BOOL(IS_ASCII(state)); //This can indicate either high chars or special sorting chars.
}
FCIMPLEND



//This function relies on the fact that we put a terminating null on the end of
//all managed strings.
FCIMPL2(INT32, COMString::FCCompareOrdinalIgnoreCaseWC, StringObject* strA, __in_z INT8 *strBChars) {
    FCALL_CONTRACT;

    VALIDATEOBJECT(strA);
    WCHAR *strAChars;
    WCHAR *strAStart;
    INT32 aLength;
    INT32 ret;

    _ASSERT(strA != NULL && strBChars != NULL);

    //Get our data.
    strA->RefInterpretGetStringValuesDangerousForGC((WCHAR **) &strAChars, &aLength);

    //Record the start pointer for some comparisons at the end.
    strAStart = strAChars;

    if (!StringObject::CaseInsensitiveCompHelper(strAChars, strBChars, aLength, -1, &ret)) {
        //This will happen if we have characters greater than 0x7F. This indicates that the function failed.
        // We don't throw an exception here. You can look at the success value returned to do something meaningful.
        ret = 1;
    }

    FC_GC_POLL_RET();
    return ret;
}
FCIMPLEND

/*================================CompareOrdinalEx===============================
**Args: typedef struct {STRINGREF thisRef; INT32 options; INT32 length; INT32 valueOffset;\
        STRINGREF value; INT32 thisOffset;} _compareOrdinalArgsEx;
==============================================================================*/

FCIMPL6(INT32, COMString::CompareOrdinalEx, StringObject* strA, INT32 indexA, INT32 countA, StringObject* strB, INT32 indexB, INT32 countB)
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(strA);
    VALIDATEOBJECT(strB);
    DWORD *strAChars, *strBChars;
    int strALength, strBLength;

    // These runtime tests are handled in the managed wrapper.
    _ASSERTE(strA != NULL && strB != NULL);
    _ASSERTE(indexA >= 0 && indexB >= 0);
    _ASSERTE(countA >= 0 && countB >= 0);

    strA->RefInterpretGetStringValuesDangerousForGC((WCHAR **) &strAChars, &strALength);
    strB->RefInterpretGetStringValuesDangerousForGC((WCHAR **) &strBChars, &strBLength);

    _ASSERTE(countA <= strALength - indexA);
    _ASSERTE(countB <= strBLength - indexB);

    // Set up the loop variables.
    strAChars = (DWORD *) ((WCHAR *) strAChars + indexA);
    strBChars = (DWORD *) ((WCHAR *) strBChars + indexB);

    INT32 result = StringObject::FastCompareStringHelper(strAChars, countA, strBChars, countB);

    FC_GC_POLL_RET();
    return result;

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


/*================================ReplaceString=================================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
FCIMPL3(Object*, COMString::ReplaceString, StringObject* thisRefUNSAFE, StringObject* oldValueUNSAFE, StringObject* newValueUNSAFE)
{
    FCALL_CONTRACT;

    struct _gc
    {
        STRINGREF     thisRef;
        STRINGREF     oldValue;
        STRINGREF     newValue;
        STRINGREF     retValString;
    } gc;

    gc.thisRef        = ObjectToSTRINGREF(thisRefUNSAFE);
    gc.oldValue       = ObjectToSTRINGREF(oldValueUNSAFE);
    gc.newValue       = ObjectToSTRINGREF(newValueUNSAFE);
    gc.retValString   = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    int *replaceIndex;
    int index=0;
    int replaceCount=0;
    int readPos, writePos;
    WCHAR *thisBuffer, *oldBuffer, *newBuffer, *retValBuffer;
    int thisLength, oldLength, newLength;
    int endIndex;
    CQuickBytes replaceIndices;


    if (gc.thisRef==NULL) {
        COMPlusThrow(kNullReferenceException, W("NullReference_This"));
    }

    //Verify all of the arguments.
    if (!gc.oldValue) {
        COMPlusThrowArgumentNull(W("oldValue"), W("ArgumentNull_Generic"));
    }

    //If they asked to replace oldValue with a null, replace all occurances
    //with the empty string.
    if (!gc.newValue) {
        gc.newValue = StringObject::GetEmptyString();
    }

    gc.thisRef->RefInterpretGetStringValuesDangerousForGC(&thisBuffer, &thisLength);
    gc.oldValue->RefInterpretGetStringValuesDangerousForGC(&oldBuffer,  &oldLength);
    gc.newValue->RefInterpretGetStringValuesDangerousForGC(&newBuffer,  &newLength);

    //Record the endIndex so that we don't need to do this calculation all over the place.
    endIndex = thisLength;

    //If our old Length is 0, we won't know what to replace
    if (oldLength==0) {
        COMPlusThrowArgumentException(W("oldValue"), W("Argument_StringZeroLength"));
    }

    //replaceIndex is made large enough to hold the maximum number of replacements possible:
    //The case where every character in the current buffer gets replaced.
    replaceIndex = (int *)replaceIndices.AllocThrows((thisLength/oldLength+1)*sizeof(int));
    index=0;

    _ASSERTE(endIndex - oldLength <= endIndex);
    //Prefix: oldLength validated in mscorlib.dll!String.Replace
    PREFIX_ASSUME(endIndex - oldLength <= endIndex);

    while (((index=StringBufferObject::LocalIndexOfString(thisBuffer,oldBuffer,thisLength,oldLength,index))>-1) && (index<=endIndex-oldLength))
    {
        replaceIndex[replaceCount++] = index;
        index+=oldLength;
    }

    if (replaceCount != 0)
    {
        //Calculate the new length of the string and ensure that we have sufficent room.
        INT64 retValBuffLength = thisLength - ((oldLength - newLength) * (INT64)replaceCount);
        _ASSERTE(retValBuffLength >= 0);
        if (retValBuffLength > 0x7FFFFFFF)
            COMPlusThrowOM();

        gc.retValString = StringObject::NewString((INT32)retValBuffLength);
        retValBuffer = gc.retValString->GetBuffer();

        //Get the update buffers for all the Strings since the allocation could have triggered a GC.
        thisBuffer  = gc.thisRef->GetBuffer();
        newBuffer   = gc.newValue->GetBuffer();
        oldBuffer   = gc.oldValue->GetBuffer();


        //Set replaceHolder to be the upper limit of our array.
        int replaceHolder = replaceCount;
        replaceCount=0;

        //Walk the array forwards copying each character as we go.  If we reach an instance
        //of the string being replaced, replace the old string with the new string.
        readPos = 0;
        writePos = 0;
        while (readPos<thisLength)
        {
            if (replaceCount<replaceHolder&&readPos==replaceIndex[replaceCount])
            {
                replaceCount++;
                readPos+=(oldLength);
                memcpyNoGCRefs(&retValBuffer[writePos], newBuffer, newLength*sizeof(WCHAR));
                writePos+=(newLength);
            }
            else
            {
                retValBuffer[writePos++] = thisBuffer[readPos++];
            }
        }
        retValBuffer[retValBuffLength]='\0';
    }
    else
    {
        gc.retValString = gc.thisRef;
    }

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.retValString);
}
FCIMPLEND



#ifdef FEATURE_COMINTEROP

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

#endif // FEATURE_COMINTEROP

// Revert to command line compilation flags
#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize ("", on)
#endif
