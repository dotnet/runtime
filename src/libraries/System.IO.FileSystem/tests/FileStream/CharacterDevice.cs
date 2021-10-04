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
            FileStreamOptions options = new() { Options = fileOptions, Access = FileAccess.Write };
            if (IsDeviceUnreachable(devicePath, options))
            {
                return;
            }

            using FileStream fs = new(devicePath, options);
            fs.Write(Encoding.UTF8.GetBytes("foo"));
        }

        [Theory]
        [MemberData(nameof(DevicePath_FileOptions_TestData))]
        public async Task CharacterDevice_FileStream_WriteAsync(string devicePath, FileOptions fileOptions)
        {
            FileStreamOptions options = new() { Options = fileOptions, Access = FileAccess.Write };
            if (IsDeviceUnreachable(devicePath, options))
            {
                return;
            }

            using FileStream fs = new(devicePath, options);
            await fs.WriteAsync(Encoding.UTF8.GetBytes("foo"));
        }

        [Theory]
        [MemberData(nameof(DevicePath_TestData))]
        public void CharacterDevice_WriteAllBytes(string devicePath)
        {
            if (IsDeviceUnreachable(devicePath, new FileStreamOptions{ Access = FileAccess.Write }))
            {
                return;
            }

            File.WriteAllBytes(devicePath, Encoding.UTF8.GetBytes("foo"));
        }

        [Theory]
        [MemberData(nameof(DevicePath_TestData))]
        public async Task CharacterDevice_WriteAllBytesAsync(string devicePath)
        {
            if (IsDeviceUnreachable(devicePath, new FileStreamOptions{ Options = FileOptions.Asynchronous, Access = FileAccess.Write }))
            {
                return;
            }

            await File.WriteAllBytesAsync(devicePath, Encoding.UTF8.GetBytes("foo"));
        }

        [Theory]
        [MemberData(nameof(DevicePath_TestData))]
        public void CharacterDevice_WriteAllText(string devicePath)
        {
            if (IsDeviceUnreachable(devicePath, new FileStreamOptions{ Access = FileAccess.Write }))
            {
                return;
            }

            File.WriteAllText(devicePath, "foo");
        }

        [Theory]
        [MemberData(nameof(DevicePath_TestData))]
        public async Task CharacterDevice_WriteAllTextAsync(string devicePath)
        {
            if (IsDeviceUnreachable(devicePath, new FileStreamOptions{ Options = FileOptions.Asynchronous, Access = FileAccess.Write }))
            {
                return;
            }

            await File.WriteAllTextAsync(devicePath, "foo");
        }

        private static bool IsDeviceUnreachable(string devicePath, FileStreamOptions? options)
        {
            if (!File.Exists(devicePath))
            {
                return true;
            }

            try
            {
                File.Open(devicePath, options).Dispose();
            }
            catch (IOException)
            {
                return true;
            }

            return false;
        }

        private static string[] DevicePaths = { "/dev/tty", "/dev/console", "/dev/null", "/dev/zero" };

        public static IEnumerable<object[]> DevicePath_FileOptions_TestData()
        {
            foreach (string devicePath in DevicePaths)
            {
                foreach (FileOptions options in new[] { FileOptions.None, FileOptions.Asynchronous })
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
