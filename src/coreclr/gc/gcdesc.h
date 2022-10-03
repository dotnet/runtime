// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
//
// GC Object Pointer Location Series Stuff
//



#ifndef _GCDESC_H_
#define _GCDESC_H_

#ifdef HOST_64BIT
typedef uint32_t HALF_SIZE_T;
#else   // HOST_64BIT
typedef uint16_t HALF_SIZE_T;
#endif


typedef size_t *JSlot;


//
// These two classes make up the apparatus with which the object references
// within an object can be found.
//
// CGCDescSeries:
//
// The CGCDescSeries class describes a series of object references within an
// object by describing the size of the series (which has an adjustment which
// will be explained later) and the starting point of the series.
//
// The series size is adjusted when the map is created by subtracting the
// GetBaseSize() of the object.   On retieval of the size the total size
// of the object is added back.   For non-array objects the total object
// size is equal to the base size, so this returns the same value.   For
// array objects this will yield the size of the data portion of the array.
// Since arrays containing object references will contain ONLY object references
// this is a fast way of handling arrays and normal objects without a
// conditional test
//
//
//
// CGCDesc:
//
// The CGCDesc is a collection of CGCDescSeries objects to describe all the
// different runs of pointers in a particular object.   <TODO> [add more on the strange
// way the CGCDesc grows backwards in memory behind the MethodTable]
//</TODO>

struct val_serie_item
{
    HALF_SIZE_T nptrs;
    HALF_SIZE_T skip;
    void set_val_serie_item (HALF_SIZE_T nptrs, HALF_SIZE_T skip)
    {
        this->nptrs = nptrs;
        this->skip = skip;
    }
};

typedef DPTR(class CGCDescSeries) PTR_CGCDescSeries;
typedef DPTR(class MethodTable) PTR_MethodTable;
class CGCDescSeries
{
public:
    union
    {
        size_t seriessize;              // adjusted length of series (see above) in bytes
        val_serie_item val_serie[1];    //coded serie for value class array
    };

    size_t startoffset;

    size_t GetSeriesCount ()
    {
        return seriessize/sizeof(JSlot);
    }

    void SetSeriesCount (size_t newcount)
    {
        seriessize = newcount * sizeof(JSlot);
    }

    void IncSeriesCount (size_t increment = 1)
    {
        seriessize += increment * sizeof(JSlot);
    }

    size_t GetSeriesSize ()
    {
        return seriessize;
    }

    void SetSeriesSize (size_t newsize)
    {
        seriessize = newsize;
    }

    void SetSeriesValItem (val_serie_item item, int index)
    {
        val_serie [index] = item;
    }

    void SetSeriesOffset (size_t newoffset)
    {
        startoffset = newoffset;
    }

    size_t GetSeriesOffset ()
    {
        return startoffset;
    }
};





typedef DPTR(class CGCDesc) PTR_CGCDesc;
class CGCDesc
{
    // Don't construct me, you have to hand me a ptr to the *top* of my storage in Init.
    CGCDesc () {}

    //
    // NOTE: for alignment reasons, NumSeries is stored as a size_t.
    //       This makes everything nicely 8-byte aligned on IA64.
    //
public:
    static size_t ComputeSize (size_t NumSeries)
    {
        _ASSERTE (ptrdiff_t(NumSeries) > 0);

        return sizeof(size_t) + NumSeries*sizeof(CGCDescSeries);
    }

    // For value type array
    static size_t ComputeSizeRepeating (size_t NumSeries)
    {
        _ASSERTE (ptrdiff_t(NumSeries) > 0);

        return sizeof(size_t) + sizeof(CGCDescSeries) +
               (NumSeries-1)*sizeof(val_serie_item);
    }

#ifndef DACCESS_COMPILE
    static void Init (void* mem, size_t NumSeries)
    {
        *((size_t*)mem-1) = NumSeries;
    }

    static void InitValueClassSeries (void* mem, size_t NumSeries)
    {
        *((ptrdiff_t*)mem-1) = -((ptrdiff_t)NumSeries);
    }
#endif

    static PTR_CGCDesc GetCGCDescFromMT (MethodTable * pMT)
    {
        // If it doesn't contain pointers, there isn't a GCDesc
        PTR_MethodTable mt(pMT);

        _ASSERTE(mt->ContainsPointers());

        return PTR_CGCDesc(mt);
    }

    size_t GetNumSeries ()
    {
        return *(PTR_size_t(PTR_CGCDesc(this))-1);
    }

    // Returns lowest series in memory.
    // Cannot be used for valuetype arrays
    PTR_CGCDescSeries GetLowestSeries ()
    {
        _ASSERTE (ptrdiff_t(GetNumSeries()) > 0);
        return PTR_CGCDescSeries(PTR_uint8_t(PTR_CGCDesc(this))
                                 - ComputeSize(GetNumSeries()));
    }

    // Returns highest series in memory.
    PTR_CGCDescSeries GetHighestSeries ()
    {
        return PTR_CGCDescSeries(PTR_size_t(PTR_CGCDesc(this))-1)-1;
    }

    // Returns number of immediate pointers this object has. It should match the number of
    // pointers enumerated by go_through_object_cl macro. The implementation shape has intentional
    // similarity with the go_through_object family of macros.
    // size is only used if you have an array of value types.
#ifndef DACCESS_COMPILE
    static size_t GetNumPointers (MethodTable* pMT, size_t ObjectSize, size_t NumComponents)
    {
        size_t NumOfPointers = 0;

        if (pMT->ContainsPointers())
        {
            CGCDesc* map = GetCGCDescFromMT(pMT);
            CGCDescSeries* cur = map->GetHighestSeries();
            ptrdiff_t cnt = (ptrdiff_t)map->GetNumSeries();

            if (cnt >= 0)
            {
                CGCDescSeries* last = map->GetLowestSeries();
                do
                {
                    NumOfPointers += (cur->GetSeriesSize() + ObjectSize) / sizeof(JSlot);
                    cur--;
                }
                while (cur >= last);
            }
            else
            {
                /* Handle the repeating case - array of valuetypes */
                for (ptrdiff_t __i = 0; __i > cnt; __i--)
                {
                    NumOfPointers += cur->val_serie[__i].nptrs;
                }

                NumOfPointers *= NumComponents;
            }
        }

#ifndef FEATURE_NATIVEAOT
        if (pMT->Collectible())
        {
            NumOfPointers += 1;
        }
#endif

        return NumOfPointers;
    }
#endif

    // Size of the entire slot map.
    size_t GetSize ()
    {
        ptrdiff_t numSeries = (ptrdiff_t) GetNumSeries();
        if (numSeries < 0)
        {
            return ComputeSizeRepeating(-numSeries);
        }
        else
        {
            return ComputeSize(numSeries);
        }
    }

    uint8_t *GetStartOfGCData()
    {
        return ((uint8_t *)this) - GetSize();
    }

private:

    BOOL IsValueClassSeries()
    {
        return ((ptrdiff_t) GetNumSeries()) < 0;
    }

};

#define MAX_SIZE_FOR_VALUECLASS_IN_ARRAY 0xffff
#define MAX_PTRS_FOR_VALUECLASSS_IN_ARRAY 0xffff


#endif // _GCDESC_H_
