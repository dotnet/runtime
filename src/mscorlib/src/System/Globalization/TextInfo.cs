// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////
//
//
//  Purpose:  This Class defines behaviors specific to a writing system.
//            A writing system is the collection of scripts and
//            orthographic rules required to represent a language as text.
//
//
////////////////////////////////////////////////////////////////////////////

using System.Security;

namespace System.Globalization {
    using System;
    using System.Text;
    using System.Threading;
    using System.Runtime;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;


    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public partial class TextInfo : ICloneable, IDeserializationCallback
    {
        //--------------------------------------------------------------------//
        //                        Internal Information                        //
        //--------------------------------------------------------------------//


        //
        //  Variables.
        //

        [OptionalField(VersionAdded = 2)]
        private String m_listSeparator;

        [OptionalField(VersionAdded = 2)]
        private bool m_isReadOnly = false;

        //
        // In Whidbey we had several names:
        //      m_win32LangID is the name of the culture, but only used for (de)serialization.
        //      customCultureName is the name of the creating custom culture (if custom)  In combination with m_win32LangID
        //              this is authoratative, ie when deserializing.
        //      m_cultureTableRecord was the data record of the creating culture.  (could have different name if custom)
        //      m_textInfoID is the LCID of the textinfo itself (no longer used)
        //      m_name is the culture name (from cultureinfo.name)
        //
        // In Silverlight/Arrowhead this is slightly different:
        //      m_cultureName is the name of the creating culture.  Note that we consider this authoratative,
        //              if the culture's textinfo changes when deserializing, then behavior may change.
        //              (ala Whidbey behavior).  This is the only string Arrowhead needs to serialize.
        //      m_cultureData is the data that backs this class.
        //      m_textInfoName  is the actual name of the textInfo (from cultureData.STEXTINFO)
        //              m_textInfoName can be the same as m_cultureName on Silverlight since the OS knows
        //              how to do the sorting. However in the desktop, when we call the sorting dll, it doesn't
        //              know how to resolve custom locle names to sort ids so we have to have alredy resolved this.
        //      

        [OptionalField(VersionAdded = 3)]
        private String                          m_cultureName;      // Name of the culture that created this text info
        [NonSerialized]private CultureData      m_cultureData;      // Data record for the culture that made us, not for this textinfo
        [NonSerialized]private String           m_textInfoName;     // Name of the text info we're using (ie: m_cultureData.STEXTINFO)
        [NonSerialized]private IntPtr           m_dataHandle;       // Sort handle
        [NonSerialized]private IntPtr           m_handleOrigin;
        [NonSerialized]private bool?            m_IsAsciiCasingSameAsInvariant;


        // Invariant text info
        internal static TextInfo Invariant  
        {
            get 
            {
                if (s_Invariant == null)
                    s_Invariant = new TextInfo(CultureData.Invariant);
                return s_Invariant;
            }
        }
        internal volatile static TextInfo s_Invariant;

        ////////////////////////////////////////////////////////////////////////
        //
        //  TextInfo Constructors
        //
        //  Implements CultureInfo.TextInfo.
        //
        ////////////////////////////////////////////////////////////////////////
        internal TextInfo(CultureData cultureData) 
        {
            // This is our primary data source, we don't need most of the rest of this
            this.m_cultureData = cultureData;
            this.m_cultureName = this.m_cultureData.CultureName;
            this.m_textInfoName = this.m_cultureData.STEXTINFO;
#if !FEATURE_CORECLR
            IntPtr handleOrigin;
            this.m_dataHandle = CompareInfo.InternalInitSortHandle(m_textInfoName, out handleOrigin);
            this.m_handleOrigin = handleOrigin;
#endif
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  Serialization / Deserialization
        //
        //  Note that we have to respect the Whidbey behavior for serialization compatibility
        //
        ////////////////////////////////////////////////////////////////////////

#region Serialization 
        // the following fields are defined to keep the compatibility with Whidbey.
        // don't change/remove the names/types of these fields.
        [OptionalField(VersionAdded = 2)]
        private string customCultureName;

        // the following fields are defined to keep compatibility with Everett.
        // don't change/remove the names/types of these fields.
        [OptionalField(VersionAdded = 1)]
        internal int    m_nDataItem;
        [OptionalField(VersionAdded = 1)]
        internal bool   m_useUserOverride;
        [OptionalField(VersionAdded = 1)]
        internal int    m_win32LangID;


        [OnDeserializing] 
        private void OnDeserializing(StreamingContext ctx) 
        { 
            // Clear these so we can check if we've fixed them yet            
            this.m_cultureData = null;
            this.m_cultureName = null;            
        }   

        private void OnDeserialized()
        {
            // this method will be called twice because of the support of IDeserializationCallback
            if (this.m_cultureData == null)
            {
                if (this.m_cultureName == null)
                {
                    // This is whidbey data, get it from customCultureName/win32langid               
                    if (this.customCultureName != null)
                    {
                        // They gave a custom cultuer name, so use that
                        this.m_cultureName = this.customCultureName; 
                    }
#if FEATURE_USE_LCID
                    else
                    {
                        if (m_win32LangID == 0)
                        {
                            // m_cultureName and m_win32LangID are nulls which means we got uninitialized textinfo serialization stream. 
                            // To be compatible with v2/3/3.5 we need to return ar-SA TextInfo in this case.
                            m_cultureName = "ar-SA";
                        }
                        else
                        {
                            // No custom culture, use the name from the LCID
                            m_cultureName = CultureInfo.GetCultureInfo(m_win32LangID).m_cultureData.CultureName;
                        }
                    }
#endif
                }
                
                // Get the text info name belonging to that culture
                this.m_cultureData = CultureInfo.GetCultureInfo(m_cultureName).m_cultureData;
                this.m_textInfoName = this.m_cultureData.STEXTINFO;
#if !FEATURE_CORECLR
                IntPtr handleOrigin;
                this.m_dataHandle = CompareInfo.InternalInitSortHandle(m_textInfoName, out handleOrigin);
                this.m_handleOrigin = handleOrigin;
#endif
            }            
        }

        
        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            OnDeserialized();
        }   
        
        [OnSerializing]
        private void OnSerializing(StreamingContext ctx) 
        { 
#if !FEATURE_CORECLR
            // Initialize the fields Whidbey expects:
            // Whidbey expected this, so set it, but the value doesn't matter much
            this.m_useUserOverride = false;
#endif // FEATURE_CORECLR

            // Relabel our name since Whidbey expects it to be called customCultureName
            this.customCultureName = this.m_cultureName;

#if FEATURE_USE_LCID
            // Ignore the m_win32LangId because whidbey'll just get it by name if we make it the LOCALE_CUSTOM_UNSPECIFIED.
            this.m_win32LangID     = (CultureInfo.GetCultureInfo(m_cultureName)).LCID;
#endif
        }   
        
#endregion Serialization

        //
        // Internal ordinal comparison functions
        //
        internal static int GetHashCodeOrdinalIgnoreCase(String s)
        {
            return GetHashCodeOrdinalIgnoreCase(s, false, 0);
        }

        internal static int GetHashCodeOrdinalIgnoreCase(String s, bool forceRandomizedHashing, long additionalEntropy)
        {
            // This is the same as an case insensitive hash for Invariant
            // (not necessarily true for sorting, but OK for casing & then we apply normal hash code rules)
            return (Invariant.GetCaseInsensitiveHashCode(s, forceRandomizedHashing, additionalEntropy));
        }

        [System.Security.SecuritySafeCritical]
        internal static unsafe bool TryFastFindStringOrdinalIgnoreCase(int searchFlags, String source, int startIndex, String value, int count, ref int foundIndex)
        {
            return InternalTryFindStringOrdinalIgnoreCase(searchFlags, source, count, startIndex, value, value.Length, ref foundIndex);
        }

        // This function doesn't check arguments. Please do check in the caller.
        // The underlying unmanaged code will assert the sanity of arguments.
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static unsafe int CompareOrdinalIgnoreCase(String str1, String str2)
        {
            // Compare the whole string and ignore case.
            return InternalCompareStringOrdinalIgnoreCase(str1, 0, str2, 0, str1.Length, str2.Length);
        }

        // This function doesn't check arguments. Please do check in the caller.
        // The underlying unmanaged code will assert the sanity of arguments.
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static unsafe int CompareOrdinalIgnoreCaseEx(String strA, int indexA, String strB, int indexB, int lengthA, int lengthB )
        {
            Contract.Assert(strA.Length >= indexA + lengthA,  "[TextInfo.CompareOrdinalIgnoreCaseEx] Caller should've validated strA.Length >= indexA + lengthA");
            Contract.Assert(strB.Length >= indexB + lengthB, "[TextInfo.CompareOrdinalIgnoreCaseEx]  Caller should've validated strB.Length >= indexB + lengthB");
            return InternalCompareStringOrdinalIgnoreCase(strA, indexA, strB, indexB, lengthA, lengthB);
        }

        internal static int IndexOfStringOrdinalIgnoreCase(String source, String value, int startIndex, int count)
        {
            Contract.Assert(source != null, "[TextInfo.IndexOfStringOrdinalIgnoreCase] Caller should've validated source != null");
            Contract.Assert(value != null, "[TextInfo.IndexOfStringOrdinalIgnoreCase] Caller should've validated value != null");
            Contract.Assert(startIndex + count <= source.Length, "[TextInfo.IndexOfStringOrdinalIgnoreCase] Caller should've validated startIndex + count <= source.Length");

            // We return 0 if both inputs are empty strings
            if (source.Length == 0 && value.Length == 0)
            {
                return 0;
            }

            // fast path
            int ret = -1;
            if (TryFastFindStringOrdinalIgnoreCase(Microsoft.Win32.Win32Native.FIND_FROMSTART, source, startIndex, value, count, ref ret))
                return ret;

            // the search space within [source] starts at offset [startIndex] inclusive and includes
            // [count] characters (thus the last included character is at index [startIndex + count -1]
            // [end] is the index of the next character after the search space
            // (it points past the end of the search space)
            int end = startIndex + count;
            
            // maxStartIndex is the index beyond which we never *start* searching, inclusive; in other words;
            // a search could include characters beyond maxStartIndex, but we'd never begin a search at an 
            // index strictly greater than maxStartIndex. 
            int maxStartIndex = end - value.Length;

            for (; startIndex <= maxStartIndex; startIndex++)
            {
                // We should always have the same or more characters left to search than our actual pattern
                Contract.Assert(end - startIndex >= value.Length);
                // since this is an ordinal comparison, we can assume that the lengths must match
                if (CompareOrdinalIgnoreCaseEx(source, startIndex, value, 0, value.Length, value.Length) == 0)
                {
                    return startIndex;
                }
            }
            
            // Not found
            return -1;
        }

        internal static int LastIndexOfStringOrdinalIgnoreCase(String source, String value, int startIndex, int count)
        {
            Contract.Assert(source != null, "[TextInfo.LastIndexOfStringOrdinalIgnoreCase] Caller should've validated source != null");
            Contract.Assert(value != null, "[TextInfo.LastIndexOfStringOrdinalIgnoreCase] Caller should've validated value != null");
            Contract.Assert(startIndex - count+1 >= 0, "[TextInfo.LastIndexOfStringOrdinalIgnoreCase] Caller should've validated startIndex - count+1 >= 0");
            Contract.Assert(startIndex <= source.Length, "[TextInfo.LastIndexOfStringOrdinalIgnoreCase] Caller should've validated startIndex <= source.Length");

            // If value is Empty, the return value is startIndex
            if (value.Length == 0)
            {
                return startIndex;
            }

            // fast path
            int ret = -1;
            if (TryFastFindStringOrdinalIgnoreCase(Microsoft.Win32.Win32Native.FIND_FROMEND, source, startIndex, value, count, ref ret))
                return ret;

            // the search space within [source] ends at offset [startIndex] inclusive
            // and includes [count] characters 
            // minIndex is the first included character and is at index [startIndex - count + 1]
            int minIndex = startIndex - count + 1;
        
            // First place we can find it is start index - (value.length -1)
            if (value.Length > 0)
            {
                startIndex -= (value.Length - 1);
            }

            for (; startIndex >= minIndex; startIndex--)
            {
                if (CompareOrdinalIgnoreCaseEx(source, startIndex, value, 0, value.Length, value.Length) == 0)
                {
                    return startIndex;
                }
            }
        
            // Not found
            return -1;
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  CodePage
        //
        //  Returns the number of the code page used by this writing system.
        //  The type parameter can be any of the following values:
        //      ANSICodePage
        //      OEMCodePage
        //      MACCodePage
        //
        ////////////////////////////////////////////////////////////////////////


#if !FEATURE_CORECLR
        public virtual int ANSICodePage {
            get {
                return (this.m_cultureData.IDEFAULTANSICODEPAGE);
            }
        }

 
        public virtual int OEMCodePage {
            get {
                return (this.m_cultureData.IDEFAULTOEMCODEPAGE);
            }
        }


        public virtual int MacCodePage {
            get {
                return (this.m_cultureData.IDEFAULTMACCODEPAGE);
            }
        }


        public virtual int EBCDICCodePage {
            get {
                return (this.m_cultureData.IDEFAULTEBCDICCODEPAGE);
            }
        }
#endif


        ////////////////////////////////////////////////////////////////////////
        //
        //  LCID
        //
        //  We need a way to get an LCID from outside of the BCL. This prop is the way.
        //  NOTE: neutral cultures will cause GPS incorrect LCIDS from this
        //
        ////////////////////////////////////////////////////////////////////////

#if FEATURE_USE_LCID
        [System.Runtime.InteropServices.ComVisible(false)]
        public int LCID 
        {
            get 
            {
                // Just use the LCID from our text info name
                return CultureInfo.GetCultureInfo(this.m_textInfoName).LCID;
            }
        }
#endif
        ////////////////////////////////////////////////////////////////////////
        //
        //  CultureName
        //
        //  The name of the culture associated with the current TextInfo.
        //
        ////////////////////////////////////////////////////////////////////////
        [System.Runtime.InteropServices.ComVisible(false)]
        public string CultureName 
        {
            get 
            {
                return(this.m_textInfoName);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  IsReadOnly
        //
        //  Detect if the object is readonly.
        //
        ////////////////////////////////////////////////////////////////////////
        [System.Runtime.InteropServices.ComVisible(false)]
        public bool IsReadOnly 
        {
            get { return (m_isReadOnly); }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  Clone
        //
        //  Is the implementation of ICloneable.
        //
        ////////////////////////////////////////////////////////////////////////
        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual Object Clone()
        {
            object o = MemberwiseClone();
            ((TextInfo) o).SetReadOnlyState(false);
            return (o);
        }
        
        ////////////////////////////////////////////////////////////////////////
        //
        //  ReadOnly
        //
        //  Create a cloned readonly instance or return the input one if it is 
        //  readonly.
        //
        ////////////////////////////////////////////////////////////////////////
        [System.Runtime.InteropServices.ComVisible(false)]
        public static TextInfo ReadOnly(TextInfo textInfo) 
        {
            if (textInfo == null)       { throw new ArgumentNullException("textInfo"); }
            Contract.EndContractBlock();
            if (textInfo.IsReadOnly)    { return (textInfo); }
            
            TextInfo clonedTextInfo = (TextInfo)(textInfo.MemberwiseClone());
            clonedTextInfo.SetReadOnlyState(true);
            
            return (clonedTextInfo);
        }

        private void VerifyWritable()
        {
            if (m_isReadOnly) 
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
            }
            Contract.EndContractBlock();
        }

        internal void SetReadOnlyState(bool readOnly)
        {
            m_isReadOnly = readOnly;
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  ListSeparator
        //
        //  Returns the string used to separate items in a list.
        //
        ////////////////////////////////////////////////////////////////////////


        public virtual String ListSeparator 
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get 
            {
                if (m_listSeparator == null) {
                    m_listSeparator = this.m_cultureData.SLIST;
                }
                return (m_listSeparator);
            }

            [System.Runtime.InteropServices.ComVisible(false)]
            set 
            {
                if (value == null) 
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                m_listSeparator = value;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  ToLower
        //
        //  Converts the character or string to lower case.  Certain locales
        //  have different casing semantics from the file systems in Win32.
        //
        ////////////////////////////////////////////////////////////////////////

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual char ToLower(char c) 
        {
            if(IsAscii(c) && IsAsciiCasingSameAsInvariant)
            {
                return ToLowerAsciiInvariant(c);
            }
            return (InternalChangeCaseChar(this.m_dataHandle, this.m_handleOrigin, this.m_textInfoName, c, false));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual String ToLower(String str) 
        {
            if (str == null) { throw new ArgumentNullException("str"); }
            Contract.EndContractBlock();

            return InternalChangeCaseString(this.m_dataHandle, this.m_handleOrigin, this.m_textInfoName, str, false);

        }

        static private Char ToLowerAsciiInvariant(Char c)
        {
            if ('A' <= c && c <= 'Z')
            {
                c = (Char)(c | 0x20);
            }
            return c;
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  ToUpper
        //
        //  Converts the character or string to upper case.  Certain locales
        //  have different casing semantics from the file systems in Win32.
        //
        ////////////////////////////////////////////////////////////////////////

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual char ToUpper(char c) 
        {
            if (IsAscii(c) && IsAsciiCasingSameAsInvariant)
            {
                return ToUpperAsciiInvariant(c);
            }
            return (InternalChangeCaseChar(this.m_dataHandle, this.m_handleOrigin, this.m_textInfoName, c, true));
        }


        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual String ToUpper(String str) 
        {
            if (str == null) { throw new ArgumentNullException("str"); }
            Contract.EndContractBlock();
            return InternalChangeCaseString(this.m_dataHandle, this.m_handleOrigin, this.m_textInfoName, str, true);
        }

        static private Char ToUpperAsciiInvariant(Char c)
        {
            if ('a' <= c && c <= 'z')
            {
                c = (Char)(c & ~0x20);
            }
            return c;
        }

        static private bool IsAscii(Char c)
        {
            return c < 0x80;
        }

        private bool IsAsciiCasingSameAsInvariant
        {
            get
            {
                if (m_IsAsciiCasingSameAsInvariant == null)
                {
                    m_IsAsciiCasingSameAsInvariant =
                        CultureInfo.GetCultureInfo(m_textInfoName).CompareInfo.Compare("abcdefghijklmnopqrstuvwxyz",
                                                                             "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                                                                             CompareOptions.IgnoreCase) == 0;
                }
                return (bool)m_IsAsciiCasingSameAsInvariant;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  Equals
        //
        //  Implements Object.Equals().  Returns a boolean indicating whether
        //  or not object refers to the same CultureInfo as the current instance.
        //
        ////////////////////////////////////////////////////////////////////////


        public override bool Equals(Object obj)
        {
            TextInfo that = obj as TextInfo;
            
            if (that != null)
            {
                return this.CultureName.Equals(that.CultureName);
            }
            
            return (false);
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  GetHashCode
        //
        //  Implements Object.GetHashCode().  Returns the hash code for the
        //  CultureInfo.  The hash code is guaranteed to be the same for CultureInfo A
        //  and B where A.Equals(B) is true.
        //
        ////////////////////////////////////////////////////////////////////////


        public override int GetHashCode()
        {
            return (this.CultureName.GetHashCode());
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  ToString
        //
        //  Implements Object.ToString().  Returns a string describing the
        //  TextInfo.
        //
        ////////////////////////////////////////////////////////////////////////


        public override String ToString()
        {
            return ("TextInfo - " + this.m_cultureData.CultureName);
        }


        //
        // Titlecasing:
        // -----------
        // Titlecasing refers to a casing practice wherein the first letter of a word is an uppercase letter
        // and the rest of the letters are lowercase.  The choice of which words to titlecase in headings
        // and titles is dependent on language and local conventions.  For example, "The Merry Wives of Windor"
        // is the appropriate titlecasing of that play's name in English, with the word "of" not titlecased.
        // In German, however, the title is "Die lustigen Weiber von Windsor," and both "lustigen" and "von"
        // are not titlecased.  In French even fewer words are titlecased: "Les joyeuses commeres de Windsor."
        //
        // Moreover, the determination of what actually constitutes a word is language dependent, and this can
        // influence which letter or letters of a "word" are uppercased when titlecasing strings.  For example
        // "l'arbre" is considered two words in French, whereas "can't" is considered one word in English.
        //
        //
        // Differences between UNICODE 5.0 and the .NET Framework:
        // -------------------------------------------------------------------------------------
        // The .NET Framework previously shipped a naive titlecasing implementation.  Every word is titlecased
        // regardless of language or orthographic practice.  Furthermore, apostrophe is always considered to be
        // a word joiner as used in English.  The longterm vision is to depend on the operating system for
        // titlecasing.  Windows 7 is expected to be the first release with this feature.  On the Macintosh side,
        // titlecasing is not available as of version 10.5 of the operating system.
        //
#if !FEATURE_CORECLR
        public unsafe String ToTitleCase(String str) {
            if (str==null)  {
                throw new ArgumentNullException("str");
            }
            Contract.EndContractBlock();
            if (str.Length == 0) {
                return (str);
            }

            StringBuilder result = new StringBuilder();
            String lowercaseData = null;

            for (int i = 0; i < str.Length; i++) {
                UnicodeCategory charType;
                int charLen;

                charType = CharUnicodeInfo.InternalGetUnicodeCategory(str, i, out charLen);
                if (Char.CheckLetter(charType)) {
                    // Do the titlecasing for the first character of the word.
                    i = AddTitlecaseLetter(ref result, ref str, i, charLen) + 1;
                     
                    //
                    // Convert the characters until the end of the this word
                    // to lowercase.
                    //
                    int lowercaseStart = i;

                    //
                    // Use hasLowerCase flag to prevent from lowercasing acronyms (like "URT", "USA", etc)
                    // This is in line with Word 2000 behavior of titlecasing.
                    //
                    bool hasLowerCase = (charType == UnicodeCategory.LowercaseLetter);
                    // Use a loop to find all of the other letters following this letter.
                    while (i < str.Length) {
                        charType = CharUnicodeInfo.InternalGetUnicodeCategory(str, i, out charLen);
                        if (IsLetterCategory(charType)) {
                            if (charType == UnicodeCategory.LowercaseLetter) {
                                hasLowerCase = true;
                            }
                            i += charLen;
                        } else if (str[i] == '\'') {
                            i++;
                            if (hasLowerCase) {
                                if (lowercaseData==null) {
                                    lowercaseData = this.ToLower(str);
                                }
                                result.Append(lowercaseData, lowercaseStart, i - lowercaseStart);
                            } else {
                                result.Append(str, lowercaseStart, i - lowercaseStart);
                            }
                            lowercaseStart = i;
                            hasLowerCase = true;
                        } else if (!IsWordSeparator(charType)) {
                            // This category is considered to be part of the word.
                            // This is any category that is marked as false in wordSeprator array.
                            i+= charLen;
                        } else {
                            // A word separator. Break out of the loop.
                            break;
                        }
                    }

                    int count = i - lowercaseStart;

                    if (count>0) {
                        if (hasLowerCase) {
                            if (lowercaseData==null) {
                                lowercaseData = this.ToLower(str);
                            }
                            result.Append(lowercaseData, lowercaseStart, count);
                        } else {
                            result.Append(str, lowercaseStart, count);
                        }
                    }

                    if (i < str.Length) {
                        // not a letter, just append it
                        i = AddNonLetter(ref result, ref str, i, charLen);
                    }
                }
                else {
                    // not a letter, just append it
                    i = AddNonLetter(ref result, ref str, i, charLen);
                }
            }
            return (result.ToString());
        }

        private static int AddNonLetter(ref StringBuilder result, ref String input, int inputIndex, int charLen) {
            Contract.Assert(charLen == 1 || charLen == 2, "[TextInfo.AddNonLetter] CharUnicodeInfo.InternalGetUnicodeCategory returned an unexpected charLen!");
            if (charLen == 2) {
                // Surrogate pair
                result.Append(input[inputIndex++]);
                result.Append(input[inputIndex]);
            }
            else {
                result.Append(input[inputIndex]);
            }                   
            return inputIndex;
        }


        private int AddTitlecaseLetter(ref StringBuilder result, ref String input, int inputIndex, int charLen) {
            Contract.Assert(charLen == 1 || charLen == 2, "[TextInfo.AddTitlecaseLetter] CharUnicodeInfo.InternalGetUnicodeCategory returned an unexpected charLen!");

            // for surrogate pairs do a simple ToUpper operation on the substring
            if (charLen == 2) {
                // Surrogate pair
                result.Append( this.ToUpper(input.Substring(inputIndex, charLen)) );
                inputIndex++;
            }
            else {
                switch (input[inputIndex]) {
                    //
                    // For AppCompat, the Titlecase Case Mapping data from NDP 2.0 is used below.
                    case (char)0x01C4:  // DZ with Caron -> Dz with Caron
                    case (char)0x01C5:  // Dz with Caron -> Dz with Caron
                    case (char)0x01C6:  // dz with Caron -> Dz with Caron
                        result.Append( (char)0x01C5 );
                        break;
                    case (char)0x01C7:  // LJ -> Lj
                    case (char)0x01C8:  // Lj -> Lj
                    case (char)0x01C9:  // lj -> Lj
                        result.Append( (char)0x01C8 );
                        break;
                    case (char)0x01CA:  // NJ -> Nj
                    case (char)0x01CB:  // Nj -> Nj
                    case (char)0x01CC:  // nj -> Nj
                        result.Append( (char)0x01CB );
                        break;
                    case (char)0x01F1:  // DZ -> Dz
                    case (char)0x01F2:  // Dz -> Dz
                    case (char)0x01F3:  // dz -> Dz
                        result.Append( (char)0x01F2 );
                        break;
                    default:
                        result.Append( this.ToUpper(input[inputIndex]) );
                        break;
                }
            }                   
            return inputIndex;
        }


        //
        // Used in ToTitleCase():
        // When we find a starting letter, the following array decides if a category should be
        // considered as word seprator or not.
        //
        private const int wordSeparatorMask = 
            /* false */ (0 <<  0) | // UppercaseLetter = 0,
            /* false */ (0 <<  1) | // LowercaseLetter = 1,
            /* false */ (0 <<  2) | // TitlecaseLetter = 2,
            /* false */ (0 <<  3) | // ModifierLetter = 3,
            /* false */ (0 <<  4) | // OtherLetter = 4,
            /* false */ (0 <<  5) | // NonSpacingMark = 5,
            /* false */ (0 <<  6) | // SpacingCombiningMark = 6,
            /* false */ (0 <<  7) | // EnclosingMark = 7,
            /* false */ (0 <<  8) | // DecimalDigitNumber = 8,
            /* false */ (0 <<  9) | // LetterNumber = 9,
            /* false */ (0 << 10) | // OtherNumber = 10,
            /* true  */ (1 << 11) | // SpaceSeparator = 11,
            /* true  */ (1 << 12) | // LineSeparator = 12,
            /* true  */ (1 << 13) | // ParagraphSeparator = 13,
            /* true  */ (1 << 14) | // Control = 14,
            /* true  */ (1 << 15) | // Format = 15,
            /* false */ (0 << 16) | // Surrogate = 16,
            /* false */ (0 << 17) | // PrivateUse = 17,
            /* true  */ (1 << 18) | // ConnectorPunctuation = 18,
            /* true  */ (1 << 19) | // DashPunctuation = 19,
            /* true  */ (1 << 20) | // OpenPunctuation = 20,
            /* true  */ (1 << 21) | // ClosePunctuation = 21,
            /* true  */ (1 << 22) | // InitialQuotePunctuation = 22,
            /* true  */ (1 << 23) | // FinalQuotePunctuation = 23,
            /* true  */ (1 << 24) | // OtherPunctuation = 24,
            /* true  */ (1 << 25) | // MathSymbol = 25,
            /* true  */ (1 << 26) | // CurrencySymbol = 26,
            /* true  */ (1 << 27) | // ModifierSymbol = 27,
            /* true  */ (1 << 28) | // OtherSymbol = 28,
            /* false */ (0 << 29);  // OtherNotAssigned = 29;

        private static bool IsWordSeparator(UnicodeCategory category) {
            return (wordSeparatorMask & (1 << (int)category)) != 0;
        }

        private static bool IsLetterCategory(UnicodeCategory uc) {
            return (uc == UnicodeCategory.UppercaseLetter
                 || uc == UnicodeCategory.LowercaseLetter
                 || uc == UnicodeCategory.TitlecaseLetter
                 || uc == UnicodeCategory.ModifierLetter
                 || uc == UnicodeCategory.OtherLetter);
        }
#endif


        // IsRightToLeft
        //
        // Returns true if the dominant direction of text and UI such as the relative position of buttons and scroll bars
        //
        [System.Runtime.InteropServices.ComVisible(false)]
        public bool IsRightToLeft
        {
            get
            {
                return this.m_cultureData.IsRightToLeft;
            }
        }

        /// <internalonly/>
        void IDeserializationCallback.OnDeserialization(Object sender)
        {
            OnDeserialized();
        }

        //
        // Get case-insensitive hash code for the specified string.
        //
        // NOTENOTE: this is an internal function.  The caller should verify the string
        // is not null before calling this.  Currenlty, CaseInsensitiveHashCodeProvider
        // does that.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe int GetCaseInsensitiveHashCode(String str)
        {
            return GetCaseInsensitiveHashCode(str, false, 0);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe int GetCaseInsensitiveHashCode(String str, bool forceRandomizedHashing, long additionalEntropy)
        {
            // Validate inputs
            if (str==null) 
            {
                 throw new ArgumentNullException("str");
            }
            Contract.EndContractBlock();

            // Return our result
            return (InternalGetCaseInsHash(this.m_dataHandle, this.m_handleOrigin, this.m_textInfoName, str, forceRandomizedHashing, additionalEntropy));
        }

        // Change case (ToUpper/ToLower) -- COMNlsInfo::InternalChangeCaseChar
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static unsafe extern char InternalChangeCaseChar(IntPtr handle, IntPtr handleOrigin, String localeName, char ch, bool isToUpper);
        
        // Change case (ToUpper/ToLower) -- COMNlsInfo::InternalChangeCaseString
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static unsafe extern String InternalChangeCaseString(IntPtr handle, IntPtr handleOrigin, String localeName, String str, bool isToUpper);

        // Get case insensitive hash -- ComNlsInfo::InternalGetCaseInsHash
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static unsafe extern int InternalGetCaseInsHash(IntPtr handle, IntPtr handleOrigin, String localeName, String str, bool forceRandomizedHashing, long additionalEntropy);

        // Call ::CompareStringOrdinal -- ComNlsInfo::InternalCompareStringOrdinalIgnoreCase
        // Start at indexes and compare for length characters (or remainder of string if length == -1)
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static unsafe extern int InternalCompareStringOrdinalIgnoreCase(String string1, int index1, String string2, int index2, int length1, int length2);

        // ComNlsInfo::InternalTryFindStringOrdinalIgnoreCase attempts a faster IndexOf/LastIndexOf OrdinalIgnoreCase using a kernel function.
        // Returns true if FindStringOrdinal was handled, with foundIndex set to the target's index into the source
        // Returns false when FindStringOrdinal wasn't handled
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe extern bool InternalTryFindStringOrdinalIgnoreCase(int searchFlags, String source, int sourceCount, int startIndex, String target, int targetCount, ref int foundIndex);
    }

}


