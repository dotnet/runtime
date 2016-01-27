// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: ArrayHelpers.cpp
//

//

#include "common.h"

#include <object.h>
#include "ceeload.h"

#include "excep.h"
#include "frames.h"
#include "vars.hpp"
#include "classnames.h"
#include "arrayhelpers.h"
#include <memory.h>

INT32 ArrayHelper::IndexOfUINT8( UINT8* array, UINT32 index, UINT32 count, UINT8 value) {
    LIMITED_METHOD_CONTRACT;
    UINT8 * pvalue = (UINT8 *)memchr(array + index, value, count);
    if ( NULL == pvalue ) {
        return -1;
    }
    else {
        return static_cast<INT32>(pvalue - array);
    }
}


// A fast IndexOf method for arrays of primitive types.  Returns TRUE or FALSE
// if it succeeds, and stores result in retVal.
FCIMPL5(FC_BOOL_RET, ArrayHelper::TrySZIndexOf, ArrayBase * array, UINT32 index, UINT32 count, Object * value, INT32 * retVal)
    FCALL_CONTRACT;

    VALIDATEOBJECT(array);
    _ASSERTE(array != NULL);

    // <TODO>@TODO: Eventually, consider adding support for single dimension arrays with
    // non-zero lower bounds.  VB might care.  </TODO>
    if (array->GetRank() != 1 || array->GetLowerBoundsPtr()[0] != 0)
        FC_RETURN_BOOL(FALSE);

    _ASSERTE(retVal != NULL);
	_ASSERTE(index <= array->GetNumComponents());
	_ASSERTE(count <= array->GetNumComponents());
	_ASSERTE(array->GetNumComponents() >= index + count);
    *retVal = 0xdeadbeef;  // Initialize the return value.
    // value can be NULL, but of course, will not be in primitive arrays.
    
    TypeHandle arrayTH = array->GetArrayElementTypeHandle();
    const CorElementType arrayElType = arrayTH.GetVerifierCorElementType();
    if (!CorTypeInfo::IsPrimitiveType_NoThrow(arrayElType))
        FC_RETURN_BOOL(FALSE);
    // Handle special case of looking for a NULL object in a primitive array.
    if (value == NULL) {
        *retVal = -1;
        FC_RETURN_BOOL(TRUE);
    }
    TypeHandle valueTH = value->GetTypeHandle();
    if (arrayTH != valueTH)
        FC_RETURN_BOOL(FALSE);

    
    switch(arrayElType) {
    case ELEMENT_TYPE_I1:
    case ELEMENT_TYPE_U1:
    case ELEMENT_TYPE_BOOLEAN:
        *retVal = IndexOfUINT8((U1*) array->GetDataPtr(), index, count, *(U1*)value->UnBox());
        break;

    case ELEMENT_TYPE_I2:
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_CHAR:
        *retVal = ArrayHelpers<U2>::IndexOf((U2*) array->GetDataPtr(), index, count, *(U2*)value->UnBox());		
        break;

    case ELEMENT_TYPE_I4:
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_R4:
    IN_WIN32(case ELEMENT_TYPE_I:)
    IN_WIN32(case ELEMENT_TYPE_U:)
        *retVal = ArrayHelpers<U4>::IndexOf((U4*) array->GetDataPtr(), index, count, *(U4*)value->UnBox());
        break;

    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    case ELEMENT_TYPE_R8:
    IN_WIN64(case ELEMENT_TYPE_I:)
    IN_WIN64(case ELEMENT_TYPE_U:)
        *retVal = ArrayHelpers<U8>::IndexOf((U8*) array->GetDataPtr(), index, count, *(U8*)value->UnBox());
        break;

    default:
        _ASSERTE(!"Unrecognized primitive type in ArrayHelper::TrySZIndexOf");
        FC_RETURN_BOOL(FALSE);
    }
    FC_RETURN_BOOL(TRUE);
FCIMPLEND

// A fast LastIndexOf method for arrays of primitive types.  Returns TRUE or FALSE
// if it succeeds, and stores result in retVal.
FCIMPL5(FC_BOOL_RET, ArrayHelper::TrySZLastIndexOf, ArrayBase * array, UINT32 index, UINT32 count, Object * value, INT32 * retVal)
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(array);
    _ASSERTE(array != NULL);

    // <TODO>@TODO: Eventually, consider adding support for single dimension arrays with
    // non-zero lower bounds.  VB might care.  </TODO>
    if (array->GetRank() != 1 || array->GetLowerBoundsPtr()[0] != 0)
        FC_RETURN_BOOL(FALSE);

    _ASSERTE(retVal != NULL);
    *retVal = 0xdeadbeef;  // Initialize the return value.
    // value can be NULL, but of course, will not be in primitive arrays.
    
    TypeHandle arrayTH = array->GetArrayElementTypeHandle();
    const CorElementType arrayElType = arrayTH.GetVerifierCorElementType();
    if (!CorTypeInfo::IsPrimitiveType_NoThrow(arrayElType))
        FC_RETURN_BOOL(FALSE);
    // Handle special case of looking for a NULL object in a primitive array.
    // Also handle case where the array is of 0 length.
    if (value == NULL || array->GetNumComponents() == 0) {
        *retVal = -1;
        FC_RETURN_BOOL(TRUE);
    }

	_ASSERTE(index < array->GetNumComponents());
	_ASSERTE(count <= array->GetNumComponents());
    _ASSERTE(index + 1 >= count);

    TypeHandle valueTH = value->GetTypeHandle();
    if (arrayTH != valueTH)
        FC_RETURN_BOOL(FALSE);

    
    switch(arrayElType) {
    case ELEMENT_TYPE_I1:
    case ELEMENT_TYPE_U1:
    case ELEMENT_TYPE_BOOLEAN:
        *retVal = ArrayHelpers<U1>::LastIndexOf((U1*) array->GetDataPtr(), index, count, *(U1*)value->UnBox());
        break;

    case ELEMENT_TYPE_I2:
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_CHAR:
        *retVal = ArrayHelpers<U2>::LastIndexOf((U2*) array->GetDataPtr(), index, count, *(U2*)value->UnBox());
        break;

    case ELEMENT_TYPE_I4:
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_R4:
    IN_WIN32(case ELEMENT_TYPE_I:)
    IN_WIN32(case ELEMENT_TYPE_U:)
        *retVal = ArrayHelpers<U4>::LastIndexOf((U4*) array->GetDataPtr(), index, count, *(U4*)value->UnBox());
        break;

    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    case ELEMENT_TYPE_R8:
    IN_WIN64(case ELEMENT_TYPE_I:)
    IN_WIN64(case ELEMENT_TYPE_U:)
        *retVal = ArrayHelpers<U8>::LastIndexOf((U8*) array->GetDataPtr(), index, count, *(U8*)value->UnBox());
        break;

    default:
        _ASSERTE(!"Unrecognized primitive type in ArrayHelper::TrySZLastIndexOf");
        FC_RETURN_BOOL(FALSE);
    }
    FC_RETURN_BOOL(TRUE);
}
FCIMPLEND


FCIMPL5(FC_BOOL_RET, ArrayHelper::TrySZBinarySearch, ArrayBase * array, UINT32 index, UINT32 count, Object * value, INT32 * retVal)
    FCALL_CONTRACT;

    VALIDATEOBJECT(array);
    _ASSERTE(array != NULL);

    // <TODO>@TODO: Eventually, consider adding support for single dimension arrays with
    // non-zero lower bounds.  VB might care.  </TODO>
    if (array->GetRank() != 1 || array->GetLowerBoundsPtr()[0] != 0)
        FC_RETURN_BOOL(FALSE);

    _ASSERTE(retVal != NULL);
	_ASSERTE(index <= array->GetNumComponents());
	_ASSERTE(count <= array->GetNumComponents());
	_ASSERTE(array->GetNumComponents() >= index + count);
    *retVal = 0xdeadbeef;  // Initialize the return value.
    // value can be NULL, but of course, will not be in primitive arrays.
    TypeHandle arrayTH = array->GetArrayElementTypeHandle();
    const CorElementType arrayElType = arrayTH.GetVerifierCorElementType();
    if (!CorTypeInfo::IsPrimitiveType_NoThrow(arrayElType))
        FC_RETURN_BOOL(FALSE);
    // Handle special case of looking for a NULL object in a primitive array.
    if (value == NULL) {
        *retVal = -1;
        FC_RETURN_BOOL(TRUE);
    }

    TypeHandle valueTH = value->GetTypeHandle();
    if (arrayTH != valueTH)
        FC_RETURN_BOOL(FALSE);

    switch(arrayElType) {
    case ELEMENT_TYPE_I1:
		*retVal = ArrayHelpers<I1>::BinarySearchBitwiseEquals((I1*) array->GetDataPtr(), index, count, *(I1*)value->UnBox());
		break;

    case ELEMENT_TYPE_U1:
    case ELEMENT_TYPE_BOOLEAN:
        *retVal = ArrayHelpers<U1>::BinarySearchBitwiseEquals((U1*) array->GetDataPtr(), index, count, *(U1*)value->UnBox());
        break;

    case ELEMENT_TYPE_I2:
        *retVal = ArrayHelpers<I2>::BinarySearchBitwiseEquals((I2*) array->GetDataPtr(), index, count, *(I2*)value->UnBox());
        break;

    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_CHAR:
        *retVal = ArrayHelpers<U2>::BinarySearchBitwiseEquals((U2*) array->GetDataPtr(), index, count, *(U2*)value->UnBox());
        break;

    case ELEMENT_TYPE_I4:
        *retVal = ArrayHelpers<I4>::BinarySearchBitwiseEquals((I4*) array->GetDataPtr(), index, count, *(I4*)value->UnBox());
        break;

    case ELEMENT_TYPE_U4:
        *retVal = ArrayHelpers<U4>::BinarySearchBitwiseEquals((U4*) array->GetDataPtr(), index, count, *(U4*)value->UnBox());
        break;

    case ELEMENT_TYPE_R4:
        *retVal = ArrayHelpers<R4>::BinarySearchBitwiseEquals((R4*) array->GetDataPtr(), index, count, *(R4*)value->UnBox());
        break;

    case ELEMENT_TYPE_I8:
        *retVal = ArrayHelpers<I8>::BinarySearchBitwiseEquals((I8*) array->GetDataPtr(), index, count, *(I8*)value->UnBox());
        break;

    case ELEMENT_TYPE_U8:
        *retVal = ArrayHelpers<U8>::BinarySearchBitwiseEquals((U8*) array->GetDataPtr(), index, count, *(U8*)value->UnBox());
        break;

	case ELEMENT_TYPE_R8:
        *retVal = ArrayHelpers<R8>::BinarySearchBitwiseEquals((R8*) array->GetDataPtr(), index, count, *(R8*)value->UnBox());
        break;

    case ELEMENT_TYPE_I:
    case ELEMENT_TYPE_U:
        // In V1.0, IntPtr & UIntPtr are not fully supported types.  They do 
        // not implement IComparable, so searching & sorting for them should
        // fail.  In V1.1 or V2.0, this should change.  
        FC_RETURN_BOOL(FALSE);

    default:
        _ASSERTE(!"Unrecognized primitive type in ArrayHelper::TrySZBinarySearch");
        FC_RETURN_BOOL(FALSE);
    }
    FC_RETURN_BOOL(TRUE);
FCIMPLEND

FCIMPL4(FC_BOOL_RET, ArrayHelper::TrySZSort, ArrayBase * keys, ArrayBase * items, UINT32 left, UINT32 right)
    FCALL_CONTRACT;

    VALIDATEOBJECT(keys);
    VALIDATEOBJECT(items);
    _ASSERTE(keys != NULL);

    // <TODO>@TODO: Eventually, consider adding support for single dimension arrays with
    // non-zero lower bounds.  VB might care.  </TODO>
    if (keys->GetRank() != 1 || keys->GetLowerBoundsPtr()[0] != 0)
        FC_RETURN_BOOL(FALSE);

	_ASSERTE(left <= right);
	_ASSERTE(right < keys->GetNumComponents() || keys->GetNumComponents() == 0);

    TypeHandle keysTH = keys->GetArrayElementTypeHandle();
    const CorElementType keysElType = keysTH.GetVerifierCorElementType();
    if (!CorTypeInfo::IsPrimitiveType_NoThrow(keysElType))
        FC_RETURN_BOOL(FALSE);
    if (items != NULL) {
        TypeHandle itemsTH = items->GetArrayElementTypeHandle();
        if (keysTH != itemsTH)
            FC_RETURN_BOOL(FALSE);   // Can't currently handle sorting different types of arrays.
    }

    // Handle special case of a 0 element range to sort.
    // Consider both Sort(array, x, x) and Sort(zeroLen, 0, zeroLen.Length-1);
    if (left == right || right == 0xffffffff)
        FC_RETURN_BOOL(TRUE);

    switch(keysElType) {
    case ELEMENT_TYPE_I1:
		ArrayHelpers<I1>::IntrospectiveSort((I1*) keys->GetDataPtr(), (I1*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
		break;

    case ELEMENT_TYPE_U1:
    case ELEMENT_TYPE_BOOLEAN:
        ArrayHelpers<U1>::IntrospectiveSort((U1*) keys->GetDataPtr(), (U1*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
        break;

    case ELEMENT_TYPE_I2:
        ArrayHelpers<I2>::IntrospectiveSort((I2*) keys->GetDataPtr(), (I2*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
        break;

    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_CHAR:
		ArrayHelpers<U2>::IntrospectiveSort((U2*) keys->GetDataPtr(), (U2*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
        break;

    case ELEMENT_TYPE_I4:
		ArrayHelpers<I4>::IntrospectiveSort((I4*) keys->GetDataPtr(), (I4*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
        break;

    case ELEMENT_TYPE_U4:
        ArrayHelpers<U4>::IntrospectiveSort((U4*) keys->GetDataPtr(), (U4*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
        break;
        
    case ELEMENT_TYPE_R4:
    {
        R4 * R4Keys = (R4*) keys->GetDataPtr();
        R4 * R4Items = (R4*) (items == NULL ? NULL : items->GetDataPtr());

        // Comparison to NaN is always false, so do a linear pass 
        // and swap all NaNs to the front of the array
        left = ArrayHelpers<R4>::NaNPrepass(R4Keys, R4Items, left, right);
        if(left != right) ArrayHelpers<R4>::IntrospectiveSort(R4Keys, R4Items, left, right);
        break;
    };

    case ELEMENT_TYPE_I8:
        ArrayHelpers<I8>::IntrospectiveSort((I8*) keys->GetDataPtr(), (I8*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
        break;

    case ELEMENT_TYPE_U8:
        ArrayHelpers<U8>::IntrospectiveSort((U8*) keys->GetDataPtr(), (U8*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
        break;

    case ELEMENT_TYPE_R8:
    {
        R8 * R8Keys = (R8*) keys->GetDataPtr();
        R8 * R8Items = (R8*) (items == NULL ? NULL : items->GetDataPtr());

        // Comparison to NaN is always false, so do a linear pass 
        // and swap all NaNs to the front of the array
        left = ArrayHelpers<R8>::NaNPrepass(R8Keys, R8Items, left, right);
        if(left != right) ArrayHelpers<R8>::IntrospectiveSort(R8Keys, R8Items, left, right);
        break;
    };

    case ELEMENT_TYPE_I:
    case ELEMENT_TYPE_U:
        // In V1.0, IntPtr & UIntPtr are not fully supported types.  They do 
        // not implement IComparable, so searching & sorting for them should
        // fail.  In V1.1 or V2.0, this should change.  
        FC_RETURN_BOOL(FALSE);

    default:
        _ASSERTE(!"Unrecognized primitive type in ArrayHelper::TrySZSort");
        FC_RETURN_BOOL(FALSE);
    }
    FC_RETURN_BOOL(TRUE);
FCIMPLEND

FCIMPL3(FC_BOOL_RET, ArrayHelper::TrySZReverse, ArrayBase * array, UINT32 index, UINT32 count)
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(array);
    _ASSERTE(array != NULL);

    // <TODO>@TODO: Eventually, consider adding support for single dimension arrays with
    // non-zero lower bounds.  VB might care.  </TODO>
    if (array->GetRank() != 1 || array->GetLowerBoundsPtr()[0] != 0)
        FC_RETURN_BOOL(FALSE);

	_ASSERTE(index <= array->GetNumComponents());
	_ASSERTE(count <= array->GetNumComponents());
	_ASSERTE(array->GetNumComponents() >= index + count);

    TypeHandle arrayTH = array->GetArrayElementTypeHandle();
    const CorElementType arrayElType = arrayTH.GetVerifierCorElementType();
    if (!CorTypeInfo::IsPrimitiveType_NoThrow(arrayElType))
        FC_RETURN_BOOL(FALSE);

    switch(arrayElType) {
    case ELEMENT_TYPE_I1:
    case ELEMENT_TYPE_U1:
    case ELEMENT_TYPE_BOOLEAN:
        ArrayHelpers<U1>::Reverse((U1*) array->GetDataPtr(), index, count);
        break;

    case ELEMENT_TYPE_I2:
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_CHAR:
        ArrayHelpers<U2>::Reverse((U2*) array->GetDataPtr(), index, count);
        break;

    case ELEMENT_TYPE_I4:
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_R4:
    IN_WIN32(case ELEMENT_TYPE_I:)
    IN_WIN32(case ELEMENT_TYPE_U:)
        ArrayHelpers<U4>::Reverse((U4*) array->GetDataPtr(), index, count);
        break;

    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    case ELEMENT_TYPE_R8:
    IN_WIN64(case ELEMENT_TYPE_I:)
    IN_WIN64(case ELEMENT_TYPE_U:)
        ArrayHelpers<U8>::Reverse((U8*) array->GetDataPtr(), index, count);
        break;

    default:
        _ASSERTE(!"Unrecognized primitive type in ArrayHelper::TrySZReverse");
        FC_RETURN_BOOL(FALSE);
    }
    FC_RETURN_BOOL(TRUE);
}
FCIMPLEND
