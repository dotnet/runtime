// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;

namespace System.IO
{
    public sealed partial class DriveInfo
    {
        private static string NormalizeDriveName(string driveName)
        {
            // TODO: Implement this
            throw NotImplemented.ByDesign;
        }

        public DriveType DriveType
        {
            [SecuritySafeCritical]
            get
            {
                // TODO: Implement this
                throw NotImplemented.ByDesign;
            }
        }

        public string DriveFormat
        {
            [SecuritySafeCritical]
            get
            {
                // TODO: Implement this
                throw NotImplemented.ByDesign;
            }
        }

        public long AvailableFreeSpace
        {
            [SecuritySafeCritical]
            get
            {
                // TODO: Implement this
                throw NotImplemented.ByDesign;
            }
        }

        public long TotalFreeSpace
        {
            [SecuritySafeCritical]
            get
            {
                // TODO: Implement this
                throw NotImplemented.ByDesign;
            }
        }

        public long TotalSize
        {
            [SecuritySafeCritical]
            get
            {
                // TODO: Implement this
                throw NotImplemented.ByDesign;
            }
        }

        public String VolumeLabel
        {
            [SecuritySafeCritical]
            get
            {
                // TODO: Implement this
                throw NotImplemented.ByDesign;
            }
            [SecuritySafeCritical]
            set
            {
                // TODO: Implement this
                throw NotImplemented.ByDesign;
            }
        }

        public static unsafe DriveInfo[] GetDrives()
        {
            // TODO: Implement this
            throw NotImplemented.ByDesign;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

    }
}
