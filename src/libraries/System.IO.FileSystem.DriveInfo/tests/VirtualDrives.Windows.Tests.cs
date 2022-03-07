// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    // Separate class from the rest of the DriveInfo tests to prevent adding an extra virtual drive to GetDrives().
    public class DriveInfoVirtualDriveTests
    {
        // Cannot set the volume label on a SUBST'ed folder
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSubstAvailable))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SetVolumeLabel_OnVirtualDrive_Throws()
        {
            using VirtualDriveHelper virtualDrive = new();
            char letter = virtualDrive.VirtualDriveLetter; // Trigger calling subst
            DriveInfo drive = DriveInfo.GetDrives().Where(d => d.RootDirectory.FullName[0] == letter).FirstOrDefault();
            Assert.NotNull(drive);
            Assert.Throws<IOException>(() => drive.VolumeLabel = "impossible");
        }
    }
}