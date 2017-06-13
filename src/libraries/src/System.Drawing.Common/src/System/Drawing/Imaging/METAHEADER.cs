// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    using System.Runtime.InteropServices;

    /// <include file='doc\METAHEADER.uex' path='docs/doc[@for="MetaHeader"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public sealed class MetaHeader
    {
        /// The ENHMETAHEADER structure is defined natively as a union with WmfHeader.  
        /// Extreme care should be taken if changing the layout of the corresponding managaed 
        /// structures to minimize the risk of buffer overruns.  The affected managed classes 
        /// are the following: ENHMETAHEADER, MetaHeader, MetafileHeaderWmf, MetafileHeaderEmf.
        private short _type;
        private short _headerSize;
        private short _version;
        private int _size;
        private short _noObjects;
        private int _maxRecord;
        private short _noParameters;

        /// <include file='doc\METAHEADER.uex' path='docs/doc[@for="MetaHeader.Type"]/*' />
        /// <devdoc>
        ///    Represents the type of the associated
        /// <see cref='System.Drawing.Imaging.Metafile'/>.
        /// </devdoc>
        public short Type
        {
            get { return _type; }
            set { _type = value; }
        }
        /// <include file='doc\METAHEADER.uex' path='docs/doc[@for="MetaHeader.HeaderSize"]/*' />
        /// <devdoc>
        ///    Represents the sizi, in bytes, of the
        ///    header file.
        /// </devdoc>
        public short HeaderSize
        {
            get { return _headerSize; }
            set { _headerSize = value; }
        }
        /// <include file='doc\METAHEADER.uex' path='docs/doc[@for="MetaHeader.Version"]/*' />
        /// <devdoc>
        ///    Represents the version number of the header
        ///    format.
        /// </devdoc>
        public short Version
        {
            get { return _version; }
            set { _version = value; }
        }
        /// <include file='doc\METAHEADER.uex' path='docs/doc[@for="MetaHeader.Size"]/*' />
        /// <devdoc>
        ///    Represents the sizi, in bytes, of the
        ///    associated <see cref='System.Drawing.Imaging.Metafile'/>.
        /// </devdoc>
        public int Size
        {
            get { return _size; }
            set { _size = value; }
        }
        /// <include file='doc\METAHEADER.uex' path='docs/doc[@for="MetaHeader.NoObjects"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public short NoObjects
        {
            get { return _noObjects; }
            set { _noObjects = value; }
        }
        /// <include file='doc\METAHEADER.uex' path='docs/doc[@for="MetaHeader.MaxRecord"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int MaxRecord
        {
            get { return _maxRecord; }
            set { _maxRecord = value; }
        }
        /// <include file='doc\METAHEADER.uex' path='docs/doc[@for="MetaHeader.NoParameters"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public short NoParameters
        {
            get { return _noParameters; }
            set { _noParameters = value; }
        }
    }
}
