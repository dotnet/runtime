// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

// 

////////////////////////////////////////////////////////////////////////////
//
//  Class:    TextElementEnumerator
//
//  Purpose:  
//
//  Date:     March 31, 1999
//
////////////////////////////////////////////////////////////////////////////

using System.Collections;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace System.Globalization
{
    //
    // This is public because GetTextElement() is public.
    //

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class TextElementEnumerator : IEnumerator
    {
        private String str;
        private int index;
        private int startIndex;

        [NonSerialized] 
        private int strLen;                // This is the length of the total string, counting from the beginning of string.

        [NonSerialized] 
        private int currTextElementLen; // The current text element lenght after MoveNext() is called.

        [OptionalField(VersionAdded = 2)] 
        private UnicodeCategory uc;

        [OptionalField(VersionAdded = 2)] 
        private int charLen;            // The next abstract char to look at after MoveNext() is called.  It could be 1 or 2, depending on if it is a surrogate or not.

        internal TextElementEnumerator(String str, int startIndex, int strLen)
        {
            Contract.Assert(str != null, "TextElementEnumerator(): str != null");
            Contract.Assert(startIndex >= 0 && strLen >= 0, "TextElementEnumerator(): startIndex >= 0 && strLen >= 0");
            Contract.Assert(strLen >= startIndex, "TextElementEnumerator(): strLen >= startIndex");
            this.str = str;
            this.startIndex = startIndex;
            this.strLen = strLen;
            Reset();
        }

        // the following fields is defined to keep the compatibility with Everett.
        // don't change/remove the names/types of these fields.
        private int endIndex;
        private int nextTextElementLen;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            charLen = -1;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            strLen = endIndex + 1;
            currTextElementLen = nextTextElementLen;

            if (charLen == -1)
            {
                uc = CharUnicodeInfo.InternalGetUnicodeCategory(str, index, out charLen);
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx)
        {
            endIndex = strLen - 1;
            nextTextElementLen = currTextElementLen;
        }

        public bool MoveNext()
        {
            if (index >= strLen)
            {
                // Make the index to be greater than strLen so that we can throw exception if GetTextElement() is called.
                index = strLen + 1;
                return (false);
            }
            currTextElementLen = StringInfo.GetCurrentTextElementLen(str, index, strLen, ref uc, ref charLen);
            index += currTextElementLen;
            return (true);
        }

        //
        // Get the current text element.
        //

        public Object Current
        {
            get
            {
                return (GetTextElement());
            }
        }

        //
        // Get the current text element.
        //

        public String GetTextElement()
        {
            if (index == startIndex)
            {
                throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
            }
            if (index > strLen)
            {
                throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
            }

            return (str.Substring(index - currTextElementLen, currTextElementLen));
        }

        //
        // Get the starting index of the current text element.
        //

        public int ElementIndex
        {
            get
            {
                if (index == startIndex)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                }
                return (index - currTextElementLen);
            }
        }


        public void Reset()
        {
            index = startIndex;
            if (index < strLen)
            {
                // If we have more than 1 character, get the category of the current char.
                uc = CharUnicodeInfo.InternalGetUnicodeCategory(str, index, out charLen);
            }
        }
    }
}
