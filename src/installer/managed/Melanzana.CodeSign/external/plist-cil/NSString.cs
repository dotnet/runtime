// plist-cil - An open source library to parse and generate property lists for .NET
// Copyright (C) 2015 Natalia Portillo
//
// This code is based on:
// plist - An open source library to parse and generate property lists
// Copyright (C) 2014 Daniel Dreibrodt
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Text;

namespace Claunia.PropertyList
{
    /// <summary>A NSString contains a string.</summary>
    /// @author Daniel Dreibrodt
    /// @author Natalia Portillo
    public class NSString : NSObject, IComparable
    {
        static Encoding asciiEncoder, utf16beEncoder, utf8Encoder;

        /// <summary>Creates a NSString from its binary representation.</summary>
        /// <param name="bytes">The binary representation.</param>
        /// <param name="encoding">The encoding of the binary representation, the name of a supported charset.</param>
        /// <exception cref="ArgumentException">The encoding charset is invalid or not supported by the underlying platform.</exception>
        public NSString(ReadOnlySpan<byte> bytes, string encoding) : this(bytes, Encoding.GetEncoding(encoding)) {}

        /// <summary>Creates a NSString from its binary representation.</summary>
        /// <param name="bytes">The binary representation.</param>
        /// <param name="encoding">The encoding of the binary representation.</param>
        /// <exception cref="ArgumentException">The encoding charset is invalid or not supported by the underlying platform.</exception>
        public NSString(ReadOnlySpan<byte> bytes, Encoding encoding)
        {
        #if NATIVE_SPAN
            Content = encoding.GetString(bytes);
        #else
            Content = encoding.GetString(bytes.ToArray());
        #endif
        }

        /// <summary>Creates a NSString from a string.</summary>
        /// <param name="text">The string that will be contained in the NSString.</param>
        public NSString(string text) => Content = text;

        /// <summary>Gets this strings content.</summary>
        /// <returns>This NSString as .NET string object.</returns>
        public string Content { get; set; }

        /// <summary>Compares the current <see cref="Claunia.PropertyList.NSString" /> to the specified object.</summary>
        /// <returns>A 32-bit signed integer that indicates the lexical relationship between the two comparands.</returns>
        /// <param name="o">Object to compare to the current <see cref="Claunia.PropertyList.NSString" />.</param>
        public int CompareTo(object o) => o switch
        {
            NSString nsString => string.Compare(Content, nsString.Content, StringComparison.Ordinal),
            string s          => string.Compare(Content, s, StringComparison.Ordinal),
            _                 => -1
        };

        /// <summary>Appends a string to this string.</summary>
        /// <param name="s">The string to append.</param>
        public void Append(NSString s) => Append(s.Content);

        /// <summary>Appends a string to this string.</summary>
        /// <param name="s">The string to append.</param>
        public void Append(string s) => Content += s;

        /// <summary>Prepends a string to this string.</summary>
        /// <param name="s">The string to prepend.</param>
        public void Prepend(string s) => Content = s + Content;

        /// <summary>Prepends a string to this string.</summary>
        /// <param name="s">The string to prepend.</param>
        public void Prepend(NSString s) => Prepend(s.Content);

        /// <summary>
        ///     Determines whether the specified <see cref="System.Object" /> is equal to the current
        ///     <see cref="Claunia.PropertyList.NSString" />.
        /// </summary>
        /// <param name="obj">
        ///     The <see cref="System.Object" /> to compare with the current
        ///     <see cref="Claunia.PropertyList.NSString" />.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="System.Object" /> is equal to the current
        ///     <see cref="Claunia.PropertyList.NSString" />; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) => obj is NSString nsString && Content.Equals(nsString.Content);

        /// <summary>Serves as a hash function for a <see cref="Claunia.PropertyList.NSString" /> object.</summary>
        /// <returns>
        ///     A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a
        ///     hash table.
        /// </returns>
        public override int GetHashCode() => Content.GetHashCode();

        /// <summary>The textual representation of this NSString.</summary>
        /// <returns>The NSString's contents.</returns>
        public override string ToString() => Content;

        internal override void ToXml(StringBuilder xml, int level)
        {
            Indent(xml, level);
            xml.Append("<string>");

            //Make sure that the string is encoded in UTF-8 for the XML output
            lock(typeof(NSString))
            {
                utf8Encoder ??= Encoding.GetEncoding("UTF-8");

                try
                {
                    byte[] bytes = utf8Encoder.GetBytes(Content);
                    Content = utf8Encoder.GetString(bytes);
                }
                catch(Exception ex)
                {
                    throw new PropertyListException("Could not encode the NSString into UTF-8: " + ex.Message);
                }
            }

            //According to http://www.w3.org/TR/REC-xml/#syntax node values must not
            //contain the characters < or &. Also the > character should be escaped.
            if(Content.Contains("&") ||
               Content.Contains("<") ||
               Content.Contains(">"))
            {
                xml.Append("<![CDATA[");
                xml.Append(Content.Replace("]]>", "]]]]><![CDATA[>"));
                xml.Append("]]>");
            }
            else
                xml.Append(Content);

            xml.Append("</string>");
        }

        internal override void ToBinary(BinaryPropertyListWriter outPlist)
        {
            int    kind;
            byte[] byteBuf;

            lock(typeof(NSString))
            {
                // Not much use, because some characters do not fallback to exception, even if not ASCII
                asciiEncoder ??= Encoding.GetEncoding("ascii", EncoderFallback.ExceptionFallback,
                                                      DecoderFallback.ExceptionFallback);

                if(IsASCIIEncodable(Content))
                {
                    kind    = 0x5; // standard ASCII
                    byteBuf = asciiEncoder.GetBytes(Content);
                }
                else
                {
                    utf16beEncoder ??= Encoding.BigEndianUnicode;

                    kind    = 0x6; // UTF-16-BE
                    byteBuf = utf16beEncoder.GetBytes(Content);
                }
            }

            outPlist.WriteIntHeader(kind, Content.Length);
            outPlist.Write(byteBuf);
        }

        internal override void ToASCII(StringBuilder ascii, int level)
        {
            Indent(ascii, level);
            ascii.Append("\"");

            //According to https://developer.apple.com/library/mac/#documentation/Cocoa/Conceptual/PropertyLists/OldStylePlists/OldStylePLists.html
            //non-ASCII characters are not escaped but simply written into the
            //file, thus actually violating the ASCII plain text format.
            //We will escape the string anyway because current Xcode project files (ASCII property lists) also escape their strings.
            ascii.Append(EscapeStringForASCII(Content));
            ascii.Append("\"");
        }

        internal override void ToASCIIGnuStep(StringBuilder ascii, int level)
        {
            Indent(ascii, level);
            ascii.Append("\"");
            ascii.Append(EscapeStringForASCII(Content));
            ascii.Append("\"");
        }

        /// <summary>Escapes a string for use in ASCII property lists.</summary>
        /// <returns>The unescaped string.</returns>
        /// <param name="s">S.</param>
        internal static string EscapeStringForASCII(string s)
        {
            string outString = "";
            char[] cArray    = s.ToCharArray();

            foreach(char c in cArray)
                if(c > 127)
                {
                    //non-ASCII Unicode
                    outString += "\\U";
                    string hex = $"{c:x}";

                    while(hex.Length < 4)
                        hex = "0" + hex;

                    outString += hex;
                }
                else
                    outString += c switch
                    {
                        '\\' => "\\\\",
                        '\"' => "\\\"",
                        '\b' => "\\b",
                        '\n' => "\\n",
                        '\r' => "\\r",
                        '\t' => "\\t",
                        _    => c
                    };

            return outString;
        }

        /// <summary>
        ///     Determines whether the specified <see cref="Claunia.PropertyList.NSObject" /> is equal to the current
        ///     <see cref="Claunia.PropertyList.NSString" />.
        /// </summary>
        /// <param name="obj">
        ///     The <see cref="Claunia.PropertyList.NSObject" /> to compare with the current
        ///     <see cref="Claunia.PropertyList.NSString" />.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="Claunia.PropertyList.NSObject" /> is equal to the current
        ///     <see cref="Claunia.PropertyList.NSString" />; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(NSObject obj)
        {
            if(obj is not NSString nsString)
                return false;

            return Content == nsString.Content;
        }

        internal static bool IsASCIIEncodable(string text)
        {
            foreach(char c in text)
                if(c > 0x7F)
                    return false;

            return true;
        }

        public static explicit operator string(NSString value) => value.Content;

        public static explicit operator NSString(string value) => new(value);
    }
}