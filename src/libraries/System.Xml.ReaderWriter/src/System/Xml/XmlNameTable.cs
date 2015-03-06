// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    /// <include file='doc\XmlNameTable.uex' path='docs/doc[@for="XmlNameTable"]/*' />
    /// <devdoc>
    ///    <para> Table of atomized string objects. This provides an
    ///       efficient means for the XML parser to use the same string object for all
    ///       repeated element and attribute names in an XML document. This class is
    ///    <see langword='abstract'/>
    ///    .</para>
    /// </devdoc>
    public abstract class XmlNameTable
    {
        /// <include file='doc\XmlNameTable.uex' path='docs/doc[@for="XmlNameTable.Get"]/*' />
        /// <devdoc>
        ///    <para>Gets the atomized String object containing the same
        ///       chars as the specified range of chars in the given char array.</para>
        /// </devdoc>
        public abstract String Get(char[] array, int offset, int length);

        /// <include file='doc\XmlNameTable.uex' path='docs/doc[@for="XmlNameTable.Get1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the atomized String object containing the same
        ///       value as the specified string.
        ///    </para>
        /// </devdoc>
        public abstract String Get(String array);

        /// <include file='doc\XmlNameTable.uex' path='docs/doc[@for="XmlNameTable.Add"]/*' />
        /// <devdoc>
        ///    <para>Creates a new atom for the characters at the specified range
        ///       of chararacters in the specified string.</para>
        /// </devdoc>
        public abstract String Add(char[] array, int offset, int length);

        /// <include file='doc\XmlNameTable.uex' path='docs/doc[@for="XmlNameTable.Add1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Creates a new atom for the specified string.
        ///    </para>
        /// </devdoc>
        public abstract String Add(String array);
    }
}
