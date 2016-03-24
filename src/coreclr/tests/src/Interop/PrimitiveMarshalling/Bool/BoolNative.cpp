// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>
#include <stdio.h>


BOOL boolManaged = true;
BOOL boolNative = false;

extern "C" DLL_EXPORT BOOL __stdcall Marshal_In(/*[in]*/BOOL boolValue)
{
	//Check the input
	if(boolValue != boolManaged)
	{
		printf("Error in Function Marshal_In(Native Client)\n");

		//Expected
		printf("Expected: %s", (boolManaged)?"true":"false");

		//Actual
		printf("Actual:  %s", (boolValue)?"true":"false");

		//Return the error value instead if verification failed
		return false;
	}

	return true;
}

extern "C" DLL_EXPORT BOOL __stdcall Marshal_InOut(/*[In,Out]*/BOOL boolValue)
{
	//Check the input
	if(boolValue != boolManaged)
	{
		printf("Error in Function Marshal_InOut(Native Client)\n");

		//Expected
		printf("Expected: %s", (boolManaged)?"true":"false");

		//Actual
		printf("Actual:  %s", (boolValue)?"true":"false");

		//Return the error value instead if verification failed
		return false;
	}

	//In-Place Change
	boolValue = boolNative;

	//Return
	return true;
}

extern "C" DLL_EXPORT BOOL __stdcall Marshal_Out(/*[Out]*/BOOL boolValue)
{
	//In-Place Change
	boolValue = boolNative;

	//Return
	return true;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalPointer_In(/*[in]*/BOOL *pboolValue)
{
	//Check the input
	if(*pboolValue != boolManaged)
	{
		printf("Error in Function MarshalPointer_In(Native Client)\n");

		//Expected
		printf("Expected: %s", (boolManaged)?"true":"false");

		//Actual
		printf("Actual:  %s", (*pboolValue)?"true":"false");

		//Return the error value instead if verification failed
		return false;
	}

	return true;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalPointer_InOut(/*[in,out]*/BOOL *pboolValue)
{
	//Check the input
	if(*pboolValue != boolManaged)
	{
		printf("Error in Function MarshalPointer_InOut(Native Client)\n");

		//Expected
		printf("Expected: %s", (boolManaged)?"true":"false");

		//Actual
		printf("Actual:  %s", (*pboolValue)?"true":"false");

		//Return the error value instead if verification failed
		return false;
	}

	//In-Place Change
	*pboolValue = boolNative;

	//Return
	return true;
}

extern "C" DLL_EXPORT BOOL __stdcall MarshalPointer_Out(/*[out]*/ BOOL *pboolValue)
{
	//In-Place Change
	*pboolValue = boolNative;

	//Return
	return true;
}

#pragma warning(push)
// 'BOOL' forcing value to bool 'true' or 'false'
#pragma warning(disable: 4800)

extern "C" DLL_EXPORT bool __stdcall Marshal_As_In(/*[in]*/bool boolValue)
{
	//Check the input
	if(boolValue != (bool)boolManaged)
	{
		printf("Error in Function Marshal_As_In(Native Client)\n");

		//Expected
		printf("Expected: %s", (boolManaged)?"true":"false");

		//Actual
		printf("Actual:  %s", (boolValue)?"true":"false");

		//Return the error value instead if verification failed
		return false;
	}

	return true;
}

extern "C" DLL_EXPORT bool __stdcall Marshal_As_InOut(/*[In,Out]*/bool boolValue)
{
	//Check the input
	if(boolValue != (bool)boolManaged)
	{
		printf("Error in Function Marshal_As_InOut(Native Client)\n");

		//Expected
		printf("Expected: %s", (boolManaged)?"true":"false");

		//Actual
		printf("Actual:  %s", (boolValue)?"true":"false");

		//Return the error value instead if verification failed
		return false;
	}

	//In-Place Change
	boolValue = (bool)boolNative;

	//Return
	return true;
}

extern "C" DLL_EXPORT bool __stdcall Marshal_As_Out(/*[Out]*/bool boolValue)
{
	//In-Place Change
	boolValue = (bool)boolNative;

	//Return
	return true;
}
#pragma warning(pop)
