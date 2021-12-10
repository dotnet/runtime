// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System
{
    // CONTRACT with Runtime
    // The String type is one of the primitives understood by the compilers and runtime
    // Data Contract: One int field, m_stringLength, the number of 2-byte wide chars that are valid

    // STRING LAYOUT
    // -------------
    // Strings are null-terminated for easy interop with native, but the value returned by String.Length
    // does NOT include this null character in its count.  As a result, there's some trickiness here in the
    // layout and allocation of strings that needs explanation...
    //
    // String is allocated like any other array, using the RhNewArray API.  It is essentially a very special
    // char[] object.  In order to be an array, the String MethodTable must have an 'array element size' of 2,
    // which is setup by a special case in the binder.  Strings must also have a typical array instance
    // layout, which means that the first field after the m_pEEType field is the 'number of array elements'
    // field.  However, here, it is called m_stringLength because it contains the number of characters in the
    // string (NOT including the terminating null element) and, thus, directly represents both the array
    // length and String.Length.
    //
    // As with all arrays, the GC calculates the size of an object using the following formula:
    //
    //      obj_size = align(base_size + (num_elements * element_size), sizeof(void*))
    //
    // The values 'base_size' and 'element_size' are both stored in the MethodTable for String and 'num_elements'
    // is m_stringLength.
    //
    // Our base_size is the size of the fixed portion of the string defined below.  It, therefore, contains
    // the size of the m_firstChar field in it.  This means that, since our string data actually starts
    // inside the fixed 'base_size' area, and our num_elements is equal to String.Length, we end up with one
    // extra character at the end.  This is how we get our extra null terminator which allows us to pass a
    // pinned string out to native code as a null-terminated string.  This is also why we don't increment the
    // requested string length by one before passing it to RhNewArray.  There is no need to allocate an extra
    // array element, it is already allocated here in the fixed layout of the String.
    //
    // Typically, the base_size of an array type is aligned up to the nearest pointer size multiple (so that
    // array elements start out aligned in case they need alignment themselves), but we don't want to do that
    // with String because we are allocating String.Length components with RhNewArray and the overall object
    // size will then need another alignment, resulting in wasted space.  So the binder specially shrinks the
    // base_size of String, leaving it unaligned in order to allow the use of that otherwise wasted space.
    //
    // One more note on base_size -- on 64-bit, the base_size ends up being 22 bytes, which is less than the
    // min_obj_size of (3 * sizeof(void*)).  This is OK because our array allocator will still align up the
    // overall object size, so a 0-length string will end up with an object size of 24 bytes, which meets the
    // min_obj_size requirement.
    //

    // This type does not override GetHashCode, Equals
#pragma warning disable 0661, 0660
    [StructLayout(LayoutKind.Sequential)]
    public class String
    {
#if TARGET_64BIT
        private const int POINTER_SIZE = 8;
#else
        private const int POINTER_SIZE = 4;
#endif
        //                                        m_pEEType    + m_stringLength
        internal const int FIRST_CHAR_OFFSET = POINTER_SIZE + sizeof(int);

        // CS0169: The private field '{blah}' is never used
        // CS0649: Field '{blah}' is never assigned to, and will always have its default value
#pragma warning disable 169, 649
        private int _stringLength;
        private char _firstChar;

#pragma warning restore

        public int Length
        {
            get
            {
                return _stringLength;
            }
        }
    }
}
