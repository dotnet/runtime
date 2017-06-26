// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    public sealed class FrameDimension
    {
        // Frame dimension GUIDs, from sdkinc\imgguids.h
        private static FrameDimension s_time = new FrameDimension(new Guid("{6aedbd6d-3fb5-418a-83a6-7f45229dc872}"));
        private static FrameDimension s_resolution = new FrameDimension(new Guid("{84236f7b-3bd3-428f-8dab-4ea1439ca315}"));
        private static FrameDimension s_page = new FrameDimension(new Guid("{7462dc86-6180-4c7e-8e3f-ee7333a7a483}"));

        private Guid _guid;

        /// <summary>
        /// Initializes a new instance of the <see cref='FrameDimension'/> class with the specified GUID.
        /// </summary>
        public FrameDimension(Guid guid)
        {
            _guid = guid;
        }

        /// <summary>
        /// Specifies a global unique identifier (GUID) that represents this <see cref='FrameDimension'/>.
        /// </summary>
        public Guid Guid
        {
            get { return _guid; }
        }

        /// <summary>
        /// The time dimension.
        /// </summary>
        public static FrameDimension Time
        {
            get { return s_time; }
        }

        /// <summary>
        /// The resolution dimension.
        /// </summary>
        public static FrameDimension Resolution
        {
            get { return s_resolution; }
        }

        /// <summary>
        /// The page dimension.
        /// </summary>
        public static FrameDimension Page
        {
            get { return s_page; }
        }
        /// <summary>
        /// Returns a value indicating whether the specified object is an <see cref='FrameDimension'/> equivalent to
        /// this <see cref='FrameDimension'/>.
        /// </summary>
        public override bool Equals(object o)
        {
            FrameDimension format = o as FrameDimension;
            if (format == null)
                return false;
            return _guid == format._guid;
        }

        public override int GetHashCode()
        {
            return _guid.GetHashCode();
        }

        /// <summary>
        /// Converts this <see cref='FrameDimension'/> to a human-readable string.
        /// </summary>
        public override string ToString()
        {
            if (this == s_time) return "Time";
            if (this == s_resolution) return "Resolution";
            if (this == s_page) return "Page";
            return "[FrameDimension: " + _guid + "]";
        }
    }
}
