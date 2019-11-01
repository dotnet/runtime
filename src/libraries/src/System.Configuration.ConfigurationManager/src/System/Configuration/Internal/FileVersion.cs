// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration.Internal
{
    internal class FileVersion
    {
        private readonly bool _exists;
        private readonly long _fileSize;
        private readonly DateTime _utcCreationTime;
        private readonly DateTime _utcLastWriteTime;

        internal FileVersion(bool exists, long fileSize, DateTime utcCreationTime, DateTime utcLastWriteTime)
        {
            _exists = exists;
            _fileSize = fileSize;
            _utcCreationTime = utcCreationTime;
            _utcLastWriteTime = utcLastWriteTime;
        }

        public override bool Equals(object obj)
        {
            FileVersion other = obj as FileVersion;
            return
                (other != null)
                && (_exists == other._exists)
                && (_fileSize == other._fileSize)
                && (_utcCreationTime == other._utcCreationTime)
                && (_utcLastWriteTime == other._utcLastWriteTime);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}