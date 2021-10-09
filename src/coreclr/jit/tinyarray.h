// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef TINYARRAY_H
#define TINYARRAY_H

/*****************************************************************************/

// This is an array packed into some kind of integral data type
// storagetype is the type (integral) which your array is going to be packed into
// itemtype is the type of array elements
// bits_per_element is size of the elements in bits
template <class storageType, class itemType, int bits_per_element>
class TinyArray
{
public:
    // operator[] returns a 'ref' (usually a ref to the element type)
    // This presents a problem if you wanted to implement something like a
    // bitvector via this packed array, because you cannot make a ref to
    // the element type.
    //    The trick is you define something that acts like a ref (TinyArrayRef in this case)
    // which for our purposes means you can assign to and from it and our chosen
    // element type.
    class TinyArrayRef
    {
    public:
        // this is really the getter for the array.
        operator itemType()
        {
            storageType mask  = ((1 << bits_per_element) - 1);
            int         shift = bits_per_element * index;

            itemType result = (itemType)((*data >> shift) & mask);
            return result;
        }

        void operator=(const itemType b)
        {
            storageType mask = ((1 << bits_per_element) - 1);
            assert(itemType(b & mask) == b);

            mask <<= bits_per_element * index;

            *data &= ~mask;
            *data |= b << (bits_per_element * index);
        }
        friend class TinyArray;

    protected:
        TinyArrayRef(storageType* d, int idx) : data(d), index(idx)
        {
        }

        storageType* data;
        int          index;
    };

    storageType data;

    void clear()
    {
        data = 0;
    }

    TinyArrayRef operator[](unsigned int n)
    {
        assert((n + 1) * bits_per_element <= sizeof(storageType) * 8);
        return TinyArrayRef(&data, n);
    }
    // only use this for clearing it
    void operator=(void* rhs)
    {
        assert(rhs == nullptr);
        data = 0;
    }
};

#endif // TINYARRAY_H
