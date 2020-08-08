// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.ManualTests
{
    public class FileSystemManualTests
    {
        public static bool ManualTestsEnabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MANUAL_TESTS"));
        
        [ConditionalFact(nameof(ManualTestsEnabled))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void Throw_FileStreamDispose_WhenRemoteMountRunsOutOfSpace()
        {
            /*
            Pre-requisites to run this test:

            - The remote machine has a folder ~/share, where a small 1MB drive is mounted.
            - The remote drive is almost full.
            - The remote drive has one file, called "copyme.txt", large enough that if we attempt to programatically copy it into
              the same folder, the action should fail because the file is slightly larger than the available free space in the disk.
            - The local machine has a folder named "mountedremote" located in the local user's home folder.
            - The remote folder is mounted into "mountedremote".too


            Example of mounting a remote folder using sshfs and two Linux machines:

            In remote machine:
                - Install openssh-server.
                - Create a partition of 1 MB. You can use gparted and a spare USB drive. The filesystem does not seem to matter, but the bug could be repro'd with ext4.
                - Mount the drive in the 
                - Fill the new drive with files until almost full, leave a couple of bytes free.
                - Make sure to also include a small file that is larger than the available free space left and name it "copyme.txt".
                
            In local machine:
                - Install sshfs and openssh-client
                - Create a local folder inside the current user's home, named "mountedremote":
                    $ mkdir ~/mountedremote
                - Mount the remote folder into "mountedremote":
                    $ sudo sshfs -o allow_other,default_permissions remoteuser@xxx.xxx.xxx.xxx:/home/remoteuser/share /home/localuser/mountedremote
                - Set the environment variable MANUAL_TESTS=1
                - Run this manual test.
                - Expect the exception.
                - Unmount the folder:
                    $ fusermount -u ~/mountedremote
            */
            
            string mountedPath = $"{Environment.GetEnvironmentVariable("HOME")}/mountedremote";
            string origin      = $"{mountedPath}/copyme.txt";
            string destination = $"{mountedPath}/destination.txt";

            Assert.True(File.Exists(origin));

            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            Assert.Throws<IOException>(() =>
            {
                using (FileStream originStream = new FileStream(origin, FileMode.Open, FileAccess.Read))
                {
                    using (Stream destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write))
                    {
                        originStream.CopyTo(destinationStream, 1);
                    }
                }
            });
        }
    }
}