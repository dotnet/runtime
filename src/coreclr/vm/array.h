// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _ARRAY_H_
#define _ARRAY_H_

// A 32 dimension array with at least 2 elements in each dimension requires
// 4gb of memory. Limit the maximum number of dimensions to a value that 
// requires 4gb of memory.
// If you make this bigger, you need to make MAX_CLASSNAME_LENGTH bigger too.
#define MAX_RANK 32

class MethodTable;


Stub *GenerateArrayOpStub(ArrayMethodDesc* pMD);



BOOL IsImplicitInterfaceOfSZArray(MethodTable *pIntfMT);
MethodDesc* GetActualImplementationForArrayGenericIListOrIReadOnlyListMethod(MethodDesc *pItfcMeth, TypeHandle theT);

CorElementType GetNormalizedIntegralArrayElementType(CorElementType elementType);

#endif// _ARRAY_H_
