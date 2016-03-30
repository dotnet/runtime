#include "MarshalStructAsParamDLL.h"

///////////////////////////////////////////////////////////////////////////////////
//							EXPORTED METHODS
///////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByVal(InnerSequential inner)
{
	if(!IsCorrectInnerSequential(&inner))
	{
		printf("\tMarshalStructAsParam_AsSeqByVal: InnerSequential param not as expected\n");
		PrintInnerSequential(&inner,"inner");
		return FALSE;
	}
	ChangeInnerSequential(&inner);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRef(InnerSequential* inner)
{
	if(!IsCorrectInnerSequential(inner))
	{
		printf("\tMarshalStructAsParam_AsSeqByRef: InnerSequential param not as expected\n");
		PrintInnerSequential(inner,"inner");
		return FALSE;
	}
	ChangeInnerSequential(inner);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefIn(InnerSequential* inner)
{
	if(!IsCorrectInnerSequential(inner))
	{
		printf("\tMarshalStructAsParam_AsSeqByRefIn: InnerSequential param not as expected\n");
		PrintInnerSequential(inner,"inner");
		return FALSE;
	}
	ChangeInnerSequential(inner);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByValOut(InnerSequential inner)
{
	if(!IsCorrectInnerSequential(&inner))
	{
		printf("\tMarshalStructAsParam_AsSeqByValOut:NNER param not as expected\n");
		PrintInnerSequential(&inner,"inner");
		return FALSE;
	}
	ChangeInnerSequential(&inner);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefOut(InnerSequential* inner)
{
	ChangeInnerSequential(inner);
	return TRUE;
}

///////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByVal2(InnerArraySequential outer)
{
	if(!IsCorrectInnerArraySequential(&outer))
	{
		printf("\tMarshalStructAsParam_AsSeqByVal2: InnerArraySequential param not as expected\n");
		PrintInnerArraySequential(&outer,"outer");
		return FALSE;
	}
	ChangeInnerArraySequential(&outer);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRef2(InnerArraySequential* outer)
{
	if(!IsCorrectInnerArraySequential(outer))
	{
		printf("\tMarshalStructAsParam_AsSeqByRef2: InnerArraySequential param not as expected\n");
		PrintInnerArraySequential(outer,"outer");
		return FALSE;
	}
	ChangeInnerArraySequential(outer);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefIn2(InnerArraySequential* outer)
{
	if(!IsCorrectInnerArraySequential(outer))
	{
		printf("\tMarshalStructAsParam_AsSeqByRefIn2: InnerArraySequential param not as expected\n");
		PrintInnerArraySequential(outer,"inner");
		return FALSE;
	}
	ChangeInnerArraySequential(outer);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByValOut2(InnerArraySequential outer)
{
	if(!IsCorrectInnerArraySequential(&outer))
	{
		printf("\tMarshalStructAsParam_AsSeqByVal2:InnerArraySequential param not as expected\n");
		PrintInnerArraySequential(&outer,"outer");
		return FALSE;
	}
	for(int i = 0; i < NumArrElements; i++)
	{
		outer.arr[i].f1 = 77;
		outer.arr[i].f2 = 77.0;
	}
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefOut2(InnerArraySequential* outer)
{
	for(int i = 0;i<NumArrElements;i++)
	{
		if(outer->arr[i].f1 != 0 || outer->arr[i].f2 != 0.0)
		{
			printf("\tMarshalStructAsParam_AsSeqByRefOut2: InnerArraySequential param not as expected\n");
			return FALSE;
		}
	}
	ChangeInnerArraySequential(outer);
	return TRUE;
}

////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByVal3(CharSetAnsiSequential str1)
{
	if(!IsCorrectCharSetAnsiSequential(&str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByVal3:strCharStr param not as expected\n");
		PrintCharSetAnsiSequential(&str1,"CharSetAnsiSequential");
		return FALSE;
	}
	ChangeCharSetAnsiSequential(&str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRef3(CharSetAnsiSequential* str1)
{
	if(!IsCorrectCharSetAnsiSequential(str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByRef3:strCharStr param not as expected\n");
		PrintCharSetAnsiSequential(str1,"CharSetAnsiSequential");
		return FALSE;
	}
	ChangeCharSetAnsiSequential(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefIn3(CharSetAnsiSequential* str1)
{
	if(!IsCorrectCharSetAnsiSequential(str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByRefIn3:strCharStr param not as expected\n");
		PrintCharSetAnsiSequential(str1,"CharSetAnsiSequential");
		return FALSE;
	}
	ChangeCharSetAnsiSequential(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByValOut3(CharSetAnsiSequential str1)
{
	if(!IsCorrectCharSetAnsiSequential(&str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByVal3:strCharStr param not as expected\n");
		PrintCharSetAnsiSequential(&str1,"CharSetAnsiSequential");
		return FALSE;
	}
	str1.f2 = 'n';
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefOut3(CharSetAnsiSequential* str1)
{
	char const* strSource = "change string";
	int len = strlen(strSource);
	LPCSTR temp = (LPCSTR)TP_CoTaskMemAlloc((sizeof(char)*len)+1);
	if(temp != NULL)
	{
		TP_CoTaskMemFree((void*)(str1->f1));
		strcpy((char*)temp,strSource);		
		str1->f1 = temp;
		str1->f2 = 'n';
		return TRUE;
	}
	else
	{
		printf("Memory Allocated Failed !");
		return FALSE;
	}
}

////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByVal4(CharSetUnicodeSequential str1)
{
	if(!IsCorrectCharSetUnicodeSequential(&str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByVal4:CharSetUnicodeSequential param not as expected\n");
		PrintCharSetUnicodeSequential(&str1,"CharSetUnicodeSequential");
		return FALSE;
	}
	ChangeCharSetUnicodeSequential(&str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRef4(CharSetUnicodeSequential* str1)
{
	if(!IsCorrectCharSetUnicodeSequential(str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByRef4:strCharStr param not as expected\n");
		PrintCharSetUnicodeSequential(str1,"CharSetUnicodeSequential");
		return FALSE;
	}
	ChangeCharSetUnicodeSequential(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefIn4(CharSetUnicodeSequential* str1)
{
	if(!IsCorrectCharSetUnicodeSequential(str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByRefIn4:strCharStr param not as expected\n");
		PrintCharSetUnicodeSequential(str1,"CharSetUnicodeSequential");
		return FALSE;
	}
	ChangeCharSetUnicodeSequential(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByValOut4(CharSetUnicodeSequential str1)
{
	if(!IsCorrectCharSetUnicodeSequential(&str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByVal4:strCharStrOut2 param not as expected\n");
		PrintCharSetUnicodeSequential(&str1,"CharSetUnicodeSequential");
		return FALSE;
	}
	str1.f2 = L'n';
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefOut4(CharSetUnicodeSequential* str1)
{
	if(str1->f1 != 0 || str1->f2 != 0)
		return false;
	ChangeCharSetUnicodeSequential(str1);
	return true;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByVal6(NumberSequential str1)
{
	if(!IsCorrectNumberSequential(&str1))
	{
		printf("\tManaged to Native failed in MarshalStructAsParam_AsSeqByVal6:NumberSequential param not as expected\n");
		PrintNumberSequential(&str1, "str1");
		return FALSE;
	}
	ChangeNumberSequential(&str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRef6(NumberSequential* str1)
{
	if(!IsCorrectNumberSequential(str1))
	{
		printf("\tManaged to Native failed in MarshalStructAsParam_AsSeqByRef6:NumberSequential param not as expected\n");
		PrintNumberSequential(str1, "str1");
		return FALSE;
	}
	ChangeNumberSequential(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefIn6(NumberSequential* str1)
{
	if(!IsCorrectNumberSequential(str1))
	{
		printf("\tManaged to Native failed in MarshalStructAsParam_AsSeqByRefIn6:NumberSequential param not as expected\n");
		PrintNumberSequential(str1, "str1");
		return FALSE;
	}
	ChangeNumberSequential(str1);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByValOut6(NumberSequential str1)
{
	if(!IsCorrectNumberSequential(&str1))
	{
		printf("\tManaged to Native failed in MarshalStructAsParam_AsSeqByValOut6:NumberSequential param not as expected\n");
		PrintNumberSequential(&str1, "str1");
		return FALSE;
	}
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefOut6(NumberSequential* str1)
{
	ChangeNumberSequential(str1);
	return TRUE;
}

////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByVal7(S3 str1)
{
	if(!IsCorrectS3(&str1))
	{
		printf("\tManaged to Native failed in MarshalStructAsParam_AsSeqByVal7:S3 param not as expected\n");
		PrintS3(&str1, "str1");
		return FALSE;
	}
	ChangeS3(&str1);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRef7(S3* str1)
{
	if(!IsCorrectS3(str1))
	{
		printf("\tManaged to Native failed in MarshalStructAsParam_AsSeqByRef7:S3 param not as expected\n");
		return FALSE;
	}
	ChangeS3(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefIn7(S3* str1)
{
	if(!IsCorrectS3(str1))
	{
		printf("\tManaged to Native failed in MarshalStructAsParam_AsSeqByRef7:S3 param not as expected\n");
		PrintS3(str1, "str1");
		return FALSE;
	}
	ChangeS3(str1);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByValOut7(S3 str1)
{
	if(!IsCorrectS3(&str1))
	{
		printf("\tManaged to Native failed in MarshalStructAsParam_AsSeqByValOut7:S3 param not as expected\n");
		PrintS3(&str1, "str1");
		return FALSE;
	}
	str1.flag = false;
	return TRUE;

}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefOut7(S3* str1)
{
	ChangeS3(str1);
	return TRUE;
}
////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByVal8(S5 str1)
{
	if(!IsCorrectS5(&str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByVal8:S5 param not as expected\n");
		PrintS5(&str1, "str1");
		return FALSE;
	}
	ChangeS5(&str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRef8(S5* str1)
{
	if(!IsCorrectS5(str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByRef8:S5 param not as expected\n");
		PrintS5(str1, "str1");
		return FALSE;
	}
	ChangeS5(str1);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefIn8(S5* str1)
{
	if(!IsCorrectS5(str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByRefIn8:S5 param not as expected\n");
		PrintS5(str1, "str1");
		return FALSE;
	}
	ChangeS5(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefOut8(S5* str1)
{
	ChangeS5(str1);
	return TRUE;
}


/////////////////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByVal9(StringStructSequentialAnsi str1)
{
	if(!IsCorrectStringStructSequentialAnsi(&str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByVal9:StringStructSequentialAnsi param not as expected\n");
		PrintStringStructSequentialAnsi(&str1, "str1");
		return FALSE;
	}
	ChangeStringStructSequentialAnsi(&str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRef9(StringStructSequentialAnsi* str1)
{
	if(!IsCorrectStringStructSequentialAnsi(str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByRef9:StringStructSequentialAnsi param not as expected\n");
		PrintStringStructSequentialAnsi(str1, "str1");
		return FALSE;
	}
	ChangeStringStructSequentialAnsi(str1);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefIn9(StringStructSequentialAnsi* str1)
{
	if(!IsCorrectStringStructSequentialAnsi(str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByRefIn9:StringStructSequentialAnsi param not as expected\n");
		PrintStringStructSequentialAnsi(str1, "str1");
		return FALSE;
	}
	ChangeStringStructSequentialAnsi(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByValOut9(StringStructSequentialAnsi str1)
{
	if(!IsCorrectStringStructSequentialAnsi(&str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByVal9:StringStructSequentialAnsi param not as expected\n");
		PrintStringStructSequentialAnsi(&str1, "str1");
		return FALSE;
	}
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefOut9(StringStructSequentialAnsi* str1)
{
	ChangeStringStructSequentialAnsi(str1);

	return TRUE;
}


/////////////////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByVal10(StringStructSequentialUnicode str1)
{
	if(!IsCorrectStringStructSequentialUnicode(&str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByVal10:StringStructSequentialUnicode param not as expected\n");
		PrintStringStructSequentialUnicode(&str1, "str1");
		return FALSE;
	}
	ChangeStringStructSequentialUnicode(&str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRef10(StringStructSequentialUnicode* str1)
{
	if(!IsCorrectStringStructSequentialUnicode(str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByRef10:StringStructSequentialUnicode param not as expected\n");
		PrintStringStructSequentialUnicode(str1, "str1");
		return FALSE;
	}
	ChangeStringStructSequentialUnicode(str1);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefIn10(StringStructSequentialUnicode* str1)
{
	if(!IsCorrectStringStructSequentialUnicode(str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByRefIn10:StringStructSequentialUnicode param not as expected\n");
		PrintStringStructSequentialUnicode(str1, "str1");
		return FALSE;
	}
	ChangeStringStructSequentialUnicode(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByValOut10(StringStructSequentialUnicode str1)
{
	if(!IsCorrectStringStructSequentialUnicode(&str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByValOut10:StringStructSequentialUnicode param not as expected\n");
		PrintStringStructSequentialUnicode(&str1, "str1");
		return FALSE;
	}
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefOut10(StringStructSequentialUnicode* str1)
{
	ChangeStringStructSequentialUnicode(str1);

	return TRUE;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByVal11(S8 str1)
{
	if(!IsCorrectS8(&str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByVal11:S8 param not as expected\n");
		PrintS8(&str1,"str1");
		return FALSE;
	}
	ChangeS8(&str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRef11(S8* str1)
{
	if(!IsCorrectS8(str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByRef11:S8 param not as expected\n");
		PrintS8(str1,"str1");
		return FALSE;
	}
	ChangeS8(str1);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefIn11(S8* str1)
{
	if(!IsCorrectS8(str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByRefIn11:S8 param not as expected\n");
		PrintS8(str1,"str1");
		return FALSE;
	}
	ChangeS8(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByValOut11(S8 str1)
{
	if(!IsCorrectS8(&str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByValOut11:S8 param not as expected\n");
		PrintS8(&str1,"str1");
		return FALSE;
	}
	str1.i32 = 256;
	str1.ui32 = 256;
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefOut11(S8* str1)
{
	ChangeS8(str1);

	return TRUE;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" void NtestMethod(S9 str1)
{
	printf("\tAction of the delegate");
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByVal12(S9 str1)
{
	if(str1.i32 != 128 ||
		str1.myDelegate1 == NULL)
	{
		return FALSE;
	}
	str1.i32 = 256;
	str1.myDelegate1 = NULL;
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRef12(S9* str1)
{
	if(str1->i32 != 128 ||
		str1->myDelegate1 == NULL)
	{
		return FALSE;
	}
	else
	{
		str1->i32 = 256;
		str1->myDelegate1 = NULL;
	}
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefIn12(S9* str1)
{
	if(str1->i32 != 128 ||
		str1->myDelegate1 == NULL)
	{
		return FALSE;
	}
	else
	{
		str1->i32 = 256;
		str1->myDelegate1 = NULL;
	}
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByValOut12(S9 str1)
{
	if(str1.i32 != 128 ||
		str1.myDelegate1 == NULL)
	{
		return FALSE;
	}
	str1.i32 = 256;
	str1.myDelegate1 = NULL;
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefOut12(S9* str1)
{
	str1->i32 = 256;
	str1->myDelegate1 = NtestMethod;
	return TRUE;
}


///////////////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByVal13(S10 str1)
{
	if(!IsCorrectS10(&str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByVal13:S10 param not as expected\n");
		PrintS10(&str1, "str1");
		return FALSE;
	}
	ChangeS10(&str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRef13(S10* str1)
{
	if(!IsCorrectS10(str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByRef13:S10 param not as expected\n");
		PrintS10(str1, "str1");
		return FALSE;
	}
	ChangeS10(str1);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefIn13(S10* str1)
{
	if(!IsCorrectS10(str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByRefIn13:S10 param not as expected\n");
		PrintS10(str1, "str1");
		return FALSE;
	}
	ChangeS10(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByValOut13(S10 str1)
{
	if(!IsCorrectS10(&str1))
	{
		printf("\tMarshalStructAsParam_AsSeqByValOut13:S10 param not as expected\n");
		PrintS10(&str1, "str1");
		return FALSE;
	}
	str1.s.i = 64;
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefOut13(S10* str1)
{
	ChangeS10(str1);

	return TRUE;
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByVal14(S11 str1)
{
	if( str1.i32 != 0 || str1.i != 32 )
		return FALSE;
	str1.i32 = (LPINT)(long)(str1.i);
	str1.i = 64;
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRef14(S11* str1)
{
	if(str1->i32 != 0 || str1->i != 32)
		return FALSE;
	else
	{
		str1->i32 = (LPINT)(long)(str1->i);
		str1->i = 64;
		return TRUE;
	}
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefIn14(S11* str1)
{
	if(str1->i32 != 0 || str1->i != 32)
		return FALSE;
	else
	{
		str1->i32 = (LPINT)(long)(str1->i);
		str1->i = 64;
		return TRUE;
	}
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByValOut14(S11 str1)
{
	if( str1.i32 != (LPINT)32 || str1.i != 32 )
		return FALSE;
	str1.i = 64;
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsSeqByRefOut14(S11* str1)
{
	str1->i32 = (LPINT)(long)(str1->i);
	str1->i = 64;
	return TRUE;
}
//////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByValINNER2(INNER2 inner)
{
	if(!IsCorrectINNER2(&inner))
	{
		printf("\tMarshalStructAsParam_AsSeqByVal: INNER param not as expected\n");
		PrintINNER2(&inner,"inner");
		return FALSE;
	}
	ChangeINNER2(&inner);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefINNER2(INNER2* inner)
{
	if(!IsCorrectINNER2(inner))
	{
		printf("\tMarshalStructAsParam_AsSeqByRef: INNER param not as expected\n");
		PrintINNER2(inner,"inner");
		return FALSE;
	}
	ChangeINNER2(inner);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefInINNER2(INNER2* inner)
{
	if(!IsCorrectINNER2(inner))
	{
		printf("\tMarshalStructAsParam_AsSeqByRefIn: INNER param not as expected\n");
		PrintINNER2(inner,"inner");
		return FALSE;
	}
	ChangeINNER2(inner);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByValOutINNER2(INNER2 inner)
{
	if(!IsCorrectINNER2(&inner))
	{
		printf("\tMarshalStructAsParam_AsSeqByValOut:NNER param not as expected\n");
		PrintINNER2(&inner,"inner");
		return FALSE;
	}
	ChangeINNER2(&inner);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefOutINNER2(INNER2* inner)
{
	//change struct
	ChangeINNER2(inner);
	return TRUE;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByValInnerExplicit(InnerExplicit inner)
{
	if((&inner)->f1 != 1 || memcmp((&inner)->f3, "some string",11*sizeof(char)) != 0)
	{
		printf("\tMarshalStructAsParam_AsExpByVal: INNER param not as expected\n");
		PrintInnerExplicit(&inner,"inner");
		return FALSE;
	}
	ChangeInnerExplicit(&inner);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefInnerExplicit(InnerExplicit* inner)
{
	if(inner->f1 != 1 || memcmp(inner->f3, "some string",11*sizeof(char)) != 0)
	{
		printf("\tMarshalStructAsParam_AsExpByRef: INNER param not as expected\n");
		PrintInnerExplicit(inner,"inner");
		return FALSE;
	}
	ChangeInnerExplicit(inner);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefInInnerExplicit(InnerExplicit* inner)
{
	if(inner->f1 != 1 || memcmp(inner->f3, "some string",11*sizeof(char)) != 0)
	{
		printf("\tMarshalStructAsParam_AsExpByRefIn: INNER param not as expected\n");
		PrintInnerExplicit(inner,"inner");
		return FALSE;
	}
	ChangeInnerExplicit(inner);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefOutInnerExplicit(InnerExplicit* inner)
{
	if(inner->f1 != 0 || inner->f2 != 0.0)
	{
		printf("\tMarshalStructAsParam_AsExpByRefOut: INNER param not as expected\n");
		return FALSE;
	}
	ChangeInnerExplicit(inner);
	return TRUE;
}



////////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByValInnerArrayExplicit(InnerArrayExplicit outer2)
{
	for(int i = 0;i<NumArrElements;i++)
	{
		if((&outer2)->arr[i].f1 != 1)
		{
			printf("\tMarshalStructAsParam_AsExpByVal3:InnerArrayExplicit param not as expected\n");
			return FALSE;
		}
	}
	if(memcmp((&outer2)->f4,"some string2",12) != 0)
	{
		printf("\tMarshalStructAsParam_AsExpByVal3:InnerArrayExplicit param f4 not as expected\n");
		return FALSE;
	}
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefInnerArrayExplicit(InnerArrayExplicit* outer2)
{
	for(int i = 0;i<NumArrElements;i++)
	{
		if(outer2->arr[i].f1 != 1)
		{
			printf("\tMarshalStructAsParam_AsExpByRef3:InnerArrayExplicit param not as expected\n");
			return FALSE;
		}
	}
	if(memcmp(outer2->f4,"some string2",12) != 0)
	{
		printf("\tMarshalStructAsParam_AsExpByRef3:InnerArrayExplicit param f4 not as expected\n");
		return FALSE;
	}
	for(int i =0;i<NumArrElements;i++)
	{
		outer2->arr[i].f1 = 77;
	}
	char const * temp = "change string2";
	size_t len = strlen(temp);
	LPCSTR str = (LPCSTR)TP_CoTaskMemAlloc( sizeof(char)*(len+1) );
	strcpy((char*)str,temp);
	outer2->f4 = str;
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefInInnerArrayExplicit(InnerArrayExplicit* outer2)
{
	for(int i = 0;i<NumArrElements;i++)
	{
		if(outer2->arr[i].f1 != 1)
		{
			printf("\tMarshalStructAsParam_AsExpByRefIn3:InnerArrayExplicit param not as expected\n");
			return FALSE;
		}
	}
	if(memcmp(outer2->f4, "some string2",12*(sizeof(char))) != 0)
	{
		printf("\tMarshalStructAsParam_AsExpByRefIn3:InnerArrayExplicit param f4 not as expected\n");
		return FALSE;
	}
	for(int i =0;i<NumArrElements;i++)
	{
		outer2->arr[i].f1 = 77;
	}
	char const * temp = "change string2";
	size_t len = strlen(temp);
	LPCSTR str = (LPCSTR)TP_CoTaskMemAlloc( sizeof(char)*(len+1) );
	strcpy((char*)str,temp);
	outer2->f4 = str;
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefOutInnerArrayExplicit(InnerArrayExplicit* outer2)
{
	for(int i =0;i<NumArrElements;i++)
	{
		outer2->arr[i].f1 = 77;
	}
	char const * temp = "change string2";
	size_t len = strlen(temp);
	LPCSTR str = (LPCSTR)TP_CoTaskMemAlloc( sizeof(char)*(len+1) );
	strcpy((char*)str,temp);
	outer2->f4 = str;
	return TRUE;
}


////////////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByValOUTER3(OUTER3 outer3)
{
	if(!IsCorrectOUTER3(&outer3))
	{
		printf("\tMarshalStructAsParam_AsExoByVal4:OUTER3 param not as expected\n");
		PrintOUTER3(&outer3,"OUTER3");
		return FALSE;
	}
	ChangeOUTER3(&outer3);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefOUTER3(OUTER3* outer3)
{
	if(!IsCorrectOUTER3(outer3))
	{
		printf("\tMarshalStructAsParam_AsExoByRef4:OUTER3 param not as expected\n");
		PrintOUTER3(outer3,"OUTER3");
		return FALSE;
	}
	ChangeOUTER3(outer3);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefInOUTER3(OUTER3* outer3)
{
	if(!IsCorrectOUTER3(outer3))
	{
		printf("\tMarshalStructAsParam_AsExoByRefIn4:OUTER3 param not as expected\n");
		PrintOUTER3(outer3,"OUTER3");
		return FALSE;
	}
	ChangeOUTER3(outer3);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefOutOUTER3(OUTER3* outer3)
{
	ChangeOUTER3(outer3);
	return TRUE;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByValU(U str1)
{
	if(!IsCorrectU(&str1))
	{
		printf("\tMarshalStructAsParam_AsExpByVal6:U param not as expected\n");
		PrintU(&str1, "str1");
		return FALSE;
	}
	ChangeU(&str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefU(U* str1)
{
	if(!IsCorrectU(str1))
	{
		printf("\tMarshalStructAsParam_AsExpByRef6:U param not as expected\n");
		PrintU(str1, "str1");
		return FALSE;
	}
	ChangeU(str1);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefInU(U* str1)
{
	if(!IsCorrectU(str1))
	{
		printf("\tMarshalStructAsParam_AsExpByRefIn6:U param not as expected\n");
		PrintU(str1, "str1");
		return FALSE;
	}
	ChangeU(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefOutU(U* str1)
{
	ChangeU(str1);

	return TRUE;
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByValByteStructPack2Explicit(ByteStructPack2Explicit str1)
{
	if(!IsCorrectByteStructPack2Explicit(&str1))
	{
		printf("\tMarshalStructAsParam_AsExpByVal7:ByteStructPack2Explicit param not as expected\n");
		PrintByteStructPack2Explicit(&str1, "str1");
		return FALSE;
	}
	ChangeByteStructPack2Explicit(&str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefByteStructPack2Explicit(ByteStructPack2Explicit* str1)
{
	if(!IsCorrectByteStructPack2Explicit(str1))
	{
		printf("\tMarshalStructAsParam_AsExpByRef7:ByteStructPack2Explicit param not as expected\n");
		PrintByteStructPack2Explicit(str1, "str1");
		return FALSE;
	}
	ChangeByteStructPack2Explicit(str1);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefInByteStructPack2Explicit(ByteStructPack2Explicit* str1)
{
	if(!IsCorrectByteStructPack2Explicit(str1))
	{
		printf("\tMarshalStructAsParam_AsExpByRefIn7:ByteStructPack2Explicit param not as expected\n");
		PrintByteStructPack2Explicit(str1, "str1");
		return FALSE;
	}
	ChangeByteStructPack2Explicit(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefOutByteStructPack2Explicit(ByteStructPack2Explicit* str1)
{
	ChangeByteStructPack2Explicit(str1);

	return TRUE;
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByValShortStructPack4Explicit(ShortStructPack4Explicit str1)
{
	if(!IsCorrectShortStructPack4Explicit(&str1))
	{
		printf("\tMarshalStructAsParam_AsExpByVal8:ShortStructPack4Explicit param not as expected\n");
		PrintShortStructPack4Explicit(&str1, "str1");
		return FALSE;
	}
	ChangeShortStructPack4Explicit(&str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefShortStructPack4Explicit(ShortStructPack4Explicit* str1)
{
	if(!IsCorrectShortStructPack4Explicit(str1))
	{
		printf("\tMarshalStructAsParam_AsExpByRef8:ShortStructPack4Explicit param not as expected\n");
		PrintShortStructPack4Explicit(str1, "str1");
		return FALSE;
	}
	ChangeShortStructPack4Explicit(str1);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefInShortStructPack4Explicit(ShortStructPack4Explicit* str1)
{
	if(!IsCorrectShortStructPack4Explicit(str1))
	{
		printf("\tMarshalStructAsParam_AsExpByRefIn8:ShortStructPack4Explicit param not as expected\n");
		PrintShortStructPack4Explicit(str1, "str1");
		return FALSE;
	}
	ChangeShortStructPack4Explicit(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefOutShortStructPack4Explicit(ShortStructPack4Explicit* str1)
{
	ChangeShortStructPack4Explicit(str1);

	return TRUE;
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByValIntStructPack8Explicit(IntStructPack8Explicit str1)
{
	if(!IsCorrectIntStructPack8Explicit(&str1))
	{
		printf("\tMarshalStructAsParam_AsExpByVal9:IntStructPack8Explicit param not as expected\n");
		PrintIntStructPack8Explicit(&str1, "str1");
		return FALSE;
	}
	ChangeIntStructPack8Explicit(&str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefIntStructPack8Explicit(IntStructPack8Explicit* str1)
{
	if(!IsCorrectIntStructPack8Explicit(str1))
	{
		printf("\tMarshalStructAsParam_AsExpByRef9:IntStructPack8Explicit param not as expected\n");
		PrintIntStructPack8Explicit(str1, "str1");
		return FALSE;
	}
	ChangeIntStructPack8Explicit(str1);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefInIntStructPack8Explicit(IntStructPack8Explicit* str1)
{
	if(!IsCorrectIntStructPack8Explicit(str1))
	{
		printf("\tMarshalStructAsParam_AsExpByRefIn9:IntStructPack8Explicit param not as expected\n");
		PrintIntStructPack8Explicit(str1, "str1");
		return FALSE;
	}
	ChangeIntStructPack8Explicit(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefOutIntStructPack8Explicit(IntStructPack8Explicit* str1)
{
	ChangeIntStructPack8Explicit(str1);

	return TRUE;
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByValLongStructPack16Explicit(LongStructPack16Explicit str1)
{
	if(!IsCorrectLongStructPack16Explicit(&str1))
	{
		printf("\tMarshalStructAsParam_AsExpByVal10:LongStructPack16Explicit param not as expected\n");
		PrintLongStructPack16Explicit(&str1, "str1");
		return FALSE;
	}
	ChangeLongStructPack16Explicit(&str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefLongStructPack16Explicit(LongStructPack16Explicit* str1)
{
	if(!IsCorrectLongStructPack16Explicit(str1))
	{
		printf("\tMarshalStructAsParam_AsExpByRef10:LongStructPack16Explicit param not as expected\n");
		PrintLongStructPack16Explicit(str1, "str1");
		return FALSE;
	}
	ChangeLongStructPack16Explicit(str1);
	return TRUE;
}

extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefInLongStructPack16Explicit(LongStructPack16Explicit* str1)
{
	if(!IsCorrectLongStructPack16Explicit(str1))
	{
		printf("\tMarshalStructAsParam_AsExpByRefIn10:LongStructPack16Explicit param not as expected\n");
	    PrintLongStructPack16Explicit(str1, "str1");
		return FALSE;
	}
	ChangeLongStructPack16Explicit(str1);
	return TRUE;
}
extern "C" DLL_EXPORT BOOL WINAPI MarshalStructAsParam_AsExpByRefOutLongStructPack16Explicit(LongStructPack16Explicit* str1)
{
	ChangeLongStructPack16Explicit(str1);

	return TRUE;
}

