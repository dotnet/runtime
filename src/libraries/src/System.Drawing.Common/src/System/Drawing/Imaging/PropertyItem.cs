// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    // sdkinc\imaging.h
    /// <include file='doc\PropertyItem.uex' path='docs/doc[@for="PropertyItem"]/*' />
    /// <devdoc>
    ///    Encapsulates a metadata property to be
    ///    included in an image file.
    /// </devdoc>
    public sealed class PropertyItem
    {
        private int _id;
        private int _len;
        private short _type;
        private byte[] _value;

        internal PropertyItem()
        {
        }

        /// <include file='doc\PropertyItem.uex' path='docs/doc[@for="PropertyItem.Id"]/*' />
        /// <devdoc>
        ///    Represents the ID of the property.
        /// </devdoc>
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }
        /// <include file='doc\PropertyItem.uex' path='docs/doc[@for="PropertyItem.Len"]/*' />
        /// <devdoc>
        ///    Represents the length of the property.
        /// </devdoc>
        public int Len
        {
            get { return _len; }
            set { _len = value; }
        }
        /// <include file='doc\PropertyItem.uex' path='docs/doc[@for="PropertyItem.Type"]/*' />
        /// <devdoc>
        ///    Represents the type of the property.
        /// </devdoc>
        public short Type
        {
            get { return _type; }
            set { _type = value; }
        }
        /// <include file='doc\PropertyItem.uex' path='docs/doc[@for="PropertyItem.Value"]/*' />
        /// <devdoc>
        ///    Contains the property value.
        /// </devdoc>
        public byte[] Value
        {
            get { return _value; }
            set { _value = value; }
        }
    }
}

