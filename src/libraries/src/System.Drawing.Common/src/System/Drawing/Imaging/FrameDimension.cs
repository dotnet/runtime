// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /**
     * frame dimension constants (used with Bitmap.FrameDimensionsList)
     */
    /// <include file='doc\FrameDimension.uex' path='docs/doc[@for="FrameDimension"]/*' />
    /// <devdoc>
    ///    
    /// </devdoc>
    // [TypeConverterAttribute(typeof(FrameDimensionConverter))]
    public sealed class FrameDimension
    {
        // Frame dimension GUIDs, from sdkinc\imgguids.h
        private static FrameDimension s_time = new FrameDimension(new Guid("{6aedbd6d-3fb5-418a-83a6-7f45229dc872}"));
        private static FrameDimension s_resolution = new FrameDimension(new Guid("{84236f7b-3bd3-428f-8dab-4ea1439ca315}"));
        private static FrameDimension s_page = new FrameDimension(new Guid("{7462dc86-6180-4c7e-8e3f-ee7333a7a483}"));

        private Guid _guid;

        /// <include file='doc\FrameDimension.uex' path='docs/doc[@for="FrameDimension.FrameDimension"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Imaging.FrameDimension'/> class with the specified GUID.
        /// </devdoc>
        public FrameDimension(Guid guid)
        {
            _guid = guid;
        }

        /// <include file='doc\FrameDimension.uex' path='docs/doc[@for="FrameDimension.Guid"]/*' />
        /// <devdoc>
        ///    Specifies a global unique identifier (GUID)
        ///    that represents this <see cref='System.Drawing.Imaging.FrameDimension'/>.
        /// </devdoc>
        public Guid Guid
        {
            get { return _guid; }
        }

        /// <include file='doc\FrameDimension.uex' path='docs/doc[@for="FrameDimension.Time"]/*' />
        /// <devdoc>
        ///    The time dimension.
        /// </devdoc>
        public static FrameDimension Time
        {
            get { return s_time; }
        }

        /// <include file='doc\FrameDimension.uex' path='docs/doc[@for="FrameDimension.Resolution"]/*' />
        /// <devdoc>
        ///    The resolution dimension.
        /// </devdoc>
        public static FrameDimension Resolution
        {
            get { return s_resolution; }
        }

        /// <include file='doc\FrameDimension.uex' path='docs/doc[@for="FrameDimension.Page"]/*' />
        /// <devdoc>
        ///    The page dimension.
        /// </devdoc>
        public static FrameDimension Page
        {
            get { return s_page; }
        }
        /// <include file='doc\FrameDimension.uex' path='docs/doc[@for="FrameDimension.Equals"]/*' />
        /// <devdoc>
        ///    Returns a value indicating whether the
        ///    specified object is an <see cref='System.Drawing.Imaging.FrameDimension'/> equivalent to this <see cref='System.Drawing.Imaging.FrameDimension'/>.
        /// </devdoc>
        public override bool Equals(object o)
        {
            FrameDimension format = o as FrameDimension;
            if (format == null)
                return false;
            return _guid == format._guid;
        }

        /// <include file='doc\FrameDimension.uex' path='docs/doc[@for="FrameDimension.GetHashCode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override int GetHashCode()
        {
            return _guid.GetHashCode();
        }

        /// <include file='doc\FrameDimension.uex' path='docs/doc[@for="FrameDimension.ToString"]/*' />
        /// <devdoc>
        ///    Converts this <see cref='System.Drawing.Imaging.FrameDimension'/> to a human-readable string.
        /// </devdoc>
        public override string ToString()
        {
            if (this == s_time) return "Time";
            if (this == s_resolution) return "Resolution";
            if (this == s_page) return "Page";
            return "[FrameDimension: " + _guid + "]";
        }
    }
}
