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

            Example of mounting a remote folder using sshfs and two Linux machines:

            In remote machine:
                - Install openssh-server.
                - Create an ext4 partition of 1 MB size.
                
            In local machine:
                - Install sshfs and openssh-client.
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
            string largefile = $"{mountedPath}/largefile.txt";
            string origin = $"{mountedPath}/copyme.txt";
            string destination = $"{mountedPath}/destination.txt";

            // Ensure the remote folder exists
            Assert.True(Directory.Exists(mountedPath));

            // Delete copied file if exists
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            // Create huge file if not exists
            if (!File.Exists(largefile))
            {
                File.WriteAllBytes(largefile, new byte[925696]);
            }

            // Create original file if not exists
            if (!File.Exists(origin))
            {
                File.WriteAllBytes(origin, new byte[8192]);
            }

            Assert.True(File.Exists(largefile));
            Assert.True(File.Exists(origin));

            using FileStream originStream = new FileStream(origin, FileMode.Open, FileAccess.Read);
            Stream destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write);
            originStream.CopyTo(destinationStream, 1);

            Assert.Throws<IOException>(() =>
            {
                destinationStream.Dispose();
            });
        }
    }
}
