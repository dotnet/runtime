// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>

struct test1 {
    LONG64 a;
    LONG64 b;
};

extern "C" DLL_EXPORT LONG64 STDMETHODCALLTYPE PassLayout(test1* i) {
    printf("PassLayout: i->a  = %lld\n", i->a);
    printf("PassLayout: i->b = %lld\n", i->b);
    return i->b;
}


struct AsAnyField
{
    int * intArray;
};


extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassUnicodeStr(LPCWSTR str)
{
	return (SHORT)str[0] == 0x0030 && (SHORT)str[1] == 0x7777 && (SHORT)str[2] == 0x000A;		
}
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassAnsiStr(LPCSTR str , BOOL isIncludeUnMappableChar)
{
	if(isIncludeUnMappableChar)
		return (BYTE)str[0] == 0x30 && (BYTE)str[1] == 0x3f && (BYTE)str[2] == 0x0A;
	else
		return (BYTE)str[0] == 0x30 && (BYTE)str[1] == 0x35 && (BYTE)str[2] == 0x0A;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassUnicodeStrbd(LPCWSTR str)
{
	return (SHORT)str[0] == 0x0030 && (SHORT)str[1] == 0x7777 && (SHORT)str[2] == 0x000A;			
}
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassAnsiStrbd(LPCSTR str , BOOL isIncludeUnMappableChar)
{
	if(isIncludeUnMappableChar)
		return (BYTE)str[0] == 0x30 && (BYTE)str[1] == 0x3f && (BYTE)str[2] == 0x0A;
	else
		return (BYTE)str[0] == 0x30 && (BYTE)str[1] == 0x35 && (BYTE)str[2] == 0x0A;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassUnicodeCharArray(WCHAR CharArray_In [], WCHAR CharArray_InOut [], WCHAR CharArray_Out [])
{
	BOOL ret = FALSE;
	ret = (SHORT)CharArray_In[0] == 0x0030 && (SHORT)CharArray_In[1] == 0x7777 && (SHORT)CharArray_In[2] == 0x000A 
		&& (SHORT)CharArray_InOut[0] == 0x0030 && (SHORT)CharArray_InOut[1] == 0x7777 && (SHORT)CharArray_InOut[2] == 0x000A ;

	// revese the string for passing back
	WCHAR temp = CharArray_InOut[0]; 

	CharArray_InOut[0] = CharArray_InOut[2];
	CharArray_Out[0] = CharArray_InOut[2];
	CharArray_Out[1] = CharArray_InOut[1];
	CharArray_InOut[2] = temp;
	CharArray_Out[2] = temp;
	return ret;
}
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassAnsiCharArray(CHAR CharArray_In [], CHAR CharArray_InOut [], CHAR CharArray_Out [] ,
        BOOL isIncludeUnMappableChar)
{
	BOOL ret = FALSE;
	if(isIncludeUnMappableChar)
		ret = (BYTE)CharArray_In[0] == 0x30 && (BYTE)CharArray_In[1] == 0x3f && (BYTE)CharArray_In[2] == 0x0A 
			&& (BYTE)CharArray_InOut[0] == 0x30 && (BYTE)CharArray_InOut[1] == 0x3f && (BYTE)CharArray_InOut[2] == 0x0A;
	else
		ret = (BYTE)CharArray_In[0] == 0x30 && (BYTE)CharArray_In[1] == 0x35 && (BYTE)CharArray_In[2] == 0x0A 
			&& (BYTE)CharArray_InOut[0] == 0x30 && (BYTE)CharArray_InOut[1] == 0x35 && (BYTE)CharArray_InOut[2] == 0x0A;

	// reverse the string for passing back
	CHAR temp = CharArray_InOut[0]; 

	CharArray_InOut[0] = CharArray_InOut[2];
	CharArray_Out[0] = CharArray_InOut[2];
	CharArray_Out[1] = CharArray_InOut[1];
	CharArray_InOut[2] = temp;
	CharArray_Out[2] = temp;

	return ret;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassArraySbyte(
			BYTE sbyteArray[], BYTE sbyteArray_In[], BYTE sbyteArray_InOut[], BYTE sbyteArray_Out[], BYTE expected[], int len){

		for(int i = 0; i < len; i++)
		{
			if(sbyteArray[i] != expected[i] || sbyteArray_In[i] != expected[i] || sbyteArray_InOut[i] != expected[i])
			{
				printf("Not correct pass in paremeter in PassArraySbyte\n");
				return FALSE;
			}
			sbyteArray_InOut[i] = 10 + expected[i];
			sbyteArray_Out[i] = 10 + expected[i];
		}
		return TRUE;    
}


extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassArrayByte(	
		BYTE byteArray[], BYTE byteArray_In[], BYTE byteArray_InOut[], BYTE byteArray_Out[], BYTE expected[], int len){

		for(int i = 0; i < len; i++)
		{
			if(byteArray[i] != expected[i] || byteArray_In[i] != expected[i] || byteArray_InOut[i] != expected[i])
			{
				printf("Not correct pass in paremeter in PassArrayByte\n");
				return FALSE;
			}
			byteArray_InOut[i] = 10 + expected[i];
			byteArray_Out[i] = 10 + expected[i];
		}
		return TRUE;    
}
    
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassArrayShort(	
		SHORT shortArray[], SHORT shortArray_In[], SHORT shortArray_InOut[], SHORT shortArray_Out[], SHORT expected[], int len){

		for(int i = 0; i < len; i++)
		{
			if(shortArray[i] != expected[i] || shortArray_In[i] != expected[i] || shortArray_InOut[i] != expected[i])
			{
				printf("Not correct pass in paremeter in PassArrayShort\n");
				return FALSE;
			}
			shortArray_InOut[i] = 10 + expected[i];
			shortArray_Out[i] = 10 + expected[i];
		}
		return TRUE;    
}
    
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassArrayUshort(	
		USHORT ushortArray[], USHORT ushortArray_In[], USHORT ushortArray_InOut[], USHORT ushortArray_Out[], USHORT expected[], int len){

		for(int i = 0; i < len; i++)
		{
			if(ushortArray[i] != expected[i] || ushortArray_In[i] != expected[i] || ushortArray_InOut[i] != expected[i])
			{
				printf("Not correct pass in paremeter in PassArrayUshort\n");
				return FALSE;
			}
			ushortArray_InOut[i] = 10 + expected[i];
			ushortArray_Out[i] = 10 + expected[i];
		}
		return TRUE;    
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassArrayInt(	
		int IntArray[], int IntArray_In[], int IntArray_InOut[], int IntArray_Out[], int expected[], int len){

		for(int i = 0; i < len; i++)
		{
			if(IntArray[i] != expected[i] || IntArray_In[i] != expected[i] || IntArray_InOut[i] != expected[i])
			{
				printf("Not correct pass in paremeter in PassArrayInt\n");
				return FALSE;
			}
			IntArray_InOut[i] = 10 + expected[i];
			IntArray_Out[i] = 10 + expected[i];
		}
		return TRUE;    
}


extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassArrayUint(	
		UINT uintArray[], UINT uintArray_In[], UINT uintArray_InOut[], UINT uintArray_Out[], UINT expected[], int len){

		for(int i = 0; i < len; i++)
		{
			if(uintArray[i] != expected[i] || uintArray_In[i] != expected[i] || uintArray_InOut[i] != expected[i])
			{
				printf("Not correct pass in paremeter in PassArrayUint\n");
				return FALSE;
			}
			uintArray_InOut[i] = 10 + expected[i];
			uintArray_Out[i] = 10 + expected[i];
		}
		return TRUE;    
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassArrayLong(	
		LONG64 longArray[], LONG64 longArray_In[], LONG64 longArray_InOut[], LONG64 longArray_Out[], LONG64 expected[], int len){

		for(int i = 0; i < len; i++)
		{
			if(longArray[i] != expected[i] || longArray_In[i] != expected[i] || longArray_InOut[i] != expected[i])
			{
				printf("Not correct pass in paremeter in PassArrayLong\n");
				return FALSE;
			}
			longArray_InOut[i] = 10 + expected[i];
			longArray_Out[i] = 10 + expected[i];
		}
		return TRUE;    
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassArrayUlong(	
	LONG64 ulongArray[], LONG64 ulongArray_In[], LONG64 ulongArray_InOut[], 
	LONG64 ulongArray_Out[], LONG64 expected[], int len){

		for(int i = 0; i < len; i++)
		{
			if(ulongArray[i] != expected[i] || ulongArray_In[i] != expected[i] || ulongArray_InOut[i] != expected[i])
			{
				printf("Not correct pass in paremeter in PassArrayUlong\n");
				return FALSE;
			}
			ulongArray_InOut[i] = 10 + expected[i];
			ulongArray_Out[i] = 10 + expected[i];
		}
		return TRUE;  
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassArraySingle(	
	float singleArray[], float singleArray_In[], float singleArray_InOut[], 
	float singleArray_Out[], float expected[], int len){

		for(int i = 0; i < len; i++)
		{
			if(singleArray[i] != expected[i] || singleArray_In[i] != expected[i] || singleArray_InOut[i] != expected[i])
			{
				printf("Not correct pass in paremeter in PassArraySingle\n");
				return FALSE;
			}
			singleArray_InOut[i] = 10 + expected[i];
			singleArray_Out[i] = 10 + expected[i];
		}
		return TRUE;  
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassArrayDouble(	
	double doubleArray[], double doubleArray_In[], double doubleArray_InOut[], double doubleArray_Out[], double expected[], int len){

		for(int i = 0; i < len; i++)
		{
			if(doubleArray[i] != expected[i] || doubleArray_In[i] != expected[i] || doubleArray_InOut[i] != expected[i])
			{
				printf("Not correct pass in paremeter in PassArrayDouble\n");
				return FALSE;
			}
			doubleArray_InOut[i] = 10 + expected[i];
			doubleArray_Out[i] = 10 + expected[i];
		}
		return TRUE;  
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassArrayChar(	
	CHAR charArray[], CHAR charArray_In[], CHAR charArray_InOut[], CHAR charArray_Out[], CHAR expected[], int len){
		for(int i = 0; i < len; i++)
		{
			if(charArray[i] != expected[i] || charArray_In[i] != expected[i] || charArray_InOut[i] != expected[i])
			{
				printf("Not correct pass in paremeter in PassArrayChar\n");
				return FALSE;
			}			
		}
		
		charArray_InOut[0] = 100;
		charArray_Out[0] = 100;
		charArray_InOut[1] = 101;
		charArray_Out[1] = 101;
		charArray_InOut[2] = 102;
		charArray_Out[2] = 102;

		return TRUE;  
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassArrayBool(	
	BOOL boolArray[], BOOL boolArray_In[], BOOL boolArray_InOut[], BOOL boolArray_Out[], BOOL expected[], int len){

		for(int i = 0; i < len; i++)
		{
			if(boolArray[i] != expected[i] || boolArray_In[i] != expected[i] || boolArray_InOut[i] != expected[i])
			{
				printf("Not correct pass in paremeter in PassArrayBool\n");
				return FALSE;
			}			
		}
		
		boolArray_InOut[0] = FALSE;
		boolArray_Out[0] = FALSE;
		boolArray_InOut[1] = TRUE;
		boolArray_Out[1] = TRUE;
		boolArray_InOut[2] = TRUE;
		boolArray_Out[2] = TRUE;

		return TRUE;  
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassArrayIntPtr(	
	INT_PTR intPtrArray[], INT_PTR intPtrArray_In[], INT_PTR intPtrArray_InOut[], INT_PTR intPtrArray_Out[], INT_PTR expected[], int len){

		for(int i = 0; i < len; i++)
		{
			if(intPtrArray[i] != expected[i] || intPtrArray_In[i] != expected[i] || intPtrArray_InOut[i] != expected[i])
			{
				printf("Not correct pass in paremeter in PassArrayIntPtr\n");
				return FALSE;
			}
			intPtrArray_InOut[i] = 10 + expected[i];
			intPtrArray_Out[i] = 10 + expected[i];
		}
		return TRUE;  
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassArrayUIntPtr(	
	UINT_PTR uIntPtrArray[], UINT_PTR uIntPtrArray_In[], UINT_PTR uIntPtrArray_InOut[], UINT_PTR uIntPtrArray_Out[], UINT_PTR expected[], int len){

		for(int i = 0; i < len; i++)
		{
			if(uIntPtrArray[i] != expected[i] || uIntPtrArray_In[i] != expected[i] || uIntPtrArray_InOut[i] != expected[i])
			{
				printf("Not correct pass in paremeter in PassArrayUIntPtr\n");
				return FALSE;
			}
			uIntPtrArray_InOut[i] = 10 + expected[i];
			uIntPtrArray_Out[i] = 10 + expected[i];
		}
		return TRUE;  
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE PassMixStruct(AsAnyField mix){
		return TRUE;  
}
