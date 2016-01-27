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

    inline static void SwapIfGreaterWithItems(KIND keys[], KIND items[], int a, int b) {
        if (a != b) {
            if (keys[a] > keys[b]) {
                KIND key = keys[a];
                keys[a] = keys[b];
                keys[b] = key;
                if (items != NULL) {
                    KIND item = items[a];
                    items[a] = items[b];
                    items[b] = item;
                }
            }
        }
    }

    // For sorting, move all NaN instances to front of the input array
    template <class REAL>
    static unsigned int NaNPrepass(REAL keys[], REAL items[], unsigned int left, unsigned int right) {
        for (unsigned int i = left; i <= right; i++) {
            if (_isnan(keys[i])) {
                REAL temp = keys[left];
                keys[left] = keys[i];
                keys[i] = temp;
                if (items != NULL) {
                    temp = items[left];
                    items[left] = items[i];
                    items[i] = temp;
                }
                left++;
            }
        }
        return left;
    }
	
    // Implementation of Introspection Sort
    static void IntrospectiveSort(KIND keys[], KIND items[], int left, int right) {
        WRAPPER_NO_CONTRACT;

        // Make sure left != right in your own code.
        _ASSERTE(keys != NULL && left < right);

        int length = right - left + 1;

        if (length < 2)
            return;

        IntroSort(keys, items, left, right, 2 * FloorLog2(length));
    }

    static const int introsortSizeThreshold = 16;

    static int FloorLog2(int n)
    {
        int result = 0;
        while (n >= 1)
        {
            result++;
            n = n / 2;
        }
        return result;
    }

    static void IntroSort(KIND keys[], KIND items[], int lo, int hi, int depthLimit)
    {
        while (hi > lo)
        {
            int partitionSize = hi - lo + 1;
            if(partitionSize <= introsortSizeThreshold)
            {
                if (partitionSize == 1)
                {
                    return;
                }
                if (partitionSize == 2)
                {
                    SwapIfGreaterWithItems(keys, items, lo, hi);
                    return;
                }
                if (partitionSize == 3)
                {
                    SwapIfGreaterWithItems(keys, items, lo, hi-1);
                    SwapIfGreaterWithItems(keys, items, lo, hi);
                    SwapIfGreaterWithItems(keys, items, hi-1, hi);
                    return;
                }
                
                InsertionSort(keys, items, lo, hi);
                return;
            }

            if (depthLimit == 0)
            {
                Heapsort(keys, items, lo, hi);
                return;
            }
            depthLimit--;

            int p = PickPivotAndPartition(keys, items, lo, hi);
            IntroSort(keys, items, p + 1, hi, depthLimit);
            hi = p - 1;            
        }
        return;
    }

    static void Swap(KIND keys[], KIND items[], int i, int j)
    {
        KIND t = keys[i];
        keys[i] = keys[j];
        keys[j] = t;

        if (items != NULL)
        {
            KIND item = items[i];
            items[i] = items[j];
            items[j] = item;
        }
    }

    static int PickPivotAndPartition(KIND keys[], KIND items[], int lo, int hi)
    {
        // Compute median-of-three.  But also partition them, since we've done the comparison.
        int mid = lo + (hi - lo) / 2;

        // Sort lo, mid and hi appropriately, then pick mid as the pivot.
        SwapIfGreaterWithItems(keys, items, lo, mid);
        SwapIfGreaterWithItems(keys, items, lo, hi);
        SwapIfGreaterWithItems(keys, items, mid, hi);

        KIND pivot = keys[mid];
        Swap(keys, items, mid, hi - 1);

        int left = lo, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

        while (left < right)
        {
            while (left < (hi - 1) && keys[++left] < pivot);
            while (right > lo && pivot < keys[--right]);

            if ((left >= right))
                break;

            Swap(keys, items, left, right);
        }

        // Put pivot in the right location.
        Swap(keys, items, left, (hi - 1));
        return left;
    }

    static void Heapsort(KIND keys[], KIND items[], int lo, int hi)
    {
        int n = hi - lo + 1;
        for (int i = n / 2; i >= 1; i = i - 1)
        {
            DownHeap(keys, items, i, n, lo);
        }
        for (int i = n; i > 1; i = i - 1)
        {
            Swap(keys, items, lo, lo + i -1);
            DownHeap(keys, items, 1, i - 1, lo);
        }
    }

    static void DownHeap(KIND keys[], KIND items[], int i, int n, int lo)
    {
        KIND d = keys[lo + i - 1];
        KIND di = (items != NULL) ? items[lo + i - 1] : NULL;
        int child;

        while (i <= n / 2)
        {
            child = 2 * i;
            if (child < n && keys[lo + child - 1] < keys[lo + child])
            {
                child++;
            }
            if (!(d < keys[lo + child - 1]))
                break;

            keys[lo + i - 1] = keys[lo + child - 1];
            if(items != NULL)
                items[lo + i - 1] = items[ lo + child - 1];
            i = child;
        }
        keys[lo + i - 1] = d;
        if(items != NULL)
            items[lo + i - 1] = di;
    }

    static void InsertionSort(KIND keys[], KIND items[], int lo, int hi)
    {
        int i, j;
        KIND t, ti = NULL;
        for (i = lo; i < hi; i++)
        {
            j = i;
            t = keys[i + 1];
            if(items != NULL)
                ti = items[i + 1];
            while (j >= lo && t < keys[j])
            {
                keys[j + 1] = keys[j];
                if(items != NULL)
                    items[j + 1] = items[j];
                j--;
            }
            keys[j + 1] = t;
            if(items != NULL)
                items[j + 1] = ti;
        }
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

    static FCDECL4(FC_BOOL_RET, TrySZSort, ArrayBase * keys, ArrayBase * items, UINT32 left, UINT32 right);
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
