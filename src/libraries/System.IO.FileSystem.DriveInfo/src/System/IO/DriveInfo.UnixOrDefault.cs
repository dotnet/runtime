// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Security;

namespace System.IO
{
    public sealed partial class DriveInfo
    {
        public static DriveInfo[] GetDrives()
        {
            string[] mountPoints = GetMountPoints();
            DriveInfo[] info = new DriveInfo[mountPoints.Length];
            for (int i = 0; i < info.Length; i++)
            {
                info[i] = new DriveInfo(mountPoints[i]);
            }

            return info;
        }

        private static string NormalizeDriveName(string driveName)
        {
            if (driveName.Contains('\0'))
            {
                throw new ArgumentException(SR.Format(SR.Arg_InvalidDriveChars, driveName), nameof(driveName));
            }
            if (driveName.Length == 0)
            {
                throw new ArgumentException(SR.Arg_MustBeNonEmptyDriveName, nameof(driveName));
            }
            return driveName;
        }

        [AllowNull]
        public string VolumeLabel
        {
            get
            {
                return Name;
            }
            [SupportedOSPlatform("windows")]
            set
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}
