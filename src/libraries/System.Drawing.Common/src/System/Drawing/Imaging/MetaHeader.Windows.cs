// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

    using System.Runtime.InteropServices;

namespace System.Drawing.Imaging
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public sealed class MetaHeader
    {
        // The ENHMETAHEADER structure is defined natively as a union with WmfHeader.  
        // Extreme care should be taken if changing the layout of the corresponding managaed 
        // structures to minimize the risk of buffer overruns.  The affected managed classes 
        // are the following: ENHMETAHEADER, MetaHeader, MetafileHeaderWmf, MetafileHeaderEmf.
        private short _type;
        private short _headerSize;
        private short _version;
        private int _size;
        private short _noObjects;
        private int _maxRecord;
        private short _noParameters;

        /// <summary>
        /// Represents the type of the associated <see cref='Metafile'/>.
        /// </summary>
        public short Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// Represents the sizi, in bytes, of the header file.
        /// </summary>
        public short HeaderSize
        {
            get { return _headerSize; }
            set { _headerSize = value; }
        }

        /// <summary>
        /// Represents the version number of the header format.
        /// </summary>
        public short Version
        {
            get { return _version; }
            set { _version = value; }
        }

        /// <summary>
        /// Represents the size, in bytes, of the associated <see cref='Metafile'/>.
        /// </summary>
        public int Size
        {
            get { return _size; }
            set { _size = value; }
        }

        public short NoObjects
        {
            get { return _noObjects; }
            set { _noObjects = value; }
        }

        public int MaxRecord
        {
            get { return _maxRecord; }
            set { _maxRecord = value; }
        }

        public short NoParameters
        {
            get { return _noParameters; }
            set { _noParameters = value; }
        }
    }
}
