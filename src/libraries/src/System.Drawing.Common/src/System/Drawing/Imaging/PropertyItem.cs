// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    // sdkinc\imaging.h
    /// <summary>
    /// Encapsulates a metadata property to be included in an image file.
    /// </summary>
    public sealed class PropertyItem
    {
        private int _id;
        private int _len;
        private short _type;
        private byte[] _value;

        internal PropertyItem()
        {
        }

        /// <summary>
        /// Represents the ID of the property.
        /// </summary>
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }
        /// <summary>
        /// Represents the length of the property.
        /// </summary>
        public int Len
        {
            get { return _len; }
            set { _len = value; }
        }
        /// <summary>
        /// Represents the type of the property.
        /// </summary>
        public short Type
        {
            get { return _type; }
            set { _type = value; }
        }
        /// <summary>
        /// Contains the property value.
        /// </summary>
        public byte[] Value
        {
            get { return _value; }
            set { _value = value; }
        }
    }
}
