// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private WmfMetaHeader _data;

        public MetaHeader()
        {
        }

        internal MetaHeader(WmfMetaHeader header)
        {
            _data._type = header._type;
            _data._headerSize = header._headerSize;
            _data._version = header._version;
            _data._size = header._size;
            _data._noObjects = header._noObjects;
            _data._maxRecord = header._maxRecord;
            _data._noParameters = header._noParameters;
        }

        /// <summary>
        /// Represents the type of the associated <see cref='Metafile'/>.
        /// </summary>
        public short Type
        {
            get { return _data._type; }
            set { _data._type = value; }
        }

        /// <summary>
        /// Represents the sizi, in bytes, of the header file.
        /// </summary>
        public short HeaderSize
        {
            get { return _data._headerSize; }
            set { _data._headerSize = value; }
        }

        /// <summary>
        /// Represents the version number of the header format.
        /// </summary>
        public short Version
        {
            get { return _data._version; }
            set { _data._version = value; }
        }

        /// <summary>
        /// Represents the size, in bytes, of the associated <see cref='Metafile'/>.
        /// </summary>
        public int Size
        {
            get { return _data._size; }
            set { _data._size = value; }
        }

        public short NoObjects
        {
            get { return _data._noObjects; }
            set { _data._noObjects = value; }
        }

        public int MaxRecord
        {
            get { return _data._maxRecord; }
            set { _data._maxRecord = value; }
        }

        public short NoParameters
        {
            get { return _data._noParameters; }
            set { _data._noParameters = value; }
        }

        internal WmfMetaHeader GetNativeValue() => _data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct WmfMetaHeader
    {
        internal short _type;
        internal short _headerSize;
        internal short _version;
        internal int _size;
        internal short _noObjects;
        internal int _maxRecord;
        internal short _noParameters;
    }
}
