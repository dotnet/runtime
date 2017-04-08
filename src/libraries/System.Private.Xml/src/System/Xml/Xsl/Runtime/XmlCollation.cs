// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.IO;

namespace System.Xml.Xsl.Runtime
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed partial class XmlCollation
    {
        // lgid support for sort
        private const string deDE = "de-DE";
        private const string huHU = "hu-HU";
        private const string jaJP = "ja-JP";
        private const string kaGE = "ka-GE";
        private const string koKR = "ko-KR";
        private const string zhTW = "zh-TW";
        private const string zhCN = "zh-CN";
        private const string zhHK = "zh-HK";
        private const string zhSG = "zh-SG";
        private const string zhMO = "zh-MO";
        private const string zhTWbopo = "zh-TW_pronun";
        private const string deDEphon = "de-DE_phoneb";
        private const string huHUtech = "hu-HU_technl";
        private const string kaGEmode = "ka-GE_modern";

        // Invariant: compops == (options & Options.mask)
        private CultureInfo _cultInfo;
        private Options _options;
        private CompareOptions _compops;


        /// <summary>
        /// Extends System.Globalization.CompareOptions with additional flags.
        /// </summary>
        private struct Options
        {
            public const int FlagUpperFirst = 0x1000;
            public const int FlagEmptyGreatest = 0x2000;
            public const int FlagDescendingOrder = 0x4000;

            private const int Mask = FlagUpperFirst | FlagEmptyGreatest | FlagDescendingOrder;

            private int _value;

            public Options(int value)
            {
                _value = value;
            }

            public bool GetFlag(int flag)
            {
                return (_value & flag) != 0;
            }

            public void SetFlag(int flag, bool value)
            {
                if (value)
                    _value |= flag;
                else
                    _value &= ~flag;
            }

            public bool UpperFirst
            {
                get { return GetFlag(FlagUpperFirst); }
                set { SetFlag(FlagUpperFirst, value); }
            }

            public bool EmptyGreatest
            {
                get { return GetFlag(FlagEmptyGreatest); }
            }

            public bool DescendingOrder
            {
                get { return GetFlag(FlagDescendingOrder); }
            }

            public bool IgnoreCase
            {
                get { return GetFlag((int)CompareOptions.IgnoreCase); }
            }

            public bool Ordinal
            {
                get { return GetFlag((int)CompareOptions.Ordinal); }
            }

            public CompareOptions CompareOptions
            {
                get
                {
                    return (CompareOptions)(_value & ~Mask);
                }
                set
                {
                    Debug.Assert(((int)value & Mask) == 0);
                    _value = (_value & Mask) | (int)value;
                }
            }

            public static implicit operator int (Options options)
            {
                return options._value;
            }
        }


        //-----------------------------------------------
        // Constructors
        //-----------------------------------------------

        /// <summary>
        /// Construct a collation that uses the specified culture and compare options.
        /// </summary>
        private XmlCollation(CultureInfo cultureInfo, Options options)
        {
            _cultInfo = cultureInfo;
            _options = options;
            _compops = options.CompareOptions;
        }


        //-----------------------------------------------
        // Create
        //-----------------------------------------------

        /// <summary>
        /// Singleton collation that sorts according to Unicode code points.
        /// </summary>
        private static XmlCollation s_cp = new XmlCollation(CultureInfo.InvariantCulture, new Options((int)CompareOptions.Ordinal));

        internal static XmlCollation CodePointCollation
        {
            get { return s_cp; }
        }

        internal static XmlCollation Create(string collationLiteral)
        {
            return Create(collationLiteral, /*throw:*/true);
        }
        // This function is used in both parser and F&O library, so just strictly map valid literals to XmlCollation.
        // Set compare options one by one:
        //     0, false: no effect; 1, true: yes
        // Disregard unrecognized options.
        internal static XmlCollation Create(string collationLiteral, bool throwOnError)
        {
            Debug.Assert(collationLiteral != null, "collation literal should not be null");

            if (collationLiteral == XmlReservedNs.NsCollCodePoint)
            {
                return CodePointCollation;
            }

            Uri collationUri;
            CultureInfo cultInfo = null;
            Options options = new Options();

            if (throwOnError)
            {
                collationUri = new Uri(collationLiteral);
            }
            else
            {
                if (!Uri.TryCreate(collationLiteral, UriKind.Absolute, out collationUri))
                {
                    return null;
                }
            }
            string authority = collationUri.GetComponents(UriComponents.Scheme | UriComponents.UserInfo | UriComponents.Host | UriComponents.Port, UriFormat.UriEscaped);
            if (authority == XmlReservedNs.NsCollationBase)
            {
                // Language
                // at least a '/' will be returned for Uri.LocalPath
                string lang = collationUri.LocalPath.Substring(1);
                if (lang.Length == 0)
                {
                    // Use default culture of current thread (cultinfo = null)
                }
                else
                {
                    // Create culture from RFC 1766 string
                    try
                    {
                        cultInfo = new CultureInfo(lang);
                    }
                    catch (ArgumentException)
                    {
                        if (!throwOnError) return null;
                        throw new XslTransformException(SR.Coll_UnsupportedLanguage, lang);
                    }
                }
            }
            else if (collationUri.IsBaseOf(new Uri(XmlReservedNs.NsCollCodePoint)))
            {
                // language with codepoint collation is not allowed
                options.CompareOptions = CompareOptions.Ordinal;
            }
            else
            {
                // Unrecognized collation
                if (!throwOnError) return null;
                throw new XslTransformException(SR.Coll_Unsupported, collationLiteral);
            }

            // Sort & Compare option
            // at least a '?' will be returned for Uri.Query if not empty
            string query = collationUri.Query;
            string sort = null;

            if (query.Length != 0)
            {
                foreach (string option in query.Substring(1).Split('&'))
                {
                    string[] pair = option.Split('=');

                    if (pair.Length != 2)
                    {
                        if (!throwOnError) return null;
                        throw new XslTransformException(SR.Coll_BadOptFormat, option);
                    }

                    string optionName = pair[0].ToUpper();
                    string optionValue = pair[1].ToUpper();

                    if (optionName == "SORT")
                    {
                        sort = optionValue;
                    }
                    else
                    {
                        int flag;

                        switch (optionName)
                        {
                            case "IGNORECASE": flag = (int)CompareOptions.IgnoreCase; break;
                            case "IGNORENONSPACE": flag = (int)CompareOptions.IgnoreNonSpace; break;
                            case "IGNORESYMBOLS": flag = (int)CompareOptions.IgnoreSymbols; break;
                            case "IGNOREKANATYPE": flag = (int)CompareOptions.IgnoreKanaType; break;
                            case "IGNOREWIDTH": flag = (int)CompareOptions.IgnoreWidth; break;
                            case "UPPERFIRST": flag = Options.FlagUpperFirst; break;
                            case "EMPTYGREATEST": flag = Options.FlagEmptyGreatest; break;
                            case "DESCENDINGORDER": flag = Options.FlagDescendingOrder; break;
                            default:
                                if (!throwOnError) return null;
                                throw new XslTransformException(SR.Coll_UnsupportedOpt, pair[0]);
                        }

                        switch (optionValue)
                        {
                            case "0": case "FALSE": options.SetFlag(flag, false); break;
                            case "1": case "TRUE": options.SetFlag(flag, true); break;
                            default:
                                if (!throwOnError) return null;
                                throw new XslTransformException(SR.Coll_UnsupportedOptVal, pair[0], pair[1]);
                        }
                    }
                }
            }

            // upperfirst option is only meaningful when not ignore case
            if (options.UpperFirst && options.IgnoreCase)
                options.UpperFirst = false;

            // other CompareOptions are only meaningful if Ordinal comparison is not being used
            if (options.Ordinal)
            {
                options.CompareOptions = CompareOptions.Ordinal;
                options.UpperFirst = false;
            }

            // new cultureinfo based on alternate sorting option
            if (sort != null && cultInfo != null)
            {
                string cultName = cultInfo.Name;
                switch (sort)
                {
                    case "bopo":
                        if (cultName == zhTW)
                        {
                            cultInfo = new CultureInfo(zhTWbopo);
                        }
                        break;
                    case "strk":
                        if (cultName == zhCN || cultName == zhHK || cultName == zhSG || cultName == zhMO)
                        {
                            cultInfo = new CultureInfo(cultName);
                        }
                        break;
                    case "uni":
                        if (cultName == jaJP || cultName == koKR)
                        {
                            cultInfo = new CultureInfo(cultName);
                        }
                        break;
                    case "phn":
                        if (cultName == deDE)
                        {
                            cultInfo = new CultureInfo(deDEphon);
                        }
                        break;
                    case "tech":
                        if (cultName == huHU)
                        {
                            cultInfo = new CultureInfo(huHUtech);
                        }
                        break;
                    case "mod":
                        // ka-GE(Georgian - Georgia) Modern Sort: 0x00010437
                        if (cultName == kaGE)
                        {
                            cultInfo = new CultureInfo(kaGEmode);
                        }
                        break;
                    case "pron":
                    case "dict":
                    case "trad":
                        // es-ES(Spanish - Spain) Traditional: 0x0000040A
                        // They are removing 0x040a (Spanish Traditional sort) in NLS+.
                        // So if you create 0x040a, it's just like 0x0c0a (Spanish International sort).
                        // Thus I don't handle it differently.
                        break;
                    default:
                        if (!throwOnError) return null;
                        throw new XslTransformException(SR.Coll_UnsupportedSortOpt, sort);
                }
            }
            return new XmlCollation(cultInfo, options);
        }


        //-----------------------------------------------
        // Collection Support
        //-----------------------------------------------

        // Redefine Equals and GetHashCode methods, they are needed for UniqueList<XmlCollation>
        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            XmlCollation that = obj as XmlCollation;
            return that != null &&
                _options == that._options &&
                object.Equals(_cultInfo, that._cultInfo);
        }

        public override int GetHashCode()
        {
            int hashCode = _options;
            if (_cultInfo != null)
            {
                hashCode ^= _cultInfo.GetHashCode();
            }
            return hashCode;
        }


        //-----------------------------------------------
        // Serialization Support
        //-----------------------------------------------

        // Denotes the current thread locale
        private const string LOCALE_CURRENT = "<!-- LOCALE CURRENT -->";

        internal void GetObjectData(BinaryWriter writer)
        {
            // NOTE: For CultureInfo we serialize only LCID. It seems to suffice for our purposes.
            Debug.Assert(_cultInfo == null || _cultInfo.Equals(new CultureInfo(_cultInfo.Name)),
                "Cannot serialize CultureInfo correctly");
            writer.Write(_cultInfo != null ? _cultInfo.Name : LOCALE_CURRENT);
            writer.Write(_options);
        }

        internal XmlCollation(BinaryReader reader)
        {
            string cultName = reader.ReadString();
            _cultInfo = (cultName != LOCALE_CURRENT) ? new CultureInfo(cultName) : null;
            _options = new Options(reader.ReadInt32());
            _compops = _options.CompareOptions;
        }

        //-----------------------------------------------
        // Compare Properties
        //-----------------------------------------------

        internal bool UpperFirst
        {
            get { return _options.UpperFirst; }
        }

        internal bool EmptyGreatest
        {
            get { return _options.EmptyGreatest; }
        }

        internal bool DescendingOrder
        {
            get { return _options.DescendingOrder; }
        }

        internal CultureInfo Culture
        {
            get
            {
                // Use default thread culture if this.cultinfo = null
                if (_cultInfo == null)
                    return CultureInfo.CurrentCulture;

                return _cultInfo;
            }
        }

#if not_used
        /// <summary>
        /// Compare two strings with each other.  Return <0 if str1 sorts before str2, 0 if they're equal, and >0
        /// if str1 sorts after str2.
        /// </summary>
        internal int Compare(string str1, string str2) {
            CultureInfo cultinfo = Culture;
            int result;

            if (this.options.Ordinal) {
                result = string.CompareOrdinal(str1, str2);
                if (result < 0) result = -1;
                else if (result > 0) result = 1;
            }
            else if (UpperFirst) {
                // First compare case-insensitive, then break ties by considering case
                result = cultinfo.CompareInfo.Compare(str1, str2, this.compops | CompareOptions.IgnoreCase);
                if (result == 0)
                    result = -cultinfo.CompareInfo.Compare(str1, str2, this.compops);
            }
            else {
                result = cultinfo.CompareInfo.Compare(str1, str2, this.compops);
            }

            if (DescendingOrder)
                result = -result;

            return result;
        }

        /// <summary>
        /// Return the index of str1 in str2, or -1 if str1 is not a substring of str2.
        /// </summary>
        internal int IndexOf(string str1, string str2) {
            return Culture.CompareInfo.IndexOf(str1, str2, this.compops);
        }

        /// <summary>
        /// Return true if str1 ends with str2.
        /// </summary>
        internal bool IsSuffix(string str1, string str2) {
            if (this.options.Ordinal){
                if (str1.Length < str2.Length) {
                    return false;
                } else {
                    return String.CompareOrdinal(str1, str1.Length - str2.Length, str2, 0, str2.Length) == 0;
                }
            }
            return Culture.CompareInfo.IsSuffix (str1, str2, this.compops);
        }

        /// <summary>
        /// Return true if str1 starts with str2.
        /// </summary>
        internal bool IsPrefix(string str1, string str2) {
            if (this.options.Ordinal) {
                if (str1.Length < str2.Length) {
                    return false;
                } else {
                    return String.CompareOrdinal(str1, 0, str2, 0, str2.Length) == 0;
                }
            }
            return Culture.CompareInfo.IsPrefix (str1, str2, this.compops);
        }
#endif
    }
}
