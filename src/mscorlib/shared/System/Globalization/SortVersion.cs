// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    [Serializable]
    public sealed class SortVersion : IEquatable<SortVersion>
    {
        private int _nlsVersion;
        private Guid _sortId;

        public int FullVersion
        {
            get
            {
                return _nlsVersion;
            }
        }

        public Guid SortId
        {
            get
            {
                return _sortId;
            }
        }

        public SortVersion(int fullVersion, Guid sortId)
        {
            _sortId = sortId;
            _nlsVersion = fullVersion;
        }

        internal SortVersion(int nlsVersion, int effectiveId, Guid customVersion)
        {
            _nlsVersion = nlsVersion;

            if (customVersion == Guid.Empty)
            {
                byte b1 = (byte)(effectiveId >> 24);
                byte b2 = (byte)((effectiveId & 0x00FF0000) >> 16);
                byte b3 = (byte)((effectiveId & 0x0000FF00) >> 8);
                byte b4 = (byte)(effectiveId & 0xFF);
                customVersion = new Guid(0, 0, 0, 0, 0, 0, 0, b1, b2, b3, b4);
            }

            _sortId = customVersion;
        }

        public override bool Equals(object obj)
        {
            SortVersion n = obj as SortVersion;
            if (n != null)
            {
                return this.Equals(n);
            }

            return false;
        }

        public bool Equals(SortVersion other)
        {
            if (other == null)
            {
                return false;
            }

            return _nlsVersion == other._nlsVersion && _sortId == other._sortId;
        }

        public override int GetHashCode()
        {
            return _nlsVersion * 7 | _sortId.GetHashCode();
        }

        public static bool operator ==(SortVersion left, SortVersion right)
        {
            if (((object)left) != null)
            {
                return left.Equals(right);
            }

            if (((object)right) != null)
            {
                return right.Equals(left);
            }

            // Both null.
            return true;
        }

        public static bool operator !=(SortVersion left, SortVersion right)
        {
            return !(left == right);
        }
    }
}
