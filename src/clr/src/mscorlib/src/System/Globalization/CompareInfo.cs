// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////
//
//
//
//  Purpose:  This class implements a set of methods for comparing
//            strings.
//
//
////////////////////////////////////////////////////////////////////////////

namespace System.Globalization {

    //
    // We pass all of the sorting calls to the native side, preferrably to the OS to do
    // the actual work.
    //

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Threading;
    using System.Security.Permissions;
    using Microsoft.Win32;
    using System.Security;
    using System.Diagnostics.Contracts;

    //
    //  Options can be used during string comparison.
    //
    //  Native implementation (COMNlsInfo.cpp & SortingTable.cpp) relies on the values of these,
    //  If you change the values below, be sure to change the values in native part as well.
    //


[Serializable]
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum CompareOptions
    {
        None                = 0x00000000,
        IgnoreCase          = 0x00000001,
        IgnoreNonSpace      = 0x00000002,
        IgnoreSymbols       = 0x00000004,
        IgnoreKanaType      = 0x00000008,   // ignore kanatype
        IgnoreWidth         = 0x00000010,   // ignore width
        OrdinalIgnoreCase   = 0x10000000,   // This flag can not be used with other flags.
        StringSort          = 0x20000000,   // use string sort method
        Ordinal             = 0x40000000,   // This flag can not be used with other flags.

        // StopOnNull      = 0x10000000,

        // StopOnNull is defined in SortingTable.h, but we didn't enable this option here.
        // Do not use this value for other flags accidentally.
    }


    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public partial class CompareInfo : IDeserializationCallback
    {
        // Mask used to check if IndexOf()/LastIndexOf()/IsPrefix()/IsPostfix() has the right flags.
        private const CompareOptions ValidIndexMaskOffFlags =
            ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace |
              CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType);

        // Mask used to check if Compare() has the right flags.
        private const CompareOptions ValidCompareMaskOffFlags =
            ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace |
              CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType | CompareOptions.StringSort);

        // Mask used to check if GetHashCodeOfString() has the right flags.
        private const CompareOptions ValidHashCodeOfStringMaskOffFlags =
            ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace |
              CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType);

        //
        // CompareInfos have an interesting identity.  They are attached to the locale that created them,
        // ie: en-US would have an en-US sort.  For haw-US (custom), then we serialize it as haw-US.
        // The interesting part is that since haw-US doesn't have its own sort, it has to point at another
        // locale, which is what SCOMPAREINFO does.

        [OptionalField(VersionAdded = 2)]
        private String m_name;  // The name used to construct this CompareInfo

        [NonSerialized] 
        private String m_sortName; // The name that defines our behavior

        [NonSerialized]
        private IntPtr m_dataHandle;

        [NonSerialized]
        private IntPtr m_handleOrigin;

        ////////////////////////////////////////////////////////////////////////
        //
        //  CompareInfo Constructor
        //
        //
        ////////////////////////////////////////////////////////////////////////
        // Constructs an instance that most closely corresponds to the NLS locale
        // identifier.
        internal CompareInfo(CultureInfo culture)
        {
            this.m_name = culture.m_name;
            this.m_sortName = culture.SortName;

            IntPtr handleOrigin;
            this.m_dataHandle = InternalInitSortHandle(m_sortName, out handleOrigin);
            this.m_handleOrigin = handleOrigin;
        }

        /*=================================GetCompareInfo==========================
        **Action: Get the CompareInfo constructed from the data table in the specified assembly for the specified culture.
        **       Warning: The assembly versioning mechanism is dead!
        **Returns: The CompareInfo for the specified culture.
        **Arguments:
        **   culture     the ID of the culture
        **   assembly   the assembly which contains the sorting table.
        **Exceptions:
        **  ArugmentNullException when the assembly is null
        **  ArgumentException if culture is invalid.
        ============================================================================*/
#if FEATURE_USE_LCID
        // Assembly constructor should be deprecated, we don't act on the assembly information any more
        public static CompareInfo GetCompareInfo(int culture, Assembly assembly){
            // Parameter checking.
            if (assembly == null) {
                throw new ArgumentNullException("assembly");
            }
            if (assembly!=typeof(Object).Module.Assembly) {
                throw new ArgumentException(Environment.GetResourceString("Argument_OnlyMscorlib"));
            }
            Contract.EndContractBlock();

            return GetCompareInfo(culture);
        }
#endif


        /*=================================GetCompareInfo==========================
        **Action: Get the CompareInfo constructed from the data table in the specified assembly for the specified culture.
        **       The purpose of this method is to provide version for CompareInfo tables.
        **Returns: The CompareInfo for the specified culture.
        **Arguments:
        **   name    the name of the culture
        **   assembly   the assembly which contains the sorting table.
        **Exceptions:
        **  ArugmentNullException when the assembly is null
        **  ArgumentException if name is invalid.
        ============================================================================*/
        // Assembly constructor should be deprecated, we don't act on the assembly information any more
        public static CompareInfo GetCompareInfo(String name, Assembly assembly){
            if (name == null || assembly == null) {
                throw new ArgumentNullException(name == null ? "name" : "assembly");
            }
            Contract.EndContractBlock();

            if (assembly!=typeof(Object).Module.Assembly) {
                throw new ArgumentException(Environment.GetResourceString("Argument_OnlyMscorlib"));
            }

            return GetCompareInfo(name);
        }

        /*=================================GetCompareInfo==========================
        **Action: Get the CompareInfo for the specified culture.
        ** This method is provided for ease of integration with NLS-based software.
        **Returns: The CompareInfo for the specified culture.
        **Arguments:
        **   culture    the ID of the culture.
        **Exceptions:
        **  ArgumentException if culture is invalid.
        ============================================================================*/

#if FEATURE_USE_LCID
        // People really shouldn't be calling LCID versions, no custom support
        public static CompareInfo GetCompareInfo(int culture)
        {
            if (CultureData.IsCustomCultureId(culture))
            {
                // Customized culture cannot be created by the LCID.
                throw new ArgumentException(Environment.GetResourceString("Argument_CustomCultureCannotBePassedByNumber", "culture"));
            }

            return CultureInfo.GetCultureInfo(culture).CompareInfo;
        }
#endif

        /*=================================GetCompareInfo==========================
        **Action: Get the CompareInfo for the specified culture.
        **Returns: The CompareInfo for the specified culture.
        **Arguments:
        **   name    the name of the culture.
        **Exceptions:
        **  ArgumentException if name is invalid.
        ============================================================================*/

        public static CompareInfo GetCompareInfo(String name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            Contract.EndContractBlock();

            return CultureInfo.GetCultureInfo(name).CompareInfo;
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public static bool IsSortable(char ch) {
            return(IsSortable(ch.ToString()));
        }

        [System.Security.SecuritySafeCritical]
        [System.Runtime.InteropServices.ComVisible(false)]
        public static bool IsSortable(String text) {
            if (text == null) {
                // A null param is invalid here.
                throw new ArgumentNullException("text");
            }

            if (0 == text.Length) {
                // A zero length string is not invalid, but it is also not sortable.
                return(false);
            }

            CompareInfo c = CultureInfo.InvariantCulture.CompareInfo;

            return (InternalIsSortable(c.m_dataHandle, c.m_handleOrigin, c.m_sortName, text, text.Length));
        }


#region Serialization
        // the following fields are defined to keep the compatibility with Whidbey.
        // don't change/remove the names/types of these fields.
#if FEATURE_USE_LCID
                [OptionalField(VersionAdded = 1)]
                private int win32LCID;             // mapped sort culture id of this instance
                private int culture;               // the culture ID used to create this instance.
#endif
        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            this.m_name          = null;
        }

        private void OnDeserialized()
        {
            CultureInfo ci;
            // If we didn't have a name, use the LCID
            if (this.m_name == null)
            {
#if FEATURE_USE_LCID
                // From whidbey, didn't have a name
                ci = CultureInfo.GetCultureInfo(this.culture);
                this.m_name = ci.m_name;
                this.m_sortName = ci.SortName;
#endif
            }
            else
            {
                ci = CultureInfo.GetCultureInfo(m_name);
                this.m_sortName = ci.SortName;
            }

            IntPtr handleOrigin;
            this.m_dataHandle = InternalInitSortHandle(m_sortName, out handleOrigin);
            this.m_handleOrigin = handleOrigin;

        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            OnDeserialized();
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx)
        {
#if FEATURE_USE_LCID
            // This is merely for serialization compatibility with Whidbey/Orcas, it can go away when we don't want that compat any more.
            culture = CultureInfo.GetCultureInfo(this.Name).LCID; // This is the lcid of the constructing culture (still have to dereference to get target sort)
            Contract.Assert(m_name != null, "CompareInfo.OnSerializing - expected m_name to be set already");
#endif
        }

        void IDeserializationCallback.OnDeserialization(Object sender)
        {
            OnDeserialized();
        }

#endregion Serialization


        ///////////////////////////----- Name -----/////////////////////////////////
        //
        //  Returns the name of the culture (well actually, of the sort).
        //  Very important for providing a non-LCID way of identifying
        //  what the sort is.
        //
        //  Note that this name isn't dereferenced in case the CompareInfo is a different locale
        //  which is consistent with the behaviors of earlier versions.  (so if you ask for a sort
        //  and the locale's changed behavior, then you'll get changed behavior, which is like
        //  what happens for a version update)
        //
        ////////////////////////////////////////////////////////////////////////

        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual String Name
        {
            get
            {
                Contract.Assert(m_name != null, "CompareInfo.Name Expected m_name to be set");
                if (m_name == "zh-CHT" || m_name == "zh-CHS")
                {
                    return m_name;
                }

                return (m_sortName);
            }
        }

        // These flags are used in the native Win32. so we need to map the managed options to those flags
        private const int LINGUISTIC_IGNORECASE      = 0x00000010;       // linguistically appropriate 'ignore case'
        private const int NORM_IGNORECASE            = 0x00000001;       // Ignores case.  (use LINGUISTIC_IGNORECASE instead)
        private const int NORM_IGNOREKANATYPE        = 0x00010000;       // Does not differentiate between Hiragana and Katakana characters. Corresponding Hiragana and Katakana will compare as equal.
        private const int LINGUISTIC_IGNOREDIACRITIC = 0x00000020;       // linguistically appropriate 'ignore nonspace'
        private const int NORM_IGNORENONSPACE        = 0x00000002;       // Ignores nonspacing. This flag also removes Japanese accent characters.  (use LINGUISTIC_IGNOREDIACRITIC instead)
        private const int NORM_IGNORESYMBOLS         = 0x00000004;       // Ignores symbols.
        private const int NORM_IGNOREWIDTH           = 0x00020000;       // Does not differentiate between a single-byte character and the same character as a double-byte character.
        private const int SORT_STRINGSORT            = 0x00001000;       // Treats punctuation the same as symbols.
        private const int COMPARE_OPTIONS_ORDINAL    = 0x40000000;       // Ordinal (handled by Comnlsinfo)
        internal const int NORM_LINGUISTIC_CASING    = 0x08000000;       // use linguistic rules for casing

        
        private const int RESERVED_FIND_ASCII_STRING = 0x20000000;       // This flag used only to tell the sorting DLL can assume the string characters are in ASCII.

        [Pure]
        internal static int GetNativeCompareFlags(CompareOptions options)
        {
            // some NLS VM functions can handle COMPARE_OPTIONS_ORDINAL
            // in which case options should be simply cast to int instead of using this function
            // Does not look like the best approach to me but for now I am going to leave it as it is
            Contract.Assert(options != CompareOptions.OrdinalIgnoreCase, "[CompareInfo.GetNativeCompareFlags]CompareOptions.OrdinalIgnoreCase should be handled separately");

            // Use "linguistic casing" by default (load the culture's casing exception tables)
            int nativeCompareFlags = NORM_LINGUISTIC_CASING;

            if ((options & CompareOptions.IgnoreCase)       != 0) { nativeCompareFlags |= NORM_IGNORECASE;        }
            if ((options & CompareOptions.IgnoreKanaType)   != 0) { nativeCompareFlags |= NORM_IGNOREKANATYPE;    }
            if ((options & CompareOptions.IgnoreNonSpace)   != 0) { nativeCompareFlags |= NORM_IGNORENONSPACE;    }
            if ((options & CompareOptions.IgnoreSymbols)    != 0) { nativeCompareFlags |= NORM_IGNORESYMBOLS;     }
            if ((options & CompareOptions.IgnoreWidth)      != 0) { nativeCompareFlags |= NORM_IGNOREWIDTH;       }
            if ((options & CompareOptions.StringSort)       != 0) { nativeCompareFlags |= SORT_STRINGSORT;        }

            // Suffix & Prefix shouldn't use this, make sure to turn off the NORM_LINGUISTIC_CASING flag
            if (options == CompareOptions.Ordinal)                { nativeCompareFlags = COMPARE_OPTIONS_ORDINAL; }

            Contract.Assert(((options & ~(CompareOptions.IgnoreCase |
                                          CompareOptions.IgnoreKanaType |
                                          CompareOptions.IgnoreNonSpace |
                                          CompareOptions.IgnoreSymbols |
                                          CompareOptions.IgnoreWidth |
                                          CompareOptions.StringSort)) == 0) ||
                             (options == CompareOptions.Ordinal), "[CompareInfo.GetNativeCompareFlags]Expected all flags to be handled");

            Contract.Assert((nativeCompareFlags & RESERVED_FIND_ASCII_STRING) == 0, "[CompareInfo.GetNativeCompareFlags] RESERVED_FIND_ASCII_STRING shouldn't be set here");

            return nativeCompareFlags;
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  Compare
        //
        //  Compares the two strings with the given options.  Returns 0 if the
        //  two strings are equal, a number less than 0 if string1 is less
        //  than string2, and a number greater than 0 if string1 is greater
        //  than string2.
        //
        ////////////////////////////////////////////////////////////////////////


        public virtual int Compare(String string1, String string2)
        {
            return (Compare(string1, string2, CompareOptions.None));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual int Compare(String string1, String string2, CompareOptions options){

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return String.Compare(string1, string2, StringComparison.OrdinalIgnoreCase);
            }

            // Verify the options before we do any real comparison.
            if ((options & CompareOptions.Ordinal) != 0)
            {
                if (options != CompareOptions.Ordinal)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_CompareOptionOrdinal"), "options");
                        }
                return String.CompareOrdinal(string1, string2);
                    }

            if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            }

            //Our paradigm is that null sorts less than any other string and
            //that two nulls sort as equal.
            if (string1 == null) {
                if (string2 == null) {
                    return (0);     // Equal
                }
                return (-1);    // null < non-null
            }
            if (string2 == null) {
                return (1);     // non-null > null
            }

            return InternalCompareString(m_dataHandle, m_handleOrigin, m_sortName, string1, 0, string1.Length, string2, 0, string2.Length, GetNativeCompareFlags(options));
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  Compare
        //
        //  Compares the specified regions of the two strings with the given
        //  options.
        //  Returns 0 if the two strings are equal, a number less than 0 if
        //  string1 is less than string2, and a number greater than 0 if
        //  string1 is greater than string2.
        //
        ////////////////////////////////////////////////////////////////////////


        public unsafe virtual int Compare(String string1, int offset1, int length1, String string2, int offset2, int length2)
        {
            return Compare(string1, offset1, length1, string2, offset2, length2, 0);
        }


        public unsafe virtual int Compare(String string1, int offset1, String string2, int offset2, CompareOptions options)
        {
            return Compare(string1, offset1, string1 == null ? 0 : string1.Length-offset1,
                           string2, offset2, string2 == null ? 0 : string2.Length-offset2, options);
        }


        public unsafe virtual int Compare(String string1, int offset1, String string2, int offset2)
        {
            return Compare(string1, offset1, string2, offset2, 0);
        }


        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual int Compare(String string1, int offset1, int length1, String string2, int offset2, int length2, CompareOptions options)
        {
            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                int result = String.Compare(string1, offset1, string2, offset2, length1<length2 ? length1 : length2, StringComparison.OrdinalIgnoreCase);
                if ((length1 != length2) && result == 0)
                    return (length1 > length2? 1: -1);
                return (result);
            }

            // Verify inputs
            if (length1 < 0 || length2 < 0)
            {
                throw new ArgumentOutOfRangeException((length1 < 0) ? "length1" : "length2", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            }
            if (offset1 < 0 || offset2 < 0)
            {
                throw new ArgumentOutOfRangeException((offset1 < 0) ? "offset1" : "offset2", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            }
            if (offset1 > (string1 == null ? 0 : string1.Length) - length1)
            {
                throw new ArgumentOutOfRangeException("string1", Environment.GetResourceString("ArgumentOutOfRange_OffsetLength"));
            }
            if (offset2 > (string2 == null ? 0 : string2.Length) - length2)
            {
                throw new ArgumentOutOfRangeException("string2", Environment.GetResourceString("ArgumentOutOfRange_OffsetLength"));
            }
            if ((options & CompareOptions.Ordinal) != 0)
            {
                if (options != CompareOptions.Ordinal)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_CompareOptionOrdinal"),
                                                "options");
                }
            }
            else if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            }

            //
            // Check for the null case.
            //
            if (string1 == null)
            {
                if (string2 == null)
                {
                    return (0);
                }
                return (-1);
            }
            if (string2 == null)
            {
                return (1);
            }

            if (options == CompareOptions.Ordinal)
            {
                return string.CompareOrdinalHelper(string1, offset1, length1, string2, offset2, length2);
            }
            return InternalCompareString(this.m_dataHandle, this.m_handleOrigin, this.m_sortName, 
                                         string1, offset1, length1, 
                                         string2, offset2, length2, 
                                         GetNativeCompareFlags(options));
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  IsPrefix
        //
        //  Determines whether prefix is a prefix of string.  If prefix equals
        //  String.Empty, true is returned.
        //
        ////////////////////////////////////////////////////////////////////////


        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual bool IsPrefix(String source, String prefix, CompareOptions options)
        {
            if (source == null || prefix == null) {
                throw new ArgumentNullException((source == null ? "source" : "prefix"),
                    Environment.GetResourceString("ArgumentNull_String"));
            }
            Contract.EndContractBlock();
            int prefixLen = prefix.Length;

            if (prefixLen == 0)
            {
                return (true);
            }

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            if (options == CompareOptions.Ordinal)
            {
                return source.StartsWith(prefix, StringComparison.Ordinal);
            }

            if ((options & ValidIndexMaskOffFlags) != 0) {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            }


            // to let the sorting DLL do the call optimization in case of Ascii strings, we check if the strings are in Ascii and then send the flag RESERVED_FIND_ASCII_STRING  to 
            // the sorting DLL API SortFindString so sorting DLL don't have to check if the string is Ascii with every call to SortFindString.

            return (InternalFindNLSStringEx(
                        m_dataHandle, m_handleOrigin, m_sortName, 
                        GetNativeCompareFlags(options) | Win32Native.FIND_STARTSWITH | ((source.IsAscii() && prefix.IsAscii()) ? RESERVED_FIND_ASCII_STRING : 0),
                        source, source.Length, 0, prefix, prefix.Length) > -1);
        }

        public virtual bool IsPrefix(String source, String prefix)
        {
            return (IsPrefix(source, prefix, 0));
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  IsSuffix
        //
        //  Determines whether suffix is a suffix of string.  If suffix equals
        //  String.Empty, true is returned.
        //
        ////////////////////////////////////////////////////////////////////////


        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual bool IsSuffix(String source, String suffix, CompareOptions options)
        {
            if (source == null || suffix == null) {
                throw new ArgumentNullException((source == null ? "source" : "suffix"),
                    Environment.GetResourceString("ArgumentNull_String"));
            }
            Contract.EndContractBlock();
            int suffixLen = suffix.Length;

            if (suffixLen == 0)
            {
                return (true);
            }

            if (options == CompareOptions.OrdinalIgnoreCase) {
                return source.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
            }

            if (options == CompareOptions.Ordinal) {
                return source.EndsWith(suffix, StringComparison.Ordinal);
            }

            if ((options & ValidIndexMaskOffFlags) != 0) {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            }

            // to let the sorting DLL do the call optimization in case of Ascii strings, we check if the strings are in Ascii and then send the flag RESERVED_FIND_ASCII_STRING  to 
            // the sorting DLL API SortFindString so sorting DLL don't have to check if the string is Ascii with every call to SortFindString.
            return InternalFindNLSStringEx(
                        m_dataHandle, m_handleOrigin, m_sortName, 
                        GetNativeCompareFlags(options) | Win32Native.FIND_ENDSWITH | ((source.IsAscii() && suffix.IsAscii()) ? RESERVED_FIND_ASCII_STRING : 0),
                        source, source.Length, source.Length - 1, suffix, suffix.Length) >= 0;
        }


        public virtual bool IsSuffix(String source, String suffix)
        {
            return (IsSuffix(source, suffix, 0));
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  IndexOf
        //
        //  Returns the first index where value is found in string.  The
        //  search starts from startIndex and ends at endIndex.  Returns -1 if
        //  the specified value is not found.  If value equals String.Empty,
        //  startIndex is returned.  Throws IndexOutOfRange if startIndex or
        //  endIndex is less than zero or greater than the length of string.
        //  Throws ArgumentException if value is null.
        //
        ////////////////////////////////////////////////////////////////////////


        public unsafe virtual int IndexOf(String source, char value)
        {
            if (source==null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();

            return IndexOf(source, value, 0, source.Length, CompareOptions.None);
        }


        public unsafe virtual int IndexOf(String source, String value)
        {
            if (source==null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();

            return IndexOf(source, value, 0, source.Length, CompareOptions.None);
        }


        public unsafe virtual int IndexOf(String source, char value, CompareOptions options)
        {
            if (source==null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();

            return IndexOf(source, value, 0, source.Length, options);
        }


        public unsafe virtual int IndexOf(String source, String value, CompareOptions options)
        {
            if (source==null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();

            return IndexOf(source, value, 0, source.Length, options);
        }


        public unsafe virtual int IndexOf(String source, char value, int startIndex)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();

            return IndexOf(source, value, startIndex, source.Length - startIndex, CompareOptions.None);
        }


        public unsafe virtual int IndexOf(String source, String value, int startIndex)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();

            return IndexOf(source, value, startIndex, source.Length - startIndex, CompareOptions.None);
        }


        public unsafe virtual int IndexOf(String source, char value, int startIndex, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();

            return IndexOf(source, value, startIndex, source.Length - startIndex, options);
        }


        public unsafe virtual int IndexOf(String source, String value, int startIndex, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();

            return IndexOf(source, value, startIndex, source.Length - startIndex, options);
        }


        public unsafe virtual int IndexOf(String source, char value, int startIndex, int count)
        {
            return IndexOf(source, value, startIndex, count, CompareOptions.None);
        }


        public unsafe virtual int IndexOf(String source, String value, int startIndex, int count)
        {
            return IndexOf(source, value, startIndex, count, CompareOptions.None);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual int IndexOf(String source, char value, int startIndex, int count, CompareOptions options)
        {
            // Validate inputs
            if (source == null)
                throw new ArgumentNullException("source");

            if (startIndex < 0 || startIndex > source.Length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));

            if (count < 0 || startIndex > source.Length - count)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            Contract.EndContractBlock();

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.IndexOf(value.ToString(), startIndex, count, StringComparison.OrdinalIgnoreCase);
            }

            // Validate CompareOptions
            // Ordinal can't be selected with other flags
            if ((options & ValidIndexMaskOffFlags) != 0 && (options != CompareOptions.Ordinal))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");

            // to let the sorting DLL do the call optimization in case of Ascii strings, we check if the strings are in Ascii and then send the flag RESERVED_FIND_ASCII_STRING  to 
            // the sorting DLL API SortFindString so sorting DLL don't have to check if the string is Ascii with every call to SortFindString.
            return InternalFindNLSStringEx(
                        m_dataHandle, m_handleOrigin, m_sortName, 
                        GetNativeCompareFlags(options) | Win32Native.FIND_FROMSTART | ((source.IsAscii() && (value <= '\x007f')) ? RESERVED_FIND_ASCII_STRING : 0),
                        source, count, startIndex, new String(value, 1), 1);
        }


        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual int IndexOf(String source, String value, int startIndex, int count, CompareOptions options)
        {
            // Validate inputs
            if (source == null)
                throw new ArgumentNullException("source");
            if (value == null)
                throw new ArgumentNullException("value");

            if (startIndex > source.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }
            Contract.EndContractBlock();

            // In Everett we used to return -1 for empty string even if startIndex is negative number so we keeping same behavior here.
            // We return 0 if both source and value are empty strings for Everett compatibility too.
            if (source.Length == 0)
            {
                if (value.Length == 0)
                {
                    return 0;
                }
                return -1;
            }

            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (count < 0 || startIndex > source.Length - count)
                throw new ArgumentOutOfRangeException("count",Environment.GetResourceString("ArgumentOutOfRange_Count"));

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.IndexOf(value, startIndex, count, StringComparison.OrdinalIgnoreCase);
            }

            // Validate CompareOptions
            // Ordinal can't be selected with other flags
            if ((options & ValidIndexMaskOffFlags) != 0 && (options != CompareOptions.Ordinal))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");

            // to let the sorting DLL do the call optimization in case of Ascii strings, we check if the strings are in Ascii and then send the flag RESERVED_FIND_ASCII_STRING  to 
            // the sorting DLL API SortFindString so sorting DLL don't have to check if the string is Ascii with every call to SortFindString.
            return InternalFindNLSStringEx(
                        m_dataHandle, m_handleOrigin, m_sortName, 
                        GetNativeCompareFlags(options) | Win32Native.FIND_FROMSTART | ((source.IsAscii() && value.IsAscii()) ? RESERVED_FIND_ASCII_STRING : 0),
                        source, count, startIndex, value, value.Length);
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  LastIndexOf
        //
        //  Returns the last index where value is found in string.  The
        //  search starts from startIndex and ends at endIndex.  Returns -1 if
        //  the specified value is not found.  If value equals String.Empty,
        //  endIndex is returned.  Throws IndexOutOfRange if startIndex or
        //  endIndex is less than zero or greater than the length of string.
        //  Throws ArgumentException if value is null.
        //
        ////////////////////////////////////////////////////////////////////////


        public unsafe virtual int LastIndexOf(String source, char value)
        {
            if (source==null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();

            // Can't start at negative index, so make sure we check for the length == 0 case.
            return LastIndexOf(source, value, source.Length - 1,
                source.Length, CompareOptions.None);
        }


        public virtual int LastIndexOf(String source, String value)
        {
            if (source==null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();

            // Can't start at negative index, so make sure we check for the length == 0 case.
            return LastIndexOf(source, value, source.Length - 1,
                source.Length, CompareOptions.None);
        }


        public virtual int LastIndexOf(String source, char value, CompareOptions options)
        {
            if (source==null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();

            // Can't start at negative index, so make sure we check for the length == 0 case.
            return LastIndexOf(source, value, source.Length - 1,
                source.Length, options);
        }

        public unsafe virtual int LastIndexOf(String source, String value, CompareOptions options)
        {
            if (source==null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();

            // Can't start at negative index, so make sure we check for the length == 0 case.
            return LastIndexOf(source, value, source.Length - 1,
                source.Length, options);
        }


        public unsafe virtual int LastIndexOf(String source, char value, int startIndex)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, CompareOptions.None);
        }


        public unsafe virtual int LastIndexOf(String source, String value, int startIndex)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, CompareOptions.None);
        }


        public unsafe virtual int LastIndexOf(String source, char value, int startIndex, CompareOptions options)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, options);
        }


        public unsafe virtual int LastIndexOf(String source, String value, int startIndex, CompareOptions options)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, options);
        }


        public unsafe virtual int LastIndexOf(String source, char value, int startIndex, int count)
        {
            return LastIndexOf(source, value, startIndex, count, CompareOptions.None);
        }


        public unsafe virtual int LastIndexOf(String source, String value, int startIndex, int count)
        {
            return LastIndexOf(source, value, startIndex, count, CompareOptions.None);
        }


        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual int LastIndexOf(String source, char value, int startIndex, int count, CompareOptions options)
        {
            // Verify Arguments
            if (source==null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();

            // Validate CompareOptions
            // Ordinal can't be selected with other flags
            if ((options & ValidIndexMaskOffFlags) != 0 &&
                (options != CompareOptions.Ordinal) &&
                (options != CompareOptions.OrdinalIgnoreCase))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");

            // Special case for 0 length input strings
            if (source.Length == 0 && (startIndex == -1 || startIndex == 0))
                return -1;

            // Make sure we're not out of range
            if (startIndex < 0 || startIndex > source.Length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));

            // Make sure that we allow startIndex == source.Length
            if (startIndex == source.Length)
            {
                startIndex--;
                if (count > 0)
                    count--;
            }

            // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.LastIndexOf(value.ToString(), startIndex, count, StringComparison.OrdinalIgnoreCase);
            }

            // to let the sorting DLL do the call optimization in case of Ascii strings, we check if the strings are in Ascii and then send the flag RESERVED_FIND_ASCII_STRING  to 
            // the sorting DLL API SortFindString so sorting DLL don't have to check if the string is Ascii with every call to SortFindString.
            return InternalFindNLSStringEx(
                        m_dataHandle, m_handleOrigin, m_sortName, 
                        GetNativeCompareFlags(options) | Win32Native.FIND_FROMEND | ((source.IsAscii() && (value <= '\x007f')) ? RESERVED_FIND_ASCII_STRING : 0),
                        source, count, startIndex, new String(value, 1), 1);
        }


        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual int LastIndexOf(String source, String value, int startIndex, int count, CompareOptions options)
        {
            // Verify Arguments
            if (source == null)
                throw new ArgumentNullException("source");
            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();

            // Validate CompareOptions
            // Ordinal can't be selected with other flags
            if ((options & ValidIndexMaskOffFlags) != 0 &&
                (options != CompareOptions.Ordinal) &&
                (options != CompareOptions.OrdinalIgnoreCase))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");

            // Special case for 0 length input strings
            if (source.Length == 0 && (startIndex == -1 || startIndex == 0))
                return (value.Length == 0) ? 0 : -1;

            // Make sure we're not out of range
            if (startIndex < 0 || startIndex > source.Length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));

            // Make sure that we allow startIndex == source.Length
            if (startIndex == source.Length)
            {
                startIndex--;
                if (count > 0)
                    count--;

                // If we are looking for nothing, just return 0
                if (value.Length == 0 && count >= 0 && startIndex - count + 1 >= 0)
                    return startIndex;
            }

            // 2nd half of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.LastIndexOf(value, startIndex, count, StringComparison.OrdinalIgnoreCase);
            }

            // to let the sorting DLL do the call optimization in case of Ascii strings, we check if the strings are in Ascii and then send the flag RESERVED_FIND_ASCII_STRING  to 
            // the sorting DLL API SortFindString so sorting DLL don't have to check if the string is Ascii with every call to SortFindString.
            return InternalFindNLSStringEx(
                        m_dataHandle, m_handleOrigin, m_sortName, 
                        GetNativeCompareFlags(options) | Win32Native.FIND_FROMEND | ((source.IsAscii() && value.IsAscii()) ? RESERVED_FIND_ASCII_STRING : 0),
                        source, count, startIndex, value, value.Length);
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  GetSortKey
        //
        //  Gets the SortKey for the given string with the given options.
        //
        ////////////////////////////////////////////////////////////////////////
        public unsafe virtual SortKey GetSortKey(String source, CompareOptions options)
        {
            return CreateSortKey(source, options);
        }


        public unsafe virtual SortKey GetSortKey(String source)
        {
            return CreateSortKey(source, CompareOptions.None);
        }

        [System.Security.SecuritySafeCritical]
        private SortKey CreateSortKey(String source, CompareOptions options)
        {
            if (source==null) { throw new ArgumentNullException("source"); }
            Contract.EndContractBlock();

            // Mask used to check if we have the right flags.
            const CompareOptions ValidSortkeyCtorMaskOffFlags = ~(CompareOptions.IgnoreCase |
                                                                  CompareOptions.IgnoreSymbols |
                                                                  CompareOptions.IgnoreNonSpace |
                                                                  CompareOptions.IgnoreWidth |
                                                                  CompareOptions.IgnoreKanaType |
                                                                  CompareOptions.StringSort);

            if ((options & ValidSortkeyCtorMaskOffFlags) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            }
            byte[] keyData = null;
            // The OS doesn't have quite the same behavior so we have to test for empty inputs
            if (String.IsNullOrEmpty(source))
            {
                // Empty strings get an empty sort key
                keyData = EmptyArray<Byte>.Value;
                // Fake value to test though so we can verify our flags
                source = "\x0000";
            }

            int flags = GetNativeCompareFlags(options);

            // Go ahead and call the OS
            // First get the count
            int length = InternalGetSortKey(m_dataHandle, m_handleOrigin, m_sortName, flags, source, source.Length, null, 0);

            // If there was an error, return an error
            if (length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "source");
            }

            // If input was empty, return the empty byte[] we made earlier and skip this
            if (keyData == null)
            {
                // Make an appropriate byte array
                keyData  = new byte[length];

                // Fill up the array
                length = InternalGetSortKey(m_dataHandle, m_handleOrigin, m_sortName, flags, source, source.Length, keyData, keyData.Length);
            }
            else
            {
                source = String.Empty; // back to original
            }

            return new SortKey(Name, source, options, keyData);
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  Equals
        //
        //  Implements Object.Equals().  Returns a boolean indicating whether
        //  or not object refers to the same CompareInfo as the current
        //  instance.
        //
        ////////////////////////////////////////////////////////////////////////


        public override bool Equals(Object value)
        {
            CompareInfo that = value as CompareInfo;

            if (that != null)
            {
                return this.Name == that.Name;
            }

            return (false);
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  GetHashCode
        //
        //  Implements Object.GetHashCode().  Returns the hash code for the
        //  CompareInfo.  The hash code is guaranteed to be the same for
        //  CompareInfo A and B where A.Equals(B) is true.
        //
        ////////////////////////////////////////////////////////////////////////


        public override int GetHashCode()
        {
            return (this.Name.GetHashCode());
        }

        //
        // return hash value for the string according to the input CompareOptions 
        //

        public virtual int GetHashCode(string source, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (options == CompareOptions.Ordinal)
            {
                return source.GetHashCode();
            }

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return TextInfo.GetHashCodeOrdinalIgnoreCase(source);
            }

            //
            // GetHashCodeOfString does more parameters validation. basically will throw when  
            // having Ordinal, OrdinalIgnoreCase and StringSort
            //

            return GetHashCodeOfString(source, options, false, 0);
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  GetHashCodeOfString
        //
        //  This internal method allows a method that allows the equivalent of creating a Sortkey for a
        //  string from CompareInfo, and generate a hashcode value from it.  It is not very convenient
        //  to use this method as is and it creates an unnecessary Sortkey object that will be GC'ed.
        //
        //  The hash code is guaranteed to be the same for string A and B where A.Equals(B) is true and both
        //  the CompareInfo and the CompareOptions are the same. If two different CompareInfo objects
        //  treat the string the same way, this implementation will treat them differently (the same way that
        //  Sortkey does at the moment).
        //
        //  This method will never be made public itself, but public consumers of it could be created, e.g.:
        //
        //      string.GetHashCode(CultureInfo)
        //      string.GetHashCode(CompareInfo)
        //      string.GetHashCode(CultureInfo, CompareOptions)
        //      string.GetHashCode(CompareInfo, CompareOptions)
        //      etc.
        //
        //  (the methods above that take a CultureInfo would use CultureInfo.CompareInfo)
        //
        ////////////////////////////////////////////////////////////////////////
        internal int GetHashCodeOfString(string source, CompareOptions options)
        {
            return GetHashCodeOfString(source, options, false, 0);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal int GetHashCodeOfString(string source, CompareOptions options, bool forceRandomizedHashing, long additionalEntropy)
        {
            //
            //  Parameter validation
            //
            if(null == source)
            {
                throw new ArgumentNullException("source");
            }

            if ((options & ValidHashCodeOfStringMaskOffFlags) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            }
            Contract.EndContractBlock();

            if(0 == source.Length)
            {
                return(0);
            }

            //
            ////////////////////////////////////////////////////////////////////////
            return (InternalGetGlobalizedHashCode(m_dataHandle, m_handleOrigin, this.m_sortName, source, source.Length, GetNativeCompareFlags(options), forceRandomizedHashing, additionalEntropy));
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  ToString
        //
        //  Implements Object.ToString().  Returns a string describing the
        //  CompareInfo.
        //
        ////////////////////////////////////////////////////////////////////////


        public override String ToString()
        {
            return ("CompareInfo - " + this.Name);
        }

#if FEATURE_USE_LCID
        public int LCID
        {
            get
            {
                return CultureInfo.GetCultureInfo(this.Name).LCID;
            }
        }
#endif

        [System.Security.SecuritySafeCritical]
        internal static IntPtr InternalInitSortHandle(String localeName, out IntPtr handleOrigin)
        {
            return NativeInternalInitSortHandle(localeName, out handleOrigin);
        }

#if !FEATURE_CORECLR
        private const int SORT_VERSION_WHIDBEY = 0x00001000;
        private const int SORT_VERSION_V4 = 0x00060101;

        internal static bool IsLegacy20SortingBehaviorRequested
        {
            get
            {
                return InternalSortVersion == SORT_VERSION_WHIDBEY;
            }
        }

        private static uint InternalSortVersion
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return InternalGetSortVersion();
            }
        }

        [OptionalField(VersionAdded = 3)]
        private SortVersion m_SortVersion;

        public SortVersion Version
        {
            [SecuritySafeCritical]
            get
            {
                if(m_SortVersion == null) 
                {
                    Win32Native.NlsVersionInfoEx v = new Win32Native.NlsVersionInfoEx();
                    v.dwNLSVersionInfoSize = Marshal.SizeOf(typeof(Win32Native.NlsVersionInfoEx));
                    InternalGetNlsVersionEx(m_dataHandle, m_handleOrigin, m_sortName, ref v);
                    m_SortVersion = new SortVersion(v.dwNLSVersion, (v.dwEffectiveId != 0) ? v.dwEffectiveId : LCID, v.guidCustomVersion);
                }

                return m_SortVersion;
            }
        }
        
        [System.Security.SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InternalGetNlsVersionEx(IntPtr handle, IntPtr handleOrigin, String localeName, ref Win32Native.NlsVersionInfoEx lpNlsVersionInformation);

        [System.Security.SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern uint InternalGetSortVersion();

#endif
        [System.Security.SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern IntPtr NativeInternalInitSortHandle(String localeName, out IntPtr handleOrigin);

        // Get a locale sensitive sort hash code from native code -- COMNlsInfo::InternalGetGlobalizedHashCode
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern int InternalGetGlobalizedHashCode(IntPtr handle, IntPtr handleOrigin, string localeName, string source, int length, int dwFlags, bool forceRandomizedHashing, long additionalEntropy);

        // Use native API calls to see if this string is entirely defined -- COMNlsInfo::InternalIsSortable
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InternalIsSortable(IntPtr handle, IntPtr handleOrigin, String localeName, String source, int length);

        // Compare a string using the native API calls -- COMNlsInfo::InternalCompareString
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern int InternalCompareString(IntPtr handle, IntPtr handleOrigin, String localeName, String string1, int offset1, int length1,
                                                                              String string2, int offset2, int length2, int flags);

        // InternalFindNLSStringEx parameters is not exactly matching kernel32::FindNLSStringEx parameters.
        // Call through to NewApis::FindNLSStringEx so we can get the right behavior
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern int InternalFindNLSStringEx(IntPtr handle, IntPtr handleOrigin, String localeName, int flags, String source, int sourceCount, int startIndex, string target, int targetCount);

        // Call through to NewAPis::LCMapStringEx so we can get appropriate behavior for all platforms
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern int InternalGetSortKey(IntPtr handle, IntPtr handleOrigin, String localeName, int flags, String source, int sourceCount, byte[] target, int targetCount);
    }
}
