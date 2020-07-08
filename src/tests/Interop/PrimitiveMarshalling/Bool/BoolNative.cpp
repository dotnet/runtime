// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>
#include <platformdefines.h>
#include <stdio.h>


BOOL boolManaged = true;
BOOL boolNative = false;

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Marshal_In(/*[in]*/BOOL boolValue)
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

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Marshal_InOut(/*[In,Out]*/BOOL boolValue)
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

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Marshal_Out(/*[Out]*/BOOL boolValue)
{
	//In-Place Change
	boolValue = boolNative;

	//Return
	return true;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalPointer_In(/*[in]*/BOOL *pboolValue)
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

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalPointer_InOut(/*[in,out]*/BOOL *pboolValue)
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

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MarshalPointer_Out(/*[out]*/ BOOL *pboolValue)
{
	//In-Place Change
	*pboolValue = boolNative;

	//Return
	return true;
}

#pragma warning(push)
#if _MSC_VER <= 1900
// 'BOOL' forcing value to bool 'true' or 'false'
#pragma warning(disable: 4800)
#endif

extern "C" DLL_EXPORT bool STDMETHODCALLTYPE Marshal_As_In(/*[in]*/bool boolValue)
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

extern "C" DLL_EXPORT bool STDMETHODCALLTYPE Marshal_As_InOut(/*[In,Out]*/bool boolValue)
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

extern "C" DLL_EXPORT bool STDMETHODCALLTYPE Marshal_As_Out(/*[Out]*/bool boolValue)
{
	//In-Place Change
	boolValue = (bool)boolNative;

	//Return
	return true;
}

#ifdef _WIN32
extern "C" DLL_EXPORT bool STDMETHODCALLTYPE Marshal_ByValue_Variant(VARIANT_BOOL boolValue, bool expected)
{
    if (boolValue != (expected ? VARIANT_TRUE : VARIANT_FALSE))
    {
        printf("Error in function Marshal_ByValue_Variant(Native Client)\n");

        printf("Expected %s ", expected ? "true" : "false");
        printf("Actual %s (%hi)", boolValue == VARIANT_FALSE ? "false" : "(unknown variant value)", boolValue);

        return false;
    }

    return true;
}

extern "C" DLL_EXPORT bool STDMETHODCALLTYPE Marshal_Ref_Variant(VARIANT_BOOL* pBoolValue)
{
    if (*pBoolValue != (boolManaged ? VARIANT_TRUE : VARIANT_FALSE))
    {
        printf("Error in function Marshal_ByValue_Variant(Native Client)\n");

        printf("Expected %s ", boolManaged ? "true" : "false");
        printf("Actual %s (%hi)", *pBoolValue == VARIANT_FALSE ? "false" : "(unknown variant value)", *pBoolValue);

        return false;
    }

    *pBoolValue = (boolNative ? VARIANT_TRUE : VARIANT_FALSE);
    return true;
}

struct ContainsVariantBool
{
    VARIANT_BOOL value;
};

extern "C" DLL_EXPORT bool STDMETHODCALLTYPE Marshal_ByValue_Struct_Variant(ContainsVariantBool value, bool expected)
{
    if (value.value != (expected ? VARIANT_TRUE : VARIANT_FALSE))
    {
        printf("Error in function Marshal_ByValue_Variant(Native Client)\n");

        printf("Expected %s ", expected ? "true" : "false");
        printf("Actual %s (%hi)", value.value == VARIANT_FALSE ? "false" : "(unknown variant value)", value.value);

        return false;
    }

    return true;
}

extern "C" DLL_EXPORT bool STDMETHODCALLTYPE Marshal_Ref_Struct_Variant(ContainsVariantBool* pBoolValue)
{
    if (pBoolValue->value != (boolManaged ? VARIANT_TRUE : VARIANT_FALSE))
    {
        printf("Error in function Marshal_ByValue_Variant(Native Client)\n");

        printf("Expected %s ", boolManaged ? "true" : "false");
        printf("Actual %s (%hi)", pBoolValue->value == VARIANT_FALSE ? "false" : "(unknown variant value)", pBoolValue->value);

        return false;
    }

    pBoolValue->value = (boolNative ? VARIANT_TRUE : VARIANT_FALSE);
    return true;
}

#endif
#pragma warning(pop)
