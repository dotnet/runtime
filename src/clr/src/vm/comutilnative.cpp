// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//

/*============================================================
**
** File:  COMUtilNative
**
**  
**
** Purpose: A dumping ground for classes which aren't large
** enough to get their own file in the EE.
**
**
**
===========================================================*/
#include "common.h"
#include "object.h"
#include "excep.h"
#include "vars.hpp"
#include "comutilnative.h"

#include "utilcode.h"
#include "frames.h"
#include "field.h"
#include "winwrap.h"
#include "gc.h"
#include "fcall.h"
#include "invokeutil.h"
#include "eeconfig.h"
#include "typestring.h"
#include "sha1.h"
#include "finalizerthread.h"

#ifdef FEATURE_COMINTEROP
    #include "comcallablewrapper.h"
    #include "comcache.h"
#endif // FEATURE_COMINTEROP

#define STACK_OVERFLOW_MESSAGE   W("StackOverflowException")

//These are defined in System.ParseNumbers and should be kept in sync.
#define PARSE_TREATASUNSIGNED 0x200
#define PARSE_TREATASI1 0x400
#define PARSE_TREATASI2 0x800
#define PARSE_ISTIGHT 0x1000
#define PARSE_NOSPACE 0x2000


//
//
// PARSENUMBERS (and helper functions)
//
//

/*===================================IsDigit====================================
**Returns a bool indicating whether the character passed in represents a   **
**digit.
==============================================================================*/
bool IsDigit(WCHAR c, int radix, int *result)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(result));
    }
    CONTRACTL_END;

    if (IS_DIGIT(c)) {
        *result = DIGIT_TO_INT(c);
    }
    else if (c>='A' && c<='Z') {
        //+10 is necessary because A is actually 10, etc.
        *result = c-'A'+10;
    }
    else if (c>='a' && c<='z') {
        //+10 is necessary because a is actually 10, etc.
        *result = c-'a'+10;
    }
    else {
        *result = -1;
    }

    if ((*result >=0) && (*result < radix))
        return true;

    return false;
}

INT32 wtoi(__in_ecount(length) WCHAR* wstr, DWORD length)
{  
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(wstr));
        PRECONDITION(length >= 0);
    }
    CONTRACTL_END;

    DWORD i = 0;
    int value;
    INT32 result = 0;

    while ( (i < length) && (IsDigit(wstr[i], 10 ,&value)) ) {
        //Read all of the digits and convert to a number
        result = result*10 + value;
        i++;
    }

    return result;
}

INT32 ParseNumbers::GrabInts(const INT32 radix, __in_ecount(length) WCHAR *buffer, const int length, int *i, BOOL isUnsigned)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckPointer(i));
        PRECONDITION(*i >= 0);
        PRECONDITION(length >= 0);
        PRECONDITION( radix==2 || radix==8 || radix==10 || radix==16 );
    }
    CONTRACTL_END;

    UINT32 result=0;
    int value;
    UINT32 maxVal;

    // Allow all non-decimal numbers to set the sign bit.
    if (radix==10 && !isUnsigned) {
        maxVal = (0x7FFFFFFF / 10);

        //Read all of the digits and convert to a number
        while (*i<length&&(IsDigit(buffer[*i],radix,&value))) {
            // Check for overflows - this is sufficient & correct.
            if (result > maxVal || ((INT32)result)<0)
                COMPlusThrow(kOverflowException, W("Overflow_Int32"));
            result = result*radix + value;
            (*i)++;
        }
        if ((INT32)result<0 && result!=0x80000000)
            COMPlusThrow(kOverflowException, W("Overflow_Int32"));

    }
    else {
        maxVal = ((UINT32) -1) / radix;

        //Read all of the digits and convert to a number
        while (*i<length&&(IsDigit(buffer[*i],radix,&value))) {
            // Check for overflows - this is sufficient & correct.
            if (result > maxVal)
                COMPlusThrow(kOverflowException, W("Overflow_UInt32"));
            // the above check won't cover 4294967296 to 4294967299
            UINT32 temp = result*radix + value;
            if( temp < result) { // this means overflow as well
                COMPlusThrow(kOverflowException, W("Overflow_UInt32"));
            }

            result = temp;
            (*i)++;
        }
    }
    return(INT32) result;
}

INT64 ParseNumbers::GrabLongs(const INT32 radix, __in_ecount(length) WCHAR *buffer, const int length, int *i, BOOL isUnsigned)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckPointer(i));
        PRECONDITION(*i >= 0);
        PRECONDITION(length >= 0);
    }
    CONTRACTL_END;

    UINT64 result=0;
    int value;
    UINT64 maxVal;

    // Allow all non-decimal numbers to set the sign bit.
    if (radix==10 && !isUnsigned) {
        maxVal = (UI64(0x7FFFFFFFFFFFFFFF) / 10);

        //Read all of the digits and convert to a number
        while (*i<length&&(IsDigit(buffer[*i],radix,&value))) {
            // Check for overflows - this is sufficient & correct.
            if (result > maxVal || ((INT64)result)<0)
                COMPlusThrow(kOverflowException, W("Overflow_Int64"));
            result = result*radix + value;
            (*i)++;
        }
        if ((INT64)result<0 && result!=UI64(0x8000000000000000))
            COMPlusThrow(kOverflowException, W("Overflow_Int64"));

    }
    else {
        maxVal = ((UINT64) -1L) / radix;

        //Read all of the digits and convert to a number
        while (*i<length&&(IsDigit(buffer[*i],radix,&value))) {
            // Check for overflows - this is sufficient & correct.
            if (result > maxVal)
                COMPlusThrow(kOverflowException, W("Overflow_UInt64"));

            UINT64 temp = result*radix + value;
            if( temp < result) { // this means overflow as well
                COMPlusThrow(kOverflowException, W("Overflow_UInt64"));
            }
            result = temp;

            (*i)++;
        }
    }
    return(INT64) result;
}

void EatWhiteSpace(__in_ecount(length) WCHAR *buffer, int length, int *i)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(buffer));
        PRECONDITION(CheckPointer(i));
        PRECONDITION(length >= 0);
    }
    CONTRACTL_END;

    for (; *i<length && COMCharacter::nativeIsWhiteSpace(buffer[*i]); (*i)++);
}

FCIMPL5_VII(LPVOID, ParseNumbers::LongToString, INT64 n, INT32 radix, INT32 width, CLR_CHAR paddingChar, INT32 flags)
{
    FCALL_CONTRACT;

    LPVOID rv = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    bool isNegative = false;
    int index=0;
    int charVal;
    UINT64 l;
    INT32 i;
    INT32 buffLength=0;
    WCHAR buffer[67];//Longest possible string length for an integer in binary notation with prefix

    if (radix<MinRadix || radix>MaxRadix)
        COMPlusThrowArgumentException(W("radix"), W("Arg_InvalidBase"));

    //If the number is negative, make it positive and remember the sign.
    if (n<0) {
        isNegative=true;

        // For base 10, write out -num, but other bases write out the
        // 2's complement bit pattern
        if (10==radix)
            l = (UINT64)(-n);
        else
            l = (UINT64)n;
    }
    else {
        l=(UINT64)n;
    }

    if (flags&PrintAsI1)
        l = l&0xFF;
    else if (flags&PrintAsI2)
        l = l&0xFFFF;
    else if (flags&PrintAsI4)
        l=l&0xFFFFFFFF;

    //Special case the 0.
    if (0==l) {
        buffer[0]='0';
        index=1;
    }
    else {
        //Pull apart the number and put the digits (in reverse order) into the buffer.
        for (index=0; l>0; l=l/radix, index++) {
            if ((charVal=(int)(l%radix))<10)
                buffer[index] = (WCHAR)(charVal + '0');
            else
                buffer[index] = (WCHAR)(charVal + 'a' - 10);
        }
    }

    //If they want the base, append that to the string (in reverse order)
    if (radix!=10 && ((flags&PrintBase)!=0)) {
        if (16==radix) {
            buffer[index++]='x';
            buffer[index++]='0';
        }
        else if (8==radix) {
            buffer[index++]='0';
        }
        else if ((flags&PrintRadixBase)!=0) {
            buffer[index++]='#';
            buffer[index++]=((radix%10)+'0');
            buffer[index++]=((static_cast<char>(radix)/10)+'0');
        }
    }

    if (10==radix) {
        //If it was negative, append the sign.
        if (isNegative) {
            buffer[index++]='-';
        }

        //else if they requested, add the '+';
        else if ((flags&PrintSign)!=0) {
            buffer[index++]='+';
        }

        //If they requested a leading space, put it on.
        else if ((flags&PrefixSpace)!=0) {
            buffer[index++]=' ';
        }
    }

    //Figure out the size of our string.
    if (width<=index)
        buffLength=index;
    else
        buffLength=width;

    STRINGREF Local = StringObject::NewString(buffLength);
    WCHAR *LocalBuffer = Local->GetBuffer();

    //Put the characters into the String in reverse order
    //Fill the remaining space -- if there is any --
    //with the correct padding character.
    if ((flags&LeftAlign)!=0) {
        for (i=0; i<index; i++) {
            LocalBuffer[i]=buffer[index-i-1];
        }
        for (;i<buffLength; i++) {
            LocalBuffer[i]=paddingChar;
        }
    }
    else {
        for (i=0; i<index; i++) {
            LocalBuffer[buffLength-i-1]=buffer[i];
        }
        for (int j=buffLength-i-1; j>=0; j--) {
            LocalBuffer[j]=paddingChar;
        }
    }

    *((STRINGREF *)&rv)=Local;

    HELPER_METHOD_FRAME_END();

    return rv;
}
FCIMPLEND


FCIMPL5(LPVOID, ParseNumbers::IntToString, INT32 n, INT32 radix, INT32 width, CLR_CHAR paddingChar, INT32 flags)
{
    FCALL_CONTRACT;

    LPVOID rv = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    bool isNegative = false;
    int index=0;
    int charVal;
    int buffLength;
    int i;
    UINT32 l;
    WCHAR buffer[66];  //Longest possible string length for an integer in binary notation with prefix

    if (radix<MinRadix || radix>MaxRadix)
        COMPlusThrowArgumentException(W("radix"), W("Arg_InvalidBase"));

    //If the number is negative, make it positive and remember the sign.
    //If the number is MIN_VALUE, this will still be negative, so we'll have to
    //special case this later.
    if (n<0) {
        isNegative=true;
        // For base 10, write out -num, but other bases write out the
        // 2's complement bit pattern
        if (10==radix)
            l = (UINT32)(-n);
        else
            l = (UINT32)n;
    }
    else {
        l=(UINT32)n;
    }

    //The conversion to a UINT will sign extend the number.  In order to ensure
    //that we only get as many bits as we expect, we chop the number.
    if (flags&PrintAsI1) {
        l = l&0xFF;
    }
    else if (flags&PrintAsI2) {
        l = l&0xFFFF;
    }
    else if (flags&PrintAsI4) {
        l=l&0xFFFFFFFF;
    }

    //Special case the 0.
    if (0==l) {
        buffer[0]='0';
        index=1;
    }
    else {
        do {
            charVal = l%radix;
            l=l/radix;
            if (charVal<10) {
                buffer[index++] = (WCHAR)(charVal + '0');
            }
            else {
                buffer[index++] = (WCHAR)(charVal + 'a' - 10);
            }
        }
        while (l!=0);
    }

    //If they want the base, append that to the string (in reverse order)
    if (radix!=10 && ((flags&PrintBase)!=0)) {
        if (16==radix) {
            buffer[index++]='x';
            buffer[index++]='0';
        }
        else if (8==radix) {
            buffer[index++]='0';
        }
    }

    if (10==radix) {
        //If it was negative, append the sign.
        if (isNegative) {
            buffer[index++]='-';
        }

        //else if they requested, add the '+';
        else if ((flags&PrintSign)!=0) {
            buffer[index++]='+';
        }

        //If they requested a leading space, put it on.
        else if ((flags&PrefixSpace)!=0) {
            buffer[index++]=' ';
        }
    }

    //Figure out the size of our string.
    if (width<=index) {
        buffLength=index;
    }
    else {
        buffLength=width;
    }

    STRINGREF Local = StringObject::NewString(buffLength);
    WCHAR *LocalBuffer = Local->GetBuffer();

    //Put the characters into the String in reverse order
    //Fill the remaining space -- if there is any --
    //with the correct padding character.
    if ((flags&LeftAlign)!=0) {
        for (i=0; i<index; i++) {
            LocalBuffer[i]=buffer[index-i-1];
        }
        for (;i<buffLength; i++) {
            LocalBuffer[i]=paddingChar;
        }
    }
    else {
        for (i=0; i<index; i++) {
            LocalBuffer[buffLength-i-1]=buffer[i];
        }
        for (int j=buffLength-i-1; j>=0; j--) {
            LocalBuffer[j]=paddingChar;
        }
    }

    *((STRINGREF *)&rv)=Local;

    HELPER_METHOD_FRAME_END();

    return rv;
}
FCIMPLEND


/*===================================FixRadix===================================
**It's possible that we parsed the radix in a base other than 10 by accident.
**This method will take that number, verify that it only contained valid base 10
**digits, and then do the conversion to base 10.  If it contained invalid digits,
**they tried to pass us a radix such as 1A, so we throw a FormatException.
**
**Args: oldVal: The value that we had actually parsed in some arbitrary base.
**      oldBase: The base in which we actually did the parsing.
**
**Returns:  oldVal as if it had been parsed as a base-10 number.
**Exceptions: FormatException if either of the digits in the radix aren't
**            valid base-10 numbers.
==============================================================================*/
int FixRadix(int oldVal, int oldBase)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    int firstDigit = (oldVal/oldBase);
    int secondDigit = (oldVal%oldBase);

    if ((firstDigit>=10) || (secondDigit>=10))
        COMPlusThrow(kFormatException, W("Format_BadBase"));

    return(firstDigit*10)+secondDigit;
}

/*=================================StringToLong=================================
**Action:
**Returns:
**Exceptions:
==============================================================================*/
FCIMPL4(INT64, ParseNumbers::StringToLong, StringObject * s, INT32 radix, INT32 flags, INT32 *currPos)
{
    FCALL_CONTRACT;

    INT64 result = 0;

    HELPER_METHOD_FRAME_BEGIN_RET_1(s);

    int sign = 1;
    WCHAR *input;
    int length;
    int i;
    int grabNumbersStart=0;
    INT32 r;

    _ASSERTE((flags & PARSE_TREATASI1) == 0 && (flags & PARSE_TREATASI2) == 0);

    if (s) {
        i = currPos ? *currPos : 0;

        //Do some radix checking.
        //A radix of -1 says to use whatever base is spec'd on the number.
        //Parse in Base10 until we figure out what the base actually is.
        r = (-1==radix)?10:radix;

        if (r!=2 && r!=10 && r!=8 && r!=16)
            COMPlusThrow(kArgumentException, W("Arg_InvalidBase"));

        s->RefInterpretGetStringValuesDangerousForGC(&input, &length);

        if (i<0 || i>=length)
            COMPlusThrowArgumentOutOfRange(W("startIndex"), W("ArgumentOutOfRange_Index"));

        //Get rid of the whitespace and then check that we've still got some digits to parse.
        if (!(flags & PARSE_ISTIGHT) && !(flags & PARSE_NOSPACE)) {
            EatWhiteSpace(input,length,&i);
            if (i==length)
                COMPlusThrow(kFormatException, W("Format_EmptyInputString"));
        }

        //Check for a sign
        if (input[i]=='-') {
            if (r != 10)
                COMPlusThrow(kArgumentException, W("Arg_CannotHaveNegativeValue"));

            if (flags & PARSE_TREATASUNSIGNED)
                COMPlusThrow(kOverflowException, W("Overflow_NegativeUnsigned"));

            sign = -1;
            i++;
        }
        else if (input[i]=='+') {
            i++;
        }

        if ((radix==-1 || radix==16) && (i+1<length) && input[i]=='0') {
            if (input[i+1]=='x' || input [i+1]=='X') {
                r=16;
                i+=2;
            }
        }

        grabNumbersStart=i;
        result = GrabLongs(r,input,length,&i, (flags & PARSE_TREATASUNSIGNED));

        //Check if they passed us a string with no parsable digits.
        if (i==grabNumbersStart)
            COMPlusThrow(kFormatException, W("Format_NoParsibleDigits"));

        if (flags & PARSE_ISTIGHT) {
            //If we've got effluvia left at the end of the string, complain.
            if (i<length)
                COMPlusThrow(kFormatException, W("Format_ExtraJunkAtEnd"));
        }

        //Put the current index back into the correct place.
        if (currPos != NULL) *currPos = i;

        //Return the value properly signed.
        if ((UINT64) result==UI64(0x8000000000000000) && sign==1 && r==10 && !(flags & PARSE_TREATASUNSIGNED))
            COMPlusThrow(kOverflowException, W("Overflow_Int64"));

        if (r == 10)
            result *= sign;
    }
    else {
        result = 0;
    }

    HELPER_METHOD_FRAME_END();

    return result;
}
FCIMPLEND

FCIMPL4(INT32, ParseNumbers::StringToInt, StringObject * s, INT32 radix, INT32 flags, INT32* currPos)
{
    FCALL_CONTRACT;

    INT32 result = 0;

    HELPER_METHOD_FRAME_BEGIN_RET_1(s);

    int sign = 1;
    WCHAR *input;
    int length;
    int i;
    int grabNumbersStart=0;
    INT32 r;

    // TreatAsI1 and TreatAsI2 are mutually exclusive.
    _ASSERTE(!((flags & PARSE_TREATASI1) != 0 && (flags & PARSE_TREATASI2) != 0));

    if (s) {
        //They're requied to tell me where to start parsing.
        i = currPos ? (*currPos) : 0;

        //Do some radix checking.
        //A radix of -1 says to use whatever base is spec'd on the number.
        //Parse in Base10 until we figure out what the base actually is.
        r = (-1==radix)?10:radix;

        if (r!=2 && r!=10 && r!=8 && r!=16)
            COMPlusThrow(kArgumentException, W("Arg_InvalidBase"));

        s->RefInterpretGetStringValuesDangerousForGC(&input, &length);

        if (i<0 || i>=length)
            COMPlusThrowArgumentOutOfRange(W("startIndex"), W("ArgumentOutOfRange_Index"));

        //Get rid of the whitespace and then check that we've still got some digits to parse.
        if (!(flags & PARSE_ISTIGHT) && !(flags & PARSE_NOSPACE)) {
            EatWhiteSpace(input,length,&i);
            if (i==length)
                COMPlusThrow(kFormatException, W("Format_EmptyInputString"));
        }

        //Check for a sign
        if (input[i]=='-') {
            if (r != 10)
                COMPlusThrow(kArgumentException, W("Arg_CannotHaveNegativeValue"));

            if (flags & PARSE_TREATASUNSIGNED)
                COMPlusThrow(kOverflowException, W("Overflow_NegativeUnsigned"));

            sign = -1;
            i++;
        }
        else if (input[i]=='+') {
            i++;
        }

        //Consume the 0x if we're in an unknown base or in base-16.
        if ((radix==-1||radix==16) && (i+1<length) && input[i]=='0') {
            if (input[i+1]=='x' || input [i+1]=='X') {
                r=16;
                i+=2;
            }
        }

        grabNumbersStart=i;
        result = GrabInts(r,input,length,&i, (flags & PARSE_TREATASUNSIGNED));

        //Check if they passed us a string with no parsable digits.
        if (i==grabNumbersStart)
            COMPlusThrow(kFormatException, W("Format_NoParsibleDigits"));

        if (flags & PARSE_ISTIGHT) {
            //If we've got effluvia left at the end of the string, complain.
            if (i<(length))
                COMPlusThrow(kFormatException, W("Format_ExtraJunkAtEnd"));
        }

        //Put the current index back into the correct place.
        if (currPos != NULL) *currPos = i;

        //Return the value properly signed.
        if (flags & PARSE_TREATASI1) {
            if ((UINT32)result > 0xFF)
                COMPlusThrow(kOverflowException, W("Overflow_SByte"));

            // result looks positive when parsed as an I4
            _ASSERTE(sign==1 || r==10);
        }
        else if (flags & PARSE_TREATASI2) {
            if ((UINT32)result > 0xFFFF)
                COMPlusThrow(kOverflowException, W("Overflow_Int16"));

            // result looks positive when parsed as an I4
            _ASSERTE(sign==1 || r==10);
        }
        else if ((UINT32) result==0x80000000U && sign==1 && r==10 && !(flags & PARSE_TREATASUNSIGNED)) {
            COMPlusThrow(kOverflowException, W("Overflow_Int32"));
        }

        if (r == 10)
            result *= sign;
    }
    else {
        result = 0;
    }

    HELPER_METHOD_FRAME_END();

    return result;
}
FCIMPLEND

//
//
// EXCEPTION NATIVE
//
//
FCIMPL1(FC_BOOL_RET, ExceptionNative::IsImmutableAgileException, Object* pExceptionUNSAFE)
{
    FCALL_CONTRACT;

    ASSERT(pExceptionUNSAFE != NULL);

    OBJECTREF pException = (OBJECTREF) pExceptionUNSAFE;

    // The preallocated exception objects may be used from multiple AppDomains
    // and therefore must remain immutable from the application's perspective.
    FC_RETURN_BOOL(CLRException::IsPreallocatedExceptionObject(pException));
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, ExceptionNative::IsTransient, INT32 hresult)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(Exception::IsTransient(hresult));
}
FCIMPLEND

#ifndef FEATURE_CORECLR

FCIMPL3(StringObject *, ExceptionNative::StripFileInfo, Object *orefExcepUNSAFE, StringObject *orefStrUNSAFE, CLR_BOOL isRemoteStackTrace)
{
    FCALL_CONTRACT;

    OBJECTREF orefExcep = ObjectToOBJECTREF(orefExcepUNSAFE);
    STRINGREF orefStr = (STRINGREF)ObjectToOBJECTREF(orefStrUNSAFE);

    if (orefStr == NULL)
    {
        return NULL;
    }

    HELPER_METHOD_FRAME_BEGIN_RET_2(orefExcep, orefStr);

    if (isRemoteStackTrace)
    {
        if (!AppX::IsAppXProcess() && ExceptionTypeOverridesStackTraceGetter(orefExcep->GetMethodTable()))
        {
            // In classic processes, the remote stack trace could have been generated using a custom get_StackTrace
            // override which means that we would not be able to parse is - strip the whole string by returning NULL.
            orefStr = NULL;
        }
    }

    if (orefStr != NULL)
    {
        SString stackTrace;
        orefStr->GetSString(stackTrace);

        StripFileInfoFromStackTrace(stackTrace);

        orefStr = AllocateString(stackTrace);
    }

    HELPER_METHOD_FRAME_END();
    return (StringObject *)OBJECTREFToObject(orefStr);
}
FCIMPLEND

#endif // !FEATURE_CORECLR

#if defined(FEATURE_EXCEPTIONDISPATCHINFO)
// This FCall sets a flag against the thread exception state to indicate to
// IL_Throw and the StackTraceInfo implementation to account for the fact
// that we have restored a foreign exception dispatch details.
//
// Refer to the respective methods for details on how they use this flag.
FCIMPL0(VOID, ExceptionNative::PrepareForForeignExceptionRaise)
{
    FCALL_CONTRACT;

    PTR_ThreadExceptionState pCurTES = GetThread()->GetExceptionState();

	// Set a flag against the TES to indicate this is a foreign exception raise.
	pCurTES->SetRaisingForeignException();
}
FCIMPLEND

// Given an exception object, this method will extract the stacktrace and dynamic method array and set them up for return to the caller.
FCIMPL3(VOID, ExceptionNative::GetStackTracesDeepCopy, Object* pExceptionObjectUnsafe, Object **pStackTraceUnsafe, Object **pDynamicMethodsUnsafe);
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    ASSERT(pExceptionObjectUnsafe != NULL);
    ASSERT(pStackTraceUnsafe != NULL);
    ASSERT(pDynamicMethodsUnsafe != NULL);

    struct _gc
    {
        StackTraceArray stackTrace;
        StackTraceArray stackTraceCopy;
        EXCEPTIONREF refException;
        PTRARRAYREF dynamicMethodsArray; // Object array of Managed Resolvers
        PTRARRAYREF dynamicMethodsArrayCopy; // Copy of the object array of Managed Resolvers
    };
    _gc gc;
    ZeroMemory(&gc, sizeof(gc));

    // GC protect the array reference
    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);
    
    // Get the exception object reference
    gc.refException = (EXCEPTIONREF)(ObjectToOBJECTREF(pExceptionObjectUnsafe));

    // Fetch the stacktrace details from the exception under a lock
    gc.refException->GetStackTrace(gc.stackTrace, &gc.dynamicMethodsArray);
    
    bool fHaveStackTrace = false;
    bool fHaveDynamicMethodArray = false;

    if ((unsigned)gc.stackTrace.Size() > 0)
    {
        // Deepcopy the array
        gc.stackTraceCopy.CopyFrom(gc.stackTrace);
        fHaveStackTrace = true;
    }
    
    if (gc.dynamicMethodsArray != NULL)
    {
        // Get the number of elements in the dynamic methods array
        unsigned   cOrigDynamic = gc.dynamicMethodsArray->GetNumComponents();
    
        // ..and allocate a new array. This can trigger GC or throw under OOM.
        gc.dynamicMethodsArrayCopy = (PTRARRAYREF)AllocateObjectArray(cOrigDynamic, g_pObjectClass);
    
        // Deepcopy references to the new array we just allocated
        memmoveGCRefs(gc.dynamicMethodsArrayCopy->GetDataPtr(), gc.dynamicMethodsArray->GetDataPtr(),
                                                  cOrigDynamic * sizeof(Object *));

        fHaveDynamicMethodArray = true;
    }

    // Prep to return
    *pStackTraceUnsafe = fHaveStackTrace?OBJECTREFToObject(gc.stackTraceCopy.Get()):NULL;
    *pDynamicMethodsUnsafe = fHaveDynamicMethodArray?OBJECTREFToObject(gc.dynamicMethodsArrayCopy):NULL;

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

// Given an exception object and deep copied instances of a stacktrace and/or dynamic method array, this method will set the latter in the exception object instance.
FCIMPL3(VOID, ExceptionNative::SaveStackTracesFromDeepCopy, Object* pExceptionObjectUnsafe, Object *pStackTraceUnsafe, Object *pDynamicMethodsUnsafe);
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    ASSERT(pExceptionObjectUnsafe != NULL);

    struct _gc
    {
        StackTraceArray stackTrace;
        EXCEPTIONREF refException;
        PTRARRAYREF dynamicMethodsArray; // Object array of Managed Resolvers
    };
    _gc gc;
    ZeroMemory(&gc, sizeof(gc));

    // GC protect the array reference
    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);
    
    // Get the exception object reference
    gc.refException = (EXCEPTIONREF)(ObjectToOBJECTREF(pExceptionObjectUnsafe));

    if (pStackTraceUnsafe != NULL)
    {
        // Copy the stacktrace
        StackTraceArray stackTraceArray((I1ARRAYREF)ObjectToOBJECTREF(pStackTraceUnsafe));
        gc.stackTrace.Swap(stackTraceArray);
    }

    gc.dynamicMethodsArray = NULL;
    if (pDynamicMethodsUnsafe != NULL)
    {
        gc.dynamicMethodsArray = (PTRARRAYREF)ObjectToOBJECTREF(pDynamicMethodsUnsafe);
    }

    // If there is no stacktrace, then there cannot be any dynamic method array. Thus,
    // save stacktrace only when we have it.
    if (gc.stackTrace.Size() > 0)
    {
        // Save the stacktrace details in the exception under a lock
        gc.refException->SetStackTrace(gc.stackTrace, gc.dynamicMethodsArray);
    }
    else
    {
        gc.refException->SetNullStackTrace();
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

// This method performs a deep copy of the stack trace array.
FCIMPL1(Object*, ExceptionNative::CopyStackTrace, Object* pStackTraceUNSAFE)
{
    FCALL_CONTRACT;

    ASSERT(pStackTraceUNSAFE != NULL);

    struct _gc
    {
        StackTraceArray stackTrace;
        StackTraceArray stackTraceCopy;
        _gc(I1ARRAYREF refStackTrace)
            : stackTrace(refStackTrace)
        {}
    };
    _gc gc((I1ARRAYREF)(ObjectToOBJECTREF(pStackTraceUNSAFE)));

    // GC protect the array reference
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
        
    // Deepcopy the array
    gc.stackTraceCopy.CopyFrom(gc.stackTrace);
    
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.stackTraceCopy.Get());
}
FCIMPLEND

// This method performs a deep copy of the dynamic method array.
FCIMPL1(Object*, ExceptionNative::CopyDynamicMethods, Object* pDynamicMethodsUNSAFE)
{
    FCALL_CONTRACT;

    ASSERT(pDynamicMethodsUNSAFE != NULL);

    struct _gc
    {
        PTRARRAYREF dynamicMethodsArray; // Object array of Managed Resolvers
        PTRARRAYREF dynamicMethodsArrayCopy; // Copy of the object array of Managed Resolvers
        _gc()
        {}
    };
    _gc gc;
    ZeroMemory(&gc, sizeof(gc));
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
    
    gc.dynamicMethodsArray = (PTRARRAYREF)(ObjectToOBJECTREF(pDynamicMethodsUNSAFE));

    // Get the number of elements in the array
    unsigned   cOrigDynamic = gc.dynamicMethodsArray->GetNumComponents();
    // ..and allocate a new array. This can trigger GC or throw under OOM.
    gc.dynamicMethodsArrayCopy = (PTRARRAYREF)AllocateObjectArray(cOrigDynamic, g_pObjectClass);
    
    // Copy references to the new array we just allocated
    memmoveGCRefs(gc.dynamicMethodsArrayCopy->GetDataPtr(), gc.dynamicMethodsArray->GetDataPtr(),
                                              cOrigDynamic * sizeof(Object *));
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.dynamicMethodsArrayCopy);
}
FCIMPLEND

#endif // defined(FEATURE_EXCEPTIONDISPATCHINFO)

BSTR BStrFromString(STRINGREF s)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    WCHAR *wz;
    int cch;
    BSTR bstr;

    if (s == NULL)
        return NULL;

    s->RefInterpretGetStringValuesDangerousForGC(&wz, &cch);

    bstr = SysAllocString(wz);
    if (bstr == NULL)
        COMPlusThrowOM();

    return bstr;
}

static BSTR GetExceptionDescription(OBJECTREF objException)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION( IsException(objException->GetMethodTable()) );
    }
    CONTRACTL_END;

    BSTR bstrDescription;

    STRINGREF MessageString = NULL;
    GCPROTECT_BEGIN(MessageString)
    GCPROTECT_BEGIN(objException)
    {
#ifdef FEATURE_APPX
        if (AppX::IsAppXProcess())
        {
            // In AppX, call Exception.ToString(false, false) which returns a string containing the exception class
            // name and callstack without file paths/names. This is used for unhandled exception bucketing/analysis.
            MethodDescCallSite getMessage(METHOD__EXCEPTION__TO_STRING, &objException);

            ARG_SLOT GetMessageArgs[] =
            {
                ObjToArgSlot(objException),
                BoolToArgSlot(false),  // needFileLineInfo
                BoolToArgSlot(false)   // needMessage
            };
            MessageString = getMessage.Call_RetSTRINGREF(GetMessageArgs);
        }
        else
#endif // FEATURE_APPX
        {
            // read Exception.Message property
            MethodDescCallSite getMessage(METHOD__EXCEPTION__GET_MESSAGE, &objException);

            ARG_SLOT GetMessageArgs[] = { ObjToArgSlot(objException)};
            MessageString = getMessage.Call_RetSTRINGREF(GetMessageArgs);

            // if the message string is empty then use the exception classname.
            if (MessageString == NULL || MessageString->GetStringLength() == 0) {
                // call GetClassName
                MethodDescCallSite getClassName(METHOD__EXCEPTION__GET_CLASS_NAME, &objException);
                ARG_SLOT GetClassNameArgs[] = { ObjToArgSlot(objException)};
                MessageString = getClassName.Call_RetSTRINGREF(GetClassNameArgs);
                _ASSERTE(MessageString != NULL && MessageString->GetStringLength() != 0);
            }
        }

        // Allocate the description BSTR.
        int DescriptionLen = MessageString->GetStringLength();
        bstrDescription = SysAllocStringLen(MessageString->GetBuffer(), DescriptionLen);
    }
    GCPROTECT_END();
    GCPROTECT_END();

    return bstrDescription;
}

static BSTR GetExceptionSource(OBJECTREF objException)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION( IsException(objException->GetMethodTable()) );
    }
    CONTRACTL_END;

    STRINGREF refRetVal;
    GCPROTECT_BEGIN(objException)

    // read Exception.Source property
    MethodDescCallSite getSource(METHOD__EXCEPTION__GET_SOURCE, &objException);

    ARG_SLOT GetSourceArgs[] = { ObjToArgSlot(objException)};

    refRetVal = getSource.Call_RetSTRINGREF(GetSourceArgs);

    GCPROTECT_END();
    return BStrFromString(refRetVal);
}

static void GetExceptionHelp(OBJECTREF objException, BSTR *pbstrHelpFile, DWORD *pdwHelpContext)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(IsException(objException->GetMethodTable()));
        PRECONDITION(CheckPointer(pbstrHelpFile));
        PRECONDITION(CheckPointer(pdwHelpContext));
    }
    CONTRACTL_END;

    *pdwHelpContext = 0;

    GCPROTECT_BEGIN(objException);

    // read Exception.HelpLink property
    MethodDescCallSite getHelpLink(METHOD__EXCEPTION__GET_HELP_LINK, &objException);

    ARG_SLOT GetHelpLinkArgs[] = { ObjToArgSlot(objException)};
    *pbstrHelpFile = BStrFromString(getHelpLink.Call_RetSTRINGREF(GetHelpLinkArgs));

    GCPROTECT_END();

    // parse the help file to check for the presence of helpcontext
    int len = SysStringLen(*pbstrHelpFile);
    int pos = len;
    WCHAR *pwstr = *pbstrHelpFile;
    if (pwstr) {
        BOOL fFoundPound = FALSE;

        for (pos = len - 1; pos >= 0; pos--) {
            if (pwstr[pos] == W('#')) {
                fFoundPound = TRUE;
                break;
            }
        }

        if (fFoundPound) {
            int PoundPos = pos;
            int NumberStartPos = -1;
            BOOL bNumberStarted = FALSE;
            BOOL bNumberFinished = FALSE;
            BOOL bInvalidDigitsFound = FALSE;

            _ASSERTE(pwstr[pos] == W('#'));

            // Check to see if the string to the right of the pound a valid number.
            for (pos++; pos < len; pos++) {
                if (bNumberFinished) {
                    if (!COMCharacter::nativeIsWhiteSpace(pwstr[pos])) {
                        bInvalidDigitsFound = TRUE;
                        break;
                    }
                }
                else if (bNumberStarted) {
                    if (COMCharacter::nativeIsWhiteSpace(pwstr[pos])) {
                        bNumberFinished = TRUE;
                    }
                    else if (!COMCharacter::nativeIsDigit(pwstr[pos])) {
                        bInvalidDigitsFound = TRUE;
                        break;
                    }
                }
                else {
                    if (COMCharacter::nativeIsDigit(pwstr[pos])) {
                        NumberStartPos = pos;
                        bNumberStarted = TRUE;
                    }
                    else if (!COMCharacter::nativeIsWhiteSpace(pwstr[pos])) {
                        bInvalidDigitsFound = TRUE;
                        break;
                    }
                }
            }

            if (bNumberStarted && !bInvalidDigitsFound) {
                // Grab the help context and remove it from the help file.
                *pdwHelpContext = (DWORD)wtoi(&pwstr[NumberStartPos], len - NumberStartPos);

                // Allocate a new help file string of the right length.
                BSTR strOld = *pbstrHelpFile;
                *pbstrHelpFile = SysAllocStringLen(strOld, PoundPos);
                SysFreeString(strOld);
                if (!*pbstrHelpFile)
                    COMPlusThrowOM();
            }
        }
    }
}

// NOTE: caller cleans up any partially initialized BSTRs in pED
void ExceptionNative::GetExceptionData(OBJECTREF objException, ExceptionData *pED)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsException(objException->GetMethodTable()));
        PRECONDITION(CheckPointer(pED));
    }
    CONTRACTL_END;

    ZeroMemory(pED, sizeof(ExceptionData));

    if (objException->GetMethodTable() == g_pStackOverflowExceptionClass) {
        // In a low stack situation, most everything else in here will fail.
        // <TODO>@TODO: We're not turning the guard page back on here, yet.</TODO>
        pED->hr = COR_E_STACKOVERFLOW;
        pED->bstrDescription = SysAllocString(STACK_OVERFLOW_MESSAGE);
        return;
    }

    GCPROTECT_BEGIN(objException);
    pED->hr = GetExceptionHResult(objException);
    pED->bstrDescription = GetExceptionDescription(objException);
    pED->bstrSource = GetExceptionSource(objException);
    GetExceptionHelp(objException, &pED->bstrHelpFile, &pED->dwHelpContext);
    GCPROTECT_END();
    return;
}

#ifdef FEATURE_COMINTEROP

HRESULT SimpleComCallWrapper::IErrorInfo_hr()
{
    WRAPPER_NO_CONTRACT;
    return GetExceptionHResult(this->GetObjectRef());
}

BSTR SimpleComCallWrapper::IErrorInfo_bstrDescription()
{
    WRAPPER_NO_CONTRACT;
    return GetExceptionDescription(this->GetObjectRef());
}

BSTR SimpleComCallWrapper::IErrorInfo_bstrSource()
{
    WRAPPER_NO_CONTRACT;
    return GetExceptionSource(this->GetObjectRef());
}

BSTR SimpleComCallWrapper::IErrorInfo_bstrHelpFile()
{
    WRAPPER_NO_CONTRACT;
    BSTR  bstrHelpFile;
    DWORD dwHelpContext;
    GetExceptionHelp(this->GetObjectRef(), &bstrHelpFile, &dwHelpContext);
    return bstrHelpFile;
}

DWORD SimpleComCallWrapper::IErrorInfo_dwHelpContext()
{
    WRAPPER_NO_CONTRACT;
    BSTR  bstrHelpFile;
    DWORD dwHelpContext;
    GetExceptionHelp(this->GetObjectRef(), &bstrHelpFile, &dwHelpContext);
    SysFreeString(bstrHelpFile);
    return dwHelpContext;
}

GUID SimpleComCallWrapper::IErrorInfo_guid()
{
    LIMITED_METHOD_CONTRACT;
    return GUID_NULL;
}

#endif // FEATURE_COMINTEROP

FCIMPL0(EXCEPTION_POINTERS*, ExceptionNative::GetExceptionPointers)
{
    FCALL_CONTRACT;

    EXCEPTION_POINTERS* retVal = NULL;

    Thread *pThread = GetThread();
    _ASSERTE(pThread);

    if (pThread->IsExceptionInProgress())
    {
        retVal = pThread->GetExceptionState()->GetExceptionPointers();
    }

    return retVal;
}
FCIMPLEND

FCIMPL0(INT32, ExceptionNative::GetExceptionCode)
{
    FCALL_CONTRACT;

    INT32 retVal = 0;

    Thread *pThread = GetThread();
    _ASSERTE(pThread);

    if (pThread->IsExceptionInProgress())
    {
        retVal = pThread->GetExceptionState()->GetExceptionCode();
    }

    return retVal;
}
FCIMPLEND


//
// This must be implemented as an FCALL because managed code cannot
// swallow a thread abort exception without resetting the abort,
// which we don't want to do.  Additionally, we can run into deadlocks
// if we use the ResourceManager to do resource lookups - it requires
// taking managed locks when initializing Globalization & Security,
// but a thread abort on a separate thread initializing those same
// systems would also do a resource lookup via the ResourceManager.
// We've deadlocked in CompareInfo.GetCompareInfo &
// Environment.GetResourceString.  It's not practical to take all of
// our locks within CER's to avoid this problem - just use the CLR's
// unmanaged resources.
//
void QCALLTYPE ExceptionNative::GetMessageFromNativeResources(ExceptionMessageKind kind, QCall::StringHandleOnStack retMesg)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    SString buffer;
    HRESULT hr = S_OK;
    const WCHAR * wszFallbackString = NULL;

    switch(kind) {
    case ThreadAbort:
        hr = buffer.LoadResourceAndReturnHR(CCompRC::Error, IDS_EE_THREAD_ABORT);
        if (FAILED(hr)) {
            wszFallbackString = W("Thread was being aborted.");
        }
        break;

    case ThreadInterrupted:
        hr = buffer.LoadResourceAndReturnHR(CCompRC::Error, IDS_EE_THREAD_INTERRUPTED);
        if (FAILED(hr)) {
            wszFallbackString = W("Thread was interrupted from a waiting state.");
        }
        break;

    case OutOfMemory:
        hr = buffer.LoadResourceAndReturnHR(CCompRC::Error, IDS_EE_OUT_OF_MEMORY);
        if (FAILED(hr)) {
            wszFallbackString = W("Insufficient memory to continue the execution of the program.");
        }
        break;

    default:
        _ASSERTE(!"Unknown ExceptionMessageKind value!");
    }
    if (FAILED(hr)) {       
        STRESS_LOG1(LF_BCL, LL_ALWAYS, "LoadResource error: %x", hr);
        _ASSERTE(wszFallbackString != NULL);
        retMesg.Set(wszFallbackString);
    }
    else {
        retMesg.Set(buffer);
    }

    END_QCALL;
}

// BlockCopy
// This method from one primitive array to another based
//  upon an offset into each an a byte count.
FCIMPL5(VOID, Buffer::BlockCopy, ArrayBase *src, int srcOffset, ArrayBase *dst, int dstOffset, int count)
{
    FCALL_CONTRACT;

    // Verify that both the src and dst are Arrays of primitive
    //  types.
    // <TODO>@TODO: We need to check for booleans</TODO>
    if (src==NULL || dst==NULL)
        FCThrowArgumentNullVoid((src==NULL) ? W("src") : W("dst"));

    SIZE_T srcLen, dstLen;

    //
    // Use specialized fast path for byte arrays because of it is what Buffer::BlockCopy is 
    // typically used for.
    //

    MethodTable * pByteArrayMT = g_pByteArrayMT;
    _ASSERTE(pByteArrayMT != NULL);
    
    // Optimization: If src is a byte array, we can
    // simply set srcLen to GetNumComponents, without having
    // to call GetComponentSize or verifying GetArrayElementType
    if (src->GetMethodTable() == pByteArrayMT)
    {
        srcLen = src->GetNumComponents();
    }
    else
    {
        srcLen = src->GetNumComponents() * src->GetComponentSize();

        // We only want to allow arrays of primitives, no Objects.
        const CorElementType srcET = src->GetArrayElementType();
        if (!CorTypeInfo::IsPrimitiveType_NoThrow(srcET))
            FCThrowArgumentVoid(W("src"), W("Arg_MustBePrimArray"));
    }
    
    // Optimization: If copying to/from the same array, then
    // we know that dstLen and srcLen must be the same.
    if (src == dst)
    {
        dstLen = srcLen;
    }
    else if (dst->GetMethodTable() == pByteArrayMT)
    {
        dstLen = dst->GetNumComponents();
    }
    else
    {
        dstLen = dst->GetNumComponents() * dst->GetComponentSize();
        if (dst->GetMethodTable() != src->GetMethodTable())
        {
            const CorElementType dstET = dst->GetArrayElementType();
            if (!CorTypeInfo::IsPrimitiveType_NoThrow(dstET))
                FCThrowArgumentVoid(W("dest"), W("Arg_MustBePrimArray"));
        }
    }

    if (srcOffset < 0 || dstOffset < 0 || count < 0) {
        const wchar_t* str = W("srcOffset");
        if (dstOffset < 0) str = W("dstOffset");
        if (count < 0) str = W("count");
        FCThrowArgumentOutOfRangeVoid(str, W("ArgumentOutOfRange_NeedNonNegNum"));
    }

    if (srcLen < (SIZE_T)srcOffset + (SIZE_T)count || dstLen < (SIZE_T)dstOffset + (SIZE_T)count) {
        FCThrowArgumentVoid(NULL, W("Argument_InvalidOffLen"));
    }
    
    PTR_BYTE srcPtr = src->GetDataPtr() + srcOffset;
    PTR_BYTE dstPtr = dst->GetDataPtr() + dstOffset;

    if ((srcPtr != dstPtr) && (count > 0)) {
        memmove(dstPtr, srcPtr, count);
    }

    FC_GC_POLL();
}
FCIMPLEND


// InternalBlockCopy
// This method from one primitive array to another based
//  upon an offset into each an a byte count.
FCIMPL5(VOID, Buffer::InternalBlockCopy, ArrayBase *src, int srcOffset, ArrayBase *dst, int dstOffset, int count)
{
    FCALL_CONTRACT;

    // @TODO: We should consider writing this in managed code.  We probably
    // cannot easily do this though - how do we get at the array's data?

    // Unfortunately, we must do a check to make sure we're writing within
    // the bounds of the array.  This will ensure that we don't overwrite
    // memory elsewhere in the system nor do we write out junk.  This can
    // happen if multiple threads interact with our IO classes simultaneously
    // without being threadsafe.  Throw here.  
    // Unfortunately this even applies to setting our internal buffers to
    // null.  We don't want to debug races between Close and Read or Write.
    if (src == NULL || dst == NULL)
        FCThrowResVoid(kIndexOutOfRangeException, W("IndexOutOfRange_IORaceCondition"));

    SIZE_T srcLen = src->GetNumComponents() * src->GetComponentSize();
    SIZE_T dstLen = srcLen;
    if (src != dst)
        dstLen = dst->GetNumComponents() * dst->GetComponentSize();

    if (srcOffset < 0 || dstOffset < 0 || count < 0)
        FCThrowResVoid(kIndexOutOfRangeException, W("IndexOutOfRange_IORaceCondition"));

    if (srcLen < (SIZE_T)srcOffset + (SIZE_T)count || dstLen < (SIZE_T)dstOffset + (SIZE_T)count)
        FCThrowResVoid(kIndexOutOfRangeException, W("IndexOutOfRange_IORaceCondition"));

    _ASSERTE(srcOffset >= 0);
    _ASSERTE((src->GetNumComponents() * src->GetComponentSize()) - (unsigned) srcOffset >= (unsigned) count);
    _ASSERTE((dst->GetNumComponents() * dst->GetComponentSize()) - (unsigned) dstOffset >= (unsigned) count);
    _ASSERTE(dstOffset >= 0);
    _ASSERTE(count >= 0);

    // Copy the data.
    memmove(dst->GetDataPtr() + dstOffset, src->GetDataPtr() + srcOffset, count);

    FC_GC_POLL();
}
FCIMPLEND

void QCALLTYPE Buffer::MemMove(void *dst, void *src, size_t length)
{
    QCALL_CONTRACT;

#if defined(FEATURE_CORECLR) && !defined(FEATURE_CORESYSTEM)
    // Callers of memcpy do expect and handle access violations in some scenarios.
    // Access violations in the runtime dll are turned into fail fast by the vector exception handler by default.
    // We need to supress this behavior for CoreCLR using AVInRuntimeImplOkayHolder because of memcpy is statically linked in.
    AVInRuntimeImplOkayHolder avOk;
#endif

    memmove(dst, src, length);
}

// Returns a bool to indicate if the array is of primitive types or not.
FCIMPL1(FC_BOOL_RET, Buffer::IsPrimitiveTypeArray, ArrayBase *arrayUNSAFE)
{
    FCALL_CONTRACT;

    _ASSERTE(arrayUNSAFE != NULL);

    // Check the type from the contained element's handle
    TypeHandle elementTH = arrayUNSAFE->GetArrayElementTypeHandle();
    BOOL fIsPrimitiveTypeArray = CorTypeInfo::IsPrimitiveType_NoThrow(elementTH.GetVerifierCorElementType());

    FC_RETURN_BOOL(fIsPrimitiveTypeArray);

}
FCIMPLEND

// Gets a particular byte out of the array.  The array can't be an array of Objects - it
// must be a primitive array.
FCIMPL2(FC_UINT8_RET, Buffer::GetByte, ArrayBase *arrayUNSAFE, INT32 index)
{
    FCALL_CONTRACT;

    _ASSERTE(arrayUNSAFE != NULL);
    _ASSERTE(index >=0 && index < ((INT32)(arrayUNSAFE->GetComponentSize() * arrayUNSAFE->GetNumComponents())));

    UINT8 bData = *((BYTE*)arrayUNSAFE->GetDataPtr() + index);
    return bData;
}
FCIMPLEND

// Sets a particular byte in an array.  The array can't be an array of Objects - it
// must be a primitive array.
//
// Semantically the bData argment is of type BYTE but FCallCheckSignature expects the 
// type to be UINT8 and raises an error if this isn't this case when 
// COMPlus_ConsistencyCheck is set.
FCIMPL3(VOID, Buffer::SetByte, ArrayBase *arrayUNSAFE, INT32 index, UINT8 bData)
{
    FCALL_CONTRACT;

    _ASSERTE(arrayUNSAFE != NULL);
    _ASSERTE(index >=0 && index < ((INT32)(arrayUNSAFE->GetComponentSize() * arrayUNSAFE->GetNumComponents())));
    
    *((BYTE*)arrayUNSAFE->GetDataPtr() + index) = (BYTE) bData;
}
FCIMPLEND

// Returns the length in bytes of an array containing
// primitive type elements
FCIMPL1(INT32, Buffer::ByteLength, ArrayBase* arrayUNSAFE)
{
    FCALL_CONTRACT;

    _ASSERTE(arrayUNSAFE != NULL);

    SIZE_T iRetVal = arrayUNSAFE->GetNumComponents() * arrayUNSAFE->GetComponentSize();

    // This API is explosed both as Buffer.ByteLength and also used indirectly in argument
    // checks for Buffer.GetByte/SetByte.
    //
    // If somebody called Get/SetByte on 2GB+ arrays, there is a decent chance that 
    // the computation of the index has overflowed. Thus we intentionally always 
    // throw on 2GB+ arrays in Get/SetByte argument checks (even for indicies <2GB)
    // to prevent people from running into a trap silently.
    if (iRetVal > INT32_MAX)
        FCThrow(kOverflowException);

    return (INT32)iRetVal;
}
FCIMPLEND

//
// GCInterface
//
MethodDesc *GCInterface::m_pCacheMethod=NULL;

UINT64   GCInterface::m_ulMemPressure = 0;
UINT64   GCInterface::m_ulThreshold = MIN_GC_MEMORYPRESSURE_THRESHOLD;
INT32    GCInterface::m_gc_counts[3] = {0,0,0};
CrstStatic GCInterface::m_MemoryPressureLock;

UINT64   GCInterface::m_addPressure[NEW_PRESSURE_COUNT] = {0, 0, 0, 0};   // history of memory pressure additions
UINT64   GCInterface::m_remPressure[NEW_PRESSURE_COUNT] = {0, 0, 0, 0};   // history of memory pressure removals

// incremented after a gen2 GC has been detected,
// (m_iteration % NEW_PRESSURE_COUNT) is used as an index into m_addPressure and m_remPressure
UINT     GCInterface::m_iteration = 0;

FCIMPL0(int, GCInterface::GetGcLatencyMode)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();

    int result = (INT32)GCHeap::GetGCHeap()->GetGcLatencyMode();
    return result;
}
FCIMPLEND

FCIMPL1(int, GCInterface::SetGcLatencyMode, int newLatencyMode)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();
    
    return GCHeap::GetGCHeap()->SetGcLatencyMode(newLatencyMode);
}
FCIMPLEND

FCIMPL0(int, GCInterface::GetLOHCompactionMode)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();

    int result = (INT32)GCHeap::GetGCHeap()->GetLOHCompactionMode();
    return result;
}
FCIMPLEND

FCIMPL1(void, GCInterface::SetLOHCompactionMode, int newLOHCompactionyMode)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();
    
    GCHeap::GetGCHeap()->SetLOHCompactionMode(newLOHCompactionyMode);
}
FCIMPLEND


FCIMPL2(FC_BOOL_RET, GCInterface::RegisterForFullGCNotification, UINT32 gen2Percentage, UINT32 lohPercentage)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();

    FC_RETURN_BOOL(GCHeap::GetGCHeap()->RegisterForFullGCNotification(gen2Percentage, lohPercentage));
}
FCIMPLEND

FCIMPL0(FC_BOOL_RET, GCInterface::CancelFullGCNotification)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();
    FC_RETURN_BOOL(GCHeap::GetGCHeap()->CancelFullGCNotification());
}
FCIMPLEND

FCIMPL1(int, GCInterface::WaitForFullGCApproach, int millisecondsTimeout)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        DISABLED(GC_TRIGGERS);  // can't use this in an FCALL because we're in forbid gc mode until we setup a H_M_F.
        SO_TOLERANT;
    }
    CONTRACTL_END;

    int result = 0; 

    //We don't need to check the top end because the GC will take care of that.
    HELPER_METHOD_FRAME_BEGIN_RET_0();

    DWORD dwMilliseconds = ((millisecondsTimeout == -1) ? INFINITE : millisecondsTimeout);
    result = GCHeap::GetGCHeap()->WaitForFullGCApproach(dwMilliseconds);

    HELPER_METHOD_FRAME_END();

    return result;
}
FCIMPLEND

FCIMPL1(int, GCInterface::WaitForFullGCComplete, int millisecondsTimeout)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        DISABLED(GC_TRIGGERS);  // can't use this in an FCALL because we're in forbid gc mode until we setup a H_M_F.
        SO_TOLERANT;
    }
    CONTRACTL_END;

    int result = 0; 

    //We don't need to check the top end because the GC will take care of that.
    HELPER_METHOD_FRAME_BEGIN_RET_0();

    DWORD dwMilliseconds = ((millisecondsTimeout == -1) ? INFINITE : millisecondsTimeout);
    result = GCHeap::GetGCHeap()->WaitForFullGCComplete(dwMilliseconds);

    HELPER_METHOD_FRAME_END();

    return result;
}
FCIMPLEND

/*================================GetGeneration=================================
**Action: Returns the generation in which args->obj is found.
**Returns: The generation in which args->obj is found.
**Arguments: args->obj -- The object to locate.
**Exceptions: ArgumentException if args->obj is null.
==============================================================================*/
FCIMPL1(int, GCInterface::GetGeneration, Object* objUNSAFE)
{
    FCALL_CONTRACT;

    if (objUNSAFE == NULL)
        FCThrowArgumentNull(W("obj"));

    int result = (INT32)GCHeap::GetGCHeap()->WhichGeneration(objUNSAFE);
    FC_GC_POLL_RET();
    return result;
}
FCIMPLEND

/*================================CollectionCount=================================
**Action: Returns the number of collections for this generation since the begining of the life of the process
**Returns: The collection count.
**Arguments: args->generation -- The generation
**Exceptions: Argument exception if args->generation is < 0 or > GetMaxGeneration();
==============================================================================*/
FCIMPL2(int, GCInterface::CollectionCount, INT32 generation, INT32 getSpecialGCCount)
{
    FCALL_CONTRACT;

    //We've already checked this in GC.cs, so we'll just assert it here.
    _ASSERTE(generation >= 0);

    //We don't need to check the top end because the GC will take care of that.
    int result = (INT32)GCHeap::GetGCHeap()->CollectionCount(generation, getSpecialGCCount);
    FC_GC_POLL_RET();
    return result;
}
FCIMPLEND

int QCALLTYPE GCInterface::StartNoGCRegion(INT64 totalSize, BOOL lohSizeKnown, INT64 lohSize, BOOL disallowFullBlockingGC)
{
    QCALL_CONTRACT;

    int retVal = 0;

    BEGIN_QCALL;

    GCX_COOP();

    retVal = GCHeap::GetGCHeap()->StartNoGCRegion((ULONGLONG)totalSize, 
                                                  lohSizeKnown,
                                                  (ULONGLONG)lohSize,
                                                  disallowFullBlockingGC);

    END_QCALL;

    return retVal;
}

int QCALLTYPE GCInterface::EndNoGCRegion()
{
    QCALL_CONTRACT;

    int retVal = FALSE;

    BEGIN_QCALL;

    retVal = GCHeap::GetGCHeap()->EndNoGCRegion();

    END_QCALL;

    return retVal;
}

/*===============================GetGenerationWR================================
**Action: Returns the generation in which the object pointed to by a WeakReference is found.
**Returns:
**Arguments: args->handle -- the OBJECTHANDLE to the object which we're locating.
**Exceptions: ArgumentException if handle points to an object which is not accessible.
==============================================================================*/
FCIMPL1(int, GCInterface::GetGenerationWR, LPVOID handle)
{
    FCALL_CONTRACT;

    int iRetVal = 0;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    OBJECTREF temp;
    temp = ObjectFromHandle((OBJECTHANDLE) handle);
    if (temp == NULL)
        COMPlusThrowArgumentNull(W("weak handle"));

    iRetVal = (INT32)GCHeap::GetGCHeap()->WhichGeneration(OBJECTREFToObject(temp));

    HELPER_METHOD_FRAME_END();

    return iRetVal;
}
FCIMPLEND

/*================================GetTotalMemory================================
**Action: Returns the total number of bytes in use
**Returns: The total number of bytes in use
**Arguments: None
**Exceptions: None
==============================================================================*/
INT64 QCALLTYPE GCInterface::GetTotalMemory()
{
    QCALL_CONTRACT;

    INT64 iRetVal = 0;

    BEGIN_QCALL;

    GCX_COOP();
    iRetVal = (INT64) GCHeap::GetGCHeap()->GetTotalBytesInUse();

    END_QCALL;

    return iRetVal;
}

/*==============================Collect=========================================
**Action: Collects all generations <= args->generation
**Returns: void
**Arguments: args->generation:  The maximum generation to collect
**Exceptions: Argument exception if args->generation is < 0 or > GetMaxGeneration();
==============================================================================*/
void QCALLTYPE GCInterface::Collect(INT32 generation, INT32 mode)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    //We've already checked this in GC.cs, so we'll just assert it here.
    _ASSERTE(generation >= -1);

    //We don't need to check the top end because the GC will take care of that.

    GCX_COOP();
    GCHeap::GetGCHeap()->GarbageCollect(generation, FALSE, mode);

    END_QCALL;
}


/*==========================WaitForPendingFinalizers============================
**Action: Run all Finalizers that haven't been run.
**Arguments: None
**Exceptions: None
==============================================================================*/
void QCALLTYPE GCInterface::WaitForPendingFinalizers()
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    FinalizerThread::FinalizerThreadWait();

    END_QCALL;
}


/*===============================GetMaxGeneration===============================
**Action: Returns the largest GC generation
**Returns: The largest GC Generation
**Arguments: None
**Exceptions: None
==============================================================================*/
FCIMPL0(int, GCInterface::GetMaxGeneration)
{
    FCALL_CONTRACT;

    return(INT32)GCHeap::GetGCHeap()->GetMaxGeneration();
}
FCIMPLEND


/*==============================SuppressFinalize================================
**Action: Indicate that an object's finalizer should not be run by the system
**Arguments: Object of interest
**Exceptions: None
==============================================================================*/
FCIMPL1(void, GCInterface::SuppressFinalize, Object *obj)
{
    FCALL_CONTRACT;

    // Checked by the caller
    _ASSERTE(obj != NULL);

    if (!obj->GetMethodTable ()->HasFinalizer())
        return;

    GCHeap::GetGCHeap()->SetFinalizationRun(obj);
    FC_GC_POLL();
}
FCIMPLEND


/*============================ReRegisterForFinalize==============================
**Action: Indicate that an object's finalizer should be run by the system.
**Arguments: Object of interest
**Exceptions: None
==============================================================================*/
FCIMPL1(void, GCInterface::ReRegisterForFinalize, Object *obj)
{
    FCALL_CONTRACT;

    // Checked by the caller
    _ASSERTE(obj != NULL);

    if (obj->GetMethodTable()->HasFinalizer())
    {
        HELPER_METHOD_FRAME_BEGIN_1(obj);
        GCHeap::GetGCHeap()->RegisterForFinalization(-1, obj);
        HELPER_METHOD_FRAME_END();
    }
}
FCIMPLEND

FORCEINLINE UINT64 GCInterface::InterlockedAdd (UINT64 *pAugend, UINT64 addend) {
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    UINT64 oldMemValue;
    UINT64 newMemValue;

    do {
        oldMemValue = *pAugend;
        newMemValue = oldMemValue + addend;

        // check for overflow
        if (newMemValue < oldMemValue)
        {
            newMemValue = UINT64_MAX;
        }
    } while (InterlockedCompareExchange64((LONGLONG*) pAugend, (LONGLONG) newMemValue, (LONGLONG) oldMemValue) != (LONGLONG) oldMemValue);

    return newMemValue;
}

FORCEINLINE UINT64 GCInterface::InterlockedSub(UINT64 *pMinuend, UINT64 subtrahend) {
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    UINT64 oldMemValue;
    UINT64 newMemValue;

    do {
        oldMemValue = *pMinuend;
        newMemValue = oldMemValue - subtrahend;

        // check for underflow
        if (newMemValue > oldMemValue)
            newMemValue = 0;
        
    } while (InterlockedCompareExchange64((LONGLONG*) pMinuend, (LONGLONG) newMemValue, (LONGLONG) oldMemValue) != (LONGLONG) oldMemValue);

    return newMemValue;
}

void QCALLTYPE GCInterface::_AddMemoryPressure(UINT64 bytesAllocated) 
{
    QCALL_CONTRACT;

    // AddMemoryPressure could cause a GC, so we need a frame 
    BEGIN_QCALL;
    AddMemoryPressure(bytesAllocated);
    END_QCALL;
}

void GCInterface::AddMemoryPressure(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    SendEtwAddMemoryPressureEvent(bytesAllocated);

    UINT64 newMemValue = InterlockedAdd(&m_ulMemPressure, bytesAllocated);

    if (newMemValue > m_ulThreshold)
    {
        INT32 gen_collect = 0;
        {
            GCX_PREEMP();
            CrstHolder holder(&m_MemoryPressureLock);

            // to avoid collecting too often, take the max threshold of the linear and geometric growth 
            // heuristics.          
            UINT64 addMethod;
            UINT64 multMethod;
            UINT64 bytesAllocatedMax = (UINT64_MAX - m_ulThreshold) / 8;

            if (bytesAllocated >= bytesAllocatedMax) // overflow check
            {
                addMethod = UINT64_MAX;
            }
            else
            {
                addMethod = m_ulThreshold + bytesAllocated * 8;
            }

            multMethod = newMemValue + newMemValue / 10;
            if (multMethod < newMemValue) // overflow check
            {
                multMethod = UINT64_MAX;
            }

            m_ulThreshold = (addMethod > multMethod) ? addMethod : multMethod;
            for (int i = 0; i <= 1; i++)
            {
                if ((GCHeap::GetGCHeap()->CollectionCount(i) / RELATIVE_GC_RATIO) > GCHeap::GetGCHeap()->CollectionCount(i + 1))
                {
                    gen_collect = i + 1;
                    break;
                }
            }
        }

        PREFIX_ASSUME(gen_collect <= 2);

        if ((gen_collect == 0) || (m_gc_counts[gen_collect] == GCHeap::GetGCHeap()->CollectionCount(gen_collect)))
        {
            GarbageCollectModeAny(gen_collect);
        }

        for (int i = 0; i < 3; i++) 
        {
            m_gc_counts [i] = GCHeap::GetGCHeap()->CollectionCount(i);
        }
    }
}

#ifdef _WIN64
const unsigned MIN_MEMORYPRESSURE_BUDGET = 4 * 1024 * 1024;        // 4 MB
#else // _WIN64
const unsigned MIN_MEMORYPRESSURE_BUDGET = 3 * 1024 * 1024;        // 3 MB
#endif // _WIN64

const unsigned MAX_MEMORYPRESSURE_RATIO = 10;                      // 40 MB or 30 MB


// Resets pressure accounting after a gen2 GC has occurred.
void GCInterface::CheckCollectionCount()
{
    LIMITED_METHOD_CONTRACT;

    GCHeap * pHeap = GCHeap::GetGCHeap();
    
    if (m_gc_counts[2] != pHeap->CollectionCount(2))
    {
        for (int i = 0; i < 3; i++) 
        {
            m_gc_counts[i] = pHeap->CollectionCount(i);
        }

        m_iteration++;

        UINT p = m_iteration % NEW_PRESSURE_COUNT;

        m_addPressure[p] = 0;   // new pressure will be accumulated here
        m_remPressure[p] = 0; 
    }
}


// New AddMemoryPressure implementation (used by RCW and the CLRServicesImpl class)
//
//   1. Less sensitive than the original implementation (start budget 3 MB)
//   2. Focuses more on newly added memory pressure
//   3. Budget adjusted by effectiveness of last 3 triggered GC (add / remove ratio, max 10x)
//   4. Budget maxed with 30% of current managed GC size
//   5. If Gen2 GC is happening naturally, ignore past pressure
//
// Here's a brief description of the ideal algorithm for Add/Remove memory pressure:
// Do a GC when (HeapStart < X * MemPressureGrowth) where
// - HeapStart is GC Heap size after doing the last GC
// - MemPressureGrowth is the net of Add and Remove since the last GC
// - X is proportional to our guess of the ummanaged memory death rate per GC interval,
//   and would be calculated based on historic data using standard exponential approximation:
//   Xnew = UMDeath/UMTotal * 0.5 + Xprev
//
void GCInterface::NewAddMemoryPressure(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    CheckCollectionCount();

    UINT p = m_iteration % NEW_PRESSURE_COUNT;

    UINT64 newMemValue = InterlockedAdd(&m_addPressure[p], bytesAllocated);

    static_assert(NEW_PRESSURE_COUNT == 4, "NewAddMemoryPressure contains unrolled loops which depend on NEW_PRESSURE_COUNT");

    UINT64 add = m_addPressure[0] + m_addPressure[1] + m_addPressure[2] + m_addPressure[3] - m_addPressure[p];
    UINT64 rem = m_remPressure[0] + m_remPressure[1] + m_remPressure[2] + m_remPressure[3] - m_remPressure[p];

    STRESS_LOG4(LF_GCINFO, LL_INFO10000, "AMP Add: %I64u => added=%I64u total_added=%I64u total_removed=%I64u",
        bytesAllocated, newMemValue, add, rem);

    SendEtwAddMemoryPressureEvent(bytesAllocated); 

    if (newMemValue >= MIN_MEMORYPRESSURE_BUDGET)
    {
        UINT64 budget = MIN_MEMORYPRESSURE_BUDGET;

        if (m_iteration >= NEW_PRESSURE_COUNT) // wait until we have enough data points
        {
            // Adjust according to effectiveness of GC
            // Scale budget according to past m_addPressure / m_remPressure ratio
            if (add >= rem * MAX_MEMORYPRESSURE_RATIO)
            {
                budget = MIN_MEMORYPRESSURE_BUDGET * MAX_MEMORYPRESSURE_RATIO;
            }
            else if (add > rem)
            {
                CONSISTENCY_CHECK(rem != 0);

                // Avoid overflow by calculating addPressure / remPressure as fixed point (1 = 1024)
                budget = (add * 1024 / rem) * budget / 1024;
            }
        }

        // If still over budget, check current managed heap size
        if (newMemValue >= budget)
        {
            GCHeap *pGCHeap = GCHeap::GetGCHeap();
            UINT64 heapOver3 = pGCHeap->GetCurrentObjSize() / 3;

            if (budget < heapOver3) // Max
            {
                budget = heapOver3;
            }

            if (newMemValue >= budget)
            {
                // last check - if we would exceed 20% of GC "duty cycle", do not trigger GC at this time
                if ((pGCHeap->GetNow() - pGCHeap->GetLastGCStartTime(2)) > (pGCHeap->GetLastGCDuration(2) * 5))
                {
                    STRESS_LOG6(LF_GCINFO, LL_INFO10000, "AMP Budget: pressure=%I64u ? budget=%I64u (total_added=%I64u, total_removed=%I64u, mng_heap=%I64u) pos=%d",
                        newMemValue, budget, add, rem, heapOver3 * 3, m_iteration);

                    GarbageCollectModeAny(2);

                    CheckCollectionCount();
                }
            }
        }
    }
}

void QCALLTYPE GCInterface::_RemoveMemoryPressure(UINT64 bytesAllocated)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    RemoveMemoryPressure(bytesAllocated);
    END_QCALL;
}

void GCInterface::RemoveMemoryPressure(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    SendEtwRemoveMemoryPressureEvent(bytesAllocated);

    UINT64 newMemValue = InterlockedSub(&m_ulMemPressure, bytesAllocated);
    UINT64 new_th;  
    UINT64 bytesAllocatedMax = (m_ulThreshold / 4);
    UINT64 addMethod;
    UINT64 multMethod = (m_ulThreshold - m_ulThreshold / 20); // can never underflow
    if (bytesAllocated >= bytesAllocatedMax) // protect against underflow
    {
        m_ulThreshold = MIN_GC_MEMORYPRESSURE_THRESHOLD;
        return;
    }
    else
    {
        addMethod = m_ulThreshold - bytesAllocated * 4;
    }

    new_th = (addMethod < multMethod) ? addMethod : multMethod;

    if (newMemValue <= new_th)
    {
        GCX_PREEMP();
        CrstHolder holder(&m_MemoryPressureLock);
        if (new_th > MIN_GC_MEMORYPRESSURE_THRESHOLD)
            m_ulThreshold = new_th;
        else
            m_ulThreshold = MIN_GC_MEMORYPRESSURE_THRESHOLD;

        for (int i = 0; i < 3; i++) 
        {
            m_gc_counts [i] = GCHeap::GetGCHeap()->CollectionCount(i);
        }
    }
}

void GCInterface::NewRemoveMemoryPressure(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    CheckCollectionCount();
    
    UINT p = m_iteration % NEW_PRESSURE_COUNT;

    SendEtwRemoveMemoryPressureEvent(bytesAllocated);

    InterlockedAdd(&m_remPressure[p], bytesAllocated);

    STRESS_LOG2(LF_GCINFO, LL_INFO10000, "AMP Remove: %I64u => removed=%I64u",
        bytesAllocated, m_remPressure[p]);
}

inline void GCInterface::SendEtwAddMemoryPressureEvent(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    FireEtwIncreaseMemoryPressure(bytesAllocated, GetClrInstanceId());
}

// Out-of-line helper to avoid EH prolog/epilog in functions that otherwise don't throw.
NOINLINE void GCInterface::SendEtwRemoveMemoryPressureEvent(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    EX_TRY
    {
        FireEtwDecreaseMemoryPressure(bytesAllocated, GetClrInstanceId());
    }
    EX_CATCH
    {
        // Ignore failures
    }
    EX_END_CATCH(SwallowAllExceptions)
}

// Out-of-line helper to avoid EH prolog/epilog in functions that otherwise don't throw.
NOINLINE void GCInterface::GarbageCollectModeAny(int generation)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_COOP();
    GCHeap::GetGCHeap()->GarbageCollect(generation, FALSE, collection_non_blocking);
}

//
// COMInterlocked
//

#include <optsmallperfcritical.h>

FCIMPL2(INT32,COMInterlocked::Exchange, INT32 *location, INT32 value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    return FastInterlockExchange((LONG *) location, value);
}
FCIMPLEND

FCIMPL2_IV(INT64,COMInterlocked::Exchange64, INT64 *location, INT64 value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    return FastInterlockExchangeLong((INT64 *) location, value);
}
FCIMPLEND

FCIMPL2(LPVOID,COMInterlocked::ExchangePointer, LPVOID *location, LPVOID value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    FCUnique(0x15);
    return FastInterlockExchangePointer(location, value);
}
FCIMPLEND

FCIMPL3(INT32, COMInterlocked::CompareExchange, INT32* location, INT32 value, INT32 comparand)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    return FastInterlockCompareExchange((LONG*)location, value, comparand);
}
FCIMPLEND

FCIMPL4(INT32, COMInterlocked::CompareExchangeReliableResult, INT32* location, INT32 value, INT32 comparand, CLR_BOOL* succeeded)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    INT32 result = FastInterlockCompareExchange((LONG*)location, value, comparand);
    if (result == comparand)
        *succeeded = true;

    return result;
}
FCIMPLEND

FCIMPL3_IVV(INT64, COMInterlocked::CompareExchange64, INT64* location, INT64 value, INT64 comparand)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    return FastInterlockCompareExchangeLong((INT64*)location, value, comparand);
}
FCIMPLEND

FCIMPL3(LPVOID,COMInterlocked::CompareExchangePointer, LPVOID *location, LPVOID value, LPVOID comparand)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    FCUnique(0x59);
    return FastInterlockCompareExchangePointer(location, value, comparand);
}
FCIMPLEND

FCIMPL2_IV(float,COMInterlocked::ExchangeFloat, float *location, float value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    LONG ret = FastInterlockExchange((LONG *) location, *(LONG*)&value);
    return *(float*)&ret;
}
FCIMPLEND

FCIMPL2_IV(double,COMInterlocked::ExchangeDouble, double *location, double value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }


    INT64 ret = FastInterlockExchangeLong((INT64 *) location, *(INT64*)&value);
    return *(double*)&ret;
}
FCIMPLEND

FCIMPL3_IVV(float,COMInterlocked::CompareExchangeFloat, float *location, float value, float comparand)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    LONG ret = (LONG)FastInterlockCompareExchange((LONG*) location, *(LONG*)&value, *(LONG*)&comparand);
    return *(float*)&ret;
}
FCIMPLEND

FCIMPL3_IVV(double,COMInterlocked::CompareExchangeDouble, double *location, double value, double comparand)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    INT64 ret = (INT64)FastInterlockCompareExchangeLong((INT64*) location, *(INT64*)&value, *(INT64*)&comparand);
    return *(double*)&ret;
}
FCIMPLEND

FCIMPL2(LPVOID,COMInterlocked::ExchangeObject, LPVOID*location, LPVOID value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    LPVOID ret = FastInterlockExchangePointer(location, value);
#ifdef _DEBUG
    Thread::ObjectRefAssign((OBJECTREF *)location);
#endif
    ErectWriteBarrier((OBJECTREF*) location, ObjectToOBJECTREF((Object*) value));
    return ret;
}
FCIMPLEND

FCIMPL2_VV(void,COMInterlocked::ExchangeGeneric, FC_TypedByRef location, FC_TypedByRef value)
{
    FCALL_CONTRACT;

    LPVOID* loc = (LPVOID*)location.data;
    if( NULL == loc) {
        FCThrowVoid(kNullReferenceException);
    }

    LPVOID val = *(LPVOID*)value.data;
    *(LPVOID*)value.data = FastInterlockExchangePointer(loc, val);
#ifdef _DEBUG
    Thread::ObjectRefAssign((OBJECTREF *)loc);
#endif
    ErectWriteBarrier((OBJECTREF*) loc, ObjectToOBJECTREF((Object*) val));
}
FCIMPLEND

FCIMPL3_VVI(void,COMInterlocked::CompareExchangeGeneric, FC_TypedByRef location, FC_TypedByRef value, LPVOID comparand)
{
    FCALL_CONTRACT;

    LPVOID* loc = (LPVOID*)location.data;
    LPVOID val = *(LPVOID*)value.data;
    if( NULL == loc) {
        FCThrowVoid(kNullReferenceException);
    }

    LPVOID ret = FastInterlockCompareExchangePointer(loc, val, comparand);
    *(LPVOID*)value.data = ret;
    if(ret == comparand)
    {
#ifdef _DEBUG
        Thread::ObjectRefAssign((OBJECTREF *)loc);
#endif
        ErectWriteBarrier((OBJECTREF*) loc, ObjectToOBJECTREF((Object*) val));
    }
}
FCIMPLEND

FCIMPL3(LPVOID,COMInterlocked::CompareExchangeObject, LPVOID *location, LPVOID value, LPVOID comparand)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    // <TODO>@todo: only set ref if is updated</TODO>
    LPVOID ret = FastInterlockCompareExchangePointer(location, value, comparand);
    if (ret == comparand) {
#ifdef _DEBUG
        Thread::ObjectRefAssign((OBJECTREF *)location);
#endif
        ErectWriteBarrier((OBJECTREF*) location, ObjectToOBJECTREF((Object*) value));
    }
    return ret;
}
FCIMPLEND

FCIMPL2(INT32,COMInterlocked::ExchangeAdd32, INT32 *location, INT32 value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    return FastInterlockExchangeAdd((LONG *) location, value);
}
FCIMPLEND

FCIMPL2_IV(INT64,COMInterlocked::ExchangeAdd64, INT64 *location, INT64 value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    return FastInterlockExchangeAddLong((INT64 *) location, value);
}
FCIMPLEND

#include <optdefault.h>



FCIMPL6(INT32, ManagedLoggingHelper::GetRegistryLoggingValues, CLR_BOOL* bLoggingEnabled, CLR_BOOL* bLogToConsole, INT32 *iLogLevel, CLR_BOOL* bPerfWarnings, CLR_BOOL* bCorrectnessWarnings, CLR_BOOL* bSafeHandleStackTraces)
{
    FCALL_CONTRACT;

    INT32 logFacility = 0;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    *bLoggingEnabled         = (bool)(g_pConfig->GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_LogEnable, 0)!=0);
    *bLogToConsole           = (bool)(g_pConfig->GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_LogToConsole, 0)!=0);
    *iLogLevel               = (INT32)(g_pConfig->GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_LogLevel, 0));
    logFacility              = (INT32)(g_pConfig->GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_ManagedLogFacility, 0));
    *bPerfWarnings           = (bool)(g_pConfig->GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_BCLPerfWarnings, 0)!=0);
    *bCorrectnessWarnings    = (bool)(g_pConfig->GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_BCLCorrectnessWarnings, 0)!=0);
    *bSafeHandleStackTraces  = (bool)(g_pConfig->GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_SafeHandleStackTraces, 0)!=0);

    HELPER_METHOD_FRAME_END();                              \

    return logFacility;
}
FCIMPLEND

// Return true if the valuetype does not contain pointer and is tightly packed
FCIMPL1(FC_BOOL_RET, ValueTypeHelper::CanCompareBits, Object* obj)
{
    FCALL_CONTRACT;

    _ASSERTE(obj != NULL);
    MethodTable* mt = obj->GetMethodTable();
    FC_RETURN_BOOL(!mt->ContainsPointers() && !mt->IsNotTightlyPacked());
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, ValueTypeHelper::FastEqualsCheck, Object* obj1, Object* obj2)
{
    FCALL_CONTRACT;

    _ASSERTE(obj1 != NULL);
    _ASSERTE(obj2 != NULL);
    _ASSERTE(!obj1->GetMethodTable()->ContainsPointers());
    _ASSERTE(obj1->GetSize() == obj2->GetSize());

    TypeHandle pTh = obj1->GetTypeHandle();

    FC_RETURN_BOOL(memcmp(obj1->GetData(),obj2->GetData(),pTh.GetSize()) == 0);
}
FCIMPLEND

static BOOL CanUseFastGetHashCodeHelper(MethodTable *mt)
{
    LIMITED_METHOD_CONTRACT;
    return !mt->ContainsPointers() && !mt->IsNotTightlyPacked();
}

static INT32 FastGetValueTypeHashCodeHelper(MethodTable *mt, void *pObjRef)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
        PRECONDITION(CanUseFastGetHashCodeHelper(mt));
    } CONTRACTL_END;

    INT32 hashCode = 0;
    INT32 *pObj = (INT32*)pObjRef;
            
    // this is a struct with no refs and no "strange" offsets, just go through the obj and xor the bits
    INT32 size = mt->GetNumInstanceFieldBytes();
    for (INT32 i = 0; i < (INT32)(size / sizeof(INT32)); i++)
        hashCode ^= *pObj++;

    return hashCode;
}

static INT32 RegularGetValueTypeHashCode(MethodTable *mt, void *pObjRef)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    INT32 hashCode = 0;
    INT32 *pObj = (INT32*)pObjRef;

    // While we shouln't get here directly from ValueTypeHelper::GetHashCode, if we recurse we need to 
    // be able to handle getting the hashcode for an embedded structure whose hashcode is computed by the fast path.
    if (CanUseFastGetHashCodeHelper(mt))
    {
        return FastGetValueTypeHashCodeHelper(mt, pObjRef);
    }
    else
    {
        // it's looking ugly so we'll use the old behavior in managed code. Grab the first non-null
        // field and return its hash code or 'it' as hash code
        // <TODO> Note that the old behavior has already been broken for value types
        //              that is qualified for CanUseFastGetHashCodeHelper. So maybe we should
        //              change the implementation here to use all fields instead of just the 1st one.
        // </TODO>
        //
        // <TODO> check this approximation - we may be losing exact type information </TODO>
        ApproxFieldDescIterator fdIterator(mt, ApproxFieldDescIterator::INSTANCE_FIELDS);
        INT32 count = (INT32)fdIterator.Count();

        if (count != 0)
        {
            for (INT32 i = 0; i < count; i++)
            {
                FieldDesc *field = fdIterator.Next();
                _ASSERTE(!field->IsRVA());
                void *pFieldValue = (BYTE *)pObj + field->GetOffsetUnsafe();
                if (field->IsObjRef())
                {
                    // if we get an object reference we get the hash code out of that
                    if (*(Object**)pFieldValue != NULL)
                    {

                        OBJECTREF fieldObjRef = ObjectToOBJECTREF(*(Object **) pFieldValue);
                        GCPROTECT_BEGIN(fieldObjRef);

                        MethodDescCallSite getHashCode(METHOD__OBJECT__GET_HASH_CODE, &fieldObjRef);

                        // Make the call.
                        ARG_SLOT arg[1] = {ObjToArgSlot(fieldObjRef)};
                        hashCode = getHashCode.Call_RetI4(arg);

                        GCPROTECT_END();
                    }
                    else
                    {
                        // null object reference, try next
                        continue;
                    }
                }
                else
                {
                    UINT fieldSize = field->LoadSize();
                    INT32 *pValue = (INT32*)pFieldValue;
                    CorElementType fieldType = field->GetFieldType();
                    if (fieldType != ELEMENT_TYPE_VALUETYPE)
                    {
                        for (INT32 j = 0; j < (INT32)(fieldSize / sizeof(INT32)); j++)
                            hashCode ^= *pValue++;
                    }
                    else
                    {
                        // got another value type. Get the type
                        TypeHandle fieldTH = field->LookupFieldTypeHandle(); // the type was loaded already
                        _ASSERTE(!fieldTH.IsNull());
                        hashCode = RegularGetValueTypeHashCode(fieldTH.GetMethodTable(), pValue);
                    }
                }
                break;
            }
        }
    }
    return hashCode;
}

// The default implementation of GetHashCode() for all value types.
// Note that this implementation reveals the value of the fields.
// So if the value type contains any sensitive information it should
// implement its own GetHashCode().
FCIMPL1(INT32, ValueTypeHelper::GetHashCode, Object* objUNSAFE)
{
    FCALL_CONTRACT;

    if (objUNSAFE == NULL)
        FCThrow(kNullReferenceException);

    OBJECTREF obj = ObjectToOBJECTREF(objUNSAFE);
    VALIDATEOBJECTREF(obj);

    INT32 hashCode = 0;
    MethodTable *pMT = objUNSAFE->GetMethodTable();

    // We don't want to expose the method table pointer in the hash code
    // Let's use the typeID instead.
    UINT32 typeID = pMT->LookupTypeID();
    if (typeID == TypeIDProvider::INVALID_TYPE_ID)
    {
        // If the typeID has yet to be generated, fall back to GetTypeID
        // This only needs to be done once per MethodTable
        HELPER_METHOD_FRAME_BEGIN_RET_1(obj);        
        typeID = pMT->GetTypeID();
        HELPER_METHOD_FRAME_END();
    }

    // To get less colliding and more evenly distributed hash codes,
    // we munge the class index with two big prime numbers
    hashCode = typeID * 711650207 + 2506965631U;

    if (CanUseFastGetHashCodeHelper(pMT))
    {
        hashCode ^= FastGetValueTypeHashCodeHelper(pMT, obj->UnBox());
    }
    else
    {
        HELPER_METHOD_FRAME_BEGIN_RET_1(obj);        
        hashCode ^= RegularGetValueTypeHashCode(pMT, obj->UnBox());
        HELPER_METHOD_FRAME_END();
    }
    
    return hashCode;
}
FCIMPLEND

static LONG s_dwSeed;

FCIMPL1(INT32, ValueTypeHelper::GetHashCodeOfPtr, LPVOID ptr)
{
    FCALL_CONTRACT;

    INT32 hashCode = (INT32)((INT64)(ptr));

    if (hashCode == 0)
    {
        return 0;
    }

    DWORD dwSeed = s_dwSeed;

    // Initialize s_dwSeed lazily
    if (dwSeed == 0)
    {
        // We use the first non-0 pointer as the seed, all hashcodes will be based off that.
        // This is to make sure that we only reveal relative memory addresses and never absolute ones.
        dwSeed = hashCode;
        InterlockedCompareExchange(&s_dwSeed, dwSeed, 0);
        dwSeed = s_dwSeed;
    }
    _ASSERTE(dwSeed != 0);

    return hashCode - dwSeed;
}
FCIMPLEND

#ifndef FEATURE_CORECLR
FCIMPL1(OBJECTHANDLE, SizedRefHandle::Initialize, Object* _obj)
{
    FCALL_CONTRACT;

    OBJECTHANDLE result = 0; 
    OBJECTREF obj(_obj);

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    result = GetAppDomain()->CreateSizedRefHandle(obj);

    HELPER_METHOD_FRAME_END();

    return result;
}
FCIMPLEND

FCIMPL1(VOID, SizedRefHandle::Free, OBJECTHANDLE handle)
{
    FCALL_CONTRACT;

    _ASSERTE(handle != NULL);

    HELPER_METHOD_FRAME_BEGIN_0();

    DestroySizedRefHandle(handle);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL1(LPVOID, SizedRefHandle::GetTarget, OBJECTHANDLE handle)
{
    FCALL_CONTRACT;

    _ASSERTE(handle != NULL);

    OBJECTREF objRef = NULL;

    objRef = ObjectFromHandle(handle);

    FCUnique(0x33);
    return *((LPVOID*)&objRef);
}
FCIMPLEND

FCIMPL1(INT64, SizedRefHandle::GetApproximateSize, OBJECTHANDLE handle)
{
    FCALL_CONTRACT;

    _ASSERTE(handle != NULL);

    return (INT64)HndGetHandleExtraInfo(handle);
}
FCIMPLEND
#endif //!FEATURE_CORECLR

#ifdef FEATURE_CORECLR
COMNlsHashProvider COMNlsHashProvider::s_NlsHashProvider;
#endif // FEATURE_CORECLR


COMNlsHashProvider::COMNlsHashProvider()
{
    LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_RANDOMIZED_STRING_HASHING
    bUseRandomHashing = FALSE;
    pEntropy = NULL;
    pDefaultSeed = NULL;
#endif // FEATURE_RANDOMIZED_STRING_HASHING
}

INT32 COMNlsHashProvider::HashString(LPCWSTR szStr, SIZE_T strLen, BOOL forceRandomHashing, INT64 additionalEntropy)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef FEATURE_RANDOMIZED_STRING_HASHING
   _ASSERTE(forceRandomHashing == false);
   _ASSERTE(additionalEntropy == 0);
#endif

#ifdef FEATURE_RANDOMIZED_STRING_HASHING
    if(bUseRandomHashing || forceRandomHashing)
    {
        int marvinResult[SYMCRYPT_MARVIN32_RESULT_SIZE / sizeof(int)];
        
        if(additionalEntropy == 0)
        {
            SymCryptMarvin32(GetDefaultSeed(), (PCBYTE) szStr, strLen * sizeof(WCHAR), (PBYTE) &marvinResult);
        }
        else
        {
            SYMCRYPT_MARVIN32_EXPANDED_SEED seed;
            CreateMarvin32Seed(additionalEntropy, &seed);
            SymCryptMarvin32(&seed, (PCBYTE) szStr, strLen * sizeof(WCHAR), (PBYTE) &marvinResult);
        }

        return marvinResult[0] ^ marvinResult[1];
    }
    else
    {
#endif // FEATURE_RANDOMIZED_STRING_HASHING
        return ::HashString(szStr);
#ifdef FEATURE_RANDOMIZED_STRING_HASHING
    }
#endif // FEATURE_RANDOMIZED_STRING_HASHING
}


INT32 COMNlsHashProvider::HashSortKey(PCBYTE pSrc, SIZE_T cbSrc, BOOL forceRandomHashing, INT64 additionalEntropy)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef FEATURE_RANDOMIZED_STRING_HASHING
   _ASSERTE(forceRandomHashing == false);
   _ASSERTE(additionalEntropy == 0);
#endif

#ifdef FEATURE_RANDOMIZED_STRING_HASHING
    if(bUseRandomHashing || forceRandomHashing)
    {
        int marvinResult[SYMCRYPT_MARVIN32_RESULT_SIZE / sizeof(int)];
        
        // Sort Keys are terminated with a null byte which we didn't hash using the old algorithm, 
        // so we don't have it with Marvin32 either.
        if(additionalEntropy == 0)
        {
            SymCryptMarvin32(GetDefaultSeed(), pSrc, cbSrc - 1, (PBYTE) &marvinResult);
        }
        else
        {
            SYMCRYPT_MARVIN32_EXPANDED_SEED seed;       
            CreateMarvin32Seed(additionalEntropy, &seed);
            SymCryptMarvin32(&seed, pSrc, cbSrc - 1, (PBYTE) &marvinResult);
        }
 
        return marvinResult[0] ^ marvinResult[1];
    }
    else
    {
#endif // FEATURE_RANDOMIZED_STRING_HASHING 
        // Ok, lets build the hashcode -- mostly lifted from GetHashCode() in String.cs, for strings.
        int hash1 = 5381;
        int hash2 = hash1;
        const BYTE *pB = pSrc;
        BYTE    c;

        while (pB != 0 && *pB != 0) {
            hash1 = ((hash1 << 5) + hash1) ^ *pB;
            c = pB[1];

            //
            // FUTURE: Update NewAPis::LCMapStringEx to perhaps use a different, bug free, Win32 API on Win2k3 to workaround the issue discussed below.
            //
            // On Win2k3 Server, LCMapStringEx(LCMAP_SORTKEY) output does not correspond to CompareString in all cases, breaking the .NET GetHashCode<->Equality Contract
            // Due to a fluke in our GetHashCode method, we avoided this issue due to the break out of the loop on the binary-zero byte.
            //
            if (c == 0)
                break;

            hash2 = ((hash2 << 5) + hash2) ^ c;
            pB += 2;
        }

        return hash1 + (hash2 * 1566083941);

#ifdef FEATURE_RANDOMIZED_STRING_HASHING
    }
#endif // FEATURE_RANDOMIZED_STRING_HASHING

}

INT32 COMNlsHashProvider::HashiStringKnownLower80(LPCWSTR szStr, INT32 strLen, BOOL forceRandomHashing, INT64 additionalEntropy)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef FEATURE_RANDOMIZED_STRING_HASHING
   _ASSERTE(forceRandomHashing == false);
   _ASSERTE(additionalEntropy == 0);
#endif

#ifdef FEATURE_RANDOMIZED_STRING_HASHING
    if(bUseRandomHashing || forceRandomHashing)
    {
        WCHAR buf[SYMCRYPT_MARVIN32_INPUT_BLOCK_SIZE * 8];
        SYMCRYPT_MARVIN32_STATE marvinState;
        SYMCRYPT_MARVIN32_EXPANDED_SEED seed;

        if(additionalEntropy == 0)
        {
            SymCryptMarvin32Init(&marvinState, GetDefaultSeed());
        }
        else
        {
            CreateMarvin32Seed(additionalEntropy, &seed);
            SymCryptMarvin32Init(&marvinState, &seed);
        }

        LPCWSTR szEnd = szStr + strLen;

        const UINT A_TO_Z_RANGE = (UINT)('z' - 'a');

        while (szStr != szEnd)
        {
            size_t count = (sizeof(buf) / sizeof(buf[0]));

            if ((size_t)(szEnd - szStr) < count)
                count = (size_t)(szEnd - szStr);

            for (size_t i = 0; i<count; i++)
            {
                WCHAR c = szStr[i];

                if ((UINT)(c - 'a') <= A_TO_Z_RANGE)  // if (c >='a' && c <= 'z') 
                {
                   //If we have a lowercase character, ANDing off 0x20
                   // will make it an uppercase character.
                   c &= ~0x20;
                }

                buf[i] = c;
            }

            szStr += count;

            SymCryptMarvin32Append(&marvinState, (PCBYTE) &buf, sizeof(WCHAR) * count);
        }

        int marvinResult[SYMCRYPT_MARVIN32_RESULT_SIZE / sizeof(int)];
        SymCryptMarvin32Result(&marvinState, (PBYTE) &marvinResult);
        return marvinResult[0] ^ marvinResult[1];
    }
    else
    {
#endif // FEATURE_RANDOMIZED_STRING_HASHING
        return ::HashiStringKnownLower80(szStr);
#ifdef FEATURE_RANDOMIZED_STRING_HASHING
    }
#endif // FEATURE_RANDOMIZED_STRING_HASHING
}


#ifdef FEATURE_RANDOMIZED_STRING_HASHING
void COMNlsHashProvider::InitializeDefaultSeed()
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    PCBYTE pEntropy = GetEntropy();
    AllocMemHolder<SYMCRYPT_MARVIN32_EXPANDED_SEED> pSeed(GetAppDomain()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(SYMCRYPT_MARVIN32_EXPANDED_SEED))));
    SymCryptMarvin32ExpandSeed(pSeed, pEntropy, SYMCRYPT_MARVIN32_SEED_SIZE);

    if(InterlockedCompareExchangeT(&pDefaultSeed, (PCSYMCRYPT_MARVIN32_EXPANDED_SEED) pSeed, NULL) == NULL)
    {
        pSeed.SuppressRelease();
    }
}

PCSYMCRYPT_MARVIN32_EXPANDED_SEED COMNlsHashProvider::GetDefaultSeed()
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(pDefaultSeed == NULL)
    {
        InitializeDefaultSeed();
    }

    return pDefaultSeed;
}

PCBYTE COMNlsHashProvider::GetEntropy()
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(pEntropy == NULL)
    {
        AllocMemHolder<BYTE> pNewEntropy(GetAppDomain()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(SYMCRYPT_MARVIN32_SEED_SIZE))));

#ifdef FEATURE_PAL
        PAL_Random(TRUE, pNewEntropy, SYMCRYPT_MARVIN32_SEED_SIZE);
#else
        HCRYPTPROV hCryptProv;
        WszCryptAcquireContext(&hCryptProv, NULL, NULL, PROV_RSA_FULL, CRYPT_VERIFYCONTEXT);
        CryptGenRandom(hCryptProv, SYMCRYPT_MARVIN32_SEED_SIZE, pNewEntropy);
        CryptReleaseContext(hCryptProv, 0);
#endif

        if(InterlockedCompareExchangeT(&pEntropy, (PBYTE) pNewEntropy, NULL) == NULL)
        {
            pNewEntropy.SuppressRelease();
        }
    }
 
    return (PCBYTE) pEntropy;
}


void COMNlsHashProvider::CreateMarvin32Seed(INT64 additionalEntropy, PSYMCRYPT_MARVIN32_EXPANDED_SEED pExpandedMarvinSeed)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    INT64 *pEntropy = (INT64*) GetEntropy();
    INT64 entropy;

    entropy = *pEntropy ^ additionalEntropy;

    SymCryptMarvin32ExpandSeed(pExpandedMarvinSeed, (PCBYTE) &entropy, SYMCRYPT_MARVIN32_SEED_SIZE);
}
#endif // FEATURE_RANDOMIZED_STRING_HASHING

#ifdef FEATURE_COREFX_GLOBALIZATION
INT32 QCALLTYPE CoreFxGlobalization::HashSortKey(PCBYTE pSortKey, INT32 cbSortKey, BOOL forceRandomizedHashing, INT64 additionalEntropy)
{
    QCALL_CONTRACT;

    INT32 retVal = 0;

    BEGIN_QCALL;

    retVal = COMNlsHashProvider::s_NlsHashProvider.HashSortKey(pSortKey, cbSortKey, forceRandomizedHashing, additionalEntropy);

    END_QCALL;

    return retVal;
}
#endif //FEATURE_COREFX_GLOBALIZATION

static MethodTable * g_pStreamMT;
static WORD g_slotBeginRead, g_slotEndRead;
static WORD g_slotBeginWrite, g_slotEndWrite;

static bool HasOverriddenStreamMethod(MethodTable * pMT, WORD slot)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    } CONTRACTL_END;

    PCODE actual = pMT->GetRestoredSlot(slot);
    PCODE base = g_pStreamMT->GetRestoredSlot(slot);
    if (actual == base)
        return false;

    if (!g_pStreamMT->IsZapped())
    {
        // If mscorlib is JITed, the slots can be patched and thus we need to compare the actual MethodDescs 
        // to detect match reliably
        if (MethodTable::GetMethodDescForSlotAddress(actual) == MethodTable::GetMethodDescForSlotAddress(base))
            return false;
    }

    return true;
}

FCIMPL1(FC_BOOL_RET, StreamNative::HasOverriddenBeginEndRead, Object *stream)
{
    FCALL_CONTRACT;

    if (stream == NULL)
        FC_RETURN_BOOL(TRUE);

    if (g_pStreamMT == NULL || g_slotBeginRead == 0 || g_slotEndRead == 0)
    {
        HELPER_METHOD_FRAME_BEGIN_RET_1(stream);
        g_pStreamMT = MscorlibBinder::GetClass(CLASS__STREAM);
        g_slotBeginRead = MscorlibBinder::GetMethod(METHOD__STREAM__BEGIN_READ)->GetSlot();
        g_slotEndRead = MscorlibBinder::GetMethod(METHOD__STREAM__END_READ)->GetSlot();
        HELPER_METHOD_FRAME_END();
    }

    MethodTable * pMT = stream->GetMethodTable();

    FC_RETURN_BOOL(HasOverriddenStreamMethod(pMT, g_slotBeginRead) || HasOverriddenStreamMethod(pMT, g_slotEndRead));
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, StreamNative::HasOverriddenBeginEndWrite, Object *stream)
{
    FCALL_CONTRACT;

    if (stream == NULL) 
        FC_RETURN_BOOL(TRUE);

    if (g_pStreamMT == NULL || g_slotBeginWrite == 0 || g_slotEndWrite == 0)
    {
        HELPER_METHOD_FRAME_BEGIN_RET_1(stream);
        g_pStreamMT = MscorlibBinder::GetClass(CLASS__STREAM);
        g_slotBeginWrite = MscorlibBinder::GetMethod(METHOD__STREAM__BEGIN_WRITE)->GetSlot();
        g_slotEndWrite = MscorlibBinder::GetMethod(METHOD__STREAM__END_WRITE)->GetSlot();
        HELPER_METHOD_FRAME_END();
    }

    MethodTable * pMT = stream->GetMethodTable();

    FC_RETURN_BOOL(HasOverriddenStreamMethod(pMT, g_slotBeginWrite) || HasOverriddenStreamMethod(pMT, g_slotEndWrite));
}
FCIMPLEND
