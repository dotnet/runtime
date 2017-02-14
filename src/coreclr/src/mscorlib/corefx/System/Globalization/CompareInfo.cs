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

using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace System.Globalization
{
    [Flags]
    [Serializable]
    public enum CompareOptions
    {
        None = 0x00000000,
        IgnoreCase = 0x00000001,
        IgnoreNonSpace = 0x00000002,
        IgnoreSymbols = 0x00000004,
        IgnoreKanaType = 0x00000008,   // ignore kanatype
        IgnoreWidth = 0x00000010,   // ignore width
        OrdinalIgnoreCase = 0x10000000,   // This flag can not be used with other flags.
        StringSort = 0x20000000,   // use string sort method
        Ordinal = 0x40000000,   // This flag can not be used with other flags.
    }

    [Serializable]
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

            // Mask used to check if we have the right flags.
        private const CompareOptions ValidSortkeyCtorMaskOffFlags = 
            ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace |
              CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType | CompareOptions.StringSort);

        //
        // CompareInfos have an interesting identity.  They are attached to the locale that created them,
        // ie: en-US would have an en-US sort.  For haw-US (custom), then we serialize it as haw-US.
        // The interesting part is that since haw-US doesn't have its own sort, it has to point at another
        // locale, which is what SCOMPAREINFO does.

        [OptionalField(VersionAdded = 2)]
        private String _name;  // The name used to construct this CompareInfo
        [NonSerialized] 
        private String _sortName; // The name that defines our behavior

        [OptionalField(VersionAdded = 3)]
        private SortVersion _sortVersion;

        /*=================================GetCompareInfo==========================
        **Action: Get the CompareInfo constructed from the data table in the specified assembly for the specified culture.
        **       Warning: The assembly versioning mechanism is dead!
        **Returns: The CompareInfo for the specified culture.
        **Arguments:
        **   culture    the ID of the culture
        **   assembly   the assembly which contains the sorting table.
        **Exceptions:
        **  ArugmentNullException when the assembly is null
        **  ArgumentException if culture is invalid.
        ============================================================================*/
        // Assembly constructor should be deprecated, we don't act on the assembly information any more
        public static CompareInfo GetCompareInfo(int culture, Assembly assembly)
        {
            // Parameter checking.
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            if (assembly != typeof(Object).Module.Assembly) 
            {
                throw new ArgumentException(SR.Argument_OnlyMscorlib);
            }
            Contract.EndContractBlock();

            return GetCompareInfo(culture);
        }

        /*=================================GetCompareInfo==========================
        **Action: Get the CompareInfo constructed from the data table in the specified assembly for the specified culture.
        **       The purpose of this method is to provide version for CompareInfo tables.
        **Returns: The CompareInfo for the specified culture.
        **Arguments:
        **   name      the name of the culture
        **   assembly  the assembly which contains the sorting table.
        **Exceptions:
        **  ArugmentNullException when the assembly is null
        **  ArgumentException if name is invalid.
        ============================================================================*/
        // Assembly constructor should be deprecated, we don't act on the assembly information any more
        public static CompareInfo GetCompareInfo(String name, Assembly assembly)
        {
            if (name == null || assembly == null)
            {
                throw new ArgumentNullException(name == null ? nameof(name) : nameof(assembly));
            }
            Contract.EndContractBlock();

            if (assembly != typeof(Object).Module.Assembly)
            {
                throw new ArgumentException(SR.Argument_OnlyMscorlib);
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
        // People really shouldn't be calling LCID versions, no custom support
        public static CompareInfo GetCompareInfo(int culture)
        {
            if (CultureData.IsCustomCultureId(culture))
            {
                // Customized culture cannot be created by the LCID.
                throw new ArgumentException(SR.Argument_CustomCultureCannotBePassedByNumber, nameof(culture));
            }

            return CultureInfo.GetCultureInfo(culture).CompareInfo;
        }

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
                throw new ArgumentNullException(nameof(name));
            }
            Contract.EndContractBlock();

            return CultureInfo.GetCultureInfo(name).CompareInfo;
        }

        public static unsafe bool IsSortable(char ch)
        {
            char *pChar = &ch;
            return IsSortable(pChar, 1);
        }

        public static unsafe bool IsSortable(string text)
        {
            if (text == null) 
            {
                // A null param is invalid here.
                throw new ArgumentNullException(nameof(text));
            }

            if (0 == text.Length)
            {
                // A zero length string is not invalid, but it is also not sortable.
                return (false);
            }
            
            fixed (char *pChar = text)
            {
                return IsSortable(pChar, text.Length);
            }
        }


        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            _name = null;
        }

        void IDeserializationCallback.OnDeserialization(Object sender)
        {
            OnDeserialized();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            OnDeserialized();
        }

        private void OnDeserialized()
        {
            if (_name != null)
            {
                InitSort(CultureInfo.GetCultureInfo(_name));
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx) { }

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

        public virtual String Name
        {
            get
            {
                Debug.Assert(_name != null, "CompareInfo.Name Expected _name to be set");
                if (_name == "zh-CHT" || _name == "zh-CHS")
                {
                    return _name;
                }

                return _sortName;
            }
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

        public unsafe virtual int Compare(String string1, String string2, CompareOptions options)
        {
            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return String.Compare(string1, string2, StringComparison.OrdinalIgnoreCase);
            }

            // Verify the options before we do any real comparison.
            if ((options & CompareOptions.Ordinal) != 0)
            {
                if (options != CompareOptions.Ordinal)
                {
                    throw new ArgumentException(SR.Argument_CompareOptionOrdinal, nameof(options));
                }
                return String.CompareOrdinal(string1, string2);
            }

            if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            //Our paradigm is that null sorts less than any other string and
            //that two nulls sort as equal.
            if (string1 == null)
            {
                if (string2 == null)
                {
                    return (0);     // Equal
                }
                return (-1);    // null < non-null
            }
            if (string2 == null)
            {
                return (1);     // non-null > null
            }

            return CompareString(string1, 0, string1.Length, string2, 0, string2.Length, options);
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
            return Compare(string1, offset1, string1 == null ? 0 : string1.Length - offset1,
                           string2, offset2, string2 == null ? 0 : string2.Length - offset2, options);
        }


        public unsafe virtual int Compare(String string1, int offset1, String string2, int offset2)
        {
            return Compare(string1, offset1, string2, offset2, 0);
        }


        public unsafe virtual int Compare(String string1, int offset1, int length1, String string2, int offset2, int length2, CompareOptions options)
        {
            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                int result = String.Compare(string1, offset1, string2, offset2, length1 < length2 ? length1 : length2, StringComparison.OrdinalIgnoreCase);
                if ((length1 != length2) && result == 0)
                    return (length1 > length2 ? 1 : -1);
                return (result);
            }

            // Verify inputs
            if (length1 < 0 || length2 < 0)
            {
                throw new ArgumentOutOfRangeException((length1 < 0) ? nameof(length1) : nameof(length2), SR.ArgumentOutOfRange_NeedPosNum);
            }
            if (offset1 < 0 || offset2 < 0)
            {
                throw new ArgumentOutOfRangeException((offset1 < 0) ? nameof(offset1) : nameof(offset2), SR.ArgumentOutOfRange_NeedPosNum);
            }
            if (offset1 > (string1 == null ? 0 : string1.Length) - length1)
            {
                throw new ArgumentOutOfRangeException(nameof(string1), SR.ArgumentOutOfRange_OffsetLength);
            }
            if (offset2 > (string2 == null ? 0 : string2.Length) - length2)
            {
                throw new ArgumentOutOfRangeException(nameof(string2), SR.ArgumentOutOfRange_OffsetLength);
            }
            if ((options & CompareOptions.Ordinal) != 0)
            {
                if (options != CompareOptions.Ordinal)
                {
                    throw new ArgumentException(SR.Argument_CompareOptionOrdinal,
                                                nameof(options));
                }
            }
            else if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
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
                return CompareOrdinal(string1, offset1, length1,
                                      string2, offset2, length2);
            }
            return CompareString(string1, offset1, length1,
                                 string2, offset2, length2,
                                 options);
        }

        private static int CompareOrdinal(string string1, int offset1, int length1, string string2, int offset2, int length2)
        {
            int result = String.CompareOrdinal(string1, offset1, string2, offset2,
                                                       (length1 < length2 ? length1 : length2));
            if ((length1 != length2) && result == 0)
            {
                return (length1 > length2 ? 1 : -1);
            }
            return (result);
        }

        //
        // CompareOrdinalIgnoreCase compare two string oridnally with ignoring the case.
        // it assumes the strings are Ascii string till we hit non Ascii character in strA or strB and then we continue the comparison by
        // calling the OS.
        //
        internal static unsafe int CompareOrdinalIgnoreCase(string strA, int indexA, int lengthA, string strB, int indexB, int lengthB)
        {
            Debug.Assert(indexA + lengthA <= strA.Length);
            Debug.Assert(indexB + lengthB <= strB.Length);

            int length = Math.Min(lengthA, lengthB);
            int range = length;

            fixed (char* ap = strA) fixed (char* bp = strB)
            {
                char* a = ap + indexA;
                char* b = bp + indexB;

                while (length != 0 && (*a <= 0x80) && (*b <= 0x80))
                {
                    int charA = *a;
                    int charB = *b;

                    if (charA == charB)
                    {
                        a++; b++;
                        length--;
                        continue;
                    }

                    // uppercase both chars - notice that we need just one compare per char
                    if ((uint)(charA - 'a') <= (uint)('z' - 'a')) charA -= 0x20;
                    if ((uint)(charB - 'a') <= (uint)('z' - 'a')) charB -= 0x20;

                    //Return the (case-insensitive) difference between them.
                    if (charA != charB)
                        return charA - charB;

                    // Next char
                    a++; b++;
                    length--;
                }

                if (length == 0)
                    return lengthA - lengthB;

                range -= length;

                return CompareStringOrdinalIgnoreCase(a, lengthA - range, b, lengthB - range);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  IsPrefix
        //
        //  Determines whether prefix is a prefix of string.  If prefix equals
        //  String.Empty, true is returned.
        //
        ////////////////////////////////////////////////////////////////////////
        public unsafe virtual bool IsPrefix(String source, String prefix, CompareOptions options)
        {
            if (source == null || prefix == null)
            {
                throw new ArgumentNullException((source == null ? nameof(source) : nameof(prefix)),
                    SR.ArgumentNull_String);
            }
            Contract.EndContractBlock();

            if (prefix.Length == 0)
            {
                return (true);
            }

            if (source.Length == 0)
            {
                return false;
            }

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            if (options == CompareOptions.Ordinal)
            {
                return source.StartsWith(prefix, StringComparison.Ordinal);
            }

            if ((options & ValidIndexMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            return StartsWith(source, prefix, options);
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
        public unsafe virtual bool IsSuffix(String source, String suffix, CompareOptions options)
        {
            if (source == null || suffix == null)
            {
                throw new ArgumentNullException((source == null ? nameof(source) : nameof(suffix)),
                    SR.ArgumentNull_String);
            }
            Contract.EndContractBlock();

            if (suffix.Length == 0)
            {
                return (true);
            }

            if (source.Length == 0)
            {
                return false;
            }

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
            }

            if (options == CompareOptions.Ordinal)
            {
                return source.EndsWith(suffix, StringComparison.Ordinal);
            }

            if ((options & ValidIndexMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            return EndsWith(source, suffix, options);
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
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Contract.EndContractBlock();

            return IndexOf(source, value, 0, source.Length, CompareOptions.None);
        }


        public unsafe virtual int IndexOf(String source, String value)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Contract.EndContractBlock();

            return IndexOf(source, value, 0, source.Length, CompareOptions.None);
        }


        public unsafe virtual int IndexOf(String source, char value, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Contract.EndContractBlock();

            return IndexOf(source, value, 0, source.Length, options);
        }


        public unsafe virtual int IndexOf(String source, String value, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Contract.EndContractBlock();

            return IndexOf(source, value, 0, source.Length, options);
        }

        public unsafe virtual int IndexOf(String source, char value, int startIndex)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Contract.EndContractBlock();

            return IndexOf(source, value, startIndex, source.Length - startIndex, CompareOptions.None);
        }

        public unsafe virtual int IndexOf(String source, String value, int startIndex)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Contract.EndContractBlock();

            return IndexOf(source, value, startIndex, source.Length - startIndex, CompareOptions.None);
        }

        public unsafe virtual int IndexOf(String source, char value, int startIndex, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Contract.EndContractBlock();

            return IndexOf(source, value, startIndex, source.Length - startIndex, options);
        }


        public unsafe virtual int IndexOf(String source, String value, int startIndex, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
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

        public unsafe virtual int IndexOf(String source, char value, int startIndex, int count, CompareOptions options)
        {
            // Validate inputs
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (startIndex < 0 || startIndex > source.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);

            if (count < 0 || startIndex > source.Length - count)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);
            Contract.EndContractBlock();

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.IndexOf(value.ToString(), startIndex, count, StringComparison.OrdinalIgnoreCase);
            }

            // Validate CompareOptions
            // Ordinal can't be selected with other flags
            if ((options & ValidIndexMaskOffFlags) != 0 && (options != CompareOptions.Ordinal))
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));

            return IndexOfCore(source, new string(value, 1), startIndex, count, options, null);
        }


        public unsafe virtual int IndexOf(String source, String value, int startIndex, int count, CompareOptions options)
        {
            // Validate inputs
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (startIndex > source.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);
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
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);
            }

            if (count < 0 || startIndex > source.Length - count)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return IndexOfOrdinal(source, value, startIndex, count, ignoreCase: true);
            }

            // Validate CompareOptions
            // Ordinal can't be selected with other flags
            if ((options & ValidIndexMaskOffFlags) != 0 && (options != CompareOptions.Ordinal))
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));

            return IndexOfCore(source, value, startIndex, count, options, null);
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
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Contract.EndContractBlock();

            // Can't start at negative index, so make sure we check for the length == 0 case.
            return LastIndexOf(source, value, source.Length - 1,
                source.Length, CompareOptions.None);
        }


        public virtual int LastIndexOf(String source, String value)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Contract.EndContractBlock();

            // Can't start at negative index, so make sure we check for the length == 0 case.
            return LastIndexOf(source, value, source.Length - 1,
                source.Length, CompareOptions.None);
        }


        public virtual int LastIndexOf(String source, char value, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Contract.EndContractBlock();

            // Can't start at negative index, so make sure we check for the length == 0 case.
            return LastIndexOf(source, value, source.Length - 1,
                source.Length, options);
        }

        public unsafe virtual int LastIndexOf(String source, String value, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
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


        public unsafe virtual int LastIndexOf(String source, char value, int startIndex, int count, CompareOptions options)
        {
            // Verify Arguments
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Contract.EndContractBlock();

            // Validate CompareOptions
            // Ordinal can't be selected with other flags
            if ((options & ValidIndexMaskOffFlags) != 0 &&
                (options != CompareOptions.Ordinal) &&
                (options != CompareOptions.OrdinalIgnoreCase))
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));

            // Special case for 0 length input strings
            if (source.Length == 0 && (startIndex == -1 || startIndex == 0))
                return -1;

            // Make sure we're not out of range
            if (startIndex < 0 || startIndex > source.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);

            // Make sure that we allow startIndex == source.Length
            if (startIndex == source.Length)
            {
                startIndex--;
                if (count > 0)
                    count--;
            }

            // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.LastIndexOf(value.ToString(), startIndex, count, StringComparison.OrdinalIgnoreCase);
            }

            return LastIndexOfCore(source, value.ToString(), startIndex, count, options);
        }


        public unsafe virtual int LastIndexOf(String source, String value, int startIndex, int count, CompareOptions options)
        {
            // Verify Arguments
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            Contract.EndContractBlock();

            // Validate CompareOptions
            // Ordinal can't be selected with other flags
            if ((options & ValidIndexMaskOffFlags) != 0 &&
                (options != CompareOptions.Ordinal) &&
                (options != CompareOptions.OrdinalIgnoreCase))
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));

            // Special case for 0 length input strings
            if (source.Length == 0 && (startIndex == -1 || startIndex == 0))
                return (value.Length == 0) ? 0 : -1;

            // Make sure we're not out of range
            if (startIndex < 0 || startIndex > source.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);

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
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return LastIndexOfOrdinal(source, value, startIndex, count, ignoreCase: true);
            }

            return LastIndexOfCore(source, value, startIndex, count, options);
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
            //
            //  Parameter validation
            //
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if ((options & ValidHashCodeOfStringMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }
            Contract.EndContractBlock();

            return GetHashCodeOfStringCore(source, options);
        }

        public virtual int GetHashCode(string source, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
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

            return GetHashCodeOfString(source, options);
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

        public SortVersion Version
        {
            get
            {
                if (_sortVersion == null)
                {
                    _sortVersion = GetSortVersion();
                }

                return _sortVersion;
            }
        }

        public int LCID
        {
            get
            {
                return CultureInfo.GetCultureInfo(Name).LCID;
            }
        }
    }
}
