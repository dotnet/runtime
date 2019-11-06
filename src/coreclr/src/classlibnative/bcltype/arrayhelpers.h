// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: ArrayHelpers.h
//

//
// Helper methods for the Array class
// Specifically, this contains indexing, sorting & searching templates.


#ifndef _ARRAYHELPERS_H_
#define _ARRAYHELPERS_H_

#if defined(_MSC_VER) && defined(_TARGET_X86_) && !defined(FPO_ON)
#pragma optimize("y", on)		// Small critical routines, don't put in EBP frame
#define FPO_ON 1
#define COMARRAYHELPERS_TURNED_FPO_ON 1
#endif

#include "fcall.h"


template <class KIND>
class ArrayHelpers
{
public:
    static int IndexOf(KIND array[], UINT32 index, UINT32 count, KIND value) {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(array != NULL && index >= 0 && count >= 0);
        for(UINT32 i=index; i<index+count; i++)
            if (array[i] == value)
                return i;
        return -1;
    }

    static int LastIndexOf(KIND array[], UINT32 index, UINT32 count, KIND value) {
        LIMITED_METHOD_CONTRACT;

        INT32 startIndex = (INT32)index;
        INT32 n = (INT32)count;
        _ASSERTE(array != NULL);
        _ASSERTE(startIndex >= 0);
        _ASSERTE(n >= 0);

        // Note (startIndex- n) may be -1 when startIndex is 0 and n is 1.
        _ASSERTE(startIndex >= n - 1);

        // Prefast: caller asserts guarantee that startIndex - n won't underflow, but we need to spell
        // this out for prefast.
        PREFIX_ASSUME(startIndex >= startIndex - n);
        INT32 endIndex = max(startIndex - n, -1);

        for(INT32 i=startIndex; i> endIndex; i--)
            if (array[i] == value)
                return i;
        return -1;
    }

    static int BinarySearchBitwiseEquals(KIND array[], int index, int length, KIND value) {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(array != NULL);
        _ASSERTE(length >= 0);
        _ASSERTE(index >= 0);

        int lo = index;

        // Prefast: mscorlib.dll!System.Array.BinarySearch(Array,int,int,Object,IComparer)
        // guarantees index and length are in the array bounds and do not overflow
        PREFIX_ASSUME(index >= 0 && length >= 0 && INT32_MAX >= index + length - 1);
        int hi = index + length - 1;

        // Note: if length == 0, hi will be Int32.MinValue, and our comparison
        // here between 0 & -1 will prevent us from breaking anything.
        while (lo <= hi) {
            int i = lo + ((hi - lo) >> 1);
            if (array[i] < value) {
                lo = i + 1;
            }
            else if (array[i] > value){
                hi = i - 1;
            }
            else {
                return i;
            }
        }
        return ~lo;
    }

    static void Reverse(KIND array[], UINT32 index, UINT32 count) {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(array != NULL);
        if (count == 0) {
            return;
        }
        UINT32 i = index;
        UINT32 j = index + count - 1;
        while(i < j) {
            KIND temp = array[i];
            array[i] = array[j];
            array[j] = temp;
            i++;
            j--;
        }
    }
};


class ArrayHelper
{
public:
    // These methods return TRUE or FALSE for success or failure, and the real
    // result is an out param.  They're helpers to make operations on SZ arrays of
    // primitives significantly faster.
    static FCDECL5(FC_BOOL_RET, TrySZIndexOf, ArrayBase * array, UINT32 index, UINT32 count, Object * value, INT32 * retVal);
    static FCDECL5(FC_BOOL_RET, TrySZLastIndexOf, ArrayBase * array, UINT32 index, UINT32 count, Object * value, INT32 * retVal);
    static FCDECL5(FC_BOOL_RET, TrySZBinarySearch, ArrayBase * array, UINT32 index, UINT32 count, Object * value, INT32 * retVal);

    static FCDECL3(FC_BOOL_RET, TrySZReverse, ArrayBase * array, UINT32 index, UINT32 count);

    // Helper methods
    static INT32 IndexOfUINT8( UINT8* array, UINT32 index, UINT32 count, UINT8 value);
};

#if defined(COMARRAYHELPERS_TURNED_FPO_ON)
#pragma optimize("", on)		// Go back to command line default optimizations
#undef COMARRAYHELPERS_TURNED_FPO_ON
#undef FPO_ON
#endif

#endif // _ARRAYHELPERS_H_
