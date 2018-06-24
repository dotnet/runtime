// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Threading;

namespace System.Text
{
    //
    // Data table for encoding classes.  Used by System.Text.Encoding.
    // This class contains two hashtables to allow System.Text.Encoding
    // to retrieve the data item either by codepage value or by webName.
    //

    // Only statics, does not need to be marked with the serializable attribute
    internal static class EncodingTable
    {
        //This number is the size of the table in native.  The value is retrieved by
        //calling the native GetNumEncodingItems().
        private static int lastEncodingItem = GetNumEncodingItems() - 1;

        //This number is the size of the code page table.  Its generated when we walk the table the first time.
        private static volatile int lastCodePageItem;

        //
        // This points to a native data table which maps an encoding name to the correct code page.        
        //
        internal static unsafe InternalEncodingDataItem* encodingDataPtr = GetEncodingData();
        //
        // This points to a native data table which stores the properties for the code page, and
        // the table is indexed by code page.
        //
        internal static unsafe InternalCodePageDataItem* codePageDataPtr = GetCodePageData();
        //
        // This caches the mapping of an encoding name to a code page.
        //
        private static Hashtable hashByName = Hashtable.Synchronized(new Hashtable(StringComparer.OrdinalIgnoreCase));
        //
        // THe caches the data item which is indexed by the code page value.
        //
        private static Hashtable hashByCodePage = Hashtable.Synchronized(new Hashtable());

        // Find the data item by binary searching the table that we have in native.
        // nativeCompareOrdinalWC is an internal-only function.
        private static unsafe int internalGetCodePageFromName(string name)
        {
            int left = 0;
            int right = lastEncodingItem;
            int index;
            int result;

            //Binary search the array until we have only a couple of elements left and then
            //just walk those elements.
            while ((right - left) > 3)
            {
                index = ((right - left) / 2) + left;

                result = nativeCompareOrdinalIgnoreCaseWC(name, encodingDataPtr[index].webName);

                if (result == 0)
                {
                    //We found the item, return the associated codepage.
                    return (encodingDataPtr[index].codePage);
                }
                else if (result < 0)
                {
                    //The name that we're looking for is less than our current index.
                    right = index;
                }
                else
                {
                    //The name that we're looking for is greater than our current index
                    left = index;
                }
            }

            //Walk the remaining elements (it'll be 3 or fewer).
            for (; left <= right; left++)
            {
                if (nativeCompareOrdinalIgnoreCaseWC(name, encodingDataPtr[left].webName) == 0)
                {
                    return (encodingDataPtr[left].codePage);
                }
            }
            // The encoding name is not valid.
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    SR.Argument_EncodingNotSupported, name), nameof(name));
        }

        // Return a list of all EncodingInfo objects describing all of our encodings
        internal static unsafe EncodingInfo[] GetEncodings()
        {
            if (lastCodePageItem == 0)
            {
                int count;
                for (count = 0; codePageDataPtr[count].codePage != 0; count++)
                {
                    // Count them
                }
                lastCodePageItem = count;
            }

            EncodingInfo[] arrayEncodingInfo = new EncodingInfo[lastCodePageItem];

            int i;
            for (i = 0; i < lastCodePageItem; i++)
            {
                arrayEncodingInfo[i] = new EncodingInfo(codePageDataPtr[i].codePage, CodePageDataItem.CreateString(codePageDataPtr[i].Names, 0),
                    SR.GetResourceString("Globalization_cp_" + codePageDataPtr[i].codePage));
            }

            return arrayEncodingInfo;
        }

        /*=================================GetCodePageFromName==========================
        **Action: Given a encoding name, return the correct code page number for this encoding.
        **Returns: The code page for the encoding.
        **Arguments:
        **  name    the name of the encoding
        **Exceptions:
        **  ArgumentNullException if name is null.
        **  internalGetCodePageFromName will throw ArgumentException if name is not a valid encoding name.
        ============================================================================*/

        internal static int GetCodePageFromName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            object codePageObj;

            //
            // The name is case-insensitive, but ToLower isn't free.  Check for
            // the code page in the given capitalization first.
            //
            codePageObj = hashByName[name];

            if (codePageObj != null)
            {
                return ((int)codePageObj);
            }

            //Okay, we didn't find it in the hash table, try looking it up in the 
            //unmanaged data.
            int codePage = internalGetCodePageFromName(name);

            hashByName[name] = codePage;

            return codePage;
        }

        internal static unsafe CodePageDataItem GetCodePageDataItem(int codepage)
        {
            CodePageDataItem dataItem;

            // We synchronize around dictionary gets/sets. There's still a possibility that two threads
            // will create a CodePageDataItem and the second will clobber the first in the dictionary. 
            // However, that's acceptable because the contents are correct and we make no guarantees
            // other than that. 

            //Look up the item in the hashtable.
            dataItem = (CodePageDataItem)hashByCodePage[codepage];

            //If we found it, return it.
            if (dataItem != null)
            {
                return dataItem;
            }


            //If we didn't find it, try looking it up now.
            //If we find it, add it to the hashtable.
            //This is a linear search, but we probably won't be doing it very often.
            //
            int i = 0;
            int data;
            while ((data = codePageDataPtr[i].codePage) != 0)
            {
                if (data == codepage)
                {
                    dataItem = new CodePageDataItem(i);
                    hashByCodePage[codepage] = dataItem;
                    return (dataItem);
                }
                i++;
            }

            //Nope, we didn't find it.
            return null;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe InternalEncodingDataItem* GetEncodingData();

        //
        // Return the number of encoding data items.
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetNumEncodingItems();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe InternalCodePageDataItem* GetCodePageData();

        //This will not work in case-insensitive mode for any character greater than 0x7F.  
        //We'll throw an ArgumentException.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe int nativeCompareOrdinalIgnoreCaseWC(string strA, sbyte* strBBytes);
    }

    /*=================================InternalEncodingDataItem==========================
    **Action: This is used to map a encoding name to a correct code page number. By doing this,
    ** we can get the properties of this encoding via the InternalCodePageDataItem.
    **
    ** We use this structure to access native data exposed by the native side.
    ============================================================================*/

    [System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
    internal unsafe struct InternalEncodingDataItem
    {
        internal sbyte* webName;
        internal ushort codePage;
    }

    /*=================================InternalCodePageDataItem==========================
    **Action: This is used to access the properties related to a code page.
    ** We use this structure to access native data exposed by the native side.
    ============================================================================*/

    [System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
    internal unsafe struct InternalCodePageDataItem
    {
        internal ushort codePage;
        internal ushort uiFamilyCodePage;
        internal uint flags;
        internal sbyte* Names;
    }
}
