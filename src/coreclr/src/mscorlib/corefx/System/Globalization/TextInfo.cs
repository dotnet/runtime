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

using System;
using System.Diagnostics.Contracts;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading;

namespace System.Globalization
{
    [Serializable]
    public partial class TextInfo : ICloneable, IDeserializationCallback
    {
        ////--------------------------------------------------------------------//
        ////                        Internal Information                        //
        ////--------------------------------------------------------------------//

        private enum Tristate : byte
        {
            NotInitialized,
            True,
            False,
        }

        ////
        ////  Variables.
        ////

        [OptionalField(VersionAdded = 2)]
        private String _listSeparator;
        [OptionalField(VersionAdded = 2)]
        private bool _isReadOnly = false;

        ////      _cultureName is the name of the creating culture.  Note that we consider this authoratative,
        ////              if the culture's textinfo changes when deserializing, then behavior may change.
        ////              (ala Whidbey behavior).  This is the only string Arrowhead needs to serialize.
        ////      _cultureData is the data that backs this class.
        ////      _textInfoName is the actual name of the textInfo (from cultureData.STEXTINFO)
        ////              this can be the same as _cultureName on Silverlight since the OS knows
        ////              how to do the sorting. However in the desktop, when we call the sorting dll, it doesn't
        ////              know how to resolve custom locle names to sort ids so we have to have alredy resolved this.
        ////      

        [OptionalField(VersionAdded = 3)]
        private String _cultureName;      // Name of the culture that created this text info
        [NonSerialized]
        private CultureData _cultureData;      // Data record for the culture that made us, not for this textinfo
        [NonSerialized]
        private String _textInfoName;     // Name of the text info we're using (ie: _cultureData.STEXTINFO)
        [NonSerialized]
        private Tristate _isAsciiCasingSameAsInvariant = Tristate.NotInitialized;

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

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx) { }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            // Clear these so we can check if we've fixed them yet            
            _cultureData = null;
            _cultureName = null;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            OnDeserialized();
        }

        void IDeserializationCallback.OnDeserialization(Object sender)
        {
            OnDeserialized();
        }

        private void OnDeserialized()
        {
            // this method will be called twice because of the support of IDeserializationCallback
            if (_cultureData == null)
            {
                // Get the text info name belonging to that culture
                _cultureData = CultureInfo.GetCultureInfo(_cultureName).m_cultureData;
                _textInfoName = _cultureData.STEXTINFO;
                FinishInitialization(_textInfoName);
            }
        }

        //
        // Internal ordinal comparison functions
        //

        internal static int GetHashCodeOrdinalIgnoreCase(String s)
        {
            // This is the same as an case insensitive hash for Invariant
            // (not necessarily true for sorting, but OK for casing & then we apply normal hash code rules)
            return (Invariant.GetCaseInsensitiveHashCode(s));
        }

        // Currently we don't have native functions to do this, so we do it the hard way
        internal static int IndexOfStringOrdinalIgnoreCase(String source, String value, int startIndex, int count)
        {
            if (count > source.Length || count < 0 || startIndex < 0 || startIndex >= source.Length || startIndex + count > source.Length)
            {
                return -1;
            }

            return CompareInfo.IndexOfOrdinal(source, value, startIndex, count, ignoreCase: true);
        }

        // Currently we don't have native functions to do this, so we do it the hard way
        internal static int LastIndexOfStringOrdinalIgnoreCase(String source, String value, int startIndex, int count)
        {
            if (count > source.Length || count < 0 || startIndex < 0 || startIndex > source.Length - 1 || (startIndex - count + 1 < 0))
            {
                return -1;
            }

            return CompareInfo.LastIndexOfOrdinal(source, value, startIndex, count, ignoreCase: true);
        }

        //////////////////////////////////////////////////////////////////////////
        ////
        ////  CultureName
        ////
        ////  The name of the culture associated with the current TextInfo.
        ////
        //////////////////////////////////////////////////////////////////////////
        public string CultureName
        {
            get
            {
                return _textInfoName;
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
            get { return (_isReadOnly); }
        }

        //////////////////////////////////////////////////////////////////////////
        ////
        ////  Clone
        ////
        ////  Is the implementation of IColnable.
        ////
        //////////////////////////////////////////////////////////////////////////
        public virtual Object Clone()
        {
            object o = MemberwiseClone();
            ((TextInfo)o).SetReadOnlyState(false);
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
        internal static TextInfo ReadOnly(TextInfo textInfo)
        {
            if (textInfo == null) { throw new ArgumentNullException("textInfo"); }
            Contract.EndContractBlock();
            if (textInfo.IsReadOnly) { return (textInfo); }

            TextInfo clonedTextInfo = (TextInfo)(textInfo.MemberwiseClone());
            clonedTextInfo.SetReadOnlyState(true);

            return (clonedTextInfo);
        }

        private void VerifyWritable()
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
            }
        }

        internal void SetReadOnlyState(bool readOnly)
        {
            _isReadOnly = readOnly;
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
            get
            {
                if (_listSeparator == null)
                {
                    _listSeparator = _cultureData.SLIST;
                }
                return (_listSeparator);
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value", SR.ArgumentNull_String);
                }
                VerifyWritable();
                _listSeparator = value;
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
        public unsafe virtual char ToLower(char c)
        {
            if (IsAscii(c) && IsAsciiCasingSameAsInvariant)
            {
                return ToLowerAsciiInvariant(c);
            }
            return (ChangeCase(c, toUpper: false));
        }

        public unsafe virtual String ToLower(String str)
        {
            if (str == null) { throw new ArgumentNullException("str"); }

            return ChangeCase(str, toUpper: false);
        }

        private static Char ToLowerAsciiInvariant(Char c)
        {
            Contract.Assert(IsAscii(c));
            return (Char)s_toLowerAsciiMapping[c & 0x7F];
        }

        private static readonly byte[] s_toLowerAsciiMapping = new byte[128]
        {
            // s_toLowerAsciiMapping[c] == invariant lower case value of c
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
            0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
            0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,
            0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
            0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
            0x40, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67,
            0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
            0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77,
            0x78, 0x79, 0x7A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
            0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67,
            0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
            0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77,
            0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F
        };

        ////////////////////////////////////////////////////////////////////////
        //
        //  ToUpper
        //
        //  Converts the character or string to upper case.  Certain locales
        //  have different casing semantics from the file systems in Win32.
        //
        ////////////////////////////////////////////////////////////////////////
        public unsafe virtual char ToUpper(char c)
        {
            if (IsAscii(c) && IsAsciiCasingSameAsInvariant)
            {
                return ToUpperAsciiInvariant(c);
            }
            return (ChangeCase(c, toUpper: true));
        }

        public unsafe virtual String ToUpper(String str)
        {
            if (str == null) { throw new ArgumentNullException("str"); }

            return ChangeCase(str, toUpper: true);
        }

        private static Char ToUpperAsciiInvariant(Char c)
        {
            Contract.Assert(IsAscii(c));
            return (Char)s_toUpperAsciiMapping[c & 0x7F];
        }

        private static readonly byte[] s_toUpperAsciiMapping = new byte[128]
        {
            // s_toUpperAsciiMapping[c] == invariant upper case value of c
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
            0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
            0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,
            0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
            0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
            0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
            0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
            0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57,
            0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
            0x60, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
            0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
            0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57,
            0x58, 0x59, 0x5A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F
        };

        static private bool IsAscii(Char c)
        {
            return c < 0x80;
        }

        private bool IsAsciiCasingSameAsInvariant
        {
            get
            {
                if (_isAsciiCasingSameAsInvariant == Tristate.NotInitialized)
                {
                    _isAsciiCasingSameAsInvariant = CultureInfo.GetCultureInfo(_textInfoName).CompareInfo.Compare("abcdefghijklmnopqrstuvwxyz",
                                                                             "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                                                                             CompareOptions.IgnoreCase) == 0 ? Tristate.True : Tristate.False;
                }
                return _isAsciiCasingSameAsInvariant == Tristate.True;
            }
        }

        // IsRightToLeft
        //
        // Returns true if the dominant direction of text and UI such as the relative position of buttons and scroll bars
        //
        public bool IsRightToLeft
        {
            get
            {
                return _cultureData.IsRightToLeft;
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
            return ("TextInfo - " + _cultureData.CultureName);
        }

        //
        // Get case-insensitive hash code for the specified string.
        //
        internal unsafe int GetCaseInsensitiveHashCode(String str)
        {
            // Validate inputs
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            // This code assumes that ASCII casing is safe for whatever context is passed in.
            // this is true today, because we only ever call these methods on Invariant.  It would be ideal to refactor
            // these methods so they were correct by construction and we could only ever use Invariant.

            uint hash = 5381;
            uint c;

            // Note: We assume that str contains only ASCII characters until
            // we hit a non-ASCII character to optimize the common case.
            for (int i = 0; i < str.Length; i++)
            {
                c = str[i];
                if (c >= 0x80)
                {
                    return GetCaseInsensitiveHashCodeSlow(str);
                }

                // If we have a lowercase character, ANDing off 0x20
                // will make it an uppercase character.
                if ((c - 'a') <= ('z' - 'a'))
                {
                    c = (uint)((int)c & ~0x20);
                }

                hash = ((hash << 5) + hash) ^ c;
            }

            return (int)hash;
        }

        private unsafe int GetCaseInsensitiveHashCodeSlow(String str)
        {
            Contract.Assert(str != null);

            string upper = ToUpper(str);

            uint hash = 5381;
            uint c;

            for (int i = 0; i < upper.Length; i++)
            {
                c = upper[i];
                hash = ((hash << 5) + hash) ^ c;
            }

            return (int)hash;
        }
    }
}
