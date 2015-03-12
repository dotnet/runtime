// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "platformdefines.cpp"
#include <stdio.h>
#include <stdlib.h>

const int iManaged = 10;
const int iNative = 11;

const int ArraySIZE = 5;


//Rule for Passing Value
//         Reverse Pinvoke
//M--->N  true,true,true,true,true
//N----M  true,false,true,false,true

//Reverse PInvoke
//Pass by value
typedef BOOL (__stdcall *CallBackIn)(int size,bool arr[]);
extern "C" BOOL DoCallBackIn(CallBackIn callback)
{
	//Init
	bool * arr = (bool*)TP_CoTaskMemAlloc(ArraySIZE);
	for(int i = 0;i < ArraySIZE; i++ )
	{
		if( 0 == i%2) 
		{
			arr[i] =true;
		}
		else
		{
			arr[i] = false;
		}
	}

	if(!callback(ArraySIZE,arr))
    {
        printf("Native Side: in DoCallBackIn, The Callback return wrong value");
        return false;
    }

	//Check the data
	for(int i = 0;i < ArraySIZE; i++ )//Expected:true,false,true,false,true
	{
		if((0 == (i%2)) && !arr[i]) //expect true
		{
			printf("Native Side:Error in DoCallBackIn.The Item is %d\n",i+1);
			TP_CoTaskMemFree(arr);
			return false;
		}
		else if((1 == (i%2))&&arr[i]) //expect false
		{
			printf("Native Side:Error in DoCallBackIn.The Item is %d\n",i+1);
			TP_CoTaskMemFree(arr);
			return false;
		}	
	}
	TP_CoTaskMemFree(arr);
	return true;
}

typedef BOOL (__stdcall *CallBackOut)(int size,bool arr[]);
extern "C" BOOL DoCallBackOut(CallBackOut callback)
{
	bool * arr =(bool *)TP_CoTaskMemAlloc(ArraySIZE);

	if(!callback(ArraySIZE,arr))
    {
        printf("Native Side: in DoCallBackOut, The Callback return wrong value");
        return FALSE;
    }

	//Check the data returnd from Managed Side
	for(int i = 0;i < ArraySIZE; i++ )
	{
		if(!arr[i]) //expect true
		{
			printf("Native Side:Error in DoCallBackOut.The Item is %d\n",i+1);
			TP_CoTaskMemFree(arr);
			return false;
		}	
	}
	TP_CoTaskMemFree(arr);
	return true;
}

typedef BOOL (__stdcall *CallBackInOut)(int size,bool arr[]);
extern "C" BOOL DoCallBackInOut(CallBackInOut callback)
{
	//Init
	bool * arr =(bool *)TP_CoTaskMemAlloc(ArraySIZE);
	for(int i = 0;i < ArraySIZE; i++ )
	{
		if( 0 == i%2)
		{
			arr[i] = true;
		}
		else
		{
			arr[i] = false;
		}
	}

	if(!callback(ArraySIZE,arr))
    {
        printf("Native Side: in DoCallBackInOut, The Callback return wrong value");

        return FALSE;
    }

	//Check the data
	for(int i = 0;i < ArraySIZE; i++ )
	{
		if(!arr[i]) //expect true
		{
			printf("Native Side:Error in DoCallBackInOut.The Item is %d\n",i+1);
			TP_CoTaskMemFree(arr);
			return false;
		}	
	}
	TP_CoTaskMemFree(arr);
	return true;
}


//Reverse PInvoke
//Pass by reference
typedef BOOL (__stdcall *CallBackRefIn)(int size,bool ** arr);
extern "C" BOOL DoCallBackRefIn(CallBackRefIn callback)
{
	//Init:true,false,true,false,true
	bool *parr = (bool *)TP_CoTaskMemAlloc(ArraySIZE);

	for(int i = 0;i < ArraySIZE;++i)
	{
		if( 0 == i%2)
		{
			parr[i] = true;
		}
		else
		{
			parr[i] = false;
		}
	}

	if(!callback(ArraySIZE,&parr)) // &parr
    {
        printf("Native Side: in DoCallBackRefIn, The Callback return wrong value");
        return FALSE;
    }

	//Check the data werent changed
	for(int i = 0;i<ArraySIZE;++i)
	{
		if((0==(i%2)) && !parr[i]) //expect true
		{
			printf("Native Side:Error in DoCallBackInOut.The Item is %d\n",i+1);
			TP_CoTaskMemFree(parr);
			return false;
		}
		else if((1==(i%2))&&parr[i]) //expect false
		{
			printf("Native Side:Error in DoCallBackInOut.The Item is %d\n",i+1);
			TP_CoTaskMemFree(parr);
			return false;
		}
	}
	TP_CoTaskMemFree(parr);
	return true;
}

typedef BOOL (__stdcall *CallBackRefOut)(int size,bool ** arr);
extern "C" BOOL DoCallBackRefOut(CallBackRefOut callback)
{

	bool* parr = NULL;

	if(!callback(ArraySIZE,&parr))
    {
        printf("Native Side: in DoCallBackRefOut, The Callback return wrong value");
        return FALSE;
    }

	//Check the data were changed to true,true
	for(int i = 0;i<ArraySIZE;++i)
	{
		if(!(*(parr + i))) //expect true
		{
			printf("Native Side:Error in DoCallBackRefOut.The Item is %d\n",i+1);
			TP_CoTaskMemFree(parr);
			return false;
		}
	}
	TP_CoTaskMemFree(parr);
	return true;
}

typedef BOOL (__stdcall *CallBackRefInOut)(int size,bool ** arr);
extern "C" BOOL DoCallBackRefInOut(CallBackRefInOut callback)
{
	//Init,true,false,true,false
	bool* parr = (bool*)TP_CoTaskMemAlloc(ArraySIZE);
	for(int i = 0;i<ArraySIZE;++i)
	{
		if( 0 == i%2)
		{
			parr[i] = true;
		}
		else
		{
			parr[i] = false;
		}
	}

	if(!callback(ArraySIZE,&parr))
    {
        printf("Native Side: in DoCallBackRefInOut, The Callback return wrong value");
        return FALSE;
    }

	//Check the data were changed to true,true
	for(int i = 0;i<ArraySIZE;++i)
	{
		if(!(parr[i])) //expect true
		{
			printf("Native Side:Error in DoCallBackRefOut.The Item is %d\n",i+1);
			TP_CoTaskMemFree(parr);
			return false;
		}
	}
	TP_CoTaskMemFree(parr);
	return true;
}
