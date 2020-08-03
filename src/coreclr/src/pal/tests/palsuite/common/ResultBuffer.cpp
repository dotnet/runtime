// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#include "stdafx.h"
#include "resultbuffer.h"

ResultBuffer:: ResultBuffer(int ThreadCount, int ThreadLogSize)
	{
		// Declare an internal status variable
		int Status=0;

		// Update the maximum thread count
		MaxThreadCount = ThreadCount;

		// Allocate the memory buffer based on the passed in thread and process counts
		// and the specified size of the thread specific buffer
		buffer = NULL;
		buffer = (char*)malloc(ThreadCount*ThreadLogSize);
		// Check to see if the buffer memory was allocated
		if (buffer == NULL)
			Status = -1;
		// Initialize the buffer to 0 to prevent bogus data
		memset(buffer,0,ThreadCount*ThreadLogSize);

		// The ThreadOffset is equal to the total number of bytes that will be stored per thread
		ThreadOffset = ThreadLogSize;

	}


		int ResultBuffer::LogResult(int Thread, char* Data)
	{
		// Declare an internal status flad
		int	status = 0;
		
		// Declare an object to store the offset address into the buffer
		int	Offset;

		// Check to make sure the  Thread index is not out of range
		if(Thread > MaxThreadCount)
        {
            Trace("Thread index is out of range, Value of Thread[%d], Value of MaxThreadCount[%d]\n", Thread, MaxThreadCount);
            status = -1;
            return(status);
        }

		// Caculate the offset into the shared buffer based on the process and thread indices
		Offset = (Thread)*ThreadOffset;

		// Write the passed in data to the reserved buffer
		memcpy(buffer+Offset,Data,ThreadOffset);
        
		return(status);
	}


        char* ResultBuffer::getResultBuffer(int threadId)
        {
        
            return (buffer + threadId*ThreadOffset);    
        
        }

