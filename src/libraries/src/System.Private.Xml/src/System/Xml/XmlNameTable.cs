// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    /// <devdoc>
    ///    <para> Table of atomized string objects. This provides an
    ///       efficient means for the XML parser to use the same string object for all
    ///       repeated element and attribute names in an XML document. This class is
    ///    <see langword='abstract'/>
    ///    .</para>
    /// </devdoc>
    public abstract class XmlNameTable
    {
        /// <devdoc>
        ///    <para>Gets the atomized String object containing the same
        ///       chars as the specified range of chars in the given char array.</para>
        /// </devdoc>
        public abstract string Get(char[] array, int offset, int length);

        /// <devdoc>
        ///    <para>
        ///       Gets the atomized String object containing the same
        ///       value as the specified string.
        ///    </para>
        /// </devdoc>
        public abstract string Get(string array);

        /// <devdoc>
        ///    <para>Creates a new atom for the characters at the specified range
        ///       of characters in the specified string.</para>
        /// </devdoc>
        public abstract string Add(char[] array, int offset, int length);

        /// <devdoc>
        ///    <para>
        ///       Creates a new atom for the specified string.
        ///    </para>
        /// </devdoc>
        public abstract string Add(string array);
    }
}
