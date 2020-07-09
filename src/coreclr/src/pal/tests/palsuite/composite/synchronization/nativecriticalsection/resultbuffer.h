// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <iostream>
//#include <assert.h>
#ifndef _RESULT_BUFFER_H_
#define _RESULT_BUFFER_H_

//#include <palsuite.h>

struct ResultData
{
	int value;
	int size;
//	ResultData* NextResult;
};

 class ResultBuffer
{
		//  Declare a pointer to a memory buffer to store the logged results
		char* buffer;
		// Declare an object to store the maximum Thread count
		int	MaxThreadCount;
		// Declare and internal data object to store the calculated offset between adjacent threads data sets
		int	ThreadOffset;

		// Declare a linked list object to store the parameter values
public:

	// Declare a constructor for the single process case
	ResultBuffer(int ThreadCount, int ThreadLogSize);
	// Declare a method to log data for the single process instance
	int LogResult(int Thread, char* Data);

    char* getResultBuffer(int threadId);
};

#include "resultbuffer.cpp"
#endif // _RESULT_BUFFER_H_


