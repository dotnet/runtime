// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public class CharacterDevice
    {
        [Theory]
        [MemberData(nameof(DevicePath_FileOptions_TestData))]
        public void CharacterDevice_FileStream_Write(string devicePath, FileOptions fileOptions)
        {
            VerifyDeviceExists(devicePath);
            using FileStream fs = new(devicePath, new FileStreamOptions { Options = fileOptions, Access = FileAccess.Write });
            fs.Write(Encoding.UTF8.GetBytes("foo"));
        }

        [Theory]
        [MemberData(nameof(DevicePath_FileOptions_TestData))]
        public async Task CharacterDevice_FileStream_WriteAsync(string devicePath, FileOptions fileOptions)
        {
            VerifyDeviceExists(devicePath);
            using FileStream fs = new(devicePath, new FileStreamOptions { Options = fileOptions, Access = FileAccess.Write  });
            await fs.WriteAsync(Encoding.UTF8.GetBytes("foo"));
        }

        [Theory]
        [MemberData(nameof(DevicePath_TestData))]
        public void CharacterDevice_WriteAllBytes(string devicePath)
        {
            VerifyDeviceExists(devicePath);
            File.WriteAllBytes(devicePath, Encoding.UTF8.GetBytes("foo"));
        }

        [Theory]
        [MemberData(nameof(DevicePath_TestData))]
        public async Task CharacterDevice_WriteAllBytesAsync(string devicePath)
        {
            VerifyDeviceExists(devicePath);
            await File.WriteAllBytesAsync(devicePath, Encoding.UTF8.GetBytes("foo"));
        }

        [Theory]
        [MemberData(nameof(DevicePath_TestData))]
        public void CharacterDevice_WriteAllText(string devicePath)
        {
            VerifyDeviceExists(devicePath);
            File.WriteAllText(devicePath, "foo");
        }

        [Theory]
        [MemberData(nameof(DevicePath_TestData))]
        public async Task CharacterDevice_WriteAllTextAsync(string devicePath)
        {
            VerifyDeviceExists(devicePath);
            await File.WriteAllTextAsync(devicePath, "foo");
        }

        private static void VerifyDeviceExists(string devicePath)
        {
            if (!File.Exists(devicePath))
            {
                throw new SkipTestException("Device does not exists in this platform");
            }   
        }

        private static string[] DevicePaths = { "/dev/tty", "/dev/console", "/dev/null", "/dev/zero" };

        public static IEnumerable<object[]> DevicePath_FileOptions_TestData()
        {
            foreach (string devicePath in DevicePaths)
            {
                foreach (FileOptions options in Enum.GetValues<FileOptions>())
                {
                    yield return new object[] { devicePath, options};
                }
            }
        }

        public static IEnumerable<object[]> DevicePath_TestData()
        {
            foreach (string devicePath in DevicePaths)
            {
                yield return new object[] { devicePath };
            }
        }
    }
}
