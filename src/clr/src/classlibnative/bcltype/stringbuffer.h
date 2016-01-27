// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: StringBuffer.h
//

//
// Purpose: Contains types and method signatures for the 
// StringBuffer class.
// 
// Each function that we call through native only gets one argument,
// which is actually a pointer to it's stack of arguments.  Our structs
// for accessing these are defined below.
//

//

#ifndef _STRINGBUFFER_H_
#define _STRINGBUFFER_H_

#define CAPACITY_LOW  10000
#define CAPACITY_MID  15000
#define CAPACITY_HIGH 20000
#define CAPACITY_FIXEDINC 5000
#define CAPACITY_PERCENTINC 1.25

class COMStringBuffer {

public:
    //
    // NATIVE HELPER METHODS
    //
    static FCDECL3(void, ReplaceBufferInternal, StringBufferObject* thisRefUNSAFE, __in_ecount(newLength) WCHAR* newBuffer, INT32 newLength);
    static FCDECL3(void, ReplaceBufferAnsiInternal, StringBufferObject* thisRefUNSAFE, __in_ecount(newCapacity) CHAR* newBuffer, INT32 newCapacity);
};

#endif // _STRINGBUFFER_H
