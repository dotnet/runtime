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

        [ConditionalFact(nameof(ManualTestsEnabled))]
        [PlatformSpecific(TestPlatforms.Linux)]
        public static void FileCopy_WorksToExFatVolume()
        {
            // We copy attributes after copying the file; when copying to EXFAT,
            // where all files appeared to be owned by root, we can't copy attributes
            // (unless we're root) and should skip silently

            /* This test requires an EXFAT partition. That can be created in memory like this:

            sudo mkdir /mnt/ramdisk
            sudo mount -t ramfs ramfs /mnt/ramdisk
            sudo dd if=/dev/zero of=/mnt/ramdisk/exfat.image bs=1M count=512
            sudo mkfs.exfat /mnt/ramdisk/exfat.image
            sudo mkdir /mnt/exfatrd
            sudo mount -o loop /mnt/ramdisk/exfat.image /mnt/exfatrd

            */

            File.WriteAllText("/mnt/exfatrd/1", "content");
            File.Copy("/mnt/exfatrd/1", "/mnt/exfatrd/2");
            Assert.True(File.Exists("/mnt/exfatrd/2"));
        }

        const long InitialFileSize = 1024;

        [ConditionalFact(nameof(ManualTestsEnabled))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void SetLength_DoesNotAlterPositionWhenNativeCallFails()
        {
            /* This test verifies that Position is not altered when SetLength fails when a "disk out of space" error occurs.
             
                Setup environment to have a drive with less than 1k available space:
                - Create an 8mb fixed size VHD.
                    - Open Computer Management -> Storage -> Disk Management
                    - Follow these instructions:
                      https://docs.microsoft.com/en-us/windows-server/storage/disk-management/manage-virtual-hard-disks

                - Restrict the space available in the VHD.
                    - Create a 512 bytes quota in the VHD created above using cmd:
                      fsutil quota modify E: 512 512 SYSTEM
                      fsutil quota modify E: 512 512 YourUser

                - Run the test. If configured correctly, the SetLength operation should fail at least once.
             */

            using FileStream fs = File.Open("E:/dummy_file.txt", FileMode.OpenOrCreate);

            // Position was less than new Length; should remain the same.
            fs.Seek(0, SeekOrigin.Begin);
            VerifySetLength(fs);
            Assert.Equal(0, fs.Position);
            Assert.True(fs.Position < fs.Length);

            // Position was larger than new Length; should be adjusted to the Length.
            fs.Seek(InitialFileSize + 1, SeekOrigin.Begin);
            VerifySetLength(fs);
            Assert.Equal(fs.Length, fs.Position);
        }

        private static void VerifySetLength(FileStream fs)
        {
            long originalPosition = fs.Position;
            bool success = false;
            long size = InitialFileSize;

            while (!success)
            {
                try
                {
                    Console.WriteLine($"Attempting to write {size} bytes...");
                    fs.SetLength(size);
                    Console.WriteLine("Success!");
                    success = true;
                }
                catch (IOException)
                {
                    Console.WriteLine("Failed.");
                    Assert.Equal(originalPosition, fs.Position);
                    size = (long)(size * 0.9);
                }
            }
        }
    }
}
