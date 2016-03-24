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


#define PROBABILISTICMAP_BLOCK_INDEX_MASK    0X7
#define PROBABILISTICMAP_BLOCK_INDEX_SHIFT   0x3
#define PROBABILISTICMAP_SIZE                0X8

//
//
// FORWARD DECLARATIONS
//
//
int ArrayContains(WCHAR searchChar, __in_ecount(length) const WCHAR *begin, int length);
void InitializeProbabilisticMap(int* charMap, __in_ecount(length) const WCHAR* charArray, int length);
bool ProbablyContains(const int* charMap, WCHAR searchChar);
bool IsBitSet(int value, int bitPos);
void SetBit(int* value, int bitPos);

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

#ifdef FEATURE_RANDOMIZED_STRING_HASHING

inline COMNlsHashProvider * GetCurrentNlsHashProvider()
{
    LIMITED_METHOD_CONTRACT;
#ifndef FEATURE_CORECLR
    AppDomain* curDomain = GetAppDomain();
    return curDomain->m_pNlsHashProvider;
#else
    return &COMNlsHashProvider::s_NlsHashProvider;
#endif // FEATURE_CORECLR
}

FCIMPL3(INT32, COMString::Marvin32HashString, StringObject* thisRefUNSAFE, INT32 strLen, INT64 additionalEntropy) {
    FCALL_CONTRACT;

    int iReturnHash = 0;

    if (thisRefUNSAFE == NULL) {
        FCThrow(kNullReferenceException);
    }

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), FCThrow(kStackOverflowException));
    iReturnHash = GetCurrentNlsHashProvider()->HashString(thisRefUNSAFE->GetBuffer(), thisRefUNSAFE->GetStringLength(), TRUE, additionalEntropy);
    END_SO_INTOLERANT_CODE;

    FC_GC_POLL_RET();

    return iReturnHash;
}
FCIMPLEND

BOOL QCALLTYPE COMString::UseRandomizedHashing() {
    QCALL_CONTRACT;

    BOOL bUseRandomizedHashing = FALSE;

    BEGIN_QCALL;

    bUseRandomizedHashing = GetCurrentNlsHashProvider()->GetUseRandomHashing();

    END_QCALL;

    return bUseRandomizedHashing;
}
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

FCIMPL5(INT32, COMString::CompareOrdinalEx, StringObject* strA, INT32 indexA, StringObject* strB, INT32 indexB, INT32 count)
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(strA);
    VALIDATEOBJECT(strB);
    DWORD *strAChars, *strBChars;
    int strALength, strBLength;

    // This runtime test is handled in the managed wrapper.
    _ASSERTE(strA != NULL && strB != NULL);

    //If any of our indices are negative throw an exception.
    if (count<0)
    {
        FCThrowArgumentOutOfRange(W("count"), W("ArgumentOutOfRange_NegativeCount"));
    }
    if (indexA < 0)
    {
        FCThrowArgumentOutOfRange(W("indexA"), W("ArgumentOutOfRange_Index"));
    }
    if (indexB < 0)
    {
        FCThrowArgumentOutOfRange(W("indexB"), W("ArgumentOutOfRange_Index"));
    }

    strA->RefInterpretGetStringValuesDangerousForGC((WCHAR **) &strAChars, &strALength);
    strB->RefInterpretGetStringValuesDangerousForGC((WCHAR **) &strBChars, &strBLength);

    int countA = count;
    int countB = count;

    //Do a lot of range checking to make sure that everything is kosher and legit.
    if (count  > (strALength - indexA)) {
        countA = strALength - indexA;
        if (countA < 0)
            FCThrowArgumentOutOfRange(W("indexA"), W("ArgumentOutOfRange_Index"));
    }

    if (count > (strBLength - indexB)) {
        countB = strBLength - indexB;
        if (countB < 0)
            FCThrowArgumentOutOfRange(W("indexB"), W("ArgumentOutOfRange_Index"));
    }

    // Set up the loop variables.
    strAChars = (DWORD *) ((WCHAR *) strAChars + indexA);
    strBChars = (DWORD *) ((WCHAR *) strBChars + indexB);

    INT32 result = StringObject::FastCompareStringHelper(strAChars, countA, strBChars, countB);

    FC_GC_POLL_RET();
    return result;

}
FCIMPLEND

/*=================================IndexOfChar==================================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/

FCIMPL4 (INT32, COMString::IndexOfChar, StringObject* thisRef, CLR_CHAR value, INT32 startIndex, INT32 count )
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(thisRef);
    if (thisRef==NULL)
        FCThrow(kNullReferenceException);

    WCHAR *thisChars;
    int thisLength;

    thisRef->RefInterpretGetStringValuesDangerousForGC(&thisChars, &thisLength);

    if (startIndex < 0 || startIndex > thisLength) {
        FCThrowArgumentOutOfRange(W("startIndex"), W("ArgumentOutOfRange_Index"));
    }

    if (count   < 0 || count > thisLength - startIndex) {
        FCThrowArgumentOutOfRange(W("count"), W("ArgumentOutOfRange_Count"));
    }

    int endIndex = startIndex + count;
    for (int i=startIndex; i<endIndex; i++)
    {
        if (thisChars[i]==((WCHAR)value))
        {
            FC_GC_POLL_RET();
            return i;
        }
    }

    FC_GC_POLL_RET();
    return -1;
}
FCIMPLEND

/*===============================IndexOfCharArray===============================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
FCIMPL4(INT32, COMString::IndexOfCharArray, StringObject* thisRef, CHARArray* valueRef, INT32 startIndex, INT32 count )
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(thisRef);
    VALIDATEOBJECT(valueRef);

    if (thisRef == NULL)
        FCThrow(kNullReferenceException);
    if (valueRef == NULL)
        FCThrowArgumentNull(W("anyOf"));

    WCHAR *thisChars;
    WCHAR *valueChars;
    int valueLength;
    int thisLength;

    thisRef->RefInterpretGetStringValuesDangerousForGC(&thisChars, &thisLength);

    if (startIndex < 0 || startIndex > thisLength) {
        FCThrowArgumentOutOfRange(W("startIndex"), W("ArgumentOutOfRange_Index"));
    }

    if (count < 0 || count > thisLength - startIndex) {
        FCThrowArgumentOutOfRange(W("count"), W("ArgumentOutOfRange_Count"));
    }

    int endIndex = startIndex + count;

    valueLength = valueRef->GetNumComponents();
    valueChars = (WCHAR *)valueRef->GetDataPtr();

    // use probabilistic map, see (code:InitializeProbabilisticMap)
    int charMap[PROBABILISTICMAP_SIZE] = {0};

    InitializeProbabilisticMap(charMap, valueChars, valueLength);

    for(int i = startIndex; i < endIndex; i++) {
        WCHAR thisChar = thisChars[i];
        if (ProbablyContains(charMap, thisChar))
            if (ArrayContains(thisChars[i], valueChars, valueLength) >= 0) {
                FC_GC_POLL_RET();
                return i;
            }
    }

    FC_GC_POLL_RET();
    return -1;
}
FCIMPLEND


/*===============================LastIndexOfChar================================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/

FCIMPL4(INT32, COMString::LastIndexOfChar, StringObject* thisRef, CLR_CHAR value, INT32 startIndex, INT32 count )
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(thisRef);
    WCHAR *thisChars;
    int thisLength;

    if (thisRef==NULL) {
        FCThrow(kNullReferenceException);
    }

    thisRef->RefInterpretGetStringValuesDangerousForGC(&thisChars, &thisLength);

    if (thisLength == 0) {
        FC_GC_POLL_RET();
        return -1;
    }


    if (startIndex<0 || startIndex>=thisLength) {
        FCThrowArgumentOutOfRange(W("startIndex"), W("ArgumentOutOfRange_Index"));
    }

    if (count<0 || count - 1 > startIndex) {
        FCThrowArgumentOutOfRange(W("count"), W("ArgumentOutOfRange_Count"));
    }

    int endIndex = startIndex - count + 1;

    //We search [startIndex..EndIndex]
    for (int i=startIndex; i>=endIndex; i--) {
        if (thisChars[i]==((WCHAR)value)) {
            FC_GC_POLL_RET();
            return i;
        }
    }

    FC_GC_POLL_RET();
    return -1;
}
FCIMPLEND
/*=============================LastIndexOfCharArray=============================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/

FCIMPL4(INT32, COMString::LastIndexOfCharArray, StringObject* thisRef, CHARArray* valueRef, INT32 startIndex, INT32 count )
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(thisRef);
    VALIDATEOBJECT(valueRef);
    WCHAR *thisChars, *valueChars;
    int thisLength, valueLength;

    if (thisRef==NULL) {
        FCThrow(kNullReferenceException);
    }

    if (valueRef == NULL)
        FCThrowArgumentNull(W("anyOf"));

    thisRef->RefInterpretGetStringValuesDangerousForGC(&thisChars, &thisLength);

    if (thisLength == 0) {
        return -1;
    }

    if (startIndex < 0 || startIndex >= thisLength) {
        FCThrowArgumentOutOfRange(W("startIndex"), W("ArgumentOutOfRange_Index"));
    }

    if (count<0 || count - 1 > startIndex) {
        FCThrowArgumentOutOfRange(W("count"), W("ArgumentOutOfRange_Count"));
    }


    valueLength = valueRef->GetNumComponents();
    valueChars = (WCHAR *)valueRef->GetDataPtr();

    int endIndex = startIndex - count + 1;

    // use probabilistic map, see (code:InitializeProbabilisticMap)
    int charMap[PROBABILISTICMAP_SIZE] = {0};

    InitializeProbabilisticMap(charMap, valueChars, valueLength);

    //We search [startIndex..EndIndex]
    for (int i=startIndex; i>=endIndex; i--) {
        WCHAR thisChar = thisChars[i];
        if (ProbablyContains(charMap, thisChar))
            if (ArrayContains(thisChars[i],valueChars, valueLength) >= 0) {
                FC_GC_POLL_RET();
                return i;
            }
    }

    FC_GC_POLL_RET();
    return -1;

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


/*==================================PadHelper===================================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
FCIMPL4(Object*, COMString::PadHelper, StringObject* thisRefUNSAFE, INT32 totalWidth, CLR_CHAR paddingChar, CLR_BOOL isRightPadded)
{
    FCALL_CONTRACT;

    STRINGREF refRetVal = NULL;
    STRINGREF thisRef = (STRINGREF) thisRefUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_1(thisRef);

    WCHAR *thisChars, *padChars;
    INT32 thisLength;


    if (thisRef==NULL) {
        COMPlusThrow(kNullReferenceException, W("NullReference_This"));
    }

    thisRef->RefInterpretGetStringValuesDangerousForGC(&thisChars, &thisLength);

    //Don't let them pass in a negative totalWidth
    if (totalWidth<0) {
        COMPlusThrowArgumentOutOfRange(W("totalWidth"), W("ArgumentOutOfRange_NeedNonNegNum"));
    }

    //If the string is longer than the length which they requested, give them
    //back the old string.
    if (totalWidth<thisLength) {
        refRetVal = thisRef;
        goto lExit;
    }

    refRetVal = StringObject::NewString(totalWidth);

    // Reget thisChars, since if NewString triggers GC, thisChars may become trash.
    thisRef->RefInterpretGetStringValuesDangerousForGC(&thisChars, &thisLength);
    padChars = refRetVal->GetBuffer();

    if (isRightPadded) {

        memcpyNoGCRefs(padChars, thisChars, thisLength * sizeof(WCHAR));

        for (int i=thisLength; i<totalWidth; i++) {
            padChars[i] = paddingChar;
        }
    } else {
        INT32 startingPos = totalWidth-thisLength;
        memcpyNoGCRefs(padChars+startingPos, thisChars, thisLength * sizeof(WCHAR));

        for (int i=0; i<startingPos; i++) {
            padChars[i] = paddingChar;
        }
    }
    _ASSERTE(padChars[totalWidth] == 0);

lExit: ;
    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(refRetVal);
}
FCIMPLEND

// HELPER METHODS
// 
// 
// A probabilistic map is an optimization that is used in IndexOfAny/
// LastIndexOfAny methods. The idea is to create a bit map of the characters we
// are searching for and use this map as a "cheap" check to decide if the
// current character in the string exists in the array of input characters.
// There are 256 bits in the map, with each character mapped to 2 bits. Every
// character is divided into 2 bytes, and then every byte is mapped to 1 bit.
// The character map is an array of 8 integers acting as map blocks. The 3 lsb
// in each byte in the character is used to index into this map to get the
// right block, the value of the remaining 5 msb are used as the bit position
// inside this block. 
void InitializeProbabilisticMap(int* charMap, __in_ecount(length) const WCHAR* charArray, int length) {
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(charMap != NULL);
    _ASSERTE(charArray != NULL);
    _ASSERTE(length >= 0);

    for(int i = 0; i < length; ++i) {
        int hi,lo;

        WCHAR c = charArray[i];

        hi = (c >> 8) & 0xFF;
        lo = c & 0xFF;

        int* value = &charMap[lo & PROBABILISTICMAP_BLOCK_INDEX_MASK];
        SetBit(value, lo >> PROBABILISTICMAP_BLOCK_INDEX_SHIFT);

        value = &charMap[hi & PROBABILISTICMAP_BLOCK_INDEX_MASK];
        SetBit(value, hi >> PROBABILISTICMAP_BLOCK_INDEX_SHIFT);
    }
}

// Use the probabilistic map to decide if the character value exists in the
// map. When this method return false, we are certain the character doesn't
// exist, however a true return means it *may* exist.
inline bool ProbablyContains(const int* charMap, WCHAR searchValue) {
    LIMITED_METHOD_CONTRACT;

    int lo, hi;

    lo = searchValue & 0xFF;
    int value = charMap[lo & PROBABILISTICMAP_BLOCK_INDEX_MASK];

    if (IsBitSet(value, lo >> PROBABILISTICMAP_BLOCK_INDEX_SHIFT)) {
        hi = (searchValue >> 8) & 0xFF;
        value = charMap[hi & PROBABILISTICMAP_BLOCK_INDEX_MASK];

        return IsBitSet(value, hi >> PROBABILISTICMAP_BLOCK_INDEX_SHIFT);
    }

    return false;
}

inline void SetBit(int* value, int bitPos) {
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(bitPos <= 31);

    *value |= (1 << bitPos);
}

inline bool IsBitSet(int value, int bitPos) {
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(bitPos <= 31);

    return (value & (1 << bitPos)) != 0;
}


/*================================ArrayContains=================================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
int ArrayContains(WCHAR searchChar, __in_ecount(length) const WCHAR *begin, int length) {
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(begin != NULL);
    _ASSERTE(length >= 0);

    for(int i = 0; i < length; i++) {
        if(begin[i] == searchChar) {
            return i;
        }
    }
    return -1;
}


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
